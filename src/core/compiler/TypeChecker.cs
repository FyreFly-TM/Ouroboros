using System;
using System.Collections.Generic;
using System.Linq;
using Ouroboros.Core.AST;
using Ouroboros.Core.VM;
using Ouroboros.Tokens;

namespace Ouroboros.Core.Compiler
{
    /// <summary>
    /// Type checker for semantic analysis (simplified version)
    /// </summary>
    public class TypeChecker : IAstVisitor<TypeNode>
    {
        private SymbolTable symbols;
        private TypeRegistry typeRegistry;
        private List<TypeCheckError> errors;
        private Stack<Dictionary<string, TypeNode>> scopes;
        private TypeNode currentReturnType;
        
        public TypeChecker()
        {
            symbols = new SymbolTable();
            typeRegistry = new TypeRegistry();
            errors = new List<TypeCheckError>();
            scopes = new Stack<Dictionary<string, TypeNode>>();
            scopes.Push(new Dictionary<string, TypeNode>()); // Global scope
        }
        
        public Ouroboros.Core.AST.Program Check(Ouroboros.Core.AST.Program ast)
        {
            ast.Accept(this);
            
            if (errors.Count > 0)
            {
                throw new TypeCheckException(errors);
            }
            
            return ast;
        }
        
        public TypeNode GetType(Expression expr)
        {
            return expr.Accept(this);
        }
        
        private void EnterScope()
        {
            scopes.Push(new Dictionary<string, TypeNode>());
        }
        
        private void ExitScope()
        {
            if (scopes.Count > 1)
                scopes.Pop();
        }
        
        private void DefineVariable(string name, TypeNode type)
        {
            scopes.Peek()[name] = type;
        }
        
        private TypeNode LookupVariable(string name)
        {
            foreach (var scope in scopes)
            {
                if (scope.ContainsKey(name))
                    return scope[name];
            }
            return null;
        }
        
        private void AddError(string message, int line, int column)
        {
            errors.Add(new TypeCheckError(message, line, column));
        }
        
        // Visitor implementations
        public TypeNode VisitProgram(Ouroboros.Core.AST.Program program) 
        { 
            foreach (var s in program.Statements) 
                s.Accept(this); 
            return null; 
        }
        
        public TypeNode VisitBinaryExpression(BinaryExpression expr) 
        { 
            var leftType = expr.Left.Accept(this);
            var rightType = expr.Right.Accept(this);
            
            switch (expr.Operator.Type)
            {
                case TokenType.Plus:
                case TokenType.Minus:
                case TokenType.Multiply:
                case TokenType.Divide:
                case TokenType.Modulo:
                case TokenType.Power:
                    if (IsNumericType(leftType) && IsNumericType(rightType))
                        return GetNumericResultType(leftType, rightType);
                    if (expr.Operator.Type == TokenType.Plus && 
                        (leftType?.Name == "string" || rightType?.Name == "string"))
                        return typeRegistry.String;
                    AddError($"Invalid operands for {expr.Operator.Lexeme}: {leftType?.Name} and {rightType?.Name}", 
                            expr.Line, expr.Column);
                    return typeRegistry.Unknown;
                    
                case TokenType.Equal:
                case TokenType.NotEqual:
                case TokenType.Less:
                case TokenType.Greater:
                case TokenType.LessEqual:
                case TokenType.GreaterEqual:
                    return typeRegistry.Bool;
                    
                case TokenType.LogicalAnd:
                case TokenType.LogicalOr:
                    if (leftType?.Name == "bool" && rightType?.Name == "bool")
                        return typeRegistry.Bool;
                    AddError("Logical operators require boolean operands", expr.Line, expr.Column);
                    return typeRegistry.Bool;
                    
                default:
                    return typeRegistry.Unknown;
            }
        }
        
        public TypeNode VisitUnaryExpression(UnaryExpression expr) 
        { 
            var operandType = expr.Operand.Accept(this);
            
            switch (expr.Operator.Type)
            {
                case TokenType.Minus:
                case TokenType.Plus:
                    if (IsNumericType(operandType))
                        return operandType;
                    AddError($"Unary {expr.Operator.Lexeme} requires numeric operand", expr.Line, expr.Column);
                    return typeRegistry.Unknown;
                    
                case TokenType.LogicalNot:
                    if (operandType?.Name == "bool")
                        return typeRegistry.Bool;
                    AddError("Logical not requires boolean operand", expr.Line, expr.Column);
                    return typeRegistry.Bool;
                    
                case TokenType.Increment:
                case TokenType.Decrement:
                    if (IsNumericType(operandType))
                        return operandType;
                    AddError($"{expr.Operator.Lexeme} requires numeric operand", expr.Line, expr.Column);
                    return typeRegistry.Unknown;
                    
                default:
                    return typeRegistry.Unknown;
            }
        }
        
        public TypeNode VisitLiteralExpression(LiteralExpression expr) 
        {
            if (expr.Value == null)
                return typeRegistry.Null;
                
            switch (expr.Value)
            {
                case int _:
                    return typeRegistry.Int;
                case double _:
                    return typeRegistry.Double;
                case float _:
                    return typeRegistry.Float;
                case string _:
                    return typeRegistry.String;
                case bool _:
                    return typeRegistry.Bool;
                case char _:
                    return typeRegistry.Char;
                default:
                    return typeRegistry.Unknown;
            }
        }
        
        public TypeNode VisitIdentifierExpression(IdentifierExpression expr) 
        {
            var type = LookupVariable(expr.Name);
            if (type == null)
            {
                AddError($"Undefined variable '{expr.Name}'", expr.Line, expr.Column);
                return typeRegistry.Unknown;
            }
            return type;
        }
        
        public TypeNode VisitAssignmentExpression(AssignmentExpression expr) 
        { 
            var targetType = expr.Target.Accept(this);
            var valueType = expr.Value.Accept(this);
            
            if (targetType != null && valueType != null && !AreTypesCompatible(targetType, valueType))
            {
                AddError($"Cannot assign {valueType.Name} to {targetType.Name}", expr.Line, expr.Column);
            }
            
            return valueType;
        }
        
        public TypeNode VisitVariableDeclaration(VariableDeclaration stmt) 
        { 
            var declaredType = stmt.Type;
            
            if (stmt.Initializer != null)
            {
                var initType = stmt.Initializer.Accept(this);
                if (!AreTypesCompatible(declaredType, initType))
                {
                    AddError($"Cannot initialize {declaredType.Name} with {initType?.Name}", stmt.Line, stmt.Column);
                }
            }
            
            DefineVariable(stmt.Name, declaredType);
            return null;
        }
        
        public TypeNode VisitIfStatement(IfStatement stmt) 
        { 
            var condType = stmt.Condition.Accept(this);
            if (condType?.Name != "bool")
            {
                AddError("If condition must be boolean", stmt.Line, stmt.Column);
            }
            
            stmt.ThenBranch.Accept(this);
            stmt.ElseBranch?.Accept(this);
            return null;
        }
        
        public TypeNode VisitBlockStatement(BlockStatement stmt) 
        { 
            EnterScope();
            foreach (var s in stmt.Statements) 
                s.Accept(this);
            ExitScope();
            return null;
        }
        
        public TypeNode VisitFunctionDeclaration(FunctionDeclaration decl) 
        { 
            EnterScope();
            
            // Define parameters
            foreach (var param in decl.Parameters)
            {
                DefineVariable(param.Name, param.Type);
            }
            
            // Set current return type for return statement checking
            var previousReturnType = currentReturnType;
            currentReturnType = decl.ReturnType;
            
            decl.Body.Accept(this);
            
            currentReturnType = previousReturnType;
            ExitScope();
            
            // Store function type
            var funcType = new FunctionTypeNode(
                decl.Parameters.Select(p => p.Type).ToList(),
                decl.ReturnType
            );
            DefineVariable(decl.Name, funcType);
            
            return null;
        }
        
        public TypeNode VisitReturnStatement(ReturnStatement stmt) 
        { 
            if (stmt.Value != null)
            {
                var returnType = stmt.Value.Accept(this);
                if (currentReturnType != null && !AreTypesCompatible(currentReturnType, returnType))
                {
                    AddError($"Return type mismatch: expected {currentReturnType.Name}, got {returnType?.Name}", 
                            stmt.Line, stmt.Column);
                }
            }
            else if (currentReturnType?.Name != "void")
            {
                AddError("Missing return value", stmt.Line, stmt.Column);
            }
            return null;
        }
        
        public TypeNode VisitCallExpression(CallExpression expr) 
        { 
            var calleeType = expr.Callee.Accept(this);
            
            if (calleeType is FunctionTypeNode funcType)
            {
                // Check argument count
                if (expr.Arguments.Count != funcType.ParameterTypes.Count)
                {
                    AddError($"Wrong number of arguments: expected {funcType.ParameterTypes.Count}, got {expr.Arguments.Count}", 
                            expr.Line, expr.Column);
                }
                else
                {
                    // Check argument types
                    for (int i = 0; i < expr.Arguments.Count; i++)
                    {
                        var argType = expr.Arguments[i].Accept(this);
                        if (!AreTypesCompatible(funcType.ParameterTypes[i], argType))
                        {
                            AddError($"Argument {i + 1} type mismatch: expected {funcType.ParameterTypes[i].Name}, got {argType?.Name}", 
                                    expr.Line, expr.Column);
                        }
                    }
                }
                
                return funcType.ReturnType;
            }
            else
            {
                // Try built-in functions
                if (expr.Callee is IdentifierExpression id)
                {
                    switch (id.Name)
                    {
                        case "print":
                        case "println":
                            foreach (var arg in expr.Arguments)
                                arg.Accept(this);
                            return typeRegistry.Void;
                        case "len":
                        case "length":
                            if (expr.Arguments.Count == 1)
                            {
                                expr.Arguments[0].Accept(this);
                                return typeRegistry.Int;
                            }
                            break;
                    }
                }
                
                foreach (var a in expr.Arguments) 
                    a.Accept(this);
                return typeRegistry.Unknown;
            }
        }
        
        private bool IsNumericType(TypeNode type)
        {
            if (type == null) return false;
            return type.Name == "int" || type.Name == "float" || type.Name == "double" || 
                   type.Name == "byte" || type.Name == "short" || type.Name == "long";
        }
        
        private TypeNode GetNumericResultType(TypeNode left, TypeNode right)
        {
            // Type promotion rules
            if (left.Name == "double" || right.Name == "double")
                return typeRegistry.Double;
            if (left.Name == "float" || right.Name == "float")
                return typeRegistry.Float;
            if (left.Name == "long" || right.Name == "long")
                return typeRegistry.Long;
            return typeRegistry.Int;
        }
        
        private bool AreTypesCompatible(TypeNode target, TypeNode source)
        {
            if (target == null || source == null) return true; // Be lenient
            if (target.Name == source.Name) return true;
            if (target.Name == "?" || source.Name == "?") return true; // Unknown type
            
            // Numeric conversions
            if (IsNumericType(target) && IsNumericType(source))
            {
                // Allow widening conversions
                var targetWidth = GetTypeWidth(target);
                var sourceWidth = GetTypeWidth(source);
                return targetWidth >= sourceWidth;
            }
            
            // Any type accepts null
            if (source.Name == "null" && target.IsNullable)
                return true;
                
            return false;
        }
        
        private int GetTypeWidth(TypeNode type)
        {
            switch (type.Name)
            {
                case "byte": return 1;
                case "short": return 2;
                case "int": return 4;
                case "long": return 8;
                case "float": return 4;
                case "double": return 8;
                default: return 0;
            }
        }
        
        // Properly implemented visitor methods
        public TypeNode VisitMemberExpression(MemberExpression expr) 
        { 
            var objectType = expr.Object.Accept(this);
            
            // Handle array/string length
            if (expr.MemberName == "Length" || expr.MemberName == "length")
            {
                if (objectType is ArrayTypeNode || objectType?.Name == "string")
                    return typeRegistry.Int;
            }
            
            // Handle type member access
            if (objectType is TypeNode type)
            {
                // Look up member in type
                // This would require type metadata in a full implementation
                return new TypeNode($"{type.Name}.{expr.MemberName}");
            }
            
            return new TypeNode("dynamic");
        }
        
        public TypeNode VisitArrayExpression(ArrayExpression expr) 
        { 
            if (expr.Elements.Count == 0)
                return new ArrayTypeNode(typeRegistry.Unknown);
                
            // Infer element type from first element
            var elementType = expr.Elements[0].Accept(this);
            
            // Check all elements have compatible types
            foreach (var element in expr.Elements.Skip(1))
            {
                var elemType = element.Accept(this);
                if (!AreTypesCompatible(elementType, elemType))
                {
                    AddError($"Array elements must have consistent types", expr.Line, expr.Column);
                }
            }
            
            return new ArrayTypeNode(elementType);
        }
        
        public TypeNode VisitLambdaExpression(LambdaExpression expr) 
        { 
            EnterScope();
            
            // Define parameters
            var paramTypes = new List<TypeNode>();
            foreach (var param in expr.Parameters)
            {
                var paramType = param.Type ?? typeRegistry.Unknown;
                DefineVariable(param.Name, paramType);
                paramTypes.Add(paramType);
            }
            
            // Infer return type from body
            TypeNode returnType;
            if (expr.Body is Expression bodyExpr)
            {
                returnType = bodyExpr.Accept(this);
            }
            else
            {
                // Statement body - need to analyze return statements
                var previousReturnType = currentReturnType;
                currentReturnType = null;
                expr.Body.Accept(this);
                returnType = currentReturnType ?? typeRegistry.Void;
                currentReturnType = previousReturnType;
            }
            
            ExitScope();
            
            return new FunctionTypeNode(paramTypes, returnType);
        }
        
        public TypeNode VisitThisExpression(ThisExpression expr)
        {
            // Look up current class/struct type
            var thisType = LookupVariable("this");
            if (thisType == null)
            {
                AddError("'this' can only be used inside class/struct methods", expr.Line, expr.Column);
                return typeRegistry.Unknown;
            }
            return thisType;
        }
        
        public TypeNode VisitBaseExpression(BaseExpression expr)
        {
            // Look up base class type
            var thisType = LookupVariable("this");
            if (thisType == null)
            {
                AddError("'base' can only be used inside class methods", expr.Line, expr.Column);
                return typeRegistry.Unknown;
            }
            // Would need to look up base type from class hierarchy
            return new TypeNode($"{thisType.Name}.Base");
        }
        
        public TypeNode VisitGenericIdentifierExpression(GenericIdentifierExpression expr)
        {
            // Handle generic type instantiation
            var baseType = LookupVariable(expr.Name);
            if (baseType == null)
            {
                // Try as type name
                baseType = new TypeNode(expr.Name);
            }
            
            // Build generic type name
            var typeArgs = expr.GenericTypeArguments.Select(t => t.Name).ToArray();
            return new TypeNode($"{expr.Name}<{string.Join(", ", typeArgs)}>");
        }
        
        public TypeNode VisitNewExpression(NewExpression expr) { foreach (var a in expr.Arguments) a.Accept(this); return expr.Type; }
        public TypeNode VisitConditionalExpression(ConditionalExpression expr) { expr.Condition.Accept(this); var t1 = expr.TrueExpression.Accept(this); var t2 = expr.FalseExpression.Accept(this); return t1; }
        public TypeNode VisitTypeofExpression(TypeofExpression expr) => typeRegistry.Type;
        public TypeNode VisitSizeofExpression(SizeofExpression expr) => typeRegistry.Int;
        public TypeNode VisitNameofExpression(NameofExpression expr) => typeRegistry.String;
        public TypeNode VisitInterpolatedStringExpression(InterpolatedStringExpression expr) { foreach (var p in expr.Parts) p.Accept(this); return typeRegistry.String; }
        public TypeNode VisitVectorExpression(VectorExpression expr) { foreach (var c in expr.Components) c.Accept(this); return new TypeNode($"Vector{expr.Dimensions}"); }
        public TypeNode VisitMatrixExpression(MatrixExpression expr) { foreach (var r in expr.Elements) foreach (var e in r) e.Accept(this); return new TypeNode($"Matrix{expr.Rows}x{expr.Columns}"); }
        public TypeNode VisitQuaternionExpression(QuaternionExpression expr) { expr.W.Accept(this); expr.X.Accept(this); expr.Y.Accept(this); expr.Z.Accept(this); return new TypeNode("Quaternion"); }
        public TypeNode VisitMathExpression(MathExpression expr) { foreach (var o in expr.Operands) o.Accept(this); return typeRegistry.Double; }
        
        public TypeNode VisitExpressionStatement(ExpressionStatement stmt) { stmt.Expression.Accept(this); return null; }
        public TypeNode VisitWhileStatement(WhileStatement stmt) { var cond = stmt.Condition.Accept(this); if (cond?.Name != "bool") AddError("While condition must be boolean", stmt.Line, stmt.Column); stmt.Body.Accept(this); return null; }
        public TypeNode VisitDoWhileStatement(DoWhileStatement stmt) { stmt.Body.Accept(this); var cond = stmt.Condition.Accept(this); if (cond?.Name != "bool") AddError("Do-while condition must be boolean", stmt.Line, stmt.Column); return null; }
        public TypeNode VisitForStatement(ForStatement stmt) { EnterScope(); stmt.Initializer?.Accept(this); stmt.Condition?.Accept(this); stmt.Update?.Accept(this); stmt.Body.Accept(this); ExitScope(); return null; }
        public TypeNode VisitForEachStatement(ForEachStatement stmt) { EnterScope(); stmt.Collection.Accept(this); DefineVariable(stmt.ElementName, stmt.ElementType); stmt.Body.Accept(this); ExitScope(); return null; }
        public TypeNode VisitRepeatStatement(RepeatStatement stmt) { stmt.Count?.Accept(this); stmt.Body.Accept(this); return null; }
        public TypeNode VisitIterateStatement(IterateStatement stmt) { EnterScope(); stmt.Start.Accept(this); stmt.End.Accept(this); stmt.Step.Accept(this); DefineVariable(stmt.IteratorName, typeRegistry.Int); stmt.Body.Accept(this); ExitScope(); return null; }
        public TypeNode VisitParallelForStatement(ParallelForStatement stmt) { stmt.BaseFor.Accept(this); return null; }
        public TypeNode VisitSwitchStatement(SwitchStatement stmt) { stmt.Expression.Accept(this); foreach (var c in stmt.Cases) { c.Value.Accept(this); foreach (var s in c.Statements) s.Accept(this); } stmt.DefaultCase?.Accept(this); return null; }
        public TypeNode VisitBreakStatement(BreakStatement stmt) => null;
        public TypeNode VisitContinueStatement(ContinueStatement stmt) => null;
        public TypeNode VisitThrowStatement(ThrowStatement stmt) { stmt.Exception.Accept(this); return null; }
        public TypeNode VisitTryStatement(TryStatement stmt) { stmt.TryBlock.Accept(this); foreach (var c in stmt.CatchClauses) c.Body.Accept(this); stmt.FinallyBlock?.Accept(this); return null; }
        public TypeNode VisitMatchStatement(MatchStatement stmt) { stmt.Expression.Accept(this); foreach (var c in stmt.Cases) { c.Guard?.Accept(this); c.Body.Accept(this); } return null; }
        public TypeNode VisitUsingStatement(UsingStatement stmt) { stmt.Resource.Accept(this); stmt.Body.Accept(this); return null; }
        public TypeNode VisitLockStatement(LockStatement stmt) { stmt.LockObject.Accept(this); stmt.Body.Accept(this); return null; }
        public TypeNode VisitUnsafeStatement(UnsafeStatement stmt) { stmt.Body.Accept(this); return null; }
        public TypeNode VisitFixedStatement(FixedStatement stmt) { stmt.Target.Accept(this); stmt.Body.Accept(this); return null; }
        public TypeNode VisitYieldStatement(YieldStatement stmt) { stmt.Value?.Accept(this); return null; }
        public TypeNode VisitAssemblyStatement(AssemblyStatement stmt) => null;
        
        public TypeNode VisitClassDeclaration(ClassDeclaration decl) { EnterScope(); foreach (var m in decl.Members) m.Accept(this); ExitScope(); return null; }
        public TypeNode VisitInterfaceDeclaration(InterfaceDeclaration decl) { foreach (var m in decl.Members) m.Accept(this); return null; }
        public TypeNode VisitStructDeclaration(StructDeclaration decl) { EnterScope(); foreach (var m in decl.Members) m.Accept(this); ExitScope(); return null; }
        public TypeNode VisitEnumDeclaration(EnumDeclaration decl) => null;
        public TypeNode VisitDomainDeclaration(DomainDeclaration decl) => null;
        public TypeNode VisitPropertyDeclaration(PropertyDeclaration decl) => null;
        public TypeNode VisitFieldDeclaration(FieldDeclaration decl) => null;
        public TypeNode VisitComponentDeclaration(ComponentDeclaration decl) => null;
        public TypeNode VisitSystemDeclaration(SystemDeclaration decl) { foreach (var m in decl.Methods) m.Accept(this); return null; }
        public TypeNode VisitEntityDeclaration(EntityDeclaration decl) => null;
        public TypeNode VisitNamespaceDeclaration(NamespaceDeclaration decl) { EnterScope(); foreach (var m in decl.Members) m.Accept(this); ExitScope(); return null; }
        public TypeNode VisitImportDeclaration(ImportDeclaration decl) => null;
        public TypeNode VisitTypeAliasDeclaration(TypeAliasDeclaration decl) => null;
        public TypeNode VisitIsExpression(IsExpression expr) { expr.Left.Accept(this); return typeRegistry.Bool; }
        public TypeNode VisitCastExpression(CastExpression expr) { expr.Expression.Accept(this); return expr.TargetType; }
        public TypeNode VisitMatchExpression(MatchExpression expr) { expr.Target.Accept(this); TypeNode resultType = null; foreach (var arm in expr.Arms) { arm.Guard?.Accept(this); var armType = arm.Body.Accept(this); if (resultType == null) resultType = armType; } return resultType ?? typeRegistry.Unknown; }
        public TypeNode VisitThrowExpression(ThrowExpression expr) { expr.Expression?.Accept(this); return typeRegistry.Unknown; }
        public TypeNode VisitMatchArm(MatchArm arm) { arm.Guard?.Accept(this); return arm.Body.Accept(this); }
        public TypeNode VisitMacroDeclaration(MacroDeclaration decl) => null;
        public TypeNode VisitTraitDeclaration(TraitDeclaration decl) => null;
        public TypeNode VisitImplementDeclaration(ImplementDeclaration decl) => null;
    
    public TypeNode VisitStructLiteral(StructLiteral expr) 
    { 
        // Find the struct type
        var structType = new TypeNode(expr.StructName.Lexeme);
        
        // Type check all field values
        foreach (var field in expr.Fields)
        {
            field.Value.Accept(this);
        }
        
        return structType;
    }
    }
    
    /// <summary>
    /// Simple type registry
    /// </summary>
    public class TypeRegistry
    {
        // Built-in types
        public TypeNode Unknown { get; } = new TypeNode("?");
        public TypeNode Int { get; } = new TypeNode("int");
        public TypeNode Double { get; } = new TypeNode("double");
        public TypeNode Float { get; } = new TypeNode("float");
        public TypeNode String { get; } = new TypeNode("string");
        public TypeNode Bool { get; } = new TypeNode("bool");
        public TypeNode Char { get; } = new TypeNode("char");
        public TypeNode Void { get; } = new TypeNode("void");
        public TypeNode Null { get; } = new TypeNode("null");
        public TypeNode Type { get; } = new TypeNode("Type");
        public TypeNode Long { get; } = new TypeNode("long");
        public TypeNode Byte { get; } = new TypeNode("byte");
        public TypeNode Short { get; } = new TypeNode("short");
    }
    
    /// <summary>
    /// Type check error
    /// </summary>
    public class TypeCheckError
    {
        public string Message { get; }
        public int Line { get; }
        public int Column { get; }
        
        public TypeCheckError(string message, int line, int column)
        {
            Message = message;
            Line = line;
            Column = column;
        }
    }
    
    /// <summary>
    /// Type check exception
    /// </summary>
    public class TypeCheckException : Exception
    {
        public List<TypeCheckError> Errors { get; }
        
        public TypeCheckException(List<TypeCheckError> errors) 
            : base($"Type check failed with {errors.Count} errors")
        {
            Errors = errors;
        }
    }
    
    /// <summary>
    /// Function type node
    /// </summary>
    public class FunctionTypeNode : TypeNode
    {
        public List<TypeNode> ParameterTypes { get; }
        public TypeNode ReturnType { get; }
        
        public FunctionTypeNode(List<TypeNode> paramTypes, TypeNode returnType) 
            : base($"({string.Join(", ", paramTypes.Select(p => p.Name))}) => {returnType.Name}")
        {
            ParameterTypes = paramTypes;
            ReturnType = returnType;
        }
    }
    
    /// <summary>
    /// Array type node for array types
    /// </summary>
    public class ArrayTypeNode : TypeNode
    {
        public TypeNode ElementType { get; }
        
        public ArrayTypeNode(TypeNode elementType)
            : base($"{elementType.Name}[]")
        {
            ElementType = elementType;
        }
    }
} 
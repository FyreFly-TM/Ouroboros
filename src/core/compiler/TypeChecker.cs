using System;
using System.Collections.Generic;
using System.Linq;
using Ouro.Core.AST;
using Ouro.Core.VM;
using Ouro.Tokens;

namespace Ouro.Core.Compiler
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
        private Stack<ContractContext> contractStack;
        private Dictionary<string, GenericTypeConstraints> genericConstraints;
        private TypeInferenceEngine inferenceEngine;
        
        public TypeChecker()
        {
            symbols = new SymbolTable();
            typeRegistry = new TypeRegistry();
            errors = new List<TypeCheckError>();
            scopes = new Stack<Dictionary<string, TypeNode>>();
            scopes.Push(new Dictionary<string, TypeNode>()); // Global scope
            contractStack = new Stack<ContractContext>();
            genericConstraints = new Dictionary<string, GenericTypeConstraints>();
            inferenceEngine = new TypeInferenceEngine(this);
        }
        
        public Ouro.Core.AST.Program Check(Ouro.Core.AST.Program ast)
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
        
        public TypeNode LookupVariable(string name)
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
        
        private void AddErrorWithSuggestion(string message, string suggestion, int line, int column)
        {
            var fullMessage = $"{message}. {suggestion}";
            errors.Add(new TypeCheckError(fullMessage, line, column));
        }
        
        private string FormatType(TypeNode type)
        {
            if (type == null) return "<unknown>";
            
            if (type.IsArray)
                return $"{type.Name}[{new string(',', type.ArrayRank - 1)}]";
            if (type.IsNullable)
                return $"{type.Name}?";
            if (type.IsPointer)
                return $"{type.Name}*";
            if (type is FunctionTypeNode funcType)
                return $"({string.Join(", ", funcType.ParameterTypes.Select(FormatType))}) => {FormatType(funcType.ReturnType)}";
            if (type is ArrayTypeNode arrayType)
                return $"{FormatType(arrayType.ElementType)}[]";
            if (type is UnitTypeNode unitType)
                return $"{FormatType(unitType.BaseType)}[{unitType.Unit}]";
            
            return type.Name;
        }
        
        // Visitor implementations
        public TypeNode VisitProgram(Ouro.Core.AST.Program program) 
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
                    AddErrorWithSuggestion(
                        $"Cannot apply operator '{expr.Operator.Lexeme}' to operands of type '{FormatType(leftType)}' and '{FormatType(rightType)}'",
                        "Ensure both operands are numeric or use appropriate type conversions",
                        expr.Line, expr.Column);
                    return typeRegistry.Unknown;
                    
                case TokenType.Equal:
                case TokenType.NotEqual:
                case TokenType.Less:
                case TokenType.Greater:
                case TokenType.LessEqual:
                case TokenType.GreaterEqual:
                    if (!AreTypesComparable(leftType, rightType))
                    {
                        AddErrorWithSuggestion(
                            $"Cannot compare values of type '{FormatType(leftType)}' and '{FormatType(rightType)}'",
                            "Values must be of comparable types (numeric, string, or same type)",
                            expr.Line, expr.Column);
                    }
                    return typeRegistry.Bool;
                    
                case TokenType.LogicalAnd:
                case TokenType.LogicalOr:
                    if (leftType?.Name != "bool")
                        AddErrorWithSuggestion(
                            $"Left operand of '{expr.Operator.Lexeme}' must be boolean, but was '{FormatType(leftType)}'",
                            "Consider using a comparison operator or converting to boolean",
                            expr.Left.Line, expr.Left.Column);
                    if (rightType?.Name != "bool")
                        AddErrorWithSuggestion(
                            $"Right operand of '{expr.Operator.Lexeme}' must be boolean, but was '{FormatType(rightType)}'",
                            "Consider using a comparison operator or converting to boolean",
                            expr.Right.Line, expr.Right.Column);
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
                    AddErrorWithSuggestion(
                        $"Unary '{expr.Operator.Lexeme}' cannot be applied to operand of type '{FormatType(operandType)}'",
                        "This operator requires a numeric operand",
                        expr.Line, expr.Column);
                    return typeRegistry.Unknown;
                    
                case TokenType.LogicalNot:
                    if (operandType?.Name == "bool")
                        return typeRegistry.Bool;
                    AddErrorWithSuggestion(
                        $"Logical not (!) cannot be applied to operand of type '{FormatType(operandType)}'",
                        "Use a boolean expression or convert to boolean first",
                        expr.Line, expr.Column);
                    return typeRegistry.Bool;
                    
                case TokenType.Increment:
                case TokenType.Decrement:
                    if (IsNumericType(operandType))
                        return operandType;
                    AddErrorWithSuggestion(
                        $"'{expr.Operator.Lexeme}' cannot be applied to operand of type '{FormatType(operandType)}'",
                        "This operator can only be used with numeric types",
                        expr.Line, expr.Column);
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
                // Try to provide a helpful suggestion
                var suggestion = "";
                var similarName = FindSimilarVariableName(expr.Name);
                if (similarName != null)
                {
                    suggestion = $"Did you mean '{similarName}'?";
                }
                else
                {
                    suggestion = "Make sure the variable is declared before use";
                }
                
                AddErrorWithSuggestion(
                    $"The name '{expr.Name}' does not exist in the current context",
                    suggestion,
                    expr.Line, expr.Column);
                return typeRegistry.Unknown;
            }
            return type;
        }
        
        private string FindSimilarVariableName(string name)
        {
            // Simple edit distance check for typos
            foreach (var scope in scopes)
            {
                foreach (var varName in scope.Keys)
                {
                    if (LevenshteinDistance(name, varName) <= 2)
                    {
                        return varName;
                    }
                }
            }
            return null;
        }
        
        private int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (int i = 0; i <= n; d[i, 0] = i++) { }
            for (int j = 0; j <= m; d[0, j] = j++) { }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }
        
        public TypeNode VisitAssignmentExpression(AssignmentExpression expr) 
        { 
            var targetType = expr.Target.Accept(this);
            var valueType = expr.Value.Accept(this);
            
            if (targetType != null && valueType != null && !AreTypesCompatible(targetType, valueType))
            {
                AddErrorWithSuggestion(
                    $"Cannot implicitly convert type '{FormatType(valueType)}' to '{FormatType(targetType)}'",
                    $"Consider using an explicit cast or changing the target type",
                    expr.Line, expr.Column);
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
                    AddErrorWithSuggestion(
                        $"Cannot initialize variable of type '{FormatType(declaredType)}' with value of type '{FormatType(initType)}'",
                        $"Either change the variable type to '{FormatType(initType)}' or convert the initializer",
                        stmt.Line, stmt.Column);
                }
            }
            else if (stmt.IsConst)
            {
                AddError("A const field requires a value to be provided", stmt.Line, stmt.Column);
            }
            
            // Check for variable redefinition
            if (scopes.Peek().ContainsKey(stmt.Name))
            {
                AddErrorWithSuggestion(
                    $"A local variable named '{stmt.Name}' is already defined in this scope",
                    "Use a different variable name or remove the duplicate declaration",
                    stmt.Line, stmt.Column);
            }
            else
            {
                DefineVariable(stmt.Name, declaredType);
            }
            
            return null;
        }
        
        public TypeNode VisitIfStatement(IfStatement stmt) 
        { 
            var condType = stmt.Condition.Accept(this);
            if (condType?.Name != "bool")
            {
                AddErrorWithSuggestion(
                    $"Condition must evaluate to boolean, but was '{FormatType(condType)}'",
                    "Use a comparison operator or boolean expression",
                    stmt.Line, stmt.Column);
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
            var paramNames = new HashSet<string>();
            foreach (var param in decl.Parameters)
            {
                if (!paramNames.Add(param.Name))
                {
                    AddError($"Duplicate parameter name '{param.Name}'", decl.Line, decl.Column);
                }
                DefineVariable(param.Name, param.Type);
            }
            
            // Set current return type for return statement checking
            var previousReturnType = currentReturnType;
            currentReturnType = decl.ReturnType;
            
            decl.Body.Accept(this);
            
            // Check if non-void function has return path
            if (currentReturnType?.Name != "void" && !HasReturnPath(decl.Body))
            {
                AddErrorWithSuggestion(
                    $"Not all code paths return a value in function '{decl.Name}'",
                    $"Add a return statement that returns a value of type '{FormatType(currentReturnType)}'",
                    decl.Line, decl.Column);
            }
            
            currentReturnType = previousReturnType;
            ExitScope();
            
            // Store function type
            var funcType = new FunctionTypeNode(
                decl.Parameters.Select(p => p.Type).ToList(),
                decl.ReturnType
            );
            DefineVariable(decl.Name, funcType);
            
            // Verify contracts
            VerifyContracts(decl);
            
            return null;
        }
        
        private bool HasReturnPath(BlockStatement block)
        {
            // Simple check - a more sophisticated implementation would do control flow analysis
            foreach (var stmt in block.Statements)
            {
                if (stmt is ReturnStatement)
                    return true;
                if (stmt is IfStatement ifStmt && 
                    ifStmt.ElseBranch != null &&
                    HasReturnPath(ifStmt.ThenBranch as BlockStatement) &&
                    HasReturnPath(ifStmt.ElseBranch as BlockStatement))
                    return true;
            }
            return false;
        }
        
        public TypeNode VisitReturnStatement(ReturnStatement stmt) 
        { 
            if (stmt.Value != null)
            {
                var returnType = stmt.Value.Accept(this);
                if (currentReturnType != null && !AreTypesCompatible(currentReturnType, returnType))
                {
                    if (currentReturnType.Name == "void")
                    {
                        AddErrorWithSuggestion(
                            $"Cannot return a value from a void function",
                            "Remove the return value or change the function return type",
                            stmt.Line, stmt.Column);
                    }
                    else
                    {
                        AddErrorWithSuggestion(
                            $"Cannot return '{FormatType(returnType)}' from function declared to return '{FormatType(currentReturnType)}'",
                            $"Return a value of type '{FormatType(currentReturnType)}' or change the function signature",
                            stmt.Line, stmt.Column);
                    }
                }
            }
            else if (currentReturnType?.Name != "void")
            {
                AddErrorWithSuggestion(
                    $"Must return a value of type '{FormatType(currentReturnType)}'",
                    "Add a return value or change the function to return void",
                    stmt.Line, stmt.Column);
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
                    var funcName = expr.Callee is IdentifierExpression id ? id.Name : "function";
                    AddErrorWithSuggestion(
                        $"'{funcName}' expects {funcType.ParameterTypes.Count} arguments but was given {expr.Arguments.Count}",
                        $"Provide exactly {funcType.ParameterTypes.Count} arguments",
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
                            AddErrorWithSuggestion(
                                $"Argument {i + 1}: cannot convert from '{FormatType(argType)}' to '{FormatType(funcType.ParameterTypes[i])}'",
                                $"Expected type '{FormatType(funcType.ParameterTypes[i])}'",
                                expr.Arguments[i].Line, expr.Arguments[i].Column);
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
                                var argType = expr.Arguments[0].Accept(this);
                                if (argType is ArrayTypeNode || argType?.Name == "string")
                                    return typeRegistry.Int;
                                else
                                    AddErrorWithSuggestion(
                                        $"'{id.Name}' can only be used with arrays or strings, not '{FormatType(argType)}'",
                                        "Pass an array or string to this function",
                                        expr.Line, expr.Column);
                            }
                            else
                            {
                                AddError($"'{id.Name}' expects exactly 1 argument", expr.Line, expr.Column);
                            }
                            return typeRegistry.Int;
                    }
                }
                
                // Unknown function
                if (expr.Callee is IdentifierExpression unknownId)
                {
                    AddErrorWithSuggestion(
                        $"'{unknownId.Name}' is not a function or is not callable",
                        "Check that the function is defined and in scope",
                        expr.Line, expr.Column);
                }
                else
                {
                    AddError($"Expression is not callable", expr.Line, expr.Column);
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
                else
                {
                    AddErrorWithSuggestion(
                        $"'{expr.MemberName}' is not a member of type '{FormatType(objectType)}'",
                        "This property is only available on arrays and strings",
                        expr.Line, expr.Column);
                    return typeRegistry.Unknown;
                }
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
            {
                // For empty arrays, try to infer from context
                // For now, default to object[]
                return new ArrayTypeNode(new TypeNode("object"));
            }
                
            // Infer element type from first element
            var elementType = expr.Elements[0].Accept(this);
            
            // Check all elements have compatible types
            for (int i = 1; i < expr.Elements.Count; i++)
            {
                var elemType = expr.Elements[i].Accept(this);
                if (!AreTypesCompatible(elementType, elemType))
                {
                    AddErrorWithSuggestion(
                        $"Array element at index {i} has type '{FormatType(elemType)}' but expected '{FormatType(elementType)}'",
                        "Ensure all array elements have the same type or use explicit casts",
                        expr.Elements[i].Line, expr.Elements[i].Column);
                    // Try to find common base type
                    elementType = FindCommonType(elementType, elemType);
                }
            }
            
            return new ArrayTypeNode(elementType);
        }
        
        private TypeNode FindCommonType(TypeNode type1, TypeNode type2)
        {
            // If types are the same, return that type
            if (type1.Name == type2.Name) return type1;
            
            // If either is unknown, return the other
            if (type1.Name == "?") return type2;
            if (type2.Name == "?") return type1;
            
            // If both are numeric, return the wider type
            if (IsNumericType(type1) && IsNumericType(type2))
            {
                return GetNumericResultType(type1, type2);
            }
            
            // Otherwise return object as the common base type
            return new TypeNode("object");
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
        public TypeNode VisitWhileStatement(WhileStatement stmt) 
        { 
            var cond = stmt.Condition.Accept(this); 
            if (cond?.Name != "bool") 
            {
                AddErrorWithSuggestion(
                    $"While loop condition must be boolean, but was '{FormatType(cond)}'",
                    "Use a comparison operator or boolean expression",
                    stmt.Line, stmt.Column);
            }
            stmt.Body.Accept(this); 
            return null;
        }
        
        public TypeNode VisitDoWhileStatement(DoWhileStatement stmt) 
        { 
            stmt.Body.Accept(this); 
            var cond = stmt.Condition.Accept(this); 
            if (cond?.Name != "bool") 
            {
                AddErrorWithSuggestion(
                    $"Do-while loop condition must be boolean, but was '{FormatType(cond)}'",
                    "Use a comparison operator or boolean expression",
                    stmt.Line, stmt.Column);
            }
            return null;
        }
        
        public TypeNode VisitForStatement(ForStatement stmt) 
        { 
            EnterScope(); 
            stmt.Initializer?.Accept(this); 
            
            if (stmt.Condition != null)
            {
                var cond = stmt.Condition.Accept(this);
                if (cond?.Name != "bool")
                {
                    AddErrorWithSuggestion(
                        $"For loop condition must be boolean, but was '{FormatType(cond)}'",
                        "Use a comparison operator or boolean expression",
                        stmt.Condition.Line, stmt.Condition.Column);
                }
            }
            
            stmt.Update?.Accept(this); 
            stmt.Body.Accept(this); 
            ExitScope(); 
            return null;
        }
        
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
        // Check if struct type exists and validate fields
        // For now, return unknown type
        foreach (var field in expr.Fields.Values)
        {
            field.Accept(this);
        }
        
        // Try to infer struct type from name
        return new TypeNode(expr.StructName.Lexeme);
    }
    
    // Contract verification methods
    private void VerifyContracts(FunctionDeclaration func)
    {
        var context = new ContractContext();
        contractStack.Push(context);
        
        // Extract contracts from function attributes or special statements
        ExtractContracts(func, context);
        
        // Verify preconditions are checkable
        foreach (var requires in context.Requires)
        {
            var condType = requires.Accept(this);
            if (condType?.Name != "bool")
            {
                AddError($"Requires clause must be boolean expression", requires.Line, requires.Column);
            }
        }
        
        // Store old values for postcondition checking
        if (context.Ensures.Count > 0)
        {
            // In a real implementation, would analyze which values need to be saved
        }
        
        contractStack.Pop();
    }
    
    private void ExtractContracts(FunctionDeclaration func, ContractContext context)
    {
        // Look for contract statements at the beginning of function body
        if (func.Body != null)
        {
            foreach (var stmt in func.Body.Statements)
            {
                if (stmt is ExpressionStatement exprStmt)
                {
                    if (exprStmt.Expression is CallExpression call)
                    {
                        if (call.Callee is IdentifierExpression id)
                        {
                            switch (id.Name)
                            {
                                case "requires":
                                    if (call.Arguments.Count > 0)
                                        context.Requires.Add(call.Arguments[0]);
                                    break;
                                    
                                case "ensures":
                                    if (call.Arguments.Count > 0)
                                        context.Ensures.Add(call.Arguments[0]);
                                    break;
                                    
                                case "invariant":
                                    if (call.Arguments.Count > 0)
                                        context.Invariants.Add(call.Arguments[0]);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        // Stop at first non-contract statement
                        break;
                    }
                }
            }
        }
    }
    
    // Unit type operations
    private TypeNode HandleUnitOperation(BinaryExpression expr, TypeNode leftType, TypeNode rightType)
    {
        // Extract base types and units
        var leftBase = leftType is UnitTypeNode leftUnit ? leftUnit.BaseType : leftType;
        var rightBase = rightType is UnitTypeNode rightUnit ? rightUnit.BaseType : rightType;
        
        var leftUnitStr = leftType is UnitTypeNode lu ? lu.Unit : "";
        var rightUnitStr = rightType is UnitTypeNode ru ? ru.Unit : "";
        
        // Check base types are compatible
        if (!IsNumericType(leftBase) || !IsNumericType(rightBase))
        {
            AddError("Unit operations require numeric types", expr.Line, expr.Column);
            return typeRegistry.Unknown;
        }
        
        switch (expr.Operator.Type)
        {
            case TokenType.Multiply:
                return MultiplyUnits(leftUnitStr, rightUnitStr, GetNumericResultType(leftBase, rightBase));
                
            case TokenType.Divide:
                return DivideUnits(leftUnitStr, rightUnitStr, GetNumericResultType(leftBase, rightBase));
                
            case TokenType.Plus:
            case TokenType.Minus:
                if (leftUnitStr != rightUnitStr)
                {
                    AddError($"Cannot {expr.Operator.Lexeme} incompatible units: [{leftUnitStr}] and [{rightUnitStr}]", 
                            expr.Line, expr.Column);
                    return typeRegistry.Unknown;
                }
                return new UnitTypeNode(GetNumericResultType(leftBase, rightBase), leftUnitStr);
                
            default:
                return GetNumericResultType(leftBase, rightBase);
        }
    }
    
    private TypeNode MultiplyUnits(string left, string right, TypeNode baseType)
    {
        if (string.IsNullOrEmpty(left) && string.IsNullOrEmpty(right))
            return baseType;
            
        if (string.IsNullOrEmpty(left))
            return new UnitTypeNode(baseType, right);
            
        if (string.IsNullOrEmpty(right))
            return new UnitTypeNode(baseType, left);
            
        // Simplify common patterns
        if (left == right)
            return new UnitTypeNode(baseType, $"{left}²");
            
        return new UnitTypeNode(baseType, $"{left}·{right}");
    }
    
    private TypeNode DivideUnits(string left, string right, TypeNode baseType)
    {
        if (string.IsNullOrEmpty(left) && string.IsNullOrEmpty(right))
            return baseType;
            
        if (string.IsNullOrEmpty(right))
            return new UnitTypeNode(baseType, left);
            
        if (string.IsNullOrEmpty(left))
            return new UnitTypeNode(baseType, $"1/{right}");
            
        // Cancel out same units
        if (left == right)
            return baseType;
            
        return new UnitTypeNode(baseType, $"{left}/{right}");
    }
    
    private bool AreTypesComparable(TypeNode left, TypeNode right)
    {
        if (left == null || right == null) return true;
        
        // Same types are always comparable
        if (left.Name == right.Name) return true;
        
        // Numeric types are comparable
        if (IsNumericType(left) && IsNumericType(right)) return true;
        
        // Null can be compared with nullable types
        if ((left.Name == "null" && right.IsNullable) ||
            (right.Name == "null" && left.IsNullable))
            return true;
            
        return false;
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
    
    /// <summary>
    /// Represents a type with units (e.g., int[meters], float[kg])
    /// </summary>
    public class UnitTypeNode : TypeNode
    {
        public TypeNode BaseType { get; }
        public string Unit { get; }
        
        public UnitTypeNode(TypeNode baseType, string unit)
            : base($"{baseType.Name}[{unit}]")
        {
            BaseType = baseType;
            Unit = unit;
        }
    }
    
    /// <summary>
    /// Contract context for tracking requires/ensures clauses
    /// </summary>
    public class ContractContext
    {
        public List<Expression> Requires { get; } = new List<Expression>();
        public List<Expression> Ensures { get; } = new List<Expression>();
        public List<Expression> Invariants { get; } = new List<Expression>();
        public Dictionary<string, object> OldValues { get; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Generic type constraints
    /// </summary>
    public class GenericTypeConstraints
    {
        public string TypeParameterName { get; set; }
        public List<TypeNode> Constraints { get; set; } = new List<TypeNode>();
        public bool IsCovariant { get; set; }
        public bool IsContravariant { get; set; }
    }
    
    /// <summary>
    /// Type inference engine for automatic type deduction
    /// </summary>
    public class TypeInferenceEngine
    {
        private readonly TypeChecker typeChecker;
        private readonly Dictionary<string, TypeNode> inferredTypes;
        
        public TypeInferenceEngine(TypeChecker typeChecker)
        {
            this.typeChecker = typeChecker;
            this.inferredTypes = new Dictionary<string, TypeNode>();
        }
        
        public TypeNode InferType(Expression expr)
        {
            // Basic inference rules
            switch (expr)
            {
                case LiteralExpression lit:
                    return InferLiteralType(lit);
                    
                case BinaryExpression bin:
                    return InferBinaryType(bin);
                    
                case CallExpression call:
                    return InferCallType(call);
                    
                default:
                    return expr.Accept(typeChecker);
            }
        }
        
        private TypeNode InferLiteralType(LiteralExpression lit)
        {
            if (lit.Value == null) return new TypeNode("null");
            
            return lit.Value switch
            {
                int _ => new TypeNode("int"),
                double _ => new TypeNode("double"),
                float _ => new TypeNode("float"),
                string _ => new TypeNode("string"),
                bool _ => new TypeNode("bool"),
                _ => new TypeNode("object")
            };
        }
        
        private TypeNode InferBinaryType(BinaryExpression bin)
        {
            var leftType = InferType(bin.Left);
            var rightType = InferType(bin.Right);
            
            // Handle unit types for mathematical operations
            if (leftType is UnitTypeNode leftUnit && rightType is UnitTypeNode rightUnit)
            {
                switch (bin.Operator.Type)
                {
                    case TokenType.Multiply:
                        // m * m = m²
                        if (leftUnit.Unit == rightUnit.Unit)
                            return new UnitTypeNode(leftUnit.BaseType, $"{leftUnit.Unit}²");
                        // m * s = m·s
                        return new UnitTypeNode(leftUnit.BaseType, $"{leftUnit.Unit}·{rightUnit.Unit}");
                        
                    case TokenType.Divide:
                        // m / s = m/s
                        return new UnitTypeNode(leftUnit.BaseType, $"{leftUnit.Unit}/{rightUnit.Unit}");
                        
                    case TokenType.Plus:
                    case TokenType.Minus:
                        // Can only add/subtract same units
                        if (leftUnit.Unit == rightUnit.Unit)
                            return leftUnit;
                        throw new TypeCheckException(new List<TypeCheckError> {
                            new TypeCheckError($"Cannot {bin.Operator.Lexeme} different units: {leftUnit.Unit} and {rightUnit.Unit}", 
                                bin.Line, bin.Column)
                        });
                }
            }
            
            return typeChecker.VisitBinaryExpression(bin);
        }
        
        private TypeNode InferCallType(CallExpression call)
        {
            // Infer generic type arguments if not provided
            if (call.GenericTypeArguments == null && call.Callee is IdentifierExpression id)
            {
                // Look up function signature and try to infer type arguments
                var funcType = typeChecker.LookupVariable(id.Name);
                if (funcType is GenericFunctionTypeNode genFunc)
                {
                    var inferredArgs = InferGenericArguments(genFunc, call.Arguments);
                    if (inferredArgs != null)
                    {
                        // Create specialized function type
                        return InstantiateGenericFunction(genFunc, inferredArgs);
                    }
                }
            }
            
            return call.Accept(typeChecker);
        }
        
        private List<TypeNode> InferGenericArguments(GenericFunctionTypeNode funcType, List<Expression> arguments)
        {
            // Simple unification-based type inference
            var typeVars = new Dictionary<string, TypeNode>();
            
            for (int i = 0; i < Math.Min(funcType.ParameterTypes.Count, arguments.Count); i++)
            {
                var paramType = funcType.ParameterTypes[i];
                var argType = InferType(arguments[i]);
                
                if (!UnifyTypes(paramType, argType, typeVars))
                    return null;
            }
            
            // Extract inferred types in order
            return funcType.TypeParameters.Select(tp => 
                typeVars.ContainsKey(tp) ? typeVars[tp] : new TypeNode("object")
            ).ToList();
        }
        
        private bool UnifyTypes(TypeNode pattern, TypeNode actual, Dictionary<string, TypeNode> typeVars)
        {
            // If pattern is a type variable
            if (pattern is TypeVariableNode tv)
            {
                if (typeVars.ContainsKey(tv.Name))
                    return UnifyTypes(typeVars[tv.Name], actual, typeVars);
                    
                typeVars[tv.Name] = actual;
                return true;
            }
            
            // If both are generic types
            if (pattern is GenericTypeNode genPattern && actual is GenericTypeNode genActual)
            {
                if (genPattern.Name != genActual.Name) return false;
                if (genPattern.TypeArguments.Count != genActual.TypeArguments.Count) return false;
                
                for (int i = 0; i < genPattern.TypeArguments.Count; i++)
                {
                    if (!UnifyTypes(genPattern.TypeArguments[i], genActual.TypeArguments[i], typeVars))
                        return false;
                }
                return true;
            }
            
            // Otherwise, types must match exactly
            return pattern.Name == actual.Name;
        }
        
        private TypeNode InstantiateGenericFunction(GenericFunctionTypeNode funcType, List<TypeNode> typeArgs)
        {
            var substitution = new Dictionary<string, TypeNode>();
            for (int i = 0; i < Math.Min(funcType.TypeParameters.Count, typeArgs.Count); i++)
            {
                substitution[funcType.TypeParameters[i]] = typeArgs[i];
            }
            
            var paramTypes = funcType.ParameterTypes.Select(pt => SubstituteType(pt, substitution)).ToList();
            var returnType = SubstituteType(funcType.ReturnType, substitution);
            
            return new FunctionTypeNode(paramTypes, returnType);
        }
        
        private TypeNode SubstituteType(TypeNode type, Dictionary<string, TypeNode> substitution)
        {
            if (type is TypeVariableNode tv && substitution.ContainsKey(tv.Name))
                return substitution[tv.Name];
                
            if (type is GenericTypeNode gen)
            {
                var args = gen.TypeArguments.Select(arg => SubstituteType(arg, substitution)).ToList();
                return new GenericTypeNode(gen.Name, args);
            }
            
            return type;
        }
    }
    
    /// <summary>
    /// Represents a generic function type
    /// </summary>
    public class GenericFunctionTypeNode : FunctionTypeNode
    {
        public List<string> TypeParameters { get; }
        
        public GenericFunctionTypeNode(List<string> typeParams, List<TypeNode> paramTypes, TypeNode returnType)
            : base(paramTypes, returnType)
        {
            TypeParameters = typeParams;
        }
    }
    
    /// <summary>
    /// Represents a type variable (T, U, etc.)
    /// </summary>
    public class TypeVariableNode : TypeNode
    {
        public TypeVariableNode(string name) : base(name)
        {
        }
    }
    
    /// <summary>
    /// Represents a generic type with type arguments
    /// </summary>
    public class GenericTypeNode : TypeNode
    {
        public new List<TypeNode> TypeArguments { get; }
        
        public GenericTypeNode(string name, List<TypeNode> typeArgs)
            : base($"{name}<{string.Join(", ", typeArgs.Select(t => t.Name))}>")
        {
            TypeArguments = typeArgs;
        }
    }
} 
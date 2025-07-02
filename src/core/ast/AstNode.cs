using System;
using System.Collections.Generic;
using Ouro.Tokens;

namespace Ouro.Core.AST
{
    /// <summary>
    /// Base class for all AST nodes in the Ouro language
    /// </summary>
    public abstract class AstNode
    {
        public Token? Token { get; }
        public SyntaxLevel SyntaxLevel { get; }
        public int Line => Token?.Line ?? 0;
        public int Column => Token?.Column ?? 0;
        public string FileName => Token?.FileName ?? "";

        protected AstNode(Token? token = null)
        {
            Token = token;
            SyntaxLevel = token?.SyntaxLevel ?? SyntaxLevel.Medium;
        }

        public abstract T Accept<T>(IAstVisitor<T> visitor);
    }

    /// <summary>
    /// Visitor pattern interface for AST traversal
    /// </summary>
    public interface IAstVisitor<T>
    {
        // Expressions
        T VisitBinaryExpression(BinaryExpression expr);
        T VisitUnaryExpression(UnaryExpression expr);
        T VisitLiteralExpression(LiteralExpression expr);
        T VisitIdentifierExpression(IdentifierExpression expr);
        T VisitGenericIdentifierExpression(GenericIdentifierExpression expr);
        T VisitAssignmentExpression(AssignmentExpression expr);
        T VisitCallExpression(CallExpression expr);
        T VisitMemberExpression(MemberExpression expr);
        T VisitArrayExpression(ArrayExpression expr);
        T VisitLambdaExpression(LambdaExpression expr);
        T VisitConditionalExpression(ConditionalExpression expr);
        T VisitNewExpression(NewExpression expr);
        T VisitThisExpression(ThisExpression expr);
        T VisitBaseExpression(BaseExpression expr);
        T VisitTypeofExpression(TypeofExpression expr);
        T VisitSizeofExpression(SizeofExpression expr);
        T VisitNameofExpression(NameofExpression expr);
        T VisitInterpolatedStringExpression(InterpolatedStringExpression expr);
        T VisitMathExpression(MathExpression expr);
        T VisitVectorExpression(VectorExpression expr);
        T VisitMatrixExpression(MatrixExpression expr);
        T VisitQuaternionExpression(QuaternionExpression expr);
        T VisitIsExpression(IsExpression expr);
        T VisitCastExpression(CastExpression expr);
        T VisitMatchExpression(MatchExpression expr);
        T VisitThrowExpression(ThrowExpression expr);
        T VisitMatchArm(MatchArm arm);
        T VisitStructLiteral(StructLiteral expr);

        // Statements
        T VisitBlockStatement(BlockStatement stmt);
        T VisitExpressionStatement(ExpressionStatement stmt);
        T VisitVariableDeclaration(VariableDeclaration stmt);
        T VisitIfStatement(IfStatement stmt);
        T VisitWhileStatement(WhileStatement stmt);
        T VisitForStatement(ForStatement stmt);
        T VisitForEachStatement(ForEachStatement stmt);
        T VisitRepeatStatement(RepeatStatement stmt);
        T VisitIterateStatement(IterateStatement stmt);
        T VisitParallelForStatement(ParallelForStatement stmt);
        T VisitDoWhileStatement(DoWhileStatement stmt);
        T VisitSwitchStatement(SwitchStatement stmt);
        T VisitReturnStatement(ReturnStatement stmt);
        T VisitBreakStatement(BreakStatement stmt);
        T VisitContinueStatement(ContinueStatement stmt);
        T VisitThrowStatement(ThrowStatement stmt);
        T VisitTryStatement(TryStatement stmt);
        T VisitUsingStatement(UsingStatement stmt);
        T VisitLockStatement(LockStatement stmt);
        T VisitUnsafeStatement(UnsafeStatement stmt);
        T VisitFixedStatement(FixedStatement stmt);
        T VisitYieldStatement(YieldStatement stmt);
        T VisitMatchStatement(MatchStatement stmt);
        T VisitAssemblyStatement(AssemblyStatement stmt);

        // Declarations
        T VisitClassDeclaration(ClassDeclaration decl);
        T VisitInterfaceDeclaration(InterfaceDeclaration decl);
        T VisitStructDeclaration(StructDeclaration decl);
        T VisitEnumDeclaration(EnumDeclaration decl);
        T VisitFunctionDeclaration(FunctionDeclaration decl);
        T VisitPropertyDeclaration(PropertyDeclaration decl);
        T VisitFieldDeclaration(FieldDeclaration decl);
        T VisitNamespaceDeclaration(NamespaceDeclaration decl);
        T VisitImportDeclaration(ImportDeclaration decl);
        T VisitTypeAliasDeclaration(TypeAliasDeclaration decl);

        // Data-Oriented
        T VisitComponentDeclaration(ComponentDeclaration decl);
        T VisitSystemDeclaration(SystemDeclaration decl);
        T VisitEntityDeclaration(EntityDeclaration decl);

        // Domain-Specific Languages
        T VisitDomainDeclaration(DomainDeclaration decl);

        // Metaprogramming
        T VisitMacroDeclaration(MacroDeclaration decl);
        T VisitTraitDeclaration(TraitDeclaration decl);
        T VisitImplementDeclaration(ImplementDeclaration decl);

        // Program
        T VisitProgram(Program program);
    }

    #region Expression Nodes

    public abstract class Expression : AstNode
    {
        protected Expression(Token? token = null) : base(token) { }
    }

    public class BinaryExpression : Expression
    {
        public Expression Left { get; }
        public Token Operator { get; }
        public Expression Right { get; }

        public BinaryExpression(Expression left, Token op, Expression right) : base(op)
        {
            Left = left;
            Operator = op;
            Right = right;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitBinaryExpression(this);
    }

    public class UnaryExpression : Expression
    {
        public Token Operator { get; }
        public Expression Operand { get; }
        public bool IsPrefix { get; }

        public UnaryExpression(Token op, Expression operand, bool isPrefix = true) : base(op)
        {
            Operator = op;
            Operand = operand;
            IsPrefix = isPrefix;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitUnaryExpression(this);
    }

    public class LiteralExpression : Expression
    {
        public object Value { get; }

        public LiteralExpression(Token token) : base(token)
        {
            // LiteralExpression constructor - token type and value
            Value = token.Value;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitLiteralExpression(this);
    }

    public class IdentifierExpression : Expression
    {
        public string Name { get; }

        public IdentifierExpression(Token token) : base(token)
        {
            Name = token.Lexeme;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitIdentifierExpression(this);
    }

    public class GenericIdentifierExpression : Expression
    {
        public string Name { get; }
        public List<TypeNode> GenericTypeArguments { get; }

        public GenericIdentifierExpression(Token token, string name, List<TypeNode> genericTypeArguments) : base(token)
        {
            Name = name;
            GenericTypeArguments = genericTypeArguments;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitGenericIdentifierExpression(this);
    }

    public class AssignmentExpression : Expression
    {
        public Expression Target { get; }
        public Token Operator { get; }
        public Expression Value { get; }

        public AssignmentExpression(Expression target, Token op, Expression value) : base(op)
        {
            Target = target;
            Operator = op;
            Value = value;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitAssignmentExpression(this);
    }

    public class CallExpression : Expression
    {
        public Expression Callee { get; }
        public List<Expression> Arguments { get; }
        public List<TypeNode>? GenericTypeArguments { get; }
        public bool IsAsync { get; }

        public CallExpression(Expression callee, List<Expression> arguments, bool isAsync = false, List<TypeNode>? genericTypeArguments = null) 
            : base(callee.Token)
        {
            Callee = callee;
            Arguments = arguments;
            GenericTypeArguments = genericTypeArguments;
            IsAsync = isAsync;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitCallExpression(this);
    }

    public class MemberExpression : Expression
    {
        public Expression Object { get; }
        public Token MemberOperator { get; } // . or -> or ?.
        public string MemberName { get; }

        public MemberExpression(Expression obj, Token memberOp, Token memberName) : base(memberOp)
        {
            Object = obj;
            MemberOperator = memberOp;
            MemberName = memberName.Lexeme;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitMemberExpression(this);
    }

    public class ArrayExpression : Expression
    {
        public List<Expression> Elements { get; }

        public ArrayExpression(Token token, List<Expression> elements) : base(token)
        {
            Elements = elements;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitArrayExpression(this);
    }

    public class LambdaExpression : Expression
    {
        public List<Parameter> Parameters { get; }
        public AstNode Body { get; } // Can be Expression or BlockStatement
        public bool IsAsync { get; }

        public LambdaExpression(List<Parameter> parameters, AstNode body, bool isAsync = false, Token? token = null) 
            : base(token)
        {
            Parameters = parameters;
            Body = body;
            IsAsync = isAsync;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitLambdaExpression(this);
    }

    public class ConditionalExpression : Expression
    {
        public Expression Condition { get; }
        public Expression TrueExpression { get; }
        public Expression FalseExpression { get; }

        public ConditionalExpression(Expression condition, Expression trueExpr, Expression falseExpr) 
            : base(condition.Token)
        {
            Condition = condition;
            TrueExpression = trueExpr;
            FalseExpression = falseExpr;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitConditionalExpression(this);
    }

    public class NewExpression : Expression
    {
        public TypeNode Type { get; }
        public List<Expression> Arguments { get; }
        public List<Expression>? Initializer { get; }

        public NewExpression(Token newToken, TypeNode type, List<Expression> arguments, List<Expression>? initializer = null) 
            : base(newToken)
        {
            Type = type;
            Arguments = arguments;
            Initializer = initializer;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitNewExpression(this);
    }

    public class StructLiteral : Expression
    {
        public Token StructName { get; set; }
        public Dictionary<string, Expression> Fields { get; set; }
        
        public StructLiteral(Token structName, Dictionary<string, Expression> fields)
        {
            StructName = structName;
            Fields = fields;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitStructLiteral(this);
        }
    }

    public class ThisExpression : Expression
    {
        public ThisExpression(Token token) : base(token) { }
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitThisExpression(this);
    }

    public class IsExpression : Expression
    {
        public Expression Left { get; }
        public TypeNode Type { get; }
        public Token? Variable { get; }  // Optional variable declaration

        public IsExpression(Expression left, Token isToken, TypeNode type, Token? variable = null) : base(isToken)
        {
            Left = left;
            Type = type;
            Variable = variable;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitIsExpression(this);
    }

    public class CastExpression : Expression
    {
        public TypeNode TargetType { get; }
        public Expression Expression { get; }

        public CastExpression(Token castToken, TypeNode targetType, Expression expression) : base(castToken)
        {
            TargetType = targetType;
            Expression = expression;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitCastExpression(this);
    }

    public class BaseExpression : Expression
    {
        public BaseExpression(Token token) : base(token) { }
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitBaseExpression(this);
    }

    public class TypeofExpression : Expression
    {
        public TypeNode Type { get; }

        public TypeofExpression(Token token, TypeNode type) : base(token)
        {
            Type = type;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitTypeofExpression(this);
    }

    public class SizeofExpression : Expression
    {
        public TypeNode Type { get; }

        public SizeofExpression(Token token, TypeNode type) : base(token)
        {
            Type = type;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitSizeofExpression(this);
    }

    public class NameofExpression : Expression
    {
        public Expression Expression { get; }

        public NameofExpression(Token token, Expression expr) : base(token)
        {
            Expression = expr;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitNameofExpression(this);
    }

    public class InterpolatedStringExpression : Expression
    {
        public List<Expression> Parts { get; } // Mix of string literals and expressions

        public InterpolatedStringExpression(Token token, List<Expression> parts) : base(token)
        {
            Parts = parts;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitInterpolatedStringExpression(this);
    }

    // Math-specific expressions
    public class MathExpression : Expression
    {
        public MathOperationType Operation { get; }
        public List<Expression> Operands { get; }

        public MathExpression(Token token, MathOperationType operation, List<Expression> operands) : base(token)
        {
            Operation = operation;
            Operands = operands;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitMathExpression(this);
    }

    public class VectorExpression : Expression
    {
        public List<Expression> Components { get; }
        public int Dimensions { get; }

        public VectorExpression(Token token, List<Expression> components) : base(token)
        {
            Components = components;
            Dimensions = components.Count;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitVectorExpression(this);
    }

    public class MatrixExpression : Expression
    {
        public List<List<Expression>> Elements { get; }
        public int Rows { get; }
        public int Columns { get; }

        public MatrixExpression(Token token, List<List<Expression>> elements) : base(token)
        {
            Elements = elements;
            Rows = elements.Count;
            Columns = elements.Count > 0 ? elements[0].Count : 0;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitMatrixExpression(this);
    }

    public class QuaternionExpression : Expression
    {
        public Expression W { get; }
        public Expression X { get; }
        public Expression Y { get; }
        public Expression Z { get; }

        public QuaternionExpression(Token token, Expression w, Expression x, Expression y, Expression z) : base(token)
        {
            W = w;
            X = x;
            Y = y;
            Z = z;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitQuaternionExpression(this);
    }

    #endregion

    #region Statement Nodes

    public abstract class Statement : AstNode
    {
        protected Statement(Token? token = null) : base(token) { }
    }

    public class BlockStatement : Statement
    {
        public List<Statement> Statements { get; }

        public BlockStatement(List<Statement> statements, Token? token = null) : base(token)
        {
            Statements = statements;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitBlockStatement(this);
    }

    public class ExpressionStatement : Statement
    {
        public Expression Expression { get; }

        public ExpressionStatement(Expression expression) : base(expression.Token)
        {
            Expression = expression;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitExpressionStatement(this);
    }

    public class VariableDeclaration : Statement
    {
        public TypeNode Type { get; }
        public string Name { get; }
        public Expression? Initializer { get; }
        public bool IsConst { get; }
        public bool IsReadonly { get; }

        public VariableDeclaration(TypeNode type, Token name, Expression? initializer = null, 
                                  bool isConst = false, bool isReadonly = false) : base(name)
        {
            Type = type;
            Name = name.Lexeme;
            Initializer = initializer;
            IsConst = isConst;
            IsReadonly = isReadonly;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitVariableDeclaration(this);
    }

    // Control flow statements
    public class IfStatement : Statement
    {
        public Expression Condition { get; }
        public Statement ThenBranch { get; }
        public Statement? ElseBranch { get; }

        public IfStatement(Token ifToken, Expression condition, Statement thenBranch, Statement? elseBranch = null) 
            : base(ifToken)
        {
            Condition = condition;
            ThenBranch = thenBranch;
            ElseBranch = elseBranch;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitIfStatement(this);
    }

    public class WhileStatement : Statement
    {
        public Expression Condition { get; }
        public Statement Body { get; }

        public WhileStatement(Token whileToken, Expression condition, Statement body) : base(whileToken)
        {
            Condition = condition;
            Body = body;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitWhileStatement(this);
    }

    public class ForStatement : Statement
    {
        public Statement? Initializer { get; }
        public Expression? Condition { get; }
        public Expression? Update { get; }
        public Statement Body { get; }

        public ForStatement(Token forToken, Statement? initializer, Expression? condition, 
                           Expression? update, Statement body) : base(forToken)
        {
            Initializer = initializer;
            Condition = condition;
            Update = update;
            Body = body;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitForStatement(this);
    }

    public class ForEachStatement : Statement
    {
        public TypeNode ElementType { get; }
        public string ElementName { get; }
        public Expression Collection { get; }
        public Statement Body { get; }

        public ForEachStatement(Token forEachToken, TypeNode elementType, Token elementName, 
                               Expression collection, Statement body) : base(forEachToken)
        {
            ElementType = elementType;
            ElementName = elementName.Lexeme;
            Collection = collection;
            Body = body;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitForEachStatement(this);
    }

    // Custom loop constructs
    public class RepeatStatement : Statement
    {
        public Expression Count { get; }
        public Statement Body { get; }

        public RepeatStatement(Token repeatToken, Expression count, Statement body) : base(repeatToken)
        {
            Count = count;
            Body = body;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitRepeatStatement(this);
    }

    public class IterateStatement : Statement
    {
        public Expression Start { get; }
        public Expression End { get; }
        public Expression Step { get; }
        public string IteratorName { get; }
        public Statement Body { get; }

        public IterateStatement(Token iterateToken, string iteratorName, Expression start, 
                               Expression end, Expression step, Statement body) : base(iterateToken)
        {
            IteratorName = iteratorName;
            Start = start;
            End = end;
            Step = step;
            Body = body;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitIterateStatement(this);
    }

    public class ParallelForStatement : Statement
    {
        public ForStatement BaseFor { get; }
        public int? MaxDegreeOfParallelism { get; }

        public ParallelForStatement(Token parallelToken, ForStatement baseFor, int? maxParallelism = null) 
            : base(parallelToken)
        {
            BaseFor = baseFor;
            MaxDegreeOfParallelism = maxParallelism;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitParallelForStatement(this);
    }

    public class DoWhileStatement : Statement
    {
        public Statement Body { get; }
        public Expression Condition { get; }

        public DoWhileStatement(Token doToken, Statement body, Expression condition) : base(doToken)
        {
            Body = body;
            Condition = condition;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitDoWhileStatement(this);
    }

    public class SwitchStatement : Statement
    {
        public Expression Expression { get; }
        public List<CaseClause> Cases { get; }
        public Statement? DefaultCase { get; }

        public SwitchStatement(Token switchToken, Expression expression, List<CaseClause> cases, 
                              Statement? defaultCase = null) : base(switchToken)
        {
            Expression = expression;
            Cases = cases;
            DefaultCase = defaultCase;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitSwitchStatement(this);
    }

    public class CaseClause
    {
        public Expression Value { get; }
        public List<Statement> Statements { get; }

        public CaseClause(Expression value, List<Statement> statements)
        {
            Value = value;
            Statements = statements;
        }
    }

    public class ReturnStatement : Statement
    {
        public Expression? Value { get; }

        public ReturnStatement(Token returnToken, Expression? value = null) : base(returnToken)
        {
            Value = value;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitReturnStatement(this);
    }

    public class BreakStatement : Statement
    {
        public BreakStatement(Token breakToken) : base(breakToken) { }
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitBreakStatement(this);
    }

    public class ContinueStatement : Statement
    {
        public ContinueStatement(Token continueToken) : base(continueToken) { }
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitContinueStatement(this);
    }

    public class ThrowStatement : Statement
    {
        public Expression Exception { get; }

        public ThrowStatement(Token throwToken, Expression exception) : base(throwToken)
        {
            Exception = exception;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitThrowStatement(this);
    }

    public class TryStatement : Statement
    {
        public Statement TryBlock { get; }
        public List<CatchClause> CatchClauses { get; }
        public Statement? FinallyBlock { get; }

        public TryStatement(Token tryToken, Statement tryBlock, List<CatchClause> catchClauses, 
                           Statement? finallyBlock = null) : base(tryToken)
        {
            TryBlock = tryBlock;
            CatchClauses = catchClauses;
            FinallyBlock = finallyBlock;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitTryStatement(this);
    }

    public class CatchClause
    {
        public TypeNode ExceptionType { get; }
        public string ExceptionName { get; }
        public Statement Body { get; }
        public Expression? WhenCondition { get; } // Exception filter condition

        public CatchClause(TypeNode exceptionType, string exceptionName, Statement body, Expression? whenCondition = null)
        {
            ExceptionType = exceptionType;
            ExceptionName = exceptionName;
            Body = body;
            WhenCondition = whenCondition;
        }
    }

    public class UsingStatement : Statement
    {
        public VariableDeclaration Resource { get; }
        public Statement Body { get; }

        public UsingStatement(Token usingToken, VariableDeclaration resource, Statement body) : base(usingToken)
        {
            Resource = resource;
            Body = body;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitUsingStatement(this);
    }

    public class LockStatement : Statement
    {
        public Expression LockObject { get; }
        public Statement Body { get; }

        public LockStatement(Token lockToken, Expression lockObject, Statement body) : base(lockToken)
        {
            LockObject = lockObject;
            Body = body;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitLockStatement(this);
    }

    public class UnsafeStatement : Statement
    {
        public Statement Body { get; }

        public UnsafeStatement(Token unsafeToken, Statement body) : base(unsafeToken)
        {
            Body = body;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitUnsafeStatement(this);
    }
    
    public class FixedStatement : Statement
    {
        public TypeNode Type { get; }
        public Token Name { get; }
        public Expression Target { get; }
        public Statement Body { get; }

        public FixedStatement(Token fixedToken, TypeNode type, Token name, Expression target, Statement body) : base(fixedToken)
        {
            Type = type;
            Name = name;
            Target = target;
            Body = body;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitFixedStatement(this);
    }

    public class YieldStatement : Statement
    {
        public Expression? Value { get; }
        public bool IsBreak { get; }

        public YieldStatement(Token yieldToken, Expression? value = null, bool isBreak = false) : base(yieldToken)
        {
            Value = value;
            IsBreak = isBreak;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitYieldStatement(this);
    }

    public class MatchStatement : Statement
    {
        public Expression Expression { get; }
        public List<MatchCase> Cases { get; }

        public MatchStatement(Token matchToken, Expression expression, List<MatchCase> cases) : base(matchToken)
        {
            Expression = expression;
            Cases = cases;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitMatchStatement(this);
    }

    public class MatchCase
    {
        public Pattern Pattern { get; }
        public Expression? Guard { get; } // Optional when clause
        public Statement Body { get; }

        public MatchCase(Pattern pattern, Expression? guard, Statement body)
        {
            Pattern = pattern;
            Guard = guard;
            Body = body;
        }
    }

    public abstract class Pattern
    {
        // Base class for pattern matching patterns
    }

    public class AssemblyStatement : Statement
    {
        public string AssemblyCode { get; }

        public AssemblyStatement(Token asmToken, string assemblyCode) : base(asmToken)
        {
            AssemblyCode = assemblyCode;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitAssemblyStatement(this);
    }

    #endregion

    #region Declaration Nodes

    public abstract class Declaration : Statement
    {
        public List<Modifier> Modifiers { get; }
        public string Name { get; }

        protected Declaration(Token nameToken, List<Modifier>? modifiers = null) : base(nameToken)
        {
            Name = nameToken?.Lexeme ?? "";
            Modifiers = modifiers ?? new List<Modifier>();
        }
    }

    public class ClassDeclaration : Declaration
    {
        public TypeNode? BaseClass { get; }
        public List<TypeNode> Interfaces { get; }
        public List<Declaration> Members { get; }
        public List<TypeParameter> TypeParameters { get; }

        public ClassDeclaration(Token classToken, Token nameToken, TypeNode? baseClass, 
                               List<TypeNode> interfaces, List<Declaration> members,
                               List<TypeParameter>? typeParameters = null, List<Modifier>? modifiers = null) 
            : base(nameToken, modifiers)
        {
            BaseClass = baseClass;
            Interfaces = interfaces;
            Members = members;
            TypeParameters = typeParameters ?? new List<TypeParameter>();
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitClassDeclaration(this);
    }

    public class InterfaceDeclaration : Declaration
    {
        public List<TypeNode> BaseInterfaces { get; }
        public List<Declaration> Members { get; }
        public List<TypeParameter> TypeParameters { get; }

        public InterfaceDeclaration(Token interfaceToken, Token nameToken, List<TypeNode> baseInterfaces,
                                   List<Declaration> members, List<TypeParameter>? typeParameters = null,
                                   List<Modifier>? modifiers = null) : base(nameToken, modifiers)
        {
            BaseInterfaces = baseInterfaces;
            Members = members;
            TypeParameters = typeParameters ?? new List<TypeParameter>();
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitInterfaceDeclaration(this);
    }

    public class StructDeclaration : Declaration
    {
        public List<TypeNode> Interfaces { get; }
        public List<Declaration> Members { get; }
        public List<TypeParameter> TypeParameters { get; }

        public StructDeclaration(Token structToken, Token nameToken, List<TypeNode> interfaces,
                                List<Declaration> members, List<TypeParameter>? typeParameters = null,
                                List<Modifier>? modifiers = null) : base(nameToken, modifiers)
        {
            Interfaces = interfaces;
            Members = members;
            TypeParameters = typeParameters ?? new List<TypeParameter>();
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitStructDeclaration(this);
    }

    public class EnumDeclaration : Declaration
    {
        public TypeNode? UnderlyingType { get; }
        public List<EnumMember> Members { get; }

        public EnumDeclaration(Token enumToken, Token nameToken, TypeNode? underlyingType,
                              List<EnumMember> members, List<Modifier>? modifiers = null) 
            : base(nameToken, modifiers)
        {
            UnderlyingType = underlyingType;
            Members = members;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitEnumDeclaration(this);
    }

    public class EnumMember
    {
        public string Name { get; }
        public Expression? Value { get; }

        public EnumMember(string name, Expression? value = null)
        {
            Name = name;
            Value = value;
        }
    }

    public class DomainDeclaration : Declaration
    {
        public List<Statement> Members { get; }

        public DomainDeclaration(Token domainToken, Token nameToken, List<Statement> members,
                                List<Modifier>? modifiers = null) : base(nameToken, modifiers)
        {
            Members = members;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitDomainDeclaration(this);
    }

    public class FunctionDeclaration : Declaration
    {
        public TypeNode ReturnType { get; }
        public List<Parameter> Parameters { get; }
        public BlockStatement Body { get; }
        public List<TypeParameter> TypeParameters { get; }
        public bool IsAsync { get; }

        public FunctionDeclaration(Token nameToken, TypeNode returnType, List<Parameter> parameters,
                                  BlockStatement body, List<TypeParameter>? typeParameters = null,
                                  bool isAsync = false, List<Modifier>? modifiers = null) 
            : base(nameToken, modifiers)
        {
            ReturnType = returnType;
            Parameters = parameters;
            Body = body;
            TypeParameters = typeParameters ?? new List<TypeParameter>();
            IsAsync = isAsync;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitFunctionDeclaration(this);
    }

    public class PropertyDeclaration : Declaration
    {
        public TypeNode Type { get; }
        public BlockStatement? Getter { get; }
        public BlockStatement? Setter { get; }
        public Expression? Initializer { get; }

        public PropertyDeclaration(Token nameToken, TypeNode type, BlockStatement? getter,
                                  BlockStatement? setter, Expression? initializer = null,
                                  List<Modifier>? modifiers = null) : base(nameToken, modifiers)
        {
            Type = type;
            Getter = getter;
            Setter = setter;
            Initializer = initializer;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitPropertyDeclaration(this);
    }

    public class FieldDeclaration : Declaration
    {
        public TypeNode Type { get; }
        public Expression? Initializer { get; }

        public FieldDeclaration(Token nameToken, TypeNode type, Expression? initializer = null,
                               List<Modifier>? modifiers = null) : base(nameToken, modifiers)
        {
            Type = type;
            Initializer = initializer;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitFieldDeclaration(this);
    }

    public class NamespaceDeclaration : Declaration
    {
        public List<Statement> Members { get; }

        public NamespaceDeclaration(Token namespaceToken, string name, List<Statement> members) 
            : base(new Token(TokenType.Identifier, name, name, 0, 0, 0, 0, "", SyntaxLevel.Medium))
        {
            Members = members;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitNamespaceDeclaration(this);
    }

    public class ImportDeclaration : Statement
    {
        public Token? ImportToken { get; }
        public string ModulePath { get; }
        public string? Alias { get; }
        public List<string>? ImportedNames { get; }
        public bool IsStatic { get; }

        public ImportDeclaration(Token? importToken, string modulePath, string? alias = null, List<string>? importedNames = null, bool isStatic = false)
        {
            ImportToken = importToken;
            ModulePath = modulePath;
            Alias = alias;
            ImportedNames = importedNames;
            IsStatic = isStatic;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitImportDeclaration(this);
    }

    public class TypeAliasDeclaration : Declaration
    {
        public TypeNode AliasedType { get; }

        public TypeAliasDeclaration(Token aliasToken, Token nameToken, TypeNode aliasedType) 
            : base(nameToken)
        {
            AliasedType = aliasedType;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitTypeAliasDeclaration(this);
    }

    // Data-Oriented Programming support
    public class ComponentDeclaration : Declaration
    {
        public List<FieldDeclaration> Fields { get; }

        public ComponentDeclaration(Token componentToken, Token nameToken, List<FieldDeclaration> fields,
                                   List<Modifier>? modifiers = null) : base(nameToken, modifiers)
        {
            Fields = fields;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitComponentDeclaration(this);
    }

    public class SystemDeclaration : Declaration
    {
        public List<TypeNode> RequiredComponents { get; }
        public List<FunctionDeclaration> Methods { get; }

        public SystemDeclaration(Token systemToken, Token nameToken, List<TypeNode> requiredComponents,
                                List<FunctionDeclaration> methods, List<Modifier>? modifiers = null) 
            : base(nameToken, modifiers)
        {
            RequiredComponents = requiredComponents;
            Methods = methods;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitSystemDeclaration(this);
    }

    public class EntityDeclaration : Declaration
    {
        public List<TypeNode> Components { get; }

        public EntityDeclaration(Token entityToken, Token nameToken, List<TypeNode> components,
                                List<Modifier>? modifiers = null) : base(nameToken, modifiers)
        {
            Components = components;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitEntityDeclaration(this);
    }

    public class MacroDeclaration : Declaration
    {
        public List<Parameter> Parameters { get; }
        public BlockStatement Body { get; }

        public MacroDeclaration(Token macroToken, Token nameToken, List<Parameter> parameters,
                               BlockStatement body, List<Modifier>? modifiers = null) 
            : base(nameToken, modifiers)
        {
            Parameters = parameters;
            Body = body;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitMacroDeclaration(this);
    }

    public class TraitDeclaration : Declaration
    {
        public List<TypeParameter> TypeParameters { get; }
        public List<Declaration> Members { get; }

        public TraitDeclaration(Token traitToken, Token nameToken, List<TypeParameter> typeParameters,
                               List<Declaration> members, List<Modifier>? modifiers = null) 
            : base(nameToken, modifiers)
        {
            TypeParameters = typeParameters;
            Members = members;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitTraitDeclaration(this);
    }

    public class ImplementDeclaration : Declaration
    {
        public TypeNode TraitType { get; }
        public TypeNode? TargetType { get; }
        public List<Declaration> Members { get; }

        public ImplementDeclaration(Token implementToken, TypeNode traitType, TypeNode? targetType,
                                   List<Declaration> members, List<Modifier>? modifiers = null) 
            : base(implementToken, modifiers)
        {
            TraitType = traitType;
            TargetType = targetType;
            Members = members;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitImplementDeclaration(this);
    }

    #endregion

    #region Support Classes

    public class Program : AstNode
    {
        public List<Statement> Statements { get; }

        public Program(List<Statement> statements) : base()
        {
            Statements = statements;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitProgram(this);
    }

    public class Parameter
    {
        public TypeNode Type { get; }
        public string Name { get; }
        public Expression? DefaultValue { get; }
        public ParameterModifier Modifier { get; }

        public Parameter(TypeNode type, string name, Expression? defaultValue = null,
                        ParameterModifier modifier = ParameterModifier.None)
        {
            Type = type;
            Name = name;
            DefaultValue = defaultValue;
            Modifier = modifier;
        }
    }

    public enum ParameterModifier
    {
        None,
        Ref,
        Out,
        In,
        Params
    }

    public class TypeNode
    {
        public string Name { get; }
        public List<TypeNode>? TypeArguments { get; }
        public bool IsArray { get; }
        public int ArrayRank { get; }
        public bool IsNullable { get; }
        public bool IsPointer { get; }
        public bool IsReference { get; }

        public TypeNode(string name, List<TypeNode>? typeArguments = null, 
                       bool isArray = false, int arrayRank = 0, bool isNullable = false, bool isPointer = false, bool isReference = false)
        {
            Name = name;
            TypeArguments = typeArguments ?? new List<TypeNode>();
            IsArray = isArray;
            ArrayRank = arrayRank;
            IsNullable = isNullable;
            IsPointer = isPointer;
            IsReference = isReference;
        }
    }

    public class TypeParameter
    {
        public string Name { get; }
        public List<TypeNode>? Constraints { get; }
        public bool IsCovariant { get; }
        public bool IsContravariant { get; }

        public TypeParameter(string name, List<TypeNode>? constraints = null,
                            bool isCovariant = false, bool isContravariant = false)
        {
            Name = name;
            Constraints = constraints ?? new List<TypeNode>();
            IsCovariant = isCovariant;
            IsContravariant = isContravariant;
        }
    }

    public enum Modifier
    {
        Public,
        Private,
        Protected,
        Internal,
        Static,
        Abstract,
        Virtual,
        Override,
        Sealed,
        Readonly,
        Const,
        Volatile,
        Unsafe,
        Async,
        Partial,
        Extern,
        Operator
    }

    public enum MathOperationType
    {
        // Basic operations
        Add,
        Subtract,
        Multiply,
        Divide,
        Power,
        SquareRoot,
        CubeRoot,
        Absolute,
        
        // Trigonometry
        Sin,
        Cos,
        Tan,
        Asin,
        Acos,
        Atan,
        Atan2,
        
        // Advanced math
        Log,
        Log10,
        Exp,
        Floor,
        Ceiling,
        Round,
        
        // Vector operations
        DotProduct,
        CrossProduct,
        Normalize,
        
        // Matrix operations
        MatrixMultiply,
        Transpose,
        Determinant,
        Inverse,
        
        // Calculus
        Derivative,
        Integral,
        PartialDerivative,
        
        // Set operations
        Union,
        Intersection,
        Difference,
        
        // Special
        Summation,
        Product
    }

    public class WildcardPattern : Pattern
    {
        public WildcardPattern() : base() { }
    }
    
    /// <summary>
    /// Constant pattern for pattern matching
    /// </summary>
    public class ConstantPattern : Pattern
    {
        public Expression Value { get; set; }
        
        public ConstantPattern(Expression value) : base()
        {
            Value = value;
        }
    }

    /// <summary>
    /// Match expression for pattern matching
    /// </summary>
    public class MatchExpression : Expression
    {
        public Expression Target { get; }
        public List<MatchArm> Arms { get; }

        public MatchExpression(Expression target, List<MatchArm> arms) : base(target.Token)
        {
            Target = target;
            Arms = arms;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitMatchExpression(this);
    }

    /// <summary>
    /// Match arm in a match expression
    /// </summary>
    public class MatchArm : AstNode
    {
        public Pattern Pattern { get; }
        public Expression Guard { get; }
        public Expression Body { get; }

        public MatchArm(Pattern pattern, Expression guard, Expression body) : base(body.Token)
        {
            Pattern = pattern;
            Guard = guard;
            Body = body;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitMatchArm(this);
    }

    /// <summary>
    /// Literal pattern for pattern matching
    /// </summary>
    public class LiteralPattern : Pattern
    {
        public Expression Value { get; }

        public LiteralPattern(Expression value) : base()
        {
            Value = value;
        }
    }

    /// <summary>
    /// Identifier pattern for variable capture
    /// </summary>
    public class IdentifierPattern : Pattern
    {
        public Token Identifier { get; }

        public IdentifierPattern(Token identifier) : base()
        {
            Identifier = identifier;
        }
    }

    /// <summary>
    /// Tuple pattern for destructuring tuples
    /// </summary>
    public class TupleMatchPattern : Pattern
    {
        public List<Pattern> Patterns { get; }

        public TupleMatchPattern(List<Pattern> patterns) : base()
        {
            Patterns = patterns;
        }
    }

    /// <summary>
    /// Throw expression (C# 7.0+ feature) - throw as expression, not statement
    /// </summary>
    public class ThrowExpression : Expression
    {
        public Expression? Expression { get; }

        public ThrowExpression(Token throwKeyword, Expression? expression) : base(throwKeyword)
        {
            Expression = expression;
        }

        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitThrowExpression(this);
    }

    #endregion
} 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ouro.Core.AST;
using Ouro.Core.Lexer;
using Ouro.Core.Parser;
using Ouro.Tokens;

namespace Ouro.Core.Macros
{
    /// <summary>
    /// Macro system for Ouro with hygiene support
    /// </summary>
    public class MacroSystem
    {
        private readonly Dictionary<string, MacroDefinition> macros = new();
        private readonly MacroExpander expander;
        private readonly HygieneContext hygieneContext;
        private int expansionDepth = 0;
        private const int MaxExpansionDepth = 100;

        public MacroSystem()
        {
            expander = new MacroExpander(this);
            hygieneContext = new HygieneContext();
        }

        /// <summary>
        /// Define a new macro
        /// </summary>
        public void DefineMacro(string name, MacroDefinition macro)
        {
            macros[name] = macro;
        }

        /// <summary>
        /// Check if a macro is defined
        /// </summary>
        public bool IsMacroDefined(string name)
        {
            return macros.ContainsKey(name);
        }

        /// <summary>
        /// Get macro definition
        /// </summary>
        public MacroDefinition? GetMacro(string name)
        {
            return macros.TryGetValue(name, out var macro) ? macro : null;
        }

        /// <summary>
        /// Expand a macro invocation
        /// </summary>
        public AstNode ExpandMacro(MacroInvocation invocation)
        {
            if (expansionDepth >= MaxExpansionDepth)
            {
                throw new MacroExpansionException($"Maximum macro expansion depth ({MaxExpansionDepth}) exceeded");
            }

            if (!macros.TryGetValue(invocation.MacroName, out var macro))
            {
                throw new MacroExpansionException($"Undefined macro: {invocation.MacroName}");
            }

            expansionDepth++;
            try
            {
                // Create new hygiene scope
                using var scope = hygieneContext.CreateScope();

                // Bind arguments
                var bindings = BindArguments(macro, invocation);

                // Expand the macro body
                var expanded = expander.Expand(macro.Body, bindings, scope);

                // Apply hygiene renaming
                expanded = hygieneContext.ApplyHygiene(expanded, scope);

                return expanded;
            }
            finally
            {
                expansionDepth--;
            }
        }

        /// <summary>
        /// Expand all macros in an AST
        /// </summary>
        public AstNode ExpandAllMacros(AstNode ast)
        {
            var visitor = new MacroExpansionVisitor(this);
            return visitor.Visit(ast);
        }

        private Dictionary<string, AstNode> BindArguments(MacroDefinition macro, MacroInvocation invocation)
        {
            var bindings = new Dictionary<string, AstNode>();

            if (macro.Parameters.Count != invocation.Arguments.Count)
            {
                throw new MacroExpansionException(
                    $"Macro '{invocation.MacroName}' expects {macro.Parameters.Count} arguments, " +
                    $"but {invocation.Arguments.Count} were provided");
            }

            for (int i = 0; i < macro.Parameters.Count; i++)
            {
                bindings[macro.Parameters[i].Name] = invocation.Arguments[i];
            }

            return bindings;
        }
    }

    /// <summary>
    /// Macro definition
    /// </summary>
    public class MacroDefinition
    {
        public string Name { get; set; }
        public List<MacroParameter> Parameters { get; set; }
        public AstNode Body { get; set; }
        public MacroType Type { get; set; }
        public bool IsVariadic { get; set; }

        public MacroDefinition(string name, List<MacroParameter> parameters, AstNode body)
        {
            Name = name;
            Parameters = parameters;
            Body = body;
            Type = MacroType.Expression;
            IsVariadic = parameters.Any(p => p.IsVariadic);
        }
    }

    /// <summary>
    /// Macro parameter
    /// </summary>
    public class MacroParameter
    {
        public string Name { get; set; }
        public MacroParameterType Type { get; set; }
        public bool IsVariadic { get; set; }

        public MacroParameter(string name, MacroParameterType type = MacroParameterType.Expression, bool isVariadic = false)
        {
            Name = name;
            Type = type;
            IsVariadic = isVariadic;
        }
    }

    /// <summary>
    /// Macro parameter type
    /// </summary>
    public enum MacroParameterType
    {
        Expression,
        Statement,
        Type,
        Pattern,
        Identifier,
        Literal
    }

    /// <summary>
    /// Macro type
    /// </summary>
    public enum MacroType
    {
        Expression,
        Statement,
        Declaration,
        Pattern
    }

    /// <summary>
    /// Macro invocation
    /// </summary>
    public class MacroInvocation : Expression
    {
        public string MacroName { get; set; }
        public List<AstNode> Arguments { get; set; }

        public MacroInvocation(Token token, string macroName, List<AstNode> arguments) : base(token)
        {
            MacroName = macroName;
            Arguments = arguments;
        }

        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            // This should be expanded before visiting
            throw new InvalidOperationException("Macro invocation should be expanded before AST visiting");
        }
    }

    /// <summary>
    /// Macro expander
    /// </summary>
    public class MacroExpander
    {
        private readonly MacroSystem macroSystem;

        public MacroExpander(MacroSystem macroSystem)
        {
            this.macroSystem = macroSystem;
        }

        /// <summary>
        /// Expand macro body with argument bindings
        /// </summary>
        public AstNode Expand(AstNode body, Dictionary<string, AstNode> bindings, HygieneScope scope)
        {
            var visitor = new MacroSubstitutionVisitor(bindings, scope);
            return visitor.Visit(body);
        }
    }

    /// <summary>
    /// Hygiene context for macro expansion
    /// </summary>
    public class HygieneContext
    {
        private int scopeCounter = 0;
        private readonly Stack<HygieneScope> scopeStack = new();

        /// <summary>
        /// Create a new hygiene scope
        /// </summary>
        public HygieneScope CreateScope()
        {
            var scope = new HygieneScope(++scopeCounter, this);
            scopeStack.Push(scope);
            return scope;
        }

        /// <summary>
        /// Apply hygiene renaming to an AST
        /// </summary>
        public AstNode ApplyHygiene(AstNode ast, HygieneScope scope)
        {
            var visitor = new HygieneRenamingVisitor(scope);
            return visitor.Visit(ast);
        }

        internal void PopScope(HygieneScope scope)
        {
            if (scopeStack.Peek() == scope)
            {
                scopeStack.Pop();
            }
        }
    }

    /// <summary>
    /// Hygiene scope
    /// </summary>
    public class HygieneScope : IDisposable
    {
        private readonly int id;
        private readonly HygieneContext context;
        private readonly Dictionary<string, string> renamedIdentifiers = new();

        public int Id => id;

        internal HygieneScope(int id, HygieneContext context)
        {
            this.id = id;
            this.context = context;
        }

        /// <summary>
        /// Get renamed identifier for hygiene
        /// </summary>
        public string GetRenamedIdentifier(string original)
        {
            if (!renamedIdentifiers.TryGetValue(original, out var renamed))
            {
                renamed = $"{original}__hygiene_{id}";
                renamedIdentifiers[original] = renamed;
            }
            return renamed;
        }

        public void Dispose()
        {
            context.PopScope(this);
        }
    }

    /// <summary>
    /// Visitor for macro expansion
    /// </summary>
    internal class MacroExpansionVisitor : AstVisitorBase<AstNode>
    {
        private readonly MacroSystem macroSystem;

        public MacroExpansionVisitor(MacroSystem macroSystem)
        {
            this.macroSystem = macroSystem;
        }

        public AstNode Visit(AstNode node)
        {
            return node.Accept(this);
        }

        protected override AstNode DefaultVisit(AstNode node)
        {
            // Check if this is a macro invocation
            if (node is CallExpression call && call.Callee is IdentifierExpression id)
            {
                if (macroSystem.IsMacroDefined(id.Name))
                {
                    var invocation = new MacroInvocation(call.Token, id.Name, call.Arguments.Cast<AstNode>().ToList());
                    return macroSystem.ExpandMacro(invocation);
                }
            }

            // Otherwise, recursively visit children
            return base.DefaultVisit(node);
        }
    }

    /// <summary>
    /// Visitor for macro argument substitution
    /// </summary>
    internal class MacroSubstitutionVisitor : AstVisitorBase<AstNode>
    {
        private readonly Dictionary<string, AstNode> bindings;
        private readonly HygieneScope scope;

        public MacroSubstitutionVisitor(Dictionary<string, AstNode> bindings, HygieneScope scope)
        {
            this.bindings = bindings;
            this.scope = scope;
        }

        public AstNode Visit(AstNode node)
        {
            return node.Accept(this);
        }

        public override AstNode VisitIdentifierExpression(IdentifierExpression expr)
        {
            // Check if this identifier should be substituted
            if (bindings.TryGetValue(expr.Name, out var substitution))
            {
                return substitution;
            }

            return expr;
        }

        protected override AstNode DefaultVisit(AstNode node)
        {
            // Recursively visit and transform children
            return base.DefaultVisit(node);
        }
    }

    /// <summary>
    /// Visitor for hygiene renaming
    /// </summary>
    internal class HygieneRenamingVisitor : AstVisitorBase<AstNode>
    {
        private readonly HygieneScope scope;
        private readonly HashSet<string> capturedIdentifiers = new();

        public HygieneRenamingVisitor(HygieneScope scope)
        {
            this.scope = scope;
        }

        public AstNode Visit(AstNode node)
        {
            return node.Accept(this);
        }

        public override AstNode VisitIdentifierExpression(IdentifierExpression expr)
        {
            // Only rename local identifiers, not captured ones
            if (!capturedIdentifiers.Contains(expr.Name))
            {
                var renamed = scope.GetRenamedIdentifier(expr.Name);
                return new IdentifierExpression(
                    new Token(expr.Token!.Type, renamed, renamed, 
                        expr.Token.Line, expr.Token.Column, 
                        expr.Token.StartPosition, expr.Token.EndPosition, 
                        expr.Token.FileName, expr.Token.SyntaxLevel));
            }

            return expr;
        }

        public override AstNode VisitVariableDeclaration(VariableDeclaration decl)
        {
            // Rename the variable declaration
            var renamed = scope.GetRenamedIdentifier(decl.Name);
            var newDecl = new VariableDeclaration(
                decl.Type,
                new Token(decl.Token!.Type, renamed, renamed,
                    decl.Token.Line, decl.Token.Column,
                    decl.Token.StartPosition, decl.Token.EndPosition,
                    decl.Token.FileName, decl.Token.SyntaxLevel),
                decl.Initializer != null ? Visit(decl.Initializer) as Expression : null,
                decl.IsConst,
                decl.IsReadonly);

            return newDecl;
        }

        protected override AstNode DefaultVisit(AstNode node)
        {
            // Recursively visit and transform children
            return base.DefaultVisit(node);
        }
    }

    /// <summary>
    /// Base visitor for AST transformation
    /// </summary>
    internal abstract class AstVisitorBase<T> : IAstVisitor<T> where T : class
    {
        protected virtual T DefaultVisit(AstNode node)
        {
            // Default implementation - return the node unchanged
            return node as T ?? throw new InvalidOperationException($"Cannot convert {node.GetType()} to {typeof(T)}");
        }

        // Implement all visitor methods with default behavior
        public virtual T VisitBinaryExpression(BinaryExpression expr) => DefaultVisit(expr);
        public virtual T VisitUnaryExpression(UnaryExpression expr) => DefaultVisit(expr);
        public virtual T VisitLiteralExpression(LiteralExpression expr) => DefaultVisit(expr);
        public virtual T VisitIdentifierExpression(IdentifierExpression expr) => DefaultVisit(expr);
        public virtual T VisitGenericIdentifierExpression(GenericIdentifierExpression expr) => DefaultVisit(expr);
        public virtual T VisitAssignmentExpression(AssignmentExpression expr) => DefaultVisit(expr);
        public virtual T VisitCallExpression(CallExpression expr) => DefaultVisit(expr);
        public virtual T VisitMemberExpression(MemberExpression expr) => DefaultVisit(expr);
        public virtual T VisitArrayExpression(ArrayExpression expr) => DefaultVisit(expr);
        public virtual T VisitLambdaExpression(LambdaExpression expr) => DefaultVisit(expr);
        public virtual T VisitConditionalExpression(ConditionalExpression expr) => DefaultVisit(expr);
        public virtual T VisitNewExpression(NewExpression expr) => DefaultVisit(expr);
        public virtual T VisitThisExpression(ThisExpression expr) => DefaultVisit(expr);
        public virtual T VisitBaseExpression(BaseExpression expr) => DefaultVisit(expr);
        public virtual T VisitTypeofExpression(TypeofExpression expr) => DefaultVisit(expr);
        public virtual T VisitSizeofExpression(SizeofExpression expr) => DefaultVisit(expr);
        public virtual T VisitNameofExpression(NameofExpression expr) => DefaultVisit(expr);
        public virtual T VisitInterpolatedStringExpression(InterpolatedStringExpression expr) => DefaultVisit(expr);
        public virtual T VisitMathExpression(MathExpression expr) => DefaultVisit(expr);
        public virtual T VisitVectorExpression(VectorExpression expr) => DefaultVisit(expr);
        public virtual T VisitMatrixExpression(MatrixExpression expr) => DefaultVisit(expr);
        public virtual T VisitQuaternionExpression(QuaternionExpression expr) => DefaultVisit(expr);
        public virtual T VisitIsExpression(IsExpression expr) => DefaultVisit(expr);
        public virtual T VisitCastExpression(CastExpression expr) => DefaultVisit(expr);
        public virtual T VisitMatchExpression(MatchExpression expr) => DefaultVisit(expr);
        public virtual T VisitThrowExpression(ThrowExpression expr) => DefaultVisit(expr);
        public virtual T VisitMatchArm(MatchArm arm) => DefaultVisit(arm);
        public virtual T VisitStructLiteral(StructLiteral expr) => DefaultVisit(expr);

        public virtual T VisitBlockStatement(BlockStatement stmt) => DefaultVisit(stmt);
        public virtual T VisitExpressionStatement(ExpressionStatement stmt) => DefaultVisit(stmt);
        public virtual T VisitVariableDeclaration(VariableDeclaration stmt) => DefaultVisit(stmt);
        public virtual T VisitIfStatement(IfStatement stmt) => DefaultVisit(stmt);
        public virtual T VisitWhileStatement(WhileStatement stmt) => DefaultVisit(stmt);
        public virtual T VisitForStatement(ForStatement stmt) => DefaultVisit(stmt);
        public virtual T VisitForEachStatement(ForEachStatement stmt) => DefaultVisit(stmt);
        public virtual T VisitRepeatStatement(RepeatStatement stmt) => DefaultVisit(stmt);
        public virtual T VisitIterateStatement(IterateStatement stmt) => DefaultVisit(stmt);
        public virtual T VisitParallelForStatement(ParallelForStatement stmt) => DefaultVisit(stmt);
        public virtual T VisitDoWhileStatement(DoWhileStatement stmt) => DefaultVisit(stmt);
        public virtual T VisitSwitchStatement(SwitchStatement stmt) => DefaultVisit(stmt);
        public virtual T VisitReturnStatement(ReturnStatement stmt) => DefaultVisit(stmt);
        public virtual T VisitBreakStatement(BreakStatement stmt) => DefaultVisit(stmt);
        public virtual T VisitContinueStatement(ContinueStatement stmt) => DefaultVisit(stmt);
        public virtual T VisitThrowStatement(ThrowStatement stmt) => DefaultVisit(stmt);
        public virtual T VisitTryStatement(TryStatement stmt) => DefaultVisit(stmt);
        public virtual T VisitUsingStatement(UsingStatement stmt) => DefaultVisit(stmt);
        public virtual T VisitLockStatement(LockStatement stmt) => DefaultVisit(stmt);
        public virtual T VisitUnsafeStatement(UnsafeStatement stmt) => DefaultVisit(stmt);
        public virtual T VisitFixedStatement(FixedStatement stmt) => DefaultVisit(stmt);
        public virtual T VisitYieldStatement(YieldStatement stmt) => DefaultVisit(stmt);
        public virtual T VisitMatchStatement(MatchStatement stmt) => DefaultVisit(stmt);
        public virtual T VisitAssemblyStatement(AssemblyStatement stmt) => DefaultVisit(stmt);

        public virtual T VisitClassDeclaration(ClassDeclaration decl) => DefaultVisit(decl);
        public virtual T VisitInterfaceDeclaration(InterfaceDeclaration decl) => DefaultVisit(decl);
        public virtual T VisitStructDeclaration(StructDeclaration decl) => DefaultVisit(decl);
        public virtual T VisitEnumDeclaration(EnumDeclaration decl) => DefaultVisit(decl);
        public virtual T VisitFunctionDeclaration(FunctionDeclaration decl) => DefaultVisit(decl);
        public virtual T VisitPropertyDeclaration(PropertyDeclaration decl) => DefaultVisit(decl);
        public virtual T VisitFieldDeclaration(FieldDeclaration decl) => DefaultVisit(decl);
        public virtual T VisitNamespaceDeclaration(NamespaceDeclaration decl) => DefaultVisit(decl);
        public virtual T VisitImportDeclaration(Core.AST.ImportDeclaration decl) => DefaultVisit(decl);
        public virtual T VisitTypeAliasDeclaration(TypeAliasDeclaration decl) => DefaultVisit(decl);

        public virtual T VisitComponentDeclaration(ComponentDeclaration decl) => DefaultVisit(decl);
        public virtual T VisitSystemDeclaration(SystemDeclaration decl) => DefaultVisit(decl);
        public virtual T VisitEntityDeclaration(EntityDeclaration decl) => DefaultVisit(decl);
        public virtual T VisitDomainDeclaration(DomainDeclaration decl) => DefaultVisit(decl);
        public virtual T VisitMacroDeclaration(MacroDeclaration decl) => DefaultVisit(decl);
        public virtual T VisitTraitDeclaration(TraitDeclaration decl) => DefaultVisit(decl);
        public virtual T VisitImplementDeclaration(ImplementDeclaration decl) => DefaultVisit(decl);
        public virtual T VisitProgram(Core.AST.Program program) => DefaultVisit(program);
    }

    /// <summary>
    /// Macro expansion exception
    /// </summary>
    public class MacroExpansionException : Exception
    {
        public MacroExpansionException(string message) : base(message) { }
        public MacroExpansionException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Built-in macros
    /// </summary>
    public static class BuiltInMacros
    {
        /// <summary>
        /// Register built-in macros
        /// </summary>
        public static void RegisterBuiltInMacros(MacroSystem macroSystem)
        {
            // assert! macro
            RegisterAssertMacro(macroSystem);

            // debug! macro
            RegisterDebugMacro(macroSystem);

            // todo! macro
            RegisterTodoMacro(macroSystem);

            // unreachable! macro
            RegisterUnreachableMacro(macroSystem);

            // stringify! macro
            RegisterStringifyMacro(macroSystem);

            // vec! macro for vector literals
            RegisterVecMacro(macroSystem);

            // try! macro for error handling
            RegisterTryMacro(macroSystem);
        }

        private static void RegisterAssertMacro(MacroSystem macroSystem)
        {
            // assert!(condition) => if (!condition) throw new AssertionException("Assertion failed: condition");
            var macro = new MacroDefinition("assert", 
                new List<MacroParameter> { new MacroParameter("condition", MacroParameterType.Expression) },
                null); // Body will be created dynamically
            
            macro.Type = MacroType.Statement;
            macroSystem.DefineMacro("assert", macro);
        }

        private static void RegisterDebugMacro(MacroSystem macroSystem)
        {
            // debug!(expr) => Console.WriteLine($"Debug: {nameof(expr)} = {expr}");
            var macro = new MacroDefinition("debug",
                new List<MacroParameter> { new MacroParameter("expr", MacroParameterType.Expression) },
                null); // Body will be created dynamically
            
            macro.Type = MacroType.Statement;
            macroSystem.DefineMacro("debug", macro);
        }

        private static void RegisterTodoMacro(MacroSystem macroSystem)
        {
            // todo!() => throw new NotImplementedException("TODO: Not implemented");
            // todo!(message) => throw new NotImplementedException($"TODO: {message}");
            var macro = new MacroDefinition("todo",
                new List<MacroParameter> { new MacroParameter("message", MacroParameterType.Expression) { IsVariadic = true } },
                null); // Body will be created dynamically
            
            macro.Type = MacroType.Expression;
            macroSystem.DefineMacro("todo", macro);
        }

        private static void RegisterUnreachableMacro(MacroSystem macroSystem)
        {
            // unreachable!() => throw new UnreachableException("This code should be unreachable");
            var macro = new MacroDefinition("unreachable",
                new List<MacroParameter>(),
                null); // Body will be created dynamically
            
            macro.Type = MacroType.Expression;
            macroSystem.DefineMacro("unreachable", macro);
        }

        private static void RegisterStringifyMacro(MacroSystem macroSystem)
        {
            // stringify!(expr) => "expr" (converts expression to string literal)
            var macro = new MacroDefinition("stringify",
                new List<MacroParameter> { new MacroParameter("expr", MacroParameterType.Expression) },
                null); // Body will be created dynamically
            
            macro.Type = MacroType.Expression;
            macroSystem.DefineMacro("stringify", macro);
        }

        private static void RegisterVecMacro(MacroSystem macroSystem)
        {
            // vec![1, 2, 3] => new Vector3(1, 2, 3)
            // vec![x; n] => Vector.Repeat(x, n)
            var macro = new MacroDefinition("vec",
                new List<MacroParameter> { new MacroParameter("elements", MacroParameterType.Expression) { IsVariadic = true } },
                null); // Body will be created dynamically
            
            macro.Type = MacroType.Expression;
            macroSystem.DefineMacro("vec", macro);
        }

        private static void RegisterTryMacro(MacroSystem macroSystem)
        {
            // try!(expr) => expr.IsError ? return expr.Error : expr.Value
            var macro = new MacroDefinition("try",
                new List<MacroParameter> { new MacroParameter("expr", MacroParameterType.Expression) },
                null); // Body will be created dynamically
            
            macro.Type = MacroType.Expression;
            macroSystem.DefineMacro("try", macro);
        }
    }
} 
using System;
using System.Collections.Generic;
using System.Linq;
using Ouro.Core;
using Ouro.Core.AST;
using Ouro.Core.Compiler;
using Ouro.Core.VM;
using Ouro.Tokens;

namespace Ouro.Optimization
{
    /// <summary>
    /// Main optimizer for Ouroboros AST and bytecode optimization
    /// </summary>
    public class Optimizer
    {
        private readonly OptimizationOptions options;
        private readonly List<IOptimizationPass> astPasses;
        private readonly List<IOptimizationPass> bytecodePasses;
        
        public Optimizer(OptimizationOptions options = null)
        {
            this.options = options ?? new OptimizationOptions();
            astPasses = new List<IOptimizationPass>();
            bytecodePasses = new List<IOptimizationPass>();
            
            InitializePasses();
        }
        
        private void InitializePasses()
        {
            // AST optimization passes
            if (options.EnableConstantFolding)
                astPasses.Add(new ConstantFoldingPass());
            
            if (options.EnableDeadCodeElimination)
                astPasses.Add(new DeadCodeEliminationPass());
            
            if (options.EnableInlining)
                astPasses.Add(new InliningPass());
            
            if (options.EnableLoopOptimization)
                astPasses.Add(new LoopOptimizationPass());
            
            if (options.EnableCommonSubexpressionElimination)
                astPasses.Add(new CommonSubexpressionEliminationPass());
            
            // Bytecode optimization passes
            if (options.EnablePeepholeOptimization)
                bytecodePasses.Add(new PeepholeOptimizationPass());
            
            if (options.EnableRegisterAllocation)
                bytecodePasses.Add(new RegisterAllocationPass());
        }
        
        /// <summary>
        /// Optimize AST before compilation
        /// </summary>
        public List<Statement> OptimizeAst(List<Statement> ast)
        {
            var optimized = ast;
            
            foreach (var pass in astPasses)
            {
                optimized = pass.Apply(optimized);
            }
            
            return optimized;
        }
        
        /// <summary>
        /// Optimize bytecode after compilation
        /// </summary>
        public byte[] OptimizeBytecode(byte[] bytecode)
        {
            var optimized = bytecode;
            
            foreach (var pass in bytecodePasses)
            {
                optimized = pass.ApplyToBytecode(optimized);
            }
            
            return optimized;
        }
    }
    
    /// <summary>
    /// Optimization options
    /// </summary>
    public class OptimizationOptions
    {
        public OptimizationLevel Level { get; set; } = OptimizationLevel.Balanced;
        public bool EnableConstantFolding { get; set; } = true;
        public bool EnableDeadCodeElimination { get; set; } = true;
        public bool EnableInlining { get; set; } = true;
        public bool EnableLoopOptimization { get; set; } = true;
        public bool EnableCommonSubexpressionElimination { get; set; } = true;
        public bool EnablePeepholeOptimization { get; set; } = true;
        public bool EnableRegisterAllocation { get; set; } = true;
        public bool EnableTailCallOptimization { get; set; } = true;
        public bool PreserveDebugInfo { get; set; } = false;
        
        public static OptimizationOptions None => new OptimizationOptions 
        { 
            Level = OptimizationLevel.None,
            EnableConstantFolding = false,
            EnableDeadCodeElimination = false,
            EnableInlining = false,
            EnableLoopOptimization = false,
            EnableCommonSubexpressionElimination = false,
            EnablePeepholeOptimization = false,
            EnableRegisterAllocation = false,
            EnableTailCallOptimization = false
        };
        
        public static OptimizationOptions Debug => new OptimizationOptions
        {
            Level = OptimizationLevel.Debug,
            EnableConstantFolding = true,
            EnableDeadCodeElimination = false,
            EnableInlining = false,
            EnableLoopOptimization = false,
            EnableCommonSubexpressionElimination = false,
            EnablePeepholeOptimization = true,
            EnableRegisterAllocation = false,
            EnableTailCallOptimization = false,
            PreserveDebugInfo = true
        };
        
        public static OptimizationOptions Release => new OptimizationOptions
        {
            Level = OptimizationLevel.Aggressive,
            EnableConstantFolding = true,
            EnableDeadCodeElimination = true,
            EnableInlining = true,
            EnableLoopOptimization = true,
            EnableCommonSubexpressionElimination = true,
            EnablePeepholeOptimization = true,
            EnableRegisterAllocation = true,
            EnableTailCallOptimization = true,
            PreserveDebugInfo = false
        };
    }
    
    public enum OptimizationLevel
    {
        None,
        Debug,
        Balanced,
        Aggressive
    }
    
    /// <summary>
    /// Base interface for optimization passes
    /// </summary>
    public interface IOptimizationPass
    {
        string Name { get; }
        List<Statement> Apply(List<Statement> ast);
        byte[] ApplyToBytecode(byte[] bytecode);
    }
    
    /// <summary>
    /// Constant folding optimization pass
    /// </summary>
    public class ConstantFoldingPass : IOptimizationPass
    {
        public string Name => "Constant Folding";
        
        public List<Statement> Apply(List<Statement> ast)
        {
            var visitor = new ConstantFoldingVisitor();
            return ast.Select(stmt => visitor.VisitStatement(stmt)).ToList();
        }
        
        public byte[] ApplyToBytecode(byte[] bytecode) => bytecode;
        
        private class ConstantFoldingVisitor : AstVisitor<AstNode>
        {
            public override Expression VisitBinaryExpression(BinaryExpression expr)
            {
                var left = Visit(expr.Left) as Expression;
                var right = Visit(expr.Right) as Expression;
                
                // If both operands are constants, fold them
                if (left is LiteralExpression leftLit && right is LiteralExpression rightLit)
                {
                    try
                    {
                        object result = EvaluateBinary(expr.Operator, leftLit.Value, rightLit.Value);
                        return new LiteralExpression(new Token(expr.Token.Type, result.ToString(), result, expr.Line, expr.Column, 0, 0, expr.FileName, expr.SyntaxLevel));
                    }
                    catch
                    {
                        // If evaluation fails, return original
                    }
                }
                
                return new BinaryExpression(left, expr.Operator, right);
            }
            
            private object EvaluateBinary(Token op, object left, object right)
            {
                if (left is int leftInt && right is int rightInt)
                {
                    return op.Type switch
                    {
                        TokenType.Plus => leftInt + rightInt,
                        TokenType.Minus => leftInt - rightInt,
                        TokenType.Multiply => leftInt * rightInt,
                        TokenType.Divide => leftInt / rightInt,
                        TokenType.Modulo => leftInt % rightInt,
                        _ => throw new NotSupportedException()
                    };
                }
                
                if (left is double leftDouble && right is double rightDouble)
                {
                    return op.Type switch
                    {
                        TokenType.Plus => leftDouble + rightDouble,
                        TokenType.Minus => leftDouble - rightDouble,
                        TokenType.Multiply => leftDouble * rightDouble,
                        TokenType.Divide => leftDouble / rightDouble,
                        TokenType.Modulo => leftDouble % rightDouble,
                        _ => throw new NotSupportedException()
                    };
                }
                
                if (left is string leftStr && right is string rightStr && op.Type == TokenType.Plus)
                {
                    return leftStr + rightStr;
                }
                
                throw new NotSupportedException();
            }
        }
    }
    
    /// <summary>
    /// Dead code elimination pass
    /// </summary>
    public class DeadCodeEliminationPass : IOptimizationPass
    {
        public string Name => "Dead Code Elimination";
        
        public List<Statement> Apply(List<Statement> ast)
        {
            var visitor = new DeadCodeEliminationVisitor();
            var result = new List<Statement>();
            
            foreach (var stmt in ast)
            {
                var optimized = visitor.VisitStatement(stmt);
                if (optimized != null && !visitor.IsDeadCode(optimized))
                {
                    result.Add(optimized);
                }
            }
            
            return result;
        }
        
        public byte[] ApplyToBytecode(byte[] bytecode) => bytecode;
        
        private class DeadCodeEliminationVisitor : AstVisitor<AstNode>
        {
            private bool unreachableCode = false;
            
            public bool IsDeadCode(Statement stmt)
            {
                // Check for unreachable code after return/throw
                if (stmt is ReturnStatement || stmt is ThrowStatement)
                {
                    unreachableCode = true;
                }
                
                return unreachableCode && !(stmt is FunctionDeclaration || stmt is ClassDeclaration);
            }
            
            public override Statement VisitIfStatement(IfStatement stmt)
            {
                // Check for constant conditions
                if (stmt.Condition is LiteralExpression lit)
                {
                    if (lit.Value is bool boolValue)
                    {
                        if (boolValue)
                        {
                            // Always true, eliminate else branch
                            return Visit(stmt.ThenBranch) as Statement;
                        }
                        else if (stmt.ElseBranch != null)
                        {
                            // Always false, eliminate then branch
                            return Visit(stmt.ElseBranch) as Statement;
                        }
                        else
                        {
                            // Always false with no else, eliminate entire if
                            return null;
                        }
                    }
                }
                
                return base.VisitIfStatement(stmt);
            }
            
            public override Statement VisitWhileStatement(WhileStatement stmt)
            {
                // Check for constant false condition
                if (stmt.Condition is LiteralExpression { Value: false })
                {
                    // Loop never executes
                    return null;
                }
                
                return base.VisitWhileStatement(stmt);
            }
        }
    }
    
    /// <summary>
    /// Function inlining pass
    /// </summary>
    public class InliningPass : IOptimizationPass
    {
        public string Name => "Function Inlining";
        
        private readonly Dictionary<string, FunctionDeclaration> inlineCandidates;
        
        public InliningPass()
        {
            inlineCandidates = new Dictionary<string, FunctionDeclaration>();
        }
        
        public List<Statement> Apply(List<Statement> ast)
        {
            // First pass: collect inline candidates
            CollectInlineCandidates(ast);
            
            // Second pass: inline function calls
            var visitor = new InliningVisitor(inlineCandidates);
            return ast.Select(stmt => visitor.VisitStatement(stmt)).ToList();
        }
        
        public byte[] ApplyToBytecode(byte[] bytecode) => bytecode;
        
        private void CollectInlineCandidates(List<Statement> ast)
        {
            foreach (var stmt in ast)
            {
                if (stmt is FunctionDeclaration func && ShouldInline(func))
                {
                    inlineCandidates[func.Name] = func;
                }
            }
        }
        
        private bool ShouldInline(FunctionDeclaration func)
        {
            // Simple heuristics for inlining
            if (func.Parameters.Count > 3) return false;
            if (GetStatementCount(func.Body) > 5) return false;
            if (ContainsRecursion(func)) return false;
            // if (func.Attributes.Any(a => a.Name == "NoInline")) return false; // TODO: Add attributes support
            
            return true;
        }
        
        private int GetStatementCount(Statement stmt)
        {
            if (stmt is BlockStatement block)
                return block.Statements.Sum(GetStatementCount);
            return 1;
        }
        
        private bool ContainsRecursion(FunctionDeclaration func)
        {
            // Check if function calls itself directly or indirectly
            var recursionChecker = new RecursionChecker(func.Name);
            recursionChecker.Visit(func.Body);
            return recursionChecker.IsRecursive;
        }
        
        private class RecursionChecker : AstVisitor<AstNode>
        {
            private readonly string functionName;
            public bool IsRecursive { get; private set; }
            
            public RecursionChecker(string functionName)
            {
                this.functionName = functionName;
            }
            
            public override Expression VisitCallExpression(CallExpression expr)
            {
                if (expr.Callee is IdentifierExpression idExpr && idExpr.Name == functionName)
                {
                    IsRecursive = true;
                }
                return base.VisitCallExpression(expr);
            }
        }
        
        private class InliningVisitor : AstVisitor<AstNode>
        {
            private readonly Dictionary<string, FunctionDeclaration> inlineCandidates;
            private readonly Dictionary<string, Expression> parameterMap;
            
            public InliningVisitor(Dictionary<string, FunctionDeclaration> candidates)
            {
                inlineCandidates = candidates;
                parameterMap = new Dictionary<string, Expression>();
            }
            
            public override Expression VisitCallExpression(CallExpression expr)
            {
                if (expr.Callee is IdentifierExpression idExpr && 
                    inlineCandidates.TryGetValue(idExpr.Name, out var func))
                {
                    // Map parameters to arguments
                    parameterMap.Clear();
                    for (int i = 0; i < func.Parameters.Count; i++)
                    {
                        parameterMap[func.Parameters[i].Name] = expr.Arguments[i];
                    }
                    
                    // Inline the function body
                    return InlineFunction(func);
                }
                
                return base.VisitCallExpression(expr);
            }
            
            private Expression InlineFunction(FunctionDeclaration func)
            {
                // Clone the function body and replace parameter references
                var cloner = new ExpressionCloner(parameterMap);
                
                // Handle different return patterns
                if (func.Body is BlockStatement block)
                {
                    // Find return statements
                    var returnFinder = new ReturnStatementFinder();
                    var returns = returnFinder.FindReturns(block);
                    
                    if (returns.Count == 0)
                    {
                        // No return, function returns null/void
                        var nullToken = new Token(TokenType.NullLiteral, "null", null, func.Line, func.Column, 0, 0, "", SyntaxLevel.Medium);
                        return new LiteralExpression(nullToken);
                    }
                    else if (returns.Count == 1 && IsLastStatement(returns[0], block))
                    {
                        // Single return at end - inline the return expression
                        return cloner.Visit(returns[0].Value) as Expression;
                    }
                    else
                    {
                        // Multiple returns or complex control flow
                        // Create an immediately invoked function expression (IIFE)
                        return CreateIIFE(func, cloner);
                    }
                }
                else
                {
                    // Other cases - create IIFE
                    return CreateIIFE(func, cloner);
                }
                
                // Fallback
                var fallbackToken = new Token(TokenType.NullLiteral, "null", null, func.Line, func.Column, 0, 0, "", SyntaxLevel.Medium);
                return new LiteralExpression(fallbackToken);
            }
            
            private bool IsLastStatement(ReturnStatement ret, BlockStatement block)
            {
                if (block.Statements.Count == 0) return false;
                var last = block.Statements[block.Statements.Count - 1];
                return last == ret || (last is BlockStatement innerBlock && IsLastStatement(ret, innerBlock));
            }
            
            private Expression CreateIIFE(FunctionDeclaration func, ExpressionCloner cloner)
            {
                // Create a lambda expression that's immediately called
                var lambdaParams = func.Parameters.Select(p => new Parameter(
                    p.Type,
                    p.Name,
                    p.DefaultValue,
                    ParameterModifier.None
                )).ToList();
                var lambdaBody = cloner.Visit(func.Body) as Statement;
                var lambda = new LambdaExpression(lambdaParams, lambdaBody);
                
                // Create call with mapped arguments
                var args = func.Parameters.Select(p => parameterMap[p.Name]).ToList();
                return new CallExpression(lambda, args);
            }
        }
        
        private class ReturnStatementFinder : AstVisitor<AstNode>
        {
            public List<ReturnStatement> Returns { get; } = new List<ReturnStatement>();
            
            public List<ReturnStatement> FindReturns(Statement stmt)
            {
                Returns.Clear();
                Visit(stmt);
                return Returns;
            }
            
            public override Statement VisitReturnStatement(ReturnStatement stmt)
            {
                Returns.Add(stmt);
                return stmt;
            }
        }
        
        private class ExpressionCloner : AstVisitor<AstNode>
        {
            private readonly Dictionary<string, Expression> substitutions;
            
            public ExpressionCloner(Dictionary<string, Expression> substitutions)
            {
                this.substitutions = substitutions;
            }
            
            public override Expression VisitIdentifierExpression(IdentifierExpression expr)
            {
                if (substitutions.TryGetValue(expr.Name, out var replacement))
                {
                    // Clone the replacement to avoid sharing nodes
                    return Visit(replacement) as Expression;
                }
                return expr;
            }
            
            public override Expression VisitBinaryExpression(BinaryExpression expr)
            {
                var left = Visit(expr.Left) as Expression;
                var right = Visit(expr.Right) as Expression;
                return new BinaryExpression(left, expr.Operator, right);
            }
            
            public override Expression VisitUnaryExpression(UnaryExpression expr)
            {
                var operand = Visit(expr.Operand) as Expression;
                return new UnaryExpression(expr.Operator, operand, expr.IsPrefix);
            }
            
            public override Expression VisitCallExpression(CallExpression expr)
            {
                var callee = Visit(expr.Callee) as Expression;
                var args = expr.Arguments.Select(a => Visit(a) as Expression).ToList();
                return new CallExpression(callee, args);
            }
            
            public override Statement VisitBlockStatement(BlockStatement stmt)
            {
                var statements = stmt.Statements.Select(s => Visit(s) as Statement).ToList();
                return new BlockStatement(statements, stmt.Token);
            }
        }
    }
    
    /// <summary>
    /// Loop optimization pass
    /// </summary>
    public class LoopOptimizationPass : IOptimizationPass
    {
        public string Name => "Loop Optimization";
        
        public List<Statement> Apply(List<Statement> ast)
        {
            var visitor = new LoopOptimizationVisitor();
            return ast.Select(stmt => visitor.VisitStatement(stmt)).ToList();
        }
        
        public byte[] ApplyToBytecode(byte[] bytecode) => bytecode;
        
        private class LoopOptimizationVisitor : AstVisitor<AstNode>
        {
            public override Statement VisitForStatement(ForStatement stmt)
            {
                // Loop unrolling for small constant loops
                if (CanUnroll(stmt, out int iterations) && iterations <= 4)
                {
                    return UnrollLoop(stmt, iterations);
                }
                
                // Loop invariant code motion
                var invariants = FindLoopInvariants(stmt);
                if (invariants.Any())
                {
                    return HoistInvariants(stmt, invariants);
                }
                
                return base.VisitForStatement(stmt);
            }
            
            private bool CanUnroll(ForStatement stmt, out int iterations)
            {
                iterations = 0;
                
                // Check for simple counting loop: for (int i = 0; i < N; i++)
                if (stmt.Initializer is VariableDeclaration init &&
                    init.Initializer is LiteralExpression { Value: 0 } &&
                    stmt.Condition is BinaryExpression cond &&
                    cond.Operator.Type == TokenType.Less &&
                    cond.Left is IdentifierExpression { Name: var iterVar } &&
                    iterVar == init.Name &&
                    cond.Right is LiteralExpression { Value: int limit } &&
                    stmt.Update is UnaryExpression update &&
                    update.Operator.Type == TokenType.Increment)
                {
                    iterations = limit;
                    return true;
                }
                
                return false;
            }
            
            private Statement UnrollLoop(ForStatement stmt, int iterations)
            {
                var unrolled = new List<Statement>();
                
                // Add initializer
                if (stmt.Initializer != null)
                    unrolled.Add(stmt.Initializer);
                
                // Unroll loop body
                for (int i = 0; i < iterations; i++)
                {
                    // Clone and adjust loop body for each iteration
                    unrolled.Add(stmt.Body);
                }
                
                return new BlockStatement(unrolled, stmt.Token);
            }
            
            private List<Statement> FindLoopInvariants(ForStatement stmt)
            {
                // Find statements that don't depend on loop variables
                var invariants = new List<Statement>();
                var loopVars = new HashSet<string>();
                
                // Identify loop variables
                if (stmt.Initializer is VariableDeclaration init)
                {
                    loopVars.Add(init.Name);
                }
                
                // Check for variables modified in the loop
                var modifiedVars = FindModifiedVariables(stmt.Body);
                loopVars.UnionWith(modifiedVars);
                
                // Find statements that don't depend on or modify loop variables
                if (stmt.Body is BlockStatement block)
                {
                    foreach (var s in block.Statements)
                    {
                        if (IsLoopInvariant(s, loopVars))
                        {
                            invariants.Add(s);
                        }
                    }
                }
                
                return invariants;
            }
            
            private HashSet<string> FindModifiedVariables(Statement stmt)
            {
                var modified = new HashSet<string>();
                
                switch (stmt)
                {
                    case ExpressionStatement exprStmt:
                        if (exprStmt.Expression is AssignmentExpression assign)
                        {
                            if (assign.Target is IdentifierExpression idExpr)
                            {
                                modified.Add(idExpr.Name);
                            }
                        }
                        break;
                        
                    case VariableDeclaration varDecl:
                        modified.Add(varDecl.Name);
                        break;
                        
                    case BlockStatement block:
                        foreach (var s in block.Statements)
                        {
                            modified.UnionWith(FindModifiedVariables(s));
                        }
                        break;
                        
                    case IfStatement ifStmt:
                        modified.UnionWith(FindModifiedVariables(ifStmt.ThenBranch));
                        if (ifStmt.ElseBranch != null)
                        {
                            modified.UnionWith(FindModifiedVariables(ifStmt.ElseBranch));
                        }
                        break;
                        
                    case WhileStatement whileStmt:
                        modified.UnionWith(FindModifiedVariables(whileStmt.Body));
                        break;
                        
                    case ForStatement forStmt:
                        modified.UnionWith(FindModifiedVariables(forStmt.Body));
                        break;
                }
                
                return modified;
            }
            
            private bool IsLoopInvariant(Statement stmt, HashSet<string> loopVars)
            {
                // Check if statement references or modifies loop variables
                var referenced = FindReferencedVariables(stmt);
                var modified = FindModifiedVariables(stmt);
                
                // Statement is invariant if it doesn't reference or modify loop variables
                return !referenced.Overlaps(loopVars) && !modified.Overlaps(loopVars);
            }
            
            private HashSet<string> FindReferencedVariables(Statement stmt)
            {
                var referenced = new HashSet<string>();
                
                // Extract referenced variables from expressions
                void ExtractFromExpression(Expression expr)
                {
                    switch (expr)
                    {
                        case IdentifierExpression idExpr:
                            referenced.Add(idExpr.Name);
                            break;
                            
                        case BinaryExpression binExpr:
                            ExtractFromExpression(binExpr.Left);
                            ExtractFromExpression(binExpr.Right);
                            break;
                            
                        case UnaryExpression unExpr:
                            ExtractFromExpression(unExpr.Operand);
                            break;
                            
                        case CallExpression callExpr:
                            ExtractFromExpression(callExpr.Callee);
                            foreach (var arg in callExpr.Arguments)
                            {
                                ExtractFromExpression(arg);
                            }
                            break;
                            
                        case AssignmentExpression assignExpr:
                            ExtractFromExpression(assignExpr.Value);
                            break;
                    }
                }
                
                // Extract from statement
                switch (stmt)
                {
                    case ExpressionStatement exprStmt:
                        ExtractFromExpression(exprStmt.Expression);
                        break;
                        
                    case VariableDeclaration varDecl:
                        if (varDecl.Initializer != null)
                        {
                            ExtractFromExpression(varDecl.Initializer);
                        }
                        break;
                        
                    case ReturnStatement retStmt:
                        if (retStmt.Value != null)
                        {
                            ExtractFromExpression(retStmt.Value);
                        }
                        break;
                        
                    case IfStatement ifStmt:
                        ExtractFromExpression(ifStmt.Condition);
                        break;
                        
                    case WhileStatement whileStmt:
                        ExtractFromExpression(whileStmt.Condition);
                        break;
                }
                
                return referenced;
            }
            
            private Statement HoistInvariants(ForStatement stmt, List<Statement> invariants)
            {
                // Move invariants before the loop
                return stmt;
            }
        }
    }
    
    /// <summary>
    /// Common subexpression elimination pass
    /// </summary>
    public class CommonSubexpressionEliminationPass : IOptimizationPass
    {
        public string Name => "Common Subexpression Elimination";
        
        public List<Statement> Apply(List<Statement> ast)
        {
            var visitor = new CseVisitor();
            return ast.Select(stmt => visitor.VisitStatement(stmt)).ToList();
        }
        
        public byte[] ApplyToBytecode(byte[] bytecode) => bytecode;
        
        private class CseVisitor : AstVisitor<AstNode>
        {
            private readonly Dictionary<string, Expression> expressions;
            private int tempVarCounter;
            
            public CseVisitor()
            {
                expressions = new Dictionary<string, Expression>();
                tempVarCounter = 0;
            }
            
            public override Expression VisitBinaryExpression(BinaryExpression expr)
            {
                var left = Visit(expr.Left) as Expression;
                var right = Visit(expr.Right) as Expression;
                
                // Create canonical representation
                string key = $"{GetExpressionKey(left)}_{expr.Operator.Type}_{GetExpressionKey(right)}";
                
                if (expressions.TryGetValue(key, out var existing))
                {
                    // Found common subexpression
                    return existing;
                }
                
                var newExpr = new BinaryExpression(left, expr.Operator, right);
                expressions[key] = newExpr;
                return newExpr;
            }
            
            private string GetExpressionKey(Expression expr)
            {
                return expr switch
                {
                    LiteralExpression lit => $"lit_{lit.Value}",
                    IdentifierExpression id => $"var_{id.Name}",
                    BinaryExpression bin => $"bin_{GetExpressionKey(bin.Left)}_{bin.Operator.Type}_{GetExpressionKey(bin.Right)}",
                    _ => $"expr_{expr.GetHashCode()}"
                };
            }
        }
    }
    
    /// <summary>
    /// Peephole optimization for bytecode
    /// </summary>
    public class PeepholeOptimizationPass : IOptimizationPass
    {
        public string Name => "Peephole Optimization";
        
        public List<Statement> Apply(List<Statement> ast) => ast;
        
        public byte[] ApplyToBytecode(byte[] bytecode)
        {
            var optimized = new List<byte>(bytecode);
            bool changed;
            
            do
            {
                changed = false;
                
                // Pattern: Push 0, Add -> Nop
                changed |= ReplacePattern(optimized, 
                    new byte[] { (byte)Opcode.Push, 0, (byte)Opcode.Add }, 
                    new byte[] { (byte)Opcode.Nop, (byte)Opcode.Nop, (byte)Opcode.Nop });
                
                // Pattern: Push x, Pop -> Nop Nop Nop
                changed |= ReplacePattern(optimized,
                    new byte[] { (byte)Opcode.Push, 0, (byte)Opcode.Pop },
                    new byte[] { (byte)Opcode.Nop, (byte)Opcode.Nop, (byte)Opcode.Nop },
                    matchValue: false);
                
                // Pattern: JMP next_instruction -> NOP
                changed |= OptimizeJumps(optimized);
                
                // Remove NOPs
                if (changed)
                {
                    optimized.RemoveAll(b => b == (byte)Opcode.Nop);
                }
                
            } while (changed);
            
            return optimized.ToArray();
        }
        
        private bool ReplacePattern(List<byte> bytecode, byte[] pattern, byte[] replacement, bool matchValue = true)
        {
            bool changed = false;
            
            for (int i = 0; i <= bytecode.Count - pattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (matchValue || j == 0)  // Only check opcode, not operands
                    {
                        if (bytecode[i + j] != pattern[j])
                        {
                            match = false;
                            break;
                        }
                    }
                }
                
                if (match)
                {
                    for (int j = 0; j < replacement.Length; j++)
                    {
                        bytecode[i + j] = replacement[j];
                    }
                    changed = true;
                }
            }
            
            return changed;
        }
        
        private bool OptimizeJumps(List<byte> bytecode)
        {
            // Optimize jumps to next instruction
            bool changed = false;
            
            for (int i = 0; i < bytecode.Count - 2; i++)
            {
                if (bytecode[i] == (byte)Opcode.Jump)
                {
                    int target = bytecode[i + 1];
                    if (target == i + 2)  // Jump to next instruction
                    {
                        bytecode[i] = (byte)Opcode.Nop;
                        bytecode[i + 1] = (byte)Opcode.Nop;
                        changed = true;
                    }
                }
            }
            
            return changed;
        }
    }
    
    /// <summary>
    /// Register allocation pass
    /// </summary>
    public class RegisterAllocationPass : IOptimizationPass
    {
        public string Name => "Register Allocation";
        
        public List<Statement> Apply(List<Statement> ast) => ast;
        
        public byte[] ApplyToBytecode(byte[] bytecode)
        {
            // Linear scan register allocation
            var allocator = new LinearScanAllocator();
            return allocator.Allocate(bytecode);
        }
        
        private class LinearScanAllocator
        {
            private const int NUM_REGISTERS = 16; // Typical register count
            private readonly bool[] registerUsed = new bool[NUM_REGISTERS];
            private readonly Dictionary<int, int> variableToRegister = new Dictionary<int, int>();
            private readonly List<LiveInterval> intervals = new List<LiveInterval>();
            
            public byte[] Allocate(byte[] bytecode)
            {
                // Phase 1: Build live intervals
                BuildLiveIntervals(bytecode);
                
                // Phase 2: Sort intervals by start point
                intervals.Sort((a, b) => a.Start.CompareTo(b.Start));
                
                // Phase 3: Allocate registers using linear scan
                AllocateRegisters();
                
                // Phase 4: Rewrite bytecode with register assignments
                return RewriteBytecode(bytecode);
            }
            
            private void BuildLiveIntervals(byte[] bytecode)
            {
                // Scan bytecode to determine variable lifetimes
                for (int i = 0; i < bytecode.Length; i++)
                {
                    var opcode = (Opcode)bytecode[i];
                    
                    switch (opcode)
                    {
                        case Opcode.StoreLocal:
                            if (i + 1 < bytecode.Length)
                            {
                                int varIndex = bytecode[++i];
                                RecordVariableUse(varIndex, i);
                            }
                            break;
                            
                        case Opcode.LoadLocal:
                            if (i + 1 < bytecode.Length)
                            {
                                int varIndex = bytecode[++i];
                                RecordVariableUse(varIndex, i);
                            }
                            break;
                    }
                }
            }
            
            private void RecordVariableUse(int varIndex, int position)
            {
                var interval = intervals.FirstOrDefault(i => i.VarIndex == varIndex);
                if (interval == null)
                {
                    interval = new LiveInterval { VarIndex = varIndex, Start = position, End = position };
                    intervals.Add(interval);
                }
                else
                {
                    interval.End = position; // Extend live range
                }
            }
            
            private void AllocateRegisters()
            {
                var active = new List<LiveInterval>();
                
                foreach (var interval in intervals)
                {
                    // Expire old intervals
                    active.RemoveAll(i => i.End < interval.Start);
                    
                    // Try to find a free register
                    int register = FindFreeRegister(active);
                    
                    if (register != -1)
                    {
                        // Assign register
                        interval.Register = register;
                        variableToRegister[interval.VarIndex] = register;
                        active.Add(interval);
                    }
                    else
                    {
                        // Spill - use memory location
                        interval.Register = -1; // Indicates spill
                    }
                }
            }
            
            private int FindFreeRegister(List<LiveInterval> active)
            {
                Array.Fill(registerUsed, false);
                
                foreach (var interval in active)
                {
                    if (interval.Register >= 0)
                    {
                        registerUsed[interval.Register] = true;
                    }
                }
                
                for (int i = 0; i < NUM_REGISTERS; i++)
                {
                    if (!registerUsed[i])
                    {
                        return i;
                    }
                }
                
                return -1; // No free registers
            }
            
            private byte[] RewriteBytecode(byte[] bytecode)
            {
                var result = new List<byte>();
                
                for (int i = 0; i < bytecode.Length; i++)
                {
                    var opcode = (Opcode)bytecode[i];
                    result.Add(bytecode[i]);
                    
                    switch (opcode)
                    {
                        case Opcode.StoreLocal:
                            if (i + 1 < bytecode.Length)
                            {
                                int varIndex = bytecode[++i];
                                if (variableToRegister.TryGetValue(varIndex, out int register))
                                {
                                    // Replace with register store
                                    result[result.Count - 1] = (byte)Opcode.StoreRegister;
                                    result.Add((byte)register);
                                }
                                else
                                {
                                    // Keep memory store
                                    result.Add((byte)varIndex);
                                }
                            }
                            break;
                            
                        case Opcode.LoadLocal:
                            if (i + 1 < bytecode.Length)
                            {
                                int varIndex = bytecode[++i];
                                if (variableToRegister.TryGetValue(varIndex, out int register))
                                {
                                    // Replace with register load
                                    result[result.Count - 1] = (byte)Opcode.LoadRegister;
                                    result.Add((byte)register);
                                }
                                else
                                {
                                    // Keep memory load
                                    result.Add((byte)varIndex);
                                }
                            }
                            break;
                            
                        default:
                            // Copy operands
                            while (i + 1 < bytecode.Length && !IsOpcode(bytecode[i + 1]))
                            {
                                result.Add(bytecode[++i]);
                            }
                            break;
                    }
                }
                
                return result.ToArray();
            }
            
            private bool IsOpcode(byte value)
            {
                return Enum.IsDefined(typeof(Opcode), value);
            }
            
            private class LiveInterval
            {
                public int VarIndex { get; set; }
                public int Start { get; set; }
                public int End { get; set; }
                public int Register { get; set; } = -1;
            }
        }
    }
    
    /// <summary>
    /// Base AST visitor for optimization passes
    /// </summary>
    public abstract class AstVisitor<T> where T : AstNode
    {
        public virtual T Visit(AstNode node)
        {
            if (node == null) return default(T);
            
            // Since we can't use the Accept pattern directly without implementing IAstVisitor,
            // we'll use type checking and casting
            if (node is Statement stmt)
                return (T)(object)VisitStatement(stmt);
            else if (node is Expression expr)
                return (T)(object)VisitExpression(expr);
            else
                return (T)(object)node;
        }
        
        public virtual Statement VisitStatement(Statement stmt)
        {
            return stmt switch
            {
                ExpressionStatement s => VisitExpressionStatement(s),
                VariableDeclaration s => VisitVariableDeclaration(s),
                FunctionDeclaration s => VisitFunctionDeclaration(s),
                ClassDeclaration s => VisitClassDeclaration(s),
                IfStatement s => VisitIfStatement(s),
                WhileStatement s => VisitWhileStatement(s),
                ForStatement s => VisitForStatement(s),
                ReturnStatement s => VisitReturnStatement(s),
                BlockStatement s => VisitBlockStatement(s),
                _ => stmt
            };
        }
        
        public virtual Expression VisitExpression(Expression expr)
        {
            return expr switch
            {
                BinaryExpression e => VisitBinaryExpression(e),
                UnaryExpression e => VisitUnaryExpression(e),
                LiteralExpression e => VisitLiteralExpression(e),
                IdentifierExpression e => VisitIdentifierExpression(e),
                AssignmentExpression e => VisitAssignmentExpression(e),
                CallExpression e => VisitCallExpression(e),
                _ => expr
            };
        }
        
        public virtual Statement VisitExpressionStatement(ExpressionStatement stmt)
        {
            var expr = Visit(stmt.Expression) as Expression;
            return new ExpressionStatement(expr);
        }
        
        public virtual Statement VisitVariableDeclaration(VariableDeclaration stmt)
        {
            var initializer = stmt.Initializer != null ? Visit(stmt.Initializer) as Expression : null;
            return new VariableDeclaration(stmt.Type, stmt.Token, initializer, stmt.IsConst, stmt.IsReadonly);
        }
        
        public virtual Statement VisitFunctionDeclaration(FunctionDeclaration stmt)
        {
            var body = Visit(stmt.Body) as BlockStatement;
            return new FunctionDeclaration(stmt.Token, stmt.ReturnType, stmt.Parameters, body, stmt.TypeParameters, stmt.IsAsync, stmt.Modifiers);
        }
        
        public virtual Statement VisitClassDeclaration(ClassDeclaration stmt)
        {
            return stmt;
        }
        
        public virtual Statement VisitIfStatement(IfStatement stmt)
        {
            var condition = Visit(stmt.Condition) as Expression;
            var thenBranch = Visit(stmt.ThenBranch) as Statement;
            var elseBranch = stmt.ElseBranch != null ? Visit(stmt.ElseBranch) as Statement : null;
            return new IfStatement(stmt.Token, condition, thenBranch, elseBranch);
        }
        
        public virtual Statement VisitWhileStatement(WhileStatement stmt)
        {
            var condition = Visit(stmt.Condition) as Expression;
            var body = Visit(stmt.Body) as Statement;
            return new WhileStatement(stmt.Token, condition, body);
        }
        
        public virtual Statement VisitForStatement(ForStatement stmt)
        {
            var initializer = stmt.Initializer != null ? Visit(stmt.Initializer) as Statement : null;
            var condition = stmt.Condition != null ? Visit(stmt.Condition) as Expression : null;
            var update = stmt.Update != null ? Visit(stmt.Update) as Expression : null;
            var body = Visit(stmt.Body) as Statement;
            return new ForStatement(stmt.Token, initializer, condition, update, body);
        }
        
        public virtual Statement VisitReturnStatement(ReturnStatement stmt)
        {
            var value = stmt.Value != null ? Visit(stmt.Value) as Expression : null;
            return new ReturnStatement(stmt.Token, value);
        }
        
        public virtual Statement VisitBlockStatement(BlockStatement stmt)
        {
            var statements = stmt.Statements.Select(s => Visit(s) as Statement).ToList();
            return new BlockStatement(statements, stmt.Token);
        }
        
        public virtual Expression VisitBinaryExpression(BinaryExpression expr)
        {
            var left = Visit(expr.Left) as Expression;
            var right = Visit(expr.Right) as Expression;
            return new BinaryExpression(left, expr.Operator, right);
        }
        
        public virtual Expression VisitUnaryExpression(UnaryExpression expr)
        {
            var operand = Visit(expr.Operand) as Expression;
            return new UnaryExpression(expr.Operator, operand, expr.IsPrefix);
        }
        
        public virtual Expression VisitLiteralExpression(LiteralExpression expr)
        {
            return expr;
        }
        
        public virtual Expression VisitIdentifierExpression(IdentifierExpression expr)
        {
            return expr;
        }
        
        public virtual Expression VisitAssignmentExpression(AssignmentExpression expr)
        {
            var target = Visit(expr.Target) as Expression;
            var value = Visit(expr.Value) as Expression;
            return new AssignmentExpression(target, expr.Operator, value);
        }
        
        public virtual Expression VisitCallExpression(CallExpression expr)
        {
            var callee = Visit(expr.Callee) as Expression;
            var arguments = expr.Arguments.Select(a => Visit(a) as Expression).ToList();
            return new CallExpression(callee, arguments, expr.IsAsync, expr.GenericTypeArguments);
        }
    }
} 
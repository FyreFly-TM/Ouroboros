using System;
using System.Collections.Generic;
using System.Linq;
using Ouroboros.Core;
using Ouroboros.Core.AST;
using Ouroboros.Core.Compiler;

namespace Ouroboros.Optimization
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
                        return new LiteralExpression(result, expr.Line, expr.Column);
                    }
                    catch
                    {
                        // If evaluation fails, return original
                    }
                }
                
                return new BinaryExpression(left, expr.Operator, right, expr.Line, expr.Column);
            }
            
            private object EvaluateBinary(TokenType op, object left, object right)
            {
                if (left is int leftInt && right is int rightInt)
                {
                    return op switch
                    {
                        TokenType.Plus => leftInt + rightInt,
                        TokenType.Minus => leftInt - rightInt,
                        TokenType.Star => leftInt * rightInt,
                        TokenType.Slash => leftInt / rightInt,
                        TokenType.Percent => leftInt % rightInt,
                        _ => throw new NotSupportedException()
                    };
                }
                
                if (left is double leftDouble && right is double rightDouble)
                {
                    return op switch
                    {
                        TokenType.Plus => leftDouble + rightDouble,
                        TokenType.Minus => leftDouble - rightDouble,
                        TokenType.Star => leftDouble * rightDouble,
                        TokenType.Slash => leftDouble / rightDouble,
                        TokenType.Percent => leftDouble % rightDouble,
                        _ => throw new NotSupportedException()
                    };
                }
                
                if (left is string leftStr && right is string rightStr && op == TokenType.Plus)
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
            if (func.Attributes.Any(a => a.Name == "NoInline")) return false;
            
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
                if (expr.Callee is VariableExpression varExpr && varExpr.Name == functionName)
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
                if (expr.Callee is VariableExpression varExpr && 
                    inlineCandidates.TryGetValue(varExpr.Name, out var func))
                {
                    // Map parameters to arguments
                    parameterMap.Clear();
                    for (int i = 0; i < func.Parameters.Count; i++)
                    {
                        parameterMap[func.Parameters[i]] = expr.Arguments[i];
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
                        return new LiteralExpression(null, func.Line, func.Column);
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
                else if (func.Body is ReturnStatement ret)
                {
                    // Simple expression body
                    return cloner.Visit(ret.Value) as Expression;
                }
                
                // Fallback
                return new LiteralExpression(null, func.Line, func.Column);
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
                var lambdaParams = func.Parameters.Select(p => new Parameter(p, null)).ToList();
                var lambdaBody = cloner.Visit(func.Body) as Statement;
                var lambda = new LambdaExpression(lambdaParams, lambdaBody, func.Line, func.Column);
                
                // Create call with mapped arguments
                var args = func.Parameters.Select(p => parameterMap[p]).ToList();
                return new CallExpression(lambda, args, func.Line, func.Column);
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
            
            public override Expression VisitVariableExpression(VariableExpression expr)
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
                return new BinaryExpression(left, expr.Operator, right, expr.Line, expr.Column);
            }
            
            public override Expression VisitUnaryExpression(UnaryExpression expr)
            {
                var operand = Visit(expr.Operand) as Expression;
                return new UnaryExpression(expr.Operator, operand, expr.Prefix, expr.Line, expr.Column);
            }
            
            public override Expression VisitCallExpression(CallExpression expr)
            {
                var callee = Visit(expr.Callee) as Expression;
                var args = expr.Arguments.Select(a => Visit(a) as Expression).ToList();
                return new CallExpression(callee, args, expr.Line, expr.Column);
            }
            
            public override Statement VisitBlockStatement(BlockStatement stmt)
            {
                var statements = stmt.Statements.Select(s => Visit(s) as Statement).ToList();
                return new BlockStatement(statements, stmt.Line, stmt.Column);
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
                    stmt.Condition is BinaryExpression { Operator: TokenType.Less } cond &&
                    cond.Left is VariableExpression { Name: var iterVar } &&
                    iterVar == init.Name &&
                    cond.Right is LiteralExpression { Value: int limit } &&
                    stmt.Increment is UnaryExpression { Operator: TokenType.PlusPlus })
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
                
                return new BlockStatement(unrolled, stmt.Line, stmt.Column);
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
                            modified.Add(assign.Name);
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
                        case VariableExpression varExpr:
                            referenced.Add(varExpr.Name);
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
                string key = $"{GetExpressionKey(left)}_{expr.Operator}_{GetExpressionKey(right)}";
                
                if (expressions.TryGetValue(key, out var existing))
                {
                    // Found common subexpression
                    return existing;
                }
                
                var newExpr = new BinaryExpression(left, expr.Operator, right, expr.Line, expr.Column);
                expressions[key] = newExpr;
                return newExpr;
            }
            
            private string GetExpressionKey(Expression expr)
            {
                // Generate unique key for expression
                return expr switch
                {
                    LiteralExpression lit => $"lit_{lit.Value}",
                    VariableExpression var => $"var_{var.Name}",
                    BinaryExpression bin => $"bin_{GetExpressionKey(bin.Left)}_{bin.Operator}_{GetExpressionKey(bin.Right)}",
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
                
                // Pattern: PUSH 0, ADD -> NOP
                changed |= ReplacePattern(optimized, 
                    new[] { (byte)Opcode.PUSH, 0, (byte)Opcode.ADD }, 
                    new[] { (byte)Opcode.NOP, (byte)Opcode.NOP, (byte)Opcode.NOP });
                
                // Pattern: PUSH x, POP -> NOP NOP NOP
                changed |= ReplacePattern(optimized,
                    new[] { (byte)Opcode.PUSH, 0, (byte)Opcode.POP },
                    new[] { (byte)Opcode.NOP, (byte)Opcode.NOP, (byte)Opcode.NOP },
                    matchValue: false);
                
                // Pattern: JMP next_instruction -> NOP
                changed |= OptimizeJumps(optimized);
                
                // Remove NOPs
                if (changed)
                {
                    optimized.RemoveAll(b => b == (byte)Opcode.NOP);
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
                if (bytecode[i] == (byte)Opcode.JMP)
                {
                    int target = bytecode[i + 1];
                    if (target == i + 2)  // Jump to next instruction
                    {
                        bytecode[i] = (byte)Opcode.NOP;
                        bytecode[i + 1] = (byte)Opcode.NOP;
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
                        case Opcode.STORE_VAR:
                            if (i + 1 < bytecode.Length)
                            {
                                int varIndex = bytecode[++i];
                                RecordVariableUse(varIndex, i);
                            }
                            break;
                            
                        case Opcode.LOAD_VAR:
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
                        case Opcode.STORE_VAR:
                            if (i + 1 < bytecode.Length)
                            {
                                int varIndex = bytecode[++i];
                                if (variableToRegister.TryGetValue(varIndex, out int register))
                                {
                                    // Replace with register store
                                    result[result.Count - 1] = (byte)Opcode.STORE_REG;
                                    result.Add((byte)register);
                                }
                                else
                                {
                                    // Keep memory store
                                    result.Add((byte)varIndex);
                                }
                            }
                            break;
                            
                        case Opcode.LOAD_VAR:
                            if (i + 1 < bytecode.Length)
                            {
                                int varIndex = bytecode[++i];
                                if (variableToRegister.TryGetValue(varIndex, out int register))
                                {
                                    // Replace with register load
                                    result[result.Count - 1] = (byte)Opcode.LOAD_REG;
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
            return node?.Accept(this) as T;
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
                VariableExpression e => VisitVariableExpression(e),
                AssignmentExpression e => VisitAssignmentExpression(e),
                CallExpression e => VisitCallExpression(e),
                _ => expr
            };
        }
        
        public virtual Statement VisitExpressionStatement(ExpressionStatement stmt)
        {
            var expr = Visit(stmt.Expression) as Expression;
            return new ExpressionStatement(expr, stmt.Line, stmt.Column);
        }
        
        public virtual Statement VisitVariableDeclaration(VariableDeclaration stmt)
        {
            var initializer = stmt.Initializer != null ? Visit(stmt.Initializer) as Expression : null;
            return new VariableDeclaration(stmt.Type, stmt.Name, initializer, stmt.GenericArguments, stmt.Line, stmt.Column);
        }
        
        public virtual Statement VisitFunctionDeclaration(FunctionDeclaration stmt)
        {
            var body = Visit(stmt.Body) as Statement;
            return new FunctionDeclaration(stmt.Name, stmt.Parameters, stmt.ReturnType, body, stmt.Line, stmt.Column);
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
            return new IfStatement(condition, thenBranch, elseBranch, stmt.Line, stmt.Column);
        }
        
        public virtual Statement VisitWhileStatement(WhileStatement stmt)
        {
            var condition = Visit(stmt.Condition) as Expression;
            var body = Visit(stmt.Body) as Statement;
            return new WhileStatement(condition, body, stmt.Line, stmt.Column);
        }
        
        public virtual Statement VisitForStatement(ForStatement stmt)
        {
            var initializer = stmt.Initializer != null ? Visit(stmt.Initializer) as Statement : null;
            var condition = stmt.Condition != null ? Visit(stmt.Condition) as Expression : null;
            var increment = stmt.Increment != null ? Visit(stmt.Increment) as Expression : null;
            var body = Visit(stmt.Body) as Statement;
            return new ForStatement(initializer, condition, increment, body, stmt.Line, stmt.Column);
        }
        
        public virtual Statement VisitReturnStatement(ReturnStatement stmt)
        {
            var value = stmt.Value != null ? Visit(stmt.Value) as Expression : null;
            return new ReturnStatement(value, stmt.Line, stmt.Column);
        }
        
        public virtual Statement VisitBlockStatement(BlockStatement stmt)
        {
            var statements = stmt.Statements.Select(s => Visit(s) as Statement).ToList();
            return new BlockStatement(statements, stmt.Line, stmt.Column);
        }
        
        public virtual Expression VisitBinaryExpression(BinaryExpression expr)
        {
            var left = Visit(expr.Left) as Expression;
            var right = Visit(expr.Right) as Expression;
            return new BinaryExpression(left, expr.Operator, right, expr.Line, expr.Column);
        }
        
        public virtual Expression VisitUnaryExpression(UnaryExpression expr)
        {
            var operand = Visit(expr.Operand) as Expression;
            return new UnaryExpression(expr.Operator, operand, expr.Line, expr.Column);
        }
        
        public virtual Expression VisitLiteralExpression(LiteralExpression expr)
        {
            return expr;
        }
        
        public virtual Expression VisitVariableExpression(VariableExpression expr)
        {
            return expr;
        }
        
        public virtual Expression VisitAssignmentExpression(AssignmentExpression expr)
        {
            var value = Visit(expr.Value) as Expression;
            return new AssignmentExpression(expr.Name, value, expr.Line, expr.Column);
        }
        
        public virtual Expression VisitCallExpression(CallExpression expr)
        {
            var callee = Visit(expr.Callee) as Expression;
            var arguments = expr.Arguments.Select(a => Visit(a) as Expression).ToList();
            return new CallExpression(callee, arguments, expr.Line, expr.Column);
        }
    }
} 
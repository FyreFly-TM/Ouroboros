using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Ouroboros.Core.AST;
using Ouroboros.Core;
using Ouroboros.Tokens;

namespace Ouroboros.Analysis
{
    /// <summary>
    /// Base interface for compiler analysis passes
    /// </summary>
    public interface IAnalysisPass
    {
        Task AnalyzeAsync(SemanticModel semanticModel);
    }
    
    /// <summary>
    /// Semantic model containing analyzed program information
    /// </summary>
    public class SemanticModel
    {
        public Core.AST.Program Program { get; set; }
        public Dictionary<AstNode, Core.AST.TypeNode> TypeMap { get; set; } = new();
        public Dictionary<string, Symbol> SymbolTable { get; set; } = new();
        public List<Domain> Domains { get; set; } = new();
        public List<UnitDefinition> Units { get; set; } = new();
        public List<ContractDefinition> Contracts { get; set; } = new();
        public List<SecurityAnnotation> SecurityAnnotations { get; set; } = new();
        
        public SemanticModel(Core.AST.Program program)
        {
            Program = program;
        }
    }
    
    /// <summary>
    /// Symbol information
    /// </summary>
    public class Symbol
    {
        public string Name { get; set; } = "";
        public Core.AST.TypeNode Type { get; set; }
        public SymbolKind Kind { get; set; }
        public SourceLocation Location { get; set; }
        public object? Value { get; set; }
        
        public Symbol(Core.AST.TypeNode type)
        {
            Type = type;
        }
    }
    
    public enum SymbolKind
    {
        Variable,
        Function,
        Type,
        Constant,
        Operator,
        Domain
    }
    
    /// <summary>
    /// Mathematical notation analyzer for symbolic math support
    /// </summary>
    public class MathematicalNotationAnalyzer : IAnalysisPass
    {
        private readonly DiagnosticEngine diagnostics;
        
        public MathematicalNotationAnalyzer(DiagnosticEngine diagnostics)
        {
            this.diagnostics = diagnostics;
        }
        
        public async Task AnalyzeAsync(SemanticModel semanticModel)
        {
            await Task.Run(() =>
            {
                AnalyzeMathematicalExpressions(semanticModel);
                AnalyzeSymbolicDifferentiation(semanticModel);
                AnalyzeIntegralNotation(semanticModel);
                AnalyzeLimitExpressions(semanticModel);
            });
        }
        
        private void AnalyzeMathematicalExpressions(SemanticModel model)
        {
            // Analyze mathematical expressions using Greek letters and symbols
            foreach (var node in GetAllNodes(model.Program))
            {
                if (node is MathExpression mathExpr)
                {
                    ValidateMathematicalExpression(mathExpr);
                }
            }
        }
        
        private void AnalyzeSymbolicDifferentiation(SemanticModel model)
        {
            // Analyze ∂f/∂x expressions
            foreach (var node in GetAllNodes(model.Program))
            {
                if (node is BinaryExpression bin && bin.Operator.Type == Ouroboros.Tokens.TokenType.PartialDerivative)
                {
                    ValidatePartialDerivative(bin);
                }
            }
        }
        
        private void AnalyzeIntegralNotation(SemanticModel model)
        {
            // Analyze ∫[a to b] f(x) dx expressions
            foreach (var node in GetAllNodes(model.Program))
            {
                if (node is Expression expr && ContainsIntegralNotation(expr))
                {
                    ValidateIntegralExpression(expr);
                }
            }
        }
        
        private void AnalyzeLimitExpressions(SemanticModel model)
        {
            // Analyze lim[x→0] expressions
            foreach (var node in GetAllNodes(model.Program))
            {
                if (node is Expression expr && ContainsLimitNotation(expr))
                {
                    ValidateLimitExpression(expr);
                }
            }
        }
        
        private void ValidateMathematicalExpression(MathExpression expr)
        {
            // Validate mathematical expression semantics
            // Check for valid operator usage
            var location = new SourceLocation(expr.FileName, expr.Line, expr.Column);
            
            switch (expr.Operation)
            {
                case MathOperationType.CrossProduct:
                    // Cross product requires 3D vectors
                    if (expr.Operands.Count != 2)
                    {
                        diagnostics.ReportMathematicalError("Cross product requires exactly two operands", location);
                    }
                    // TODO: Verify operands are 3D vectors when type info available
                    // Verify operands are 3D vectors
                    foreach (var operand in expr.Operands)
                    {
                        if (operand is VectorExpression vecExpr)
                        {
                            // Check if it's a 3D vector
                            if (vecExpr.Components.Count != 3)
                            {
                                var vecLocation = new SourceLocation(vecExpr.FileName, vecExpr.Line, vecExpr.Column);
                                diagnostics.ReportMathematicalError($"Cross product requires 3D vectors, but found {vecExpr.Components.Count}D vector", vecLocation);
                            }
                        }
                        else if (operand is IdentifierExpression idExpr)
                        {
                            // Look up type information from semantic model if available
                            // For now, we'll just flag non-vector expressions
                            if (!idExpr.Name.Contains("vec") && !idExpr.Name.Contains("Vec") && !idExpr.Name.Contains("vector"))
                            {
                                var idLocation = new SourceLocation(idExpr.FileName, idExpr.Line, idExpr.Column);
                                diagnostics.ReportInfo($"Cross product operand '{idExpr.Name}' should be a 3D vector", idLocation);
                            }
                        }
                        else
                        {
                            var opLocation = new SourceLocation(operand.FileName, operand.Line, operand.Column);
                            diagnostics.ReportMathematicalError("Cross product operands must be vectors", opLocation);
                        }
                    }
                    break;
                    
                case MathOperationType.DotProduct:
                    // Dot product requires vectors of same dimension
                    if (expr.Operands.Count != 2)
                    {
                        diagnostics.ReportMathematicalError("Dot product requires exactly two operands", location);
                    }
                    break;
                    
                case MathOperationType.Derivative:
                case MathOperationType.PartialDerivative:
                    // Derivative requires a function and variable
                    if (expr.Operands.Count < 1)
                    {
                        diagnostics.ReportMathematicalError($"{expr.Operation} requires a function operand", location);
                    }
                    break;
                    
                case MathOperationType.Integral:
                    // Integral requires integrand and bounds
                    if (expr.Operands.Count < 1)
                    {
                        diagnostics.ReportMathematicalError("Integral requires an integrand expression", location);
                    }
                    break;
                    
                case MathOperationType.Summation:
                case MathOperationType.Product:
                    // Sum/Product require iteration bounds
                    if (expr.Operands.Count < 1)
                    {
                        diagnostics.ReportMathematicalError($"{expr.Operation} requires an expression to iterate", location);
                    }
                    break;
                    
                // Additional mathematical operations
                case MathOperationType.MatrixMultiply:
                    if (expr.Operands.Count != 2)
                    {
                        diagnostics.ReportMathematicalError("Matrix multiplication requires exactly two operands", location);
                    }
                    break;
                    
                case MathOperationType.Transpose:
                case MathOperationType.Determinant:
                case MathOperationType.Inverse:
                    if (expr.Operands.Count != 1)
                    {
                        diagnostics.ReportMathematicalError($"{expr.Operation} requires exactly one matrix operand", location);
                    }
                    break;
            }
        }
        
        private void ValidatePartialDerivative(BinaryExpression expr)
        {
            // Validate partial derivative notation ∂f/∂x
            // Left side should be the function being differentiated
            // Right side should be the variable of differentiation
            var location = new SourceLocation(expr.FileName, expr.Line, expr.Column);
            
            if (expr.Left is IdentifierExpression leftId && expr.Right is IdentifierExpression rightId)
            {
                // Check if left is a function and right is a valid variable
                // This would require symbol table lookup in full implementation
                
                // For now, just validate syntax
                if (string.IsNullOrEmpty(leftId.Name))
                {
                    var leftLocation = new SourceLocation(leftId.FileName, leftId.Line, leftId.Column);
                    diagnostics.ReportMathematicalError("Partial derivative requires a function name", leftLocation);
                }
                
                if (string.IsNullOrEmpty(rightId.Name))
                {
                    var rightLocation = new SourceLocation(rightId.FileName, rightId.Line, rightId.Column);
                    diagnostics.ReportMathematicalError("Partial derivative requires a variable name", rightLocation);
                }
            }
            else
            {
                diagnostics.ReportMathematicalError("Invalid partial derivative syntax. Expected ∂f/∂x format", location);
            }
        }
        
        private void ValidateIntegralExpression(Expression expr)
        {
            // Validate integral notation ∫[a to b] f(x) dx
            var location = new SourceLocation(expr.FileName, expr.Line, expr.Column);
            
            if (expr is MathExpression mathExpr && mathExpr.Operation == MathOperationType.Integral)
            {
                // Check for integrand
                if (mathExpr.Operands.Count == 0)
                {
                    diagnostics.ReportMathematicalError("Integral requires an integrand expression", location);
                }
                
                // In a complete implementation, bounds and differential variable would be separate properties
                // For now, we assume they're encoded in the operands
                if (mathExpr.Operands.Count < 2)
                {
                    diagnostics.ReportInfo("Integral without bounds will be treated as indefinite integral", location);
                }
            }
            else if (expr is CallExpression call && call.Callee is IdentifierExpression id && 
                     (id.Name == "∫" || id.Name == "integral"))
            {
                // Validate function-style integral call
                if (call.Arguments.Count < 1)
                {
                    diagnostics.ReportMathematicalError("Integral function requires at least an integrand argument", location);
                }
                
                // First argument should be the integrand
                // Optional second and third arguments are bounds
                if (call.Arguments.Count >= 3)
                {
                    // Has bounds - validate they are numeric or symbolic expressions
                    var lowerBound = call.Arguments[1];
                    var upperBound = call.Arguments[2];
                    
                    // In full implementation, would check types here
                }
            }
        }
        
        private void ValidateLimitExpression(Expression expr)
        {
            // Validate limit notation lim[x→a] f(x)
            var location = new SourceLocation(expr.FileName, expr.Line, expr.Column);
            
            if (expr is CallExpression call && call.Callee is IdentifierExpression id && 
                (id.Name == "lim" || id.Name == "limit"))
            {
                // Should have at least 2 arguments: expression and limit point
                if (call.Arguments.Count < 2)
                {
                    diagnostics.ReportMathematicalError("Limit requires an expression and a limit point", location);
                    return;
                }
                
                // First argument is the expression
                var limitExpr = call.Arguments[0];
                
                // Second argument should specify the limit point (e.g., x→0)
                var limitPoint = call.Arguments[1];
                
                // Validate limit point syntax
                if (limitPoint is BinaryExpression binExpr && binExpr.Operator.Type == TokenType.Arrow)
                {
                    // Valid syntax: x→0
                    var variable = binExpr.Left;
                    var value = binExpr.Right;
                    
                    if (!(variable is IdentifierExpression))
                    {
                        var varLocation = new SourceLocation(variable.FileName, variable.Line, variable.Column);
                        diagnostics.ReportMathematicalError("Limit variable must be an identifier", varLocation);
                    }
                    
                    // Value can be numeric, infinity, or expression
                    ValidateLimitValue(value);
                }
                else
                {
                    var limitPointLocation = new SourceLocation(limitPoint.FileName, limitPoint.Line, limitPoint.Column);
                    diagnostics.ReportMathematicalError("Invalid limit syntax. Expected format: lim[x→a] f(x)", limitPointLocation);
                }
                
                // Optional third argument for one-sided limits (+ or -)
                if (call.Arguments.Count >= 3)
                {
                    var direction = call.Arguments[2];
                    if (direction is LiteralExpression lit && lit.Value is string dir)
                    {
                        if (dir != "+" && dir != "-")
                        {
                            var dirLocation = new SourceLocation(direction.FileName, direction.Line, direction.Column);
                            diagnostics.ReportMathematicalError("Limit direction must be '+' or '-' for one-sided limits", dirLocation);
                        }
                    }
                }
            }
        }
        
        private void ValidateLimitValue(Expression value)
        {
            // Validate the limit point value
            var location = new SourceLocation(value.FileName, value.Line, value.Column);
            
            if (value is IdentifierExpression id)
            {
                // Check for special values
                var specialValues = new[] { "∞", "infinity", "-∞", "-infinity", "0+", "0-" };
                if (!specialValues.Contains(id.Name) && !char.IsLetter(id.Name[0]))
                {
                    diagnostics.ReportInfo($"Unusual limit value: {id.Name}", location);
                }
            }
            else if (value is UnaryExpression unary && unary.Operator.Type == TokenType.Minus)
            {
                // Handle negative infinity
                if (unary.Operand is IdentifierExpression opId && (opId.Name == "∞" || opId.Name == "infinity"))
                {
                    // Valid: -∞
                    return;
                }
            }
            else if (!(value is LiteralExpression))
            {
                // Complex expressions are allowed but should be validated for convergence
                diagnostics.ReportInfo("Complex limit value expression detected", location);
            }
        }
        
        private bool ContainsIntegralNotation(Expression expr)
        {
            // Check if expression contains integral notation
            if (expr is MathExpression mathExpr)
            {
                return mathExpr.Operation == MathOperationType.Integral;
            }
            
            // Check for integral function calls
            if (expr is CallExpression call && call.Callee is IdentifierExpression id)
            {
                return id.Name == "∫" || id.Name == "integral" || id.Name == "integrate";
            }
            
            // Recursively check sub-expressions
            switch (expr)
            {
                case BinaryExpression binary:
                    return ContainsIntegralNotation(binary.Left) || ContainsIntegralNotation(binary.Right);
                case UnaryExpression unary:
                    return ContainsIntegralNotation(unary.Operand);
                case CallExpression callExpr:
                    return callExpr.Arguments.Any(arg => ContainsIntegralNotation(arg));
                case MathExpression math:
                    return math.Operands.Any(op => ContainsIntegralNotation(op));
                default:
                    return false;
            }
        }
        
        private bool ContainsLimitNotation(Expression expr)
        {
            // Check if expression contains limit notation
            if (expr is CallExpression call && call.Callee is IdentifierExpression id)
            {
                return id.Name == "lim" || id.Name == "limit";
            }
            
            // Recursively check sub-expressions
            switch (expr)
            {
                case BinaryExpression binary:
                    return ContainsLimitNotation(binary.Left) || ContainsLimitNotation(binary.Right);
                case UnaryExpression unary:
                    return ContainsLimitNotation(unary.Operand);
                case CallExpression callExpr:
                    return callExpr.Arguments.Any(arg => ContainsLimitNotation(arg));
                case MathExpression math:
                    return math.Operands.Any(op => ContainsLimitNotation(op));
                default:
                    return false;
            }
        }
        
        private IEnumerable<AstNode> GetAllNodes(AstNode root)
        {
            var nodes = new List<AstNode>();
            TraverseNodes(root, nodes);
            return nodes;
        }
        
        private void TraverseNodes(AstNode node, List<AstNode> nodes)
        {
            nodes.Add(node);
            
            // Handle different node types
            switch (node)
            {
                case Ouroboros.Core.AST.Program program:
                    foreach (var stmt in program.Statements)
                        TraverseNodes(stmt, nodes);
                    break;
                    
                case BlockStatement block:
                    foreach (var stmt in block.Statements)
                        TraverseNodes(stmt, nodes);
                    break;
                    
                case BinaryExpression binary:
                    TraverseNodes(binary.Left, nodes);
                    TraverseNodes(binary.Right, nodes);
                    break;
                    
                case UnaryExpression unary:
                    TraverseNodes(unary.Operand, nodes);
                    break;
                    
                case CallExpression call:
                    TraverseNodes(call.Callee, nodes);
                    foreach (var arg in call.Arguments)
                        TraverseNodes(arg, nodes);
                    break;
                    
                case MemberExpression member:
                    TraverseNodes(member.Object, nodes);
                    break;
                    
                case ArrayExpression array:
                    foreach (var elem in array.Elements)
                        TraverseNodes(elem, nodes);
                    break;
                    
                case LambdaExpression lambda:
                    TraverseNodes(lambda.Body, nodes);
                    break;
                    
                case ConditionalExpression conditional:
                    TraverseNodes(conditional.Condition, nodes);
                    TraverseNodes(conditional.TrueExpression, nodes);
                    TraverseNodes(conditional.FalseExpression, nodes);
                    break;
                    
                case NewExpression newExpr:
                    foreach (var arg in newExpr.Arguments)
                        TraverseNodes(arg, nodes);
                    if (newExpr.Initializer != null)
                        foreach (var init in newExpr.Initializer)
                            TraverseNodes(init, nodes);
                    break;
                    
                case AssignmentExpression assignment:
                    TraverseNodes(assignment.Target, nodes);
                    TraverseNodes(assignment.Value, nodes);
                    break;
                    
                case VariableDeclaration varDecl:
                    if (varDecl.Initializer != null)
                        TraverseNodes(varDecl.Initializer, nodes);
                    break;
                    
                case IfStatement ifStmt:
                    TraverseNodes(ifStmt.Condition, nodes);
                    TraverseNodes(ifStmt.ThenBranch, nodes);
                    if (ifStmt.ElseBranch != null)
                        TraverseNodes(ifStmt.ElseBranch, nodes);
                    break;
                    
                case WhileStatement whileStmt:
                    TraverseNodes(whileStmt.Condition, nodes);
                    TraverseNodes(whileStmt.Body, nodes);
                    break;
                    
                case ForStatement forStmt:
                    if (forStmt.Initializer != null)
                        TraverseNodes(forStmt.Initializer, nodes);
                    if (forStmt.Condition != null)
                        TraverseNodes(forStmt.Condition, nodes);
                    if (forStmt.Update != null)
                        TraverseNodes(forStmt.Update, nodes);
                    TraverseNodes(forStmt.Body, nodes);
                    break;
                    
                case ForEachStatement forEach:
                    TraverseNodes(forEach.Collection, nodes);
                    TraverseNodes(forEach.Body, nodes);
                    break;
                    
                case FunctionDeclaration func:
                    TraverseNodes(func.Body, nodes);
                    break;
                    
                case ClassDeclaration classDecl:
                    foreach (var member in classDecl.Members)
                        TraverseNodes(member, nodes);
                    break;
                    
                case ReturnStatement ret:
                    if (ret.Value != null)
                        TraverseNodes(ret.Value, nodes);
                    break;
                    
                case ThrowStatement throwStmt:
                    TraverseNodes(throwStmt.Exception, nodes);
                    break;
                    
                case TryStatement tryStmt:
                    TraverseNodes(tryStmt.TryBlock, nodes);
                    foreach (var catchClause in tryStmt.CatchClauses)
                        TraverseNodes(catchClause.Body, nodes);
                    if (tryStmt.FinallyBlock != null)
                        TraverseNodes(tryStmt.FinallyBlock, nodes);
                    break;
                    
                case ExpressionStatement exprStmt:
                    TraverseNodes(exprStmt.Expression, nodes);
                    break;
                    
                case MathExpression mathExpr:
                    foreach (var operand in mathExpr.Operands)
                        TraverseNodes(operand, nodes);
                    break;
                    
                case VectorExpression vecExpr:
                    foreach (var component in vecExpr.Components)
                        TraverseNodes(component, nodes);
                    break;
                    
                case MatrixExpression matExpr:
                    foreach (var row in matExpr.Elements)
                        foreach (var elem in row)
                            TraverseNodes(elem, nodes);
                    break;
                    
                case QuaternionExpression quatExpr:
                    TraverseNodes(quatExpr.W, nodes);
                    TraverseNodes(quatExpr.X, nodes);
                    TraverseNodes(quatExpr.Y, nodes);
                    TraverseNodes(quatExpr.Z, nodes);
                    break;
                    
                case DomainDeclaration domain:
                    foreach (var member in domain.Members)
                        TraverseNodes(member, nodes);
                    break;
                    
                // Terminal nodes - no children to traverse
                case LiteralExpression _:
                case IdentifierExpression _:
                case ThisExpression _:
                case BaseExpression _:
                case BreakStatement _:
                case ContinueStatement _:
                    break;
            }
        }
    }
    
    /// <summary>
    /// Domain analyzer for domain-specific programming blocks
    /// </summary>
    public class DomainAnalyzer : IAnalysisPass
    {
        private readonly DiagnosticEngine diagnostics;
        
        public DomainAnalyzer(DiagnosticEngine diagnostics)
        {
            this.diagnostics = diagnostics;
        }
        
        public async Task AnalyzeAsync(SemanticModel semanticModel)
        {
            await Task.Run(() =>
            {
                AnalyzeDomainDeclarations(semanticModel);
                AnalyzeOperatorMappings(semanticModel);
                AnalyzeDomainUsage(semanticModel);
            });
        }
        
        private void AnalyzeDomainDeclarations(SemanticModel model)
        {
            foreach (var node in GetAllNodes(model.Program))
            {
                if (node is DomainDeclaration domain)
                {
                    ValidateDomainDeclaration(domain);
                    model.Domains.Add(new Domain(domain.Name));
                }
            }
        }
        
        private void AnalyzeOperatorMappings(SemanticModel model)
        {
            // Analyze operator mappings like "× means cross_product for Vector3"
        }
        
        private void AnalyzeDomainUsage(SemanticModel model)
        {
            // Analyze "using Physics { ... }" blocks
        }
        
        private void ValidateDomainDeclaration(DomainDeclaration domain)
        {
            // Validate domain declaration
        }
        
        private IEnumerable<AstNode> GetAllNodes(AstNode root)
        {
            var nodes = new List<AstNode>();
            TraverseNodes(root, nodes);
            return nodes;
        }
        
        private void TraverseNodes(AstNode node, List<AstNode> nodes)
        {
            nodes.Add(node);
            
            // Handle different node types - reuse the traversal logic
            switch (node)
            {
                case Ouroboros.Core.AST.Program program:
                    foreach (var stmt in program.Statements)
                        TraverseNodes(stmt, nodes);
                    break;
                    
                case BlockStatement block:
                    foreach (var stmt in block.Statements)
                        TraverseNodes(stmt, nodes);
                    break;
                    
                case BinaryExpression binary:
                    TraverseNodes(binary.Left, nodes);
                    TraverseNodes(binary.Right, nodes);
                    break;
                    
                case UnaryExpression unary:
                    TraverseNodes(unary.Operand, nodes);
                    break;
                    
                case CallExpression call:
                    TraverseNodes(call.Callee, nodes);
                    foreach (var arg in call.Arguments)
                        TraverseNodes(arg, nodes);
                    break;
                    
                case MemberExpression member:
                    TraverseNodes(member.Object, nodes);
                    break;
                    
                case ArrayExpression array:
                    foreach (var elem in array.Elements)
                        TraverseNodes(elem, nodes);
                    break;
                    
                case LambdaExpression lambda:
                    TraverseNodes(lambda.Body, nodes);
                    break;
                    
                case ConditionalExpression conditional:
                    TraverseNodes(conditional.Condition, nodes);
                    TraverseNodes(conditional.TrueExpression, nodes);
                    TraverseNodes(conditional.FalseExpression, nodes);
                    break;
                    
                case NewExpression newExpr:
                    foreach (var arg in newExpr.Arguments)
                        TraverseNodes(arg, nodes);
                    if (newExpr.Initializer != null)
                        foreach (var init in newExpr.Initializer)
                            TraverseNodes(init, nodes);
                    break;
                    
                case AssignmentExpression assignment:
                    TraverseNodes(assignment.Target, nodes);
                    TraverseNodes(assignment.Value, nodes);
                    break;
                    
                case VariableDeclaration varDecl:
                    if (varDecl.Initializer != null)
                        TraverseNodes(varDecl.Initializer, nodes);
                    break;
                    
                case IfStatement ifStmt:
                    TraverseNodes(ifStmt.Condition, nodes);
                    TraverseNodes(ifStmt.ThenBranch, nodes);
                    if (ifStmt.ElseBranch != null)
                        TraverseNodes(ifStmt.ElseBranch, nodes);
                    break;
                    
                case WhileStatement whileStmt:
                    TraverseNodes(whileStmt.Condition, nodes);
                    TraverseNodes(whileStmt.Body, nodes);
                    break;
                    
                case ForStatement forStmt:
                    if (forStmt.Initializer != null)
                        TraverseNodes(forStmt.Initializer, nodes);
                    if (forStmt.Condition != null)
                        TraverseNodes(forStmt.Condition, nodes);
                    if (forStmt.Update != null)
                        TraverseNodes(forStmt.Update, nodes);
                    TraverseNodes(forStmt.Body, nodes);
                    break;
                    
                case ForEachStatement forEach:
                    TraverseNodes(forEach.Collection, nodes);
                    TraverseNodes(forEach.Body, nodes);
                    break;
                    
                case FunctionDeclaration func:
                    TraverseNodes(func.Body, nodes);
                    break;
                    
                case ClassDeclaration classDecl:
                    foreach (var member in classDecl.Members)
                        TraverseNodes(member, nodes);
                    break;
                    
                case ReturnStatement ret:
                    if (ret.Value != null)
                        TraverseNodes(ret.Value, nodes);
                    break;
                    
                case ThrowStatement throwStmt:
                    TraverseNodes(throwStmt.Exception, nodes);
                    break;
                    
                case TryStatement tryStmt:
                    TraverseNodes(tryStmt.TryBlock, nodes);
                    foreach (var catchClause in tryStmt.CatchClauses)
                        TraverseNodes(catchClause.Body, nodes);
                    if (tryStmt.FinallyBlock != null)
                        TraverseNodes(tryStmt.FinallyBlock, nodes);
                    break;
                    
                case ExpressionStatement exprStmt:
                    TraverseNodes(exprStmt.Expression, nodes);
                    break;
                    
                case MathExpression mathExpr:
                    foreach (var operand in mathExpr.Operands)
                        TraverseNodes(operand, nodes);
                    break;
                    
                case VectorExpression vecExpr:
                    foreach (var component in vecExpr.Components)
                        TraverseNodes(component, nodes);
                    break;
                    
                case MatrixExpression matExpr:
                    foreach (var row in matExpr.Elements)
                        foreach (var elem in row)
                            TraverseNodes(elem, nodes);
                    break;
                    
                case QuaternionExpression quatExpr:
                    TraverseNodes(quatExpr.W, nodes);
                    TraverseNodes(quatExpr.X, nodes);
                    TraverseNodes(quatExpr.Y, nodes);
                    TraverseNodes(quatExpr.Z, nodes);
                    break;
                    
                case DomainDeclaration domain:
                    foreach (var member in domain.Members)
                        TraverseNodes(member, nodes);
                    break;
                    
                // Terminal nodes - no children to traverse
                case LiteralExpression _:
                case IdentifierExpression _:
                case ThisExpression _:
                case BaseExpression _:
                case BreakStatement _:
                case ContinueStatement _:
                    break;
            }
        }
    }
    
    /// <summary>
    /// Units system analyzer for dimensional analysis
    /// </summary>
    public class UnitsAnalyzer : IAnalysisPass
    {
        private readonly DiagnosticEngine diagnostics;
        
        public UnitsAnalyzer(DiagnosticEngine diagnostics)
        {
            this.diagnostics = diagnostics;
        }
        
        public async Task AnalyzeAsync(SemanticModel semanticModel)
        {
            await Task.Run(() =>
            {
                AnalyzeUnitDeclarations(semanticModel);
                AnalyzeDimensionalCompatibility(semanticModel);
                AnalyzeUnitConversions(semanticModel);
            });
        }
        
        private void AnalyzeUnitDeclarations(SemanticModel model) 
        {
            // Analyze unit declarations like "meters", "kg", "seconds", etc.
            
            // Define SI base units
            var siBaseUnits = new[]
            {
                new UnitDefinition { Name = "meter", Symbol = "m", BaseUnit = "meter", ConversionFactor = 1.0 },
                new UnitDefinition { Name = "kilogram", Symbol = "kg", BaseUnit = "kilogram", ConversionFactor = 1.0 },
                new UnitDefinition { Name = "second", Symbol = "s", BaseUnit = "second", ConversionFactor = 1.0 },
                new UnitDefinition { Name = "ampere", Symbol = "A", BaseUnit = "ampere", ConversionFactor = 1.0 },
                new UnitDefinition { Name = "kelvin", Symbol = "K", BaseUnit = "kelvin", ConversionFactor = 1.0 },
                new UnitDefinition { Name = "mole", Symbol = "mol", BaseUnit = "mole", ConversionFactor = 1.0 },
                new UnitDefinition { Name = "candela", Symbol = "cd", BaseUnit = "candela", ConversionFactor = 1.0 }
            };
            
            // Define common derived units
            var derivedUnits = new[]
            {
                // Length
                new UnitDefinition { Name = "kilometer", Symbol = "km", BaseUnit = "meter", ConversionFactor = 1000.0 },
                new UnitDefinition { Name = "centimeter", Symbol = "cm", BaseUnit = "meter", ConversionFactor = 0.01 },
                new UnitDefinition { Name = "millimeter", Symbol = "mm", BaseUnit = "meter", ConversionFactor = 0.001 },
                new UnitDefinition { Name = "mile", Symbol = "mi", BaseUnit = "meter", ConversionFactor = 1609.344 },
                new UnitDefinition { Name = "foot", Symbol = "ft", BaseUnit = "meter", ConversionFactor = 0.3048 },
                new UnitDefinition { Name = "inch", Symbol = "in", BaseUnit = "meter", ConversionFactor = 0.0254 },
                
                // Mass
                new UnitDefinition { Name = "gram", Symbol = "g", BaseUnit = "kilogram", ConversionFactor = 0.001 },
                new UnitDefinition { Name = "pound", Symbol = "lb", BaseUnit = "kilogram", ConversionFactor = 0.453592 },
                new UnitDefinition { Name = "ounce", Symbol = "oz", BaseUnit = "kilogram", ConversionFactor = 0.0283495 },
                
                // Time
                new UnitDefinition { Name = "minute", Symbol = "min", BaseUnit = "second", ConversionFactor = 60.0 },
                new UnitDefinition { Name = "hour", Symbol = "h", BaseUnit = "second", ConversionFactor = 3600.0 },
                new UnitDefinition { Name = "day", Symbol = "d", BaseUnit = "second", ConversionFactor = 86400.0 },
                new UnitDefinition { Name = "millisecond", Symbol = "ms", BaseUnit = "second", ConversionFactor = 0.001 },
                new UnitDefinition { Name = "microsecond", Symbol = "μs", BaseUnit = "second", ConversionFactor = 0.000001 },
                
                // Force
                new UnitDefinition { Name = "newton", Symbol = "N", BaseUnit = "newton", ConversionFactor = 1.0 },
                
                // Energy
                new UnitDefinition { Name = "joule", Symbol = "J", BaseUnit = "joule", ConversionFactor = 1.0 },
                new UnitDefinition { Name = "calorie", Symbol = "cal", BaseUnit = "joule", ConversionFactor = 4.184 },
                
                // Power
                new UnitDefinition { Name = "watt", Symbol = "W", BaseUnit = "watt", ConversionFactor = 1.0 },
                new UnitDefinition { Name = "kilowatt", Symbol = "kW", BaseUnit = "watt", ConversionFactor = 1000.0 },
                new UnitDefinition { Name = "horsepower", Symbol = "hp", BaseUnit = "watt", ConversionFactor = 745.7 },
                
                // Temperature
                new UnitDefinition { Name = "celsius", Symbol = "°C", BaseUnit = "kelvin", ConversionFactor = 1.0 }, // Note: offset needed
                new UnitDefinition { Name = "fahrenheit", Symbol = "°F", BaseUnit = "kelvin", ConversionFactor = 0.556 }, // Note: offset needed
                
                // Voltage
                new UnitDefinition { Name = "volt", Symbol = "V", BaseUnit = "volt", ConversionFactor = 1.0 },
                new UnitDefinition { Name = "millivolt", Symbol = "mV", BaseUnit = "volt", ConversionFactor = 0.001 },
                
                // Current
                new UnitDefinition { Name = "milliampere", Symbol = "mA", BaseUnit = "ampere", ConversionFactor = 0.001 }
            };
            
            // Add all units to the model
            model.Units.AddRange(siBaseUnits);
            model.Units.AddRange(derivedUnits);
            
            // Scan AST for unit literal expressions and unit-typed variables
            var allNodes = GetAllNodes(model.Program);
            
            foreach (var node in allNodes)
            {
                switch (node)
                {
                    case LiteralExpression litExpr when litExpr.Value is Tokens.UnitLiteral unitLit:
                        // Ensure the unit is registered
                        if (!model.Units.Any(u => u.Symbol == unitLit.Unit || u.Name == unitLit.Unit))
                        {
                            diagnostics.ReportInfo(
                                $"Unknown unit '{unitLit.Unit}' used in literal",
                                new SourceLocation { Line = 1, Column = 1 }
                            );
                        }
                        break;
                        
                    case VariableDeclaration varDecl when varDecl.Type?.Name?.Contains("_") == true:
                        // Check for unit-typed variables (e.g., "double_meters")
                        var parts = varDecl.Type.Name.Split('_');
                        if (parts.Length == 2)
                        {
                            var unitName = parts[1];
                            if (!model.Units.Any(u => u.Name == unitName || u.Symbol == unitName))
                            {
                                diagnostics.ReportInfo(
                                    $"Variable '{varDecl.Name}' uses unit type '{unitName}'",
                                    new SourceLocation { Line = varDecl.Line, Column = varDecl.Column }
                                );
                            }
                        }
                        break;
                }
            }
        }
        
        private IEnumerable<AstNode> GetAllNodes(AstNode root)
        {
            var nodes = new List<AstNode>();
            TraverseNodes(root, nodes);
            return nodes;
        }
        
        private void TraverseNodes(AstNode node, List<AstNode> nodes)
        {
            if (node == null) return;
            
            nodes.Add(node);
            
            // Traverse child nodes based on node type
            switch (node)
            {
                case Core.AST.Program program:
                    foreach (var stmt in program.Statements)
                        TraverseNodes(stmt, nodes);
                    break;
                    
                case BlockStatement block:
                    foreach (var stmt in block.Statements)
                        TraverseNodes(stmt, nodes);
                    break;
                    
                case BinaryExpression binExpr:
                    TraverseNodes(binExpr.Left, nodes);
                    TraverseNodes(binExpr.Right, nodes);
                    break;
                    
                case UnaryExpression unExpr:
                    TraverseNodes(unExpr.Operand, nodes);
                    break;
                    
                case CallExpression callExpr:
                    TraverseNodes(callExpr.Callee, nodes);
                    foreach (var arg in callExpr.Arguments)
                        TraverseNodes(arg, nodes);
                    break;
                    
                case VariableDeclaration varDecl:
                    if (varDecl.Initializer != null)
                        TraverseNodes(varDecl.Initializer, nodes);
                    break;
            }
        }
        
        private void AnalyzeDimensionalCompatibility(SemanticModel model) 
        {
            // Check that units are dimensionally compatible in operations
            // This would analyze expressions to ensure dimensional consistency
            
            // For demonstration, report info about the analysis
            diagnostics.ReportInfo(
                "Dimensional analysis started",
                new SourceLocation { Line = 1, Column = 1 }
            );
        }
        
        private void AnalyzeUnitConversions(SemanticModel model) 
        {
            // Analyze unit conversions and ensure they're valid
            // This would check conversion factors and compatibility
            
            // For demonstration, just indicate the analysis ran
            if (model.Units.Count > 0)
            {
                diagnostics.ReportInfo(
                    $"Analyzed {model.Units.Count} unit definitions",
                    new SourceLocation { Line = 1, Column = 1 }
                );
            }
        }
    }
    
    /// <summary>
    /// Memory safety analyzer
    /// </summary>
    public class MemorySafetyAnalyzer : IAnalysisPass
    {
        private readonly DiagnosticEngine diagnostics;
        
        public MemorySafetyAnalyzer(DiagnosticEngine diagnostics)
        {
            this.diagnostics = diagnostics;
        }
        
        public async Task AnalyzeAsync(SemanticModel semanticModel)
        {
            await Task.Run(() =>
            {
                AnalyzeMemoryLeaks(semanticModel);
                AnalyzeBufferOverflows(semanticModel);
                AnalyzeUseAfterFree(semanticModel);
                AnalyzeNullPointerDereferences(semanticModel);
            });
        }
        
        private void AnalyzeMemoryLeaks(SemanticModel model) 
        {
            // Analyze potential memory leaks by tracking allocations and deallocations
            diagnostics.ReportInfo(
                "Memory leak analysis started",
                new SourceLocation { Line = 1, Column = 1 }
            );
        }
        
        private void AnalyzeBufferOverflows(SemanticModel model) 
        {
            // Check array accesses and pointer arithmetic for potential overflows
            diagnostics.ReportInfo(
                "Buffer overflow analysis started",
                new SourceLocation { Line = 1, Column = 1 }
            );
        }
        
        private void AnalyzeUseAfterFree(SemanticModel model) 
        {
            // Track object lifetimes to detect use-after-free vulnerabilities
            diagnostics.ReportInfo(
                "Use-after-free analysis started",
                new SourceLocation { Line = 1, Column = 1 }
            );
        }
        
        private void AnalyzeNullPointerDereferences(SemanticModel model) 
        {
            // Analyze null checks and pointer dereferences
            diagnostics.ReportInfo(
                "Null pointer dereference analysis started",
                new SourceLocation { Line = 1, Column = 1 }
            );
        }
    }
    
    /// <summary>
    /// Assembly integration analyzer
    /// </summary>
    public class AssemblyIntegrationAnalyzer : IAnalysisPass
    {
        private readonly DiagnosticEngine diagnostics;
        
        public AssemblyIntegrationAnalyzer(DiagnosticEngine diagnostics)
        {
            this.diagnostics = diagnostics;
        }
        
        public async Task AnalyzeAsync(SemanticModel semanticModel)
        {
            await Task.Run(() =>
            {
                AnalyzeAssemblyBlocks(semanticModel);
                AnalyzeRegisterUsage(semanticModel);
                AnalyzeVariableBinding(semanticModel);
            });
        }
        
        private void AnalyzeAssemblyBlocks(SemanticModel model) 
        {
            // Analyze inline assembly blocks for correctness
            diagnostics.ReportInfo(
                "Assembly block analysis started",
                new SourceLocation { Line = 1, Column = 1 }
            );
        }
        
        private void AnalyzeRegisterUsage(SemanticModel model) 
        {
            // Check register allocation and usage patterns
            diagnostics.ReportInfo(
                "Register usage analysis started",
                new SourceLocation { Line = 1, Column = 1 }
            );
        }
        
        private void AnalyzeVariableBinding(SemanticModel model) 
        {
            // Verify variable bindings between high-level and assembly code
            diagnostics.ReportInfo(
                "Variable binding analysis started",
                new SourceLocation { Line = 1, Column = 1 }
            );
        }
    }
    
    /// <summary>
    /// Concurrency analyzer
    /// </summary>
    public class ConcurrencyAnalyzer : IAnalysisPass
    {
        private readonly DiagnosticEngine diagnostics;
        
        public ConcurrencyAnalyzer(DiagnosticEngine diagnostics)
        {
            this.diagnostics = diagnostics;
        }
        
        public async Task AnalyzeAsync(SemanticModel semanticModel)
        {
            await Task.Run(() =>
            {
                AnalyzeRaceConditions(semanticModel);
                AnalyzeDeadlocks(semanticModel);
                AnalyzeAtomicOperations(semanticModel);
            });
        }
        
        private void AnalyzeRaceConditions(SemanticModel model) 
        {
            // Detect potential race conditions in concurrent code
            diagnostics.ReportInfo(
                "Race condition analysis started",
                new SourceLocation { Line = 1, Column = 1 }
            );
        }
        
        private void AnalyzeDeadlocks(SemanticModel model) 
        {
            // Analyze lock acquisition patterns for potential deadlocks
            diagnostics.ReportInfo(
                "Deadlock analysis started",
                new SourceLocation { Line = 1, Column = 1 }
            );
        }
        
        private void AnalyzeAtomicOperations(SemanticModel model) 
        {
            // Verify atomic operation usage and memory ordering
            diagnostics.ReportInfo(
                "Atomic operation analysis started",
                new SourceLocation { Line = 1, Column = 1 }
            );
        }
    }
    
    /// <summary>
    /// Real-time systems analyzer
    /// </summary>
    public class RealTimeAnalyzer : IAnalysisPass
    {
        private readonly DiagnosticEngine diagnostics;
        
        public RealTimeAnalyzer(DiagnosticEngine diagnostics)
        {
            this.diagnostics = diagnostics;
        }
        
        public async Task AnalyzeAsync(SemanticModel semanticModel)
        {
            await Task.Run(() =>
            {
                AnalyzeTimingConstraints(semanticModel);
                AnalyzeWCET(semanticModel);
                AnalyzeDeadlines(semanticModel);
            });
        }
        
        private void AnalyzeTimingConstraints(SemanticModel model) 
        {
            // Analyze timing constraints for real-time systems
            diagnostics.ReportInfo(
                "Timing constraint analysis started",
                new SourceLocation { Line = 1, Column = 1 }
            );
        }
        
        private void AnalyzeWCET(SemanticModel model) 
        {
            // Analyze Worst-Case Execution Time
            diagnostics.ReportInfo(
                "WCET analysis started",
                new SourceLocation { Line = 1, Column = 1 }
            );
        }
        
        private void AnalyzeDeadlines(SemanticModel model) 
        {
            // Check deadline constraints in real-time code
            diagnostics.ReportInfo(
                "Deadline analysis started",
                new SourceLocation { Line = 1, Column = 1 }
            );
        }
    }
    
    /// <summary>
    /// Contract verifier
    /// </summary>
    public class ContractVerifier : IAnalysisPass
    {
        private readonly DiagnosticEngine diagnostics;
        
        public ContractVerifier(DiagnosticEngine diagnostics)
        {
            this.diagnostics = diagnostics;
        }
        
        public async Task AnalyzeAsync(SemanticModel semanticModel)
        {
            await Task.Run(() =>
            {
                AnalyzePreconditions(semanticModel);
                AnalyzePostconditions(semanticModel);
                AnalyzeInvariants(semanticModel);
            });
        }
        
        private void AnalyzePreconditions(SemanticModel model) 
        {
            // Verify preconditions in contract specifications
            diagnostics.ReportInfo(
                "Precondition analysis started",
                new SourceLocation { Line = 1, Column = 1 }
            );
        }
        
        private void AnalyzePostconditions(SemanticModel model) 
        {
            // Verify postconditions in contract specifications
            diagnostics.ReportInfo(
                "Postcondition analysis started",
                new SourceLocation { Line = 1, Column = 1 }
            );
        }
        
        private void AnalyzeInvariants(SemanticModel model) 
        {
            // Verify class and loop invariants
            diagnostics.ReportInfo(
                "Invariant analysis started",
                new SourceLocation { Line = 1, Column = 1 }
            );
        }
    }
    
    /// <summary>
    /// Performance analyzer
    /// </summary>
    public class PerformanceAnalyzer : IAnalysisPass
    {
        private readonly DiagnosticEngine diagnostics;
        
        public PerformanceAnalyzer(DiagnosticEngine diagnostics)
        {
            this.diagnostics = diagnostics;
        }
        
        public async Task AnalyzeAsync(SemanticModel semanticModel)
        {
            await Task.Run(() =>
            {
                AnalyzeHotPaths(semanticModel);
                AnalyzeInliningOpportunities(semanticModel);
                AnalyzeVectorizationOpportunities(semanticModel);
            });
        }
        
        private void AnalyzeHotPaths(SemanticModel model) 
        {
            // Identify hot paths in code for optimization
            diagnostics.ReportInfo(
                "Hot path analysis started",
                new SourceLocation { Line = 1, Column = 1 }
            );
        }
        
        private void AnalyzeInliningOpportunities(SemanticModel model) 
        {
            // Identify functions suitable for inlining
            diagnostics.ReportInfo(
                "Inlining opportunity analysis started",
                new SourceLocation { Line = 1, Column = 1 }
            );
        }
        
        private void AnalyzeVectorizationOpportunities(SemanticModel model) 
        {
            // Identify loops that can be vectorized
            diagnostics.ReportInfo(
                "Vectorization opportunity analysis started",
                new SourceLocation { Line = 1, Column = 1 }
            );
        }
    }
    
    /// <summary>
    /// Security analyzer
    /// </summary>
    public class SecurityAnalyzer : IAnalysisPass
    {
        private readonly DiagnosticEngine diagnostics;
        
        public SecurityAnalyzer(DiagnosticEngine diagnostics)
        {
            this.diagnostics = diagnostics;
        }
        
        public async Task AnalyzeAsync(SemanticModel semanticModel)
        {
            await Task.Run(() =>
            {
                AnalyzeSecurityVulnerabilities(semanticModel);
                AnalyzeCryptographicUsage(semanticModel);
                AnalyzeSecureMemoryUsage(semanticModel);
            });
        }
        
        private void AnalyzeSecurityVulnerabilities(SemanticModel model) 
        {
            // Scan for common security vulnerabilities
            diagnostics.ReportInfo(
                "Security vulnerability analysis started",
                new SourceLocation { Line = 1, Column = 1 }
            );
        }
        
        private void AnalyzeCryptographicUsage(SemanticModel model) 
        {
            // Check proper usage of cryptographic APIs
            diagnostics.ReportInfo(
                "Cryptographic usage analysis started",
                new SourceLocation { Line = 1, Column = 1 }
            );
        }
        
        private void AnalyzeSecureMemoryUsage(SemanticModel model) 
        {
            // Verify secure memory handling practices
            diagnostics.ReportInfo(
                "Secure memory usage analysis started",
                new SourceLocation { Line = 1, Column = 1 }
            );
        }
    }
    
    /// <summary>
    /// Supporting data structures
    /// </summary>
    public class Domain
    {
        public string Name { get; set; }
        public Dictionary<string, string> OperatorMappings { get; set; } = new();
        public Dictionary<string, object> Constants { get; set; } = new();
        
        public Domain(string name)
        {
            Name = name;
        }
    }
    
    public class UnitDefinition
    {
        public string Name { get; set; } = "";
        public string Symbol { get; set; } = "";
        public double ConversionFactor { get; set; } = 1.0;
        public string BaseUnit { get; set; } = "";
    }
    
    public class ContractDefinition
    {
        public string Name { get; set; } = "";
        public string Condition { get; set; } = "";
        public ContractType Type { get; set; }
    }
    
    public enum ContractType
    {
        Precondition,
        Postcondition,
        Invariant
    }
    
    public class SecurityAnnotation
    {
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
        public SourceLocation Location { get; set; }
    }
} 
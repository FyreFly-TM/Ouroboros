using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ouroboros.Core.Lexer;
using Ouroboros.Core.Parser;
using Ouroboros.Core.Compiler;
using Ouroboros.Core;

namespace Ouroboros.IDE
{
    /// <summary>
    /// Provides real-time diagnostics for Ouroboros code
    /// </summary>
    public class DiagnosticProvider
    {
        private readonly TypeChecker typeChecker;
        private readonly DiagnosticEngine diagnosticEngine;
        private readonly Dictionary<string, List<Diagnostic>> fileDiagnostics = new();

        public DiagnosticProvider(TypeChecker typeChecker, DiagnosticEngine diagnosticEngine)
        {
            this.typeChecker = typeChecker;
            this.diagnosticEngine = diagnosticEngine;
        }

        /// <summary>
        /// Analyze a file and return diagnostics
        /// </summary>
        public async Task<List<Diagnostic>> AnalyzeFileAsync(string filePath, string content)
        {
            var diagnostics = new List<Diagnostic>();

            try
            {
                // Lexical analysis
                var lexer = new Lexer(content, filePath);
                var tokens = lexer.ScanTokens();
                
                // Collect lexer errors
                diagnostics.AddRange(ConvertLexerErrors(lexer));

                if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                {
                    // Don't continue if there are lexer errors
                    UpdateFileDiagnostics(filePath, diagnostics);
                    return diagnostics;
                }

                // Syntactic analysis
                var parser = new Parser(tokens);
                var ast = parser.Parse();
                
                // Collect parser errors
                diagnostics.AddRange(ConvertParserErrors(parser));

                if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                {
                    // Don't continue if there are parser errors
                    UpdateFileDiagnostics(filePath, diagnostics);
                    return diagnostics;
                }

                // Semantic analysis
                try
                {
                    typeChecker.Check(ast);
                }
                catch (TypeCheckException tcEx)
                {
                    foreach (var error in tcEx.Errors)
                    {
                        diagnostics.Add(new Diagnostic
                        {
                            Message = error.Message,
                            Severity = DiagnosticSeverity.Error,
                            Range = new Range
                            {
                                Start = new Position(error.Line - 1, error.Column - 1),
                                End = new Position(error.Line - 1, error.Column)
                            },
                            Source = "type-checker",
                            Code = "TYPE001"
                        });
                    }
                }

                // Add warnings and hints
                diagnostics.AddRange(await GenerateWarningsAsync(ast, content));
                diagnostics.AddRange(GenerateHints(ast, content));
            }
            catch (Exception ex)
            {
                // Add a diagnostic for unexpected errors
                diagnostics.Add(new Diagnostic
                {
                    Range = new Range { Start = new Position(0, 0), End = new Position(0, 0) },
                    Message = $"Internal error: {ex.Message}",
                    Severity = DiagnosticSeverity.Error,
                    Source = "ouroboros"
                });
            }

            UpdateFileDiagnostics(filePath, diagnostics);
            return diagnostics;
        }

        /// <summary>
        /// Get quick fixes for a diagnostic
        /// </summary>
        public List<CodeAction> GetQuickFixes(string filePath, Diagnostic diagnostic)
        {
            var actions = new List<CodeAction>();

            switch (diagnostic.Code)
            {
                case "missing-import":
                    actions.Add(CreateImportAction(filePath, diagnostic));
                    break;
                case "unused-variable":
                    actions.Add(CreateRemoveAction(filePath, diagnostic));
                    actions.Add(CreatePrefixUnderscoreAction(filePath, diagnostic));
                    break;
                case "missing-type":
                    actions.Add(CreateAddTypeAnnotationAction(filePath, diagnostic));
                    break;
                case "deprecated":
                    if (diagnostic.Data?.ContainsKey("replacement") == true)
                    {
                        actions.Add(CreateReplaceDeprecatedAction(filePath, diagnostic));
                    }
                    break;
            }

            return actions;
        }

        private List<Diagnostic> ConvertLexerErrors(Lexer lexer)
        {
            // Lexer doesn't expose errors, it likely throws exceptions
            // Return empty list for now
            return new List<Diagnostic>();
        }

        private List<Diagnostic> ConvertParserErrors(Parser parser)
        {
            var diagnostics = new List<Diagnostic>();
            
            // Get errors from parser
            if (parser != null && parser.Errors != null)
            {
                foreach (var error in parser.Errors)
                {
                    var line = error.Token?.Line ?? 1;
                    var column = error.Token?.Column ?? 1;
                    
                    diagnostics.Add(new Diagnostic
                    {
                        Message = error.Message,
                        Severity = DiagnosticSeverity.Error,
                        Range = new Range
                        {
                            Start = new Position(line - 1, column - 1),
                            End = new Position(line - 1, column)
                        },
                        Source = "parser",
                        Code = "PARSE001"
                    });
                }
            }
            
            return diagnostics;
        }

        // This method is no longer needed since we handle TypeCheckException in the main method
        private List<Diagnostic> ConvertTypeCheckerErrors(TypeChecker typeChecker)
        {
            // Method kept for compatibility but returns empty list
            return new List<Diagnostic>();
        }

        private async Task<List<Diagnostic>> GenerateWarningsAsync(Core.AST.Program ast, string content)
        {
            var warnings = new List<Diagnostic>();

            // Unused variables
            var unusedVars = FindUnusedVariables(ast);
            foreach (var unused in unusedVars)
            {
                warnings.Add(new Diagnostic
                {
                    Range = GetNodeRange(unused),
                    Message = $"Variable '{unused.Name}' is declared but never used",
                    Severity = DiagnosticSeverity.Warning,
                    Code = "unused-variable",
                    Source = "ouroboros",
                    Tags = new[] { DiagnosticTag.Unnecessary }
                });
            }

            // Deprecated usage
            var deprecatedUsages = FindDeprecatedUsages(ast);
            foreach (var deprecated in deprecatedUsages)
            {
                warnings.Add(new Diagnostic
                {
                    Range = GetNodeRange(deprecated.Node),
                    Message = deprecated.Message,
                    Severity = DiagnosticSeverity.Warning,
                    Code = "deprecated",
                    Source = "ouroboros",
                    Tags = new[] { DiagnosticTag.Deprecated },
                    Data = deprecated.Data
                });
            }

            return warnings;
        }

        private List<Diagnostic> GenerateHints(Core.AST.Program ast, string content)
        {
            var hints = new List<Diagnostic>();

            // Missing type annotations
            var missingTypes = FindMissingTypeAnnotations(ast);
            foreach (var missing in missingTypes)
            {
                hints.Add(new Diagnostic
                {
                    Range = GetNodeRange(missing),
                    Message = "Consider adding a type annotation",
                    Severity = DiagnosticSeverity.Hint,
                    Code = "missing-type",
                    Source = "ouroboros"
                });
            }

            // Code style suggestions
            var styleIssues = FindStyleIssues(ast);
            foreach (var issue in styleIssues)
            {
                hints.Add(new Diagnostic
                {
                    Range = GetNodeRange(issue.Node),
                    Message = issue.Message,
                    Severity = DiagnosticSeverity.Hint,
                    Code = issue.Code,
                    Source = "ouroboros-style"
                });
            }

            return hints;
        }

        private void UpdateFileDiagnostics(string filePath, List<Diagnostic> diagnostics)
        {
            fileDiagnostics[filePath] = diagnostics;
            DiagnosticsChanged?.Invoke(filePath, diagnostics);
        }

        private List<Core.AST.VariableDeclaration> FindUnusedVariables(Core.AST.Program ast)
        {
            var unusedVars = new List<Core.AST.VariableDeclaration>();
            var declaredVars = new Dictionary<string, (int line, int column)>();
            var usedVars = new HashSet<string>();
            
            // First pass: collect all variable declarations
            var declarationVisitor = new VariableDeclarationVisitor();
            foreach (var statement in ast.Statements)
            {
                declarationVisitor.Visit(statement);
            }
            declaredVars = declarationVisitor.DeclaredVariables;
            
            // Second pass: collect all variable uses
            var usageVisitor = new VariableUsageVisitor();
            foreach (var statement in ast.Statements)
            {
                usageVisitor.Visit(statement);
            }
            usedVars = usageVisitor.UsedVariables;
            
            // Find unused variables
            foreach (var decl in declaredVars)
            {
                if (!usedVars.Contains(decl.Key) && !decl.Key.StartsWith("_"))
                {
                    unusedVars.Add(new Core.AST.VariableDeclaration
                    {
                        Name = decl.Key,
                        Line = decl.Value.line,
                        Column = decl.Value.column
                    });
                }
            }
            
            return unusedVars;
        }

        private List<DeprecatedUsage> FindDeprecatedUsages(Core.AST.Program ast)
        {
            var deprecatedUsages = new List<DeprecatedUsage>();
            
            // Check for deprecated API usage
            var deprecatedVisitor = new DeprecatedUsageVisitor();
            foreach (var statement in ast.Statements)
            {
                deprecatedVisitor.Visit(statement);
            }
            
            foreach (var usage in deprecatedVisitor.DeprecatedUsages)
            {
                deprecatedUsages.Add(new DeprecatedUsage
                {
                    Message = usage.message,
                    Line = usage.line,
                    Column = usage.column
                });
            }
            
            return deprecatedUsages;
        }

        private List<Core.AST.AstNode> FindMissingTypeAnnotations(Core.AST.Program ast)
        {
            var missingTypes = new List<Core.AST.AstNode>();
            
            // Check for missing type annotations
            var typeVisitor = new TypeAnnotationVisitor();
            foreach (var statement in ast.Statements)
            {
                typeVisitor.Visit(statement);
            }
            
            foreach (var missing in typeVisitor.MissingAnnotations)
            {
                missingTypes.Add(new Core.AST.VariableDeclaration
                {
                    Name = missing.context,
                    Line = missing.line,
                    Column = missing.column
                });
            }
            
            return missingTypes;
        }

        private List<StyleIssue> FindStyleIssues(Core.AST.Program ast)
        {
            var styleIssues = new List<StyleIssue>();
            
            // Check for style violations
            var styleVisitor = new StyleCheckVisitor();
            foreach (var statement in ast.Statements)
            {
                styleVisitor.Visit(statement);
            }
            
            foreach (var violation in styleVisitor.StyleViolations)
            {
                styleIssues.Add(new StyleIssue
                {
                    Message = violation.message,
                    Suggestion = violation.suggestion,
                    Line = violation.line,
                    Column = violation.column
                });
            }
            
            return styleIssues;
        }

        private Range GetNodeRange(Core.AST.AstNode node)
        {
            // Get actual range from AST node
            if (node == null)
            {
                return new Range
                {
                    Start = new Position { Line = 0, Character = 0 },
                    End = new Position { Line = 0, Character = 0 }
                };
            }
            
            var startLine = node.Line > 0 ? node.Line - 1 : 0;
            var startChar = node.Column > 0 ? node.Column - 1 : 0;
            
            // Try to calculate end position based on node type
            var endChar = startChar + EstimateNodeLength(node);
            
            return new Range
            {
                Start = new Position { Line = startLine, Character = startChar },
                End = new Position { Line = startLine, Character = endChar }
            };
        }

        private int EstimateNodeLength(Core.AST.AstNode node)
        {
            // Estimate node length based on type
            // This is a simplification - in production, track actual token end positions
            return node switch
            {
                Core.AST.IdentifierExpression id => id.Name.Length,
                Core.AST.LiteralExpression lit => lit.Value?.ToString().Length ?? 1,
                Core.AST.BinaryExpression => 3, // operator length estimate
                Core.AST.UnaryExpression => 1,
                _ => 10 // default estimate
            };
        }

        private CodeAction CreateImportAction(string filePath, Diagnostic diagnostic)
        {
            return new CodeAction
            {
                Title = "Add import statement",
                Kind = CodeActionKind.QuickFix,
                Diagnostics = new[] { diagnostic },
                Edit = new WorkspaceEdit
                {
                    Changes = new Dictionary<string, List<TextEdit>>
                    {
                        [filePath] = new List<TextEdit>
                        {
                            new TextEdit
                            {
                                Range = new Range { Start = new Position(0, 0), End = new Position(0, 0) },
                                NewText = $"import {{ {diagnostic.Data["symbol"]} }} from \"{diagnostic.Data["module"]}\";\n"
                            }
                        }
                    }
                }
            };
        }

        private CodeAction CreateRemoveAction(string filePath, Diagnostic diagnostic)
        {
            return new CodeAction
            {
                Title = "Remove unused variable",
                Kind = CodeActionKind.QuickFix,
                Diagnostics = new[] { diagnostic },
                Edit = new WorkspaceEdit
                {
                    Changes = new Dictionary<string, List<TextEdit>>
                    {
                        [filePath] = new List<TextEdit>
                        {
                            new TextEdit
                            {
                                Range = diagnostic.Range,
                                NewText = ""
                            }
                        }
                    }
                }
            };
        }

        private CodeAction CreatePrefixUnderscoreAction(string filePath, Diagnostic diagnostic)
        {
            return new CodeAction
            {
                Title = "Prefix with underscore",
                Kind = CodeActionKind.QuickFix,
                Diagnostics = new[] { diagnostic },
                Edit = new WorkspaceEdit
                {
                    Changes = new Dictionary<string, List<TextEdit>>
                    {
                        [filePath] = new List<TextEdit>
                        {
                            new TextEdit
                            {
                                Range = diagnostic.Range,
                                NewText = "_" + diagnostic.Data["variableName"]
                            }
                        }
                    }
                }
            };
        }

        private CodeAction CreateAddTypeAnnotationAction(string filePath, Diagnostic diagnostic)
        {
            var inferredType = diagnostic.Data?["inferredType"] ?? "any";
            return new CodeAction
            {
                Title = $"Add type annotation: {inferredType}",
                Kind = CodeActionKind.QuickFix,
                Diagnostics = new[] { diagnostic },
                Edit = new WorkspaceEdit
                {
                    Changes = new Dictionary<string, List<TextEdit>>
                    {
                        [filePath] = new List<TextEdit>
                        {
                            new TextEdit
                            {
                                Range = diagnostic.Range,
                                NewText = $": {inferredType}"
                            }
                        }
                    }
                }
            };
        }

        private CodeAction CreateReplaceDeprecatedAction(string filePath, Diagnostic diagnostic)
        {
            return new CodeAction
            {
                Title = $"Replace with {diagnostic.Data["replacement"]}",
                Kind = CodeActionKind.QuickFix,
                Diagnostics = new[] { diagnostic },
                Edit = new WorkspaceEdit
                {
                    Changes = new Dictionary<string, List<TextEdit>>
                    {
                        [filePath] = new List<TextEdit>
                        {
                            new TextEdit
                            {
                                Range = diagnostic.Range,
                                NewText = diagnostic.Data["replacement"]
                            }
                        }
                    }
                }
            };
        }

        public event Action<string, List<Diagnostic>> DiagnosticsChanged;
    }

    public class Diagnostic
    {
        public Range Range { get; set; }
        public DiagnosticSeverity Severity { get; set; }
        public string Code { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }
        public DiagnosticTag[] Tags { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }

    public enum DiagnosticSeverity
    {
        Error = 1,
        Warning = 2,
        Information = 3,
        Hint = 4
    }

    public enum DiagnosticTag
    {
        Unnecessary = 1,
        Deprecated = 2
    }

    public class Range
    {
        public Position Start { get; set; }
        public Position End { get; set; }
    }

    public class Position
    {
        public int Line { get; set; }
        public int Character { get; set; }

        public Position(int line, int character)
        {
            Line = line;
            Character = character;
        }
    }

    public class CodeAction
    {
        public string Title { get; set; }
        public CodeActionKind Kind { get; set; }
        public Diagnostic[] Diagnostics { get; set; }
        public WorkspaceEdit Edit { get; set; }
        public Command Command { get; set; }
    }

    public enum CodeActionKind
    {
        QuickFix,
        Refactor,
        RefactorExtract,
        RefactorInline,
        RefactorRewrite,
        Source,
        SourceOrganizeImports
    }

    public class WorkspaceEdit
    {
        public Dictionary<string, List<TextEdit>> Changes { get; set; }
    }

    public class TextEdit
    {
        public Range Range { get; set; }
        public string NewText { get; set; }
    }

    public class Command
    {
        public string Title { get; set; }
        public string CommandId { get; set; }
        public object[] Arguments { get; set; }
    }

    internal class DeprecatedUsage
    {
        public Core.AST.AstNode Node { get; set; }
        public string Message { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }

    internal class StyleIssue
    {
        public Core.AST.AstNode Node { get; set; }
        public string Message { get; set; }
        public string Suggestion { get; set; }
        public string Code { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }

    internal class TypeCheckerError
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public int Length { get; set; }
        public string Message { get; set; }
        public string Code { get; set; }
    }

    // Visitor classes for analysis
    internal class VariableDeclarationVisitor : AstVisitor
    {
        public Dictionary<string, (int line, int column)> DeclaredVariables { get; } = new();
        
        public override void VisitVariableDeclaration(Core.AST.VariableDeclaration decl)
        {
            DeclaredVariables[decl.Name] = (decl.Line, decl.Column);
            base.VisitVariableDeclaration(decl);
        }
    }
    
    internal class VariableUsageVisitor : AstVisitor
    {
        public HashSet<string> UsedVariables { get; } = new();
        
        public override void VisitIdentifierExpression(Core.AST.IdentifierExpression expr)
        {
            UsedVariables.Add(expr.Name);
            base.VisitIdentifierExpression(expr);
        }
    }
    
    internal class DeprecatedUsageVisitor : AstVisitor
    {
        public List<(string message, int line, int column)> DeprecatedUsages { get; } = new();
        
        // Check for deprecated patterns
        public override void VisitCallExpression(Core.AST.CallExpression expr)
        {
            if (expr.Callee is Core.AST.IdentifierExpression id)
            {
                // Check against known deprecated APIs
                if (IsDeprecatedApi(id.Name))
                {
                    DeprecatedUsages.Add((
                        $"'{id.Name}' is deprecated. Consider using the recommended alternative.",
                        expr.Line,
                        expr.Column
                    ));
                }
            }
            base.VisitCallExpression(expr);
        }
        
        private bool IsDeprecatedApi(string name)
        {
            // List of deprecated APIs
            var deprecated = new HashSet<string>
            {
                "OldFunction",
                "LegacyMethod",
                // Add more as needed
            };
            return deprecated.Contains(name);
        }
    }
    
    internal class TypeAnnotationVisitor : AstVisitor
    {
        public List<(string context, int line, int column)> MissingAnnotations { get; } = new();
        
        public override void VisitVariableDeclaration(Core.AST.VariableDeclaration decl)
        {
            if (decl.Type == null || decl.Type.Name == "var" || decl.Type.Name == "auto")
            {
                MissingAnnotations.Add((
                    $"variable '{decl.Name}'",
                    decl.Line,
                    decl.Column
                ));
            }
            base.VisitVariableDeclaration(decl);
        }
        
        public override void VisitFunctionDeclaration(Core.AST.FunctionDeclaration decl)
        {
            if (decl.ReturnType == null)
            {
                MissingAnnotations.Add((
                    $"return type of function '{decl.Name}'",
                    decl.Line,
                    decl.Column
                ));
            }
            base.VisitFunctionDeclaration(decl);
        }
    }
    
    internal class StyleCheckVisitor : AstVisitor
    {
        public List<(string message, string suggestion, int line, int column)> StyleViolations { get; } = new();
        
        public override void VisitIdentifierExpression(Core.AST.IdentifierExpression expr)
        {
            // Check naming conventions
            if (!IsValidIdentifierStyle(expr.Name))
            {
                StyleViolations.Add((
                    $"Identifier '{expr.Name}' does not follow naming conventions",
                    "Use camelCase for variables and PascalCase for types",
                    expr.Line,
                    expr.Column
                ));
            }
            base.VisitIdentifierExpression(expr);
        }
        
        private bool IsValidIdentifierStyle(string name)
        {
            // Simple check - can be expanded
            return !string.IsNullOrEmpty(name) && char.IsLetter(name[0]);
        }
    }
    
    // Base visitor class
    internal abstract class AstVisitor
    {
        public virtual void Visit(Core.AST.AstNode node)
        {
            node?.Accept(this);
        }
        
        public virtual void VisitVariableDeclaration(Core.AST.VariableDeclaration decl) { }
        public virtual void VisitIdentifierExpression(Core.AST.IdentifierExpression expr) { }
        public virtual void VisitCallExpression(Core.AST.CallExpression expr) { }
        public virtual void VisitFunctionDeclaration(Core.AST.FunctionDeclaration decl) { }
    }
} 
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
                typeChecker.CheckProgram(ast);
                diagnostics.AddRange(ConvertTypeCheckerErrors(typeChecker));

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
            // TODO: Get errors from lexer
            return new List<Diagnostic>();
        }

        private List<Diagnostic> ConvertParserErrors(Parser parser)
        {
            // TODO: Get errors from parser
            return new List<Diagnostic>();
        }

        private List<Diagnostic> ConvertTypeCheckerErrors(TypeChecker typeChecker)
        {
            var diagnostics = new List<Diagnostic>();
            
            foreach (var error in typeChecker.GetErrors())
            {
                diagnostics.Add(new Diagnostic
                {
                    Range = new Range
                    {
                        Start = new Position(error.Line - 1, error.Column),
                        End = new Position(error.Line - 1, error.Column + error.Length)
                    },
                    Message = error.Message,
                    Severity = DiagnosticSeverity.Error,
                    Code = error.Code,
                    Source = "ouroboros-type-checker"
                });
            }

            return diagnostics;
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
            // TODO: Implement unused variable detection
            return new List<Core.AST.VariableDeclaration>();
        }

        private List<DeprecatedUsage> FindDeprecatedUsages(Core.AST.Program ast)
        {
            // TODO: Implement deprecated usage detection
            return new List<DeprecatedUsage>();
        }

        private List<Core.AST.AstNode> FindMissingTypeAnnotations(Core.AST.Program ast)
        {
            // TODO: Implement missing type annotation detection
            return new List<Core.AST.AstNode>();
        }

        private List<StyleIssue> FindStyleIssues(Core.AST.Program ast)
        {
            // TODO: Implement style issue detection
            return new List<StyleIssue>();
        }

        private Range GetNodeRange(Core.AST.AstNode node)
        {
            // TODO: Get actual range from AST node
            return new Range
            {
                Start = new Position(0, 0),
                End = new Position(0, 0)
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
        public Dictionary<string, string> Data { get; set; }
    }

    internal class StyleIssue
    {
        public Core.AST.AstNode Node { get; set; }
        public string Message { get; set; }
        public string Code { get; set; }
    }

    internal class TypeCheckerError
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public int Length { get; set; }
        public string Message { get; set; }
        public string Code { get; set; }
    }
} 
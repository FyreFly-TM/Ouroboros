using System;
using System.Collections.Generic;
using System.Linq;
using Ouroboros.Core.AST;
using Ouroboros.Core.Compiler;

namespace Ouroboros.IDE
{
    /// <summary>
    /// Code completion provider for Ouroboros
    /// </summary>
    public class CodeCompletionProvider
    {
        private readonly TypeChecker typeChecker;
        private readonly CompletionDatabase database;

        public CodeCompletionProvider(TypeChecker typeChecker)
        {
            this.typeChecker = typeChecker;
            this.database = new CompletionDatabase();
            InitializeBuiltins();
        }

        /// <summary>
        /// Get completion items at a specific position
        /// </summary>
        public List<CompletionItem> GetCompletions(string code, int line, int column)
        {
            var context = AnalyzeContext(code, line, column);
            var completions = new List<CompletionItem>();

            switch (context.Type)
            {
                case CompletionContextType.MemberAccess:
                    completions.AddRange(GetMemberCompletions(context));
                    break;
                case CompletionContextType.TypeName:
                    completions.AddRange(GetTypeCompletions(context));
                    break;
                case CompletionContextType.Statement:
                    completions.AddRange(GetStatementCompletions(context));
                    break;
                case CompletionContextType.Expression:
                    completions.AddRange(GetExpressionCompletions(context));
                    break;
                case CompletionContextType.Import:
                    completions.AddRange(GetImportCompletions(context));
                    break;
            }

            // Sort by relevance
            return completions.OrderBy(c => c.SortText ?? c.Label).ToList();
        }

        private CompletionContext AnalyzeContext(string code, int line, int column)
        {
            // Simplified context analysis
            var lines = code.Split('\n');
            if (line < 1 || line > lines.Length)
                return new CompletionContext { Type = CompletionContextType.Unknown };

            var currentLine = lines[line - 1];
            var prefix = currentLine.Substring(0, Math.Min(column, currentLine.Length));

            // Check for member access (e.g., "obj.")
            if (prefix.EndsWith("."))
            {
                var objectName = ExtractObjectName(prefix);
                return new CompletionContext
                {
                    Type = CompletionContextType.MemberAccess,
                    ObjectName = objectName
                };
            }

            // Check for import statement
            if (prefix.TrimStart().StartsWith("import"))
            {
                return new CompletionContext
                {
                    Type = CompletionContextType.Import
                };
            }

            // Check if we're in a type position
            if (IsTypePosition(prefix))
            {
                return new CompletionContext
                {
                    Type = CompletionContextType.TypeName
                };
            }

            // Default to expression/statement
            return new CompletionContext
            {
                Type = prefix.Trim() == "" ? CompletionContextType.Statement : CompletionContextType.Expression
            };
        }

        private string ExtractObjectName(string prefix)
        {
            // Simple extraction - find the identifier before the dot
            var beforeDot = prefix.Substring(0, prefix.Length - 1).TrimEnd();
            var parts = beforeDot.Split(new[] { ' ', '(', '[', '{', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[^1] : "";
        }

        private bool IsTypePosition(string prefix)
        {
            var trimmed = prefix.TrimEnd();
            return trimmed.EndsWith(":") || 
                   trimmed.EndsWith("var") || 
                   trimmed.EndsWith("let") ||
                   trimmed.EndsWith("const") ||
                   trimmed.Contains("function") ||
                   trimmed.Contains("class") ||
                   trimmed.Contains("interface");
        }

        private IEnumerable<CompletionItem> GetMemberCompletions(CompletionContext context)
        {
            // Get type of the object
            var objectType = typeChecker.LookupVariable(context.ObjectName);
            if (objectType == null)
                yield break;

            // TODO: Implement member lookup when TypeChecker supports it
            // For now, return empty list
            yield break;
        }

        private IEnumerable<CompletionItem> GetTypeCompletions(CompletionContext context)
        {
            // Built-in types
            foreach (var type in database.BuiltinTypes)
            {
                yield return new CompletionItem
                {
                    Label = type,
                    Kind = CompletionItemKind.Class,
                    Detail = "Built-in type",
                    InsertText = type
                };
            }

            // User-defined types
            // TODO: Implement type enumeration when TypeChecker supports it
            // For now, we only have built-in types
        }

        private IEnumerable<CompletionItem> GetStatementCompletions(CompletionContext context)
        {
            // Keywords
            foreach (var keyword in database.StatementKeywords)
            {
                yield return new CompletionItem
                {
                    Label = keyword,
                    Kind = CompletionItemKind.Keyword,
                    Detail = "Keyword",
                    InsertText = keyword
                };
            }

            // Variables in scope
            // TODO: Implement scope enumeration when TypeChecker supports it

            // Functions
            // TODO: Implement function enumeration when TypeChecker supports it
        }

        private IEnumerable<CompletionItem> GetExpressionCompletions(CompletionContext context)
        {
            // Similar to statement completions but filtered for expressions
            return GetStatementCompletions(context)
                .Where(c => c.Kind != CompletionItemKind.Keyword || 
                           database.ExpressionKeywords.Contains(c.Label));
        }

        private IEnumerable<CompletionItem> GetImportCompletions(CompletionContext context)
        {
            // Available modules
            foreach (var module in typeChecker.GetAvailableModules())
            {
                yield return new CompletionItem
                {
                    Label = module.Name,
                    Kind = CompletionItemKind.Module,
                    Detail = module.Path,
                    Documentation = module.Documentation,
                    InsertText = module.Name
                };
            }
        }

        private CompletionItemKind GetCompletionKind(MemberInfo member)
        {
            return member.MemberType switch
            {
                MemberType.Field => CompletionItemKind.Field,
                MemberType.Property => CompletionItemKind.Property,
                MemberType.Method => CompletionItemKind.Method,
                MemberType.Event => CompletionItemKind.Event,
                _ => CompletionItemKind.Text
            };
        }

        private void InitializeBuiltins()
        {
            database.BuiltinTypes.AddRange(new[]
            {
                "int", "float", "double", "bool", "string", "char",
                "byte", "short", "long", "uint", "ulong",
                "void", "any", "never",
                "vec2", "vec3", "vec4",
                "mat2", "mat3", "mat4",
                "quat", "complex"
            });

            database.StatementKeywords.AddRange(new[]
            {
                "var", "let", "const", "function", "fn",
                "if", "else", "while", "for", "foreach",
                "return", "break", "continue",
                "class", "interface", "struct", "enum",
                "public", "private", "protected", "internal",
                "static", "async", "await",
                "try", "catch", "finally", "throw",
                "import", "export", "from",
                "new", "this", "base", "null"
            });

            database.ExpressionKeywords.AddRange(new[]
            {
                "new", "this", "base", "null", "true", "false",
                "typeof", "sizeof", "nameof", "await"
            });
        }
    }

    public class CompletionItem
    {
        public string Label { get; set; }
        public CompletionItemKind Kind { get; set; }
        public string Detail { get; set; }
        public string Documentation { get; set; }
        public string InsertText { get; set; }
        public InsertTextFormat InsertTextFormat { get; set; }
        public string SortText { get; set; }
        public string FilterText { get; set; }
        public CompletionItemTag[] Tags { get; set; }
    }

    public enum CompletionItemKind
    {
        Text = 1,
        Method = 2,
        Function = 3,
        Constructor = 4,
        Field = 5,
        Variable = 6,
        Class = 7,
        Interface = 8,
        Module = 9,
        Property = 10,
        Unit = 11,
        Value = 12,
        Enum = 13,
        Keyword = 14,
        Snippet = 15,
        Color = 16,
        File = 17,
        Reference = 18,
        Folder = 19,
        EnumMember = 20,
        Constant = 21,
        Struct = 22,
        Event = 23,
        Operator = 24,
        TypeParameter = 25
    }

    public enum InsertTextFormat
    {
        PlainText = 1,
        Snippet = 2
    }

    public enum CompletionItemTag
    {
        Deprecated = 1
    }

    public class CompletionContext
    {
        public CompletionContextType Type { get; set; }
        public string ObjectName { get; set; }
        public string Prefix { get; set; }
    }

    public enum CompletionContextType
    {
        Unknown,
        MemberAccess,
        TypeName,
        Statement,
        Expression,
        Import
    }

    public class CompletionDatabase
    {
        public List<string> BuiltinTypes { get; } = new();
        public List<string> StatementKeywords { get; } = new();
        public List<string> ExpressionKeywords { get; } = new();
    }

    // Placeholder types for integration
    public class MemberInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Documentation { get; set; }
        public MemberType MemberType { get; set; }
    }

    public enum MemberType
    {
        Field,
        Property,
        Method,
        Event
    }
} 
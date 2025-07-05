using System;
using System.Collections.Generic;
using System.Linq;
using Ouro.Core.AST;
using Ouro.Core.Compiler;

namespace Ouro.IDE
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
            return completions.OrderBy(static c => c.SortText ?? c.Label).ToList();
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
            var objectType = typeChecker.LookupVariable(context.ObjectName!);
            if (objectType == null)
                yield break;

            // Check if it's a known type with members
            if (objectType.Name == "string")
            {
                // String members
                yield return new CompletionItem { Label = "Length", Kind = CompletionItemKind.Property, Detail = "int", Documentation = "Gets the number of characters in the string" };
                yield return new CompletionItem { Label = "ToUpper", Kind = CompletionItemKind.Method, Detail = "() => string", Documentation = "Returns a copy of this string converted to uppercase" };
                yield return new CompletionItem { Label = "ToLower", Kind = CompletionItemKind.Method, Detail = "() => string", Documentation = "Returns a copy of this string converted to lowercase" };
                yield return new CompletionItem { Label = "Substring", Kind = CompletionItemKind.Method, Detail = "(int start, int? length) => string", Documentation = "Retrieves a substring from this instance" };
                yield return new CompletionItem { Label = "IndexOf", Kind = CompletionItemKind.Method, Detail = "(string value) => int", Documentation = "Reports the zero-based index of the first occurrence of the specified string" };
            }
            else if (objectType.Name.StartsWith("vec") || objectType.Name.StartsWith("Vector"))
            {
                // Vector members
                yield return new CompletionItem { Label = "x", Kind = CompletionItemKind.Field, Detail = "float", Documentation = "X component of the vector" };
                yield return new CompletionItem { Label = "y", Kind = CompletionItemKind.Field, Detail = "float", Documentation = "Y component of the vector" };
                if (objectType.Name.Contains("3") || objectType.Name.Contains("4"))
                    yield return new CompletionItem { Label = "z", Kind = CompletionItemKind.Field, Detail = "float", Documentation = "Z component of the vector" };
                if (objectType.Name.Contains("4"))
                    yield return new CompletionItem { Label = "w", Kind = CompletionItemKind.Field, Detail = "float", Documentation = "W component of the vector" };
                yield return new CompletionItem { Label = "Length", Kind = CompletionItemKind.Property, Detail = "float", Documentation = "Gets the length of the vector" };
                yield return new CompletionItem { Label = "Normalize", Kind = CompletionItemKind.Method, Detail = "() => " + objectType.Name, Documentation = "Returns a normalized version of this vector" };
            }
            else if (objectType.Name.StartsWith("mat") || objectType.Name.StartsWith("Matrix"))
            {
                // Matrix members
                yield return new CompletionItem { Label = "Transpose", Kind = CompletionItemKind.Method, Detail = "() => " + objectType.Name, Documentation = "Returns the transpose of this matrix" };
                yield return new CompletionItem { Label = "Determinant", Kind = CompletionItemKind.Property, Detail = "float", Documentation = "Gets the determinant of the matrix" };
                yield return new CompletionItem { Label = "Inverse", Kind = CompletionItemKind.Method, Detail = "() => " + objectType.Name, Documentation = "Returns the inverse of this matrix" };
            }
            else if (objectType is ArrayTypeNode arrayType)
            {
                // Array members
                yield return new CompletionItem { Label = "Length", Kind = CompletionItemKind.Property, Detail = "int", Documentation = "Gets the total number of elements in the array" };
                yield return new CompletionItem { Label = "IndexOf", Kind = CompletionItemKind.Method, Detail = $"({arrayType.ElementType.Name} item) => int", Documentation = "Searches for the specified object and returns the index of its first occurrence" };
                yield return new CompletionItem { Label = "Contains", Kind = CompletionItemKind.Method, Detail = $"({arrayType.ElementType.Name} item) => bool", Documentation = "Determines whether an element is in the array" };
            }
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

            // Common user-defined type patterns
            yield return new CompletionItem
            {
                Label = "List<T>",
                Kind = CompletionItemKind.Class,
                Detail = "Generic list collection",
                InsertText = "List<${1:T}>",
                InsertTextFormat = InsertTextFormat.Snippet
            };
            
            yield return new CompletionItem
            {
                Label = "Dictionary<K,V>",
                Kind = CompletionItemKind.Class,
                Detail = "Generic dictionary collection",
                InsertText = "Dictionary<${1:K}, ${2:V}>",
                InsertTextFormat = InsertTextFormat.Snippet
            };
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
            // Since TypeChecker doesn't expose current scope, provide common variable patterns
            yield return new CompletionItem
            {
                Label = "i",
                Kind = CompletionItemKind.Variable,
                Detail = "int (loop variable)",
                InsertText = "i"
            };
            
            yield return new CompletionItem
            {
                Label = "result",
                Kind = CompletionItemKind.Variable,
                Detail = "var",
                InsertText = "result"
            };

            // Common functions
            yield return new CompletionItem
            {
                Label = "print",
                Kind = CompletionItemKind.Function,
                Detail = "(string message) => void",
                InsertText = "print",
                Documentation = "Prints a message to the console"
            };
            
            yield return new CompletionItem
            {
                Label = "println",
                Kind = CompletionItemKind.Function,
                Detail = "(string message) => void",
                InsertText = "println",
                Documentation = "Prints a message to the console with a newline"
            };

            // Common snippets
            yield return new CompletionItem
            {
                Label = "for",
                Kind = CompletionItemKind.Snippet,
                Detail = "for loop",
                InsertText = "for (int i = 0; i < $1; i++) {\n\t$0\n}",
                InsertTextFormat = InsertTextFormat.Snippet
            };

            yield return new CompletionItem
            {
                Label = "if",
                Kind = CompletionItemKind.Snippet,
                Detail = "if statement",
                InsertText = "if ($1) {\n\t$0\n}",
                InsertTextFormat = InsertTextFormat.Snippet
            };
        }

        private IEnumerable<CompletionItem> GetExpressionCompletions(CompletionContext context)
        {
            // Similar to statement completions but filtered for expressions
            return GetStatementCompletions(context)
                .Where(c => c.Kind != CompletionItemKind.Keyword || 
                           database.ExpressionKeywords.Contains(c.Label!));
        }

        private IEnumerable<CompletionItem> GetImportCompletions(CompletionContext context)
        {
            // Available modules
            // Return standard library modules for now
            yield return new CompletionItem { Label = "std.io", Kind = CompletionItemKind.Module, Detail = "I/O operations", InsertText = "std.io" };
            yield return new CompletionItem { Label = "std.math", Kind = CompletionItemKind.Module, Detail = "Mathematical functions", InsertText = "std.math" };
            yield return new CompletionItem { Label = "std.collections", Kind = CompletionItemKind.Module, Detail = "Collection types", InsertText = "std.collections" };
            yield return new CompletionItem { Label = "std.net", Kind = CompletionItemKind.Module, Detail = "Networking", InsertText = "std.net" };
            yield return new CompletionItem { Label = "std.ui", Kind = CompletionItemKind.Module, Detail = "UI framework", InsertText = "std.ui" };
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

        // Helper methods
        private bool IsTypeName(string name)
        {
            // Simple heuristic: type names start with uppercase
            return !string.IsNullOrEmpty(name) && char.IsUpper(name[0]);
        }

        private CompletionItemKind GetTypeKind(TypeNode type)
        {
            if (type == null) return CompletionItemKind.Class;
            
            // Determine kind based on type name patterns
            if (type.Name.Contains("interface") || type.Name.Contains("Interface"))
                return CompletionItemKind.Interface;
            if (type.Name.Contains("struct") || type.Name.Contains("Struct"))
                return CompletionItemKind.Struct;
            if (type.Name.Contains("enum") || type.Name.Contains("Enum"))
                return CompletionItemKind.Enum;
            
            return CompletionItemKind.Class;
        }

        private CompletionItemKind GetSymbolKind(TypeNode type)
        {
            if (type == null) return CompletionItemKind.Variable;
            
            if (type is FunctionTypeNode)
                return CompletionItemKind.Function;
            if (type.Name.EndsWith("[]") || type is ArrayTypeNode)
                return CompletionItemKind.Variable;
            
            return CompletionItemKind.Variable;
        }

        private string FormatType(TypeNode type)
        {
            if (type == null) return "unknown";
            
            if (type is FunctionTypeNode funcType)
            {
                var paramTypes = string.Join(", ", funcType.ParameterTypes.Select(static p => p.Name));
                return $"({paramTypes}) => {funcType.ReturnType.Name}";
            }
            
            if (type is ArrayTypeNode arrayType)
            {
                return $"{arrayType.ElementType.Name}[]";
            }
            
            return type.Name;
        }
    }

    public class CompletionItem
    {
        public string? Label { get; set; }
        public CompletionItemKind Kind { get; set; }
        public string? Detail { get; set; }
        public string? Documentation { get; set; }
        public string? InsertText { get; set; }
        public InsertTextFormat InsertTextFormat { get; set; }
        public string? SortText { get; set; }
        public string? FilterText { get; set; }
        public CompletionItemTag[]? Tags { get; set; }
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
        public string? ObjectName { get; set; }
        public string? Prefix { get; set; }
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
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Documentation { get; set; }
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
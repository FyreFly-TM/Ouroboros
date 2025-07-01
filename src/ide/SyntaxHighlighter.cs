using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Ouroboros.Core.Lexer;
using Ouroboros.Tokens;

namespace Ouroboros.IDE
{
    /// <summary>
    /// Syntax highlighter for Ouroboros code
    /// </summary>
    public class SyntaxHighlighter
    {
        private readonly Dictionary<TokenType, ConsoleColor> tokenColors = new()
        {
            // Literals
            { TokenType.StringLiteral, ConsoleColor.Green },
            { TokenType.IntegerLiteral, ConsoleColor.Cyan },
            { TokenType.FloatLiteral, ConsoleColor.Cyan },
            { TokenType.DoubleLiteral, ConsoleColor.Cyan },
            { TokenType.CharLiteral, ConsoleColor.Green },
            
            // Keywords - just a subset for now
            { TokenType.If, ConsoleColor.Blue },
            { TokenType.Else, ConsoleColor.Blue },
            { TokenType.While, ConsoleColor.Blue },
            { TokenType.For, ConsoleColor.Blue },
            { TokenType.Function, ConsoleColor.Blue },
            { TokenType.Return, ConsoleColor.Blue },
            { TokenType.Class, ConsoleColor.Blue },
            
            // Identifiers
            { TokenType.Identifier, ConsoleColor.White },
            
            // Operators
            { TokenType.Plus, ConsoleColor.Yellow },
            { TokenType.Minus, ConsoleColor.Yellow },
            { TokenType.Multiply, ConsoleColor.Yellow },
            { TokenType.Divide, ConsoleColor.Yellow },
            { TokenType.Assign, ConsoleColor.Yellow },
        };

        private readonly Dictionary<string, string> keywordColors;

        public SyntaxHighlighter()
        {
            // Initialize default color scheme
            tokenColors = new Dictionary<TokenType, string>
            {
                [TokenType.StringLiteral] = "#ce9178",
                [TokenType.IntegerLiteral] = "#b5cea8",
                [TokenType.DoubleLiteral] = "#b5cea8",
                [TokenType.FloatLiteral] = "#b5cea8",
                [TokenType.Comment] = "#6a9955",
                [TokenType.Identifier] = "#9cdcfe",
                [TokenType.UnitLiteral] = "#dcdcaa",
                [TokenType.BooleanLiteral] = "#569cd6",
                [TokenType.NullLiteral] = "#569cd6"
            };

            // Special keywords that need different colors
            keywordColors = new Dictionary<string, string>
            {
                ["class"] = "#569cd6",
                ["interface"] = "#569cd6",
                ["struct"] = "#569cd6",
                ["enum"] = "#569cd6",
                ["namespace"] = "#569cd6",
                ["function"] = "#dcdcaa",
                ["fn"] = "#dcdcaa",
                ["var"] = "#569cd6",
                ["let"] = "#569cd6",
                ["const"] = "#569cd6",
                ["if"] = "#c586c0",
                ["else"] = "#c586c0",
                ["while"] = "#c586c0",
                ["for"] = "#c586c0",
                ["return"] = "#c586c0",
                ["break"] = "#c586c0",
                ["continue"] = "#c586c0",
                ["true"] = "#569cd6",
                ["false"] = "#569cd6",
                ["null"] = "#569cd6",
                ["this"] = "#569cd6",
                ["base"] = "#569cd6",
                ["new"] = "#569cd6",
                ["typeof"] = "#569cd6",
                ["sizeof"] = "#569cd6",
                ["nameof"] = "#569cd6",
                ["public"] = "#569cd6",
                ["private"] = "#569cd6",
                ["protected"] = "#569cd6",
                ["internal"] = "#569cd6",
                ["static"] = "#569cd6",
                ["async"] = "#569cd6",
                ["await"] = "#569cd6",
                ["override"] = "#569cd6",
                ["virtual"] = "#569cd6",
                ["abstract"] = "#569cd6",
                ["sealed"] = "#569cd6"
            };
        }

        /// <summary>
        /// Highlight code and return HTML
        /// </summary>
        public string HighlightToHtml(string code)
        {
            var lexer = new Lexer(code, "highlight");
            var tokens = lexer.ScanTokens();
            var html = new List<string>();

            html.Add("<pre class=\"ouroboros-code\">");
            
            int lastEnd = 0;
            foreach (var token in tokens)
            {
                // Add any whitespace between tokens
                if (token.Position > lastEnd)
                {
                    var whitespace = code.Substring(lastEnd, token.Position - lastEnd);
                    html.Add(EscapeHtml(whitespace));
                }

                // Get color for token
                var color = GetTokenColor(token);
                
                // Add highlighted token
                if (!string.IsNullOrEmpty(color))
                {
                    html.Add($"<span style=\"color: {color}\">{EscapeHtml(token.Lexeme)}</span>");
                }
                else
                {
                    html.Add(EscapeHtml(token.Lexeme));
                }

                lastEnd = token.Position + token.Lexeme.Length;
            }

            // Add any remaining text
            if (lastEnd < code.Length)
            {
                html.Add(EscapeHtml(code.Substring(lastEnd)));
            }

            html.Add("</pre>");
            
            return string.Join("", html);
        }

        /// <summary>
        /// Get semantic tokens for language server
        /// </summary>
        public List<SemanticToken> GetSemanticTokens(string code)
        {
            var lexer = new Lexer(code, "semantic");
            var tokens = lexer.ScanTokens();
            var semanticTokens = new List<SemanticToken>();

            foreach (var token in tokens)
            {
                var tokenType = GetSemanticTokenType(token);
                if (tokenType != null)
                {
                    semanticTokens.Add(new SemanticToken
                    {
                        Line = token.Line,
                        StartChar = token.Column,
                        Length = token.Lexeme.Length,
                        TokenType = tokenType.Value,
                        TokenModifiers = GetTokenModifiers(token)
                    });
                }
            }

            return semanticTokens;
        }

        /// <summary>
        /// Get TextMate scopes for a token
        /// </summary>
        public string GetTextMateScope(Token token)
        {
            return token.Type switch
            {
                TokenType.STRING => "string.quoted.double.ouroboros",
                TokenType.NUMBER or TokenType.INTEGER or TokenType.DOUBLE or TokenType.FLOAT => "constant.numeric.ouroboros",
                TokenType.COMMENT => "comment.line.double-slash.ouroboros",
                TokenType.IDENTIFIER => IsType(token.Lexeme) ? "entity.name.type.ouroboros" : "variable.other.ouroboros",
                TokenType.KEYWORD => GetKeywordScope(token.Lexeme),
                TokenType.OPERATOR => "keyword.operator.ouroboros",
                TokenType.UNIT_LITERAL => "constant.other.unit.ouroboros",
                TokenType.BOOLEAN => "constant.language.boolean.ouroboros",
                TokenType.NullLiteral => "constant.language.null.ouroboros",
                _ => "source.ouroboros"
            };
        }

        private string GetKeywordScope(string keyword)
        {
            return keyword switch
            {
                "class" or "interface" or "struct" or "enum" => "storage.type.ouroboros",
                "function" or "fn" => "storage.type.function.ouroboros",
                "var" or "let" or "const" => "storage.type.ouroboros",
                "if" or "else" or "while" or "for" or "return" => "keyword.control.ouroboros",
                "public" or "private" or "protected" => "storage.modifier.ouroboros",
                "true" or "false" => "constant.language.boolean.ouroboros",
                "null" => "constant.language.null.ouroboros",
                "this" or "base" => "variable.language.ouroboros",
                _ => "keyword.other.ouroboros"
            };
        }

        private string GetTokenColor(Token token)
        {
            // Check if it's a special keyword first
            if (token.Type == TokenType.KEYWORD && keywordColors.ContainsKey(token.Lexeme))
            {
                return keywordColors[token.Lexeme];
            }

            // Check if it's an identifier that represents a type
            if (token.Type == TokenType.IDENTIFIER && IsType(token.Lexeme))
            {
                return tokenColors[TokenType.TYPE];
            }

            // Use default token color
            return tokenColors.ContainsKey(token.Type) ? tokenColors[token.Type] : null;
        }

        private bool IsType(string identifier)
        {
            // Simple heuristic: types start with uppercase
            return !string.IsNullOrEmpty(identifier) && char.IsUpper(identifier[0]);
        }

        private SemanticTokenType? GetSemanticTokenType(Token token)
        {
            return token.Type switch
            {
                TokenType.KEYWORD => SemanticTokenType.Keyword,
                TokenType.IDENTIFIER => IsType(token.Lexeme) ? SemanticTokenType.Type : SemanticTokenType.Variable,
                TokenType.STRING => SemanticTokenType.String,
                TokenType.NUMBER or TokenType.INTEGER or TokenType.DOUBLE or TokenType.FLOAT => SemanticTokenType.Number,
                TokenType.COMMENT => SemanticTokenType.Comment,
                TokenType.OPERATOR => SemanticTokenType.Operator,
                _ => null
            };
        }

        private SemanticTokenModifiers GetTokenModifiers(Token token)
        {
            var modifiers = SemanticTokenModifiers.None;

            // Add modifiers based on context
            // This would need more sophisticated analysis in a real implementation
            if (token.Type == TokenType.IDENTIFIER)
            {
                if (char.IsUpper(token.Lexeme[0]))
                {
                    modifiers |= SemanticTokenModifiers.Definition;
                }
            }

            return modifiers;
        }

        private string EscapeHtml(string text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }
    }

    /// <summary>
    /// Semantic token for LSP
    /// </summary>
    public class SemanticToken
    {
        public int Line { get; set; }
        public int StartChar { get; set; }
        public int Length { get; set; }
        public SemanticTokenType TokenType { get; set; }
        public SemanticTokenModifiers TokenModifiers { get; set; }
    }

    public enum SemanticTokenType
    {
        Namespace,
        Type,
        Class,
        Enum,
        Interface,
        Struct,
        TypeParameter,
        Parameter,
        Variable,
        Property,
        EnumMember,
        Event,
        Function,
        Method,
        Macro,
        Keyword,
        Modifier,
        Comment,
        String,
        Number,
        Regexp,
        Operator
    }

    [Flags]
    public enum SemanticTokenModifiers
    {
        None = 0,
        Declaration = 1 << 0,
        Definition = 1 << 1,
        Readonly = 1 << 2,
        Static = 1 << 3,
        Deprecated = 1 << 4,
        Abstract = 1 << 5,
        Async = 1 << 6,
        Modification = 1 << 7,
        Documentation = 1 << 8,
        DefaultLibrary = 1 << 9
    }
} 
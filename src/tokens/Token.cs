using System;

namespace Ouro.Tokens
{
    /// <summary>
    /// Represents a lexical token in the Ouroboros source code
    /// </summary>
    public class Token
    {
        public TokenType Type { get; }
        public string Lexeme { get; }
        public object Value { get; }
        public int Line { get; }
        public int Column { get; }
        public int StartPosition { get; }
        public int EndPosition { get; }
        public string FileName { get; }
        public SyntaxLevel SyntaxLevel { get; }

        public Token(TokenType type, string lexeme, object value, int line, int column, 
                    int startPos, int endPos, string fileName, SyntaxLevel syntaxLevel = SyntaxLevel.Medium)
        {
            Type = type;
            Lexeme = lexeme;
            Value = value;
            Line = line;
            Column = column;
            StartPosition = startPos;
            EndPosition = endPos;
            FileName = fileName;
            SyntaxLevel = syntaxLevel;
        }

        public override string ToString()
        {
            return $"[{Type}] '{Lexeme}' at {Line}:{Column} ({SyntaxLevel})";
        }

        public bool IsKeyword => Type >= TokenType.If && Type <= TokenType.Transform;
        public bool IsOperator => Type >= TokenType.Plus && Type <= TokenType.ReverseCompose;
        public bool IsGreekLetter => Type >= TokenType.Alpha && Type <= TokenType.Omega;
        public bool IsMathSymbol => Type >= TokenType.Infinity && Type <= TokenType.Tensor;
        public bool IsLiteral => Type >= TokenType.IntegerLiteral && Type <= TokenType.RawString;
        public bool IsAssembly => Type >= TokenType.AsmRegister && Type <= TokenType.AsmDirective;
    }

    /// <summary>
    /// Syntax levels for the Ouroboros language
    /// </summary>
    public enum SyntaxLevel
    {
        /// <summary>
        /// High-level syntax - most abstract, closest to natural language
        /// </summary>
        High,

        /// <summary>
        /// Medium-level syntax - balanced between abstraction and control
        /// </summary>
        Medium,

        /// <summary>
        /// Low-level syntax - close to hardware, manual memory management
        /// </summary>
        Low,

        /// <summary>
        /// Assembly-level syntax - direct hardware control
        /// </summary>
        Assembly
    }
} 
using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using Ouro.Tokens;

namespace Ouro.Core.Lexer
{
    /// <summary>
    /// The main lexer for the Ouro language
    /// Handles tokenization of all syntax levels including Greek symbols and math notation
    /// </summary>
    public class Lexer
    {
        private readonly string _source;
        private readonly string _fileName;
        private readonly List<Token> _tokens = new List<Token>();
        private readonly Dictionary<string, TokenType> _keywords;
        private readonly Dictionary<string, TokenType> _greekLetters;
        private readonly Dictionary<string, TokenType> _mathSymbols;
        
        private int _start = 0;
        private int _current = 0;
        private int _line = 1;
        private int _column = 1;
        private SyntaxLevel _currentSyntaxLevel = SyntaxLevel.Medium;

        public Lexer(string source, string fileName)
        {
            _source = source;
            _fileName = fileName;
            _keywords = InitializeKeywords();
            _greekLetters = InitializeGreekLetters();
            _mathSymbols = InitializeMathSymbols();
        }

        public List<Token> ScanTokens()
        {
            while (!IsAtEnd())
            {
                _start = _current;
                ScanToken();
            }

            _tokens.Add(new Token(TokenType.EndOfFile, "", null, _line, _column, 
                                 _current, _current, _fileName, _currentSyntaxLevel));
            return _tokens;
        }

        private void ScanToken()
        {
            char c = Advance();

            // Check for Unicode characters first (Greek letters and math notation)
            if (char.IsHighSurrogate(c) || c > 127)
            {
                HandleUnicodeCharacter(c);
                return;
            }

            switch (c)
            {
                // Whitespace
                case ' ':
                case '\r':
                case '\t':
                    break;
                case '\n':
                    _line++;
                    _column = 1;
                    break;

                // Single-character tokens
                case '(': AddToken(TokenType.LeftParen); break;
                case ')': AddToken(TokenType.RightParen); break;
                case '{': AddToken(TokenType.LeftBrace); break;
                case '}': AddToken(TokenType.RightBrace); break;
                case '[': AddToken(TokenType.LeftBracket); break;
                case ']': AddToken(TokenType.RightBracket); break;
                case ',': AddToken(TokenType.Comma); break;
                case ';': AddToken(TokenType.Semicolon); break;
                case '~': AddToken(TokenType.BitwiseNot); break;
                case '_': HandleUnderscore(); break;
                case '@': HandleSyntaxLevelOrAttribute(); break;
                case '#': 
                    // Check for #region or #endregion directives
                    if (IsAlpha(Peek()))
                    {
                        var savedPos = _current;
                        string word = ConsumeWord();
                        if (word == "region" || word == "endregion")
                        {
                            // Skip region directives - treat as comments
                            ScanRegionDirective();
                        }
                        else
                        {
                            // Not a region directive, restore position and emit hash
                            _current = savedPos;
                            AddToken(TokenType.Hash);
                        }
                    }
                    else
                    {
                        AddToken(TokenType.Hash);
                    }
                    break;
                case '$': 
                    if (Peek() == '"')
                    {
                        Advance(); // consume the '"'
                        HandleInterpolatedString();
                    }
                    else
                    {
                        AddToken(TokenType.Dollar);
                    }
                    break;
                case '`': AddToken(TokenType.Backtick); break;

                // Operators and multi-character tokens
                case '.':
                    if (Match('.'))
                    {
                        AddToken(Match('.') ? TokenType.Spread : TokenType.Range);
                    }
                    else if (IsDigit(Peek()))
                    {
                        // Check if this is tuple element access (e.g., a.1, tuple.0)
                        if (_tokens.Count > 0)
                        {
                            var lastToken = _tokens[_tokens.Count - 1];
                            // If the last token could be part of a tuple/member access expression
                            if (lastToken.Type == TokenType.Identifier || 
                                lastToken.Type == TokenType.RightParen ||
                                lastToken.Type == TokenType.RightBracket ||
                                lastToken.Type == TokenType.RightBrace ||
                                lastToken.Type == TokenType.IntegerLiteral ||
                                lastToken.Type == TokenType.FloatLiteral ||
                                lastToken.Type == TokenType.DoubleLiteral ||
                                lastToken.Type == TokenType.StringLiteral)
                            {
                                // This is member access with numeric field (tuple element)
                                AddToken(TokenType.Dot);
                            }
                            else
                            {
                                // This is a floating point number starting with dot
                                HandleNumber();
                            }
                        }
                        else
                        {
                            // No previous token, must be a float
                            HandleNumber();
                        }
                    }
                    else
                    {
                        AddToken(TokenType.Dot);
                    }
                    break;

                case '+':
                    if (Match('+')) AddToken(TokenType.Increment);
                    else if (Match('=')) AddToken(TokenType.PlusAssign);
                    else AddToken(TokenType.Plus);
                    break;

                case '-':
                    if (Match('-')) AddToken(TokenType.Decrement);
                    else if (Match('=')) AddToken(TokenType.MinusAssign);
                    else if (Match('>')) AddToken(TokenType.Arrow);
                    else AddToken(TokenType.Minus);
                    break;

                case '*':
                    if (Match('*'))
                    {
                        AddToken(Match('=') ? TokenType.PowerAssign : TokenType.Power);
                    }
                    else if (Match('='))
                    {
                        AddToken(TokenType.MultiplyAssign);
                    }
                    else
                    {
                        AddToken(TokenType.Multiply);
                    }
                    break;

                case '/':
                    if (Match('/'))
                    {
                        // Check if this is integer division by looking ahead past whitespace
                        // Be very conservative - only recognize as integer division in clear arithmetic contexts
                        int lookahead = _current;
                        int whitespaceCount = 0;
                        while (lookahead < _source.Length && char.IsWhiteSpace(_source[lookahead]))
                        {
                            whitespaceCount++;
                            lookahead++;
                        }

                        bool isIntegerDivision = false;
                        if (lookahead < _source.Length)
                        {
                            char nextChar = _source[lookahead];

                            // Enhanced detection: Only treat as integer division if:
                            // 1. Very little whitespace (max 1 space) - comments typically have more descriptive text
                            // 2. Followed by clear arithmetic patterns
                            if (whitespaceCount <= 1)
                            {
                                // Only treat as integer division if followed by: 
                                // - Single digits (numeric literals): 17 // 3
                                // - Opening parenthesis (grouped expressions): 17 // (a + b)
                                // - Unary operators (signed numbers): 17 // -3, 17 // +3
                                if ((IsDigit(nextChar) && IsSimpleNumber(lookahead)) || nextChar == '(' || nextChar == '-' || nextChar == '+')
                                {
                                    // Additional check: if followed by a digit, make sure it's not followed by letters
                                    // This helps distinguish "// 3" (integer division) from "// 32-bit" (comment)
                                    if (IsDigit(nextChar) && lookahead + 1 < _source.Length)
                                    {
                                        char afterDigit = _source[lookahead + 1];
                                        // If digit is followed by letters, it's likely descriptive text in a comment
                                        if (IsAlpha(afterDigit) || afterDigit == '-')
                                        {
                                            isIntegerDivision = false;
                                        }
                                        else
                                        {
                                            isIntegerDivision = true;
                                        }
                                    }
                                    else
                                    {
                                        isIntegerDivision = true;
                                    }
                                }
                            }
                            // - Very simple single-letter variable names (like x, y, a, b) followed by semicolon or operators
                            else if (IsAlpha(nextChar))
                            {
                                // Only consider single letters followed immediately by terminating characters
                                if (lookahead + 1 < _source.Length)
                                {
                                    char afterVar = _source[lookahead + 1];
                                    // Single letter variable followed by semicolon, operator, or whitespace then operator
                                    if (afterVar == ';' || afterVar == '+' || afterVar == '-' || 
                                        afterVar == '*' || afterVar == '/' || afterVar == ')' || 
                                        afterVar == ',' || afterVar == '\n')
                                    {
                                        isIntegerDivision = true;
                                    }
                                    // Single letter followed by whitespace, then check what comes after
                                    else if (char.IsWhiteSpace(afterVar))
                                    {
                                        int nextLookahead = lookahead + 2;
                                        while (nextLookahead < _source.Length && char.IsWhiteSpace(_source[nextLookahead]))
                                        {
                                            nextLookahead++;
                                        }
                                        if (nextLookahead < _source.Length)
                                        {
                                            char nextAfterSpace = _source[nextLookahead];
                                            if (nextAfterSpace == ';' || nextAfterSpace == '+' || nextAfterSpace == '-' || 
                                                nextAfterSpace == '*' || nextAfterSpace == '/' || nextAfterSpace == ')' || 
                                                nextAfterSpace == ',' || nextAfterSpace == '\n')
                                            {
                                                isIntegerDivision = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        
                        if (isIntegerDivision)
                        {
                            // Integer division operator
                            AddToken(TokenType.IntegerDivide);
                        }
                        else
                        {
                            // Likely a comment - consume until end of line
                            while (Peek() != '\n' && !IsAtEnd()) Advance();
                        }
                    }
                    else if (Match('*'))
                    {
                        // Multi-line comment
                        while (!IsAtEnd())
                        {
                            if (Peek() == '*' && PeekNext() == '/')
                            {
                                Advance(); // *
                                Advance(); // /
                                break;
                            }

                            if (Peek() == '\n')
                            {
                                _line++;
                                _column = 1;
                            }

                            Advance();
                        }
                    }
                    else if (Match('='))
                    {
                        AddToken(TokenType.DivideAssign);
                    }
                    else
                    {
                        AddToken(TokenType.Divide);
                    }
                    break;

                case '%':
                    AddToken(Match('=') ? TokenType.ModuloAssign : TokenType.Modulo);
                    break;

                case '&':
                    if (Match('&'))
                    {
                        AddToken(Match('=') ? TokenType.LogicalAndAssign : TokenType.LogicalAnd);
                    }
                    else if (Match('='))
                    {
                        AddToken(TokenType.BitwiseAndAssign);
                    }
                    else
                    {
                        AddToken(TokenType.BitwiseAnd);
                    }
                    break;

                case '\\':
                    // Set difference operator
                    AddToken(TokenType.SetDifference);
                    break;

                case '|':
                    if (Match('|'))
                    {
                        AddToken(TokenType.LogicalOr);
                    }
                    else if (Match('='))
                    {
                        AddToken(TokenType.BitwiseOrAssign);
                    }
                    else
                    {
                        AddToken(TokenType.BitwiseOr);
                    }
                    break;

                case '^':
                    if (Match('^'))
                    {
                        AddToken(TokenType.LogicalXor);
                    }
                    else if (Match('='))
                    {
                        AddToken(TokenType.BitwiseXorAssign);
                    }
                    else
                    {
                        AddToken(TokenType.BitwiseXor);
                    }
                    break;

                case '=':
                    if (Match('='))
                    {
                        AddToken(TokenType.Equal);
                    }
                    else if (Match('>'))
                    {
                        AddToken(TokenType.DoubleArrow);
                    }
                    else
                    {
                        AddToken(TokenType.Assign);
                    }
                    break;

                case '!':
                    AddToken(Match('=') ? TokenType.NotEqual : TokenType.LogicalNot);
                    break;

                case '<':
                    if (Match('<'))
                    {
                        if (Match('='))
                        {
                            AddToken(TokenType.LeftShiftAssign);
                        }
                        else
                        {
                            AddToken(TokenType.LeftShift);
                        }
                    }
                    else if (Match('='))
                    {
                        if (Match('>'))
                        {
                            AddToken(TokenType.Spaceship);
                        }
                        else
                        {
                            AddToken(TokenType.LessEqual);
                        }
                    }
                    else
                    {
                        AddToken(TokenType.Less);
                    }
                    break;

                case '>':
                    if (Match('>'))
                    {
                        if (Match('>'))
                        {
                            AddToken(TokenType.UnsignedRightShift);
                        }
                        else if (Match('='))
                        {
                            AddToken(TokenType.RightShiftAssign);
                        }
                        else
                        {
                            AddToken(TokenType.RightShift);
                        }
                    }
                    else if (Match('='))
                    {
                        AddToken(TokenType.GreaterEqual);
                    }
                    else
                    {
                        AddToken(TokenType.Greater);
                    }
                    break;

                case '?':
                    if (Match('?'))
                    {
                        AddToken(Match('=') ? TokenType.NullCoalesceAssign : TokenType.NullCoalesce);
                    }
                    else if (Match('.'))
                    {
                        AddToken(TokenType.NullConditional);
                    }
                    else
                    {
                        AddToken(TokenType.Question);
                    }
                    break;

                case ':':
                    if (Match(':'))
                    {
                        AddToken(TokenType.DoubleColon);
                    }
                    else if (Match('='))
                    {
                        AddToken(TokenType.Assign); // := is treated as assignment
                    }
                    else
                    {
                        AddToken(TokenType.Colon);
                    }
                    break;

                // String literals
                case '"':
                    HandleString();
                    break;

                case '\'':
                    // In high-level syntax mode, apostrophes might be possessive (person's)
                    if (_currentSyntaxLevel == SyntaxLevel.High && IsAlpha(Peek()))
                    {
                        // This looks like a possessive or contraction - treat as part of identifier
                        _current--; // Back up to include the apostrophe
                        HandleIdentifier();
                    }
                    else
                    {
                        HandleChar();
                    }
                    break;

                default:
                    if (IsDigit(c))
                    {
                        HandleNumber();
                    }
                    else if (IsAlpha(c))
                    {
                        HandleIdentifier();
                    }
                    else
                    {
                        ReportError($"Unexpected character: {c}");
                    }
                    break;
            }
        }

        private void HandleUnicodeCharacter(char c)
        {
            // Handle combining characters by looking ahead and grouping them with the base character
            var unicodeStr = c.ToString();
            
            // Special handling for combining characters that form vector notation
            if (!IsAtEnd() && char.GetUnicodeCategory(Peek()) == UnicodeCategory.NonSpacingMark)
            {
                // Combine the base character with the combining character(s)
                var combined = new StringBuilder();
                combined.Append(c);
                
                while (!IsAtEnd() && char.GetUnicodeCategory(Peek()) == UnicodeCategory.NonSpacingMark)
                {
                    combined.Append(Advance());
                }
                
                unicodeStr = combined.ToString();
            }
            
            // CRITICAL: Check for compound mathematical symbols FIRST before individual characters
            // This ensures σ² is recognized as one token, not σ followed by ²
            
            // Try to match longer compound symbols first by looking ahead
            string longestMatch = unicodeStr;
            TokenType? longestMatchType = null;
            
            // Build potential compound symbols by looking ahead for mathematical characters
            var potentialCompound = new StringBuilder();
            potentialCompound.Append(unicodeStr);
            
            int savePosition = _current;
            while (!IsAtEnd())
            {
                char next = Peek();
                var nextCategory = char.GetUnicodeCategory(next);
                
                // Include mathematical symbols, superscripts, subscripts, and other relevant characters
                if (next > 127 || 
                    char.IsSymbol(next) || 
                    char.IsNumber(next) ||
                    nextCategory == UnicodeCategory.LetterNumber ||
                    nextCategory == UnicodeCategory.OtherNumber ||
                    nextCategory == UnicodeCategory.NonSpacingMark ||
                    nextCategory == UnicodeCategory.MathSymbol ||
                    nextCategory == UnicodeCategory.OtherSymbol ||
                    nextCategory == UnicodeCategory.ModifierSymbol ||
                    nextCategory == UnicodeCategory.DecimalDigitNumber ||
                    (next >= '\u2070' && next <= '\u209F') || // Superscripts and subscripts block
                    (next >= '\u2080' && next <= '\u208E') || // Subscripts specifically  
                    (next >= '\u00B2' && next <= '\u00B3') || // Superscript 2 and 3
                    (next >= '\u00B9' && next <= '\u00B9'))   // Superscript 1
                {
                    potentialCompound.Append(next);
                    Advance();
                    
                    string compoundStr = potentialCompound.ToString();
                    
                    // Check if this compound string matches a defined math symbol
                    if (_mathSymbols.ContainsKey(compoundStr))
                    {
                        longestMatch = compoundStr;
                        longestMatchType = _mathSymbols[compoundStr];
                    }
                    else if (_greekLetters.ContainsKey(compoundStr))
                    {
                        longestMatch = compoundStr;
                        longestMatchType = _greekLetters[compoundStr];
                    }
                }
                else
                {
                    break;
                }
            }
            
            // Restore position to end of longest match
            _current = savePosition + (longestMatch.Length - unicodeStr.Length);
            
            // Use the longest match found, but check if it should be part of a larger identifier
            if (longestMatchType.HasValue)
            {
                // Check if there are identifier characters following the math symbol
                // If so, build the complete identifier instead of using the math symbol token
                if (!IsAtEnd() && (Peek() == '_' || IsAlphaNumeric(Peek()) || Peek() > 127))
                {
                    // Build complete identifier including the math symbol and following characters
                    var identifier = new StringBuilder();
                    identifier.Append(longestMatch);
                    
                    // Continue collecting identifier characters
                    while (!IsAtEnd())
                    {
                        char next = Peek();
                        var nextCategory = char.GetUnicodeCategory(next);
                        
                        // Be very inclusive for mathematical identifiers
                        if (next > 127 || // Any non-ASCII character
                            char.IsLetter(next) || 
                            char.IsDigit(next) ||
                            char.IsSymbol(next) || 
                            char.IsNumber(next) ||
                            nextCategory == UnicodeCategory.LetterNumber ||
                            nextCategory == UnicodeCategory.OtherNumber ||
                            nextCategory == UnicodeCategory.NonSpacingMark ||
                            nextCategory == UnicodeCategory.MathSymbol ||
                            nextCategory == UnicodeCategory.OtherSymbol ||
                            nextCategory == UnicodeCategory.ModifierSymbol ||
                            nextCategory == UnicodeCategory.DecimalDigitNumber ||
                            nextCategory == UnicodeCategory.EnclosingMark ||
                            nextCategory == UnicodeCategory.SpacingCombiningMark ||
                            nextCategory == UnicodeCategory.Format ||
                            (next >= '\u2070' && next <= '\u209F') || // Superscripts and subscripts block
                            (next >= '\u2080' && next <= '\u208E') || // Subscripts specifically  
                            (next >= '\u00B2' && next <= '\u00B3') || // Superscript 2 and 3
                            (next >= '\u00B9' && next <= '\u00B9') || // Superscript 1
                            next == '_')
                        {
                            identifier.Append(Advance());
                        }
                        else
                        {
                            break;
                        }
                    }
                    
                    // Always emit as identifier when mixed with identifier characters
                    AddToken(TokenType.Identifier, identifier.ToString());
                    return;
                }
                else
                {
                    // No following identifier characters, use the math symbol token
                    AddToken(longestMatchType.Value, longestMatch);
                    return;
                }
            }
            
            // Fall back to checking individual symbols
            // Check for math symbols first (before Greek letters to handle compound symbols)
            if (_mathSymbols.ContainsKey(unicodeStr))
            {
                AddToken(_mathSymbols[unicodeStr]);
                return;
            }
            
            // Check for Greek letters
            if (_greekLetters.ContainsKey(unicodeStr))
            {
                AddToken(_greekLetters[unicodeStr]);
                return;
            }

            // Handle all mathematical and scientific Unicode characters as identifiers
            // This is a comprehensive fallback for any Unicode character that could be used in mathematical notation
            var category = char.GetUnicodeCategory(c);
            
            // Be very permissive for Unicode characters - if it's any kind of symbol, letter, or number, treat as identifier
            if (c > 127 || // Any non-ASCII character
                char.IsLetter(c) || 
                char.IsSymbol(c) || 
                char.IsNumber(c) ||
                category == UnicodeCategory.LetterNumber ||
                category == UnicodeCategory.OtherNumber ||
                category == UnicodeCategory.NonSpacingMark ||
                category == UnicodeCategory.MathSymbol ||
                category == UnicodeCategory.OtherSymbol ||
                category == UnicodeCategory.ModifierSymbol ||
                category == UnicodeCategory.DecimalDigitNumber ||
                category == UnicodeCategory.EnclosingMark ||
                category == UnicodeCategory.SpacingCombiningMark ||
                category == UnicodeCategory.UppercaseLetter ||
                category == UnicodeCategory.LowercaseLetter ||
                category == UnicodeCategory.TitlecaseLetter ||
                category == UnicodeCategory.ModifierLetter ||
                category == UnicodeCategory.OtherLetter ||
                category == UnicodeCategory.Format || // Include formatting characters
                (c >= '\u2070' && c <= '\u209F') || // Superscripts and subscripts block
                (c >= '\u2080' && c <= '\u208E') || // Subscripts specifically  
                (c >= '\u00B2' && c <= '\u00B3') || // Superscript 2 and 3
                (c >= '\u00B9' && c <= '\u00B9')) // Superscript 1
            {
                // Special handling for specific operator symbols that should have their own token types
                switch (c)
                {
                    case '·': // U+00B7 - Middle dot (dot product)
                        AddToken(TokenType.Dot3D);
                        return;
                        
                    case '→': // U+2192 - Rightwards arrow
                        AddToken(TokenType.Arrow);
                        return;
                }
                
                // Create an identifier token for Unicode characters
                // Build complete Unicode identifier, including following combining characters
                var identifier = new StringBuilder();
                identifier.Append(unicodeStr);
                
                // Continue collecting Unicode identifier characters
                while (!IsAtEnd())
                {
                    char next = Peek();
                    var nextCategory = char.GetUnicodeCategory(next);
                    
                    // Be very inclusive for mathematical identifiers - include subscripts, superscripts, etc.
                    if (next > 127 || // Any non-ASCII character
                        char.IsLetter(next) || 
                        char.IsDigit(next) ||
                        char.IsSymbol(next) || 
                        char.IsNumber(next) ||
                        nextCategory == UnicodeCategory.LetterNumber ||
                        nextCategory == UnicodeCategory.OtherNumber ||
                        nextCategory == UnicodeCategory.NonSpacingMark ||
                        nextCategory == UnicodeCategory.MathSymbol ||
                        nextCategory == UnicodeCategory.OtherSymbol ||
                        nextCategory == UnicodeCategory.ModifierSymbol ||
                        nextCategory == UnicodeCategory.DecimalDigitNumber ||
                        nextCategory == UnicodeCategory.EnclosingMark ||
                        nextCategory == UnicodeCategory.SpacingCombiningMark ||
                        nextCategory == UnicodeCategory.Format || // Include formatting characters
                        (next >= '\u2070' && next <= '\u209F') || // Superscripts and subscripts block
                        (next >= '\u2080' && next <= '\u208E') || // Subscripts specifically  
                        (next >= '\u00B2' && next <= '\u00B3') || // Superscript 2 and 3
                        (next >= '\u00B9' && next <= '\u00B9') || // Superscript 1
                        next == '_')
                    {
                        identifier.Append(Advance());
                    }
                    else
                    {
                        break;
                    }
                }
                
                                string identifierText = identifier.ToString();
                
                // Check if it's a defined math symbol
                if (_mathSymbols.ContainsKey(identifierText))
                {
                    AddToken(_mathSymbols[identifierText], identifierText);
                }
                else if (_greekLetters.ContainsKey(identifierText))
                {
                    AddToken(_greekLetters[identifierText], identifierText);
                }
                else
                {
                    AddToken(TokenType.Identifier, identifierText);
                }
                return;
            }

            // If we really can't handle the Unicode character, but it's a Unicode character (> 127),
            // treat it as an identifier to be permissive with mathematical notation
            if (c > 127)
            {
                AddToken(TokenType.Identifier, unicodeStr);
                return;
            }

            // Only report error for non-Unicode characters that we truly can't handle
            ReportError($"Unexpected character: {c}");
        }

        private void HandleUnderscore()
        {
            // Check if underscore is standalone (wildcard pattern) or part of identifier
            char next = Peek();
            
            // If followed by alphanumeric character, treat as start of identifier
            if (IsAlphaNumeric(next))
            {
                // Back up and handle as identifier
                _current--;
                HandleIdentifier();
            }
            else
            {
                // Standalone underscore - wildcard pattern
                AddToken(TokenType.Underscore);
            }
        }

        private void HandleSyntaxLevelOrAttribute()
        {
            if (IsAlpha(Peek()))
            {
                int originalStart = _start;
                _start = _current;
                string word = ConsumeWord();
                
                switch (word)
                {
                    // Syntax Level Markers
                    case "high":
                        _currentSyntaxLevel = SyntaxLevel.High;
                        AddToken(TokenType.HighLevel);
                        break;
                    case "medium":
                        _currentSyntaxLevel = SyntaxLevel.Medium;
                        AddToken(TokenType.MediumLevel);
                        break;
                    case "low":
                        _currentSyntaxLevel = SyntaxLevel.Low;
                        AddToken(TokenType.LowLevel);
                        break;
                    case "asm":
                        // Check for compound attribute @asm spirv
                        if (PeekWord() == "spirv")
                        {
                            ConsumeWord(); // consume "spirv"
                            _currentSyntaxLevel = SyntaxLevel.Assembly; // SPIR-V is also assembly-level
                            AddToken(TokenType.SpirvAssembly);
                        }
                        else
                        {
                            _currentSyntaxLevel = SyntaxLevel.Assembly;
                            AddToken(TokenType.Assembly);
                        }
                        break;
                    case "assembly":
                        _currentSyntaxLevel = SyntaxLevel.Assembly;
                        AddToken(TokenType.Assembly);
                        break;
                        
                    // Compilation & Optimization Attributes
                    case "inline": AddToken(TokenType.Inline); break;
                    case "compile_time": AddToken(TokenType.CompileTime); break;
                    case "emit": AddToken(TokenType.Emit); break;
                    case "zero_cost": AddToken(TokenType.ZeroCost); break;
                    case "allocates": AddToken(TokenType.Allocates); break;
                    case "no_std": AddToken(TokenType.NoStd); break;
                    case "no_alloc": AddToken(TokenType.NoAlloc); break;
                    case "cfg": AddToken(TokenType.Cfg); break;
                    case "naked": AddToken(TokenType.Naked); break;
                    case "no_stack": AddToken(TokenType.NoStack); break;
                    case "no_mangle": AddToken(TokenType.NoMangle); break;
                    case "volatile": AddToken(TokenType.VolatileAttr); break;
                    case "packed": AddToken(TokenType.Packed); break;
                    case "section": AddToken(TokenType.Section); break;
                    case "repr": AddToken(TokenType.Repr); break;
                    
                    // GPU & Parallel Attributes
                    case "gpu": AddToken(TokenType.Gpu); break;
                    case "kernel": AddToken(TokenType.Kernel); break;
                    case "shared": AddToken(TokenType.Shared); break;
                    case "simd": AddToken(TokenType.Simd); break;
                    case "parallel": AddToken(TokenType.Parallel); break;
                    case "wasm_simd": AddToken(TokenType.WasmSimd); break;
                    
                    // Memory & Security Attributes
                    case "global_allocator": AddToken(TokenType.GlobalAllocator); break;
                    case "secure": AddToken(TokenType.Secure); break;
                    case "constant_time": AddToken(TokenType.ConstantTime); break;
                    
                    // Database Attributes
                    case "table": AddToken(TokenType.Table); break;
                    case "primary_key": AddToken(TokenType.PrimaryKey); break;
                    case "index": AddToken(TokenType.Index); break;
                    case "foreign_key": AddToken(TokenType.ForeignKey); break;
                    
                    // Real-time System Attributes
                    case "real_time": AddToken(TokenType.RealTime); break;
                    case "priority_ceiling": AddToken(TokenType.PriorityCeiling); break;
                    case "periodic": AddToken(TokenType.Periodic); break;
                    case "deadline": AddToken(TokenType.Deadline); break;
                    case "wcet": AddToken(TokenType.Wcet); break;
                    case "timed_section": AddToken(TokenType.TimedSection); break;
                    case "sporadic": AddToken(TokenType.Sporadic); break;
                    case "cyclic_executive": AddToken(TokenType.CyclicExecutive); break;
                    
                    // Verification Attributes
                    case "verified": AddToken(TokenType.Verified); break;
                    case "ghost": AddToken(TokenType.Ghost); break;
                    
                    // Machine Learning Attributes
                    case "differentiable": AddToken(TokenType.Differentiable); break;
                    case "model": AddToken(TokenType.Model); break;
                    
                    // Web/WASM Attributes
                    case "wasm": AddToken(TokenType.Wasm); break;
                    case "webgl": AddToken(TokenType.Webgl); break;
                    case "component": AddToken(TokenType.Component); break;
                    case "state": AddToken(TokenType.State); break;
                    case "import": AddToken(TokenType.ImportAttr); break;
                    
                    // Concurrency Attributes
                    case "actor": AddToken(TokenType.Actor); break;
                    case "receive": AddToken(TokenType.Receive); break;
                    case "supervisor": AddToken(TokenType.Supervisor); break;
                    
                    // Smart Contract Attributes
                    case "contract": AddToken(TokenType.Contract); break;
                    case "payable": AddToken(TokenType.Payable); break;
                    case "view": AddToken(TokenType.View); break;
                    case "external": AddToken(TokenType.External); break;
                    case "event": AddToken(TokenType.Event); break;
                    case "oracle": AddToken(TokenType.Oracle); break;
                    case "state_channel": AddToken(TokenType.StateChannel); break;
                    
                    // Scientific Computing Attributes
                    case "dna": AddToken(TokenType.Dna); break;
                    case "genomics": AddToken(TokenType.Genomics); break;
                    case "molecular_dynamics": AddToken(TokenType.MolecularDynamics); break;
                    case "mpc": AddToken(TokenType.Mpc); break;
                    case "zkp": AddToken(TokenType.Zkp); break;
                    case "spatial_index": AddToken(TokenType.SpatialIndex); break;
                    case "fixed_point": AddToken(TokenType.FixedPoint); break;
                    
                    // Graphics Attributes
                    case "shader": AddToken(TokenType.Shader); break;
                    
                    default:
                        // Unknown attribute - emit At token first, then reset to scan the identifier
                        int currentPos = _current;
                        _current = originalStart + 1; // Position after @
                        _start = originalStart;
                        AddToken(TokenType.At);
                        
                        // Now position to scan the identifier
                        _start = originalStart + 1;
                        _current = currentPos;
                        AddToken(TokenType.Identifier, word);
                        break;
                }
            }
            else
            {
                AddToken(TokenType.At);
            }
        }

        private void HandleString()
        {
            var value = new StringBuilder();
            bool isInterpolated = false;

            while (Peek() != '"' && !IsAtEnd())
            {
                if (Peek() == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else if (Peek() == '\\')
                {
                    Advance(); // consume backslash
                    if (!IsAtEnd())
                    {
                    char escaped = Advance();
                        switch (escaped)
                        {
                            case 'n': value.Append('\n'); break;
                            case 't': value.Append('\t'); break;
                            case 'r': value.Append('\r'); break;
                            case '\\': value.Append('\\'); break;
                            case '"': value.Append('"'); break;
                            case '\'': value.Append('\''); break;
                            case '0': value.Append('\0'); break;
                            default: 
                                // If not a recognized escape, keep both characters
                                value.Append('\\');
                                value.Append(escaped); 
                                break;
                        }
                    }
                }
                else if (Peek() == '$' && PeekNext() == '{')
                {
                    isInterpolated = true;
                    value.Append(Advance());
                }
                else
                {
                    value.Append(Advance());
                }
            }

            if (IsAtEnd())
            {
                ReportError("Unterminated string.");
                return;
            }

            // Consume closing "
            Advance();

            AddToken(isInterpolated ? TokenType.InterpolatedString : TokenType.StringLiteral, 
                    value.ToString());
        }

        private void HandleInterpolatedString()
        {
            var value = new StringBuilder();

            while (Peek() != '"' && !IsAtEnd())
            {
                if (Peek() == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else if (Peek() == '\\')
                {
                    Advance(); // consume backslash
                    if (!IsAtEnd())
                    {
                    char escaped = Advance();
                        switch (escaped)
                        {
                            case 'n': value.Append('\n'); break;
                            case 't': value.Append('\t'); break;
                            case 'r': value.Append('\r'); break;
                            case '\\': value.Append('\\'); break;
                            case '"': value.Append('"'); break;
                            case '\'': value.Append('\''); break;
                            case '0': value.Append('\0'); break;
                            default: 
                                // If not a recognized escape, keep both characters
                                value.Append('\\');
                                value.Append(escaped); 
                                break;
                        }
                    }
                }
                else if (Peek() == '{')
                {
                    // Found interpolation start
                    value.Append('{');
                    Advance(); // consume '{'
                    
                    // Handle nested braces inside interpolation
                    int braceCount = 1;
                    while (braceCount > 0 && !IsAtEnd())
                    {
                        if (Peek() == '{')
                        {
                            braceCount++;
                        }
                        else if (Peek() == '}')
                        {
                            braceCount--;
                        }
                        else if (Peek() == '"')
                        {
                            // Handle strings inside interpolations
                            value.Append(Advance());
                            while (Peek() != '"' && !IsAtEnd())
                            {
                                if (Peek() == '\\')
                                {
                                    value.Append(Advance());
                                    if (!IsAtEnd())
                                {
                                    value.Append(Advance());
                                }
                                }
                                else
                                {
                                value.Append(Advance());
                            }
                        }
                        }
                        if (!IsAtEnd())
                        {
                        value.Append(Advance());
                        }
                    }
                }
                else
                {
                    value.Append(Advance());
                }
            }

            if (IsAtEnd())
            {
                ReportError("Unterminated interpolated string.");
                return;
            }

            // Consume closing "
            Advance();

            AddToken(TokenType.InterpolatedString, value.ToString());
        }

        private void HandleChar()
        {
            char value;

            if (Peek() == '\\')
            {
                Advance(); // consume backslash
                value = GetEscapedChar(Advance());
            }
            else
            {
                value = Advance();
            }

            if (Peek() != '\'')
            {
                ReportError("Unterminated character literal.");
                return;
            }

            Advance(); // consume closing '
            AddToken(TokenType.CharLiteral, value);
        }

        private void HandleNumber()
        {

            // Handle hex, octal, and binary literals
            if (Peek() == '0' && !IsAtEnd())
            {
                char nextChar = PeekNext();
                if (nextChar == 'x' || nextChar == 'X')
                {
                    // Hexadecimal
                    Advance(); // consume '0'
                    Advance(); // consume 'x'
                    
                    if (!IsHexDigit(Peek()))
                    {
                        ReportError("Invalid hexadecimal literal");
                        return;
                    }
                    
                    while (IsHexDigit(Peek())) Advance();
                    
                    string hexText = _source.Substring(_start + 2, _current - _start - 2);
                    try
                    {
                        long hexValue = Convert.ToInt64(hexText, 16);
                        AddToken(TokenType.IntegerLiteral, hexValue);
                    }
                    catch (Exception)
                    {
                        ReportError($"Invalid hexadecimal number: 0x{hexText}");
                        AddToken(TokenType.IntegerLiteral, 0L);
                    }
                    return;
                }
                else if (nextChar == 'o' || nextChar == 'O')
                {
                    // Octal
                    Advance(); // consume '0'
                    Advance(); // consume 'o'
                    
                    if (!IsOctalDigit(Peek()))
                    {
                        ReportError("Invalid octal literal");
                        return;
                    }
                    
                    while (IsOctalDigit(Peek())) Advance();
                    
                    string octalText = _source.Substring(_start + 2, _current - _start - 2);
                    try
                    {
                        long octalValue = Convert.ToInt64(octalText, 8);
                        AddToken(TokenType.IntegerLiteral, octalValue);
                    }
                    catch (Exception)
                    {
                        ReportError($"Invalid octal number: 0o{octalText}");
                        AddToken(TokenType.IntegerLiteral, 0L);
                    }
                    return;
                }
                else if (nextChar == 'b' || nextChar == 'B')
                {
                    // Binary
                    Advance(); // consume '0'
                    Advance(); // consume 'b'
                    
                    if (Peek() != '0' && Peek() != '1')
                    {
                        ReportError("Invalid binary literal");
                        return;
                    }
                    
                    while (Peek() == '0' || Peek() == '1' || Peek() == '_') 
                    {
                        if (Peek() != '_') // Skip underscores in number literals
                            Advance();
                        else
                            Advance();
                    }
                    
                    string binaryText = _source.Substring(_start + 2, _current - _start - 2).Replace("_", "");
                    try
                    {
                        long binaryValue = Convert.ToInt64(binaryText, 2);
                        AddToken(TokenType.IntegerLiteral, binaryValue);
                    }
                    catch (Exception)
                    {
                        ReportError($"Invalid binary number: 0b{binaryText}");
                        AddToken(TokenType.IntegerLiteral, 0L);
                    }
                    return;
                }
            }

            bool isFloat = false;
            bool isHex = false;
            bool isBinary = false;
            bool isOctal = false;

            // Check for hex, binary, or octal
            if (_current > _start && _source[_start] == '0')
            {
                if (Peek() == 'x' || Peek() == 'X')
                {
                    isHex = true;
                    Advance();
                    while (IsHexDigit(Peek()) || Peek() == '_') Advance();
                }
                else if (Peek() == 'b' || Peek() == 'B')
                {
                    isBinary = true;
                    Advance();
                    while (Peek() == '0' || Peek() == '1' || Peek() == '_') Advance();
                }
                else if (IsDigit(Peek()))
                {
                    isOctal = true;
                    while (IsOctalDigit(Peek())) Advance();
                }
            }

            if (!isHex && !isBinary && !isOctal)
            {
                while (IsDigit(Peek())) Advance();

                // Look for a fractional part
                if (Peek() == '.' && IsDigit(PeekNext()))
                {
                    isFloat = true;
                    Advance(); // consume the '.'
                    while (IsDigit(Peek())) Advance();
                }

                // Look for scientific notation
                if (Peek() == 'e' || Peek() == 'E')
                {
                    isFloat = true;
                    Advance();
                    if (Peek() == '+' || Peek() == '-') Advance();
                    while (IsDigit(Peek())) Advance();
                }
            }

            // Type suffixes
            string text = _source.Substring(_start, _current - _start);
            TokenType type = TokenType.IntegerLiteral;
            object value = null;

            // Check for Rust-style type suffixes (f32, f64, i8, i16, i32, i64, u8, u16, u32, u64, usize, isize)
            string suffix = "";
            int suffixStart = _current;
            
            // First check for f32/f64 suffixes for floating point
            if ((Peek() == 'f' || Peek() == 'F') && !IsAtEnd())
            {
                char savedChar = Peek();
                int savedPos = _current;
                Advance(); // consume 'f'
                
                // Check for f32 or f64
                if (Peek() == '3' && PeekNext() == '2')
                {
                    Advance(); // consume '3'
                    Advance(); // consume '2'
                    suffix = "f32";
                    type = TokenType.FloatLiteral;
                    value = float.Parse(text, CultureInfo.InvariantCulture);
                }
                else if (Peek() == '6' && PeekNext() == '4')
                {
                    Advance(); // consume '6'
                    Advance(); // consume '4'
                    suffix = "f64";
                    type = TokenType.DoubleLiteral;
                    value = double.Parse(text, CultureInfo.InvariantCulture);
                }
                else
                {
                    // Just 'f' suffix
                    suffix = "f";
                    type = TokenType.FloatLiteral;
                    value = float.Parse(text, CultureInfo.InvariantCulture);
                }
            }
            // Check for integer type suffixes
            else if ((Peek() == 'i' || Peek() == 'I' || Peek() == 'u' || Peek() == 'U') && !IsAtEnd())
            {
                bool isUnsigned = (Peek() == 'u' || Peek() == 'U');
                Advance(); // consume 'i' or 'u'
                
                // Check for size suffix
                if (Peek() == '8')
                {
                    Advance();
                    suffix = isUnsigned ? "u8" : "i8";
                    value = isUnsigned ? (object)byte.Parse(text) : (object)sbyte.Parse(text);
                }
                else if (Peek() == '1' && PeekNext() == '6')
                {
                    Advance(); Advance();
                    suffix = isUnsigned ? "u16" : "i16";
                    value = isUnsigned ? (object)ushort.Parse(text) : (object)short.Parse(text);
                }
                else if (Peek() == '3' && PeekNext() == '2')
                {
                    Advance(); Advance();
                    suffix = isUnsigned ? "u32" : "i32";
                    value = isUnsigned ? (object)uint.Parse(text) : (object)int.Parse(text);
                }
                else if (Peek() == '6' && PeekNext() == '4')
                {
                    Advance(); Advance();
                    suffix = isUnsigned ? "u64" : "i64";
                    value = isUnsigned ? (object)ulong.Parse(text) : (object)long.Parse(text);
                }
                else if (Peek() == 's' && PeekNext() == 'i' && _current + 2 < _source.Length && 
                         _source[_current + 2] == 'z' && _current + 3 < _source.Length && 
                         _source[_current + 3] == 'e')
                {
                    Advance(); Advance(); Advance(); Advance(); // consume "size"
                    suffix = isUnsigned ? "usize" : "isize";
                    value = isUnsigned ? (object)ulong.Parse(text) : (object)long.Parse(text);
                }
                else
                {
                    // Just 'u' or 'i' suffix - rewind
                    _current = suffixStart;
                }
            }
            
            // If no Rust-style suffix was found, check for traditional C-style suffixes
            if (suffix == "")
            {
            // Check for unsigned/long suffixes first
            bool isUnsigned = false;
            bool isLong = false;
            
            // Look for U/u and L/l suffixes (can be combined as UL or LU)
            while (!IsAtEnd() && (Peek() == 'u' || Peek() == 'U' || Peek() == 'l' || Peek() == 'L'))
            {
                if (Peek() == 'u' || Peek() == 'U')
                {
                    isUnsigned = true;
                    Advance();
                }
                else if (Peek() == 'l' || Peek() == 'L')
                {
                    isLong = true;
                    Advance();
                }
            }

                if (Peek() == 'd' || Peek() == 'D')
            {
                Advance();
                type = TokenType.DoubleLiteral;
                value = double.Parse(text, CultureInfo.InvariantCulture);
            }
            else if (Peek() == 'm' || Peek() == 'M')
            {
                Advance();
                type = TokenType.DecimalLiteral;
                value = decimal.Parse(text, CultureInfo.InvariantCulture);
            }
            else if (isHex)
            {
                type = TokenType.HexLiteral;
                // Remove underscores and the 0x prefix before converting
                string hexDigits = text.Substring(2).Replace("_", "");
                if (isUnsigned && isLong)
                    value = Convert.ToUInt64(hexDigits, 16);
                else if (isUnsigned)
                    value = Convert.ToUInt32(hexDigits, 16);
                else if (isLong)
                    value = Convert.ToInt64(hexDigits, 16);
                else
                    value = Convert.ToInt64(hexDigits, 16);
            }
            else if (isBinary)
            {
                type = TokenType.BinaryLiteral;
                // Remove underscores and the 0b prefix before converting
                string binaryDigits = text.Substring(2).Replace("_", "");
                if (isUnsigned && isLong)
                    value = Convert.ToUInt64(binaryDigits, 2);
                else if (isUnsigned)
                    value = Convert.ToUInt32(binaryDigits, 2);
                else if (isLong)
                    value = Convert.ToInt64(binaryDigits, 2);
                else
                    value = Convert.ToInt64(binaryDigits, 2);
            }
            else if (isOctal)
            {
                type = TokenType.OctalLiteral;
                if (isUnsigned && isLong)
                    value = Convert.ToUInt64(text, 8);
                else if (isUnsigned)
                    value = Convert.ToUInt32(text, 8);
                else if (isLong)
                    value = Convert.ToInt64(text, 8);
                else
                value = Convert.ToInt64(text, 8);
            }
            else if (isFloat)
            {
                type = TokenType.DoubleLiteral;
                value = double.Parse(text, CultureInfo.InvariantCulture);
            }
            else
            {
                // Regular integer literal
                try
                {
                    if (isUnsigned && isLong)
                        value = ulong.Parse(text);
                    else if (isUnsigned)
                        value = uint.Parse(text);
                    else if (isLong)
                        value = long.Parse(text);
                    else
                        value = long.Parse(text);
                }
                catch (FormatException)
                {
                    ReportError($"Invalid number format: '{text}' at line {_line}");
                    value = 0L; // Default value
                }
                catch (OverflowException)
                {
                    ReportError($"Number overflow: '{text}' at line {_line}");
                    value = 0L; // Default value
                    }
                }
            }

            // Check for unit suffix after the number
            // Skip whitespace between number and unit
            int savedPosition = _current;
            while (!IsAtEnd() && char.IsWhiteSpace(Peek()) && Peek() != '\n')
            {
                Advance();
            }
            
            // Check if we have a physical unit identifier
            if (!IsAtEnd() && IsUnitIdentifierStart(Peek()))
            {
                int unitStart = _current;
                while (!IsAtEnd() && IsUnitIdentifierChar(Peek()))
                {
                    Advance();
                }
                string unitSuffix = _source.Substring(unitStart, _current - unitStart);
                
                // Only accept recognized physical units
                if (IsRecognizedUnit(unitSuffix))
                {
                    // Create a unit literal token with both the numeric value and unit
                    double numValue = value switch
                    {
                        float f => (double)f,
                        double d => d,
                        decimal dec => (double)dec,
                        long l => (double)l,
                        ulong ul => (double)ul,
                        int i => (double)i,
                        uint ui => (double)ui,
                        _ => Convert.ToDouble(value)
                    };
                    
                    var unitValue = new UnitLiteral(numValue, unitSuffix);
                    AddToken(TokenType.UnitLiteral, unitValue);
                    return;
                }
                else
                {
                    // Not a recognized unit, rewind
                    _current = savedPosition;
                }
            }

            AddToken(type, value);
        }

        private void HandleIdentifier()
        {
            while (IsAlphaNumeric(Peek()) || Peek() == '_' || 
                   (_currentSyntaxLevel == SyntaxLevel.High && Peek() == '\'')) 
            {
                Advance();
            }

            string text = _source.Substring(_start, _current - _start);
            
            // Check if it's a keyword
            if (_keywords.ContainsKey(text))
            {
                AddToken(_keywords[text]);
            }
            else if (text == "true" || text == "false")
            {
                AddToken(TokenType.BooleanLiteral, text == "true");
            }
            else if (text == "null")
            {
                AddToken(TokenType.NullLiteral, null);
            }
            else if (text == "e")
            {
                // Euler's number - treat as Epsilon token
                AddToken(TokenType.Epsilon);
            }
            else if (text == "c" || text == "G" || text == "h" || text == "k_B" || text == "N_A" || text == "R")
            {
                // Physics constants - treat as identifiers
                AddToken(TokenType.Identifier);
            }
            else
            {
                AddToken(TokenType.Identifier);
            }
        }

        private void HandleDocumentationComment()
        {
            var comment = new StringBuilder("///");
            
            while (Peek() != '\n' && !IsAtEnd())
            {
                comment.Append(Advance());
            }

            AddToken(TokenType.DocumentationComment, comment.ToString());
        }

        private void HandleMultiLineComment()
        {
            var comment = new StringBuilder("/*");
            
            while (!IsAtEnd())
            {
                if (Peek() == '*' && PeekNext() == '/')
                {
                    comment.Append(Advance()); // *
                    comment.Append(Advance()); // /
                    break;
                }

                if (Peek() == '\n')
                {
                    _line++;
                    _column = 1;
                }

                comment.Append(Advance());
            }

            AddToken(TokenType.MultiLineComment, comment.ToString());
        }

        private Token ScanString(char quote)
        {
            var value = new StringBuilder();
            var start = _current;
            
            while (Peek() != quote && !IsAtEnd())
            {
                if (Peek() == '\\')
                {
                    // Handle escape sequences
                    Advance(); // consume backslash
                    if (IsAtEnd()) break;
                    
                    char escaped = Advance();
                    switch (escaped)
                    {
                        case 'n': value.Append('\n'); break;
                        case 't': value.Append('\t'); break;
                        case 'r': value.Append('\r'); break;
                        case '\\': value.Append('\\'); break;
                        case '"': value.Append('"'); break;
                        case '\'': value.Append('\''); break;
                        case '0': value.Append('\0'); break;
                        default: 
                            // If not a recognized escape, keep both characters
                            value.Append('\\');
                            value.Append(escaped); 
                            break;
                    }
                }
                else if (Peek() == '\n')
                {
                    _line++;
                    value.Append(Advance());
                }
                else
                {
                    value.Append(Advance());
                }
            }

            if (IsAtEnd())
            {
                ReportError($"Unterminated string at line {_line}");
                return new Token(TokenType.StringLiteral, "", "", _line, 0, start, _current, _source, _currentSyntaxLevel);
            }

            // The closing quote
            Advance();

            return new Token(TokenType.StringLiteral, _source.Substring(start, _current - start), 
                           value.ToString(), _line, start, start, _current, _source, _currentSyntaxLevel);
        }
        
        private void ScanRegionDirective()
        {
            // Skip the rest of the line (region directives are single-line)
            while (Peek() != '\n' && !IsAtEnd())
            {
                Advance();
            }
            // The newline will be consumed in the next iteration of the main loop
        }

        #region Helper Methods

        private bool IsAtEnd() => _current >= _source.Length;
        private char Advance()
        {
            _column++;
            return _source[_current++];
        }
        private bool Match(char expected)
        {
            if (IsAtEnd()) return false;
            if (_source[_current] != expected) return false;
            _current++;
            _column++;
            return true;
        }
        private char Peek() => IsAtEnd() ? '\0' : _source[_current];
        private char PeekNext() => _current + 1 >= _source.Length ? '\0' : _source[_current + 1];
        
        private bool IsDigit(char c) => c >= '0' && c <= '9';
        private bool IsHexDigit(char c) => IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
        private bool IsOctalDigit(char c) => c >= '0' && c <= '7';
        private bool IsAlpha(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';
        private bool IsAlphaNumeric(char c) => IsAlpha(c) || IsDigit(c) || IsCombiningCharacter(c) || IsSubscriptOrSuperscript(c);

        private bool IsCombiningCharacter(char c)
        {
            // Check for Unicode combining characters used in mathematical notation
            // These should be treated as part of identifiers
            var category = char.GetUnicodeCategory(c);
            return category == System.Globalization.UnicodeCategory.NonSpacingMark ||
                   category == System.Globalization.UnicodeCategory.SpacingCombiningMark ||
                   category == System.Globalization.UnicodeCategory.EnclosingMark;
        }

        private bool IsSubscriptOrSuperscript(char c)
        {
            // Check for subscript digits (₀-₉)
            if (c >= '\u2080' && c <= '\u2089') return true;
            
            // Check for superscript digits (⁰-⁹) and other common superscripts
            if (c >= '\u2070' && c <= '\u2079') return true;
            
            // Additional subscript and superscript characters
            switch (c)
            {
                case '\u00B2': // ²
                case '\u00B3': // ³
                case '\u2074': // ⁴
                case '\u2075': // ⁵
                case '\u2076': // ⁶
                case '\u2077': // ⁷
                case '\u2078': // ⁸
                case '\u2079': // ⁹
                case '\u207A': // ⁺
                case '\u207B': // ⁻
                case '\u207C': // ⁼
                case '\u207D': // ⁽
                case '\u207E': // ⁾
                case '\u2080': // ₀
                case '\u2081': // ₁
                case '\u2082': // ₂
                case '\u2083': // ₃
                case '\u2084': // ₄
                case '\u2085': // ₅
                case '\u2086': // ₆
                case '\u2087': // ₇
                case '\u2088': // ₈
                case '\u2089': // ₉
                case '\u208A': // ₊
                case '\u208B': // ₋
                case '\u208C': // ₌
                case '\u208D': // ₍
                case '\u208E': // ₎
                    return true;
            }
            
            return false;
        }

        private bool IsSimpleNumber(int startIndex)
        {
            // Check if this looks like a simple number (for integer division) vs descriptive text (comment)
            int index = startIndex;
            
            // Skip digits
            while (index < _source.Length && IsDigit(_source[index]))
            {
                index++;
            }
            
            // If immediately followed by letters or hyphens, it's likely descriptive text like "32-bit"
            if (index < _source.Length)
            {
                char afterNumber = _source[index];
                if (IsAlpha(afterNumber) || afterNumber == '-')
                {
                    return false; // Not a simple number - likely "32-bit" etc.
                }
            }
            
            return true; // Simple number like "3"
        }

        private bool IsUnitIdentifierStart(char c) => IsAlpha(c) || c == 'Ω' || c == '°' || c == 'μ';
        
        private bool IsUnitIdentifierChar(char c) => IsAlpha(c) || IsDigit(c) || c == '_' || c == '/' || c == '²' || c == '³' || c == '·';
        
        private bool IsRecognizedUnit(string unit)
        {
            // Physical units recognized by Ouroboros
            var units = new HashSet<string>
            {
                // Electrical units
                "V", "A", "Ω", "W", "Wh", "kWh", "VA", "VAR", "F", "H", "S",
                "mV", "kV", "mA", "kA", "mW", "kW", "MW", "µF", "mH", "µH",
                
                // Frequency
                "Hz", "kHz", "MHz", "GHz", "THz",
                
                // Time
                "s", "ms", "µs", "ns", "ps", "min", "h", "d",
                
                // Length
                "m", "mm", "cm", "km", "µm", "nm", "pm", "in", "ft", "yd", "mi",
                
                // Mass
                "g", "kg", "mg", "µg", "t", "lb", "oz",
                
                // Temperature
                "K", "°C", "°F", "°R",
                
                // Force
                "N", "kN", "lbf",
                
                // Pressure
                "Pa", "kPa", "MPa", "GPa", "bar", "mbar", "atm", "psi", "Torr",
                
                // Energy
                "J", "kJ", "MJ", "cal", "kcal", "eV", "keV", "MeV", "GeV",
                
                // Information
                "bit", "B", "KB", "MB", "GB", "TB", "PB", "Kbit", "Mbit", "Gbit",
                
                // Angles
                "rad", "deg", "°", "grad", "arcmin", "arcsec",
                
                // Area
                "m²", "cm²", "mm²", "km²", "ft²", "in²",
                
                // Volume
                "m³", "cm³", "mm³", "L", "mL", "gal", "qt", "pt", "fl oz",
                
                // Speed
                "m/s", "km/h", "mph", "ft/s", "knot",
                
                // Acceleration
                "m/s²", "g",
                
                // Other
                "mol", "cd", "lm", "lx"
            };
            
            return units.Contains(unit);
        }

        private string ConsumeWord()
        {
            var start = _current;
            while (IsAlphaNumeric(Peek()) && !IsAtEnd())
            {
                Advance();
            }
            return _source.Substring(start, _current - start);
        }
        
        private string PeekWord()
        {
            // Skip whitespace to find next word
            int pos = _current;
            while (pos < _source.Length && char.IsWhiteSpace(_source[pos]))
            {
                pos++;
            }
            
            // If no characters left or not start of identifier, return empty
            if (pos >= _source.Length || !IsAlpha(_source[pos]))
            {
                return "";
            }
            
            // Extract the word
            int start = pos;
            while (pos < _source.Length && IsAlphaNumeric(_source[pos]))
            {
                pos++;
            }
            
            return _source.Substring(start, pos - start);
        }

        private char GetEscapedChar(char c)
        {
            switch (c)
            {
                case 'n': return '\n';
                case 'r': return '\r';
                case 't': return '\t';
                case 'b': return '\b';
                case 'f': return '\f';
                case '0': return '\0';
                case '\\': return '\\';
                case '"': return '"';
                case '\'': return '\'';
                default: return c;
            }
        }

        private void AddToken(TokenType type) => AddToken(type, null);
        private void AddToken(TokenType type, object value)
        {
            string text = _source.Substring(_start, _current - _start);
            _tokens.Add(new Token(type, text, value, _line, _column - text.Length, 
                                 _start, _current, _fileName, _currentSyntaxLevel));
        }

        private void ReportError(string message)
        {
            Console.Error.WriteLine($"[{_fileName}:{_line}:{_column}] Error: {message}");
        }

        #endregion

        #region Initialization

        private Dictionary<string, TokenType> InitializeKeywords()
        {
            return new Dictionary<string, TokenType>
            {
                // Traditional keywords
                ["class"] = TokenType.Class,
                ["interface"] = TokenType.Interface,
                ["struct"] = TokenType.Struct,
                ["union"] = TokenType.UnionKeyword,
                ["enum"] = TokenType.Enum,
                ["namespace"] = TokenType.Namespace,
                ["module"] = TokenType.Module,
                ["package"] = TokenType.Package,
                ["import"] = TokenType.Import,
                ["export"] = TokenType.Export,
                ["using"] = TokenType.Using,
                ["alias"] = TokenType.Alias,
                ["typedef"] = TokenType.Typedef,
                ["function"] = TokenType.Function,
                ["domain"] = TokenType.Domain,
                
                // Control flow
                ["if"] = TokenType.If,
                ["else"] = TokenType.Else,
                ["elseif"] = TokenType.ElseIf,
                ["switch"] = TokenType.Switch,
                ["case"] = TokenType.Case,
                ["default"] = TokenType.Default,
                ["for"] = TokenType.For,
                ["foreach"] = TokenType.ForEach,
                ["forin"] = TokenType.ForIn,
                ["forof"] = TokenType.ForOf,
                ["while"] = TokenType.While,
                ["do"] = TokenType.Do,
                ["loop"] = TokenType.Loop,
                ["until"] = TokenType.Until,
                ["break"] = TokenType.Break,
                ["continue"] = TokenType.Continue,
                ["return"] = TokenType.Return,
                ["yield"] = TokenType.Yield,
                ["await"] = TokenType.Await,
                ["async"] = TokenType.Async,
                ["match"] = TokenType.Match,
                ["when"] = TokenType.When,
                ["with"] = TokenType.With,
                
                // Modifiers
                ["public"] = TokenType.Public,
                ["private"] = TokenType.Private,
                ["protected"] = TokenType.Protected,
                ["internal"] = TokenType.Internal,
                ["static"] = TokenType.Static,
                ["final"] = TokenType.Final,
                ["const"] = TokenType.Const,
                ["readonly"] = TokenType.Readonly,
                ["volatile"] = TokenType.Volatile,
                ["abstract"] = TokenType.Abstract,
                ["virtual"] = TokenType.Virtual,
                ["override"] = TokenType.Override,
                ["sealed"] = TokenType.Sealed,
                ["partial"] = TokenType.Partial,
                
                // Types
                ["type"] = TokenType.Type,
                ["var"] = TokenType.Var,
                ["let"] = TokenType.Let,
                ["void"] = TokenType.Void,
                ["bool"] = TokenType.Bool,
                ["byte"] = TokenType.Byte,
                ["sbyte"] = TokenType.SByte,
                ["short"] = TokenType.Short,
                ["ushort"] = TokenType.UShort,
                ["u8"] = TokenType.Byte,        // Rust-style byte
                ["u16"] = TokenType.UShort,     // Rust-style unsigned short
                ["u32"] = TokenType.UInt,       // Rust-style unsigned int
                ["u64"] = TokenType.ULong,      // Rust-style unsigned long
                ["i8"] = TokenType.SByte,       // Rust-style signed byte
                ["i16"] = TokenType.Short,      // Rust-style signed short
                ["i32"] = TokenType.Int,        // Rust-style signed int
                ["i64"] = TokenType.Long,       // Rust-style signed long
                ["int"] = TokenType.Int,
                ["uint"] = TokenType.UInt,
                ["long"] = TokenType.Long,
                ["ulong"] = TokenType.ULong,
                ["float"] = TokenType.Float,
                ["double"] = TokenType.Double,
                ["decimal"] = TokenType.Decimal,
                ["char"] = TokenType.Char,
                ["string"] = TokenType.String,
                ["object"] = TokenType.Object,
                ["dynamic"] = TokenType.Dynamic,
                ["any"] = TokenType.Any,
                
                // Memory
                ["new"] = TokenType.New,
                ["delete"] = TokenType.Delete,
                ["malloc"] = TokenType.Malloc,
                ["free"] = TokenType.Free,
                ["sizeof"] = TokenType.Sizeof,
                ["typeof"] = TokenType.Typeof,
                ["nameof"] = TokenType.Nameof,
                ["stackalloc"] = TokenType.Stackalloc,
                
                // Exception handling
                ["try"] = TokenType.Try,
                ["catch"] = TokenType.Catch,
                ["finally"] = TokenType.Finally,
                ["throw"] = TokenType.Throw,
                ["throws"] = TokenType.Throws,
                
                // Special
                ["this"] = TokenType.This,
                ["base"] = TokenType.Base,
                ["super"] = TokenType.Super,
                ["self"] = TokenType.Self,
                ["is"] = TokenType.Is,
                ["as"] = TokenType.As,
                ["in"] = TokenType.In,
                ["out"] = TokenType.Out,
                ["ref"] = TokenType.Ref,
                ["params"] = TokenType.Params,
                
                // Custom loops
                ["iterate"] = TokenType.Iterate,
                ["repeat"] = TokenType.Repeat,
                ["forever"] = TokenType.Forever,
                
                // Data-oriented programming
                ["component"] = TokenType.Component,
                ["system"] = TokenType.System,
                ["entity"] = TokenType.Entity,
                ["data"] = TokenType.Data,
                
                // Memory management
                ["pin"] = TokenType.Pin,
                ["unpin"] = TokenType.Unpin,
                ["unsafe"] = TokenType.Unsafe,
                ["fixed"] = TokenType.Fixed,
                
                // Concurrency
                ["thread"] = TokenType.Thread,
                ["thread_local"] = TokenType.ThreadLocal,
                ["lock"] = TokenType.Lock,
                ["atomic"] = TokenType.Atomic,
                ["channel"] = TokenType.Channel,
                ["select"] = TokenType.Select,
                ["go"] = TokenType.Go,
                ["repr"] = TokenType.Repr,         // For @repr attribute
                
                // Math types
                ["vector"] = TokenType.Vector,
                ["matrix"] = TokenType.Matrix,
                ["quaternion"] = TokenType.Quaternion,
                ["transform"] = TokenType.Transform,
                
                // Contracts
                ["requires"] = TokenType.Requires,
                ["ensures"] = TokenType.Ensures,
                ["invariant"] = TokenType.Invariant,
                
                // Meta programming
                ["macro"] = TokenType.Macro,
                ["template"] = TokenType.Template,
                ["generic"] = TokenType.Generic,
                ["concept"] = TokenType.Concept,
                
                // Assembly
                ["assembly"] = TokenType.Assembly,
                
                // Natural Language Keywords (High-Level Syntax)
                ["print"] = TokenType.Print,
                ["define"] = TokenType.Define,
                ["taking"] = TokenType.Taking,
                ["through"] = TokenType.Through,
                ["from"] = TokenType.From,
                ["to"] = TokenType.To,
                ["end"] = TokenType.End,
                ["then"] = TokenType.Then,
                ["otherwise"] = TokenType.Otherwise,
                ["each"] = TokenType.Each,
                ["all"] = TokenType.All,
                ["where"] = TokenType.Where,
                ["item"] = TokenType.Item,
                ["numbers"] = TokenType.Numbers,
                ["even"] = TokenType.Even,
                ["odd"] = TokenType.Odd,
                ["multiplied"] = TokenType.Multiplied,
                ["by"] = TokenType.By,
                ["divided"] = TokenType.Divided,
                ["counter"] = TokenType.Counter,
                ["than"] = TokenType.Than,
                ["length"] = TokenType.Length,
                ["width"] = TokenType.Width,
                ["area"] = TokenType.Area,
                ["error"] = TokenType.Error,
                ["cannot"] = TokenType.Cannot,
                ["times"] = TokenType.Times,
                ["greater"] = TokenType.Greater, // For natural language "is greater than"
                
                // Mathematical Expression Keywords
                ["limit"] = TokenType.Limit,
                ["lim"] = TokenType.Limit,      // Alternative
                ["origin"] = TokenType.Origin,
                ["means"] = TokenType.Means,
                ["at"] = TokenType.At,
                ["approaches"] = TokenType.Approaches,
                
                // Boolean literals
                ["true"] = TokenType.BooleanLiteral,
                ["false"] = TokenType.BooleanLiteral,
                ["null"] = TokenType.NullLiteral,
                ["nil"] = TokenType.NullLiteral,
            };
        }

        private Dictionary<string, TokenType> InitializeGreekLetters()
        {
            return new Dictionary<string, TokenType>
            {
                { "α", TokenType.Alpha },
                { "β", TokenType.Beta },
                { "γ", TokenType.Gamma },
                { "δ", TokenType.Delta },
                { "ε", TokenType.Epsilon },
                { "ζ", TokenType.Zeta },
                { "η", TokenType.Eta },
                { "θ", TokenType.Theta },
                { "ι", TokenType.Iota },
                { "κ", TokenType.Kappa },
                { "λ", TokenType.Lambda },
                { "μ", TokenType.Mu },
                { "ν", TokenType.Nu },
                { "ξ", TokenType.Xi },
                { "ο", TokenType.Omicron },
                { "π", TokenType.Pi },
                { "ρ", TokenType.Rho },
                { "σ", TokenType.Sigma },
                { "τ", TokenType.Tau },
                { "υ", TokenType.Upsilon },
                { "φ", TokenType.Phi },
                { "χ", TokenType.Chi },
                { "ψ", TokenType.Psi },
                { "ω", TokenType.Omega },
                // Capital Greek letters
                { "Α", TokenType.Alpha },
                { "Β", TokenType.Beta },
                { "Γ", TokenType.Gamma },
                { "Δ", TokenType.Delta },
                { "Ε", TokenType.Epsilon },
                { "Ζ", TokenType.Zeta },
                { "Η", TokenType.Eta },
                { "Θ", TokenType.Theta },
                { "Ι", TokenType.Iota },
                { "Κ", TokenType.Kappa },
                { "Λ", TokenType.Lambda },
                { "Μ", TokenType.Mu },
                { "Ν", TokenType.Nu },
                { "Ξ", TokenType.Xi },
                { "Ο", TokenType.Omicron },
                { "Π", TokenType.Pi },
                { "Ρ", TokenType.Rho },
                { "Σ", TokenType.Sigma },
                { "Τ", TokenType.Tau },
                { "Υ", TokenType.Upsilon },
                { "Φ", TokenType.Phi },
                { "Χ", TokenType.Chi },
                { "Ψ", TokenType.Psi },
                { "Ω", TokenType.Omega }
            };
        }

        private Dictionary<string, TokenType> InitializeMathSymbols()
        {
            return new Dictionary<string, TokenType>
            {
                // Basic mathematical symbols
                ["∞"] = TokenType.Infinity,
                ["±"] = TokenType.PlusMinus,
                ["∓"] = TokenType.MinusPlus,
                ["×"] = TokenType.Times,
                ["÷"] = TokenType.DivisionSign,
                ["≠"] = TokenType.NotEqual2,
                ["≤"] = TokenType.LessOrEqual,
                ["≥"] = TokenType.GreaterOrEqual,
                ["≈"] = TokenType.Almost,
                ["≉"] = TokenType.NotAlmost,
                ["≡"] = TokenType.Identical,
                ["≢"] = TokenType.NotIdentical,
                ["∝"] = TokenType.Proportional,
                
                // Set theory
                ["∈"] = TokenType.Element,
                ["∉"] = TokenType.NotElement,
                ["⊂"] = TokenType.Subset,
                ["⊃"] = TokenType.Superset,
                ["⊆"] = TokenType.SubsetEqual,
                ["⊇"] = TokenType.SupersetEqual,
                ["∪"] = TokenType.Union,
                ["∩"] = TokenType.Intersection,
                ["∅"] = TokenType.EmptySet,
                ["\\"] = TokenType.SetDifference,
                
                // Calculus and advanced math
                ["∇"] = TokenType.Nabla,           // Nabla/del operator
                ["∂"] = TokenType.PartialDerivative,  // Partial derivative
                ["∫"] = TokenType.Integral,        // Integral
                ["∬"] = TokenType.DoubleIntegral,  // Double integral
                ["∭"] = TokenType.TripleIntegral,  // Triple integral
                ["∮"] = TokenType.ContourIntegral, // Contour integral
                ["Σ"] = TokenType.Summation,       // Summation
                ["∑"] = TokenType.Summation,       // Summation (alternative)
                ["Π"] = TokenType.Product,         // Product
                ["∏"] = TokenType.Product,         // Product (alternative)
                
                // Roots and powers
                ["√"] = TokenType.SquareRoot,
                ["∛"] = TokenType.CubeRoot,
                ["∜"] = TokenType.FourthRoot,
                
                // Vector operations
                ["⋅"] = TokenType.Dot3D,           // Dot product
                ["·"] = TokenType.Dot3D,           // Middle dot (alternative dot product)
                ["⊗"] = TokenType.Tensor,          // Tensor product
                
                // Physics and engineering symbols
                ["V"] = TokenType.Identifier,      // Voltage symbol
                ["A"] = TokenType.Identifier,      // Ampere symbol
                ["Hz"] = TokenType.Identifier,     // Hertz symbol
                ["Ω"] = TokenType.Identifier,      // Ohm symbol
                ["°"] = TokenType.Identifier,      // Degree symbol
                ["℃"] = TokenType.Identifier,      // Celsius symbol
                ["℉"] = TokenType.Identifier,      // Fahrenheit symbol
                
                // Additional mathematical operators
                ["lim"] = TokenType.Limit,         // Limit operator
                ["→"] = TokenType.Arrow,           // Rightwards arrow (limit approach)
                ["⟶"] = TokenType.Arrow,           // Long arrow
                ["↦"] = TokenType.Arrow,           // Maps to
                
                // Statistical symbols and superscripts/subscripts
                ["σ²"] = TokenType.Identifier,     // Variance symbol
                ["χ²"] = TokenType.Identifier,     // Chi-squared
                ["χ²_critical"] = TokenType.Identifier, // Chi-squared critical value
                ["²"] = TokenType.Identifier,      // Superscript two
                ["³"] = TokenType.Identifier,      // Superscript three
                ["⁴"] = TokenType.Identifier,      // Superscript four
                ["₀"] = TokenType.Identifier,      // Subscript zero
                ["₁"] = TokenType.Identifier,      // Subscript one
                ["₂"] = TokenType.Identifier,      // Subscript two
                ["₃"] = TokenType.Identifier,      // Subscript three
                ["₄"] = TokenType.Identifier,      // Subscript four
                ["₅"] = TokenType.Identifier,      // Subscript five
                ["₆"] = TokenType.Identifier,      // Subscript six
                ["₇"] = TokenType.Identifier,      // Subscript seven
                ["₈"] = TokenType.Identifier,      // Subscript eight
                ["₉"] = TokenType.Identifier,      // Subscript nine
                
                // Complex numbers
                ["i"] = TokenType.Identifier,      // Imaginary unit
                ["j"] = TokenType.Identifier,      // Alternative imaginary unit
                
                // Unit symbols with subscripts
                ["V₀"] = TokenType.Identifier,     // Voltage with subscript
                ["f"] = TokenType.Identifier,      // Frequency variable
                ["ε₀"] = TokenType.Identifier,     // Permittivity
                ["μ₀"] = TokenType.Identifier,     // Permeability
                ["ℏ"] = TokenType.Identifier,      // Reduced Planck constant
                
                // Combining characters for vector notation
                ["⃗"] = TokenType.Identifier,       // Combining right arrow above (vector notation)
                ["→"] = TokenType.Arrow,           // Rightwards arrow (already defined above)
                
                // Additional mathematical notation from test file
                ["∆"] = TokenType.Delta,           // Delta (triangle)
                ["∴"] = TokenType.Therefore,       // Therefore
                ["∵"] = TokenType.Because,         // Because  
                ["∀"] = TokenType.ForAll,          // For all
                ["∃"] = TokenType.Exists,          // There exists
                ["∄"] = TokenType.NotExists,       // There does not exist
                ["⊕"] = TokenType.Oplus,           // Direct sum
                ["⊖"] = TokenType.Ominus,          // Symmetric difference
                ["⊙"] = TokenType.Odot,            // Odot
                ["⊠"] = TokenType.Boxtimes,        // Boxtimes
                ["⊡"] = TokenType.Boxdot,          // Boxdot
                ["⊢"] = TokenType.Vdash,           // Provable
                ["⊣"] = TokenType.Dashv,           // Reverse provable
                ["⊤"] = TokenType.Top,             // Top
                ["⊥"] = TokenType.Bottom,          // Bottom
                ["⊨"] = TokenType.Models,          // Models/semantic entailment
                ["⊩"] = TokenType.Forces,          // Forces
                ["⊪"] = TokenType.Forces2,         // Triple turnstile
                ["⊫"] = TokenType.NotForces,       // Does not force
                ["⌈"] = TokenType.Lceil,           // Left ceiling
                ["⌉"] = TokenType.Rceil,           // Right ceiling
                ["⌊"] = TokenType.Lfloor,          // Left floor
                ["⌋"] = TokenType.Rfloor,          // Right floor
                ["〈"] = TokenType.Langle,          // Left angle bracket
                ["〉"] = TokenType.Rangle,          // Right angle bracket
                
                // Ensure specific failing characters are covered
                ["₀"] = TokenType.Identifier,      // Subscript zero 
                ["₁"] = TokenType.Identifier,      // Subscript one
                ["₂"] = TokenType.Identifier,      // Subscript two
                ["₃"] = TokenType.Identifier,      // Subscript three
                ["₄"] = TokenType.Identifier,      // Subscript four
                ["₅"] = TokenType.Identifier,      // Subscript five
                ["₆"] = TokenType.Identifier,      // Subscript six
                ["₇"] = TokenType.Identifier,      // Subscript seven
                ["₈"] = TokenType.Identifier,      // Subscript eight
                ["₉"] = TokenType.Identifier,      // Subscript nine
                ["²"] = TokenType.Identifier,      // Superscript two 
                ["³"] = TokenType.Identifier,      // Superscript three
                ["⁴"] = TokenType.Identifier,      // Superscript four
                ["⁵"] = TokenType.Identifier,      // Superscript five
                ["⁶"] = TokenType.Identifier,      // Superscript six
                ["⁷"] = TokenType.Identifier,      // Superscript seven
                ["⁸"] = TokenType.Identifier,      // Superscript eight
                ["⁹"] = TokenType.Identifier,      // Superscript nine
                ["⃗"] = TokenType.Identifier,       // Combining right arrow above (vector notation)
                
                // Arrows and mapping symbols  
                ["↑"] = TokenType.Uparrow,         // Upwards arrow
                ["↓"] = TokenType.Downarrow,       // Downwards arrow
                ["↔"] = TokenType.Leftrightarrow,  // Left-right arrow
                ["⇒"] = TokenType.Implies,         // Rightwards double arrow (implies)
                ["⇐"] = TokenType.Implied,         // Leftwards double arrow
                ["⇔"] = TokenType.Iff,             // Left-right double arrow (if and only if)
                ["⇑"] = TokenType.Uparrow2,        // Upwards double arrow
                ["⇓"] = TokenType.Downarrow2,      // Downwards double arrow
                ["↪"] = TokenType.Mapsto,          // Rightwards arrow with hook
                ["↩"] = TokenType.Hookleftarrow,   // Leftwards arrow with hook
                ["↺"] = TokenType.Circlearrowleft, // Anticlockwise arrow
                ["↻"] = TokenType.Circlearrowright,// Clockwise arrow
                
                // Physical constants and units
                ["c"] = TokenType.Identifier,      // Speed of light
                ["e"] = TokenType.Identifier,      // Euler's number or elementary charge
                ["ε"] = TokenType.Epsilon,         // Epsilon (permittivity)
                ["μ"] = TokenType.Mu,              // Mu (permeability/friction)
                ["ℏ"] = TokenType.Identifier,      // Reduced Planck constant
                ["ħ"] = TokenType.Identifier,      // Alternative reduced Planck
                ["ℓ"] = TokenType.Identifier,      // Script l
                ["℧"] = TokenType.Identifier,      // Mho (inverse ohm)
                ["℩"] = TokenType.Identifier,      // Turned iota
                
                // Unit symbols with subscripts and superscripts
                ["m²"] = TokenType.Identifier,     // Square meters
                ["m³"] = TokenType.Identifier,     // Cubic meters  
                ["s⁻¹"] = TokenType.Identifier,    // Per second
                ["kg⋅m"] = TokenType.Identifier,   // Kilogram-meter
                ["J⋅s"] = TokenType.Identifier,    // Joule-second
                ["F⋅m"] = TokenType.Identifier,    // Farad per meter
                ["H⋅m"] = TokenType.Identifier     // Henry per meter
            };
        }

        #endregion
    }
} 

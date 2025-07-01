using System;
using System.Linq;
using Ouroboros.Core.Lexer;
using Ouroboros.Testing;

namespace Ouroboros.Tests.Unit
{
    [TestClass]
    public class LexerTests
    {
        private Lexer CreateLexer(string source)
        {
            return new Lexer(source, "test.ouro");
        }

        [Test("Should tokenize simple integer")]
        public void TokenizeInteger()
        {
            var lexer = CreateLexer("42");
            var tokens = lexer.Tokenize();
            
            Assert.AreEqual(2, tokens.Count); // Number + EOF
            Assert.AreEqual(TokenType.Number, tokens[0].Type);
            Assert.AreEqual("42", tokens[0].Lexeme);
            Assert.AreEqual(42.0, tokens[0].Literal);
        }

        [Test("Should tokenize floating point number")]
        public void TokenizeFloat()
        {
            var lexer = CreateLexer("3.14159");
            var tokens = lexer.Tokenize();
            
            Assert.AreEqual(2, tokens.Count);
            Assert.AreEqual(TokenType.Number, tokens[0].Type);
            Assert.AreEqual("3.14159", tokens[0].Lexeme);
            Assert.AreEqual(3.14159, tokens[0].Literal);
        }

        [Test("Should tokenize string literal")]
        public void TokenizeString()
        {
            var lexer = CreateLexer("\"Hello, World!\"");
            var tokens = lexer.Tokenize();
            
            Assert.AreEqual(2, tokens.Count);
            Assert.AreEqual(TokenType.String, tokens[0].Type);
            Assert.AreEqual("Hello, World!", tokens[0].Literal);
        }

        [Test("Should tokenize identifiers")]
        public void TokenizeIdentifier()
        {
            var lexer = CreateLexer("myVariable");
            var tokens = lexer.Tokenize();
            
            Assert.AreEqual(2, tokens.Count);
            Assert.AreEqual(TokenType.Identifier, tokens[0].Type);
            Assert.AreEqual("myVariable", tokens[0].Lexeme);
        }

        [Test("Should tokenize keywords")]
        public void TokenizeKeywords()
        {
            var keywords = new[] { "if", "else", "while", "for", "function", "class", "return" };
            
            foreach (var keyword in keywords)
            {
                var lexer = CreateLexer(keyword);
                var tokens = lexer.Tokenize();
                
                Assert.AreEqual(2, tokens.Count);
                Assert.AreNotEqual(TokenType.Identifier, tokens[0].Type, $"{keyword} should be a keyword");
            }
        }

        [Test("Should tokenize operators")]
        public void TokenizeOperators()
        {
            var lexer = CreateLexer("+ - * / % == != < > <= >= && || !");
            var tokens = lexer.Tokenize();
            
            var operatorTypes = new[]
            {
                TokenType.Plus, TokenType.Minus, TokenType.Star, TokenType.Slash,
                TokenType.Percent, TokenType.EqualEqual, TokenType.BangEqual,
                TokenType.Less, TokenType.Greater, TokenType.LessEqual,
                TokenType.GreaterEqual, TokenType.AmpersandAmpersand,
                TokenType.PipePipe, TokenType.Bang
            };
            
            for (int i = 0; i < operatorTypes.Length; i++)
            {
                Assert.AreEqual(operatorTypes[i], tokens[i].Type);
            }
        }

        [Test("Should tokenize mathematical symbols")]
        public void TokenizeMathSymbols()
        {
            var lexer = CreateLexer("∂ ∇ ∫ ∑ ∏ √ ∞ π");
            var tokens = lexer.Tokenize();
            
            Assert.Greater(tokens.Count, 8);
            Assert.Contains(tokens.Select(t => t.Type), TokenType.PartialDerivative);
            Assert.Contains(tokens.Select(t => t.Type), TokenType.Nabla);
            Assert.Contains(tokens.Select(t => t.Type), TokenType.Integral);
        }

        [Test("Should handle comments")]
        public void HandleComments()
        {
            var lexer = CreateLexer(@"
                // This is a line comment
                42
                /* This is a
                   multi-line comment */
                true
            ");
            var tokens = lexer.Tokenize();
            
            // Comments should be skipped
            var nonEofTokens = tokens.Where(t => t.Type != TokenType.Eof).ToList();
            Assert.AreEqual(2, nonEofTokens.Count);
            Assert.AreEqual(TokenType.Number, nonEofTokens[0].Type);
            Assert.AreEqual(TokenType.True, nonEofTokens[1].Type);
        }

        [Test("Should handle escape sequences in strings")]
        public void HandleEscapeSequences()
        {
            var lexer = CreateLexer("\"Hello\\nWorld\\t!\"");
            var tokens = lexer.Tokenize();
            
            Assert.AreEqual(2, tokens.Count);
            Assert.AreEqual("Hello\nWorld\t!", tokens[0].Literal);
        }

        [Test("Should track line and column numbers")]
        public void TrackLineAndColumn()
        {
            var lexer = CreateLexer("first\nsecond\nthird");
            var tokens = lexer.Tokenize();
            
            Assert.AreEqual(1, tokens[0].Line);
            Assert.AreEqual(1, tokens[0].Column);
            
            var secondToken = tokens.First(t => t.Lexeme == "second");
            Assert.AreEqual(2, secondToken.Line);
            
            var thirdToken = tokens.First(t => t.Lexeme == "third");
            Assert.AreEqual(3, thirdToken.Line);
        }

        [Test("Should handle syntax levels")]
        public void HandleSyntaxLevels()
        {
            var lexer = CreateLexer(@"
                @high {
                    sum of all even numbers from 1 to 100
                }
                @low {
                    mov rax, 42
                }
            ");
            var tokens = lexer.Tokenize();
            
            Assert.Contains(tokens.Select(t => t.Type), TokenType.AtHigh);
            Assert.Contains(tokens.Select(t => t.Type), TokenType.AtLow);
        }

        [Test("Should handle units")]
        public void HandleUnits()
        {
            var lexer = CreateLexer("100m 50kg 9.8m/s²");
            var tokens = lexer.Tokenize();
            
            var unitTokens = tokens.Where(t => t.Type == TokenType.UnitLiteral).ToList();
            Assert.IsNotEmpty(unitTokens);
        }

        [Test("Should report lexical errors")]
        public void ReportLexicalErrors()
        {
            var lexer = CreateLexer("@");
            var tokens = lexer.Tokenize();
            
            Assert.IsTrue(lexer.HadError);
        }

        [Test("Should tokenize interpolated strings")]
        public void TokenizeInterpolatedStrings()
        {
            var lexer = CreateLexer("$\"Hello, {name}!\"");
            var tokens = lexer.Tokenize();
            
            Assert.Contains(tokens.Select(t => t.Type), TokenType.InterpolatedString);
        }

        [Test("Should handle hex and binary literals")]
        public void HandleAlternativeNumberFormats()
        {
            var lexer = CreateLexer("0xFF 0b1010");
            var tokens = lexer.Tokenize();
            
            Assert.AreEqual(255.0, tokens[0].Literal);
            Assert.AreEqual(10.0, tokens[1].Literal);
        }
    }
} 
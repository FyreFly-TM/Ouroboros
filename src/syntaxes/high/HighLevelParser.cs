using System;
using System.Collections.Generic;
using System.Linq;
using Ouro.Core.AST;
using Ouro.Core.Lexer;
using Ouro.Core.Parser;
using Ouro.Tokens;

namespace Ouro.Syntaxes.High
{
    /// <summary>
    /// Multi-level parser supporting high-level natural language, medium-level modern, and low-level systems syntax
    /// </summary>
    public class HighLevelParser
    {
        private List<Token> tokens;
        private int current;
        private SyntaxLevel currentSyntaxLevel = SyntaxLevel.High; // Default to high level
        
        public HighLevelParser(List<Token> tokens)
        {
            this.tokens = tokens;
            this.current = 0;
            DetectSyntaxLevel();
        }
        
        private void DetectSyntaxLevel()
        {
            // Look for syntax level attributes in the first few tokens
            for (int i = 0; i < Math.Min(tokens.Count, 20); i++)
            {
                var token = tokens[i];
                if (token.Type == TokenType.At)
                {
                    if (i + 1 < tokens.Count)
                    {
                        var nextToken = tokens[i + 1];
                        switch (nextToken.Lexeme?.ToLower())
                        {
                            case "high":
                                currentSyntaxLevel = SyntaxLevel.High;
                                return;
                            case "medium":
                                currentSyntaxLevel = SyntaxLevel.Medium;
                                return;
                            case "low":
                                currentSyntaxLevel = SyntaxLevel.Low;
                                return;
                        }
                    }
                }
                else if (token.Type == TokenType.HighLevel)
                {
                    currentSyntaxLevel = SyntaxLevel.High;
                    return;
                }
                else if (token.Type == TokenType.MediumLevel)
                {
                    currentSyntaxLevel = SyntaxLevel.Medium;
                    return;
                }
                else if (token.Type == TokenType.LowLevel)
                {
                    currentSyntaxLevel = SyntaxLevel.Low;
                    return;
                }
            }
        }
        
        public void SetCurrentPosition(int position)
        {
            this.current = position;
        }
        
        public int GetCurrentPosition()
        {
            return this.current;
        }
        
        public Statement ParseStatement()
        {
            return currentSyntaxLevel switch
            {
                SyntaxLevel.High => ParseHighLevelStatement(),
                SyntaxLevel.Medium => ParseMediumLevelStatement(),
                SyntaxLevel.Low => ParseLowLevelStatement(),
                _ => ParseHighLevelStatement()
            };
        }
        
        public Statement ParseHighLevelStatement()
        {
            // Prevent infinite loops by checking for progress
            var startPosition = current;
            
            // Log current token for debugging high-level parsing
            if (Environment.GetEnvironmentVariable("OURO_DEBUG") == "1")
            {
                // HighLevelParser: Current token info available via Current()
            }
            
            // Skip attributes and syntax level markers
            if (Match(TokenType.At))
            {
                if (CanBeUsedAsIdentifier())
                {
                    Advance(); // consume the attribute name
                    return ParseHighLevelStatement(); // continue parsing
                }
            }
            
            // Handle natural language if statements
            if (Match(TokenType.If))
            {
                // Matched IF token, parsing natural language if
                return ParseNaturalLanguageIf();
            }
            
            // Handle for each loops
            if (Match(TokenType.For))
            {
                if (Match(TokenType.Each))
                {
                    return ParseForEachStatement();
                }
                // Rewind if not "for each"
                current--;
            }
            
            // Handle repeat statements
            if (Match(TokenType.Repeat))
            {
                return ParseRepeatStatement();
            }
            
            // Handle iterate statements
            if (Match(TokenType.Iterate))
            {
                return ParseIterateStatement();
            }
            
            // Handle print statements
            if (Match(TokenType.Print))
            {
                return ParsePrintStatement();
            }
            
            // Handle return statements
            if (Match(TokenType.Return))
            {
                return ParseReturnStatement();
            }
            
            // Handle variable assignment with :=
            if (CanBeUsedAsIdentifier() && PeekNext() == TokenType.Assign)
            {
                // Parsing assignment for variable
                return ParseAssignment();
            }
            
            // Handle define function
            if (Match(TokenType.Define))
            {
                if (Match(TokenType.Function))
                {
                    return ParseFunctionDefinition();
                }
                // Rewind if not "define function"
                current--;
            }
            
            // Handle end markers
            if (Match(TokenType.End))
            {
                return ParseEndMarker();
            }
            
            // Handle type declarations (class, struct, etc.)
            if (Match(TokenType.Class, TokenType.Struct, TokenType.Interface))
            {
                return ParseTypeDeclaration();
            }
            
            // Handle namespace and using
            if (Match(TokenType.Namespace))
            {
                return ParseNamespaceDeclaration();
            }
            
            if (Match(TokenType.Using))
            {
                return ParseUsingDirective();
            }
            
            // Prevent infinite loops - if we haven't made progress, advance or throw error
            if (current == startPosition)
            {
                if (IsAtEnd())
                {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    return new ExpressionStatement(new LiteralExpression(CreateToken(TokenType.NullLiteral, "null", null)));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                }
                
                // Skip unknown tokens to prevent infinite loops
                if (!CanBeUsedAsIdentifier() && Current().Type != TokenType.LeftBrace && Current().Type != TokenType.RightBrace)
                {
                    Console.WriteLine($"DEBUG: Skipping unhandled token in high-level: {Current().Type} '{Current().Lexeme}'");
                    Advance();
                    return ParseHighLevelStatement();
                }
                
                // Default: try to parse as expression statement
                try
                {
                    var expr = ParseExpression();
                    return new ExpressionStatement(expr);
                }
                catch
                {
                    // If expression parsing fails, advance to prevent infinite loop
                    Advance();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    return new ExpressionStatement(new LiteralExpression(CreateToken(TokenType.NullLiteral, "null", null)));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                }
            }
            
            // Default: try to parse as expression statement
            // No statement pattern matched, parsing as expression
            var expr2 = ParseExpression();
            return new ExpressionStatement(expr2);
        }
        
        private Statement ParseMediumLevelStatement()
        {
            var startPosition = current;
            
            // Skip attributes and syntax level markers
            if (Match(TokenType.At))
            {
                if (CanBeUsedAsIdentifier())
                {
                    Advance(); // consume the attribute name
                    return ParseMediumLevelStatement(); // continue parsing
                }
            }
            
            // Handle C-style statements
            if (Match(TokenType.If))
            {
                return ParseCStyleIf();
            }
            
            if (Match(TokenType.For))
            {
                return ParseCStyleFor();
            }
            
            if (Match(TokenType.While))
            {
                return ParseWhileStatement();
            }
            
            if (Match(TokenType.Return))
            {
                return ParseReturnStatement();
            }
            
            // Handle variable declarations
            if (Match(TokenType.Var, TokenType.Let, TokenType.Const))
            {
                return ParseVariableDeclaration();
            }
            
            // Handle type declarations
            if (Match(TokenType.Class, TokenType.Struct, TokenType.Interface))
            {
                return ParseTypeDeclaration();
            }
            
            // Handle function declarations
            if (Match(TokenType.Function))
            {
                return ParseMediumFunctionDeclaration();
            }
            
            // Handle namespace and using
            if (Match(TokenType.Namespace))
            {
                return ParseNamespaceDeclaration();
            }
            
            if (Match(TokenType.Using))
            {
                return ParseUsingDirective();
            }
            
            // Handle expression statements
            if (current == startPosition)
            {
                if (IsAtEnd())
                {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    return new ExpressionStatement(new LiteralExpression(CreateToken(TokenType.NullLiteral, "null", null)));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                }
                
                // Skip unknown tokens
                if (!CanBeUsedAsIdentifier() && Current().Type != TokenType.LeftBrace && Current().Type != TokenType.RightBrace)
                {
                    Console.WriteLine($"DEBUG: Skipping unhandled token in medium-level: {Current().Type} '{Current().Lexeme}'");
                    Advance();
                    return ParseMediumLevelStatement();
                }
                
                try
                {
                    var expr = ParseExpression();
                    Match(TokenType.Semicolon); // Optional semicolon
                    return new ExpressionStatement(expr);
                }
                catch
                {
                    Advance();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    return new ExpressionStatement(new LiteralExpression(CreateToken(TokenType.NullLiteral, "null", null)));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                }
            }
            
            var expr2 = ParseExpression();
            Match(TokenType.Semicolon); // Optional semicolon
            return new ExpressionStatement(expr2);
        }
        
        private Statement ParseLowLevelStatement()
        {
            var startPosition = current;
            
            // Skip attributes and syntax level markers
            if (Match(TokenType.At))
            {
                if (CanBeUsedAsIdentifier())
                {
                    Advance(); // consume the attribute name
                    return ParseLowLevelStatement(); // continue parsing
                }
            }
            
            // Handle C-style statements
            if (Match(TokenType.If))
            {
                return ParseCStyleIf();
            }
            
            if (Match(TokenType.For))
            {
                return ParseCStyleFor();
            }
            
            if (Match(TokenType.While))
            {
                return ParseWhileStatement();
            }
            
            if (Match(TokenType.Return))
            {
                return ParseReturnStatement();
            }
            
            // Handle type declarations (struct, union, etc.)
            if (Match(TokenType.Struct))
            {
                return ParseStructDeclaration();
            }
            
            if (Match(TokenType.UnionKeyword))
            {
                return ParseUnionDeclaration();
            }
            
            if (Match(TokenType.Function))
            {
                return ParseLowLevelFunctionDeclaration();
            }
            
            // Handle type aliases
            if (Match(TokenType.Type))
            {
                return ParseTypeAlias();
            }
            
            // Handle variable declarations with types
            if (Match(TokenType.Var, TokenType.Let, TokenType.Const))
            {
                return ParseTypedVariableDeclaration();
            }
            
            // Handle namespace and using
            if (Match(TokenType.Namespace))
            {
                return ParseNamespaceDeclaration();
            }
            
            if (Match(TokenType.Using))
            {
                return ParseUsingDirective();
            }
            
            // Prevent infinite loops
            if (current == startPosition)
            {
                if (IsAtEnd())
                {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    return new ExpressionStatement(new LiteralExpression(CreateToken(TokenType.NullLiteral, "null", null)));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                }
                
                // Skip unknown tokens
                if (!CanBeUsedAsIdentifier() && Current().Type != TokenType.LeftBrace && Current().Type != TokenType.RightBrace)
                {
                    Console.WriteLine($"DEBUG: Skipping unhandled token in low-level: {Current().Type} '{Current().Lexeme}'");
                    Advance();
                    return ParseLowLevelStatement();
                }
                
                try
                {
                    var expr = ParseExpression();
                    Match(TokenType.Semicolon); // Optional semicolon
                    return new ExpressionStatement(expr);
                }
                catch
                {
                    Advance();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    return new ExpressionStatement(new LiteralExpression(CreateToken(TokenType.NullLiteral, "null", null)));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                }
            }
            
            var expr2 = ParseExpression();
            Match(TokenType.Semicolon); // Optional semicolon
            return new ExpressionStatement(expr2);
        }
        
        private Statement ParseNaturalLanguageIf()
        {
            // Parse condition with natural language support
            var condition = ParseNaturalLanguageCondition();
            
            // Expect "then"
            Consume(TokenType.Then, "Expected 'then' after if condition");
            
            // Parse then branch statements
            var thenStatements = new List<Statement>();
            while (!Check(TokenType.Otherwise) && !Check(TokenType.End) && !IsAtEnd())
            {
                thenStatements.Add(ParseHighLevelStatement());
            }
            var thenBranch = new BlockStatement(thenStatements);
            
            // Parse optional else branch
            Statement? elseBranch = null;
            if (Match(TokenType.Otherwise))
            {
                var elseStatements = new List<Statement>();
                while (!Check(TokenType.End) && !IsAtEnd())
                {
                    elseStatements.Add(ParseHighLevelStatement());
                }
                elseBranch = new BlockStatement(elseStatements);
            }
            
            // Expect "end if"
            Consume(TokenType.End, "Expected 'end' to close if statement");
            Consume(TokenType.If, "Expected 'if' after 'end'");
            
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return new IfStatement(CreateToken(TokenType.If, "if", null), condition, thenBranch, elseBranch);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
        
        private Expression ParseNaturalLanguageCondition()
        {
            var left = ParseExpression();
            
            // Handle element-of operator (∈)
            if (Match(TokenType.Element))
            {
                var right = ParseExpression();
                // Generate a call to ElementOf function: ElementOf(left, right)
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                var elementOfCall = new CallExpression(
                    new IdentifierExpression(
                        CreateToken(TokenType.Identifier, "ElementOf", null)),
                    new List<Expression> { left, right }
                );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                                    // Generated ElementOf call for '∈' operator
                return elementOfCall;
            }
            
            // Handle not element-of operator (∉)
            if (Match(TokenType.NotElement))
            {
                var right = ParseExpression();
                // Generate a call to NotElementOf function: NotElementOf(left, right)
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                var notElementOfCall = new CallExpression(
                    new IdentifierExpression(CreateToken(TokenType.Identifier, "NotElementOf", null)),
                    new List<Expression> { left, right }
                );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                Console.WriteLine($"DEBUG: Generated NotElementOf call for '∉' operator");
                return notElementOfCall;
            }
            
            // Handle "is greater than", "is less than", etc.
            if (Match(TokenType.Is))
            {
                if (Match(TokenType.Greater))
                {
                    Consume(TokenType.Than, "Expected 'than' after 'greater'");
                    var right = ParseExpression();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    return new BinaryExpression(left, CreateToken(TokenType.Greater, ">", null), right);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                }
                else if (Match(TokenType.Less))
                {
                    Consume(TokenType.Than, "Expected 'than' after 'less'");
                    var right = ParseExpression();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    return new BinaryExpression(left, CreateToken(TokenType.Less, "<", null), right);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                }
                else if (Match(TokenType.Equal))
                {
                    Consume(TokenType.To, "Expected 'to' after 'equal'");
                    var right = ParseExpression();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    return new BinaryExpression(left, CreateToken(TokenType.Equal, "==", null), right);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                }
                // Add more natural language comparisons as needed
            }
            
            // Fall back to regular expression parsing if not a natural language condition
            return left;
        }
        
        private Statement ParseForEachStatement()
        {
            // "for each" already consumed
            var element = Consume(TokenType.Identifier, "Expected element name");
            Consume(TokenType.In, "Expected 'in' after element name");
            var collection = ParseExpression();
            
            // Parse body statements
            var bodyStatements = new List<Statement>();
            while (!Check(TokenType.End) && !IsAtEnd())
            {
                bodyStatements.Add(ParseHighLevelStatement());
            }
            var body = new BlockStatement(bodyStatements);
            
            // Expect "end for"
            Consume(TokenType.End, "Expected 'end' to close for each loop");
            Consume(TokenType.For, "Expected 'for' after 'end'");
            
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return new ForEachStatement(
                CreateToken(TokenType.ForEach, "foreach", null),
                new TypeNode("var"),
                element,
                collection,
                body
            );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
        
        private Statement ParseRepeatStatement()
        {
            // "repeat" already consumed
            var count = ParseExpression();
            Consume(TokenType.Times, "Expected 'times' after repeat count");
            
            // Parse body statements
            var bodyStatements = new List<Statement>();
            while (!Check(TokenType.End) && !IsAtEnd())
            {
                bodyStatements.Add(ParseHighLevelStatement());
            }
            var body = new BlockStatement(bodyStatements);
            
            // Expect "end repeat"
            Consume(TokenType.End, "Expected 'end' to close repeat loop");
            Consume(TokenType.Repeat, "Expected 'repeat' after 'end'");
            
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return new RepeatStatement(
                CreateToken(TokenType.Repeat, "repeat", null),
                count,
                body
            );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
        
        private Statement ParseIterateStatement()
        {
            // "iterate" already consumed
            var counter = Consume(TokenType.Counter, "Expected counter name").Lexeme;
            Consume(TokenType.From, "Expected 'from' in iterate statement");
            var start = ParseExpression();
            Consume(TokenType.Through, "Expected 'through' in iterate statement");
            var end = ParseExpression();
            
            // Parse body statements
            var bodyStatements = new List<Statement>();
            while (!Check(TokenType.End) && !IsAtEnd())
            {
                bodyStatements.Add(ParseHighLevelStatement());
            }
            var body = new BlockStatement(bodyStatements);
            
            // Expect "end iterate"
            Consume(TokenType.End, "Expected 'end' to close iterate loop");
            Consume(TokenType.Iterate, "Expected 'iterate' after 'end'");
            
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return new IterateStatement(
                CreateToken(TokenType.Iterate, "iterate", null),
                counter,
                start,
                end,
                new LiteralExpression(CreateToken(TokenType.IntegerLiteral, "1", 1)),
                body
            );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
        
        private Statement ParsePrintStatement()
        {
            // "print" already consumed
            var expr = ParseExpression();
            
            // Convert print to Console.WriteLine
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            var printCall = new CallExpression(
                new IdentifierExpression(
                    CreateToken(TokenType.Identifier, "Console.WriteLine", null)),
                new List<Expression> { expr });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            
            return new ExpressionStatement(printCall);
        }
        
        private Statement ParseReturnStatement()
        {
            // "return" already consumed
            var expr = ParseExpression();
            
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return new ReturnStatement(CreateToken(TokenType.Return, "return", null), expr);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
        
        private Statement ParseAssignment()
        {
            var nameToken = Advance(); // identifier
            Consume(TokenType.Assign, "Expected ':=' in assignment");
            var initializer = ParseExpression();
            
            var varType = new TypeNode("var");
            return new VariableDeclaration(varType, nameToken, initializer, false, false);
        }
        
        private Statement ParseFunctionDefinition()
        {
            // "define function" already consumed
            var name = Consume(TokenType.Identifier, "Expected function name");
            Consume(TokenType.Taking, "Expected 'taking' after function name");
            
            // Parse parameters
            var parameters = new List<Parameter>();
            do
            {
                // Accept identifiers and reserved keywords as parameter names
                if (!CanBeUsedAsIdentifier())
                {
                    throw Error(Current(), "Expected parameter name");
                }
                var paramName = Advance();
                parameters.Add(new Parameter(new TypeNode("var"), paramName.Lexeme, null));
            } while (Check(TokenType.Identifier) && Current().Lexeme == "and" && Match(TokenType.Identifier));
            
            // Parse body statements
            var bodyStatements = new List<Statement>();
            while (!Check(TokenType.End) && !IsAtEnd())
            {
                bodyStatements.Add(ParseHighLevelStatement());
            }
            var body = new BlockStatement(bodyStatements);
            
            // Expect "end function"
            Consume(TokenType.End, "Expected 'end' to close function");
            Consume(TokenType.Function, "Expected 'function' after 'end'");
            
            return new FunctionDeclaration(
                name,
                new TypeNode("var"), // return type inferred
                parameters,
                body,
                new List<TypeParameter>(),
                false, // not async
                new List<Modifier>()
            );
        }
        
        private Statement ParseEndMarker()
        {
            // Validate that the end marker matches the expected construct
            if (!Match(TokenType.End))
            {
                throw Error(Current(), "Expected 'end' keyword");
            }
            
            // Check what follows the 'end' keyword
            if (Check(TokenType.If))
            {
                Advance(); // consume 'if'
                // This is the end of an if statement
                Console.WriteLine($"DEBUG: Parsed 'end if'");
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                return new ExpressionStatement(new LiteralExpression(CreateToken(TokenType.NullLiteral, "null", null)));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            }
            else if (Check(TokenType.While))
            {
                Advance(); // consume 'while'
                // This is the end of a while statement
                Console.WriteLine($"DEBUG: Parsed 'end while'");
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                return new ExpressionStatement(new LiteralExpression(CreateToken(TokenType.NullLiteral, "null", null)));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            }
            else if (Check(TokenType.For))
            {
                Advance(); // consume 'for'
                // This is the end of a for statement
                Console.WriteLine($"DEBUG: Parsed 'end for'");
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                return new ExpressionStatement(new LiteralExpression(CreateToken(TokenType.NullLiteral, "null", null)));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            }
            else if (Check(TokenType.Function))
            {
                // Don't consume here - let ParseFunctionDefinition handle it
                // This end marker will be consumed by the function parser
                Console.WriteLine($"DEBUG: Found 'end function' marker");
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                return new ExpressionStatement(new LiteralExpression(CreateToken(TokenType.NullLiteral, "null", null)));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            }
            else
            {
                // Generic end marker
                Console.WriteLine($"DEBUG: Parsed generic 'end' marker");
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                return new ExpressionStatement(new LiteralExpression(CreateToken(TokenType.NullLiteral, "null", null)));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            }
        }
        
        private Expression ParseNaturalLanguageLinq()
        {
            // "all" already consumed
            Console.WriteLine($"DEBUG: Parsing natural language LINQ starting with 'all'");
            
            if (Match(TokenType.Even))
            {
                // "all even numbers from X" pattern
                Consume(TokenType.Numbers, "Expected 'numbers' after 'even'");
                Consume(TokenType.From, "Expected 'from' after 'numbers'");
                var source = ParseIdentifierLikeExpression(); // Use special parsing for identifier-like tokens
                
                // Create a LINQ Where expression equivalent to numbers.Where(x => x % 2 == 0)
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                var lambdaParam = CreateToken(TokenType.Identifier, "x", null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                var parameter = new Parameter(new TypeNode("var"), "x");
                
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                var modExpression = new BinaryExpression(
                    new IdentifierExpression(lambdaParam),
                    CreateToken(TokenType.Modulo, "%", null),
                    new LiteralExpression(CreateToken(TokenType.IntegerLiteral, "2", 2))
                );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                var condition = new BinaryExpression(
                    modExpression,
                    CreateToken(TokenType.Equal, "==", null),
                    new LiteralExpression(CreateToken(TokenType.IntegerLiteral, "0", 0))
                );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                
                // Create a Where call expression using MemberExpression and proper LambdaExpression
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                var whereCall = new CallExpression(
                    new MemberExpression(source, CreateToken(TokenType.Dot, ".", null), CreateToken(TokenType.Identifier, "Where", null)),
                    new List<Expression> { new LambdaExpression(new List<Parameter> { parameter }, condition) }
                );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                
                Console.WriteLine($"DEBUG: Generated Where expression for 'all even numbers from'");
                return whereCall;
            }
            
            // Handle other patterns like "all X" in the future
            throw Error(Current(), "Unsupported natural language LINQ pattern after 'all'");
        }
        
        private Expression ParseIdentifierLikeExpression()
        {
            // Parse tokens that can be used as identifiers, including reserved keywords
            if (CanBeUsedAsIdentifier())
            {
                var token = Advance();
                Console.WriteLine($"DEBUG: Parsed identifier-like token: {token.Lexeme}");
                return new IdentifierExpression(token);
            }
            
            throw Error(Current(), "Expected identifier or identifier-like token");
        }
        
        private Expression ParseEachExpression()
        {
            // "each" already consumed
            Console.WriteLine($"DEBUG: Parsing 'each' expression");
            
            // Parse "each number in numbers multiplied by 2" pattern
            var itemName = Consume(TokenType.Identifier, "Expected identifier after 'each'").Lexeme;
            Consume(TokenType.In, "Expected 'in' after item name");
            var source = ParseIdentifierLikeExpression();
            Consume(TokenType.Multiplied, "Expected 'multiplied' after source");
            Consume(TokenType.By, "Expected 'by' after 'multiplied'");
            var multiplier = ParseExpression();
            
            // Create a LINQ Select expression equivalent to numbers.Select(x => x * 2)
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            var lambdaParam = CreateToken(TokenType.Identifier, itemName, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            var parameter = new Parameter(new TypeNode("var"), itemName);
            
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            var multiplyExpression = new BinaryExpression(
                new IdentifierExpression(lambdaParam),
                CreateToken(TokenType.Multiply, "*", null),
                multiplier
            );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            
            // Create a Select call expression using MemberExpression and proper LambdaExpression
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            var selectCall = new CallExpression(
                new MemberExpression(source, CreateToken(TokenType.Dot, ".", null), CreateToken(TokenType.Identifier, "Select", null)),
                new List<Expression> { new LambdaExpression(new List<Parameter> { parameter }, multiplyExpression) }
            );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            
            Console.WriteLine($"DEBUG: Generated Select expression for 'each {itemName} in ... multiplied by'");
            return selectCall;
        }
        
        private Expression ParseAggregationExpression(Token aggregationFunction)
        {
            // aggregationFunction (like "sum", "average", "count", etc.) already consumed
            Console.WriteLine($"DEBUG: Parsing aggregation expression starting with '{aggregationFunction.Lexeme}'");
            
            // Parse "sum of all X" pattern
            Consume(TokenType.Identifier, "Expected 'of' after aggregation function");  // consume "of"
            Consume(TokenType.All, "Expected 'all' after 'of'");
            var source = ParseIdentifierLikeExpression();
            
            // Map natural language aggregation to LINQ methods
            string methodName = aggregationFunction.Lexeme.ToLower() switch
            {
                "sum" => "Sum",
                "average" => "Average",
                "count" => "Count",
                "minimum" => "Min",
                "maximum" => "Max",
                "product" => "Aggregate",
                _ => throw Error(aggregationFunction, $"Unknown aggregation function: {aggregationFunction.Lexeme}")
            };
            
            if (methodName == "Aggregate" && aggregationFunction.Lexeme.ToLower() == "product")
            {
                // Special case for product: numbers.Aggregate(1, (acc, x) => acc * x)
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                var accParam = CreateToken(TokenType.Identifier, "acc", null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                var xParam = CreateToken(TokenType.Identifier, "x", null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                var accParameter = new Parameter(new TypeNode("var"), "acc");
                var xParameter = new Parameter(new TypeNode("var"), "x");
                
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                var multiplyExpression = new BinaryExpression(
                    new IdentifierExpression(accParam),
                    CreateToken(TokenType.Multiply, "*", null),
                    new IdentifierExpression(xParam)
                );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                var aggregationCall = new CallExpression(
                    new MemberExpression(source, CreateToken(TokenType.Dot, ".", null), CreateToken(TokenType.Identifier, methodName, null)),
                    new List<Expression> 
                    { 
                        new LiteralExpression(CreateToken(TokenType.IntegerLiteral, "1", 1)),
                        new LambdaExpression(new List<Parameter> { accParameter, xParameter }, multiplyExpression)
                    }
                );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                
                Console.WriteLine($"DEBUG: Generated PRODUCT expression using Aggregate");
                return aggregationCall;
            }
            else
            {
                // Create an aggregation call expression equivalent to numbers.Sum() / Average() / etc.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                var aggregationCall = new CallExpression(
                    new MemberExpression(source, CreateToken(TokenType.Dot, ".", null), CreateToken(TokenType.Identifier, methodName, null)),
                    new List<Expression>()  // No arguments for most aggregation functions
                );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                
                Console.WriteLine($"DEBUG: Generated {methodName} expression for '{aggregationFunction.Lexeme} of all'");
                return aggregationCall;
            }
        }
        
        private Expression ParseTryExpression()
        {
            // "try" already consumed
            Console.WriteLine($"DEBUG: Parsing try expression");
            
            var tryExpr = ParseExpression();
            
            if (Match(TokenType.Catch))
            {
                // Handle "try X catch Y" pattern
                var catchExpr = ParseExpression();
                
                // Create a lambda for the try block: () => tryExpr
                var tryLambda = new LambdaExpression(new List<Parameter>(), tryExpr);
                
                // Create a lambda for the catch block: (e) => catchExpr
                var exceptionParam = new Parameter(new TypeNode("Exception"), "e");
                var catchLambda = new LambdaExpression(new List<Parameter> { exceptionParam }, catchExpr);
                
                // Generate: System.TryCatch(tryLambda, catchLambda)
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                var tryCatchCall = new CallExpression(
                    new MemberExpression(
                        new IdentifierExpression(CreateToken(TokenType.Identifier, "System", null)),
                        CreateToken(TokenType.Dot, ".", null),
                        CreateToken(TokenType.Identifier, "TryCatch", null)
                    ),
                    new List<Expression> { tryLambda, catchLambda }
                );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                
                Console.WriteLine($"DEBUG: Generated System.TryCatch call for 'try X catch Y'");
                return tryCatchCall;
            }
            else if (Match(TokenType.Else))
            {
                // Handle "try X else Y" pattern (shorthand for try-catch with default value)
                var elseExpr = ParseExpression();
                
                // Create a lambda for the try block: () => tryExpr
                var tryLambda = new LambdaExpression(new List<Parameter>(), tryExpr);
                
                // Generate: System.TryGetValue(tryLambda, elseExpr)
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                var tryCall = new CallExpression(
                    new MemberExpression(
                        new IdentifierExpression(CreateToken(TokenType.Identifier, "System", null)),
                        CreateToken(TokenType.Dot, ".", null),
                        CreateToken(TokenType.Identifier, "TryGetValue", null)
                    ),
                    new List<Expression> { tryLambda, elseExpr }
                );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                
                Console.WriteLine($"DEBUG: Generated System.TryGetValue call for 'try X else Y'");
                return tryCall;
            }
            else
            {
                throw Error(Current(), "Expected 'catch' or 'else' after try expression");
            }
        }

        
        private Expression ParseExpression()
        {
            return ParseLogicalOr();
        }
        
        private Expression ParseLogicalOr()
        {
            var expr = ParseLogicalAnd();
            
            while (Match(TokenType.LogicalOr))
            {
                var op = Previous();
                var right = ParseLogicalAnd();
                expr = new BinaryExpression(expr, op, right);
            }
            
            return expr;
        }
        
        private Expression ParseLogicalAnd()
        {
            var expr = ParseNullCoalescing();
            
            while (Match(TokenType.LogicalAnd))
            {
                var op = Previous();
                var right = ParseNullCoalescing();
                expr = new BinaryExpression(expr, op, right);
            }
            
            return expr;
        }
        
        private Expression ParseNullCoalescing()
        {
            var expr = ParseEquality();
            
            while (Match(TokenType.NullCoalesce))
            {
                var op = Previous();
                var right = ParseEquality();
                expr = new BinaryExpression(expr, op, right);
            }
            
            return expr;
        }
        
        private Expression ParseEquality()
        {
            var expr = ParseComparison();
            
            while (Match(TokenType.Equal, TokenType.NotEqual))
            {
                var op = Previous();
                var right = ParseComparison();
                expr = new BinaryExpression(expr, op, right);
            }
            
            return expr;
        }
        
        private Expression ParseComparison()
        {
            var expr = ParseShift();
            
            while (Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual, TokenType.Spaceship))
            {
                var op = Previous();
                var right = ParseShift();
                expr = new BinaryExpression(expr, op, right);
            }
            
            return expr;
        }
        
        private Expression ParseShift()
        {
            var expr = ParseAddition();
            
            while (Match(TokenType.LeftShift, TokenType.RightShift, TokenType.UnsignedRightShift))
            {
                var op = Previous();
                var right = ParseAddition();
                expr = new BinaryExpression(expr, op, right);
            }
            
            return expr;
        }
        
        private Expression ParseAddition()
        {
            var expr = ParseMultiplication();
            
            while (Match(TokenType.Plus, TokenType.Minus))
            {
                var op = Previous();
                var right = ParseMultiplication();
                expr = new BinaryExpression(expr, op, right);
            }
            
            return expr;
        }
        
        private Expression ParseMultiplication()
        {
            var expr = ParsePower();
            
            while (Match(TokenType.Multiply, TokenType.Divide, TokenType.Modulo, TokenType.IntegerDivide) || 
                   (Check(TokenType.Multiplied) && PeekNext() == TokenType.By) ||
                   (Check(TokenType.Divided) && PeekNext() == TokenType.By))
            {
                Token op;
                if (Current().Type == TokenType.Multiplied)
                {
                    // Handle "multiplied by" pattern
                    Advance(); // consume "multiplied"
                    Consume(TokenType.By, "Expected 'by' after 'multiplied'");
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    op = CreateToken(TokenType.Multiply, "*", null); // Convert to standard multiply
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                }
                else if (Current().Type == TokenType.Divided)
                {
                    // Handle "divided by" pattern
                    Advance(); // consume "divided"
                    Consume(TokenType.By, "Expected 'by' after 'divided'");
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    op = CreateToken(TokenType.Divide, "/", null); // Convert to standard divide
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                }
                else
                {
                    op = Previous();
                }
                
                var right = ParsePower();
                expr = new BinaryExpression(expr, op, right);
            }
            
            return expr;
        }
        
        private Expression ParsePower()
        {
            var expr = ParseUnary();
            
            // Right-associative power operator
            if (Match(TokenType.Power))
            {
                var op = Previous();
                var right = ParsePower(); // Right-associative
                expr = new BinaryExpression(expr, op, right);
            }
            
            return expr;
        }
        
        private Expression ParseUnary()
        {
            if (Match(TokenType.LogicalNot, TokenType.Minus, TokenType.BitwiseNot))
            {
                var op = Previous();
                var right = ParseUnary();
                return new UnaryExpression(op, right);
            }
            
            return ParsePostfix();
        }
        
        private Expression ParsePostfix()
        {
            var expr = ParsePrimary();
            
            while (true)
            {
                if (Match(TokenType.LeftParen))
                {
                    // Function call
                    var args = new List<Expression>();
                    if (!Check(TokenType.RightParen))
                    {
                        do
                        {
                            args.Add(ParseExpression());
                        } while (Match(TokenType.Comma));
                    }
                    Consume(TokenType.RightParen, "Expected ')' after arguments");
                    expr = new CallExpression(expr, args);
                }
                else if (Match(TokenType.LeftBracket))
                {
                    // Array/indexer access
                    var index = ParseExpression();
                    Consume(TokenType.RightBracket, "Expected ']' after array index");
                    expr = new IndexExpression(expr, index);
                }
                else if (Match(TokenType.Dot))
                {
                    // Member access
                    var name = Consume(TokenType.Identifier, "Expected property name after '.'");
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    expr = new MemberExpression(expr, CreateToken(TokenType.Dot, ".", null), name);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                }
                else if (Match(TokenType.NullConditional))
                {
                    // Null-conditional member access (?.)
                    var name = Consume(TokenType.Identifier, "Expected property name after '?.'");
                    
                    // Generate a ternary expression: expr == null ? null : expr.name
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    var nullCheck = new BinaryExpression(
                        expr,
                        CreateToken(TokenType.Equal, "==", null),
                        new LiteralExpression(CreateToken(TokenType.NullLiteral, "null", null))
                    );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                    
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    var memberAccess = new MemberExpression(expr, CreateToken(TokenType.Dot, ".", null), name);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                    
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    var questionToken = CreateToken(TokenType.Question, "?", null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    var colonToken = CreateToken(TokenType.Colon, ":", null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                    
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    expr = new ConditionalExpression(
                        nullCheck,
                        questionToken,
                        new LiteralExpression(CreateToken(TokenType.NullLiteral, "null", null)),
                        colonToken,
                        memberAccess
                    );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                }
                else if (Match(TokenType.Increment, TokenType.Decrement))
                {
                    // Postfix increment/decrement
                    var op = Previous();
                    expr = new UnaryExpression(op, expr);
                }
                else if (Match(TokenType.With))
                {
                    // Handle "function_name with arg1 and arg2" pattern
                    var args = new List<Expression>();
                    
                    // Parse first argument
                    args.Add(ParseExpression());
                    
                    // Parse additional arguments separated by "and"
                    while (Check(TokenType.Identifier) && Current().Lexeme == "and")
                    {
                        Advance(); // consume "and"
                        args.Add(ParseExpression());
                    }
                    
                    expr = new CallExpression(expr, args);
                    Console.WriteLine($"DEBUG: Generated function call with 'with' syntax");
                }
                else if (Match(TokenType.Match))
                {
                    // Pattern matching expression
                    expr = ParseMatchExpression(expr);
                }
                else
                {
                    break;
                }
            }
            
            return expr;
        }
        
        private Expression ParseMatchExpression(Expression expr)
        {
            // "match" already consumed
            Consume(TokenType.LeftBrace, "Expected '{' after match");
            
            var cases = new List<Expression>();
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                // Parse pattern
                var pattern = ParseExpression();
                
                // Handle guard clauses
                Expression? guard = null;
                if (Match(TokenType.When))
                {
                    guard = ParseExpression();
                }
                
                Consume(TokenType.DoubleArrow, "Expected '=>' after match pattern");
                var result = ParseExpression();
                
                // Create a case representation (simplified)
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                var caseExpr = new BinaryExpression(pattern, CreateToken(TokenType.DoubleArrow, "=>", null), result);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                cases.Add(caseExpr);
                
                if (!Check(TokenType.RightBrace))
                {
                    Match(TokenType.Comma); // Optional comma
                }
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after match cases");
            
            // Return a simplified match expression representation
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return new CallExpression(
                new IdentifierExpression(CreateToken(TokenType.Identifier, "Match", null)),
                new List<Expression> { expr }.Concat(cases).ToList()
            );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
        
        private Expression ParsePrimary()
        {
            Console.WriteLine($"DEBUG: ParsePrimary() - Current token: {Current().Type} '{Current().Lexeme}'");
            
            if (Match(TokenType.IntegerLiteral, TokenType.FloatLiteral, TokenType.DoubleLiteral, TokenType.DecimalLiteral,
                     TokenType.HexLiteral, TokenType.BinaryLiteral, TokenType.OctalLiteral))
            {
                return new LiteralExpression(Previous());
            }
            
            if (Match(TokenType.StringLiteral, TokenType.InterpolatedString, TokenType.RawString, TokenType.CharLiteral))
            {
                return new LiteralExpression(Previous());
            }
            
            if (Check(TokenType.BooleanLiteral))
            {
                return new LiteralExpression(Advance());
            }
            
            if (Match(TokenType.NullLiteral))
            {
                return new LiteralExpression(Previous());
            }
            
            // Handle unit literals (e.g., 120 V, 60 Hz)
            if (Match(TokenType.UnitLiteral))
            {
                return new LiteralExpression(Previous());
            }
            
            // Handle range operators
            if (Check(TokenType.IntegerLiteral) || Check(TokenType.Identifier))
            {
                var start = Advance();
                if (Match(TokenType.Range))
                {
                    var end = ParseExpression();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    return new RangeExpression(
                        new LiteralExpression(start),
                        CreateToken(TokenType.Range, "..", null),
                        end,
                        false // inclusive
                    );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                }
                else if (Match(TokenType.Spread))
                {
                    var end = ParseExpression();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    return new RangeExpression(
                        new LiteralExpression(start),
                        CreateToken(TokenType.Spread, "...", null),
                        end,
                        true // exclusive
                    );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                }
                else
                {
                    // Backtrack and treat as identifier/literal
                    current--;
                }
            }
            
            // Handle identifiers and reserved keywords used as identifiers
            if (CanBeUsedAsIdentifier())
            {
                var identifier = Advance();
                Console.WriteLine($"DEBUG: Matched identifier-like token: {identifier.Lexeme}");
                
                // Check for aggregation expressions like "sum of all X"
                if (identifier.Lexeme == "sum" && Check(TokenType.Identifier) && Current().Lexeme == "of")
                {
                    return ParseAggregationExpression(identifier);
                }
                
                return new IdentifierExpression(identifier);
            }
            
            // Handle natural language LINQ expressions
            if (Match(TokenType.All))
            {
                return ParseNaturalLanguageLinq();
            }
            
            if (Match(TokenType.Each))
            {
                return ParseEachExpression();
            }
            
            if (Match(TokenType.Try))
            {
                return ParseTryExpression();
            }
            
            if (Match(TokenType.LeftParen))
            {
                // Handle tuple literals or grouped expressions
                var firstExpr = ParseExpression();
                
                if (Match(TokenType.Comma))
                {
                    // Tuple literal
                    var elements = new List<Expression> { firstExpr };
                    do
                    {
                        elements.Add(ParseExpression());
                    } while (Match(TokenType.Comma));
                    
                    Consume(TokenType.RightParen, "Expected ')' after tuple elements");
                    return new TupleExpression(elements);
                }
                else
                {
                    // Grouped expression
                    Consume(TokenType.RightParen, "Expected ')' after expression");
                    return firstExpr;
                }
            }
            
            if (Match(TokenType.LeftBracket))
            {
                // Array literal
                var elements = new List<Expression>();
                if (!Check(TokenType.RightBracket))
                {
                    do
                    {
                        // Handle spread operator in arrays
                        if (Match(TokenType.Spread))
                        {
                            var spreadExpr = ParseExpression();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                            elements.Add(new SpreadExpression(CreateToken(TokenType.Spread, "...", null), spreadExpr));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                        }
                        else
                        {
                            elements.Add(ParseExpression());
                        }
                    } while (Match(TokenType.Comma));
                }
                Consume(TokenType.RightBracket, "Expected ']' after array elements");
                return new ArrayExpression(Previous(), elements);
            }
            
            // Handle null coalescing in primary position
            if (currentSyntaxLevel == SyntaxLevel.Medium || currentSyntaxLevel == SyntaxLevel.Low)
            {
                // Look ahead for operators that might be in primary position
                if (Check(TokenType.NullCoalesce))
                {
                    // This shouldn't happen in primary, but handle gracefully
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    var nullValue = new LiteralExpression(CreateToken(TokenType.NullLiteral, "null", null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                    Advance(); // consume ??
                    var defaultValue = ParseExpression();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    return new BinaryExpression(nullValue, CreateToken(TokenType.NullCoalesce, "??", null), defaultValue);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                }
            }
            
            Console.WriteLine($"DEBUG: ParsePrimary failed - no pattern matched for token: {Current().Type} '{Current().Lexeme}'");
            throw Error(Current(), "Expected expression");
        }
        
        private bool Check(TokenType type)
        {
            if (IsAtEnd()) return false;
            return Current().Type == type;
        }
        
        private bool CanBeUsedAsIdentifier()
        {
            // Allow identifiers and reserved keywords as variable names
            var currentType = Current().Type;
            return currentType == TokenType.Identifier ||
                   currentType == TokenType.Numbers ||
                   currentType == TokenType.Counter ||
                   currentType == TokenType.Even ||
                   currentType == TokenType.Odd ||
                   currentType == TokenType.Area ||
                   currentType == TokenType.Length ||
                   currentType == TokenType.Width ||
                   currentType == TokenType.Data ||
                   currentType == TokenType.Multiplied ||
                   currentType == TokenType.Divided ||
                   currentType == TokenType.By ||
                   currentType == TokenType.With;
            // Add more reserved keywords here as needed
        }
        
        private bool Match(params TokenType[] types)
        {
            foreach (var type in types)
            {
                if (Check(type))
                {
                    Advance();
                    return true;
                }
            }
            return false;
        }
        
        private Token Advance()
        {
            if (!IsAtEnd()) current++;
            return Previous();
        }
        
        private bool IsAtEnd()
        {
            return current >= tokens.Count || tokens[current].Type == TokenType.EndOfFile;
        }
        
        private Token Current()
        {
            return tokens[current];
        }
        
        private Token Previous()
        {
            return tokens[current - 1];
        }
        
        private TokenType PeekNext()
        {
            if (current + 1 < tokens.Count)
                return tokens[current + 1].Type;
            return TokenType.EndOfFile;
        }
        
        private Token Consume(TokenType type, string message)
        {
            if (Check(type)) return Advance();
            throw Error(Current(), message);
        }
        
        private Exception Error(Token token, string message)
        {
            return new Exception($"[{token.FileName}:{token.Line}:{token.Column}] {message}");
        }
        
        private Token CreateToken(TokenType type, string lexeme, object literal)
        {
            return new Token(type, lexeme, literal, 0, 0, 0, 0, "", SyntaxLevel.High);
        }
        
        private Statement ParseCStyleIf()
        {
            // "if" already consumed
            Consume(TokenType.LeftParen, "Expected '(' after 'if'");
            var condition = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after if condition");
            
            var thenStatement = ParseBlockOrStatement();
            
            Statement? elseStatement = null;
            if (Match(TokenType.Else))
            {
                elseStatement = ParseBlockOrStatement();
            }
            
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return new IfStatement(CreateToken(TokenType.If, "if", null), condition, thenStatement, elseStatement);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
        
        private Statement ParseCStyleFor()
        {
            // "for" already consumed
            Consume(TokenType.LeftParen, "Expected '(' after 'for'");
            
            // Check for range-based for loop: for (var item in collection)
            if (Check(TokenType.Var) || Check(TokenType.Identifier))
            {
                var varToken = Advance();
                var identifier = Consume(TokenType.Identifier, "Expected identifier");
                if (Match(TokenType.In))
                {
                    var collection = ParseExpression();
                    Consume(TokenType.RightParen, "Expected ')' after for-in");
                    var body = ParseBlockOrStatement();
                    
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    return new ForEachStatement(
                        CreateToken(TokenType.ForEach, "foreach", null),
                        new TypeNode("var"),
                        identifier,
                        collection,
                        body
                    );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                }
                else
                {
                    // Backtrack for traditional for loop
                    current -= 2;
                }
            }
            
            // Traditional for loop: for (init; condition; increment)
            Statement? init = null;
            if (!Check(TokenType.Semicolon))
            {
                if (Match(TokenType.Var, TokenType.Let))
                {
                    init = ParseVariableDeclaration();
                }
                else
                {
                    init = new ExpressionStatement(ParseExpression());
                }
            }
            Consume(TokenType.Semicolon, "Expected ';' after for loop initializer");
            
            Expression? condition = null;
            if (!Check(TokenType.Semicolon))
            {
                condition = ParseExpression();
            }
            Consume(TokenType.Semicolon, "Expected ';' after for loop condition");
            
            Expression? increment = null;
            if (!Check(TokenType.RightParen))
            {
                increment = ParseExpression();
            }
            Consume(TokenType.RightParen, "Expected ')' after for clauses");
            
            var forBody = ParseBlockOrStatement();
            
            // Convert to while loop structure for AST
            var whileBody = forBody;
            if (increment != null)
            {
                var statements = new List<Statement>();
                if (forBody is BlockStatement block)
                {
                    statements.AddRange(block.Statements);
                }
                else
                {
                    statements.Add(forBody);
                }
                statements.Add(new ExpressionStatement(increment));
                whileBody = new BlockStatement(statements);
            }
            
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            var whileLoop = new WhileStatement(
                CreateToken(TokenType.While, "while", null),
                condition ?? new LiteralExpression(CreateToken(TokenType.BooleanLiteral, "true", true)),
                whileBody
            );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            
            if (init != null)
            {
                return new BlockStatement(new List<Statement> { init, whileLoop });
            }
            
            return whileLoop;
        }
        
        private Statement ParseWhileStatement()
        {
            // "while" already consumed
            Consume(TokenType.LeftParen, "Expected '(' after 'while'");
            var condition = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after while condition");
            var body = ParseBlockOrStatement();
            
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return new WhileStatement(CreateToken(TokenType.While, "while", null), condition, body);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
        
        private Statement ParseBlockOrStatement()
        {
            if (Check(TokenType.LeftBrace))
            {
                return ParseBlockStatement();
            }
            return ParseStatement();
        }
        
        private Statement ParseBlockStatement()
        {
            Consume(TokenType.LeftBrace, "Expected '{'");
            var statements = new List<Statement>();
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                statements.Add(ParseStatement());
            }
            
            Consume(TokenType.RightBrace, "Expected '}'");
            return new BlockStatement(statements);
        }
        
        private Statement ParseVariableDeclaration()
        {
            // var/let/const already consumed
            var varType = Previous();
            var name = Consume(TokenType.Identifier, "Expected variable name");
            
            TypeNode? type = null;
            if (Match(TokenType.Colon))
            {
                type = ParseType();
            }
            
            Expression? initializer = null;
            if (Match(TokenType.Assign))
            {
                initializer = ParseExpression();
            }
            
            var isConstant = varType.Type == TokenType.Const;
            var isReadonly = varType.Type == TokenType.Let;
            
            return new VariableDeclaration(type ?? new TypeNode("var"), name, initializer, isConstant, isReadonly);
        }
        
        private Statement ParseTypedVariableDeclaration()
        {
            // var/let/const already consumed
            var varType = Previous();
            var name = Consume(TokenType.Identifier, "Expected variable name");
            
            TypeNode? type = null;
            if (Match(TokenType.Colon))
            {
                type = ParseType();
            }
            else
            {
                type = new TypeNode("var");
            }
            
            Expression? initializer = null;
            if (Match(TokenType.Assign))
            {
                initializer = ParseExpression();
            }
            
            var isConstant = varType.Type == TokenType.Const;
            var isReadonly = varType.Type == TokenType.Let;
            
            return new VariableDeclaration(type, name, initializer, isConstant, isReadonly);
        }
        
        private TypeNode ParseType()
        {
            var typeName = "";
            
            // Handle basic types
            if (Match(TokenType.Bool, TokenType.Byte, TokenType.SByte, TokenType.Short, TokenType.UShort,
                     TokenType.Int, TokenType.UInt, TokenType.Long, TokenType.ULong, TokenType.Float,
                     TokenType.Double, TokenType.Decimal, TokenType.Char, TokenType.String, TokenType.Void))
            {
                typeName = Previous().Lexeme;
            }
            else if (Match(TokenType.Identifier))
            {
                typeName = Previous().Lexeme;
            }
            else
            {
                throw Error(Current(), "Expected type name");
            }
            
            // Handle array types
            if (Check(TokenType.LeftBracket))
            {
                while (Match(TokenType.LeftBracket))
                {
                    if (Check(TokenType.IntegerLiteral))
                    {
                        Advance(); // consume array size
                    }
                    Consume(TokenType.RightBracket, "Expected ']' after array type");
                    typeName += "[]";
                }
            }
            
            // Handle nullable types
            if (Match(TokenType.Question))
            {
                typeName += "?";
            }
            
            return new TypeNode(typeName);
        }
        
        private Statement ParseTypeDeclaration()
        {
            var typeKeyword = Previous(); // class/struct/interface
            var name = Consume(TokenType.Identifier, "Expected type name");
            
            // Skip generic parameters for now
            if (Match(TokenType.Less))
            {
                while (!Check(TokenType.Greater) && !IsAtEnd())
                {
                    Advance();
                }
                Consume(TokenType.Greater, "Expected '>' after generic parameters");
            }
            
            // Skip inheritance for now
            if (Match(TokenType.Colon))
            {
                while (!Check(TokenType.LeftBrace) && !IsAtEnd())
                {
                    Advance();
                }
            }
            
            var members = new List<Statement>();
            if (Match(TokenType.LeftBrace))
            {
                while (!Check(TokenType.RightBrace) && !IsAtEnd())
                {
                    // Skip modifiers
                    while (Match(TokenType.Public, TokenType.Private, TokenType.Protected, TokenType.Static,
                                TokenType.Virtual, TokenType.Override, TokenType.Abstract, TokenType.Sealed))
                    {
                        // consume modifiers
                    }
                    
                    members.Add(ParseStatement());
                }
                Consume(TokenType.RightBrace, "Expected '}' after type body");
            }
            
            // Create a simplified class declaration
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return new ExpressionStatement(new LiteralExpression(CreateToken(TokenType.Identifier, $"{typeKeyword.Lexeme} {name.Lexeme}", null)));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
        
        private Statement ParseStructDeclaration()
        {
            // "struct" already consumed
            var name = Consume(TokenType.Identifier, "Expected struct name");
            
            var fields = new List<Statement>();
            Consume(TokenType.LeftBrace, "Expected '{' after struct name");
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                // Parse field declarations
                var fieldNames = new List<Token>();
                fieldNames.Add(Consume(TokenType.Identifier, "Expected field name"));
                
                while (Match(TokenType.Comma))
                {
                    fieldNames.Add(Consume(TokenType.Identifier, "Expected field name"));
                }
                
                Consume(TokenType.Colon, "Expected ':' after field name(s)");
                var fieldType = ParseType();
                Consume(TokenType.Semicolon, "Expected ';' after field declaration");
                
                foreach (var fieldName in fieldNames)
                {
                    fields.Add(new VariableDeclaration(fieldType, fieldName, null, false, false));
                }
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after struct body");
            
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return new ExpressionStatement(new LiteralExpression(CreateToken(TokenType.Identifier, $"struct {name.Lexeme}", null)));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
        
        private Statement ParseUnionDeclaration()
        {
            // "union" already consumed
            var name = Consume(TokenType.Identifier, "Expected union name");
            
            var fields = new List<Statement>();
            Consume(TokenType.LeftBrace, "Expected '{' after union name");
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var fieldName = Consume(TokenType.Identifier, "Expected field name");
                Consume(TokenType.Colon, "Expected ':' after field name");
                var fieldType = ParseType();
                Consume(TokenType.Semicolon, "Expected ';' after field declaration");
                
                fields.Add(new VariableDeclaration(fieldType, fieldName, null, false, false));
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after union body");
            
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return new ExpressionStatement(new LiteralExpression(CreateToken(TokenType.Identifier, $"union {name.Lexeme}", null)));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
        
        private Statement ParseTypeAlias()
        {
            // "type" already consumed
            var name = Consume(TokenType.Identifier, "Expected type alias name");
            Consume(TokenType.Assign, "Expected '=' in type alias");
            var targetType = ParseType();
            Consume(TokenType.Semicolon, "Expected ';' after type alias");
            
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return new ExpressionStatement(new LiteralExpression(CreateToken(TokenType.Identifier, $"type {name.Lexeme} = {targetType.Name}", null)));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
        
        private Statement ParseMediumFunctionDeclaration()
        {
            // "function" already consumed
            var name = Consume(TokenType.Identifier, "Expected function name");
            
            Consume(TokenType.LeftParen, "Expected '(' after function name");
            var parameters = new List<Parameter>();
            
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    var paramName = Consume(TokenType.Identifier, "Expected parameter name");
                    TypeNode paramType = new TypeNode("var");
                    
                    if (Match(TokenType.Colon))
                    {
                        paramType = ParseType();
                    }
                    
                    parameters.Add(new Parameter(paramType, paramName.Lexeme, null));
                } while (Match(TokenType.Comma));
            }
            
            Consume(TokenType.RightParen, "Expected ')' after parameters");
            
            TypeNode returnType = new TypeNode("void");
            if (Match(TokenType.Arrow) || Match(TokenType.Colon))
            {
                returnType = ParseType();
            }
            
            var body = ParseBlockStatement();
            
            return new FunctionDeclaration(
                name,
                returnType,
                parameters,
                (BlockStatement)body,
                new List<TypeParameter>(),
                false,
                new List<Modifier>()
            );
        }
        
        private Statement ParseLowLevelFunctionDeclaration()
        {
            // "function" already consumed
            var name = Consume(TokenType.Identifier, "Expected function name");
            
            Consume(TokenType.LeftParen, "Expected '(' after function name");
            var parameters = new List<Parameter>();
            
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    var paramName = Consume(TokenType.Identifier, "Expected parameter name");
                    Consume(TokenType.Colon, "Expected ':' after parameter name");
                    var paramType = ParseType();
                    
                    parameters.Add(new Parameter(paramType, paramName.Lexeme, null));
                } while (Match(TokenType.Comma));
            }
            
            Consume(TokenType.RightParen, "Expected ')' after parameters");
            
            TypeNode returnType = new TypeNode("void");
            if (Match(TokenType.Arrow))
            {
                returnType = ParseType();
            }
            
            var body = ParseBlockStatement();
            
            return new FunctionDeclaration(
                name,
                returnType,
                parameters,
                (BlockStatement)body,
                new List<TypeParameter>(),
                false,
                new List<Modifier>()
            );
        }
        
        private Statement ParseNamespaceDeclaration()
        {
            // "namespace" already consumed
            var name = Consume(TokenType.Identifier, "Expected namespace name");
            
            var members = new List<Statement>();
            if (Match(TokenType.LeftBrace))
            {
                while (!Check(TokenType.RightBrace) && !IsAtEnd())
                {
                    members.Add(ParseStatement());
                }
                Consume(TokenType.RightBrace, "Expected '}' after namespace body");
            }
            
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return new ExpressionStatement(new LiteralExpression(CreateToken(TokenType.Identifier, $"namespace {name.Lexeme}", null)));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
        
        private Statement ParseUsingDirective()
        {
            // "using" already consumed
            var namespaceName = "";
            
            do
            {
                var part = Consume(TokenType.Identifier, "Expected namespace identifier");
                namespaceName += part.Lexeme;
                if (Match(TokenType.Dot))
                {
                    namespaceName += ".";
                }
            } while (Previous().Type == TokenType.Dot);
            
            Match(TokenType.Semicolon); // Optional semicolon
            
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return new ExpressionStatement(new LiteralExpression(CreateToken(TokenType.Identifier, $"using {namespaceName}", null)));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
    }
}
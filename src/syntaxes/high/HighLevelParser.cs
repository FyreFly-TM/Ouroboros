using System;
using System.Collections.Generic;
using System.Linq;
using Ouroboros.Core.AST;
using Ouroboros.Core.Lexer;
using Ouroboros.Core.Parser;
using Ouroboros.Tokens;

namespace Ouroboros.Syntaxes.High
{
    /// <summary>
    /// Parser for high-level natural language syntax
    /// </summary>
    public class HighLevelParser
    {
        private List<Token> tokens;
        private int current;
        
        public HighLevelParser(List<Token> tokens)
        {
            this.tokens = tokens;
            this.current = 0;
        }
        
        public void SetCurrentPosition(int position)
        {
            this.current = position;
        }
        
        public int GetCurrentPosition()
        {
            return this.current;
        }
        
        public Statement ParseHighLevelStatement()
        {
            Console.WriteLine($"DEBUG: HighLevelParser.ParseHighLevelStatement() - Current token: {Current().Type} '{Current().Lexeme}' at {Current().Line}:{Current().Column}");
            
            // Handle natural language if statements
            if (Match(TokenType.If))
            {
                Console.WriteLine($"DEBUG: Matched IF token, parsing natural language if");
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
                Console.WriteLine($"DEBUG: Parsing assignment for {Current().Lexeme}");
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
            
            // Default: try to parse as expression statement
            Console.WriteLine($"DEBUG: No statement pattern matched, parsing as expression");
            var expr = ParseExpression();
            return new ExpressionStatement(expr);
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
            Statement elseBranch = null;
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
            
            return new IfStatement(CreateToken(TokenType.If, "if", null), condition, thenBranch, elseBranch);
        }
        
        private Expression ParseNaturalLanguageCondition()
        {
            var left = ParseExpression();
            
            // Handle element-of operator (∈)
            if (Match(TokenType.Element))
            {
                var right = ParseExpression();
                // Generate a call to ElementOf function: ElementOf(left, right)
                var elementOfCall = new CallExpression(
                    new IdentifierExpression(CreateToken(TokenType.Identifier, "ElementOf", null)),
                    new List<Expression> { left, right }
                );
                Console.WriteLine($"DEBUG: Generated ElementOf call for '∈' operator");
                return elementOfCall;
            }
            
            // Handle not element-of operator (∉)
            if (Match(TokenType.NotElement))
            {
                var right = ParseExpression();
                // Generate a call to NotElementOf function: NotElementOf(left, right)
                var notElementOfCall = new CallExpression(
                    new IdentifierExpression(CreateToken(TokenType.Identifier, "NotElementOf", null)),
                    new List<Expression> { left, right }
                );
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
                    return new BinaryExpression(left, CreateToken(TokenType.Greater, ">", null), right);
                }
                else if (Match(TokenType.Less))
                {
                    Consume(TokenType.Than, "Expected 'than' after 'less'");
                    var right = ParseExpression();
                    return new BinaryExpression(left, CreateToken(TokenType.Less, "<", null), right);
                }
                else if (Match(TokenType.Equal))
                {
                    Consume(TokenType.To, "Expected 'to' after 'equal'");
                    var right = ParseExpression();
                    return new BinaryExpression(left, CreateToken(TokenType.Equal, "==", null), right);
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
            
            return new ForEachStatement(
                CreateToken(TokenType.ForEach, "foreach", null),
                new TypeNode("var"),
                element,
                collection,
                body
            );
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
            
            return new RepeatStatement(
                CreateToken(TokenType.Repeat, "repeat", null),
                count,
                body
            );
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
            
            return new IterateStatement(
                CreateToken(TokenType.Iterate, "iterate", null),
                counter,
                start,
                end,
                new LiteralExpression(CreateToken(TokenType.IntegerLiteral, "1", 1)),
                body
            );
        }
        
        private Statement ParsePrintStatement()
        {
            // "print" already consumed
            var expr = ParseExpression();
            
            // Convert print to Console.WriteLine
            var printCall = new CallExpression(
                new IdentifierExpression(
                    CreateToken(TokenType.Identifier, "Console.WriteLine", null)),
                new List<Expression> { expr });
            
            return new ExpressionStatement(printCall);
        }
        
        private Statement ParseReturnStatement()
        {
            // "return" already consumed
            var expr = ParseExpression();
            
            return new ReturnStatement(CreateToken(TokenType.Return, "return", null), expr);
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
            // This is a placeholder - in a full implementation,
            // we'd validate that the end marker matches the expected construct
            return new ExpressionStatement(new LiteralExpression(CreateToken(TokenType.NullLiteral, "null", null)));
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
                var lambdaParam = CreateToken(TokenType.Identifier, "x", null);
                var parameter = new Parameter(new TypeNode("var"), "x");
                
                var modExpression = new BinaryExpression(
                    new IdentifierExpression(lambdaParam),
                    CreateToken(TokenType.Modulo, "%", null),
                    new LiteralExpression(CreateToken(TokenType.IntegerLiteral, "2", 2))
                );
                var condition = new BinaryExpression(
                    modExpression,
                    CreateToken(TokenType.Equal, "==", null),
                    new LiteralExpression(CreateToken(TokenType.IntegerLiteral, "0", 0))
                );
                
                // Create a Where call expression using MemberExpression and proper LambdaExpression
                var whereCall = new CallExpression(
                    new MemberExpression(source, CreateToken(TokenType.Dot, ".", null), CreateToken(TokenType.Identifier, "Where", null)),
                    new List<Expression> { new LambdaExpression(new List<Parameter> { parameter }, condition) }
                );
                
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
            var lambdaParam = CreateToken(TokenType.Identifier, itemName, null);
            var parameter = new Parameter(new TypeNode("var"), itemName);
            
            var multiplyExpression = new BinaryExpression(
                new IdentifierExpression(lambdaParam),
                CreateToken(TokenType.Multiply, "*", null),
                multiplier
            );
            
            // Create a Select call expression using MemberExpression and proper LambdaExpression
            var selectCall = new CallExpression(
                new MemberExpression(source, CreateToken(TokenType.Dot, ".", null), CreateToken(TokenType.Identifier, "Select", null)),
                new List<Expression> { new LambdaExpression(new List<Parameter> { parameter }, multiplyExpression) }
            );
            
            Console.WriteLine($"DEBUG: Generated Select expression for 'each {itemName} in ... multiplied by'");
            return selectCall;
        }
        
        private Expression ParseAggregationExpression(Token aggregationFunction)
        {
            // aggregationFunction (like "sum") already consumed
            Console.WriteLine($"DEBUG: Parsing aggregation expression starting with '{aggregationFunction.Lexeme}'");
            
            // Parse "sum of all X" pattern
            Consume(TokenType.Identifier, "Expected 'of' after aggregation function");  // consume "of"
            Consume(TokenType.All, "Expected 'all' after 'of'");
            var source = ParseIdentifierLikeExpression();
            
            // Create an aggregation call expression equivalent to numbers.Sum()
            var aggregationCall = new CallExpression(
                new MemberExpression(source, CreateToken(TokenType.Dot, ".", null), CreateToken(TokenType.Identifier, "Sum", null)),
                new List<Expression>()  // No arguments for Sum()
            );
            
            Console.WriteLine($"DEBUG: Generated {aggregationFunction.Lexeme.ToUpper()} expression for 'sum of all'");
            return aggregationCall;
        }
        
        private Expression ParseTryExpression()
        {
            // "try" already consumed
            Console.WriteLine($"DEBUG: Parsing try expression");
            
            var tryExpr = ParseExpression();
            Consume(TokenType.Else, "Expected 'else' after try expression");
            var elseExpr = ParseExpression();
            
            // Create a try-catch expression equivalent to try { tryExpr } catch { elseExpr }
            var tryCall = new CallExpression(
                new IdentifierExpression(
                    CreateToken(TokenType.Identifier, "TryCatch", null)),
                new List<Expression> { tryExpr, elseExpr });
            
            Console.WriteLine($"DEBUG: Generated TryCatch expression for 'try X else Y'");
            return tryCall;
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
            var expr = ParseEquality();
            
            while (Match(TokenType.LogicalAnd))
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
            
            while (Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
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
            
            while (Match(TokenType.LeftShift, TokenType.RightShift))
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
            var expr = ParseUnary();
            
            while (Match(TokenType.Multiply, TokenType.Divide, TokenType.Modulo) || 
                   (Check(TokenType.Multiplied) && PeekNext() == TokenType.By) ||
                   (Check(TokenType.Divided) && PeekNext() == TokenType.By))
            {
                Token op;
                if (Current().Type == TokenType.Multiplied)
                {
                    // Handle "multiplied by" pattern
                    Advance(); // consume "multiplied"
                    Consume(TokenType.By, "Expected 'by' after 'multiplied'");
                    op = CreateToken(TokenType.Multiply, "*", null); // Convert to standard multiply
                }
                else if (Current().Type == TokenType.Divided)
                {
                    // Handle "divided by" pattern
                    Advance(); // consume "divided"
                    Consume(TokenType.By, "Expected 'by' after 'divided'");
                    op = CreateToken(TokenType.Divide, "/", null); // Convert to standard divide
                }
                else
                {
                    op = Previous();
                }
                
                var right = ParseUnary();
                expr = new BinaryExpression(expr, op, right);
            }
            
            return expr;
        }
        
        private Expression ParseUnary()
        {
            if (Match(TokenType.LogicalNot, TokenType.Minus))
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
                if (Match(TokenType.With))
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
                else
                {
                    break;
                }
            }
            
            return expr;
        }
        
        private Expression ParsePrimary()
        {
            Console.WriteLine($"DEBUG: ParsePrimary() - Current token: {Current().Type} '{Current().Lexeme}'");
            
            if (Match(TokenType.IntegerLiteral, TokenType.FloatLiteral, TokenType.DoubleLiteral))
            {
                return new LiteralExpression(Previous());
            }
            
            if (Match(TokenType.StringLiteral, TokenType.InterpolatedString))
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
                var expr = ParseExpression();
                Consume(TokenType.RightParen, "Expected ')' after expression");
                return expr;
            }
            
            if (Match(TokenType.LeftBracket))
            {
                // Array literal
                var elements = new List<Expression>();
                if (!Check(TokenType.RightBracket))
                {
                    do
                    {
                        elements.Add(ParseExpression());
                    } while (Match(TokenType.Comma));
                }
                Consume(TokenType.RightBracket, "Expected ']' after array elements");
                return new ArrayExpression(Previous(), elements);
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
    }
}
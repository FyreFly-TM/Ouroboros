using System;
using System.Collections.Generic;
using System.Linq;
using Ouroboros.Core;
using Ouroboros.Core.Lexer;
using Ouroboros.Core.AST;
using Ouroboros.Core.Parser;
using Ouroboros.Tokens;

namespace Ouroboros.Syntaxes.Medium
{
    /// <summary>
    /// Parser for medium-level syntax - traditional C-like syntax
    /// </summary>
    public class MediumLevelParser
    {
        private readonly List<Token> tokens;
        private int current = 0;

        public MediumLevelParser(List<Token> tokens)
        {
            this.tokens = tokens;
        }

        public Program Parse()
        {
            var statements = new List<Statement>();
            
            while (!IsAtEnd())
            {
                try
                {
                    var stmt = ParseDeclaration();
                    if (stmt != null)
                    {
                        statements.Add(stmt);
                    }
                }
                catch (ParseException ex)
                {
                    // Synchronize to next statement
                    Synchronize();
                    throw ex;
                }
            }
            
            return new Program(statements);
        }

        private Statement ParseDeclaration()
        {
            // Type declarations
            if (Match(TokenType.Class)) return ParseClassDeclaration();
            if (Match(TokenType.Struct)) return ParseStructDeclaration();
            if (Match(TokenType.Interface)) return ParseInterfaceDeclaration();
            if (Match(TokenType.Enum)) return ParseEnumDeclaration();
            if (Match(TokenType.UnionKeyword)) return ParseUnionDeclaration();
            
            // Function declarations
            if (IsTypeKeyword() || Check(TokenType.Identifier))
            {
                // Look ahead for function pattern
                var checkpoint = current;
                try
                {
                    ParseType(); // Try to parse type
                    if (Check(TokenType.Identifier))
                    {
                        Advance(); // consume name
                        if (Check(TokenType.LeftParen))
                        {
                            // It's a function
                            current = checkpoint;
                            return ParseFunctionDeclaration();
                        }
                    }
                }
                catch { }
                current = checkpoint;
            }
            
            // Statements
            return ParseStatement();
        }

        private Statement ParseStatement()
        {
            // Control flow
            if (Match(TokenType.If)) return ParseIfStatement();
            if (Match(TokenType.While)) return ParseWhileStatement();
            if (Match(TokenType.For)) return ParseForStatement();
            if (Match(TokenType.Do)) return ParseDoWhileStatement();
            if (Match(TokenType.Switch)) return ParseSwitchStatement();
            if (Match(TokenType.Return)) return ParseReturnStatement();
            if (Match(TokenType.Break)) return ParseBreakStatement();
            if (Match(TokenType.Continue)) return ParseContinueStatement();
            if (Match(TokenType.Throw)) return ParseThrowStatement();
            if (Match(TokenType.Try)) return ParseTryStatement();
            
            // Blocks
            if (Match(TokenType.LeftBrace)) return ParseBlock();
            
            // Variable declarations or expressions
            return ParseExpressionOrDeclaration();
        }

        private Statement ParseExpressionOrDeclaration()
        {
            // Try to parse as variable declaration
            var checkpoint = current;
            try
            {
                var type = ParseType();
                if (Check(TokenType.Identifier))
                {
                    var name = Advance();
                    
                    // Variable declaration
                    Expression initializer = null;
                    if (Match(TokenType.Assign))
                    {
                        initializer = ParseExpression();
                    }
                    
                    Consume(TokenType.Semicolon, "Expected ';' after variable declaration");
                    return new VariableDeclaration(type, name, initializer);
                }
            }
            catch { }
            
            // Reset and parse as expression
            current = checkpoint;
            var expr = ParseExpression();
            Consume(TokenType.Semicolon, "Expected ';' after expression");
            return new ExpressionStatement(expr);
        }

        private Statement ParseIfStatement()
        {
            Consume(TokenType.LeftParen, "Expected '(' after 'if'");
            Expression condition = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after if condition");
            
            Statement thenBranch = ParseStatement();
            Statement elseBranch = null;
            
            if (Match(TokenType.Else))
            {
                elseBranch = ParseStatement();
            }
            
            return new IfStatement(Previous(), condition, thenBranch, elseBranch);
        }

        private Statement ParseWhileStatement()
        {
            Consume(TokenType.LeftParen, "Expected '(' after 'while'");
            Expression condition = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after while condition");
            
            Statement body = ParseStatement();
            
            return new WhileStatement(condition, body);
        }

        private Statement ParseForStatement()
        {
            Consume(TokenType.LeftParen, "Expected '(' after 'for'");
            
            // Initializer
            Statement initializer = null;
            if (Match(TokenType.Semicolon))
            {
                initializer = null;
            }
            else if (IsTypeDeclaration())
            {
                initializer = ParseDeclaration();
            }
            else
            {
                initializer = ParseExpressionStatement();
            }
            
            // Condition
            Expression condition = null;
            if (!Match(TokenType.Semicolon))
            {
                condition = ParseExpression();
            }
            Consume(TokenType.Semicolon, "Expected ';' after for condition");
            
            // Increment
            Expression increment = null;
            if (!Match(TokenType.RightParen))
            {
                increment = ParseExpression();
            }
            Consume(TokenType.RightParen, "Expected ')' after for clauses");
            
            Statement body = ParseStatement();
            
            return new ForStatement(initializer, condition, increment, body);
        }

        private Statement ParseSwitchStatement()
        {
            Consume(TokenType.LeftParen, "Expected '(' after 'switch'");
            Expression expression = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after switch expression");
            Consume(TokenType.LeftBrace, "Expected '{' after switch expression");
            
            List<SwitchCase> cases = new List<SwitchCase>();
            List<Statement> defaultStatements = null;
            
            while (!Match(TokenType.RightBrace) && !IsAtEnd())
            {
                if (Match(TokenType.Case))
                {
                    Expression caseValue = ParseExpression();
                    Consume(TokenType.Colon, "Expected ':' after case value");
                    
                    List<Statement> statements = new List<Statement>();
                    while (!Match(TokenType.Case) && 
                           !Match(TokenType.Default) && 
                           !Match(TokenType.RightBrace))
                    {
                        statements.Add(ParseStatement());
                    }
                    
                    cases.Add(new SwitchCase(caseValue, statements));
                }
                else if (Match(TokenType.Default))
                {
                    Consume(TokenType.Colon, "Expected ':' after 'default'");
                    
                    defaultStatements = new List<Statement>();
                    while (!Match(TokenType.Case) && 
                           !Match(TokenType.RightBrace))
                    {
                        defaultStatements.Add(ParseStatement());
                    }
                }
                else
                {
                    throw Error(Current(), "Expected 'case' or 'default' in switch statement");
                }
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after switch body");
            
            return new SwitchStatement(expression, cases, defaultStatements);
        }

        private Statement ParseTryStatement()
        {
            Statement tryBlock = ParseBlockStatement();
            
            List<CatchClause> catchClauses = new List<CatchClause>();
            Statement finallyBlock = null;
            
            while (Match(TokenType.Catch))
            {
                string exceptionType = null;
                string variableName = null;
                
                if (Match(TokenType.LeftParen))
                {
                    exceptionType = Consume(TokenType.Identifier, "Expected exception type").Lexeme;
                    variableName = Consume(TokenType.Identifier, "Expected variable name").Lexeme;
                    Consume(TokenType.RightParen, "Expected ')' after catch parameters");
                }
                
                Statement catchBlock = ParseBlockStatement();
                catchClauses.Add(new CatchClause(exceptionType, variableName, catchBlock));
            }
            
            if (Match(TokenType.Finally))
            {
                finallyBlock = ParseBlockStatement();
            }
            
            if (catchClauses.Count == 0 && finallyBlock == null)
            {
                throw Error(Current(), "Try statement must have at least one catch or finally clause");
            }
            
            return new TryStatement(tryBlock, catchClauses, finallyBlock);
        }

        private Statement ParseReturnStatement()
        {
            Expression value = null;
            if (!Match(TokenType.Semicolon))
            {
                value = ParseExpression();
            }
            Consume(TokenType.Semicolon, "Expected ';' after return value");
            
            return new ReturnStatement(value);
        }

        private Statement ParseBreakStatement()
        {
            Consume(TokenType.Semicolon, "Expected ';' after 'break'");
            return new BreakStatement();
        }

        private Statement ParseContinueStatement()
        {
            Consume(TokenType.Semicolon, "Expected ';' after 'continue'");
            return new ContinueStatement();
        }

        private Statement ParseThrowStatement()
        {
            Expression exception = ParseExpression();
            Consume(TokenType.Semicolon, "Expected ';' after throw expression");
            
            return new ThrowStatement(exception);
        }

        private Statement ParseExpressionStatement()
        {
            Expression expr = ParseExpression();
            Consume(TokenType.Semicolon, "Expected ';' after expression");
            
            return new ExpressionStatement(expr);
        }

        private Statement ParseBlockStatement()
        {
            Consume(TokenType.LeftBrace, "Expected '{'");
            
            List<Statement> statements = new List<Statement>();
            while (!Match(TokenType.RightBrace) && !IsAtEnd())
            {
                statements.Add(ParseStatement());
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after block");
            
            return new BlockStatement(statements);
        }

        private bool IsTypeDeclaration()
        {
            // Check for var keyword first
            if (Match(TokenType.Var))
                return true;
                
            // Simple heuristic: check if current token could be a type name
            // and next token is an identifier
            if (!Match(TokenType.Identifier))
                return false;
            
            // Look ahead to see if this might be a declaration
            int current = this.current;
            
            // Skip type name
            Advance();
            
            // Skip nullable marker if present
            if (Match(TokenType.Question))
            {
                // This is likely a nullable type declaration
            }
            
            // Skip generic arguments if present
            if (Match(TokenType.Less))
            {
                int depth = 1;
                while (depth > 0 && !IsAtEnd())
                {
                    if (Match(TokenType.Less))
                        depth++;
                    else if (Match(TokenType.Greater))
                        depth--;
                    else
                        Advance();
                }
            }
            
            // Check if followed by identifier
            bool isDeclaration = Match(TokenType.Identifier);
            
            // Reset position
            this.current = current;
            
            return isDeclaration;
        }

        /// <summary>
        /// Parse a lambda expression in medium syntax
        /// </summary>
        public Expression ParseLambda()
        {
            List<string> parameters = new List<string>();
            
            if (Match(TokenType.LeftParen))
            {
                if (!Match(TokenType.RightParen))
                {
                    do
                    {
                        parameters.Add(Consume(TokenType.Identifier, "Expected parameter name").Lexeme);
                    } while (Match(TokenType.Comma));
                }
                Consume(TokenType.RightParen, "Expected ')' after parameters");
            }
            else
            {
                // Single parameter without parentheses
                parameters.Add(Consume(TokenType.Identifier, "Expected parameter name").Lexeme);
            }
            
            Consume(TokenType.Arrow, "Expected '=>' in lambda expression");
            
            Expression body;
            if (Match(TokenType.LeftBrace))
            {
                // Block body
                Statement blockBody = ParseBlockStatement();
                body = new BlockExpression(blockBody);
            }
            else
            {
                // Expression body - use ParseAssignment to support throw expressions
                body = ParseAssignment();
            }
            
            return new LambdaExpression(parameters, body);
        }

        /// <summary>
        /// Parse pattern matching expression
        /// </summary>
        public Expression ParsePatternMatch(Expression expression)
        {
            Consume(TokenType.Switch, "Expected 'switch' in pattern match");
            Consume(TokenType.LeftBrace, "Expected '{' after switch");
            
            List<PatternCase> cases = new List<PatternCase>();
            Expression defaultCase = null;
            
            while (!Match(TokenType.RightBrace) && !IsAtEnd())
            {
                if (Match(TokenType.Case))
                {
                    Pattern pattern = ParsePattern();
                    Expression guard = null;
                    
                    if (Match(TokenType.When))
                    {
                        guard = ParseExpression();
                    }
                    
                    Consume(TokenType.Arrow, "Expected '=>' after pattern");
                    Expression result = ParseExpression();
                    
                    cases.Add(new PatternCase(pattern, guard, result));
                    
                    if (!Match(TokenType.Comma))
                    {
                        if (!Match(TokenType.RightBrace) && !Match(TokenType.Default))
                        {
                            throw Error(Current(), "Expected ',' between pattern cases");
                        }
                    }
                }
                else if (Match(TokenType.Default))
                {
                    Consume(TokenType.Arrow, "Expected '=>' after 'default'");
                    defaultCase = ParseExpression();
                    
                    if (!Match(TokenType.RightBrace))
                    {
                        Consume(TokenType.Comma, "Expected ',' after default case");
                    }
                }
                else
                {
                    throw Error(Current(), "Expected 'case' or 'default' in pattern match");
                }
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after pattern match");
            
            return new PatternMatchExpression(expression, cases, defaultCase);
        }

        private Pattern ParsePattern()
        {
            // Parse different pattern types
            if (Match(TokenType.NumberLiteral) || 
                Match(TokenType.StringLiteral) ||
                Match(TokenType.True) ||
                Match(TokenType.False) ||
                Match(TokenType.Null))
            {
                // Constant pattern
                Expression constant = ParsePrimary();
                return new ConstantPattern(constant);
            }
            else if (Match(TokenType.Var))
            {
                // Variable pattern
                string name = Consume(TokenType.Identifier, "Expected variable name").Lexeme;
                return new VariablePattern(name);
            }
            else if (Match(TokenType.Identifier))
            {
                string typeName = Advance().Lexeme;
                
                if (Match(TokenType.LeftParen))
                {
                    // Deconstruction pattern
                    List<Pattern> subpatterns = new List<Pattern>();
                    
                    if (!Match(TokenType.RightParen))
                    {
                        do
                        {
                            subpatterns.Add(ParsePattern());
                        } while (Match(TokenType.Comma));
                    }
                    
                    Consume(TokenType.RightParen, "Expected ')' after deconstruction patterns");
                    
                    return new DeconstructionPattern(typeName, subpatterns);
                }
                else
                {
                    // Type pattern
                    return new TypePattern(typeName);
                }
            }
            else if (Match(TokenType.Underscore))
            {
                // Wildcard pattern
                return new WildcardPattern();
            }
            else
            {
                throw Error(Current(), "Invalid pattern");
            }
        }

        #region Helper Methods

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

        private bool Check(TokenType type)
        {
            if (IsAtEnd()) return false;
            return Current().Type == type;
        }

        private Token Advance()
        {
            if (!IsAtEnd()) current++;
            return Previous();
        }

        private bool IsAtEnd()
        {
            return current >= tokens.Count || Current().Type == TokenType.EndOfFile;
        }

        private Token Current()
        {
            return current < tokens.Count ? tokens[current] : 
                new Token(TokenType.EndOfFile, "", null, 0, 0, 0, 0, "", SyntaxLevel.Medium);
        }

        private Token Previous()
        {
            return tokens[current - 1];
        }

        private Token Consume(TokenType type, string message)
        {
            if (Check(type)) return Advance();
            throw Error(Current(), message);
        }

        private ParseException Error(Token token, string message)
        {
            return new ParseException(message, token.Line, token.Column);
        }

        private void Synchronize()
        {
            Advance();
            
            while (!IsAtEnd())
            {
                if (Previous().Type == TokenType.Semicolon) return;
                
                switch (Current().Type)
                {
                    case TokenType.Class:
                    case TokenType.Function:
                    case TokenType.Var:
                    case TokenType.For:
                    case TokenType.If:
                    case TokenType.While:
                    case TokenType.Return:
                        return;
                }
                
                Advance();
            }
        }

        private bool IsTypeKeyword()
        {
            return Check(TokenType.Void) || Check(TokenType.Bool) || 
                   Check(TokenType.Byte) || Check(TokenType.SByte) ||
                   Check(TokenType.Short) || Check(TokenType.UShort) ||
                   Check(TokenType.Int) || Check(TokenType.UInt) ||
                   Check(TokenType.Long) || Check(TokenType.ULong) ||
                   Check(TokenType.Float) || Check(TokenType.Double) ||
                   Check(TokenType.Decimal) || Check(TokenType.String) ||
                   Check(TokenType.Char) || Check(TokenType.Object) ||
                   Check(TokenType.Dynamic) || Check(TokenType.Var);
        }

        #endregion
    }
} 
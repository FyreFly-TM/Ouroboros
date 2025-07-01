using System;
using System.Collections.Generic;
using Ouroboros.Core;
using Ouroboros.Core.Lexer;
using Ouroboros.Core.AST;
using Ouroboros.Core.Parser;

namespace Ouroboros.Syntaxes.Medium
{
    /// <summary>
    /// Parser for medium-level syntax - traditional C-like syntax
    /// </summary>
    public class MediumLevelParser
    {
        private readonly Parser mainParser;
        
        public MediumLevelParser(Parser parser)
        {
            mainParser = parser;
        }

        /// <summary>
        /// Parse medium-level statements
        /// </summary>
        public Statement ParseStatement()
        {
            // Check for common medium-level statements
            if (mainParser.PublicMatch(TokenType.If))
            {
                return ParseIfStatement();
            }
            
            if (mainParser.PublicMatch(TokenType.While))
            {
                return ParseWhileStatement();
            }
            
            if (mainParser.PublicMatch(TokenType.For))
            {
                return ParseForStatement();
            }
            
            if (mainParser.PublicMatch(TokenType.Switch))
            {
                return ParseSwitchStatement();
            }
            
            if (mainParser.PublicMatch(TokenType.Try))
            {
                return ParseTryStatement();
            }
            
            if (mainParser.PublicMatch(TokenType.Return))
            {
                return ParseReturnStatement();
            }
            
            if (mainParser.PublicMatch(TokenType.Break))
            {
                return ParseBreakStatement();
            }
            
            if (mainParser.PublicMatch(TokenType.Continue))
            {
                return ParseContinueStatement();
            }
            
            if (mainParser.PublicMatch(TokenType.Throw))
            {
                return ParseThrowStatement();
            }
            
            // Check for declarations
            bool isTypeDecl = IsTypeDeclaration();
            
            if (isTypeDecl)
            {
                return ParseDeclaration();
            }
            
            // Otherwise, it's an expression statement
            return ParseExpressionStatement();
        }

        private Statement ParseIfStatement()
        {
            mainParser.PublicConsume(TokenType.LeftParen, "Expected '(' after 'if'");
            Expression condition = mainParser.PublicParseExpression();
            mainParser.PublicConsume(TokenType.RightParen, "Expected ')' after if condition");
            
            Statement thenBranch = ParseStatement();
            Statement elseBranch = null;
            
            if (mainParser.PublicMatch(TokenType.Else))
            {
                elseBranch = ParseStatement();
            }
            
            return new IfStatement(mainParser.PublicPrevious(), condition, thenBranch, elseBranch);
        }

        private Statement ParseWhileStatement()
        {
            mainParser.Consume(TokenType.LeftParen, "Expected '(' after 'while'");
            Expression condition = mainParser.ParseExpression();
            mainParser.Consume(TokenType.RightParen, "Expected ')' after while condition");
            
            Statement body = ParseStatement();
            
            return new WhileStatement(condition, body);
        }

        private Statement ParseForStatement()
        {
            mainParser.Consume(TokenType.LeftParen, "Expected '(' after 'for'");
            
            // Initializer
            Statement initializer = null;
            if (mainParser.Match(TokenType.Semicolon))
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
            if (!mainParser.Check(TokenType.Semicolon))
            {
                condition = mainParser.ParseExpression();
            }
            mainParser.Consume(TokenType.Semicolon, "Expected ';' after for condition");
            
            // Increment
            Expression increment = null;
            if (!mainParser.Check(TokenType.RightParen))
            {
                increment = mainParser.ParseExpression();
            }
            mainParser.Consume(TokenType.RightParen, "Expected ')' after for clauses");
            
            Statement body = ParseStatement();
            
            return new ForStatement(initializer, condition, increment, body);
        }

        private Statement ParseSwitchStatement()
        {
            mainParser.Consume(TokenType.LeftParen, "Expected '(' after 'switch'");
            Expression expression = mainParser.ParseExpression();
            mainParser.Consume(TokenType.RightParen, "Expected ')' after switch expression");
            mainParser.Consume(TokenType.LeftBrace, "Expected '{' after switch expression");
            
            List<SwitchCase> cases = new List<SwitchCase>();
            List<Statement> defaultStatements = null;
            
            while (!mainParser.Check(TokenType.RightBrace) && !mainParser.IsAtEnd())
            {
                if (mainParser.Match(TokenType.Case))
                {
                    Expression caseValue = mainParser.ParseExpression();
                    mainParser.Consume(TokenType.Colon, "Expected ':' after case value");
                    
                    List<Statement> statements = new List<Statement>();
                    while (!mainParser.Check(TokenType.Case) && 
                           !mainParser.Check(TokenType.Default) && 
                           !mainParser.Check(TokenType.RightBrace))
                    {
                        statements.Add(ParseStatement());
                    }
                    
                    cases.Add(new SwitchCase(caseValue, statements));
                }
                else if (mainParser.Match(TokenType.Default))
                {
                    mainParser.Consume(TokenType.Colon, "Expected ':' after 'default'");
                    
                    defaultStatements = new List<Statement>();
                    while (!mainParser.Check(TokenType.Case) && 
                           !mainParser.Check(TokenType.RightBrace))
                    {
                        defaultStatements.Add(ParseStatement());
                    }
                }
                else
                {
                    throw new ParseException("Expected 'case' or 'default' in switch statement");
                }
            }
            
            mainParser.Consume(TokenType.RightBrace, "Expected '}' after switch body");
            
            return new SwitchStatement(expression, cases, defaultStatements);
        }

        private Statement ParseTryStatement()
        {
            Statement tryBlock = ParseBlockStatement();
            
            List<CatchClause> catchClauses = new List<CatchClause>();
            Statement finallyBlock = null;
            
            while (mainParser.Match(TokenType.Catch))
            {
                string exceptionType = null;
                string variableName = null;
                
                if (mainParser.Match(TokenType.LeftParen))
                {
                    exceptionType = mainParser.Consume(TokenType.Identifier, "Expected exception type").Lexeme;
                    variableName = mainParser.Consume(TokenType.Identifier, "Expected variable name").Lexeme;
                    mainParser.Consume(TokenType.RightParen, "Expected ')' after catch parameters");
                }
                
                Statement catchBlock = ParseBlockStatement();
                catchClauses.Add(new CatchClause(exceptionType, variableName, catchBlock));
            }
            
            if (mainParser.Match(TokenType.Finally))
            {
                finallyBlock = ParseBlockStatement();
            }
            
            if (catchClauses.Count == 0 && finallyBlock == null)
            {
                throw new ParseException("Try statement must have at least one catch or finally clause");
            }
            
            return new TryStatement(tryBlock, catchClauses, finallyBlock);
        }

        private Statement ParseReturnStatement()
        {
            Expression value = null;
            if (!mainParser.Check(TokenType.Semicolon))
            {
                value = mainParser.ParseExpression();
            }
            mainParser.Consume(TokenType.Semicolon, "Expected ';' after return value");
            
            return new ReturnStatement(value);
        }

        private Statement ParseBreakStatement()
        {
            mainParser.Consume(TokenType.Semicolon, "Expected ';' after 'break'");
            return new BreakStatement();
        }

        private Statement ParseContinueStatement()
        {
            mainParser.Consume(TokenType.Semicolon, "Expected ';' after 'continue'");
            return new ContinueStatement();
        }

        private Statement ParseThrowStatement()
        {
            Expression exception = mainParser.ParseExpression();
            mainParser.Consume(TokenType.Semicolon, "Expected ';' after throw expression");
            
            return new ThrowStatement(exception);
        }

        private Statement ParseDeclaration()
        {
            try
            {
                // Parse type (including var keyword and nullable types)
                string typeName;
                bool isNullable = false;
                
                if (mainParser.PublicCheck(TokenType.Var))
                {
                    typeName = mainParser.PublicAdvance().Lexeme; // "var"
                }
                else
                {
                    typeName = mainParser.PublicConsume(TokenType.Identifier, "Expected type name").Lexeme;
                    
                    // Check for nullable type (e.g., string?)
                    if (mainParser.PublicMatch(TokenType.Question))
                    {
                        isNullable = true;
                        typeName += "?";
                    }
                }
                
                // Check for generics
                List<string> genericArgs = null;
                if (mainParser.PublicMatch(TokenType.Less))
                {
                    genericArgs = ParseGenericArguments();
                }
                
                // Parse variable name
                string variableName = mainParser.PublicConsume(TokenType.Identifier, "Expected variable name").Lexeme;
                
                // Check for initialization
                Expression initializer = null;
                if (mainParser.PublicMatch(TokenType.Equal))
                {
                    initializer = mainParser.PublicParseAssignment(); // Use ParseAssignment to support throw expressions in lambdas
                }
                
                mainParser.PublicConsume(TokenType.Semicolon, "Expected ';' after variable declaration");
                
                return new VariableDeclaration(typeName, variableName, initializer, genericArgs);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private List<string> ParseGenericArguments()
        {
            List<string> args = new List<string>();
            
            do
            {
                args.Add(mainParser.Consume(TokenType.Identifier, "Expected type argument").Lexeme);
            } while (mainParser.Match(TokenType.Comma));
            
            mainParser.Consume(TokenType.Greater, "Expected '>' after generic arguments");
            
            return args;
        }

        private Statement ParseExpressionStatement()
        {
            Expression expr = mainParser.ParseExpression();
            mainParser.Consume(TokenType.Semicolon, "Expected ';' after expression");
            
            return new ExpressionStatement(expr);
        }

        private Statement ParseBlockStatement()
        {
            mainParser.Consume(TokenType.LeftBrace, "Expected '{'");
            
            List<Statement> statements = new List<Statement>();
            while (!mainParser.Check(TokenType.RightBrace) && !mainParser.IsAtEnd())
            {
                statements.Add(ParseStatement());
            }
            
            mainParser.Consume(TokenType.RightBrace, "Expected '}' after block");
            
            return new BlockStatement(statements);
        }

        private bool IsTypeDeclaration()
        {
            // Check for var keyword first
            if (mainParser.Check(TokenType.Var))
                return true;
                
            // Simple heuristic: check if current token could be a type name
            // and next token is an identifier
            if (!mainParser.Check(TokenType.Identifier))
                return false;
            
            // Look ahead to see if this might be a declaration
            int current = mainParser.GetCurrentPosition();
            
            // Skip type name
            mainParser.Advance();
            
            // Skip nullable marker if present
            if (mainParser.Match(TokenType.Question))
            {
                // This is likely a nullable type declaration
            }
            
            // Skip generic arguments if present
            if (mainParser.Match(TokenType.Less))
            {
                int depth = 1;
                while (depth > 0 && !mainParser.IsAtEnd())
                {
                    if (mainParser.Match(TokenType.Less))
                        depth++;
                    else if (mainParser.Match(TokenType.Greater))
                        depth--;
                    else
                        mainParser.Advance();
                }
            }
            
            // Check if followed by identifier
            bool isDeclaration = mainParser.Check(TokenType.Identifier);
            
            // Reset position
            mainParser.SetPosition(current);
            
            return isDeclaration;
        }

        /// <summary>
        /// Parse a lambda expression in medium syntax
        /// </summary>
        public Expression ParseLambda()
        {
            List<string> parameters = new List<string>();
            
            if (mainParser.Match(TokenType.LeftParen))
            {
                if (!mainParser.Check(TokenType.RightParen))
                {
                    do
                    {
                        parameters.Add(mainParser.Consume(TokenType.Identifier, "Expected parameter name").Lexeme);
                    } while (mainParser.Match(TokenType.Comma));
                }
                mainParser.Consume(TokenType.RightParen, "Expected ')' after parameters");
            }
            else
            {
                // Single parameter without parentheses
                parameters.Add(mainParser.Consume(TokenType.Identifier, "Expected parameter name").Lexeme);
            }
            
            mainParser.Consume(TokenType.Arrow, "Expected '=>' in lambda expression");
            
            Expression body;
            if (mainParser.Check(TokenType.LeftBrace))
            {
                // Block body
                Statement blockBody = ParseBlockStatement();
                body = new BlockExpression(blockBody);
            }
            else
            {
                // Expression body - use ParseAssignment to support throw expressions
                body = mainParser.PublicParseAssignment();
            }
            
            return new LambdaExpression(parameters, body);
        }

        /// <summary>
        /// Parse pattern matching expression
        /// </summary>
        public Expression ParsePatternMatch(Expression expression)
        {
            mainParser.Consume(TokenType.Switch, "Expected 'switch' in pattern match");
            mainParser.Consume(TokenType.LeftBrace, "Expected '{' after switch");
            
            List<PatternCase> cases = new List<PatternCase>();
            Expression defaultCase = null;
            
            while (!mainParser.Check(TokenType.RightBrace) && !mainParser.IsAtEnd())
            {
                if (mainParser.Match(TokenType.Case))
                {
                    Pattern pattern = ParsePattern();
                    Expression guard = null;
                    
                    if (mainParser.Match(TokenType.When))
                    {
                        guard = mainParser.ParseExpression();
                    }
                    
                    mainParser.Consume(TokenType.Arrow, "Expected '=>' after pattern");
                    Expression result = mainParser.ParseExpression();
                    
                    cases.Add(new PatternCase(pattern, guard, result));
                    
                    if (!mainParser.Match(TokenType.Comma))
                    {
                        if (!mainParser.Check(TokenType.RightBrace) && !mainParser.Check(TokenType.Default))
                        {
                            throw new ParseException("Expected ',' between pattern cases");
                        }
                    }
                }
                else if (mainParser.Match(TokenType.Default))
                {
                    mainParser.Consume(TokenType.Arrow, "Expected '=>' after 'default'");
                    defaultCase = mainParser.ParseExpression();
                    
                    if (!mainParser.Check(TokenType.RightBrace))
                    {
                        mainParser.Consume(TokenType.Comma, "Expected ',' after default case");
                    }
                }
                else
                {
                    throw new ParseException("Expected 'case' or 'default' in pattern match");
                }
            }
            
            mainParser.Consume(TokenType.RightBrace, "Expected '}' after pattern match");
            
            return new PatternMatchExpression(expression, cases, defaultCase);
        }

        private Pattern ParsePattern()
        {
            // Parse different pattern types
            if (mainParser.Check(TokenType.NumberLiteral) || 
                mainParser.Check(TokenType.StringLiteral) ||
                mainParser.Check(TokenType.True) ||
                mainParser.Check(TokenType.False) ||
                mainParser.Check(TokenType.Null))
            {
                // Constant pattern
                Expression constant = mainParser.ParsePrimary();
                return new ConstantPattern(constant);
            }
            else if (mainParser.Match(TokenType.Var))
            {
                // Variable pattern
                string name = mainParser.Consume(TokenType.Identifier, "Expected variable name").Lexeme;
                return new VariablePattern(name);
            }
            else if (mainParser.Check(TokenType.Identifier))
            {
                string typeName = mainParser.Advance().Lexeme;
                
                if (mainParser.Match(TokenType.LeftParen))
                {
                    // Deconstruction pattern
                    List<Pattern> subpatterns = new List<Pattern>();
                    
                    if (!mainParser.Check(TokenType.RightParen))
                    {
                        do
                        {
                            subpatterns.Add(ParsePattern());
                        } while (mainParser.Match(TokenType.Comma));
                    }
                    
                    mainParser.Consume(TokenType.RightParen, "Expected ')' after deconstruction patterns");
                    
                    return new DeconstructionPattern(typeName, subpatterns);
                }
                else
                {
                    // Type pattern
                    return new TypePattern(typeName);
                }
            }
            else if (mainParser.Match(TokenType.Underscore))
            {
                // Wildcard pattern
                return new WildcardPattern();
            }
            else
            {
                throw new ParseException("Invalid pattern");
            }
        }
    }
} 
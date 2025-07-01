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
                return new ConstantPattern(new LiteralExpression(Previous()));
            }
            
            if (Match(TokenType.Underscore))
            {
                return new WildcardPattern();
            }
            
            if (Check(TokenType.Identifier))
            {
                var start = current;
                var type = ParseType();
                
                if (Check(TokenType.Identifier))
                {
                    // Type pattern with variable binding
                    var varName = Advance();
                    return new TypePattern { Type = type, VariableName = varName.Lexeme };
                }
                else
                {
                    // Reset and parse as identifier pattern
                    current = start;
                    var identifier = Advance();
                    return new IdentifierPattern(identifier);
                }
            }
                
                if (Match(TokenType.LeftParen))
                {
                // Tuple pattern
                var patterns = new List<Pattern>();
                    
                if (!Check(TokenType.RightParen))
                    {
                        do
                        {
                        patterns.Add(ParsePattern());
                        } while (Match(TokenType.Comma));
                    }
                    
                Consume(TokenType.RightParen, "Expected ')' after tuple pattern");
                return new TupleMatchPattern(patterns);
            }
            
                throw Error(Current(), "Invalid pattern");
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
            return Check(TokenType.Int) || Check(TokenType.Float) || 
                   Check(TokenType.Double) || Check(TokenType.Bool) ||
                   Check(TokenType.String) || Check(TokenType.Char) ||
                   Check(TokenType.Byte) || Check(TokenType.Short) ||
                   Check(TokenType.Long) || Check(TokenType.Void);
        }

        private Statement ParseClassDeclaration()
        {
            var classToken = Previous();
            var name = Consume(TokenType.Identifier, "Expected class name");
            
            // Parse generic parameters
            var typeParameters = new List<TypeParameter>();
            if (Match(TokenType.Less))
            {
                do
                {
                    var paramName = Consume(TokenType.Identifier, "Expected type parameter name");
                    var constraints = new List<TypeNode>();
                    
                    if (Match(TokenType.Colon))
                    {
                        do
                        {
                            constraints.Add(ParseType());
                        } while (Match(TokenType.Comma));
                    }
                    
                    typeParameters.Add(new TypeParameter(paramName.Lexeme, constraints));
                } while (Match(TokenType.Comma));
                
                Consume(TokenType.Greater, "Expected '>' after type parameters");
            }
            
            // Parse base class and interfaces
            TypeNode baseClass = null;
            var interfaces = new List<TypeNode>();
            
            if (Match(TokenType.Colon))
            {
                baseClass = ParseType();
                
                while (Match(TokenType.Comma))
                {
                    interfaces.Add(ParseType());
                }
            }
            
            // Parse class body
            Consume(TokenType.LeftBrace, "Expected '{' after class declaration");
            var members = new List<Declaration>();
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                members.Add(ParseMemberDeclaration());
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after class body");
            
            return new ClassDeclaration(classToken, name, baseClass, interfaces, members, typeParameters);
        }
        
        private Statement ParseStructDeclaration()
        {
            var structToken = Previous();
            var name = Consume(TokenType.Identifier, "Expected struct name");
            
            // Parse generic parameters
            var typeParameters = new List<TypeParameter>();
            if (Match(TokenType.Less))
            {
                do
                {
                    var paramName = Consume(TokenType.Identifier, "Expected type parameter name");
                    typeParameters.Add(new TypeParameter(paramName.Lexeme));
                } while (Match(TokenType.Comma));
                
                Consume(TokenType.Greater, "Expected '>' after type parameters");
            }
            
            // Parse interfaces
            var interfaces = new List<TypeNode>();
            if (Match(TokenType.Colon))
            {
                do
                {
                    interfaces.Add(ParseType());
                } while (Match(TokenType.Comma));
            }
            
            // Parse struct body
            Consume(TokenType.LeftBrace, "Expected '{' after struct declaration");
            var members = new List<Declaration>();
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                members.Add(ParseMemberDeclaration());
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after struct body");
            
            return new StructDeclaration(structToken, name, interfaces, members, typeParameters);
        }
        
        private Statement ParseInterfaceDeclaration()
        {
            var interfaceToken = Previous();
            var name = Consume(TokenType.Identifier, "Expected interface name");
            
            // Parse generic parameters
            var typeParameters = new List<TypeParameter>();
            if (Match(TokenType.Less))
            {
                do
                {
                    var paramName = Consume(TokenType.Identifier, "Expected type parameter name");
                    typeParameters.Add(new TypeParameter(paramName.Lexeme));
                } while (Match(TokenType.Comma));
                
                Consume(TokenType.Greater, "Expected '>' after type parameters");
            }
            
            // Parse base interfaces
            var baseInterfaces = new List<TypeNode>();
            if (Match(TokenType.Colon))
            {
                do
                {
                    baseInterfaces.Add(ParseType());
                } while (Match(TokenType.Comma));
            }
            
            // Parse interface body
            Consume(TokenType.LeftBrace, "Expected '{' after interface declaration");
            var members = new List<Declaration>();
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                members.Add(ParseInterfaceMember());
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after interface body");
            
            return new InterfaceDeclaration(interfaceToken, name, baseInterfaces, members, typeParameters);
        }
        
        private Statement ParseEnumDeclaration()
        {
            var enumToken = Previous();
            var name = Consume(TokenType.Identifier, "Expected enum name");
            
            // Parse underlying type
            TypeNode underlyingType = null;
            if (Match(TokenType.Colon))
            {
                underlyingType = ParseType();
            }
            
            // Parse enum body
            Consume(TokenType.LeftBrace, "Expected '{' after enum declaration");
            var members = new List<EnumMember>();
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var memberName = Consume(TokenType.Identifier, "Expected enum member name");
                Expression value = null;
                
                if (Match(TokenType.Assign))
                {
                    value = ParseExpression();
                }
                
                members.Add(new EnumMember(memberName.Lexeme, value));
                
                if (!Check(TokenType.RightBrace))
                {
                    Consume(TokenType.Comma, "Expected ',' between enum members");
                }
                else
                {
                    // Optional trailing comma
                    Match(TokenType.Comma);
                }
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after enum body");
            
            return new EnumDeclaration(enumToken, name, underlyingType, members);
        }
        
        private Statement ParseUnionDeclaration()
        {
            var unionToken = Previous();
            var name = Consume(TokenType.Identifier, "Expected union name");
            
            // For now, parse union as a struct with special flag
            // In a real implementation, you'd have a separate UnionDeclaration class
            Consume(TokenType.LeftBrace, "Expected '{' after union declaration");
            var members = new List<Declaration>();
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                members.Add(ParseMemberDeclaration());
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after union body");
            
            // Create a struct declaration but mark it as a union
            var structDecl = new StructDeclaration(unionToken, name, new List<TypeNode>(), members);
            // You might want to add a IsUnion property to StructDeclaration
            return structDecl;
        }
        
        private Declaration ParseMemberDeclaration()
        {
            // Parse modifiers
            var modifiers = new List<Modifier>();
            while (IsModifier())
            {
                modifiers.Add(ParseModifier());
            }
            
            // Check for constructor (name matches current class)
            // For now, parse as regular member
            
            var type = ParseType();
            var name = Consume(TokenType.Identifier, "Expected member name");
            
            // Property
            if (Match(TokenType.LeftBrace))
            {
                BlockStatement getter = null;
                BlockStatement setter = null;
                
                while (!Check(TokenType.RightBrace) && !IsAtEnd())
                {
                    if (Match(TokenType.Get))
                    {
                        if (Match(TokenType.Semicolon))
                        {
                            // Auto-property getter
                            getter = new BlockStatement(new List<Statement>());
                        }
                        else
                        {
                            getter = ParseBlockStatement();
                        }
                    }
                    else if (Match(TokenType.Set))
                    {
                        if (Match(TokenType.Semicolon))
                        {
                            // Auto-property setter
                            setter = new BlockStatement(new List<Statement>());
                        }
                        else
                        {
                            setter = ParseBlockStatement();
                        }
                    }
                }
                
                Consume(TokenType.RightBrace, "Expected '}' after property");
                
                return new PropertyDeclaration(name, type, getter, setter, null, modifiers);
            }
            // Method
            else if (Match(TokenType.LeftParen))
            {
                var parameters = new List<Parameter>();
                
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        var paramType = ParseType();
                        var paramName = Consume(TokenType.Identifier, "Expected parameter name");
                        Expression defaultValue = null;
                        
                        if (Match(TokenType.Assign))
                        {
                            defaultValue = ParseExpression();
                        }
                        
                        parameters.Add(new Parameter(paramType, paramName.Lexeme, defaultValue));
                    } while (Match(TokenType.Comma));
                }
                
                Consume(TokenType.RightParen, "Expected ')' after parameters");
                
                BlockStatement body;
                if (Match(TokenType.Semicolon))
                {
                    // Abstract method
                    body = null;
                }
                else
                {
                    body = ParseBlockStatement();
                }
                
                return new FunctionDeclaration(name, type, parameters, body, null, false, modifiers);
            }
            // Field
            else
            {
                Expression initializer = null;
                if (Match(TokenType.Assign))
                {
                    initializer = ParseExpression();
                }
                
                Consume(TokenType.Semicolon, "Expected ';' after field");
                
                return new FieldDeclaration(name, type, initializer, modifiers);
            }
        }
        
        private Declaration ParseInterfaceMember()
        {
            var type = ParseType();
            var name = Consume(TokenType.Identifier, "Expected member name");
            
            // Interface property
            if (Match(TokenType.LeftBrace))
            {
                bool hasGetter = false;
                bool hasSetter = false;
                
                while (!Check(TokenType.RightBrace) && !IsAtEnd())
                {
                    if (Match(TokenType.Get))
                    {
                        hasGetter = true;
                        Consume(TokenType.Semicolon, "Expected ';' after get");
                    }
                    else if (Match(TokenType.Set))
                    {
                        hasSetter = true;
                        Consume(TokenType.Semicolon, "Expected ';' after set");
                    }
                }
                
                Consume(TokenType.RightBrace, "Expected '}' after property");
                
                return new PropertyDeclaration(name, type, 
                    hasGetter ? new BlockStatement(new List<Statement>()) : null,
                    hasSetter ? new BlockStatement(new List<Statement>()) : null);
            }
            // Interface method
            else if (Match(TokenType.LeftParen))
            {
                var parameters = new List<Parameter>();
                
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        var paramType = ParseType();
                        var paramName = Consume(TokenType.Identifier, "Expected parameter name");
                        parameters.Add(new Parameter(paramType, paramName.Lexeme));
                    } while (Match(TokenType.Comma));
                }
                
                Consume(TokenType.RightParen, "Expected ')' after parameters");
                Consume(TokenType.Semicolon, "Expected ';' after interface method");
                
                return new FunctionDeclaration(name, type, parameters, null);
            }
            
            throw Error(Current(), "Invalid interface member");
        }
        
        private bool IsModifier()
        {
            return Check(TokenType.Public) || Check(TokenType.Private) ||
                   Check(TokenType.Protected) || Check(TokenType.Internal) ||
                   Check(TokenType.Static) || Check(TokenType.Abstract) ||
                   Check(TokenType.Virtual) || Check(TokenType.Override) ||
                   Check(TokenType.Sealed) || Check(TokenType.Readonly) ||
                   Check(TokenType.Const) || Check(TokenType.Async);
        }
        
        private Modifier ParseModifier()
        {
            var token = Advance();
            return token.Type switch
            {
                TokenType.Public => Modifier.Public,
                TokenType.Private => Modifier.Private,
                TokenType.Protected => Modifier.Protected,
                TokenType.Internal => Modifier.Internal,
                TokenType.Static => Modifier.Static,
                TokenType.Abstract => Modifier.Abstract,
                TokenType.Virtual => Modifier.Virtual,
                TokenType.Override => Modifier.Override,
                TokenType.Sealed => Modifier.Sealed,
                TokenType.Readonly => Modifier.Readonly,
                TokenType.Const => Modifier.Const,
                TokenType.Async => Modifier.Async,
                _ => throw Error(token, "Invalid modifier")
            };
        }
        
        private TypeNode ParseType()
        {
            var baseType = ParseBaseType();
            
            // Handle arrays
            if (Match(TokenType.LeftBracket))
            {
                int rank = 1;
                while (Match(TokenType.Comma))
                {
                    rank++;
                }
                Consume(TokenType.RightBracket, "Expected ']' after array type");
                
                return new TypeNode(baseType.Name, null, true, rank);
            }
            
            // Handle nullable
            if (Match(TokenType.Question))
            {
                return new TypeNode(baseType.Name, baseType.TypeArguments, 
                    baseType.IsArray, baseType.ArrayRank, true);
            }
            
            return baseType;
        }
        
        private TypeNode ParseBaseType()
        {
            var typeName = Consume(TokenType.Identifier, "Expected type name").Lexeme;
            
            // Handle generics
            if (Match(TokenType.Less))
            {
                var typeArgs = new List<TypeNode>();
                
                do
                {
                    typeArgs.Add(ParseType());
                } while (Match(TokenType.Comma));
                
                Consume(TokenType.Greater, "Expected '>' after type arguments");
                
                return new TypeNode(typeName, typeArgs);
            }
            
            return new TypeNode(typeName);
        }

        #endregion
    }
} 
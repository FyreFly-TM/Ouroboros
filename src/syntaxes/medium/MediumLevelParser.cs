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
        
        // Helper to convert local TypeParameter to Core.AST.TypeParameter
        private List<Core.AST.TypeParameter>? ConvertTypeParameters(List<TypeParameter>? localParams)
        {
            if (localParams == null || localParams.Count == 0) return null;
            return localParams.Select(p => new Core.AST.TypeParameter(
                p.Name,
                p.Constraints,
                p.Variance == Variance.Covariant,
                p.Variance == Variance.Contravariant
            )).ToList();
        }

        public Core.AST.Program Parse()
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
            
            return new Core.AST.Program(statements);
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
                            // Function declaration not implemented yet
                            throw new NotImplementedException("Function declaration parsing not implemented");
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
            if (Match(TokenType.Do)) throw new NotImplementedException("Do-while parsing not implemented");
            if (Match(TokenType.Switch)) return ParseSwitchStatement();
            if (Match(TokenType.Return)) return ParseReturnStatement();
            if (Match(TokenType.Break)) return ParseBreakStatement();
            if (Match(TokenType.Continue)) return ParseContinueStatement();
            if (Match(TokenType.Throw)) return ParseThrowStatement();
            if (Match(TokenType.Try)) return ParseTryStatement();
            
            // Blocks
            if (Match(TokenType.LeftBrace)) return ParseBlockStatement();
            
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
            var whileToken = Previous();
            Consume(TokenType.LeftParen, "Expected '(' after 'while'");
            Expression condition = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after while condition");
            
            Statement body = ParseStatement();
            
            return new WhileStatement(whileToken, condition, body);
        }

        private Statement ParseForStatement()
        {
            var forToken = Previous();
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
            
            return new ForStatement(forToken, initializer, condition, increment, body);
        }

        private Statement ParseSwitchStatement()
        {
            Consume(TokenType.LeftParen, "Expected '(' after 'switch'");
            Expression expression = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after switch expression");
            Consume(TokenType.LeftBrace, "Expected '{' after switch expression");
            
            List<CaseClause> cases = new List<CaseClause>();
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
                    
                    cases.Add(new CaseClause(caseValue, statements));
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
            
            // Need to create a default case from the statements
            Statement? defaultCase = null;
            if (defaultStatements != null && defaultStatements.Count > 0)
            {
                defaultCase = new BlockStatement(defaultStatements);
            }
            
            return new SwitchStatement(Previous(), expression, cases, defaultCase);
        }

        private Statement ParseTryStatement()
        {
            var tryToken = Previous(); // Get the 'try' token
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
                var exceptionTypeNode = exceptionType != null ? new TypeNode(exceptionType) : null;
                catchClauses.Add(new CatchClause(exceptionTypeNode, variableName, catchBlock));
            }
            
            if (Match(TokenType.Finally))
            {
                finallyBlock = ParseBlockStatement();
            }
            
            if (catchClauses.Count == 0 && finallyBlock == null)
            {
                throw Error(Current(), "Try statement must have at least one catch or finally clause");
            }
            
            return new TryStatement(tryToken, tryBlock, catchClauses, finallyBlock);
        }

        private Statement ParseReturnStatement()
        {
            var returnToken = Previous(); // Get the 'return' token
            Expression value = null;
            if (!Match(TokenType.Semicolon))
            {
                value = ParseExpression();
            }
            Consume(TokenType.Semicolon, "Expected ';' after return value");
            
            return new ReturnStatement(returnToken, value);
        }

        private Statement ParseBreakStatement()
        {
            var breakToken = Previous();
            Consume(TokenType.Semicolon, "Expected ';' after 'break'");
            return new BreakStatement(breakToken);
        }

        private Statement ParseContinueStatement()
        {
            var continueToken = Previous();
            Consume(TokenType.Semicolon, "Expected ';' after 'continue'");
            return new ContinueStatement(continueToken);
        }

        private Statement ParseThrowStatement()
        {
            var throwToken = Previous();
            Expression exception = ParseExpression();
            Consume(TokenType.Semicolon, "Expected ';' after throw expression");
            
            return new ThrowStatement(throwToken, exception);
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
                // Block body - for now, just parse the block and use first expression
                // In a real implementation, you'd need to handle this properly
                var blockBody = ParseBlockStatement();
                // Create a dummy expression for the block
                body = new LiteralExpression(new Token(TokenType.NullLiteral, "null", null, 0, 0, 0, 0, "", SyntaxLevel.Medium));
            }
            else
            {
                // Expression body - use ParseAssignment to support throw expressions
                body = ParseAssignment();
            }
            
            // Convert string parameter names to Parameter objects
            var parameterObjects = parameters.Select(name => new Parameter(
                new TypeNode("var"), // Infer type
                name,
                null,
                ParameterModifier.None
            )).ToList();
            
            return new LambdaExpression(parameterObjects, body);
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
            if (Match(TokenType.IntegerLiteral) || 
                Match(TokenType.FloatLiteral) ||
                Match(TokenType.DoubleLiteral) ||
                Match(TokenType.StringLiteral) ||
                Match(TokenType.BooleanLiteral) ||
                Match(TokenType.NullLiteral))
            {
                return new ConstantPattern(new LiteralExpression(Previous()));
            }
            
            // Type pattern with optional variable binding
            if (Check(TokenType.Identifier))
            {
                var checkpoint = current;
                var type = ParseType();
                
                // Check for variable binding
                if (Check(TokenType.Identifier))
                {
                    var variable = Advance();
                    return new TypePattern(type, variable.Lexeme);
                }
                else
                {
                    return new TypePattern(type, null);
                }
            }
            
            // Wildcard pattern
            if (Match(TokenType.Underscore))
            {
                return new WildcardPattern();
            }
            
            // Tuple pattern
            if (Match(TokenType.LeftParen))
            {
                var patterns = new List<Pattern>();
                
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        patterns.Add(ParsePattern());
                    } while (Match(TokenType.Comma));
                }
                
                Consume(TokenType.RightParen, "Expected ')' after tuple pattern");
                return new TuplePattern(patterns);
            }
            
            // Array/List pattern
            if (Match(TokenType.LeftBracket))
            {
                var patterns = new List<Pattern>();
                
                if (!Check(TokenType.RightBracket))
                {
                    do
                    {
                        patterns.Add(ParsePattern());
                    } while (Match(TokenType.Comma));
                }
                
                Consume(TokenType.RightBracket, "Expected ']' after array pattern");
                return new ArrayPattern(patterns);
            }
            
            // Range pattern
            if (IsNumericLiteral(Current()))
            {
                var start = ParseExpression();
                if (Match(TokenType.Range))
                {
                    var end = ParseExpression();
                    return new RangePattern(start, end);
                }
                else
                {
                    return new ConstantPattern(start);
                }
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
            return new ParseException(message, token);
        }

        private void Synchronize()
        {
            // Improved error recovery - synchronize to next statement
            while (!IsAtEnd())
            {
                if (Previous().Type == TokenType.Semicolon)
                    return;
                    
                switch (Current().Type)
                {
                    case TokenType.Class:
                    case TokenType.Struct:
                    case TokenType.Interface:
                    case TokenType.Enum:
                    case TokenType.UnionKeyword:
                    case TokenType.Function:
                    case TokenType.If:
                    case TokenType.While:
                    case TokenType.For:
                    case TokenType.Do:
                    case TokenType.Switch:
                    case TokenType.Return:
                    case TokenType.Break:
                    case TokenType.Continue:
                    case TokenType.Try:
                    case TokenType.Throw:
                    case TokenType.Var:
                    case TokenType.Const:
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
        
        private bool IsNumericLiteral(Token token)
        {
            return token.Type == TokenType.IntegerLiteral || 
                   token.Type == TokenType.FloatLiteral || 
                   token.Type == TokenType.DoubleLiteral;
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
                    
                    typeParameters.Add(new TypeParameter(paramName.Lexeme, Variance.Invariant, constraints));
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
            
            return new ClassDeclaration(classToken, name, baseClass, interfaces, members, ConvertTypeParameters(typeParameters));
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
                    typeParameters.Add(new TypeParameter(paramName.Lexeme, Variance.Invariant, null));
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
            
            return new StructDeclaration(structToken, name, interfaces, members, ConvertTypeParameters(typeParameters));
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
                    typeParameters.Add(new TypeParameter(paramName.Lexeme, Variance.Invariant, null));
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
            
            return new InterfaceDeclaration(interfaceToken, name, baseInterfaces, members, ConvertTypeParameters(typeParameters));
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
                    if (Check(TokenType.Identifier) && Current().Lexeme == "get")
                    {
                        Advance(); // consume 'get'
                        if (Match(TokenType.Semicolon))
                        {
                            // Auto-property getter
                            getter = new BlockStatement(new List<Statement>());
                        }
                        else
                        {
                            getter = ParseBlockStatement() as BlockStatement;
                        }
                    }
                    else if (Check(TokenType.Identifier) && Current().Lexeme == "set")
                    {
                        Advance(); // consume 'set'
                        if (Match(TokenType.Semicolon))
                        {
                            // Auto-property setter
                            setter = new BlockStatement(new List<Statement>());
                        }
                        else
                        {
                            setter = ParseBlockStatement() as BlockStatement;
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
                    body = ParseBlockStatement() as BlockStatement;
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
                    if (Check(TokenType.Identifier) && Current().Lexeme == "get")
                    {
                        Advance(); // consume 'get'
                        hasGetter = true;
                        Consume(TokenType.Semicolon, "Expected ';' after get");
                    }
                    else if (Check(TokenType.Identifier) && Current().Lexeme == "set")
                    {
                        Advance(); // consume 'set'
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

        // ===== EXPRESSION PARSING =====
        
        private Expression ParseExpression()
        {
            return ParseConditional();
        }
        
        private Expression ParseConditional()
        {
            var expr = ParseCoalescing();
            
            if (Match(TokenType.Question))
            {
                var thenExpr = ParseExpression();
                Consume(TokenType.Colon, "Expected ':' in conditional expression");
                var elseExpr = ParseExpression();
                
                return new ConditionalExpression(expr, thenExpr, elseExpr);
            }
            
            return expr;
        }
        
        private Expression ParseCoalescing()
        {
            var expr = ParseLogicalOr();
            
            while (Match(TokenType.NullCoalesce))
            {
                var op = Previous();
                var right = ParseLogicalOr();
                expr = new BinaryExpression(expr, op, right);
            }
            
            return expr;
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
            var expr = ParseBitwiseOr();
            
            while (Match(TokenType.LogicalAnd))
            {
                var op = Previous();
                var right = ParseBitwiseOr();
                expr = new BinaryExpression(expr, op, right);
            }
            
            return expr;
        }
        
        private Expression ParseBitwiseOr()
        {
            var expr = ParseBitwiseXor();
            
            while (Match(TokenType.BitwiseOr))
            {
                var op = Previous();
                var right = ParseBitwiseXor();
                expr = new BinaryExpression(expr, op, right);
            }
            
            return expr;
        }
        
        private Expression ParseBitwiseXor()
        {
            var expr = ParseBitwiseAnd();
            
            while (Match(TokenType.BitwiseXor))
            {
                var op = Previous();
                var right = ParseBitwiseAnd();
                expr = new BinaryExpression(expr, op, right);
            }
            
            return expr;
        }
        
        private Expression ParseBitwiseAnd()
        {
            var expr = ParseEquality();
            
            while (Match(TokenType.BitwiseAnd))
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
            
            while (Match(TokenType.Less, TokenType.LessEqual, 
                         TokenType.Greater, TokenType.GreaterEqual,
                         TokenType.Is, TokenType.As))
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
            
            while (Match(TokenType.Multiply, TokenType.Divide, TokenType.Modulo))
            {
                var op = Previous();
                var right = ParseUnary();
                expr = new BinaryExpression(expr, op, right);
            }
            
            return expr;
        }
        
        private Expression ParseUnary()
        {
            if (Match(TokenType.LogicalNot, TokenType.BitwiseNot, TokenType.Minus, TokenType.Plus,
                     TokenType.Increment, TokenType.Decrement))
            {
                var op = Previous();
                var right = ParseUnary();
                return new UnaryExpression(op, right);
            }
            
            // Type cast
            if (Match(TokenType.LeftParen))
            {
                var checkpoint = current;
                try
                {
                    var type = ParseType();
                    if (Match(TokenType.RightParen))
                    {
                        // It's a cast
                        var expr = ParseUnary();
                        return new CastExpression(Previous(), type, expr);
                    }
                }
                catch { }
                // Not a cast, reset
                current = checkpoint;
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
                    // Method call
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
                    // Array index
                    var index = ParseExpression();
                    Consume(TokenType.RightBracket, "Expected ']' after array index");
                    expr = new IndexExpression(expr, index);
                }
                else if (Match(TokenType.Dot))
                {
                    // Member access
                    var name = Consume(TokenType.Identifier, "Expected member name");
                    expr = new MemberExpression(expr, Previous(), name);
                }
                else if (Match(TokenType.Arrow))
                {
                    // Pointer member access
                    var name = Consume(TokenType.Identifier, "Expected member name");
                    expr = new PointerMemberExpression(expr, name);
                }
                else if (Match(TokenType.Increment, TokenType.Decrement))
                {
                    // Postfix increment/decrement
                    expr = new PostfixExpression(expr, Previous());
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
            // Literals
            if (Match(TokenType.IntegerLiteral, TokenType.FloatLiteral, 
                     TokenType.DoubleLiteral, TokenType.StringLiteral,
                     TokenType.CharLiteral, TokenType.BooleanLiteral, 
                     TokenType.NullLiteral))
            {
                return new LiteralExpression(Previous());
            }
            
            // Identifier
            if (Match(TokenType.Identifier))
            {
                return new IdentifierExpression(Previous());
            }
            
            // This
            if (Match(TokenType.This))
            {
                return new ThisExpression(Previous());
            }
            
            // Base
            if (Match(TokenType.Base))
            {
                return new BaseExpression(Previous());
            }
            
            // New
            if (Match(TokenType.New))
            {
                var type = ParseType();
                
                // Object creation
                if (Match(TokenType.LeftParen))
                {
                    var args = new List<Expression>();
                    if (!Check(TokenType.RightParen))
                    {
                        do
                        {
                            args.Add(ParseExpression());
                        } while (Match(TokenType.Comma));
                    }
                    Consume(TokenType.RightParen, "Expected ')' after arguments");
                    
                    // Object initializer
                    Dictionary<string, Expression> initializers = null;
                    if (Match(TokenType.LeftBrace))
                    {
                        initializers = new Dictionary<string, Expression>();
                        while (!Check(TokenType.RightBrace) && !IsAtEnd())
                        {
                            var name = Consume(TokenType.Identifier, "Expected property name");
                            Consume(TokenType.Assign, "Expected '=' in initializer");
                            var value = ParseExpression();
                            initializers[name.Lexeme] = value;
                            
                            if (!Match(TokenType.Comma))
                                break;
                        }
                        Consume(TokenType.RightBrace, "Expected '}' after initializers");
                    }
                    
                    return new NewExpression(type, args, initializers);
                }
                // Array creation
                else if (Match(TokenType.LeftBracket))
                {
                    var size = ParseExpression();
                    Consume(TokenType.RightBracket, "Expected ']' after array size");
                    
                    // Array initializer
                    List<Expression> elements = null;
                    if (Match(TokenType.LeftBrace))
                    {
                        elements = new List<Expression>();
                        if (!Check(TokenType.RightBrace))
                        {
                            do
                            {
                                elements.Add(ParseExpression());
                            } while (Match(TokenType.Comma));
                        }
                        Consume(TokenType.RightBrace, "Expected '}' after array elements");
                    }
                    
                    return new ArrayCreationExpression(type, size, elements);
                }
            }
            
            // Typeof
            if (Match(TokenType.Typeof))
            {
                Consume(TokenType.LeftParen, "Expected '(' after 'typeof'");
                var type = ParseType();
                Consume(TokenType.RightParen, "Expected ')' after type");
                return new TypeofExpression(type);
            }
            
            // Sizeof
            if (Match(TokenType.Sizeof))
            {
                Consume(TokenType.LeftParen, "Expected '(' after 'sizeof'");
                var type = ParseType();
                Consume(TokenType.RightParen, "Expected ')' after type");
                return new SizeofExpression(type);
            }
            
            // Lambda
            if (Check(TokenType.Identifier) || Check(TokenType.LeftParen))
            {
                var checkpoint = current;
                try
                {
                    return ParseLambda();
                }
                catch
                {
                    // Not a lambda, reset
                    current = checkpoint;
                }
            }
            
            // Parenthesized expression
            if (Match(TokenType.LeftParen))
            {
                var expr = ParseExpression();
                Consume(TokenType.RightParen, "Expected ')' after expression");
                return expr;
            }
            
            // Array literal
            if (Match(TokenType.LeftBracket))
            {
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
            
            // Anonymous object
            if (Check(TokenType.LeftBrace))
            {
                return ParseAnonymousObject();
            }
            
            throw Error(Current(), "Expected expression");
        }
        
        private Expression ParseAssignment()
        {
            var expr = ParseConditional();
            
            // Handle all assignment operators
            if (Match(TokenType.Assign, TokenType.PlusEqual, TokenType.MinusEqual,
                     TokenType.MultiplyEqual, TokenType.DivideEqual, TokenType.ModuloEqual,
                     TokenType.AndEqual, TokenType.OrEqual, TokenType.XorEqual,
                     TokenType.LeftShiftEqual, TokenType.RightShiftEqual,
                     TokenType.NullCoalescingEqual))
            {
                var op = Previous();
                var right = ParseAssignment();
                
                if (expr is IdentifierExpression || 
                    expr is MemberAccessExpression || 
                    expr is IndexExpression)
                {
                    return new AssignmentExpression(expr, op, right);
                }
                
                throw Error(op, "Invalid assignment target");
            }
            
            return expr;
        }
        
        private Expression ParseAnonymousObject()
        {
            Consume(TokenType.LeftBrace, "Expected '{' to start anonymous object");
            
            var properties = new List<(string name, Expression value)>();
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                // Property name
                string propName;
                if (Check(TokenType.Identifier))
                {
                    propName = Advance().Lexeme;
                }
                else if (Check(TokenType.StringLiteral))
                {
                    propName = Advance().Lexeme.Trim('"');
                }
                else
                {
                    throw Error(Current(), "Expected property name");
                }
                
                Expression propValue;
                if (Match(TokenType.Colon))
                {
                    // Explicit property value
                    propValue = ParseExpression();
                }
                else
                {
                    // Property shorthand (use identifier as value)
                    propValue = new IdentifierExpression(Previous());
                }
                
                properties.Add((propName, propValue));
                
                if (!Match(TokenType.Comma))
                {
                    break;
                }
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after anonymous object");
            
            return new AnonymousObjectExpression(properties);
        }

        #endregion
    }

    // Pattern classes for pattern matching
    public class TypePattern : Pattern
    {
        public TypeNode Type { get; }
        public string? VariableName { get; }
        
        public TypePattern(TypeNode type, string? variableName)
        {
            Type = type;
            VariableName = variableName;
        }
    }

    public class WildcardPattern : Pattern
    {
    }

    public class TuplePattern : Pattern
    {
        public List<Pattern> Patterns { get; }
        
        public TuplePattern(List<Pattern> patterns)
        {
            Patterns = patterns;
        }
    }

    public class ArrayPattern : Pattern
    {
        public List<Pattern> Patterns { get; }
        
        public ArrayPattern(List<Pattern> patterns)
        {
            Patterns = patterns;
        }
    }

    public class RangePattern : Pattern
    {
        public Expression Start { get; }
        public Expression End { get; }
        
        public RangePattern(Expression start, Expression end)
        {
            Start = start;
            End = end;
        }
    }

    public enum Variance
    {
        Invariant,
        Covariant,
        Contravariant
    }

    public class TypeParameter
    {
        public string Name { get; }
        public Variance Variance { get; }
        public List<TypeNode>? Constraints { get; }
        
        public TypeParameter(string name, Variance variance, List<TypeNode>? constraints)
        {
            Name = name;
            Variance = variance;
            Constraints = constraints;
        }
    }

    public class GenericConstraint
    {
        public string TypeParameterName { get; }
        public List<TypeNode> ConstraintTypes { get; }
        
        public GenericConstraint(string typeParameterName, List<TypeNode> constraintTypes)
        {
            TypeParameterName = typeParameterName;
            ConstraintTypes = constraintTypes;
        }
    }

    public class PatternCase
    {
        public Pattern Pattern { get; }
        public Expression? Guard { get; }
        public Expression Result { get; }
        
        public PatternCase(Pattern pattern, Expression? guard, Expression result)
        {
            Pattern = pattern;
            Guard = guard;
            Result = result;
        }
    }

    public class PatternMatchExpression : Expression
    {
        public Expression Target { get; }
        public List<PatternCase> Cases { get; }
        public Expression? DefaultCase { get; }
        
        public PatternMatchExpression(Expression target, List<PatternCase> cases, Expression? defaultCase)
            : base(target.Token)
        {
            Target = target;
            Cases = cases;
            DefaultCase = defaultCase;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitMatchExpression(new MatchExpression(Target, 
                Cases.Select(c => new MatchArm(c.Pattern, c.Guard, c.Result)).ToList()));
        }
    }

    public class AnonymousObjectExpression : Expression
    {
        public List<(string name, Expression value)> Properties { get; }
        
        public AnonymousObjectExpression(List<(string name, Expression value)> properties)
            : base(new Token(TokenType.LeftBrace, "{", null, 0, 0, 0, 0, "", SyntaxLevel.Medium))
        {
            Properties = properties;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            // Convert to struct literal for visitor
            var fields = Properties.ToDictionary(p => p.name, p => p.value);
            return visitor.VisitStructLiteral(new StructLiteral(Token, fields));
        }
    }

    public class IdentifierPattern : Pattern
    {
        public Token Identifier { get; }
        
        public IdentifierPattern(Token identifier)
        {
            Identifier = identifier;
        }
    }

    // Additional Expression types for enhanced medium-level syntax
    public class ConditionalAccessExpression : Expression
    {
        public Expression Object { get; }
        public string MemberName { get; }
        
        public ConditionalAccessExpression(Expression obj, string memberName)
            : base(obj.Token)
        {
            Object = obj;
            MemberName = memberName;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            // Convert to conditional expression for visitor
            return visitor.VisitConditionalExpression(
                new ConditionalExpression(
                    new BinaryExpression(Object, 
                        new Token(TokenType.NotEqual, "!=", null, Line, Column, 0, 0, "", SyntaxLevel.Medium),
                        new LiteralExpression(new Token(TokenType.NullLiteral, "null", null, Line, Column, 0, 0, "", SyntaxLevel.Medium))),
                    new MemberExpression(Object, 
                        new Token(TokenType.Dot, ".", null, Line, Column, 0, 0, "", SyntaxLevel.Medium),
                        new Token(TokenType.Identifier, MemberName, MemberName, Line, Column, 0, 0, "", SyntaxLevel.Medium)),
                    new LiteralExpression(new Token(TokenType.NullLiteral, "null", null, Line, Column, 0, 0, "", SyntaxLevel.Medium))));
        }
    }

    public class NullCoalescingExpression : Expression
    {
        public Expression Left { get; }
        public Expression Right { get; }
        
        public NullCoalescingExpression(Expression left, Expression right)
            : base(left.Token)
        {
            Left = left;
            Right = right;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            // Convert to conditional expression for visitor
            return visitor.VisitConditionalExpression(
                new ConditionalExpression(
                    new BinaryExpression(Left,
                        new Token(TokenType.NotEqual, "!=", null, Line, Column, 0, 0, "", SyntaxLevel.Medium),
                        new LiteralExpression(new Token(TokenType.NullLiteral, "null", null, Line, Column, 0, 0, "", SyntaxLevel.Medium))),
                    Left,
                    Right));
        }
    }

    public class RangeExpression : Expression
    {
        public Expression? Start { get; }
        public Expression? End { get; }
        public bool IsInclusive { get; }
        
        public RangeExpression(Expression? start, Expression? end, bool isInclusive)
            : base(new Token(TokenType.Range, "..", null, 0, 0, 0, 0, "", SyntaxLevel.Medium))
        {
            Start = start;
            End = end;
            IsInclusive = isInclusive;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            // For now, treat as a binary expression
            var startExpr = Start ?? new LiteralExpression(new Token(TokenType.IntegerLiteral, "0", 0, Line, Column, 0, 0, "", SyntaxLevel.Medium));
            var endExpr = End ?? new LiteralExpression(new Token(TokenType.IntegerLiteral, "int.MaxValue", int.MaxValue, Line, Column, 0, 0, "", SyntaxLevel.Medium));
            return visitor.VisitBinaryExpression(new BinaryExpression(startExpr, Token, endExpr));
        }
    }

    // Placeholder for ConstantPattern if not defined elsewhere
    public class ConstantPattern : Pattern
    {
        public Expression Value { get; }
        
        public ConstantPattern(Expression value)
        {
            Value = value;
        }
    }

    // End of file
} 
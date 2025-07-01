using System;
using System.Collections.Generic;
using System.Linq;
using Ouroboros.Core;
using Ouroboros.Core.AST;
using Ouroboros.Core.Lexer;
using Ouroboros.Tokens;

namespace Ouroboros.Syntaxes.Low
{
    /// <summary>
    /// Parser for low-level Ouroboros syntax
    /// Handles manual memory management, pointers, and inline assembly
    /// </summary>
    public class LowLevelParser
    {
        private readonly List<Token> tokens;
        private int current;
        private readonly Dictionary<string, int> labels;
        private readonly List<AsmInstruction> instructions;
        
        public LowLevelParser(List<Token> tokens)
        {
            this.tokens = tokens;
            current = 0;
            this.labels = new Dictionary<string, int>();
            this.instructions = new List<AsmInstruction>();
        }
        
        public void SetCurrentPosition(int position)
        {
            current = position;
        }
        
        public int GetCurrentPosition()
        {
            return current;
        }
        
        public Expression PublicParseExpression()
        {
            return ParseExpression();
        }
        
        public List<Statement> Parse()
        {
            var statements = new List<Statement>();
            
            while (!IsAtEnd())
            {
                SkipWhitespaceAndComments();
                if (IsAtEnd()) break;
                
                    var stmt = ParseStatement();
                    if (stmt != null)
                {
                        statements.Add(stmt);
                }
            }
            
            return statements;
        }
        
        public Statement ParseStatement()
        {
            // Check for labels
            if (Check(TokenType.Identifier) && Peek(1)?.Type == TokenType.Colon)
            {
                var label = Advance();
                Advance(); // consume colon
                labels[label.Lexeme] = instructions.Count;
                return null; // Labels don't generate statements
            }
            
            // Check for assembly block
            if (Match(TokenType.Assembly) || (Match(TokenType.At) && Match(TokenType.Assembly)))
            {
                return ParseAssemblyBlock();
            }
            
            // Check for function declaration
            if (Match(TokenType.Function))
            {
                return ParseLowLevelFunction();
            }
            
            // Check for data declarations
            if (Match(TokenType.Data))
            {
                return ParseDataDeclaration();
            }
            
            // Check for struct declaration
            if (Match(TokenType.Struct))
            {
                return ParseStructDeclaration();
            }
            
            // Check for union declaration
            if (Match(TokenType.UnionKeyword))
            {
                return ParseUnionDeclaration();
            }
            
            // Check for typedef
            if (Match(TokenType.Typedef))
            {
                return ParseTypedefStatement();
            }
            
            // Check for static/extern/const variable declarations
            if (IsStorageSpecifier() || IsTypeKeyword())
            {
                return ParseVariableDeclaration();
            }
            
            // Check for unsafe block
            if (Match(TokenType.Unsafe))
            {
                return ParseUnsafeBlock();
            }
            
            // Check for fixed statement
            if (Match(TokenType.Fixed))
            {
                return ParseFixedStatement();
            }
            
            // Check for directives
            if (Current().Type == TokenType.Dot)
            {
                return ParseDirective();
            }
            
            // Parse as assembly instruction
            return ParseInstruction();
        }
        
        private Statement ParseAssemblyBlock()
        {
            var asmStatements = new List<string>();
            var registers = new HashSet<string>();
            var variables = new Dictionary<string, string>();
            
            // Optional target specifier
            string target = "x86_64";
            string dialect = "intel"; // default to Intel syntax
            bool isVolatile = false;
            
            // Parse assembly attributes
            if (Match(TokenType.LeftParen))
            {
                // Parse attributes like target, dialect, volatile
                do
                {
                    if (Match(TokenType.Identifier))
                    {
                        var attr = Previous().Lexeme;
                        if (attr == "target" && Match(TokenType.Assign))
                        {
                            target = Consume(TokenType.StringLiteral, "Expected target string").Lexeme.Trim('"');
                        }
                        else if (attr == "dialect" && Match(TokenType.Assign))
                        {
                            dialect = Consume(TokenType.StringLiteral, "Expected dialect string").Lexeme.Trim('"');
                        }
                        else if (attr == "volatile")
                        {
                            isVolatile = true;
                        }
                    }
                } while (Match(TokenType.Comma));
                
                Consume(TokenType.RightParen, "Expected ')' after assembly attributes");
            }
            
            Consume(TokenType.LeftBrace, "Expected '{' after @asm");
            
            // Parse optional input/output/clobber lists
            var inputs = new List<AsmOperand>();
            var outputs = new List<AsmOperand>();
            var clobbers = new List<string>();
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                // Check for constraint syntax
                if (Match(TokenType.Colon))
                {
                    // Output operands
                    if (!Check(TokenType.Colon))
                    {
                        outputs = ParseAsmOperands();
                    }
                    
                    if (Match(TokenType.Colon))
                    {
                        // Input operands
                        if (!Check(TokenType.Colon))
                        {
                            inputs = ParseAsmOperands();
                        }
                        
                        if (Match(TokenType.Colon))
                        {
                            // Clobber list
                            clobbers = ParseClobberList();
                        }
                    }
                    break;
                }
                
                var line = ParseAsmLine();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    // Process line for variable substitutions
                    line = ProcessAsmVariables(line, variables);
                    asmStatements.Add(line);
                }
            }
            
            Consume(TokenType.RightBrace, "Expected '}' to close assembly block");
            
            var asmCode = string.Join("\n", asmStatements);
            
            // Create extended assembly statement with constraints
            return new ExtendedAssemblyStatement(
                CreateToken(TokenType.Assembly, "@asm", null),
                asmCode,
                target,
                dialect,
                isVolatile,
                outputs,
                inputs,
                clobbers
            );
        }
        
        private List<AsmOperand> ParseAsmOperands()
        {
            var operands = new List<AsmOperand>();
            
            do
            {
                // Parse constraint string
                var constraint = Consume(TokenType.StringLiteral, "Expected constraint string").Lexeme.Trim('"');
                
                // Parse expression
                Consume(TokenType.LeftParen, "Expected '(' after constraint");
                var expr = ParseExpression();
                Consume(TokenType.RightParen, "Expected ')' after expression");
                
                operands.Add(new AsmOperand(constraint, expr));
                
            } while (Match(TokenType.Comma));
            
            return operands;
        }
        
        private List<string> ParseClobberList()
        {
            var clobbers = new List<string>();
            
            do
            {
                var clobber = Consume(TokenType.StringLiteral, "Expected clobber string").Lexeme.Trim('"');
                clobbers.Add(clobber);
            } while (Match(TokenType.Comma));
            
            return clobbers;
        }
        
        private string ProcessAsmVariables(string line, Dictionary<string, string> variables)
        {
            // Replace %0, %1, etc. with actual register names
            // Replace ${varname} with variable references
            
            // Simple regex replacement for demonstration
            // In production, use proper parsing
            return line;
        }
        
        private string ParseAsmLine()
        {
            var parts = new List<string>();
            
            while (!Check(TokenType.NewLine) && !Check(TokenType.Semicolon) && 
                   !Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var token = Advance();
                parts.Add(token.Lexeme);
            }
            
            if (Match(TokenType.NewLine) || Match(TokenType.Semicolon))
            {
                // Consume line ending
            }
            
            return string.Join(" ", parts);
        }
        
        private Statement ParseLowLevelFunction()
        {
            var name = Consume(TokenType.Identifier, "Expected function name");
            
            // Parse parameters
            var parameters = new List<Parameter>();
            if (Match(TokenType.LeftParen))
            {
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
            }
            
            // Parse return type
            var returnType = new TypeNode("void");
            if (Match(TokenType.Arrow))
            {
                returnType = ParseType();
            }
            
            // Parse body
            Consume(TokenType.LeftBrace, "Expected '{' before function body");
            var body = ParseLowLevelBlock();
            Consume(TokenType.RightBrace, "Expected '}' after function body");
            
            return new FunctionDeclaration(
                name,
                returnType,
                parameters,
                body,
                isAsync: false
            );
        }
        
        private BlockStatement ParseLowLevelBlock()
        {
            var statements = new List<Statement>();
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                SkipWhitespaceAndComments();
                var stmt = ParseStatement();
                if (stmt != null)
                {
                    statements.Add(stmt);
                }
            }
            
            return new BlockStatement(statements);
        }
        
        private Statement ParseInstruction()
        {
            if (!Check(TokenType.Identifier))
            {
                throw Error(Current(), "Expected instruction mnemonic");
            }
            
            var mnemonic = Advance();
            var operands = new List<Expression>();
            
            // Check for size suffix (e.g., movl, movq, movb)
            string size = "";
            if (mnemonic.Lexeme.Length > 3)
            {
                var lastChar = mnemonic.Lexeme[^1];
                if ("bwlq".Contains(lastChar))
                {
                    size = lastChar.ToString();
                }
            }
            
            // Parse operands
            while (!Check(TokenType.NewLine) && !Check(TokenType.Semicolon) && 
                   !IsAtEnd() && !Check(TokenType.RightBrace))
            {
                operands.Add(ParseOperand());
                
                if (!Match(TokenType.Comma))
                {
                    break;
                }
            }
            
            // Validate instruction
            ValidateInstruction(mnemonic.Lexeme, operands);
            
            // Create assembly instruction with metadata
            var asmText = mnemonic.Lexeme;
            if (operands.Count > 0)
            {
                asmText += " " + string.Join(", ", operands.Select(FormatOperand));
            }
            
            return new AssemblyInstructionStatement(
                mnemonic, 
                asmText,
                operands,
                size
            );
        }
        
        private void ValidateInstruction(string mnemonic, List<Expression> operands)
        {
            // Basic validation for common x86 instructions
            var mnemonicBase = mnemonic.TrimEnd('b', 'w', 'l', 'q');
            
            switch (mnemonicBase.ToLower())
            {
                case "mov":
                case "add":
                case "sub":
                case "cmp":
                    if (operands.Count != 2)
                        throw Error(Current(), $"{mnemonic} requires exactly 2 operands");
                    break;
                    
                case "push":
                case "pop":
                case "inc":
                case "dec":
                case "not":
                case "neg":
                    if (operands.Count != 1)
                        throw Error(Current(), $"{mnemonic} requires exactly 1 operand");
                    break;
                    
                case "ret":
                case "nop":
                case "hlt":
                case "cli":
                case "sti":
                    if (operands.Count != 0)
                        throw Error(Current(), $"{mnemonic} takes no operands");
                    break;
                    
                case "call":
                case "jmp":
                case "je":
                case "jne":
                case "jg":
                case "jl":
                case "jge":
                case "jle":
                    if (operands.Count != 1)
                        throw Error(Current(), $"{mnemonic} requires exactly 1 operand");
                    break;
            }
        }
        
        private Expression ParseOperand()
        {
            // Register
            if (Check(TokenType.Modulo))
            {
                Advance(); // consume %
                var reg = Consume(TokenType.Identifier, "Expected register name");
                return new IdentifierExpression(reg);
            }
            
            // Immediate value
            if (Check(TokenType.IntegerLiteral) || Check(TokenType.HexLiteral))
            {
                return new LiteralExpression(Advance());
            }
            
            // Memory reference [base + index*scale + disp]
                if (Match(TokenType.LeftBracket))
                {
                var memExpr = ParseMemoryOperand();
                Consume(TokenType.RightBracket, "Expected ']'");
                return memExpr;
            }
            
            // Label reference
            if (Check(TokenType.Identifier))
            {
                return new IdentifierExpression(Advance());
            }
            
            throw Error(Current(), "Invalid operand");
        }
        
        private Expression ParseMemoryOperand()
        {
            // Enhanced memory operand parsing for x86 addressing modes
            // Supports: [base], [base+offset], [base+index*scale+offset]
            
            Expression baseReg = null;
            Expression index = null;
            Expression scale = null;
            Expression displacement = null;
            
            // Parse base register or displacement
            if (Check(TokenType.Modulo) || Check(TokenType.Identifier))
            {
                baseReg = ParseOperand();
            }
            else if (Check(TokenType.IntegerLiteral) || Check(TokenType.HexLiteral))
            {
                displacement = ParseOperand();
            }
            
            while (Match(TokenType.Plus) || Match(TokenType.Minus))
            {
                var op = Previous();
                
                // Check for index*scale pattern
                if (Check(TokenType.Modulo) || Check(TokenType.Identifier))
                {
                    var nextOperand = ParseOperand();
                    
                    if (Match(TokenType.Multiply))
                    {
                        // This is index*scale
                        index = nextOperand;
                        scale = ParseOperand();
                    }
                    else
                    {
                        // This is either another base or displacement
                        if (displacement == null && (Check(TokenType.IntegerLiteral) || Check(TokenType.HexLiteral)))
                        {
                            displacement = nextOperand;
                        }
                        else if (baseReg == null)
                        {
                            baseReg = nextOperand;
                        }
                        else
                        {
                            index = nextOperand;
                        }
                    }
                }
                else
                {
                    // Must be displacement
                    displacement = ParseOperand();
                    if (op.Type == TokenType.Minus && displacement is LiteralExpression lit)
                    {
                        // Negate the displacement
                        displacement = new LiteralExpression(
                            CreateToken(TokenType.IntegerLiteral, (-Convert.ToInt64(lit.Value)).ToString(), -Convert.ToInt64(lit.Value))
                        );
                    }
                }
            }
            
            return new MemoryOperandExpression(baseReg, index, scale, displacement);
        }
        
        private string FormatOperand(Expression operand)
        {
            return operand switch
            {
                LiteralExpression lit => lit.Value.ToString(),
                IdentifierExpression id => id.Name,
                BinaryExpression bin => $"[{FormatOperand(bin.Left)} + {FormatOperand(bin.Right)}]",
                _ => operand.ToString()
            };
        }
        
        private Statement ParseDataDeclaration()
        {
            var name = Consume(TokenType.Identifier, "Expected data name");
            
            TypeNode dataType = new TypeNode("byte");
            if (Match(TokenType.Colon))
            {
                dataType = ParseType();
            }
            
            Expression initializer = null;
            if (Match(TokenType.Assign))
            {
                initializer = ParseExpression();
            }
            
            return new FieldDeclaration(name, dataType, initializer);
        }
        
        private Statement ParseDirective()
        {
            Advance(); // consume '.'
            var directive = Consume(TokenType.Identifier, "Expected directive name");
            
            var args = new List<string>();
            while (!Check(TokenType.NewLine) && !IsAtEnd())
            {
                args.Add(Advance().Lexeme);
            }
            
            // Convert directive to comment for now
            var directiveText = $".{directive.Lexeme} {string.Join(" ", args)}";
            return new ExpressionStatement(
                new LiteralExpression(
                    CreateToken(TokenType.Comment, directiveText, null)
                )
            );
        }
        
        private Statement ParseStructDeclaration()
        {
            var name = Consume(TokenType.Identifier, "Expected struct name");
            var fields = new List<StructField>();
            
            // Check for packed struct
            bool isPacked = false;
            if (Match(TokenType.Attribute))
            {
                if (Match(TokenType.Packed))
                {
                    isPacked = true;
                }
            }
            
            Consume(TokenType.LeftBrace, "Expected '{' after struct name");
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var field = ParseStructField();
                if (field != null)
                {
                    fields.Add(field);
                }
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after struct fields");
            Match(TokenType.Semicolon); // Optional semicolon after struct
            
            return new StructDeclaration(name.Lexeme, fields, isPacked, name.Line, name.Column);
        }
        
        private StructField ParseStructField()
        {
            var type = ParseType();
            var name = Consume(TokenType.Identifier, "Expected field name");
            
            // Check for bit field
            int? bitWidth = null;
            if (Match(TokenType.Colon))
            {
                var widthToken = Consume(TokenType.IntegerLiteral, "Expected bit width");
                bitWidth = int.Parse(widthToken.Lexeme);
            }
            
            // Check for array
            Expression arraySize = null;
            if (Match(TokenType.LeftBracket))
            {
                if (!Check(TokenType.RightBracket))
                {
                    arraySize = ParseExpression();
                }
                Consume(TokenType.RightBracket, "Expected ']'");
            }
            
            Consume(TokenType.Semicolon, "Expected ';' after field");
            
            return new StructField(type, name.Lexeme, bitWidth, arraySize);
        }
        
        private Statement ParseUnionDeclaration()
        {
            var name = Consume(TokenType.Identifier, "Expected union name");
            var members = new List<StructField>();
            
            Consume(TokenType.LeftBrace, "Expected '{' after union name");
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var member = ParseStructField();
                if (member != null)
                {
                    members.Add(member);
                }
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after union members");
            Match(TokenType.Semicolon); // Optional semicolon
            
            return new UnionDeclaration(name.Lexeme, members, name.Line, name.Column);
        }
        
        private Statement ParseTypedefStatement()
        {
            var type = ParseType();
            var alias = Consume(TokenType.Identifier, "Expected typedef alias");
            Consume(TokenType.Semicolon, "Expected ';' after typedef");
            
            return new TypedefStatement(alias.Lexeme, type, alias.Line, alias.Column);
        }
        
        private Statement ParseVariableDeclaration()
        {
            // Parse storage specifiers
            var modifiers = new List<string>();
            while (IsStorageSpecifier())
            {
                modifiers.Add(Advance().Lexeme);
            }
            
            var type = ParseType();
            var declarations = new List<Statement>();
            
            do
            {
                // Handle pointer declarator
                int pointerLevel = 0;
                while (Match(TokenType.Multiply))
                {
                    pointerLevel++;
                }
                
                var name = Consume(TokenType.Identifier, "Expected variable name");
                
                // Handle array declarator
                Expression arraySize = null;
                if (Match(TokenType.LeftBracket))
                {
                    if (!Check(TokenType.RightBracket))
                    {
                        arraySize = ParseExpression();
                    }
                    Consume(TokenType.RightBracket, "Expected ']'");
                }
                
                // Handle initializer
                Expression initializer = null;
                if (Match(TokenType.Assign))
                {
                    initializer = ParseExpression();
                }
                
                // Create appropriate type node
                var varType = type;
                if (pointerLevel > 0)
                {
                    varType = new PointerType(type, pointerLevel);
                }
                
                var decl = new VariableDeclaration(
                    varType,
                    name,
                    initializer,
                    modifiers.Contains("const"),
                    modifiers.Contains("readonly")
                );
                
                declarations.Add(decl);
                
            } while (Match(TokenType.Comma));
            
            Consume(TokenType.Semicolon, "Expected ';' after variable declaration");
            
            // If multiple declarations, wrap in block
            if (declarations.Count == 1)
            {
                return declarations[0];
            }
            else
            {
                return new BlockStatement(declarations);
            }
        }
        
        private Statement ParseUnsafeBlock()
        {
            Consume(TokenType.LeftBrace, "Expected '{' after unsafe");
            var statements = new List<Statement>();
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var stmt = ParseStatement();
                if (stmt != null)
                {
                    statements.Add(stmt);
                }
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after unsafe block");
            
            return new UnsafeBlock(statements, 0, 0);
        }
        
        private Statement ParseFixedStatement()
        {
            Consume(TokenType.LeftParen, "Expected '(' after fixed");
            
            var type = ParseType();
            var name = Consume(TokenType.Identifier, "Expected variable name");
            Consume(TokenType.Assign, "Expected '=' in fixed statement");
            var target = ParseExpression();
            
            Consume(TokenType.RightParen, "Expected ')' after fixed declaration");
            
            var body = ParseStatement();
            
            return new FixedStatement(type, name.Lexeme, target, body, 0, 0);
        }
        
        private TypeNode ParseType()
        {
            if (Match(TokenType.Byte)) return new TypeNode("byte");
            if (Match(TokenType.Short)) return new TypeNode("short");
            if (Match(TokenType.Int)) return new TypeNode("int");
            if (Match(TokenType.Long)) return new TypeNode("long");
            if (Match(TokenType.Float)) return new TypeNode("float");
            if (Match(TokenType.Double)) return new TypeNode("double");
            if (Match(TokenType.Void)) return new TypeNode("void");
            
            if (Check(TokenType.Identifier))
            {
                var typeName = Advance();
                return new TypeNode(typeName.Lexeme);
            }
            
            throw Error(Current(), "Expected type");
        }
        
        private Expression ParseExpression()
        {
            return ParseBinaryExpression();
        }
        
        private Expression ParseBinaryExpression()
        {
            var expr = ParseUnaryExpression();
            
            while (IsOperator())
            {
                var op = Advance();
                var right = ParseUnaryExpression();
                expr = new BinaryExpression(expr, op, right);
            }
            
            return expr;
        }
        
        private Expression ParseUnaryExpression()
        {
            // Handle unary operators
            if (Match(TokenType.Multiply))
            {
                // Dereference operator *
                var expr = ParseUnaryExpression();
                return new DereferenceExpression(expr, Previous().Line, Previous().Column);
            }
            
            if (Match(TokenType.BitwiseAnd))
            {
                // Address-of operator &
                var expr = ParseUnaryExpression();
                return new AddressOfExpression(expr, Previous().Line, Previous().Column);
            }
            
            if (Match(TokenType.Plus, TokenType.Minus, TokenType.BitwiseNot, TokenType.LogicalNot))
            {
                var op = Previous();
                var expr = ParseUnaryExpression();
                return new UnaryExpression(op, expr);
            }
            
            if (Match(TokenType.Increment, TokenType.Decrement))
            {
                // Prefix increment/decrement
                var op = Previous();
                var expr = ParseUnaryExpression();
                return new UnaryExpression(op, expr);
            }
            
            return ParsePostfixExpression();
        }
        
        private Expression ParsePostfixExpression()
        {
            var expr = ParsePrimaryExpression();
            
            while (true)
            {
                if (Match(TokenType.LeftBracket))
                {
                    // Array indexing
                    var index = ParseExpression();
                    Consume(TokenType.RightBracket, "Expected ']' after array index");
                    expr = new ArrayIndexExpression(expr, index, Previous().Line, Previous().Column);
                }
                else if (Match(TokenType.Dot))
                {
                    // Member access
                    var member = Consume(TokenType.Identifier, "Expected member name");
                    expr = new MemberAccessExpression(expr, member.Lexeme, member.Line, member.Column);
                }
                else if (Match(TokenType.Arrow))
                {
                    // Pointer member access (->)
                    var member = Consume(TokenType.Identifier, "Expected member name");
                    // Convert p->member to (*p).member
                    var deref = new DereferenceExpression(expr, expr.Line, expr.Column);
                    expr = new MemberAccessExpression(deref, member.Lexeme, member.Line, member.Column);
                }
                else if (Match(TokenType.Increment, TokenType.Decrement))
                {
                    // Postfix increment/decrement
                    var op = Previous();
                    expr = new PostfixExpression(expr, op);
                }
                else if (Match(TokenType.LeftParen))
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
                else
                {
                    break;
                }
            }
            
            return expr;
        }
        
        private Expression ParsePrimaryExpression()
        {
            // Literals
            if (Check(TokenType.IntegerLiteral) || Check(TokenType.HexLiteral) || 
                Check(TokenType.OctalLiteral) || Check(TokenType.BinaryLiteral))
            {
                return new LiteralExpression(Advance());
            }
            
            if (Check(TokenType.FloatLiteral) || Check(TokenType.DoubleLiteral))
            {
                return new LiteralExpression(Advance());
            }
            
            if (Check(TokenType.StringLiteral) || Check(TokenType.CharLiteral))
            {
                return new LiteralExpression(Advance());
            }
            
            if (Match(TokenType.True, TokenType.False))
            {
                return new LiteralExpression(Previous());
            }
            
            if (Match(TokenType.Null))
            {
                return new LiteralExpression(Previous());
            }
            
            // Identifiers
            if (Check(TokenType.Identifier))
            {
                return new IdentifierExpression(Advance());
            }
            
            // Sizeof
            if (Match(TokenType.Sizeof))
            {
                Consume(TokenType.LeftParen, "Expected '(' after sizeof");
                var type = ParseType();
                Consume(TokenType.RightParen, "Expected ')' after type");
                return new SizeofExpression(type);
            }
            
            // Type cast or parenthesized expression
            if (Match(TokenType.LeftParen))
            {
                // Try to parse as cast
                var checkpoint = current;
                try
                {
                    var type = ParseType();
                    if (Match(TokenType.RightParen))
                    {
                        // It's a cast
                        var expr = ParseUnaryExpression();
                        return new CastExpression(type, expr);
                    }
                }
                catch { }
                
                // Not a cast, reset and parse as parenthesized expression
                current = checkpoint;
                var innerExpr = ParseExpression();
                Consume(TokenType.RightParen, "Expected ')' after expression");
                return innerExpr;
            }
            
            // Assembly register reference
            if (Match(TokenType.Modulo))
            {
                var reg = Consume(TokenType.Identifier, "Expected register name after %");
                return new RegisterExpression(reg.Lexeme);
            }
            
            throw Error(Current(), "Expected expression");
        }
        
        private bool IsOperator()
        {
            return Check(TokenType.Plus) || Check(TokenType.Minus) ||
                   Check(TokenType.Multiply) || Check(TokenType.Divide) || Check(TokenType.Modulo) ||
                   Check(TokenType.LeftShift) || Check(TokenType.RightShift) ||
                   Check(TokenType.Less) || Check(TokenType.LessEqual) ||
                   Check(TokenType.Greater) || Check(TokenType.GreaterEqual) ||
                   Check(TokenType.Equal) || Check(TokenType.NotEqual) ||
                   Check(TokenType.BitwiseAnd) || Check(TokenType.BitwiseOr) || Check(TokenType.BitwiseXor) ||
                   Check(TokenType.LogicalAnd) || Check(TokenType.LogicalOr) ||
                   Check(TokenType.Assign) || Check(TokenType.PlusAssign) || Check(TokenType.MinusAssign) ||
                   Check(TokenType.MultiplyAssign) || Check(TokenType.DivideAssign) || Check(TokenType.ModuloAssign) ||
                   Check(TokenType.BitwiseAndAssign) || Check(TokenType.BitwiseOrAssign) || Check(TokenType.BitwiseXorAssign) ||
                   Check(TokenType.LeftShiftAssign) || Check(TokenType.RightShiftAssign);
        }
        
        private void SkipWhitespaceAndComments()
        {
            while (Match(TokenType.Whitespace) || Match(TokenType.NewLine) || 
                   Match(TokenType.Comment) || Match(TokenType.MultiLineComment))
            {
                // Skip
            }
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
                CreateToken(TokenType.EndOfFile, "", null);
        }
        
        private Token Previous()
        {
            return tokens[current - 1];
        }
        
        private Token? Peek(int offset)
        {
            var index = current + offset;
            return index < tokens.Count ? tokens[index] : null;
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
        
        private Token CreateToken(TokenType type, string lexeme, object literal)
        {
            return new Token(type, lexeme, literal, 0, 0, 0, 0, "", SyntaxLevel.Low);
        }
        
        private class AsmInstruction
        {
            public string Mnemonic { get; set; }
            public List<string> Operands { get; set; }
            public int Address { get; set; }
        }
        
        private bool IsStorageSpecifier()
        {
            return Check(TokenType.Static) || Check(TokenType.Extern) || 
                   Check(TokenType.Const) || Check(TokenType.Volatile);
        }
        
        private bool IsTypeKeyword()
        {
            return Check(TokenType.Byte) || Check(TokenType.Short) || 
                   Check(TokenType.Int) || Check(TokenType.Long) ||
                   Check(TokenType.Float) || Check(TokenType.Double) ||
                   Check(TokenType.Void) || Check(TokenType.Char) ||
                   Check(TokenType.Unsigned) || Check(TokenType.Signed);
        }
    }
    
    #region AST Node Extensions for Low-Level
    
    public class InlineAssemblyStatement : Statement
    {
        public List<string> Instructions { get; }
        
        public InlineAssemblyStatement(List<string> instructions, int line, int column)
            : base(line, column)
        {
            Instructions = instructions;
        }
        
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitInlineAssemblyStatement(this);
        }
    }
    
    public class AllocateStatement : Statement
    {
        public TypeNode Type { get; }
        public Expression Size { get; }
        
        public AllocateStatement(TypeNode type, Expression size, int line, int column)
            : base(line, column)
        {
            Type = type;
            Size = size;
        }
        
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitAllocateStatement(this);
        }
    }
    
    public class FreeStatement : Statement
    {
        public Expression Pointer { get; }
        
        public FreeStatement(Expression pointer, int line, int column)
            : base(line, column)
        {
            Pointer = pointer;
        }
        
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitFreeStatement(this);
        }
    }
    
    public class StackAllocStatement : Statement
    {
        public TypeNode Type { get; }
        public string Name { get; }
        public Expression Size { get; }
        
        public StackAllocStatement(TypeNode type, string name, Expression size, int line, int column)
            : base(line, column)
        {
            Type = type;
            Name = name;
            Size = size;
        }
        
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitStackAllocStatement(this);
        }
    }
    
    public class AtomicBlock : Statement
    {
        public List<Statement> Statements { get; }
        
        public AtomicBlock(List<Statement> statements, int line, int column)
            : base(line, column)
        {
            Statements = statements;
        }
        
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitAtomicBlock(this);
        }
    }
    
    public class PointerDeclaration : Statement
    {
        public PointerType Type { get; }
        public string Name { get; }
        public Expression Initializer { get; }
        
        public PointerDeclaration(PointerType type, string name, Expression initializer, int line, int column)
            : base(line, column)
        {
            Type = type;
            Name = name;
            Initializer = initializer;
        }
        
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitPointerDeclaration(this);
        }
    }
    
    public class UnsafeBlock : Statement
    {
        public List<Statement> Statements { get; }
        
        public UnsafeBlock(List<Statement> statements, int line, int column)
            : base(line, column)
        {
            Statements = statements;
        }
        
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitUnsafeBlock(this);
        }
    }
    
    public class FixedStatement : Statement
    {
        public TypeNode Type { get; }
        public string Name { get; }
        public Expression Target { get; }
        public Statement Body { get; }
        
        public FixedStatement(TypeNode type, string name, Expression target, Statement body, int line, int column)
            : base(line, column)
        {
            Type = type;
            Name = name;
            Target = target;
            Body = body;
        }
        
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitFixedStatement(this);
        }
    }
    
    public class StructDeclaration : Statement
    {
        public string Name { get; }
        public List<StructField> Fields { get; }
        public bool IsPacked { get; }
        
        public StructDeclaration(string name, List<StructField> fields, bool isPacked, int line, int column)
            : base(line, column)
        {
            Name = name;
            Fields = fields;
            IsPacked = isPacked;
        }
        
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitStructDeclaration(this);
        }
    }
    
    public class UnionDeclaration : Statement
    {
        public string Name { get; }
        public List<StructField> Members { get; }
        
        public UnionDeclaration(string name, List<StructField> members, int line, int column)
            : base(line, column)
        {
            Name = name;
            Members = members;
        }
        
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitUnionDeclaration(this);
        }
    }
    
    public class TypedefStatement : Statement
    {
        public string Alias { get; }
        public TypeNode Type { get; }
        
        public TypedefStatement(string alias, TypeNode type, int line, int column)
            : base(line, column)
        {
            Alias = alias;
            Type = type;
        }
        
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitTypedefStatement(this);
        }
    }
    
    public class DereferenceExpression : Expression
    {
        public Expression Operand { get; }
        
        public DereferenceExpression(Expression operand, int line, int column)
            : base(line, column)
        {
            Operand = operand;
        }
        
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitDereferenceExpression(this);
        }
    }
    
    public class AddressOfExpression : Expression
    {
        public Expression Operand { get; }
        
        public AddressOfExpression(Expression operand, int line, int column)
            : base(line, column)
        {
            Operand = operand;
        }
        
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitAddressOfExpression(this);
        }
    }
    
    public class ArrayIndexExpression : Expression
    {
        public Expression Array { get; }
        public Expression Index { get; }
        
        public ArrayIndexExpression(Expression array, Expression index, int line, int column)
            : base(line, column)
        {
            Array = array;
            Index = index;
        }
        
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitArrayIndexExpression(this);
        }
    }
    
    public class MemberAccessExpression : Expression
    {
        public Expression Object { get; }
        public string MemberName { get; }
        
        public MemberAccessExpression(Expression obj, string memberName, int line, int column)
            : base(line, column)
        {
            Object = obj;
            MemberName = memberName;
        }
        
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitMemberAccessExpression(this);
        }
    }
    
    public class RegisterExpression : Expression
    {
        public string RegisterName { get; }
        
        public RegisterExpression(string registerName)
            : base(0, 0)
        {
            RegisterName = registerName;
        }
        
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitRegisterExpression(this);
        }
    }
    
    public class PostfixExpression : Expression
    {
        public Expression Operand { get; }
        public Token Operator { get; }
        
        public PostfixExpression(Expression operand, Token op)
            : base(op.Line, op.Column)
        {
            Operand = operand;
            Operator = op;
        }
        
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitPostfixExpression(this);
        }
    }
    
    public class SizeofExpression : Expression
    {
        public TypeNode Type { get; }
        
        public SizeofExpression(TypeNode type)
            : base(0, 0)
        {
            Type = type;
        }
        
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitSizeofExpression(this);
        }
    }
    
    public class CastExpression : Expression
    {
        public TypeNode TargetType { get; }
        public Expression Expression { get; }
        
        public CastExpression(TypeNode targetType, Expression expression)
            : base(expression.Line, expression.Column)
        {
            TargetType = targetType;
            Expression = expression;
        }
        
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitCastExpression(this);
        }
    }
    
    #endregion
    
    #region Type Nodes
    
    public class TypeNode
    {
        public string Name { get; }
        
        public TypeNode(string name)
        {
            Name = name;
        }
    }
    
    public class PointerType : TypeNode
    {
        public TypeNode BaseType { get; }
        public int PointerLevel { get; }
        
        public PointerType(TypeNode baseType, int pointerLevel)
            : base(baseType.Name + new string('*', pointerLevel))
        {
            BaseType = baseType;
            PointerLevel = pointerLevel;
        }
    }
    
    public class GenericTypeNode : TypeNode
    {
        public List<TypeNode> TypeArguments { get; }
        
        public GenericTypeNode(string name, List<TypeNode> typeArguments)
            : base(name)
        {
            TypeArguments = typeArguments;
        }
    }
    
    public class FunctionPointerType : TypeNode
    {
        public TypeNode ReturnType { get; }
        public List<Parameter> Parameters { get; }
        
        public FunctionPointerType(TypeNode returnType, List<Parameter> parameters)
            : base("function_pointer")
        {
            ReturnType = returnType;
            Parameters = parameters;
        }
    }
    
    #endregion
    
    #region Support Classes
    
    public class StructField
    {
        public TypeNode Type { get; }
        public string Name { get; }
        public int? BitWidth { get; }
        public Expression ArraySize { get; }
        
        public StructField(TypeNode type, string name, int? bitWidth, Expression arraySize)
        {
            Type = type;
            Name = name;
            BitWidth = bitWidth;
            ArraySize = arraySize;
        }
    }
    
    public class Parameter
    {
        public TypeNode Type { get; }
        public string Name { get; }
        
        public Parameter(TypeNode type, string name)
        {
            Type = type;
            Name = name;
        }
    }
    
    /// <summary>
    /// Extended assembly statement with constraints
    /// </summary>
    public class ExtendedAssemblyStatement : Statement
    {
        public string AssemblyCode { get; }
        public string Target { get; }
        public string Dialect { get; }
        public bool IsVolatile { get; }
        public List<AsmOperand> Outputs { get; }
        public List<AsmOperand> Inputs { get; }
        public List<string> Clobbers { get; }
        
        public ExtendedAssemblyStatement(Token asmToken, string assemblyCode, string target, string dialect,
            bool isVolatile, List<AsmOperand> outputs, List<AsmOperand> inputs, List<string> clobbers)
            : base(asmToken)
        {
            AssemblyCode = assemblyCode;
            Target = target;
            Dialect = dialect;
            IsVolatile = isVolatile;
            Outputs = outputs;
            Inputs = inputs;
            Clobbers = clobbers;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            // Delegate to regular assembly statement for now
            return visitor.VisitAssemblyStatement(new AssemblyStatement(Token, AssemblyCode));
        }
    }
    
    /// <summary>
    /// Assembly operand with constraint
    /// </summary>
    public class AsmOperand
    {
        public string Constraint { get; }
        public Expression Expression { get; }
        
        public AsmOperand(string constraint, Expression expression)
        {
            Constraint = constraint;
            Expression = expression;
        }
    }
    
    /// <summary>
    /// Assembly instruction statement with metadata
    /// </summary>
    public class AssemblyInstructionStatement : Statement
    {
        public string Mnemonic { get; }
        public string AssemblyCode { get; }
        public List<Expression> Operands { get; }
        public string SizeSuffix { get; }
        
        public AssemblyInstructionStatement(Token mnemonic, string assemblyCode, 
            List<Expression> operands, string sizeSuffix)
            : base(mnemonic)
        {
            Mnemonic = mnemonic.Lexeme;
            AssemblyCode = assemblyCode;
            Operands = operands;
            SizeSuffix = sizeSuffix;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitAssemblyStatement(new AssemblyStatement(Token, AssemblyCode));
        }
    }
    
    /// <summary>
    /// Memory operand expression for complex addressing modes
    /// </summary>
    public class MemoryOperandExpression : Expression
    {
        public Expression BaseRegister { get; }
        public Expression Index { get; }
        public Expression Scale { get; }
        public Expression Displacement { get; }
        
        public MemoryOperandExpression(Expression baseReg, Expression index, 
            Expression scale, Expression displacement)
            : base(baseReg?.Token ?? index?.Token ?? displacement?.Token)
        {
            BaseRegister = baseReg;
            Index = index;
            Scale = scale;
            Displacement = displacement;
        }
        
        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            // Format as string for now
            var parts = new List<string>();
            
            if (BaseRegister != null)
                parts.Add(FormatOperand(BaseRegister));
                
            if (Index != null)
            {
                var indexStr = FormatOperand(Index);
                if (Scale != null)
                    indexStr += "*" + FormatOperand(Scale);
                parts.Add(indexStr);
            }
            
            if (Displacement != null)
                parts.Add(FormatOperand(Displacement));
                
            var formatted = "[" + string.Join("+", parts) + "]";
            return visitor.VisitLiteralExpression(
                new LiteralExpression(CreateToken(TokenType.StringLiteral, formatted, formatted))
            );
        }
        
        private string FormatOperand(Expression operand)
        {
            return operand switch
            {
                LiteralExpression lit => lit.Value.ToString(),
                IdentifierExpression id => id.Name,
                _ => operand.ToString()
            };
        }
        
        private Token CreateToken(TokenType type, string lexeme, object value)
        {
            return new Token(type, lexeme, value, 0, 0, 0, 0, "", SyntaxLevel.Low);
        }
    }
    
    #endregion
} 
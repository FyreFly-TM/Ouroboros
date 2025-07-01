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
            
            // Optional target specifier
            string target = "x86_64";
            if (Check(TokenType.Identifier))
            {
                target = Advance().Lexeme;
            }
            
            Consume(TokenType.LeftBrace, "Expected '{' after @asm");
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var line = ParseAsmLine();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    asmStatements.Add(line);
                }
            }
            
            Consume(TokenType.RightBrace, "Expected '}' to close assembly block");
            
            var asmCode = string.Join("\n", asmStatements);
            return new AssemblyStatement(
                CreateToken(TokenType.Assembly, "@asm", null),
                asmCode
            );
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
            
            // Create assembly instruction
            var asmText = mnemonic.Lexeme;
            if (operands.Count > 0)
            {
                asmText += " " + string.Join(", ", operands.Select(FormatOperand));
            }
            
            return new AssemblyStatement(mnemonic, asmText);
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
            // Simplified memory operand parsing
            // Real implementation would handle full x86 addressing modes
            var baseReg = ParseOperand();
            
            if (Match(TokenType.Plus))
            {
                var offset = ParseOperand();
                return new BinaryExpression(
                    baseReg,
                    CreateToken(TokenType.Plus, "+", null),
                    offset
                );
            }
            
            return baseReg;
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
            // Simple expression parsing for low-level syntax
            if (Check(TokenType.IntegerLiteral) || Check(TokenType.HexLiteral) ||
                Check(TokenType.StringLiteral))
            {
                return new LiteralExpression(Advance());
            }
            
            if (Check(TokenType.Identifier))
            {
                return new IdentifierExpression(Advance());
            }
            
            throw Error(Current(), "Expected expression");
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
    
    #endregion
} 
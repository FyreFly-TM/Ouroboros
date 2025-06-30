using System;
using System.Collections.Generic;
using System.Linq;
using Ouroboros.Core;
using Ouroboros.Core.AST;
using Ouroboros.Core.Lexer;

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
        
        public LowLevelParser(List<Token> tokens)
        {
            this.tokens = tokens;
            current = 0;
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
                try
                {
                    var stmt = ParseStatement();
                    if (stmt != null)
                        statements.Add(stmt);
                }
                catch (ParseException ex)
                {
                    // Report error and synchronize
                    ReportError(ex);
                    Synchronize();
                }
            }
            
            return statements;
        }
        
        public Statement ParseStatement()
        {
            // Check for inline assembly
            if (Match(TokenType.At) && Check(TokenType.Asm))
            {
                Advance(); // consume 'asm'
                return ParseInlineAssembly();
            }
            
            // Memory management
            if (Match(TokenType.Allocate))
                return ParseAllocate();
            
            if (Match(TokenType.Free))
                return ParseFree();
            
            if (Match(TokenType.Stackalloc))
                return ParseStackAlloc();
            
            // Atomic operations
            if (Match(TokenType.Atomic))
                return ParseAtomic();
            
            // Pointer operations
            if (CheckPointerDeclaration())
                return ParsePointerDeclaration();
            
            // Unsafe block
            if (Match(TokenType.Unsafe))
                return ParseUnsafeBlock();
            
            // Fixed statement
            if (Match(TokenType.Fixed))
                return ParseFixedStatement();
            
            // Regular statements
            if (Match(TokenType.Struct))
                return ParseStructDeclaration();
            
            if (Match(TokenType.UnionKeyword))
                return ParseUnionDeclaration();
            
            if (Match(TokenType.Typedef))
                return ParseTypedef();
            
            // Fall back to standard statements
            return ParseStandardStatement();
        }
        
        private Statement ParseInlineAssembly()
        {
            Consume(TokenType.LeftBrace, "Expected '{' after '@asm'");
            
            var instructions = new List<string>();
            var startLine = Previous().Line;
            var startColumn = Previous().Column;
            
            // Collect assembly instructions until '}'
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var line = new List<Token>();
                
                // Collect tokens until newline or semicolon
                while (!Check(TokenType.Newline) && !Check(TokenType.Semicolon) && 
                       !Check(TokenType.RightBrace) && !IsAtEnd())
                {
                    line.Add(Advance());
                }
                
                if (line.Count > 0)
                {
                    var instruction = string.Join(" ", line.Select(t => t.Lexeme));
                    instructions.Add(instruction);
                }
                
                // Skip newline or semicolon
                if (Match(TokenType.Newline, TokenType.Semicolon))
                {
                    // Continue
                }
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after assembly instructions");
            
            return new InlineAssemblyStatement(instructions, startLine, startColumn);
        }
        
        private Statement ParseAllocate()
        {
            var startLine = Previous().Line;
            var startColumn = Previous().Column;
            
            Consume(TokenType.Less, "Expected '<' after 'allocate'");
            var type = ParseType();
            Consume(TokenType.Greater, "Expected '>' after type");
            
            Consume(TokenType.LeftParen, "Expected '(' after type");
            var size = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after size");
            
            return new AllocateStatement(type, size, startLine, startColumn);
        }
        
        private Statement ParseFree()
        {
            var startLine = Previous().Line;
            var startColumn = Previous().Column;
            
            Consume(TokenType.LeftParen, "Expected '(' after 'free'");
            var pointer = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after pointer");
            
            ConsumeSemicolon();
            
            return new FreeStatement(pointer, startLine, startColumn);
        }
        
        private Statement ParseStackAlloc()
        {
            var startLine = Previous().Line;
            var startColumn = Previous().Column;
            
            var type = ParseType();
            
            Consume(TokenType.LeftBracket, "Expected '[' after type");
            var size = ParseExpression();
            Consume(TokenType.RightBracket, "Expected ']' after size");
            
            var name = Consume(TokenType.Identifier, "Expected variable name").Lexeme;
            
            ConsumeSemicolon();
            
            return new StackAllocStatement(type, name, size, startLine, startColumn);
        }
        
        private Statement ParseAtomic()
        {
            var startLine = Previous().Line;
            var startColumn = Previous().Column;
            
            Consume(TokenType.LeftBrace, "Expected '{' after 'atomic'");
            
            var statements = new List<Statement>();
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                statements.Add(ParseStatement());
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after atomic block");
            
            return new AtomicBlock(statements, startLine, startColumn);
        }
        
        private bool CheckPointerDeclaration()
        {
            // Look for patterns like: int* ptr or char** argv
            int savepoint = current;
            
            // Try to parse a type
            if (CheckType())
            {
                // Skip the type
                while (current < tokens.Count && 
                       (Check(TokenType.Identifier) || Check(TokenType.Dot)))
                {
                    Advance();
                }
                
                // Check for pointer markers
                bool hasPointer = false;
                while (Match(TokenType.Star))
                {
                    hasPointer = true;
                }
                
                // Restore position
                current = savepoint;
                return hasPointer;
            }
            
            current = savepoint;
            return false;
        }
        
        private Statement ParsePointerDeclaration()
        {
            var startLine = Peek().Line;
            var startColumn = Peek().Column;
            
            var baseType = ParseType();
            
            // Count pointer levels
            int pointerLevel = 0;
            while (Match(TokenType.Star))
            {
                pointerLevel++;
            }
            
            var type = new PointerType(baseType, pointerLevel);
            
            var name = Consume(TokenType.Identifier, "Expected variable name").Lexeme;
            
            Expression initializer = null;
            if (Match(TokenType.Equal))
            {
                initializer = ParseExpression();
            }
            
            ConsumeSemicolon();
            
            return new PointerDeclaration(type, name, initializer, startLine, startColumn);
        }
        
        private Statement ParseUnsafeBlock()
        {
            var startLine = Previous().Line;
            var startColumn = Previous().Column;
            
            Consume(TokenType.LeftBrace, "Expected '{' after 'unsafe'");
            
            var statements = new List<Statement>();
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                statements.Add(ParseStatement());
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after unsafe block");
            
            return new UnsafeBlock(statements, startLine, startColumn);
        }
        
        private Statement ParseFixedStatement()
        {
            var startLine = Previous().Line;
            var startColumn = Previous().Column;
            
            Consume(TokenType.LeftParen, "Expected '(' after 'fixed'");
            
            // Parse pinned variable declaration
            var type = ParseType();
            while (Match(TokenType.Star))
            {
                type = new PointerType(type, 1);
            }
            
            var name = Consume(TokenType.Identifier, "Expected variable name").Lexeme;
            
            Consume(TokenType.Equal, "Expected '=' in fixed statement");
            
            var target = ParseExpression();
            
            Consume(TokenType.RightParen, "Expected ')' after fixed declaration");
            
            var body = ParseStatement();
            
            return new FixedStatement(type, name, target, body, startLine, startColumn);
        }
        
        private Statement ParseStructDeclaration()
        {
            var startLine = Previous().Line;
            var startColumn = Previous().Column;
            
            var name = Consume(TokenType.Identifier, "Expected struct name").Lexeme;
            
            // Optional: packed attribute
            bool isPacked = false;
            if (Match(TokenType.Packed))
            {
                isPacked = true;
            }
            
            Consume(TokenType.LeftBrace, "Expected '{' before struct body");
            
            var fields = new List<StructField>();
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var field = ParseStructField();
                fields.Add(field);
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after struct body");
            
            return new StructDeclaration(name, fields, isPacked, startLine, startColumn);
        }
        
        private StructField ParseStructField()
        {
            // Check if we're using compact syntax (name: type;) or traditional syntax (type name;)
            // Look ahead to see if there's a colon after the first identifier
            bool isCompactSyntax = false;
            
            if (Check(TokenType.Identifier))
            {
                // Look ahead one token to see if there's a colon
                if (current + 1 < tokens.Count && tokens[current + 1].Type == TokenType.Colon)
                {
                    isCompactSyntax = true;
                }
            }
            
            if (isCompactSyntax)
            {
                // Parse compact syntax: name: type;
                var name = Consume(TokenType.Identifier, "Expected field name").Lexeme;
                Consume(TokenType.Colon, "Expected ':' after field name");
                var type = ParseType();
                
                // Handle array fields in compact syntax
                Expression arraySize = null;
                if (Match(TokenType.LeftBracket))
                {
                    arraySize = ParseExpression();
                    Consume(TokenType.RightBracket, "Expected ']' after array size");
                }
                
                ConsumeSemicolon();
                
                return new StructField(type, name, null, arraySize);
            }
            else
            {
                // Parse traditional syntax: type name;
                var type = ParseType();
                
                // Handle pointer fields
                int pointerLevel = 0;
                while (Match(TokenType.Star))
                {
                    pointerLevel++;
                }
                
                if (pointerLevel > 0)
                {
                    type = new PointerType(type, pointerLevel);
                }
                
                var name = Consume(TokenType.Identifier, "Expected field name").Lexeme;
                
                // Handle bit fields
                int? bitWidth = null;
                if (Match(TokenType.Colon))
                {
                    var widthToken = Consume(TokenType.Number, "Expected bit width");
                    bitWidth = int.Parse(widthToken.Lexeme);
                }
                
                // Handle array fields
                Expression arraySize = null;
                if (Match(TokenType.LeftBracket))
                {
                    arraySize = ParseExpression();
                    Consume(TokenType.RightBracket, "Expected ']' after array size");
                }
                
                ConsumeSemicolon();
                
                return new StructField(type, name, bitWidth, arraySize);
            }
        }
        
        private Statement ParseUnionDeclaration()
        {
            var startLine = Previous().Line;
            var startColumn = Previous().Column;
            
            var name = Consume(TokenType.Identifier, "Expected union name").Lexeme;
            
            Consume(TokenType.LeftBrace, "Expected '{' before union body");
            
            var members = new List<StructField>();
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var member = ParseStructField();
                members.Add(member);
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after union body");
            
            return new UnionDeclaration(name, members, startLine, startColumn);
        }
        
        private Statement ParseTypedef()
        {
            var startLine = Previous().Line;
            var startColumn = Previous().Column;
            
            var type = ParseType();
            
            // Handle function pointer typedefs
            if (Match(TokenType.LeftParen))
            {
                Consume(TokenType.Star, "Expected '*' in function pointer typedef");
                var name = Consume(TokenType.Identifier, "Expected typedef name").Lexeme;
                Consume(TokenType.RightParen, "Expected ')' after typedef name");
                
                // Parse function parameters
                Consume(TokenType.LeftParen, "Expected '(' for function parameters");
                var parameters = ParseParameterList();
                Consume(TokenType.RightParen, "Expected ')' after parameters");
                
                ConsumeSemicolon();
                
                return new TypedefStatement(
                    name, 
                    new FunctionPointerType(type, parameters), 
                    startLine, 
                    startColumn
                );
            }
            
            // Regular typedef
            var alias = Consume(TokenType.Identifier, "Expected typedef name").Lexeme;
            ConsumeSemicolon();
            
            return new TypedefStatement(alias, type, startLine, startColumn);
        }
        
        private Statement ParseStandardStatement()
        {
            // Handle pointer dereference in expressions
            if (Match(TokenType.Star))
            {
                var expr = ParseDereferenceExpression();
                
                // Could be assignment
                if (Match(TokenType.Equal))
                {
                    var value = ParseExpression();
                    ConsumeSemicolon();
                    return new ExpressionStatement(
                        new AssignmentExpression(expr, value, expr.Line, expr.Column),
                        expr.Line,
                        expr.Column
                    );
                }
                
                // Just an expression
                ConsumeSemicolon();
                return new ExpressionStatement(expr, expr.Line, expr.Column);
            }
            
            // Handle address-of operator
            if (Match(TokenType.Ampersand))
            {
                var expr = ParseAddressOfExpression();
                ConsumeSemicolon();
                return new ExpressionStatement(expr, expr.Line, expr.Column);
            }
            
            // Fall back to regular statement parsing
            // This would delegate to the main parser
            return ParseRegularStatement();
        }
        
        private Expression ParseDereferenceExpression()
        {
            var startLine = Previous().Line;
            var startColumn = Previous().Column;
            
            var operand = ParseUnaryExpression();
            
            return new DereferenceExpression(operand, startLine, startColumn);
        }
        
        private Expression ParseAddressOfExpression()
        {
            var startLine = Previous().Line;
            var startColumn = Previous().Column;
            
            var operand = ParseUnaryExpression();
            
            return new AddressOfExpression(operand, startLine, startColumn);
        }
        
        #region Helper Methods
        
        private bool CheckType()
        {
            return Check(TokenType.Int) || Check(TokenType.Float) || 
                   Check(TokenType.Double) || Check(TokenType.Char) ||
                   Check(TokenType.Bool) || Check(TokenType.Void) ||
                   Check(TokenType.String) || Check(TokenType.Identifier);
        }
        
        private TypeNode ParseType()
        {
            if (Match(TokenType.Int)) return new TypeNode("int");
            if (Match(TokenType.Float)) return new TypeNode("float");
            if (Match(TokenType.Double)) return new TypeNode("double");
            if (Match(TokenType.Char)) return new TypeNode("char");
            if (Match(TokenType.Bool)) return new TypeNode("bool");
            if (Match(TokenType.Void)) return new TypeNode("void");
            if (Match(TokenType.String)) return new TypeNode("string");
            
            if (Check(TokenType.Identifier))
            {
                var name = Advance().Lexeme;
                
                // Handle generic types
                if (Match(TokenType.Less))
                {
                    var args = new List<TypeNode>();
                    
                    do
                    {
                        args.Add(ParseType());
                    } while (Match(TokenType.Comma));
                    
                    Consume(TokenType.Greater, "Expected '>' after generic arguments");
                    
                    return new GenericTypeNode(name, args);
                }
                
                return new TypeNode(name);
            }
            
            throw Error("Expected type");
        }
        
        private List<Parameter> ParseParameterList()
        {
            var parameters = new List<Parameter>();
            
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    var type = ParseType();
                    string name = null;
                    
                    if (Check(TokenType.Identifier))
                    {
                        name = Advance().Lexeme;
                    }
                    
                    parameters.Add(new Parameter(type, name));
                    
                } while (Match(TokenType.Comma));
            }
            
            return parameters;
        }
        
        private Expression ParseExpression()
        {
            // Enhanced expression parsing with postfix operations
            return ParsePostfixExpression();
        }
        
        private Expression ParseUnaryExpression()
        {
            // Handle unary operators
            if (Match(TokenType.Star))
            {
                return ParseDereferenceExpression();
            }
            
            if (Match(TokenType.Ampersand))
            {
                return ParseAddressOfExpression();
            }
            
            return ParsePrimaryExpression();
        }
        
        private Expression ParsePostfixExpression()
        {
            var expr = ParseUnaryExpression();
            
            while (true)
            {
                if (Match(TokenType.LeftBracket))
                {
                    // Array indexing: expr[index]
                    var index = ParseExpression();
                    Consume(TokenType.RightBracket, "Expected ']' after index");
                    
                    var bracketToken = Previous();
                    expr = new ArrayIndexExpression(expr, index, bracketToken.Line, bracketToken.Column);
                }
                else if (Match(TokenType.Dot))
                {
                    // Member access: expr.member
                    var memberName = Consume(TokenType.Identifier, "Expected member name after '.'");
                    expr = new MemberAccessExpression(expr, memberName.Lexeme, memberName.Line, memberName.Column);
                }
                else if (Match(TokenType.Arrow))
                {
                    // Pointer member access: expr->member
                    var memberName = Consume(TokenType.Identifier, "Expected member name after '->'");
                    // Convert to (*expr).member
                    var deref = new DereferenceExpression(expr, expr.Line, expr.Column);
                    expr = new MemberAccessExpression(deref, memberName.Lexeme, memberName.Line, memberName.Column);
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
            if (Match(TokenType.Number))
            {
                var value = Previous().Value;
                return new LiteralExpression(value, Previous().Line, Previous().Column);
            }
            
            if (Match(TokenType.String))
            {
                var value = Previous().Value;
                return new LiteralExpression(value, Previous().Line, Previous().Column);
            }
            
            if (Match(TokenType.True))
            {
                return new LiteralExpression(true, Previous().Line, Previous().Column);
            }
            
            if (Match(TokenType.False))
            {
                return new LiteralExpression(false, Previous().Line, Previous().Column);
            }
            
            if (Match(TokenType.Null))
            {
                return new LiteralExpression(null, Previous().Line, Previous().Column);
            }
            
            if (Match(TokenType.Identifier))
            {
                var name = Previous().Lexeme;
                return new VariableExpression(name, Previous().Line, Previous().Column);
            }
            
            if (Match(TokenType.LeftParen))
            {
                var expr = ParseExpression();
                Consume(TokenType.RightParen, "Expected ')' after expression");
                return expr;
            }
            
            throw Error("Expected expression");
        }
        
        private Statement ParseRegularStatement()
        {
            // This would integrate with the main parser for regular statements
            // For now, just parse expression statements
            var expr = ParseExpression();
            ConsumeSemicolon();
            return new ExpressionStatement(expr, expr.Line, expr.Column);
        }
        
        private void ConsumeSemicolon()
        {
            if (!Match(TokenType.Semicolon) && !Check(TokenType.RightBrace))
            {
                throw Error("Expected ';'");
            }
        }
        
        #endregion
        
        #region Token Management
        
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
            return Peek().Type == type;
        }
        
        private Token Advance()
        {
            if (!IsAtEnd()) current++;
            return Previous();
        }
        
        private bool IsAtEnd()
        {
            return current >= tokens.Count || Peek().Type == TokenType.Eof;
        }
        
        private Token Peek()
        {
            return tokens[current];
        }
        
        private Token Previous()
        {
            return tokens[current - 1];
        }
        
        private Token Consume(TokenType type, string message)
        {
            if (Check(type)) return Advance();
            throw Error(message);
        }
        
        #endregion
        
        #region Error Handling
        
        private ParseException Error(string message)
        {
            var token = Peek();
            return new ParseException(message, token.Line, token.Column);
        }
        
        private void ReportError(ParseException error)
        {
            Console.WriteLine($"Parse error at {error.Line}:{error.Column}: {error.Message}");
        }
        
        private void Synchronize()
        {
            Advance();
            
            while (!IsAtEnd())
            {
                if (Previous().Type == TokenType.Semicolon) return;
                
                switch (Peek().Type)
                {
                    case TokenType.Class:
                    case TokenType.Function:
                    case TokenType.Var:
                    case TokenType.Let:
                    case TokenType.For:
                    case TokenType.If:
                    case TokenType.While:
                    case TokenType.Return:
                        return;
                }
                
                Advance();
            }
        }
        
        #endregion
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
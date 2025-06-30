# Ouroboros Core API Reference

## Table of Contents
- [Core Namespace](#core-namespace)
- [Lexer API](#lexer-api)
- [Parser API](#parser-api)
- [AST API](#ast-api)
- [Compiler API](#compiler-api)
- [Virtual Machine API](#virtual-machine-api)

## Core Namespace

The `Ouroboros.Core` namespace contains the fundamental components of the language implementation.

### Token Class

```ouro
class Token {
    public TokenType Type { get; }
    public string Lexeme { get; }
    public object Value { get; }
    public int Line { get; }
    public int Column { get; }
    public int Position { get; }
    public string FileName { get; }
    public SyntaxLevel SyntaxLevel { get; }
    
    public Token(type: TokenType, lexeme: string, value: object, 
                 line: int, column: int, position: int, 
                 fileName: string, syntaxLevel: SyntaxLevel)
}
```

### TokenType Enumeration

Comprehensive enumeration of all token types including:
- Keywords (if, else, while, for, class, etc.)
- Operators (+, -, *, /, ==, !=, etc.)
- Greek symbols (α, β, γ, π, Σ, etc.)
- Mathematical operators (≤, ≥, ≠, ∈, √, etc.)
- Special tokens (@high, @medium, @low, @asm)

## Lexer API

### Lexer Class

```ouro
class Lexer {
    public Lexer(source: string, fileName: string = "<source>")
    public List<Token> ScanTokens(): List<Token>
    public bool HadError { get; }
}
```

**Methods:**
- `ScanTokens()` - Scans the source code and returns a list of tokens
- Supports Unicode characters for Greek letters and mathematical symbols
- Handles multiple number formats (decimal, hexadecimal, binary, scientific notation)
- Supports string interpolation with `$"..."` syntax

**Example Usage:**
```ouro
let source = "let π = 3.14159;";
let lexer = new Lexer(source);
let tokens = lexer.ScanTokens();
```

## Parser API

### Parser Class

```ouro
class Parser {
    public Parser(tokens: List<Token>)
    public List<Statement> Parse(): List<Statement>
    public Expression ParseExpression(): Expression
    public bool HadError { get; }
}
```

**Key Features:**
- Recursive descent parsing
- Support for all three syntax levels
- Pattern matching expressions
- Lambda expressions
- Custom loop constructs

**Example Usage:**
```ouro
let parser = new Parser(tokens);
let ast = parser.Parse();
```

## AST API

### Base AST Node

```ouro
abstract class AstNode {
    public int Line { get; set; }
    public int Column { get; set; }
    public abstract T Accept<T>(visitor: IVisitor<T>)
}
```

### Expression Nodes

- **BinaryExpression** - Binary operations (a + b)
- **UnaryExpression** - Unary operations (-x, !x)
- **LiteralExpression** - Literal values (42, "hello", true)
- **VariableExpression** - Variable references
- **AssignmentExpression** - Variable assignments
- **CallExpression** - Function calls
- **LambdaExpression** - Lambda/anonymous functions
- **PatternMatchExpression** - Pattern matching

### Statement Nodes

- **ExpressionStatement** - Expression as statement
- **VariableDeclaration** - Variable declarations
- **FunctionDeclaration** - Function definitions
- **ClassDeclaration** - Class definitions
- **IfStatement** - Conditional statements
- **WhileStatement** - While loops
- **ForStatement** - For loops
- **CustomLoopStatement** - repeat, iterate, forever loops
- **ReturnStatement** - Return statements
- **BlockStatement** - Block of statements

## Compiler API

### Compiler Class

```ouro
class Compiler {
    public Compiler()
    public byte[] Compile(ast: List<Statement>): byte[]
    public CompilationResult CompileProgram(ast: List<Statement>): CompilationResult
}
```

### BytecodeBuilder Class

```ouro
class BytecodeBuilder {
    public BytecodeBuilder()
    public void Emit(opcode: Opcode)
    public void EmitByte(byte: byte)
    public void EmitConstant(value: object): int
    public int EmitJump(opcode: Opcode): int
    public void PatchJump(offset: int)
    public byte[] GetBytecode(): byte[]
}
```

### TypeChecker Class

```ouro
class TypeChecker {
    public TypeChecker()
    public void CheckProgram(ast: List<Statement>)
    public Type InferType(expr: Expression): Type
}
```

## Virtual Machine API

### VirtualMachine Class

```ouro
class VirtualMachine {
    public int ProgramCounter { get; }
    public int StackPointer { get; }
    public int FramePointer { get; }
    public object Accumulator { get; }
    
    public VirtualMachine()
    public void Execute(bytecode: byte[])
    public void Step()
    public byte ReadMemory(address: int): byte
    public void WriteMemory(address: int, value: byte)
    
    // Events for debugging/profiling
    public event Action<string, int> OnFunctionEnter
    public event Action<string> OnFunctionExit
    public event Action<int, Opcode> OnInstructionExecute
    public event Action<long> OnMemoryAllocate
    public event Action<long> OnMemoryFree
}
```

### Opcode Enumeration

Complete set of VM opcodes including:
- Stack operations (PUSH, POP, DUP, SWAP)
- Arithmetic (ADD, SUB, MUL, DIV, MOD)
- Comparison (EQ, NE, LT, GT, LE, GE)
- Control flow (JMP, JZ, JNZ, CALL, RET)
- Memory operations (LOAD, STORE, ALLOC, FREE)
- Type operations (CAST, TYPEOF, INSTANCEOF)

## Error Handling

### OuroborosException Class

```ouro
class OuroborosException : Exception {
    public int Line { get; }
    public int Column { get; }
    public string FileName { get; }
    
    public OuroborosException(message: string, line: int, column: int, fileName: string)
}
```

### Common Exception Types

- **LexerException** - Lexical analysis errors
- **ParseException** - Parsing errors
- **CompilationException** - Compilation errors
- **RuntimeException** - Runtime execution errors
- **TypeException** - Type checking errors

## Extension Points

### IVisitor Interface

```ouro
interface IVisitor<T> {
    T VisitBinaryExpression(expr: BinaryExpression): T
    T VisitUnaryExpression(expr: UnaryExpression): T
    T VisitLiteralExpression(expr: LiteralExpression): T
    // ... other visit methods
}
```

Used for implementing custom AST traversals for:
- Code generation
- Optimization
- Analysis
- Transformation 
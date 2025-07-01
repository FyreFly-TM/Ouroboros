# Ouroboros Core API Reference

## Table of Contents
1. [Core Types](#core-types)
2. [Compiler API](#compiler-api)
3. [Virtual Machine API](#virtual-machine-api)
4. [Runtime API](#runtime-api)
5. [Type System API](#type-system-api)
6. [Module System API](#module-system-api)
7. [Memory Management API](#memory-management-api)
8. [Error Handling API](#error-handling-api)

## Core Types

### AstNode
Base class for all Abstract Syntax Tree nodes.

```csharp
public abstract class AstNode
{
    public SourceLocation Location { get; set; }
    public AstNode Parent { get; set; }
    public List<AstNode> Children { get; }
    
    public abstract T Accept<T>(IAstVisitor<T> visitor);
    public abstract void Accept(IAstVisitor visitor);
}
```

### Token
Represents a lexical token.

```csharp
public class Token
{
    public TokenType Type { get; set; }
    public string Lexeme { get; set; }
    public object Literal { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
    public string File { get; set; }
}
```

### BytecodeChunk
Container for compiled bytecode.

```csharp
public class BytecodeChunk
{
    public List<byte> Code { get; }
    public List<Value> Constants { get; }
    public List<int> LineNumbers { get; }
    public Dictionary<string, int> Labels { get; }
    
    public void EmitByte(byte b);
    public void EmitBytes(params byte[] bytes);
    public int AddConstant(Value value);
    public void PatchJump(int offset);
}
```

## Compiler API

### Compiler
Main compiler class for compiling Ouroboros source code.

```csharp
public class Compiler
{
    public CompilerOptions Options { get; set; }
    
    // Main compilation methods
    public BytecodeChunk Compile(string source);
    public BytecodeChunk Compile(string source, SyntaxLevel level);
    public BytecodeChunk Compile(AstNode ast);
    
    // Module compilation
    public Module CompileModule(string name, string source);
    public Program LinkModules(params Module[] modules);
    
    // Incremental compilation
    public void AddSourceFile(string path);
    public void RemoveSourceFile(string path);
    public BytecodeChunk CompileProject();
    
    // Error handling
    public event EventHandler<CompilerError> ErrorOccurred;
    public List<CompilerError> GetErrors();
    public void ClearErrors();
}

public class CompilerOptions
{
    public OptimizationLevel OptimizationLevel { get; set; }
    public bool EnableDebugInfo { get; set; }
    public bool EnableContracts { get; set; }
    public bool EnableGPU { get; set; }
    public Platform TargetPlatform { get; set; }
    public Architecture TargetArchitecture { get; set; }
}

public enum OptimizationLevel
{
    None,
    Basic,
    Full,
    Aggressive
}
```

### Lexer
Tokenizes Ouroboros source code.

```csharp
public class Lexer
{
    public Lexer(string source);
    public Lexer(string source, string fileName);
    
    public Token NextToken();
    public List<Token> ScanAllTokens();
    public void Reset();
    
    // Error handling
    public event EventHandler<LexerError> ErrorOccurred;
}
```

### Parser
Parses tokens into an Abstract Syntax Tree.

```csharp
public class Parser
{
    public Parser(Lexer lexer);
    public Parser(List<Token> tokens);
    
    public AstNode Parse();
    public List<AstNode> ParseStatements();
    public AstNode ParseExpression();
    
    // Syntax level switching
    public void SetSyntaxLevel(SyntaxLevel level);
    public SyntaxLevel GetCurrentSyntaxLevel();
    
    // Error recovery
    public void Synchronize();
    public bool IsAtEnd();
}

public enum SyntaxLevel
{
    High,
    Medium,
    Low,
    Assembly
}
```

### TypeChecker
Performs semantic analysis and type checking.

```csharp
public class TypeChecker
{
    public TypeEnvironment Environment { get; }
    
    public void Check(AstNode ast);
    public Type InferType(Expression expr);
    public bool IsAssignableTo(Type from, Type to);
    
    // Type operations
    public Type UnifyTypes(Type a, Type b);
    public Type ResolveGenericType(Type generic, Dictionary<string, Type> bindings);
    
    // Error reporting
    public event EventHandler<TypeError> ErrorOccurred;
}
```

## Virtual Machine API

### VirtualMachine
Executes Ouroboros bytecode.

```csharp
public class VirtualMachine
{
    public VMOptions Options { get; set; }
    public CallStack CallStack { get; }
    public ValueStack Stack { get; }
    
    // Execution
    public int Execute(BytecodeChunk chunk);
    public Task<int> ExecuteAsync(BytecodeChunk chunk);
    public void Step();
    public void Run();
    public void Halt();
    
    // Debugging
    public void SetBreakpoint(int line);
    public void RemoveBreakpoint(int line);
    public VMState GetState();
    
    // Native function binding
    public void RegisterNativeFunction(string name, NativeFunction fn);
    public void RegisterModule(string name, NativeModule module);
}

public class VMOptions
{
    public int StackSize { get; set; } = 1024 * 1024;
    public int CallStackDepth { get; set; } = 1000;
    public bool EnableTracing { get; set; }
    public bool EnableProfiling { get; set; }
    public GCOptions GCOptions { get; set; }
}
```

### Value
Represents a runtime value in the VM.

```csharp
public struct Value
{
    public ValueType Type { get; }
    public object Data { get; }
    
    // Factory methods
    public static Value Number(double n);
    public static Value String(string s);
    public static Value Bool(bool b);
    public static Value Null();
    public static Value Object(ObjInstance obj);
    
    // Operations
    public bool IsTruthy();
    public bool Equals(Value other);
    public string ToString();
}

public enum ValueType
{
    Null,
    Bool,
    Number,
    String,
    Object,
    Function,
    NativeFunction,
    Class,
    Instance
}
```

## Runtime API

### Runtime
High-level runtime environment for Ouroboros.

```csharp
public class Runtime
{
    public RuntimeOptions Options { get; set; }
    
    // Execution
    public int Execute(Program program);
    public Task<int> ExecuteAsync(Program program);
    public Task<T> EvaluateAsync<T>(string expression);
    
    // Module management
    public void LoadModule(string name, Module module);
    public Module GetModule(string name);
    public void UnloadModule(string name);
    
    // Global environment
    public void SetGlobal(string name, Value value);
    public Value GetGlobal(string name);
    
    // Event handling
    public event EventHandler<RuntimeError> ErrorOccurred;
    public event EventHandler<string> OutputReceived;
}

public class RuntimeOptions
{
    public bool EnableJIT { get; set; }
    public bool EnableAOT { get; set; }
    public int MaxMemory { get; set; }
    public string[] ImportPaths { get; set; }
}
```

### AsyncRuntime
Asynchronous execution support.

```csharp
public class AsyncRuntime
{
    public TaskScheduler Scheduler { get; set; }
    
    // Task management
    public Task<T> CreateTask<T>(Func<T> function);
    public Task RunAsync(Action action);
    public Task<T> RunAsync<T>(Func<T> function);
    
    // Synchronization
    public AsyncLock CreateLock();
    public AsyncSemaphore CreateSemaphore(int count);
    public AsyncChannel<T> CreateChannel<T>(int capacity = -1);
    
    // Cancellation
    public CancellationTokenSource CreateCancellationTokenSource();
}
```

## Type System API

### Type
Base class for all types in Ouroboros.

```csharp
public abstract class Type
{
    public string Name { get; set; }
    public TypeKind Kind { get; }
    
    public abstract bool IsAssignableFrom(Type other);
    public abstract bool IsEquivalentTo(Type other);
    public virtual Type Substitute(Dictionary<string, Type> bindings);
}

public enum TypeKind
{
    Primitive,
    Class,
    Interface,
    Array,
    Tuple,
    Function,
    Generic,
    Union,
    Intersection
}
```

### TypeSystem
Central type system management.

```csharp
public class TypeSystem
{
    // Built-in types
    public static readonly Type Int = new PrimitiveType("int");
    public static readonly Type Float = new PrimitiveType("float");
    public static readonly Type Double = new PrimitiveType("double");
    public static readonly Type Bool = new PrimitiveType("bool");
    public static readonly Type String = new PrimitiveType("string");
    public static readonly Type Void = new PrimitiveType("void");
    public static readonly Type Any = new PrimitiveType("any");
    
    // Type creation
    public ArrayType CreateArrayType(Type elementType);
    public TupleType CreateTupleType(params Type[] elements);
    public FunctionType CreateFunctionType(Type[] parameters, Type returnType);
    public GenericType CreateGenericType(string name, Type[] constraints = null);
    
    // Type registration
    public void RegisterType(Type type);
    public Type LookupType(string name);
    public bool IsTypeDefined(string name);
}
```

## Module System API

### Module
Represents a compiled module.

```csharp
public class Module
{
    public string Name { get; set; }
    public string Version { get; set; }
    public Dictionary<string, Value> Exports { get; }
    public List<string> Dependencies { get; }
    
    public void Export(string name, Value value);
    public Value Import(string name);
    public bool HasExport(string name);
}
```

### ModuleSystem
Manages module loading and resolution.

```csharp
public class ModuleSystem
{
    public List<string> SearchPaths { get; }
    
    // Module loading
    public Module LoadModule(string name);
    public Task<Module> LoadModuleAsync(string name);
    public void UnloadModule(string name);
    
    // Module resolution
    public string ResolveModulePath(string name);
    public bool IsModuleLoaded(string name);
    public Module GetLoadedModule(string name);
    
    // Module compilation
    public Module CompileModule(string path);
    public void RecompileModule(string name);
}
```

## Memory Management API

### GarbageCollector
Manages automatic memory management.

```csharp
public class GarbageCollector
{
    public GCOptions Options { get; set; }
    public GCStatistics Statistics { get; }
    
    // Collection control
    public void Collect();
    public void Collect(int generation);
    public void WaitForPendingFinalizers();
    
    // Memory pressure
    public void AddMemoryPressure(long bytes);
    public void RemoveMemoryPressure(long bytes);
    
    // Monitoring
    public long GetTotalMemory(bool forceFullCollection);
    public int GetGeneration(object obj);
}

public class GCOptions
{
    public GCMode Mode { get; set; }
    public int Gen0Threshold { get; set; }
    public int Gen1Threshold { get; set; }
    public int Gen2Threshold { get; set; }
    public bool EnableConcurrent { get; set; }
}

public enum GCMode
{
    Workstation,
    Server,
    Conservative,
    Aggressive
}
```

### MemoryAllocator
Low-level memory allocation interface.

```csharp
public unsafe class MemoryAllocator
{
    // Allocation
    public void* Allocate(int size);
    public void* AllocateZeroed(int size);
    public void* Reallocate(void* ptr, int newSize);
    
    // Deallocation
    public void Free(void* ptr);
    public void FreeAll();
    
    // Statistics
    public long GetAllocatedBytes();
    public int GetAllocationCount();
    public void ResetStatistics();
}
```

## Error Handling API

### CompilerError
Represents a compilation error.

```csharp
public class CompilerError
{
    public ErrorLevel Level { get; set; }
    public string Code { get; set; }
    public string Message { get; set; }
    public SourceLocation Location { get; set; }
    public string File { get; set; }
    public List<string> Notes { get; }
    
    public string GetFormattedMessage();
    public string GetSourceContext(int contextLines = 3);
}

public enum ErrorLevel
{
    Warning,
    Error,
    Fatal
}
```

### RuntimeError
Represents a runtime error.

```csharp
public class RuntimeError : Exception
{
    public Value ErrorValue { get; set; }
    public CallStack CallStack { get; set; }
    public string SourceFile { get; set; }
    public int Line { get; set; }
    
    public string GetStackTrace();
    public string GetDetailedMessage();
}
```

### DiagnosticEngine
Central error reporting and diagnostic system.

```csharp
public class DiagnosticEngine
{
    public DiagnosticOptions Options { get; set; }
    
    // Error reporting
    public void ReportError(CompilerError error);
    public void ReportWarning(string message, SourceLocation location);
    public void ReportInfo(string message, SourceLocation location);
    
    // Error collection
    public List<CompilerError> GetErrors();
    public List<CompilerError> GetWarnings();
    public bool HasErrors();
    public void Clear();
    
    // Error suppression
    public void SuppressWarning(string code);
    public void SetWarningAsError(string code);
}

public class DiagnosticOptions
{
    public bool TreatWarningsAsErrors { get; set; }
    public int WarningLevel { get; set; }
    public bool EnableColoredOutput { get; set; }
    public bool ShowSourceContext { get; set; }
} 
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ouro.Core.Compiler;
using Ouro.Core.Parser;
using Ouro.Core.Lexer;
using Ouro.Core.VM;
using Ouro.CodeGen;
using Ouro.Testing;
using Ouro.Runtime;

namespace Ouro.Tests.Integration
{
    [TestClass]
    public class CompilerIntegrationTests
    {
        private string testOutputDir;
        private Compiler compiler;
        private VirtualMachine vm;
        private Runtime.Runtime runtime;
        
        public void Setup()
        {
            testOutputDir = Path.Combine(Path.GetTempPath(), "ouro_tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(testOutputDir);
            compiler = new Compiler();
            vm = new VirtualMachine();
            runtime = new Runtime.Runtime();
        }
        
        public void Teardown()
        {
            if (Directory.Exists(testOutputDir))
            {
                Directory.Delete(testOutputDir, true);
            }
        }
        
        [Test("End-to-end compilation of simple program")]
        public async Task TestSimpleProgramCompilation()
        {
            var source = @"
                function Main() -> int {
                    Console.WriteLine(""Hello, Ouroboros!"");
                    return 0;
                }
            ";
            
            // Compile to bytecode
            var lexer = new Lexer(source);
            var tokens = lexer.ScanTokens();
            var parser = new Parser(tokens);
            var ast = parser.Parse();
            
            var bytecode = compiler.Compile(ast);
            
            Assert.IsNotNull(bytecode);
            Assert.Greater(bytecode.Instructions.Count, 0);
            
            // Generate native code
            var backend = new LLVMBackend();
            var outputPath = Path.Combine(testOutputDir, "test_program.exe");
            await backend.CompileToExecutable(bytecode, outputPath);
            
            Assert.IsTrue(File.Exists(outputPath));
        }
        
        [Test("Compile and run factorial program")]
        public async Task TestFactorialProgram()
        {
            var source = @"
                function Factorial(int n) -> int {
                    if (n <= 1) {
                        return 1;
                    }
                    return n * Factorial(n - 1);
                }
                
                function Main() -> int {
                    int result = Factorial(5);
                    Console.WriteLine($""5! = {result}"");
                    return 0;
                }
            ";
            
            var bytecode = CompileFromSource(source);
            
            // Run in VM
            var exitCode = vm.Run(bytecode);
            
            Assert.AreEqual(0, exitCode);
        }
        
        [Test("Compile program with classes")]
        public async Task TestClassCompilation()
        {
            var source = @"
                class Calculator {
                    private int value;
                    
                    public function Calculator(int initial) {
                        value = initial;
                    }
                    
                    public function Add(int x) -> void {
                        value += x;
                    }
                    
                    public function GetValue() -> int {
                        return value;
                    }
                }
                
                function Main() -> int {
                    Calculator calc = new Calculator(10);
                    calc.Add(5);
                    calc.Add(3);
                    int result = calc.GetValue();
                    Console.WriteLine($""Result: {result}"");
                    return 0;
                }
            ";
            
            var bytecode = CompileFromSource(source);
            var exitCode = vm.Run(bytecode);
            
            Assert.AreEqual(0, exitCode);
        }
        
        [Test("Compile with optimization levels")]
        public async Task TestOptimizationLevels()
        {
            var source = @"
                function ConstantMath() -> int {
                    int a = 2 + 3;
                    int b = a * 4;
                    int c = b - 1;
                    return c;
                }
                
                function Main() -> int {
                    return ConstantMath();
                }
            ";
            
            // Compile without optimization
            var compiler1 = new Compiler { OptimizationLevel = 0 };
            var bytecode1 = CompileFromSource(source, compiler1);
            
            // Compile with optimization
            var compiler2 = new Compiler { OptimizationLevel = 2 };
            var bytecode2 = CompileFromSource(source, compiler2);
            
            // Optimized bytecode should be smaller
            Assert.Less(bytecode2.Instructions.Count, bytecode1.Instructions.Count);
            
            // Both should produce same result
            var result1 = vm.Run(bytecode1);
            var result2 = vm.Run(bytecode2);
            Assert.AreEqual(result1, result2);
        }
        
        [Test("Multi-file compilation")]
        public async Task TestMultiFileCompilation()
        {
            var mathFile = @"
                namespace Math {
                    public function Add(int a, int b) -> int {
                        return a + b;
                    }
                    
                    public function Multiply(int a, int b) -> int {
                        return a * b;
                    }
                }
            ";
            
            var mainFile = @"
                import Math;
                
                function Main() -> int {
                    int sum = Math.Add(10, 20);
                    int product = Math.Multiply(sum, 2);
                    Console.WriteLine($""Result: {product}"");
                    return 0;
                }
            ";
            
            // Save files
            var mathPath = Path.Combine(testOutputDir, "math.ouro");
            var mainPath = Path.Combine(testOutputDir, "main.ouro");
            await File.WriteAllTextAsync(mathPath, mathFile);
            await File.WriteAllTextAsync(mainPath, mainFile);
            
            // Compile project
            compiler.AddSourceFile(mathPath);
            compiler.AddSourceFile(mainPath);
            
            var bytecode = compiler.CompileProject();
            Assert.IsNotNull(bytecode);
            
            var exitCode = vm.Run(bytecode);
            Assert.AreEqual(0, exitCode);
        }
        
        [Test("Error handling compilation")]
        public void TestCompilationErrors()
        {
            var source = @"
                function Test() -> int {
                    int x = ""not a number"";  // Type error
                    return x;
                }
            ";
            
            var lexer = new Lexer(source);
            var tokens = lexer.ScanTokens();
            var parser = new Parser(tokens);
            var ast = parser.Parse();
            
            Assert.Throws<CompilationException>(() => compiler.Compile(ast));
        }
        
        [Test("Async/await compilation")]
        public async Task TestAsyncCompilation()
        {
            var source = @"
                async function DelayedGreeting() -> Task<string> {
                    await Task.Delay(100);
                    return ""Hello from async!"";
                }
                
                async function Main() -> Task<int> {
                    string message = await DelayedGreeting();
                    Console.WriteLine(message);
                    return 0;
                }
            ";
            
            var bytecode = CompileFromSource(source);
            Assert.Contains(bytecode.Instructions, i => i.Opcode == Opcode.Await);
            
            var exitCode = await runtime.ExecuteAsync(bytecode);
            Assert.AreEqual(0, exitCode);
        }
        
        [Test("Pattern matching compilation")]
        public async Task TestPatternMatchingCompilation()
        {
            var source = @"
                function GetTypeName(object value) -> string {
                    return value switch {
                        int n => $""Integer: {n}"",
                        string s => $""String: {s}"",
                        bool b => $""Boolean: {b}"",
                        _ => ""Unknown type""
                    };
                }
                
                function Main() -> int {
                    Console.WriteLine(GetTypeName(42));
                    Console.WriteLine(GetTypeName(""hello""));
                    Console.WriteLine(GetTypeName(true));
                    return 0;
                }
            ";
            
            var bytecode = CompileFromSource(source);
            Assert.Contains(bytecode.Instructions, i => i.Opcode == Opcode.TypeCheck);
            
            var exitCode = vm.Run(bytecode);
            Assert.AreEqual(0, exitCode);
        }
        
        [Test("Compilation with all syntax levels")]
        public void TestMultiLevelSyntaxCompilation()
        {
            // High-level syntax
            string highLevel = @"
                create function fibonacci that takes n as integer and returns integer {
                    when n is less than or equal to 1, return n.
                    otherwise, return fibonacci of n minus 1 plus fibonacci of n minus 2.
                }
            ";

            // Medium-level syntax
            string mediumLevel = @"
                int32 factorial(int32 n) {
                    if (n <= 1) return 1;
                    return n * factorial(n - 1);
                }
            ";

            // Low-level syntax
            string lowLevel = @"
                .function add
                .params 2
                .locals 0
                    LOAD_ARG 0
                    LOAD_ARG 1
                    ADD
                    RETURN
                .end
            ";

            // Test each syntax level
            var highBytecode = compiler.Compile(highLevel, SyntaxLevel.High);
            Assert.IsNotNull(highBytecode);

            var mediumBytecode = compiler.Compile(mediumLevel, SyntaxLevel.Medium);
            Assert.IsNotNull(mediumBytecode);

            var lowBytecode = compiler.Compile(lowLevel, SyntaxLevel.Low);
            Assert.IsNotNull(lowBytecode);
        }
        
        [Test("Cross-module compilation and linking")]
        public async Task TestCrossModuleCompilation()
        {
            // Module A
            string moduleA = @"
                export function add(a: int, b: int): int {
                    return a + b;
                }
                
                export const PI = 3.14159;
            ";

            // Module B that imports from A
            string moduleB = @"
                import { add, PI } from './moduleA';
                
                function calculateCircumference(radius: float): float {
                    return 2 * PI * radius;
                }
                
                function main() {
                    let sum = add(5, 3);
                    let circumference = calculateCircumference(10.0);
                    print($""Sum: {sum}, Circumference: {circumference}"");
                }
            ";

            // Compile modules
            var moduleABytecode = compiler.CompileModule("moduleA", moduleA);
            var moduleBBytecode = compiler.CompileModule("moduleB", moduleB);

            // Link modules
            var linkedProgram = compiler.LinkModules(new[] { moduleABytecode, moduleBBytecode });
            Assert.IsNotNull(linkedProgram);

            // Execute linked program
            var result = await runtime.ExecuteAsync(linkedProgram);
            Assert.AreEqual(0, result);
        }
        
        [Test("Error handling and recovery")]
        public void TestCompilationErrorHandling()
        {
            // Syntax error
            string syntaxError = @"
                function test() {
                    let x = 5 +;  // Missing operand
                }
            ";

            Assert.Throws<CompilerException>(() => compiler.Compile(syntaxError));

            // Type error
            string typeError = @"
                function test() {
                    let x: int = ""hello"";  // Type mismatch
                }
            ";

            Assert.Throws<TypeCheckException>(() => compiler.Compile(typeError));

            // Undefined reference
            string undefinedRef = @"
                function test() {
                    let x = nonExistentFunction();
                }
            ";

            Assert.Throws<CompilerException>(() => compiler.Compile(undefinedRef));
        }
        
        [Test("Optimization pipeline")]
        public void TestOptimizationPipeline()
        {
            string source = @"
                function constantFolding() {
                    let a = 2 + 3 * 4;  // Should fold to 14
                    let b = true && false;  // Should fold to false
                    let c = ""hello"" + "" world"";  // Should fold to ""hello world""
                    return a;
                }
                
                function deadCodeElimination() {
                    if (false) {
                        print(""This should be eliminated"");
                    }
                    return 42;
                }
                
                function inlining() {
                    return add(5, 3);  // Should be inlined
                }
                
                inline function add(a: int, b: int): int {
                    return a + b;
                }
            ";

            // Compile without optimization
            var unoptimized = compiler.Compile(source, optimize: false);

            // Compile with optimization
            var optimized = compiler.Compile(source, optimize: true);

            // Optimized bytecode should be smaller
            Assert.Less(optimized.Instructions.Count, unoptimized.Instructions.Count);

            // Both should produce same result
            var unoptResult = vm.Execute(unoptimized);
            var optResult = vm.Execute(optimized);
            Assert.AreEqual(unoptResult, optResult);
        }
        
        [Test("Platform-specific code generation")]
        public void TestPlatformSpecificCodeGen()
        {
            string source = @"
                function platformTest() {
                    #if WINDOWS
                        return ""Running on Windows"";
                    #elif LINUX
                        return ""Running on Linux"";
                    #elif MACOS
                        return ""Running on macOS"";
                    #else
                        return ""Unknown platform"";
                    #endif
                }
            ";

            // Test Windows target
            var windowsBytecode = compiler.Compile(source, new CompilerOptions 
            { 
                TargetPlatform = Platform.Windows 
            });

            // Test Linux target
            var linuxBytecode = compiler.Compile(source, new CompilerOptions 
            { 
                TargetPlatform = Platform.Linux 
            });

            // Test macOS target
            var macosBytecode = compiler.Compile(source, new CompilerOptions 
            { 
                TargetPlatform = Platform.MacOS 
            });

            // Each should have different bytecode
            Assert.AreNotEqual(windowsBytecode.GetHashCode(), linuxBytecode.GetHashCode());
            Assert.AreNotEqual(linuxBytecode.GetHashCode(), macosBytecode.GetHashCode());
        }
        
        [Test("Memory management integration")]
        public void TestMemoryManagement()
        {
            string source = @"
                function testGarbageCollection() {
                    // Create many temporary objects
                    for (let i = 0; i < 10000; i++) {
                        let temp = new Array(1000);
                        temp[0] = i;
                    }
                    
                    // Force garbage collection
                    gc();
                    
                    // Memory should be reclaimed
                    return getMemoryUsage() < 10_000_000;  // Less than 10MB
                }
                
                function testManualMemory() {
                    // Allocate manual memory
                    let buffer = allocate(1024);
                    
                    // Write to buffer
                    for (let i = 0; i < 1024; i++) {
                        buffer[i] = i & 0xFF;
                    }
                    
                    // Read from buffer
                    let sum = 0;
                    for (let i = 0; i < 1024; i++) {
                        sum += buffer[i];
                    }
                    
                    // Free memory
                    free(buffer);
                    
                    return sum;
                }
            ";

            var bytecode = compiler.Compile(source);
            var result = vm.Execute(bytecode);
            Assert.AreEqual(0, result);
        }
        
        [Test("Contract compilation and verification")]
        public void TestContractCompilation()
        {
            string source = @"
                function divide(a: int, b: int): int
                    requires b != 0 ""Divisor cannot be zero""
                    ensures result * b == a ""Division invariant""
                {
                    return a / b;
                }
                
                class BankAccount {
                    private balance: decimal;
                    
                    invariant balance >= 0 ""Balance cannot be negative""
                    
                    function deposit(amount: decimal)
                        requires amount > 0 ""Deposit amount must be positive""
                        ensures balance == old(balance) + amount ""Balance increased by amount""
                    {
                        balance += amount;
                    }
                    
                    function withdraw(amount: decimal)
                        requires amount > 0 ""Withdrawal amount must be positive""
                        requires amount <= balance ""Insufficient funds""
                        ensures balance == old(balance) - amount ""Balance decreased by amount""
                    {
                        balance -= amount;
                    }
                }
            ";

            var bytecode = compiler.Compile(source, new CompilerOptions 
            { 
                EnableContracts = true 
            });
            
            Assert.IsNotNull(bytecode);
            Assert.IsTrue(bytecode.HasContractChecks);
        }
        
        [Test("GPU kernel compilation")]
        public void TestGPUKernelCompilation()
        {
            string source = @"
                @gpu
                kernel void vectorAdd(
                    global float* a,
                    global float* b,
                    global float* result,
                    int n
                ) {
                    let idx = getGlobalId(0);
                    if (idx < n) {
                        result[idx] = a[idx] + b[idx];
                    }
                }
                
                function testGPU() {
                    let size = 1024;
                    let a = new float[size];
                    let b = new float[size];
                    let result = new float[size];
                    
                    // Initialize arrays
                    for (let i = 0; i < size; i++) {
                        a[i] = i;
                        b[i] = i * 2;
                    }
                    
                    // Execute on GPU
                    gpu.execute(vectorAdd, size, a, b, result, size);
                    
                    // Verify results
                    for (let i = 0; i < size; i++) {
                        if (result[i] != a[i] + b[i]) {
                            return false;
                        }
                    }
                    
                    return true;
                }
            ";

            var bytecode = compiler.Compile(source, new CompilerOptions 
            { 
                EnableGPU = true 
            });
            
            Assert.IsNotNull(bytecode);
            Assert.IsTrue(bytecode.HasGPUKernels);
        }
        
        private BytecodeChunk CompileFromSource(string source, Compiler compiler = null)
        {
            compiler ??= new Compiler();
            
            var lexer = new Lexer(source);
            var tokens = lexer.ScanTokens();
            var parser = new Parser(tokens);
            var ast = parser.Parse();
            
            return compiler.Compile(ast);
        }
    }

    public enum Platform
    {
        Windows,
        Linux,
        MacOS
    }

    public class CompilerOptions
    {
        public Platform TargetPlatform { get; set; } = Platform.Windows;
        public bool EnableContracts { get; set; } = false;
        public bool EnableGPU { get; set; } = false;
    }
} 
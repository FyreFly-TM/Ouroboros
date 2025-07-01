using System;
using System.IO;
using System.Threading.Tasks;
using Ouroboros.Core.Compiler;
using Ouroboros.Core.Parser;
using Ouroboros.Core.Lexer;
using Ouroboros.Core.VM;
using Ouroboros.CodeGen;
using Ouroboros.Testing;

namespace Ouroboros.Tests.Integration
{
    [TestClass]
    public class CompilerIntegrationTests
    {
        private string testOutputDir;
        
        public void Setup()
        {
            testOutputDir = Path.Combine(Path.GetTempPath(), "ouroboros_tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(testOutputDir);
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
            
            var compiler = new Compiler();
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
            var vm = new VirtualMachine();
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
            var vm = new VirtualMachine();
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
            var vm = new VirtualMachine();
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
            var compiler = new Compiler();
            compiler.AddSourceFile(mathPath);
            compiler.AddSourceFile(mainPath);
            
            var bytecode = compiler.CompileProject();
            Assert.IsNotNull(bytecode);
            
            var vm = new VirtualMachine();
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
            
            var compiler = new Compiler();
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
            
            var vm = new VirtualMachine();
            var exitCode = await vm.RunAsync(bytecode);
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
            
            var vm = new VirtualMachine();
            var exitCode = vm.Run(bytecode);
            Assert.AreEqual(0, exitCode);
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
} 
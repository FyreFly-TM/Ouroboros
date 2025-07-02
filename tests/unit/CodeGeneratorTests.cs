using System;
using System.Collections.Generic;
using System.Linq;
using Ouro.Core.Compiler;
using Ouro.Core.Parser;
using Ouro.Core.Lexer;
using Ouro.Core.AST;
using Ouro.Core.VM;
using Ouro.Testing;
using Ouro.CodeGen;

namespace Ouro.Tests.Unit
{
    [TestClass]
    public class CodeGeneratorTests
    {
        private Compiler compiler;
        private LLVMBackend llvmBackend;
        
        [Test("Generate bytecode for simple expression")]
        public void TestSimpleExpression()
        {
            var source = "int x = 2 + 3 * 4;";
            var bytecode = CompileSource(source);
            
            Assert.IsNotNull(bytecode);
            Assert.Greater(bytecode.Instructions.Count, 0);
            
            // Should have constants for 2, 3, 4
            Assert.AreEqual(3, bytecode.Constants.Count);
        }
        
        [Test("Generate bytecode for function call")]
        public void TestFunctionCallGeneration()
        {
            var source = @"
                function Add(int a, int b) -> int {
                    return a + b;
                }
                
                int result = Add(5, 10);
            ";
            
            var bytecode = CompileSource(source);
            
            Assert.IsNotNull(bytecode);
            Assert.Contains(bytecode.Instructions, i => i.Opcode == Opcode.Call);
        }
        
        [Test("Generate bytecode for if statement")]
        public void TestIfStatementGeneration()
        {
            var source = @"
                int x = 10;
                if (x > 5) {
                    x = x * 2;
                } else {
                    x = 0;
                }
            ";
            
            var bytecode = CompileSource(source);
            
            Assert.IsNotNull(bytecode);
            Assert.Contains(bytecode.Instructions, i => i.Opcode == Opcode.JumpIfFalse);
            Assert.Contains(bytecode.Instructions, i => i.Opcode == Opcode.Jump);
        }
        
        [Test("Generate bytecode for while loop")]
        public void TestWhileLoopGeneration()
        {
            var source = @"
                int i = 0;
                while (i < 10) {
                    i = i + 1;
                }
            ";
            
            var bytecode = CompileSource(source);
            
            Assert.IsNotNull(bytecode);
            Assert.Contains(bytecode.Instructions, i => i.Opcode == Opcode.JumpIfFalse);
            Assert.Contains(bytecode.Instructions, i => i.Opcode == Opcode.Jump);
        }
        
        [Test("Generate bytecode for array operations")]
        public void TestArrayOperations()
        {
            var source = @"
                int[] arr = [1, 2, 3, 4, 5];
                arr[2] = 10;
                int value = arr[2];
            ";
            
            var bytecode = CompileSource(source);
            
            Assert.IsNotNull(bytecode);
            Assert.Contains(bytecode.Instructions, i => i.Opcode == Opcode.ArrayNew);
            Assert.Contains(bytecode.Instructions, i => i.Opcode == Opcode.ArrayStore);
            Assert.Contains(bytecode.Instructions, i => i.Opcode == Opcode.ArrayLoad);
        }
        
        [Test("Generate bytecode for class instantiation")]
        public void TestClassInstantiation()
        {
            var source = @"
                class Point {
                    int x;
                    int y;
                    
                    function Point(int x, int y) {
                        this.x = x;
                        this.y = y;
                    }
                }
                
                Point p = new Point(10, 20);
            ";
            
            var bytecode = CompileSource(source);
            
            Assert.IsNotNull(bytecode);
            Assert.Contains(bytecode.Instructions, i => i.Opcode == Opcode.New);
        }
        
        [Test("Generate bytecode for method calls")]
        public void TestMethodCalls()
        {
            var source = @"
                class Calculator {
                    function Add(int a, int b) -> int {
                        return a + b;
                    }
                }
                
                Calculator calc = new Calculator();
                int result = calc.Add(5, 3);
            ";
            
            var bytecode = CompileSource(source);
            
            Assert.IsNotNull(bytecode);
            Assert.Contains(bytecode.Instructions, i => i.Opcode == Opcode.CallMethod);
        }
        
        [Test("LLVM IR generation for function")]
        public void TestLLVMFunctionGeneration()
        {
            var source = @"
                function Square(int x) -> int {
                    return x * x;
                }
            ";
            
            var ast = ParseSource(source);
            compiler = new Compiler();
            var bytecode = compiler.Compile(ast);
            
            llvmBackend = new LLVMBackend();
            var llvmIR = llvmBackend.GenerateIR(bytecode);
            
            Assert.IsNotNull(llvmIR);
            Assert.Contains(llvmIR, "define");
            Assert.Contains(llvmIR, "Square");
            Assert.Contains(llvmIR, "mul");
        }
        
        [Test("LLVM IR optimization")]
        public void TestLLVMOptimization()
        {
            var source = @"
                function DeadCode() -> int {
                    int x = 5;
                    int y = 10;
                    x = x + 0;  // Should be optimized away
                    y = y * 1;  // Should be optimized away
                    return x + y;
                }
            ";
            
            var ast = ParseSource(source);
            compiler = new Compiler();
            var bytecode = compiler.Compile(ast);
            
            llvmBackend = new LLVMBackend();
            llvmBackend.OptimizationLevel = 2;
            var optimizedIR = llvmBackend.GenerateIR(bytecode);
            
            Assert.IsNotNull(optimizedIR);
            // Optimized IR should be shorter
        }
        
        [Test("Generate bytecode for exceptions")]
        public void TestExceptionHandling()
        {
            var source = @"
                try {
                    int x = 10 / 0;
                } catch (DivideByZeroException e) {
                    Console.WriteLine(""Error: "" + e.Message);
                }
            ";
            
            var bytecode = CompileSource(source);
            
            Assert.IsNotNull(bytecode);
            Assert.Contains(bytecode.Instructions, i => i.Opcode == Opcode.Try);
            Assert.Contains(bytecode.Instructions, i => i.Opcode == Opcode.Catch);
        }
        
        [Test("Generate bytecode for lambda")]
        public void TestLambdaGeneration()
        {
            var source = @"
                var add = (int a, int b) => a + b;
                int result = add(3, 4);
            ";
            
            var bytecode = CompileSource(source);
            
            Assert.IsNotNull(bytecode);
            Assert.Contains(bytecode.Instructions, i => i.Opcode == Opcode.CreateClosure);
        }
        
        [Test("Generate bytecode for async/await")]
        public void TestAsyncAwaitGeneration()
        {
            var source = @"
                async function FetchDataAsync() -> int {
                    await Task.Delay(100);
                    return 42;
                }
                
                int result = await FetchDataAsync();
            ";
            
            var bytecode = CompileSource(source);
            
            Assert.IsNotNull(bytecode);
            Assert.Contains(bytecode.Instructions, i => i.Opcode == Opcode.Await);
        }
        
        [Test("Generate bytecode for pattern matching")]
        public void TestPatternMatchingGeneration()
        {
            var source = @"
                object value = 42;
                
                string result = value switch {
                    int n => ""number"",
                    string s => ""string"",
                    _ => ""other""
                };
            ";
            
            var bytecode = CompileSource(source);
            
            Assert.IsNotNull(bytecode);
            Assert.Contains(bytecode.Instructions, i => i.Opcode == Opcode.TypeCheck);
        }
        
        [Test("Generate bytecode with optimization")]
        public void TestBytecodeOptimization()
        {
            var source = @"
                function ConstantFolding() -> int {
                    return 2 + 3 * 4 - 1;  // Should be folded to 13
                }
            ";
            
            compiler = new Compiler();
            compiler.OptimizationLevel = 2;
            
            var ast = ParseSource(source);
            var bytecode = compiler.Compile(ast);
            
            // Should have only one constant (13) after optimization
            Assert.AreEqual(1, bytecode.Constants.Count);
            Assert.AreEqual(13, bytecode.Constants[0]);
        }
        
        [Test("Generate native code")]
        public void TestNativeCodeGeneration()
        {
            var source = @"
                function Main() -> int {
                    return 42;
                }
            ";
            
            var ast = ParseSource(source);
            compiler = new Compiler();
            var bytecode = compiler.Compile(ast);
            
            llvmBackend = new LLVMBackend();
            var nativeCode = llvmBackend.GenerateNativeCode(bytecode, "x86_64");
            
            Assert.IsNotNull(nativeCode);
            Assert.Greater(nativeCode.Length, 0);
        }
        
        private BytecodeChunk CompileSource(string source)
        {
            var ast = ParseSource(source);
            compiler = new Compiler();
            return compiler.Compile(ast);
        }
        
        private Program ParseSource(string source)
        {
            var lexer = new Lexer(source);
            var tokens = lexer.ScanTokens();
            var parser = new Parser(tokens);
            return parser.Parse();
        }
    }
} 
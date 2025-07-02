using System;
using Ouro.Core.VM;
using Ouro.Core.Compiler;
using Ouro.Testing;

namespace Ouro.Tests.Unit
{
    [TestClass]
    public class VirtualMachineTests
    {
        private VirtualMachine CreateVM()
        {
            return new VirtualMachine();
        }

        [Test("Should execute arithmetic operations")]
        public void ExecuteArithmetic()
        {
            var vm = CreateVM();
            var bytecode = new BytecodeBuilder();
            
            // 2 + 3 = 5
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant(2.0));
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant(3.0));
            bytecode.Emit(Opcode.Add);
            bytecode.Emit(Opcode.Return);
            
            var program = new CompiledProgram
            {
                Bytecode = bytecode.GetBytecode()
            };
            
            var result = vm.Execute(program);
            Assert.AreEqual(5.0, result);
        }

        [Test("Should handle stack operations")]
        public void HandleStackOperations()
        {
            var vm = CreateVM();
            var bytecode = new BytecodeBuilder();
            
            // Push values and duplicate
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant(42.0));
            bytecode.Emit(Opcode.Dup);
            bytecode.Emit(Opcode.Add); // 42 + 42 = 84
            bytecode.Emit(Opcode.Return);
            
            var program = new CompiledProgram
            {
                Bytecode = bytecode.GetBytecode()
            };
            
            var result = vm.Execute(program);
            Assert.AreEqual(84.0, result);
        }

        [Test("Should execute comparison operations")]
        public void ExecuteComparisons()
        {
            var vm = CreateVM();
            var bytecode = new BytecodeBuilder();
            
            // 5 > 3
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant(5.0));
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant(3.0));
            bytecode.Emit(Opcode.Greater);
            bytecode.Emit(Opcode.Return);
            
            var program = new CompiledProgram
            {
                Bytecode = bytecode.GetBytecode()
            };
            
            var result = vm.Execute(program);
            Assert.AreEqual(true, result);
        }

        [Test("Should handle local variables")]
        public void HandleLocalVariables()
        {
            var vm = CreateVM();
            var bytecode = new BytecodeBuilder();
            
            // Store and load local variable
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant(123.0));
            bytecode.Emit(Opcode.StoreLocal, 0);
            bytecode.Emit(Opcode.LoadLocal, 0);
            bytecode.Emit(Opcode.Return);
            
            var program = new CompiledProgram
            {
                Bytecode = bytecode.GetBytecode()
            };
            
            var result = vm.Execute(program);
            Assert.AreEqual(123.0, result);
        }

        [Test("Should execute conditional jumps")]
        public void ExecuteConditionalJumps()
        {
            var vm = CreateVM();
            var bytecode = new BytecodeBuilder();
            
            // if (true) return 1 else return 2
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant(true));
            var jumpPos = bytecode.EmitJump(Opcode.JumpIfFalse);
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant(1.0));
            bytecode.Emit(Opcode.Return);
            bytecode.PatchJump(jumpPos);
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant(2.0));
            bytecode.Emit(Opcode.Return);
            
            var program = new CompiledProgram
            {
                Bytecode = bytecode.GetBytecode()
            };
            
            var result = vm.Execute(program);
            Assert.AreEqual(1.0, result);
        }

        [Test("Should handle function calls")]
        public void HandleFunctionCalls()
        {
            var vm = CreateVM();
            var bytecode = new BytecodeBuilder();
            
            // Define a simple function that doubles its argument
            var funcStart = bytecode.GetBytecode().Instructions.Count;
            bytecode.Emit(Opcode.LoadLocal, 0); // Load argument
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant(2.0));
            bytecode.Emit(Opcode.Multiply);
            bytecode.Emit(Opcode.Return);
            var funcEnd = bytecode.GetBytecode().Instructions.Count;
            
            // Main code: call function with argument 21
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant(21.0));
            bytecode.AddFunction("double", funcStart, funcEnd);
            bytecode.Emit(Opcode.Call, 1); // 1 argument
            bytecode.Emit(Opcode.Return);
            
            var program = new CompiledProgram
            {
                Bytecode = bytecode.GetBytecode()
            };
            
            var result = vm.Execute(program);
            Assert.AreEqual(42.0, result);
        }

        [Test("Should handle arrays")]
        public void HandleArrays()
        {
            var vm = CreateVM();
            var bytecode = new BytecodeBuilder();
            
            // Create array [1, 2, 3]
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant(1.0));
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant(2.0));
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant(3.0));
            bytecode.Emit(Opcode.MakeArray, 3);
            
            // Access array[1] (should be 2)
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant(1.0));
            bytecode.Emit(Opcode.LoadElement);
            bytecode.Emit(Opcode.Return);
            
            var program = new CompiledProgram
            {
                Bytecode = bytecode.GetBytecode()
            };
            
            var result = vm.Execute(program);
            Assert.AreEqual(2.0, result);
        }

        [Test("Should handle string operations")]
        public void HandleStringOperations()
        {
            var vm = CreateVM();
            var bytecode = new BytecodeBuilder();
            
            // Concatenate strings
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant("Hello, "));
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant("World!"));
            bytecode.Emit(Opcode.StringConcat);
            bytecode.Emit(Opcode.Return);
            
            var program = new CompiledProgram
            {
                Bytecode = bytecode.GetBytecode()
            };
            
            var result = vm.Execute(program);
            Assert.AreEqual("Hello, World!", result);
        }

        [Test("Should handle null values")]
        public void HandleNullValues()
        {
            var vm = CreateVM();
            var bytecode = new BytecodeBuilder();
            
            // null ?? "default"
            bytecode.Emit(Opcode.LoadNull);
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant("default"));
            bytecode.Emit(Opcode.NullCoalesce);
            bytecode.Emit(Opcode.Return);
            
            var program = new CompiledProgram
            {
                Bytecode = bytecode.GetBytecode()
            };
            
            var result = vm.Execute(program);
            Assert.AreEqual("default", result);
        }

        [Test("Should handle type checking")]
        public void HandleTypeChecking()
        {
            var vm = CreateVM();
            var bytecode = new BytecodeBuilder();
            
            // Check if "hello" is string
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant("hello"));
            bytecode.Emit(Opcode.IsInstance, bytecode.AddConstant("string"));
            bytecode.Emit(Opcode.Return);
            
            var program = new CompiledProgram
            {
                Bytecode = bytecode.GetBytecode()
            };
            
            var result = vm.Execute(program);
            Assert.AreEqual(true, result);
        }

        [Test("Should handle exceptions")]
        public void HandleExceptions()
        {
            var vm = CreateVM();
            var bytecode = new BytecodeBuilder();
            
            // Division by zero should throw
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant(1.0));
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant(0.0));
            bytecode.Emit(Opcode.Divide);
            bytecode.Emit(Opcode.Return);
            
            var program = new CompiledProgram
            {
                Bytecode = bytecode.GetBytecode()
            };
            
            Assert.Throws<VirtualMachineException>(() => vm.Execute(program));
        }

        [Test("Should handle global variables")]
        public void HandleGlobalVariables()
        {
            var vm = CreateVM();
            var bytecode = new BytecodeBuilder();
            
            // Store and load global
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant("test value"));
            bytecode.Emit(Opcode.StoreGlobal, bytecode.AddConstant("myGlobal"));
            bytecode.Emit(Opcode.LoadGlobal, bytecode.AddConstant("myGlobal"));
            bytecode.Emit(Opcode.Return);
            
            var program = new CompiledProgram
            {
                Bytecode = bytecode.GetBytecode()
            };
            
            var result = vm.Execute(program);
            Assert.AreEqual("test value", result);
        }

        [Test("Should handle loops")]
        public void HandleLoops()
        {
            var vm = CreateVM();
            var bytecode = new BytecodeBuilder();
            
            // Sum numbers from 1 to 5
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant(0.0)); // sum
            bytecode.Emit(Opcode.StoreLocal, 0);
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant(1.0)); // i
            bytecode.Emit(Opcode.StoreLocal, 1);
            
            var loopStart = bytecode.GetBytecode().Instructions.Count;
            bytecode.Emit(Opcode.LoadLocal, 1); // i
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant(5.0));
            bytecode.Emit(Opcode.LessEqual);
            var exitJump = bytecode.EmitJump(Opcode.JumpIfFalse);
            
            // sum += i
            bytecode.Emit(Opcode.LoadLocal, 0);
            bytecode.Emit(Opcode.LoadLocal, 1);
            bytecode.Emit(Opcode.Add);
            bytecode.Emit(Opcode.StoreLocal, 0);
            
            // i++
            bytecode.Emit(Opcode.LoadLocal, 1);
            bytecode.Emit(Opcode.LoadConstant, bytecode.AddConstant(1.0));
            bytecode.Emit(Opcode.Add);
            bytecode.Emit(Opcode.StoreLocal, 1);
            
            bytecode.EmitLoop(loopStart);
            bytecode.PatchJump(exitJump);
            
            bytecode.Emit(Opcode.LoadLocal, 0);
            bytecode.Emit(Opcode.Return);
            
            var program = new CompiledProgram
            {
                Bytecode = bytecode.GetBytecode()
            };
            
            var result = vm.Execute(program);
            Assert.AreEqual(15.0, result); // 1+2+3+4+5 = 15
        }
    }
} 
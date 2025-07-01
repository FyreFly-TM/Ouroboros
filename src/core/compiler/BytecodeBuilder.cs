using System;
using System.Collections.Generic;
using System.Linq;
using Ouroboros.Core.VM;
using Ouroboros.Core.AST;

namespace Ouroboros.Core.Compiler
{
    /// <summary>
    /// Builds bytecode instructions for the VM
    /// </summary>
    public class BytecodeBuilder
    {
        private List<byte> bytecode;
        private List<object> constantPool;
        private List<FunctionInfo> functions;
        private List<ClassInfo> classes;
        private List<InterfaceInfo> interfaces;
        private List<StructInfo> structs;
        private List<EnumInfo> enums;
        private List<ComponentInfo> components;
        private List<SystemInfo> systems;
        private List<EntityInfo> entities;
        private List<ExceptionHandler> exceptionHandlers;
        private Dictionary<int, int> jumpTargets;
        private List<int> pendingJumps;
        private readonly List<int> jumpPatches;
        private readonly Stack<int> loopStarts;
        private readonly Stack<List<int>> breakPatches;
        private readonly Stack<List<int>> continuePatches;
        
        public int CurrentPosition => bytecode.Count;
        
        public BytecodeBuilder()
        {
            bytecode = new List<byte>();
            constantPool = new List<object>();
            functions = new List<FunctionInfo>();
            classes = new List<ClassInfo>();
            interfaces = new List<InterfaceInfo>();
            structs = new List<StructInfo>();
            enums = new List<EnumInfo>();
            components = new List<ComponentInfo>();
            systems = new List<SystemInfo>();
            entities = new List<EntityInfo>();
            exceptionHandlers = new List<ExceptionHandler>();
            jumpTargets = new Dictionary<int, int>();
            pendingJumps = new List<int>();
            jumpPatches = new List<int>();
            loopStarts = new Stack<int>();
            breakPatches = new Stack<List<int>>();
            continuePatches = new Stack<List<int>>();
        }
        
        /// <summary>
        /// Emit a single opcode
        /// </summary>
        public void Emit(Opcode opcode)
        {
            bytecode.Add((byte)opcode);
        }
        
        /// <summary>
        /// Emit an opcode with one operand
        /// </summary>
        public void Emit(Opcode opcode, int operand)
        {
            bytecode.Add((byte)opcode);
            EmitInt32(operand);
        }
        
        /// <summary>
        /// Emit an opcode with two operands
        /// </summary>
        public void Emit(Opcode opcode, int operand1, int operand2)
        {
            bytecode.Add((byte)opcode);
            EmitInt32(operand1);
            EmitInt32(operand2);
        }
        
        /// <summary>
        /// Emit a jump instruction, returning position to patch
        /// </summary>
        public int EmitJump(Opcode jumpOpcode)
        {
            bytecode.Add((byte)jumpOpcode);
            var jumpAddress = bytecode.Count;
            EmitInt32(0); // Placeholder
            pendingJumps.Add(jumpAddress);
            return jumpAddress;
        }
        
        /// <summary>
        /// Emit a jump instruction with a specific target
        /// </summary>
        public void EmitJump(Opcode jumpOpcode, int target)
        {
            bytecode.Add((byte)jumpOpcode);
            EmitInt32(target);
        }
        
        /// <summary>
        /// Emit an instruction with no parameters (alias for Emit)
        /// </summary>
        public void EmitInstruction(Opcode opcode)
        {
            Emit(opcode);
        }
        
        /// <summary>
        /// Emit an instruction with a string parameter
        /// </summary>
        public void EmitInstruction(Opcode opcode, string parameter)
        {
            Emit(opcode, AddConstant(parameter));
        }
        
        /// <summary>
        /// Emit an instruction with an integer parameter (alias for Emit)
        /// </summary>
        public void EmitInstruction(Opcode opcode, int parameter)
        {
            Emit(opcode, parameter);
        }
        
        /// <summary>
        /// Emit a loop back to the specified position
        /// </summary>
        public void EmitLoop(int loopStart, Opcode loopOpcode = Opcode.Jump)
        {
            bytecode.Add((byte)loopOpcode);
            EmitInt32(loopStart - CurrentPosition - 4); // Relative jump
        }
        
        /// <summary>
        /// Patch a previously emitted jump
        /// </summary>
        public void PatchJump(int jumpAddress)
        {
            var jumpOffset = CurrentPosition - jumpAddress - 4;
            PatchInt32(jumpAddress, jumpOffset);
            pendingJumps.Remove(jumpAddress);
        }
        
        /// <summary>
        /// Add a constant to the pool
        /// </summary>
        public int AddConstant(object value)
        {
            // Check if constant already exists
            for (int i = 0; i < constantPool.Count; i++)
            {
                if (Equals(constantPool[i], value))
                    return i;
            }
            
            constantPool.Add(value);
            return constantPool.Count - 1;
        }
        
        /// <summary>
        /// Add a function definition
        /// </summary>
        public int AddFunction(string name, int startAddress, int endAddress)
        {
            functions.Add(new FunctionInfo
            {
                Name = name,
                StartAddress = startAddress,
                EndAddress = endAddress
            });
            return functions.Count - 1;
        }
        
        /// <summary>
        /// Add a class definition
        /// </summary>
        public int AddClass(ClassInfo classInfo)
        {
            classes.Add(classInfo);
            return classes.Count - 1;
        }
        
        /// <summary>
        /// Add an interface definition
        /// </summary>
        public int AddInterface(InterfaceInfo interfaceInfo)
        {
            interfaces.Add(interfaceInfo);
            return interfaces.Count - 1;
        }
        
        /// <summary>
        /// Add a struct definition
        /// </summary>
        public int AddStruct(StructInfo structInfo)
        {
            structs.Add(structInfo);
            return structs.Count - 1;
        }
        
        /// <summary>
        /// Add an enum definition
        /// </summary>
        public int AddEnum(EnumInfo enumInfo)
        {
            enums.Add(enumInfo);
            return enums.Count - 1;
        }
        
        /// <summary>
        /// Add a component definition
        /// </summary>
        public int AddComponent(ComponentInfo componentInfo)
        {
            components.Add(componentInfo);
            return components.Count - 1;
        }
        
        /// <summary>
        /// Add a system definition
        /// </summary>
        public int AddSystem(SystemInfo systemInfo)
        {
            systems.Add(systemInfo);
            return systems.Count - 1;
        }
        
        /// <summary>
        /// Add an entity archetype definition
        /// </summary>
        public int AddEntity(EntityInfo entityInfo)
        {
            entities.Add(entityInfo);
            return entities.Count - 1;
        }
        
        /// <summary>
        /// Register an exception handler
        /// </summary>
        public void RegisterExceptionHandler(int tryStart, int tryEnd, int handlerStart)
        {
            exceptionHandlers.Add(new ExceptionHandler
            {
                TryStart = tryStart,
                TryEnd = tryEnd,
                HandlerStart = handlerStart
            });
        }
        
        /// <summary>
        /// Emit inline assembly code
        /// </summary>
        public void EmitAssembly(string assemblyCode)
        {
            // Parse and emit assembly instructions
            var lines = assemblyCode.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";"))
                    continue;
                    
                EmitAssemblyInstruction(trimmed);
            }
        }
        
        /// <summary>
        /// Emit raw assembly code (alias for EmitAssembly)
        /// </summary>
        public void EmitRawAssembly(string assemblyCode)
        {
            EmitAssembly(assemblyCode);
        }
        
        /// <summary>
        /// Register an exception handler with exception type and variable name
        /// </summary>
        public void RegisterExceptionHandler(int tryStart, int tryEnd, int handlerStart, string exceptionType)
        {
            exceptionHandlers.Add(new ExceptionHandler
            {
                TryStart = tryStart,
                TryEnd = tryEnd,
                CatchStart = handlerStart,
                ExceptionType = exceptionType
            });
        }
        
        /// <summary>
        /// Get the final bytecode
        /// </summary>
        public Bytecode GetBytecode()
        {
            // Verify all jumps are patched
            if (pendingJumps.Count > 0)
            {
                throw new InvalidOperationException($"Unpatched jumps at positions: {string.Join(", ", pendingJumps)}");
            }
            
            // Convert our internal representation to the Bytecode format
            var bytecodeData = new List<byte>();
            
            // Convert list of bytes to the expected byte array
            return new Bytecode
            {
                Code = bytecode,
                Constants = constantPool
            };
        }
        
        #region Helper Methods
        
        private void EmitInt32(int value)
        {
            bytecode.Add((byte)(value & 0xFF));
            bytecode.Add((byte)((value >> 8) & 0xFF));
            bytecode.Add((byte)((value >> 16) & 0xFF));
            bytecode.Add((byte)((value >> 24) & 0xFF));
        }
        
        private void PatchInt32(int position, int value)
        {
            bytecode[position] = (byte)(value & 0xFF);
            bytecode[position + 1] = (byte)((value >> 8) & 0xFF);
            bytecode[position + 2] = (byte)((value >> 16) & 0xFF);
            bytecode[position + 3] = (byte)((value >> 24) & 0xFF);
        }
        
        private void EmitAssemblyInstruction(string instruction)
        {
            var parts = instruction.Split(' ', 2);
            var mnemonic = parts[0].ToUpper();
            var operands = parts.Length > 1 ? parts[1].Split(',').Select(s => s.Trim()).ToArray() : new string[0];
            
            switch (mnemonic)
            {
                case "PUSH":
                    if (operands.Length == 1)
                    {
                        if (int.TryParse(operands[0], out int value))
                        {
                            Emit(Opcode.LoadConstant, AddConstant(value));
                        }
                        else
                        {
                            // Register reference
                            EmitRegisterLoad(operands[0]);
                        }
                    }
                    break;
                    
                case "POP":
                    if (operands.Length == 0)
                    {
                        Emit(Opcode.Pop);
                    }
                    else
                    {
                        // Pop to register
                        EmitRegisterStore(operands[0]);
                    }
                    break;
                    
                case "ADD":
                    Emit(Opcode.Add);
                    break;
                    
                case "SUB":
                    Emit(Opcode.Subtract);
                    break;
                    
                case "MUL":
                    Emit(Opcode.Multiply);
                    break;
                    
                case "DIV":
                    Emit(Opcode.Divide);
                    break;
                    
                case "MOV":
                    if (operands.Length == 2)
                    {
                        EmitRegisterLoad(operands[1]);
                        EmitRegisterStore(operands[0]);
                    }
                    break;
                    
                case "JMP":
                    if (operands.Length == 1)
                    {
                        var label = operands[0];
                        Emit(Opcode.Jump, ResolveLabel(label));
                    }
                    break;
                    
                case "JZ":
                case "JE":
                    if (operands.Length == 1)
                    {
                        var label = operands[0];
                        Emit(Opcode.JumpIfFalse, ResolveLabel(label));
                    }
                    break;
                    
                case "JNZ":
                case "JNE":
                    if (operands.Length == 1)
                    {
                        var label = operands[0];
                        Emit(Opcode.JumpIfTrue, ResolveLabel(label));
                    }
                    break;
                    
                case "CALL":
                    if (operands.Length == 1)
                    {
                        var func = operands[0];
                        Emit(Opcode.Call, ResolveFunctionIndex(func));
                    }
                    break;
                    
                case "RET":
                    Emit(Opcode.Return);
                    break;
                    
                case "NOP":
                    Emit(Opcode.Nop);
                    break;
                    
                default:
                    // Try to handle as x86/x64 instruction
                    EmitNativeInstruction(mnemonic, operands);
                    break;
            }
        }
        
        private void EmitRegisterLoad(string register)
        {
            var regIndex = GetRegisterIndex(register);
            Emit(Opcode.LoadRegister, regIndex);
        }
        
        private void EmitRegisterStore(string register)
        {
            var regIndex = GetRegisterIndex(register);
            Emit(Opcode.StoreRegister, regIndex);
        }
        
        private int GetRegisterIndex(string register)
        {
            // Map register names to indices
            return register.ToUpper() switch
            {
                "RAX" or "EAX" or "AX" or "AL" => 0,
                "RBX" or "EBX" or "BX" or "BL" => 1,
                "RCX" or "ECX" or "CX" or "CL" => 2,
                "RDX" or "EDX" or "DX" or "DL" => 3,
                "RSI" or "ESI" or "SI" => 4,
                "RDI" or "EDI" or "DI" => 5,
                "RBP" or "EBP" or "BP" => 6,
                "RSP" or "ESP" or "SP" => 7,
                "R8" => 8,
                "R9" => 9,
                "R10" => 10,
                "R11" => 11,
                "R12" => 12,
                "R13" => 13,
                "R14" => 14,
                "R15" => 15,
                _ => throw new InvalidOperationException($"Unknown register: {register}")
            };
        }
        
        private Dictionary<string, int> labelAddresses = new Dictionary<string, int>();
        
        /// <summary>
        /// Register a label at the current position
        /// </summary>
        public void DefineLabel(string label)
        {
            labelAddresses[label] = CurrentPosition;
        }
        
        private int ResolveLabel(string label)
        {
            // Try to resolve the label to its address
            if (labelAddresses.TryGetValue(label, out int address))
            {
                return address - CurrentPosition - 4; // Return relative offset
            }
            
            // If label not found, this might be a forward reference
            // Add it to pending jumps to be resolved later
            Console.WriteLine($"Warning: Unresolved label '{label}', treating as forward reference");
            return EmitJump(Opcode.Jump); // Emit placeholder jump
        }
        
        private int ResolveFunctionIndex(string functionName)
        {
            for (int i = 0; i < functions.Count; i++)
            {
                if (functions[i].Name == functionName)
                    return i;
            }
            
            throw new InvalidOperationException($"Unknown function: {functionName}");
        }
        
        private void EmitNativeInstruction(string mnemonic, string[] operands)
        {
            // For native instructions, emit a special opcode followed by raw bytes
            Emit(Opcode.NativeInstruction);
            
            // Assemble the instruction based on mnemonic and operands
            var instructionBytes = AssembleInstruction(mnemonic, operands);
            
            Emit(Opcode.RawBytes, instructionBytes.Length);
            foreach (var b in instructionBytes)
            {
                bytecode.Add(b);
            }
        }
        
        private byte[] AssembleInstruction(string mnemonic, string[] operands)
        {
            // Basic x86-64 instruction assembly
            // This is a simplified implementation - a full assembler would be much more complex
            
            switch (mnemonic.ToUpper())
            {
                case "NOP":
                    return new byte[] { 0x90 };
                    
                case "INT":
                    if (operands.Length > 0 && byte.TryParse(operands[0], out byte intNum))
                        return new byte[] { 0xCD, intNum };
                    break;
                    
                case "HLT":
                    return new byte[] { 0xF4 };
                    
                case "CLC":
                    return new byte[] { 0xF8 };
                    
                case "STC":
                    return new byte[] { 0xF9 };
                    
                case "CLI":
                    return new byte[] { 0xFA };
                    
                case "STI":
                    return new byte[] { 0xFB };
                    
                case "CLD":
                    return new byte[] { 0xFC };
                    
                case "STD":
                    return new byte[] { 0xFD };
                    
                case "PUSHFD":
                    return new byte[] { 0x9C };
                    
                case "POPFD":
                    return new byte[] { 0x9D };
                    
                case "CPUID":
                    return new byte[] { 0x0F, 0xA2 };
                    
                case "RDTSC":
                    return new byte[] { 0x0F, 0x31 };
                    
                // Additional arithmetic instructions
                case "IDIV":
                    return new byte[] { 0xF7, 0xF8 }; // idiv rax/eax
                    
                case "IMUL":
                    return new byte[] { 0xF7, 0xE8 }; // imul rax/eax
                    
                // Bit manipulation instructions
                case "BSF":
                    return new byte[] { 0x0F, 0xBC }; // bit scan forward
                    
                case "BSR":
                    return new byte[] { 0x0F, 0xBD }; // bit scan reverse
                    
                case "POPCNT":
                    return new byte[] { 0xF3, 0x0F, 0xB8 }; // population count
                    
                case "LZCNT":
                    return new byte[] { 0xF3, 0x0F, 0xBD }; // leading zero count
                    
                case "TZCNT":
                    return new byte[] { 0xF3, 0x0F, 0xBC }; // trailing zero count
                    
                // Atomic operations
                case "LOCK":
                    return new byte[] { 0xF0 }; // lock prefix
                    
                case "XADD":
                    return new byte[] { 0x0F, 0xC1 }; // exchange and add
                    
                case "CMPXCHG":
                    return new byte[] { 0x0F, 0xB1 }; // compare and exchange
                    
                case "CMPXCHG8B":
                    return new byte[] { 0x0F, 0xC7 }; // compare and exchange 8 bytes
                    
                // SSE/AVX instructions for SIMD
                case "MOVAPS":
                    return new byte[] { 0x0F, 0x28 }; // move aligned packed single-precision
                    
                case "MOVUPS":
                    return new byte[] { 0x0F, 0x10 }; // move unaligned packed single-precision
                    
                case "ADDPS":
                    return new byte[] { 0x0F, 0x58 }; // add packed single-precision
                    
                case "MULPS":
                    return new byte[] { 0x0F, 0x59 }; // multiply packed single-precision
                    
                case "SUBPS":
                    return new byte[] { 0x0F, 0x5C }; // subtract packed single-precision
                    
                case "DIVPS":
                    return new byte[] { 0x0F, 0x5E }; // divide packed single-precision
                    
                // Memory barriers
                case "MFENCE":
                    return new byte[] { 0x0F, 0xAE, 0xF0 }; // memory fence
                    
                case "LFENCE":
                    return new byte[] { 0x0F, 0xAE, 0xE8 }; // load fence
                    
                case "SFENCE":
                    return new byte[] { 0x0F, 0xAE, 0xF8 }; // store fence
                    
                // String operations
                case "REP":
                    return new byte[] { 0xF3 }; // repeat prefix
                    
                case "MOVSB":
                    return new byte[] { 0xA4 }; // move string byte
                    
                case "STOSB":
                    return new byte[] { 0xAA }; // store string byte
                    
                case "CMPSB":
                    return new byte[] { 0xA6 }; // compare string byte
                    
                // System instructions
                case "SYSCALL":
                    return new byte[] { 0x0F, 0x05 }; // system call
                    
                case "SYSRET":
                    return new byte[] { 0x0F, 0x07 }; // return from system call
                    
                case "RDMSR":
                    return new byte[] { 0x0F, 0x32 }; // read model specific register
                    
                case "WRMSR":
                    return new byte[] { 0x0F, 0x30 }; // write model specific register
                    
                // Conditional moves
                case "CMOVE":
                    return new byte[] { 0x0F, 0x44 }; // conditional move if equal
                    
                case "CMOVNE":
                    return new byte[] { 0x0F, 0x45 }; // conditional move if not equal
                    
                case "CMOVG":
                    return new byte[] { 0x0F, 0x4F }; // conditional move if greater
                    
                case "CMOVL":
                    return new byte[] { 0x0F, 0x4C }; // conditional move if less
            }
            
            // For unrecognized instructions, emit a NOP and log a warning
            Console.WriteLine($"Warning: Unrecognized instruction '{mnemonic}', emitting NOP");
            return new byte[] { 0x90 };
        }
        
        private Stack<int> jumpsToResolve = new Stack<int>();
        
        public void PushJumpToResolve(int position)
        {
            jumpsToResolve.Push(position);
        }
        
        public bool HasJumpsToResolve()
        {
            return jumpsToResolve.Count > 0;
        }
        
        public int PopJumpToResolve()
        {
            return jumpsToResolve.Pop();
        }
        
        /// <summary>
        /// Mark the start of a loop
        /// </summary>
        public void MarkLoopStart()
        {
            loopStarts.Push(bytecode.Count);
            breakPatches.Push(new List<int>());
            continuePatches.Push(new List<int>());
        }
        
        /// <summary>
        /// Emit a break instruction
        /// </summary>
        public void EmitBreak()
        {
            if (breakPatches.Count == 0)
                throw new InvalidOperationException("Break statement outside of loop");
                
            var breakList = breakPatches.Peek();
            breakList.Add(EmitJump(Opcode.Jump));
        }
        
        /// <summary>
        /// Emit a continue instruction
        /// </summary>
        public void EmitContinue()
        {
            if (continuePatches.Count == 0)
                throw new InvalidOperationException("Continue statement outside of loop");
                
            var loopStart = loopStarts.Peek();
            EmitLoop(loopStart);
        }
        
        /// <summary>
        /// End a loop and patch all break statements
        /// </summary>
        public void EndLoop()
        {
            if (breakPatches.Count == 0)
                throw new InvalidOperationException("EndLoop called without MarkLoopStart");
                
            var breaks = breakPatches.Pop();
            continuePatches.Pop();
            loopStarts.Pop();
            
            // Patch all break statements to jump to here
            foreach (var breakPos in breaks)
            {
                PatchJump(breakPos);
            }
        }
        
        /// <summary>
        /// Get the current bytecode position
        /// </summary>
        public int GetPosition()
        {
            return bytecode.Count;
        }
        
        /// <summary>
        /// Reset loop tracking state for function scope
        /// </summary>
        public void ResetLoopTracking()
        {
            loopStarts.Clear();
            breakPatches.Clear();
            continuePatches.Clear();
        }
        
        /// <summary>
        /// Save current loop tracking state
        /// </summary>
        public (Stack<int>, Stack<List<int>>, Stack<List<int>>) SaveLoopTracking()
        {
            var savedLoopStarts = new Stack<int>(loopStarts.ToArray().Reverse());
            var savedBreakPatches = new Stack<List<int>>(breakPatches.ToArray().Reverse());
            var savedContinuePatches = new Stack<List<int>>(continuePatches.ToArray().Reverse());
            return (savedLoopStarts, savedBreakPatches, savedContinuePatches);
        }
        
        /// <summary>
        /// Restore loop tracking state
        /// </summary>
        public void RestoreLoopTracking((Stack<int>, Stack<List<int>>, Stack<List<int>>) savedState)
        {
            loopStarts.Clear();
            breakPatches.Clear();
            continuePatches.Clear();
            
            var (savedLoopStarts, savedBreakPatches, savedContinuePatches) = savedState;
            
            foreach (var loopStart in savedLoopStarts.ToArray().Reverse())
                loopStarts.Push(loopStart);
            foreach (var breakPatch in savedBreakPatches.ToArray().Reverse())
                breakPatches.Push(breakPatch);
            foreach (var continuePatch in savedContinuePatches.ToArray().Reverse())
                continuePatches.Push(continuePatch);
        }
        
        /// <summary>
        /// Emit specialized opcodes for mathematical operations
        /// </summary>
        public void EmitMathOperation(string operation)
        {
            switch (operation)
            {
                case "**": Emit(Opcode.Power); break;
                case "//": Emit(Opcode.IntegerDivision); break;
                case "<=>": Emit(Opcode.SpaceshipCompare); break;
                case "∈": Emit(Opcode.ElementOf); break;
                case "∪": Emit(Opcode.SetUnion); break;
                case "∩": Emit(Opcode.SetIntersection); break;
                case "\\": Emit(Opcode.SetDifference); break;
                case "×": Emit(Opcode.CrossProduct3D); break;
                case "·": Emit(Opcode.DotProduct3D); break;
                case "∂": Emit(Opcode.PartialDerivative); break;
                case "∇": Emit(Opcode.Gradient); break;
                case "lim": Emit(Opcode.Limit); break;
                case "∫": Emit(Opcode.Integral); break;
                case "μ": Emit(Opcode.Mean); break;
                case "σ": Emit(Opcode.StandardDeviation); break;
                case "σ²": Emit(Opcode.Variance); break;
                case "<<": Emit(Opcode.AppendToCollection); break;
                case ">>": Emit(Opcode.PrependToCollection); break;
                default:
                    throw new InvalidOperationException($"Unknown mathematical operation: {operation}");
            }
        }
        
        /// <summary>
        /// Emit function call for natural language operations
        /// </summary>
        public void EmitNaturalLanguageOperation(string operation, int argumentCount)
        {
            switch (operation)
            {
                case "all_even_numbers_from":
                    Emit(Opcode.AllEvenNumbers);
                    break;
                case "each_multiplied_by":
                    Emit(Opcode.EachMultipliedBy);
                    break;
                case "sum_of_all":
                    Emit(Opcode.SumOfAll);
                    break;
                default:
                    // Fall back to regular function call
                    Emit(Opcode.Call, argumentCount);
                    break;
            }
        }
        
        /// <summary>
        /// Emit specialized mathematical operations for Ouroboros
        /// </summary>
        public void EmitMathematicalOperation(string operation, params object[] operands)
        {
            switch (operation.ToLower())
            {
                case "partiald":
                case "∂":
                    // Partial derivative: function, point, variable index
                    foreach (var operand in operands)
                        Emit(Opcode.LoadConstant, AddConstant(operand));
                    Emit(Opcode.PartialDerivative);
                    break;
                    
                case "gradient":
                case "∇":
                case "nabla":
                    // Gradient: function, point
                    foreach (var operand in operands)
                        Emit(Opcode.LoadConstant, AddConstant(operand));
                    Emit(Opcode.Gradient);
                    break;
                    
                case "limit":
                case "lim":
                    // Limit: function, approach value
                    foreach (var operand in operands)
                        Emit(Opcode.LoadConstant, AddConstant(operand));
                    Emit(Opcode.Limit);
                    break;
                    
                case "integral":
                case "∫":
                    // Integral: function, lower bound, upper bound
                    foreach (var operand in operands)
                        Emit(Opcode.LoadConstant, AddConstant(operand));
                    Emit(Opcode.Integral);
                    break;
                    
                case "autodiff":
                    // Automatic differentiation: function, x
                    foreach (var operand in operands)
                        Emit(Opcode.LoadConstant, AddConstant(operand));
                    Emit(Opcode.AutoDiff);
                    break;
                    
                case "crossproduct":
                case "×":
                    // Cross product: vector a, vector b
                    Emit(Opcode.CrossProduct3D);
                    break;
                    
                case "dotproduct":
                case "·":
                    // Dot product: vector a, vector b
                    Emit(Opcode.DotProduct3D);
                    break;
                    
                case "mean":
                case "μ":
                    // Mean: data array
                    Emit(Opcode.Mean);
                    break;
                    
                case "stddev":
                case "σ":
                    // Standard deviation: data array
                    Emit(Opcode.StandardDeviation);
                    break;
                    
                case "variance":
                case "σ²":
                    // Variance: data array
                    Emit(Opcode.Variance);
                    break;
                    
                case "correlation":
                case "ρ":
                    // Correlation: x data, y data
                    Emit(Opcode.Correlation);
                    break;
                    
                default:
                    throw new InvalidOperationException($"Unknown mathematical operation: {operation}");
            }
        }
        
        /// <summary>
        /// Emit natural language operations for @high syntax
        /// </summary>
        public void EmitNaturalLanguageOperation(string operation)
        {
            switch (operation.ToLower())
            {
                case "alleveннumbers":
                    Emit(Opcode.AllEvenNumbers);
                    break;
                    
                case "eachmultipliedby":
                    Emit(Opcode.EachMultipliedBy);
                    break;
                    
                case "sumofall":
                    Emit(Opcode.SumOfAll);
                    break;
                    
                case "appendtocollection":
                case "<<":
                    Emit(Opcode.AppendToCollection);
                    break;
                    
                case "prependtocollection":
                case ">>":
                    Emit(Opcode.PrependToCollection);
                    break;
                    
                default:
                    throw new InvalidOperationException($"Unknown natural language operation: {operation}");
            }
        }
        
        /// <summary>
        /// Emit loop control structures for custom loop types
        /// </summary>
        public void EmitLoopControl(string loopType)
        {
            switch (loopType.ToLower())
            {
                case "repeat":
                    // repeat N times - handled by parser, no special opcode needed
                    break;
                    
                case "iterate":
                    // iterate counter from X through Y - handled by parser
                    break;
                    
                case "forever":
                    // forever loop - handled by parser
                    break;
                    
                default:
                    throw new InvalidOperationException($"Unknown loop type: {loopType}");
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Builder for class definitions
    /// </summary>
    public class ClassBuilder
    {
        private ClassInfo classInfo;
        
        public ClassBuilder(string name)
        {
            classInfo = new ClassInfo { Name = name };
            classInfo.Fields = new List<FieldInfo>();
            classInfo.Methods = new List<MethodInfo>();
            classInfo.Properties = new List<PropertyInfo>();
            classInfo.Interfaces = new List<string>();
            classInfo.TypeParameters = new Dictionary<string, List<string>>();
        }
        
        public void SetBaseClass(string baseClass)
        {
            classInfo.BaseClass = baseClass;
        }
        
        public void AddInterface(string interfaceName)
        {
            classInfo.Interfaces.Add(interfaceName);
        }
        
        public void AddTypeParameter(string name, List<string> constraints)
        {
            classInfo.TypeParameters[name] = constraints ?? new List<string>();
        }
        
        public void AddField(string name, string type, List<string> modifiers)
        {
            classInfo.Fields.Add(new FieldInfo
            {
                Name = name,
                Type = type,
                Modifiers = modifiers
            });
        }
        
        // Overload for AST.Modifier types
        public void AddField(string name, string type, List<AST.Modifier> modifiers)
        {
            AddField(name, type, modifiers?.Select(m => m.ToString()).ToList() ?? new List<string>());
        }
        
        public void AddProperty(string name, string type, int getterStart, int setterEnd, List<string> modifiers)
        {
            classInfo.Properties.Add(new PropertyInfo
            {
                Name = name,
                Type = type,
                GetterAddress = getterStart,
                SetterEndAddress = setterEnd,
                Modifiers = modifiers
            });
        }
        
        // Overload for AST.Modifier types
        public void AddProperty(string name, string type, int getterStart, int setterEnd, List<AST.Modifier> modifiers)
        {
            AddProperty(name, type, getterStart, setterEnd, modifiers?.Select(m => m.ToString()).ToList() ?? new List<string>());
        }
        
        public void AddMethod(string name, int startAddress, int endAddress, List<string> modifiers)
        {
            classInfo.Methods.Add(new MethodInfo
            {
                Name = name,
                StartAddress = startAddress,
                EndAddress = endAddress,
                Modifiers = modifiers
            });
        }
        
        // Overload for AST.Modifier types
        public void AddMethod(string name, int startAddress, int endAddress, List<AST.Modifier> modifiers)
        {
            AddMethod(name, startAddress, endAddress, modifiers?.Select(m => m.ToString()).ToList() ?? new List<string>());
        }
        
        public ClassInfo Build()
        {
            return classInfo;
        }
    }
    
    /// <summary>
    /// Builder for interface definitions
    /// </summary>
    public class InterfaceBuilder
    {
        private InterfaceInfo interfaceInfo;
        
        public InterfaceBuilder(string name)
        {
            interfaceInfo = new InterfaceInfo { Name = name };
            interfaceInfo.BaseInterfaces = new List<string>();
            interfaceInfo.Methods = new List<InterfaceMethodInfo>();
            interfaceInfo.Properties = new List<InterfacePropertyInfo>();
        }
        
        public void AddBaseInterface(string baseInterface)
        {
            interfaceInfo.BaseInterfaces.Add(baseInterface);
        }
        
        public void AddMethod(string name, AST.TypeNode returnType, List<AST.Parameter> parameters)
        {
            interfaceInfo.Methods.Add(new InterfaceMethodInfo
            {
                Name = name,
                ReturnType = returnType?.Name,
                Parameters = parameters.Select(p => new ParameterInfo { Name = p.Name, Type = p.Type?.Name }).ToList()
            });
        }
        
        public void AddProperty(string name, AST.TypeNode type, bool hasGetter, bool hasSetter)
        {
            interfaceInfo.Properties.Add(new InterfacePropertyInfo
            {
                Name = name,
                Type = type?.Name,
                HasGetter = hasGetter,
                HasSetter = hasSetter
            });
        }
        
        public InterfaceInfo Build()
        {
            return interfaceInfo;
        }
    }
    
    /// <summary>
    /// Builder for struct definitions
    /// </summary>
    public class StructBuilder
    {
        private StructInfo structInfo;
        
        public StructBuilder(string name)
        {
            structInfo = new StructInfo { Name = name };
            structInfo.Fields = new List<FieldInfo>();
            structInfo.Methods = new List<MethodInfo>();
            structInfo.Interfaces = new List<string>();
        }
        
        public void AddInterface(string interfaceName)
        {
            structInfo.Interfaces.Add(interfaceName);
        }
        
        public void AddField(string name, string type, List<string> modifiers)
        {
            structInfo.Fields.Add(new FieldInfo
            {
                Name = name,
                Type = type,
                Modifiers = modifiers
            });
        }
        
        // Overload for AST.Modifier types
        public void AddField(string name, string type, List<AST.Modifier> modifiers)
        {
            AddField(name, type, modifiers?.Select(m => m.ToString()).ToList() ?? new List<string>());
        }
        
        public void AddMethod(string name, int startAddress, int endAddress, List<string> modifiers)
        {
            structInfo.Methods.Add(new MethodInfo
            {
                Name = name,
                StartAddress = startAddress,
                EndAddress = endAddress,
                Modifiers = modifiers
            });
        }
        
        // Overload for AST.Modifier types
        public void AddMethod(string name, int startAddress, int endAddress, List<AST.Modifier> modifiers)
        {
            AddMethod(name, startAddress, endAddress, modifiers?.Select(m => m.ToString()).ToList() ?? new List<string>());
        }
        
        public StructInfo Build()
        {
            return structInfo;
        }
    }
    
    /// <summary>
    /// Builder for enum definitions
    /// </summary>
    public class EnumBuilder
    {
        private EnumInfo enumInfo;
        
        public EnumBuilder(string name, string underlyingType)
        {
            enumInfo = new EnumInfo
            {
                Name = name,
                UnderlyingType = underlyingType,
                Members = new List<EnumMemberInfo>()
            };
        }
        
        public void AddMember(string name, int value)
        {
            enumInfo.Members.Add(new EnumMemberInfo
            {
                Name = name,
                Value = value
            });
        }
        
        public EnumInfo Build()
        {
            return enumInfo;
        }
    }
    
    /// <summary>
    /// Builder for component definitions
    /// </summary>
    public class ComponentBuilder
    {
        private ComponentInfo componentInfo;
        
        public ComponentBuilder(string name)
        {
            componentInfo = new ComponentInfo
            {
                Name = name,
                Fields = new List<ComponentFieldInfo>()
            };
        }
        
        public void AddField(string name, string type)
        {
            componentInfo.Fields.Add(new ComponentFieldInfo
            {
                Name = name,
                Type = type
            });
        }
        
        public ComponentInfo Build()
        {
            return componentInfo;
        }
    }
    
    /// <summary>
    /// Builder for system definitions
    /// </summary>
    public class SystemBuilder
    {
        private SystemInfo systemInfo;
        
        public SystemBuilder(string name)
        {
            systemInfo = new SystemInfo
            {
                Name = name,
                RequiredComponents = new List<string>()
            };
        }
        
        public void RequireComponent(string componentName)
        {
            systemInfo.RequiredComponents.Add(componentName);
        }
        
        public SystemInfo Build()
        {
            return systemInfo;
        }
    }
    
    /// <summary>
    /// Builder for entity archetype definitions
    /// </summary>
    public class EntityBuilder
    {
        private EntityInfo entityInfo;
        
        public EntityBuilder(string name)
        {
            entityInfo = new EntityInfo
            {
                Name = name,
                Components = new List<string>()
            };
        }
        
        public void AddComponent(string componentName)
        {
            entityInfo.Components.Add(componentName);
        }
        
        public EntityInfo Build()
        {
            return entityInfo;
        }
    }
} 
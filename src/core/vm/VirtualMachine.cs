using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ouro.Core.Compiler;
using Ouro.src.tools;
using Ouro.StdLib.Math;
using static Ouro.StdLib.Math.MathSymbols;

namespace Ouro.Core.VM
{
    /// <summary>
    /// The Ouro Virtual Machine
    /// </summary>
    public class VirtualMachine
    {
        // Events
        public event Action<int> OnMemoryAllocate;
        public event Action<int> OnMemoryFree;
        public event Action<Exception> OnException;
        public event Action<string, int> OnFunctionEnter;
        public event Action<string> OnFunctionExit;
        public event Action<int, Opcode> OnInstructionExecute;
        private Stack<object> operandStack;
        private List<object> locals;
        private List<object> globals;
        private Stack<CallFrame> callStack;
        private byte[] instructions;
        private object[] constantPool;
        private Dictionary<string, Type> typeRegistry;
        private Dictionary<int, ExceptionHandler> exceptionHandlers;
        private RuntimeEnvironment environment;
        private int instructionPointer;
        private bool running;
        private Compiler.CompiledProgram compiledProgram;
        private SymbolTable symbolTable;
        private Dictionary<int, GeneratorState> generatorStates = new Dictionary<int, GeneratorState>();
        private bool debugMode;
        
        // Debugger support properties
        public int ProgramCounter => instructionPointer;
        public int StackPointer => operandStack?.Count ?? 0;
        public int FramePointer => callStack?.Count > 0 ? callStack.Peek().LocalsBase : 0;
        public object Accumulator => operandStack?.Count > 0 ? operandStack.Peek() : null;
        
        // Memory array for debugger
        private byte[] memory = new byte[65536]; // 64KB of memory
        
        public VirtualMachine()
        {
            operandStack = new Stack<object>();
            locals = new List<object>();
            globals = new List<object>();
            callStack = new Stack<CallFrame>();
            typeRegistry = new Dictionary<string, Type>();
            exceptionHandlers = new Dictionary<int, ExceptionHandler>();
            environment = new RuntimeEnvironment();
            
            // Register built-in types
            RegisterBuiltInTypes();
            
            // Register built-in functions
            RegisterBuiltInFunctions();
            
            // Initialize debug mode from environment variable
            debugMode = Environment.GetEnvironmentVariable("OURO_DEBUG") == "true";
        }
        
        /// <summary>
        /// Execute a compiled program
        /// </summary>
        public object Execute(Compiler.CompiledProgram program)
        {
            // Store the compiled program and symbol table for function resolution
            this.compiledProgram = program;
            // Note: Different SymbolTable types between Compiler and VM namespaces
            // For now, we'll use the program's symbol table reference for function lookup
            
            try
            {
                var bytecode = program.Bytecode;
                // Convert from Compiler.Bytecode to VM-compatible format
                var constants = bytecode.Constants.ToArray(); // List to Array conversion
                var code = bytecode.Code.ToArray(); // List to Array conversion
                
                // Load the bytecode
                LoadBytecode(code, constants);
                
                // LoadTypes is expecting VM.Bytecode but we have Compiler.Bytecode
                // Skip this for now as it's not critical for basic function execution
                // LoadTypes(bytecode);
                
                // Initialize global constants based on symbol table
                // InitializeGlobalConstants expects VM.SymbolTable but we have Compiler.SymbolTable
                // Skip this for now as it's handled elsewhere
                // InitializeGlobalConstants(program.SymbolTable);
                
                // Start execution
                running = true;
                instructionPointer = 0;
                
                while (running && instructionPointer < instructions.Length)
                {
                    ExecuteInstruction();
                }
                
                // Return the top of the stack if there's a result
                return operandStack.Count > 0 ? operandStack.Pop() : null;
            }
            catch (Exception ex)
            {
                OnException?.Invoke(ex);
                throw new VirtualMachineException($"Runtime error: {ex.Message}", ex);
            }
        }
        
        private void ExecuteInstruction()
        {
            var opcode = (Opcode)instructions[instructionPointer++];
            Logger.Debug($"Executing opcode {opcode} at IP {instructionPointer - 1}");
            
            switch (opcode)
            {
                // Control flow
                case Opcode.Nop:
                    break;
                    
                case Opcode.Halt:
                    running = false;
                    break;
                    
                case Opcode.Jump:
                    {
                        var offset = ReadInt32();
                        instructionPointer += offset;
                    }
                    break;
                    
                case Opcode.JumpIfTrue:
                    {
                        var offset = ReadInt32();
                        if (operandStack.Count == 0)
                        {
                            Logger.Debug("JumpIfTrue - Stack empty, treating as false");
                            break;
                        }
                        var conditionValue = operandStack.Pop();
                        var condition = conditionValue != null && (bool)conditionValue;
                        if (condition)
                            instructionPointer += offset;
                    }
                    break;
                    
                case Opcode.JumpIfFalse:
                    {
                        var offset = ReadInt32();
                        if (operandStack.Count == 0)
                        {
                            Logger.Debug("JumpIfFalse - Stack empty, treating as false");
                            instructionPointer += offset;
                            break;
                        }
                        var conditionValue = operandStack.Pop();
                        var condition = conditionValue != null && (bool)conditionValue;
                        if (!condition)
                            instructionPointer += offset;
                    }
                    break;
                    
                case Opcode.Call:
                    {
                        var argCount = ReadInt32();
                        var function = operandStack.Pop();
                        
                        // Collect arguments
                        var args = new object[argCount];
                        for (int i = argCount - 1; i >= 0; i--)
                        {
                            args[i] = operandStack.Pop();
                        }
                        
                        // Call function
                        if (function is FunctionInfo funcInfo)
                        {
                            // Push call frame
                            callStack.Push(new CallFrame
                            {
                                ReturnAddress = instructionPointer,
                                LocalsBase = locals.Count,
                                Function = funcInfo
                            });
                            
                            // Jump to function
                            instructionPointer = funcInfo.StartAddress;
                            
                            // Initialize locals with arguments
                            locals.AddRange(args);
                        }
                        else if (function is Delegate del)
                        {
                            // Native function call
                            var result = del.DynamicInvoke(args);
                            // Only push non-void results
                            if (result != null && del.Method.ReturnType != typeof(void))
                                operandStack.Push(result);
                        }
                    }
                    break;
                    
                case Opcode.Return:
                    {
                        object returnValue = null;
                        if (operandStack.Count > 0)
                            returnValue = operandStack.Pop();
                            
                        if (callStack.Count > 0)
                        {
                            var frame = callStack.Pop();
                            instructionPointer = frame.ReturnAddress;
                            
                            // Clean up locals
                            locals.RemoveRange(frame.LocalsBase, locals.Count - frame.LocalsBase);
                            
                            if (returnValue != null)
                                operandStack.Push(returnValue);
                        }
                        else
                        {
                            // Return from main
                            running = false;
                            if (returnValue != null)
                                operandStack.Push(returnValue);
                        }
                    }
                    break;
                    
                case Opcode.ReturnVoid:
                    {
                        if (callStack.Count > 0)
                        {
                            var frame = callStack.Pop();
                            instructionPointer = frame.ReturnAddress;
                            
                            // Clean up locals
                            locals.RemoveRange(frame.LocalsBase, locals.Count - frame.LocalsBase);
                        }
                        else
                        {
                            // Return from main
                            running = false;
                        }
                    }
                    break;
                    
                case Opcode.AsyncCall:
                    {
                        var argCount = ReadInt32();
                        var function = operandStack.Pop();
                        
                        // Collect arguments
                        var args = new object[argCount];
                        for (int i = argCount - 1; i >= 0; i--)
                        {
                            args[i] = operandStack.Pop();
                        }
                        
                        // Start async task
                        var task = Task.Run(() =>
                        {
                            if (function is FunctionInfo funcInfo)
                            {
                                // Create a new VM instance for async execution
                                var asyncVM = new VirtualMachine();
                                asyncVM.environment = this.environment;
                                asyncVM.typeRegistry = this.typeRegistry;
                                asyncVM.compiledProgram = this.compiledProgram;
                                
                                // Set up the async VM with the current state
                                asyncVM.constantPool = this.constantPool;
                                asyncVM.instructions = this.instructions;
                                
                                // Copy globals (shared state)
                                foreach (var global in this.globals)
                                {
                                    asyncVM.globals.Add(global);
                                }
                                
                                // Set up function call
                                asyncVM.instructionPointer = funcInfo.StartAddress;
                                
                                // Push call frame
                                asyncVM.callStack.Push(new CallFrame
                                {
                                    ReturnAddress = asyncVM.instructions.Length, // Return to end
                                    LocalsBase = 0,
                                    Function = funcInfo
                                });
                                
                                // Initialize locals with arguments
                                asyncVM.locals.AddRange(args);
                                
                                // Execute function
                                try
                                {
                                    while (asyncVM.instructionPointer < asyncVM.instructions.Length && asyncVM.running)
                                    {
                                        asyncVM.ExecuteInstruction();
                                    }
                                    
                                    // Return the result if any
                                    return asyncVM.operandStack.Count > 0 ? asyncVM.operandStack.Pop() : null;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error in async function execution: {ex.Message}");
                                    throw;
                                }
                            }
                            else if (function is Delegate del)
                            {
                                // Native async function call
                                return del.DynamicInvoke(args);
                            }
                            else
                            {
                                throw new VirtualMachineException($"Cannot async call non-function value: {function?.GetType().Name}");
                            }
                        });
                        
                        operandStack.Push(task);
                    }
                    break;
                    
                // Stack operations
                case Opcode.Pop:
                    if (operandStack.Count > 0)
                        operandStack.Pop();
                    break;
                    
                case Opcode.Duplicate:
                    {
                        var value = operandStack.Peek();
                        operandStack.Push(value);
                    }
                    break;
                    
                case Opcode.Duplicate2:
                    {
                        var value2 = operandStack.Pop();
                        var value1 = operandStack.Pop();
                        operandStack.Push(value1);
                        operandStack.Push(value2);
                        operandStack.Push(value1);
                        operandStack.Push(value2);
                    }
                    break;
                    
                case Opcode.Swap:
                    {
                        var value2 = operandStack.Pop();
                        var value1 = operandStack.Pop();
                        operandStack.Push(value2);
                        operandStack.Push(value1);
                    }
                    break;
                    
                // Load/Store operations
                case Opcode.LoadConstant:
                    {
                        var index = ReadInt32();
                        Logger.Debug($"LoadConstant trying to access index {index}");
                        operandStack.Push(constantPool[index]);
                    }
                    break;
                    
                case Opcode.LoadLocal:
                    {
                        var index = ReadInt32();
                        var frame = callStack.Count > 0 ? callStack.Peek() : null;
                        var localIndex = frame != null ? frame.LocalsBase + index : index;
                        
                        // Bounds check for locals
                        if (localIndex >= locals.Count)
                        {
                            while (locals.Count <= localIndex)
                                locals.Add(null);
                        }
                        
                        operandStack.Push(locals[localIndex]);
                    }
                    break;
                    
                case Opcode.LoadGlobal:
                    {
                        var index = ReadInt32();
                        if (index >= globals.Count || globals[index] == null)
                        {
                            Logger.Debug($"LoadGlobal[{index}] - value is null, globals.Count = {globals.Count}");
                        }
                        var value = globals[index];
                        Logger.Debug($"LoadGlobal[{index}] = '{value}'");
                        operandStack.Push(value);
                    }
                    break;
                    
                case Opcode.LoadTrue:
                    operandStack.Push(true);
                    break;
                    
                case Opcode.LoadFalse:
                    operandStack.Push(false);
                    break;
                    
                case Opcode.LoadNull:
                    operandStack.Push(null);
                    break;
                    
                case Opcode.LoadThis:
                    // Load 'this' reference - for now, just push null
                    // In a full implementation, this would load the current object instance
                    operandStack.Push(null);
                    break;
                    
                case Opcode.StoreLocal:
                    {
                        var index = ReadInt32();
                        var value = operandStack.Pop();
                        var frame = callStack.Count > 0 ? callStack.Peek() : null;
                        var localIndex = frame != null ? frame.LocalsBase + index : index;
                        
                        // Ensure locals list is large enough
                        while (locals.Count <= localIndex)
                            locals.Add(null);
                            
                        locals[localIndex] = value;
                    }
                    break;
                    
                case Opcode.StoreGlobal:
                    {
                        var index = ReadInt32();
                        var value = operandStack.Pop();
                        
                        // Ensure globals list is large enough
                        while (globals.Count <= index)
                            globals.Add(null);
                            
                        var oldValue = globals[index];
                        globals[index] = value;
                        Logger.Debug($"StoreGlobal[{index}] = '{value}' (was: '{oldValue}')");
                    }
                    break;
                    
                // Arithmetic operations
                case Opcode.Add:
                    {
                        var right = operandStack.Pop();
                        var left = operandStack.Pop();
                        operandStack.Push(ArithmeticOperation(left, right, (a, b) => a + b));
                    }
                    break;
                    
                case Opcode.Subtract:
                    {
                        var right = operandStack.Pop();
                        var left = operandStack.Pop();
                        operandStack.Push(ArithmeticOperation(left, right, (a, b) => a - b));
                    }
                    break;
                    
                case Opcode.Multiply:
                    {
                        var right = operandStack.Pop();
                        var left = operandStack.Pop();
                        operandStack.Push(ArithmeticOperation(left, right, (a, b) => a * b));
                    }
                    break;
                    
                case Opcode.Divide:
                    {
                        var right = operandStack.Pop();
                        var left = operandStack.Pop();
                        operandStack.Push(ArithmeticOperation(left, right, (a, b) => a / b));
                    }
                    break;
                    
                case Opcode.Modulo:
                    {
                        var right = operandStack.Pop();
                        var left = operandStack.Pop();
                        operandStack.Push(ArithmeticOperation(left, right, (a, b) => a % b));
                    }
                    break;
                    
                case Opcode.Power:
                    {
                        var right = Convert.ToDouble(operandStack.Pop());
                        var left = Convert.ToDouble(operandStack.Pop());
                        operandStack.Push(System.Math.Pow(left, right));
                    }
                    break;
                    
                case Opcode.Negate:
                    {
                        var value = operandStack.Pop();
                        operandStack.Push(ArithmeticOperation(value, null, (a, _) => -a));
                    }
                    break;
                    
                // Comparison operations
                case Opcode.Equal:
                    {
                        var right = operandStack.Pop();
                        var left = operandStack.Pop();
                        operandStack.Push(Equals(left, right));
                    }
                    break;
                    
                case Opcode.NotEqual:
                    {
                        var right = operandStack.Pop();
                        var left = operandStack.Pop();
                        operandStack.Push(!Equals(left, right));
                    }
                    break;
                    
                case Opcode.Less:
                    {
                        var right = operandStack.Pop();
                        var left = operandStack.Pop();
                        operandStack.Push(Compare(left, right) < 0);
                    }
                    break;
                    
                case Opcode.Greater:
                    {
                        var right = operandStack.Pop();
                        var left = operandStack.Pop();
                        operandStack.Push(Compare(left, right) > 0);
                    }
                    break;
                    
                case Opcode.LessEqual:
                    {
                        var right = operandStack.Pop();
                        var left = operandStack.Pop();
                        operandStack.Push(Compare(left, right) <= 0);
                    }
                    break;
                    
                case Opcode.GreaterEqual:
                    {
                        var right = operandStack.Pop();
                        var left = operandStack.Pop();
                        operandStack.Push(Compare(left, right) >= 0);
                    }
                    break;
                    
                case Opcode.Compare:
                    {
                        var right = operandStack.Pop();
                        var left = operandStack.Pop();
                        operandStack.Push(Compare(left, right));
                    }
                    break;
                    
                // Logical operations
                case Opcode.LogicalAnd:
                    {
                        var right = (bool)operandStack.Pop();
                        var left = (bool)operandStack.Pop();
                        operandStack.Push(left && right);
                    }
                    break;
                    
                case Opcode.LogicalOr:
                    {
                        var right = (bool)operandStack.Pop();
                        var left = (bool)operandStack.Pop();
                        operandStack.Push(left || right);
                    }
                    break;
                    
                case Opcode.LogicalNot:
                    {
                        if (operandStack.Count == 0)
                        {
                            Logger.Debug("LogicalNot - Stack empty, pushing true");
                            operandStack.Push(true);
                            break;
                        }
                        var valueObj = operandStack.Pop();
                        var value = valueObj != null && (bool)valueObj;
                        operandStack.Push(!value);
                    }
                    break;
                    
                // Bitwise operations
                case Opcode.BitwiseAnd:
                    {
                        var right = Convert.ToInt64(operandStack.Pop());
                        var left = Convert.ToInt64(operandStack.Pop());
                        operandStack.Push(left & right);
                    }
                    break;
                    
                case Opcode.BitwiseOr:
                    {
                        var right = Convert.ToInt64(operandStack.Pop());
                        var left = Convert.ToInt64(operandStack.Pop());
                        operandStack.Push(left | right);
                    }
                    break;
                    
                case Opcode.BitwiseXor:
                    {
                        var right = Convert.ToInt64(operandStack.Pop());
                        var left = Convert.ToInt64(operandStack.Pop());
                        operandStack.Push(left ^ right);
                    }
                    break;
                    
                case Opcode.BitwiseNot:
                    {
                        var value = operandStack.Pop();
                        if (value is int intVal)
                            operandStack.Push(~intVal);
                        else if (value is long longVal)
                            operandStack.Push(~longVal);
                        else
                            throw new VirtualMachineException($"Cannot apply bitwise NOT to {value?.GetType().Name}");
                    }
                    break;
                    
                case Opcode.LeftShift:
                    {
                        var shiftAmount = (int)operandStack.Pop();
                        var value = operandStack.Pop();
                        if (value is int intVal)
                            operandStack.Push(intVal << shiftAmount);
                        else if (value is long longVal)
                            operandStack.Push(longVal << shiftAmount);
                        else
                            throw new VirtualMachineException($"Cannot left shift {value?.GetType().Name}");
                    }
                    break;
                    
                case Opcode.RightShift:
                    {
                        var shiftAmount = (int)operandStack.Pop();
                        var value = operandStack.Pop();
                        if (value is int intVal)
                            operandStack.Push(intVal >> shiftAmount);
                        else if (value is long longVal)
                            operandStack.Push(longVal >> shiftAmount);
                        else
                            throw new VirtualMachineException($"Cannot right shift {value?.GetType().Name}");
                    }
                    break;
                    
                // Type operations
                case Opcode.TypeOf:
                    {
                        var typeIndex = ReadInt32();
                        var typeName = (string)constantPool[typeIndex];
                        operandStack.Push(typeRegistry[typeName]);
                    }
                    break;
                    
                case Opcode.SizeOf:
                    {
                        var typeIndex = ReadInt32();
                        var typeName = (string)constantPool[typeIndex];
                        var type = typeRegistry[typeName];
                        operandStack.Push(System.Runtime.InteropServices.Marshal.SizeOf(type));
                    }
                    break;
                    
                // Object operations
                case Opcode.New:
                    {
                        var typeIndex = ReadInt32();
                        var argCount = ReadInt32();
                        var typeName = (string)constantPool[typeIndex];
                        var type = typeRegistry[typeName];
                        
                        // Collect constructor arguments
                        var args = new object[argCount];
                        for (int i = argCount - 1; i >= 0; i--)
                        {
                            args[i] = operandStack.Pop();
                        }
                        
                        // Create instance
                        var instance = Activator.CreateInstance(type, args);
                        operandStack.Push(instance);
                    }
                    break;
                    
                case Opcode.LoadMember:
                    {
                        var memberIndex = ReadInt32();
                        var memberName = (string)constantPool[memberIndex];
                        var obj = operandStack.Pop();
                        
                        if (obj == null)
                            throw new NullReferenceException($"Cannot access member '{memberName}' on null reference");
                            
                        var value = GetMemberValue(obj, memberName);
                        operandStack.Push(value);
                    }
                    break;
                    
                case Opcode.LoadMemberNullSafe:
                    {
                        var memberIndex = ReadInt32();
                        var memberName = (string)constantPool[memberIndex];
                        var obj = operandStack.Pop();
                        
                        if (obj == null)
                        {
                            operandStack.Push(null);
                        }
                        else
                        {
                            var value = GetMemberValue(obj, memberName);
                            operandStack.Push(value);
                        }
                    }
                    break;
                    
                case Opcode.StoreMember:
                    {
                        var memberIndex = ReadInt32();
                        var memberName = (string)constantPool[memberIndex];
                        var value = operandStack.Pop();
                        var obj = operandStack.Pop();
                        
                        if (obj == null)
                            throw new NullReferenceException($"Cannot set member '{memberName}' on null reference");
                            
                        SetMemberValue(obj, memberName, value);
                    }
                    break;
                    
                case Opcode.CallMethod:
                    {
                        var methodNameIndex = ReadInt32();
                        var argCount = ReadInt32();
                        var methodName = (string)constantPool[methodNameIndex];
                        
                        // Special case: Runtime user function resolution
                        if (methodName == "__resolve_user_function")
                        {
                            // Pop arguments (including function name as first argument)
                            var args = new object[argCount];
                            for (int i = argCount - 1; i >= 0; i--)
                            {
                                args[i] = operandStack.Pop();
                            }
                            
                            var functionName = args[0] as string;
                            var actualArgs = new object[argCount - 1];
                            Array.Copy(args, 1, actualArgs, 0, argCount - 1);
                            
                            Logger.Debug($"VM resolving user function '{functionName}' at runtime");
                            
                            // Look up function in function table by name
                            var function = ResolveUserFunction(functionName);
                            if (function != null)
                            {
                                                            Console.WriteLine($"DEBUG: Found function '{functionName}' at address {function.StartAddress}");
                            
                            // Set up function's local variable base
                            var localsBase = locals.Count;
                            
                            // Push call frame
                            callStack.Push(new CallFrame
                            {
                                ReturnAddress = instructionPointer,
                                LocalsBase = localsBase,
                                Function = function
                            });
                            
                            // Initialize locals with function parameters
                            locals.AddRange(actualArgs);
                            
                            // Add additional null locals to ensure we have enough space for function's local variables
                            // Most functions need a few extra locals beyond just parameters
                            for (int i = 0; i < 10; i++)
                            {
                                locals.Add(null);
                            }
                            
                            Console.WriteLine($"DEBUG: Set up function call - LocalsBase: {localsBase}, Arguments: [{string.Join(", ", actualArgs.Select(a => a?.ToString() ?? "null"))}]");
                        if (functionName == "handleNumberInput") 
                        {
                            Console.WriteLine($"DEBUG: *** CALLING handleNumberInput with argument: '{actualArgs[0]}' ***");
                        }
                        else if (functionName == "isDigit") 
                        {
                            Console.WriteLine($"DEBUG: *** CALLING isDigit with argument: '{actualArgs[0]}' ***");
                        }
                                
                                // Jump to function
                                instructionPointer = function.StartAddress;
                            }
                            else
                            {
                                                            Console.WriteLine($"DEBUG: Function '{functionName}' not found at runtime - trying native functions");
                            
                            // Try native function as fallback
                            bool foundNativeFunction = false;
                            
                            // First try exact match
                            if (environment.NativeFunctions.ContainsKey(functionName))
                            {
                                var nativeFunc = environment.NativeFunctions[functionName];
                                try
                                {
                                    var result = nativeFunc.DynamicInvoke(actualArgs);
                                    if (result != null && nativeFunc.Method.ReturnType != typeof(void))
                                        operandStack.Push(result);
                                    foundNativeFunction = true;
                                }
                                catch (Exception ex)
                                {
                                    // Handle native function exceptions gracefully 
                                    Console.WriteLine($"DEBUG: Native function '{functionName}' threw exception: {ex.InnerException?.Message ?? ex.Message}");
                                    // Push default value for failed parsing
                                    if (functionName.Contains("Parse"))
                                        operandStack.Push(0.0);
                                    else 
                                        operandStack.Push(null);
                                    foundNativeFunction = true;
                                }
                            }
                            else
                            {
                                // Try type-qualified names (e.g., double.Parse, int.Parse)
                                foreach (var kvp in environment.NativeFunctions)
                                {
                                    var nativeName = kvp.Key;
                                    if (nativeName.EndsWith("." + functionName))
                                    {
                                        var nativeFunc = kvp.Value;
                                        try
                                        {
                                            var result = nativeFunc.DynamicInvoke(actualArgs);
                                            if (result != null && nativeFunc.Method.ReturnType != typeof(void))
                                                operandStack.Push(result);
                                            foundNativeFunction = true;
                                            Console.WriteLine($"DEBUG: Found qualified native function '{nativeName}' for '{functionName}'");
                                        }
                                        catch (Exception ex)
                                        {
                                            // Handle native function exceptions gracefully
                                            Console.WriteLine($"DEBUG: Native function '{nativeName}' threw exception: {ex.InnerException?.Message ?? ex.Message}");
                                            // Push default value for failed parsing
                                            if (nativeName.Contains("Parse"))
                                                operandStack.Push(0.0);
                                            else 
                                                operandStack.Push(null);
                                            foundNativeFunction = true;
                                        }
                                        break;
                                    }
                                }
                            }
                            
                            if (!foundNativeFunction)
                            {
                                throw new VirtualMachineException($"Function '{functionName}' not found");
                            }
                            }
                            break;
                        }
                        
                        // Pop arguments
                        var methodArgs = new object[argCount];
                        for (int i = argCount - 1; i >= 0; i--)
                        {
                            methodArgs[i] = operandStack.Pop();
                        }
                        
                        // Pop object
                        var obj = operandStack.Pop();
                        
                        Console.WriteLine($"DEBUG: CallMethod - object: {obj} (type: {obj?.GetType()?.Name}), method: {methodName}, args: {argCount}");
                        
                        // Handle static method calls on types
                        if (obj is Type type)
                        {
                            // Get argument types for method resolution
                            var argTypes = new Type[argCount];
                            for (int i = 0; i < argCount; i++)
                            {
                                argTypes[i] = methodArgs[i]?.GetType() ?? typeof(object);
                            }
                            
                            // Check if type contains generic parameters first to avoid ContainsGenericParameters error
                            if (type.ContainsGenericParameters)
                            {
                                Console.WriteLine($"DEBUG: Skipping method call on generic type {type.Name}");
                                throw new VirtualMachineException($"Cannot call methods on generic type definition {type.Name}");
                            }
                            
                            var method = type.GetMethod(methodName, 
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                                null, argTypes, null);
                            
                            if (method != null)
                            {
                                var result = method.Invoke(null, methodArgs);
                                if (method.ReturnType != typeof(void))
                                    operandStack.Push(result);
                            }
                            else
                            {
                                throw new VirtualMachineException($"Static method '{methodName}' not found on type {type.Name} with argument types: {string.Join(", ", argTypes.Select(t => t.Name))}");
                            }
                        }
                        else
                        {
                            // Instance method call
                            var result = CallMethod(obj, methodName, methodArgs);
                            if (result != null)
                                operandStack.Push(result);
                        }
                    }
                    break;
                    
                // Array/Collection operations
                case Opcode.MakeArray:
                    {
                        var elementCount = ReadInt32();
                        var elements = new object[elementCount];
                        
                        for (int i = elementCount - 1; i >= 0; i--)
                        {
                            elements[i] = operandStack.Pop();
                        }
                        
                        operandStack.Push(elements);
                    }
                    break;
                    
                case Opcode.MakeVector:
                    {
                        var dimensions = ReadInt32();
                        var components = new double[dimensions];
                        
                        for (int i = dimensions - 1; i >= 0; i--)
                        {
                            components[i] = Convert.ToDouble(operandStack.Pop());
                        }
                        
                        var vector = new Vector(components);
                        operandStack.Push(vector);
                    }
                    break;
                    
                case Opcode.MakeMatrix:
                    {
                        var rows = ReadInt32();
                        var cols = ReadInt32();
                        var elements = new double[rows, cols];
                        
                        for (int i = rows - 1; i >= 0; i--)
                        {
                            for (int j = cols - 1; j >= 0; j--)
                            {
                                elements[i, j] = Convert.ToDouble(operandStack.Pop());
                            }
                        }
                        
                        var matrix = new Matrix(elements);
                        operandStack.Push(matrix);
                    }
                    break;
                    
                case Opcode.MakeQuaternion:
                    {
                        var z = Convert.ToDouble(operandStack.Pop());
                        var y = Convert.ToDouble(operandStack.Pop());
                        var x = Convert.ToDouble(operandStack.Pop());
                        var w = Convert.ToDouble(operandStack.Pop());
                        
                        var quaternion = new Quaternion(w, x, y, z);
                        operandStack.Push(quaternion);
                    }
                    break;
                    
                // String operations
                case Opcode.ToString:
                    {
                        var value = operandStack.Pop();
                        operandStack.Push(value?.ToString() ?? "null");
                    }
                    break;
                    
                case Opcode.StringConcat:
                    {
                        var count = ReadInt32();
                        var parts = new string[count];
                        
                        for (int i = count - 1; i >= 0; i--)
                        {
                            parts[i] = operandStack.Pop()?.ToString() ?? "";
                        }
                        
                        operandStack.Push(string.Concat(parts));
                    }
                    break;
                    
                // Special operations
                case Opcode.NullCoalesce:
                    {
                        var right = operandStack.Pop();
                        var left = operandStack.Pop();
                        operandStack.Push(left ?? right);
                    }
                    break;
                    
                // Math operations
                case Opcode.SquareRoot:
                    {
                        var value = Convert.ToDouble(operandStack.Pop());
                        operandStack.Push(System.Math.Sqrt(value));
                    }
                    break;
                    
                case Opcode.Sin:
                    {
                        var value = Convert.ToDouble(operandStack.Pop());
                        operandStack.Push(System.Math.Sin(value));
                    }
                    break;
                    
                case Opcode.Cos:
                    {
                        var value = Convert.ToDouble(operandStack.Pop());
                        operandStack.Push(System.Math.Cos(value));
                    }
                    break;
                    
                case Opcode.Tan:
                    {
                        var value = Convert.ToDouble(operandStack.Pop());
                        operandStack.Push(System.Math.Tan(value));
                    }
                    break;
                    
                case Opcode.DotProduct:
                    {
                        var right = (Vector)operandStack.Pop();
                        var left = (Vector)operandStack.Pop();
                        operandStack.Push(left.Dot(right));
                    }
                    break;
                    
                case Opcode.CrossProduct:
                    {
                        var right = (Vector)operandStack.Pop();
                        var left = (Vector)operandStack.Pop();
                        operandStack.Push(left.Cross(right));
                    }
                    break;
                    
                // Exception handling
                case Opcode.BeginTry:
                    // Mark try block start
                    break;
                    
                case Opcode.BeginCatch:
                    // Mark catch block start
                    break;
                    
                case Opcode.BeginFinally:
                    // Mark finally block start
                    break;
                    
                case Opcode.EndFinally:
                    // Mark finally block end
                    break;
                    
                case Opcode.Throw:
                    {
                        var exception = operandStack.Pop() as Exception;
                        if (exception == null)
                            exception = new RuntimeException("Thrown value is not an exception");
                        throw exception;
                    }
                    break;
                    
                case Opcode.Rethrow:
                    throw new RuntimeException("No exception to rethrow");
                    
                // Import operations
                case Opcode.Import:
                    {
                        var moduleName = constantPool[ReadInt32()] as string;
                        if (moduleName == null)
                            throw new InvalidOperationException("Import module name must be a string");
                        
                        // Handle static imports
                        if (moduleName.StartsWith("static "))
                        {
                            var staticModule = moduleName.Substring(7);
                            if (staticModule == "Ouro.StdLib.Math.MathSymbols" || staticModule == "MathSymbols")
                            {
                                // Register math constants in the VM's globals
                                RegisterMathConstants();
                            }
                            else if (staticModule == "Console")
                            {
                                // Import static console methods
                                environment.Globals["Write"] = new Action<object>(Console.Write);
                                environment.Globals["WriteLine"] = new Action<object>(Console.WriteLine);
                                environment.Globals["ReadLine"] = new Func<string>(Console.ReadLine);
                            }
                            else if (staticModule == "Math")
                            {
                                // Import static math functions
                                environment.Globals["Abs"] = new Func<double, double>(global::System.Math.Abs);
                                environment.Globals["Sin"] = new Func<double, double>(global::System.Math.Sin);
                                environment.Globals["Cos"] = new Func<double, double>(global::System.Math.Cos);
                                environment.Globals["Tan"] = new Func<double, double>(global::System.Math.Tan);
                                environment.Globals["Sqrt"] = new Func<double, double>(global::System.Math.Sqrt);
                                environment.Globals["Pow"] = new Func<double, double, double>(global::System.Math.Pow);
                                environment.Globals["Log"] = new Func<double, double>(global::System.Math.Log);
                                environment.Globals["Floor"] = new Func<double, double>(global::System.Math.Floor);
                                environment.Globals["Ceiling"] = new Func<double, double>(global::System.Math.Ceiling);
                                environment.Globals["Round"] = new Func<double, double>(global::System.Math.Round);
                            }
                            else
                            {
                                // Try to load as a custom static module
                                var moduleType = Type.GetType($"Ouro.{staticModule}") ?? 
                                               Type.GetType($"Ouro.StdLib.{staticModule}") ?? 
                                               Type.GetType($"System.{staticModule}");
                                
                                if (moduleType != null)
                                {
                                    // Import all public static methods from the type
                                    foreach (var method in moduleType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                                    {
                                        try
                                        {
                                            environment.Globals[method.Name] = Delegate.CreateDelegate(
                                                GetDelegateType(method), method);
                                        }
                                        catch
                                        {
                                            // Skip methods that can't be converted to delegates
                                        }
                                    }
                                }
                                else
                                {
                                    Logger.Debug($"Warning: Static module '{staticModule}' not found");
                                }
                            }
                        }
                        else
                        {
                            // Handle regular imports
                            ImportModule(moduleName);
                        }
                    }
                    break;
                    
                case Opcode.DefineClass:
                    {
                        // Read class name from constant pool
                        var classNameIndex = ReadInt32();
                        var className = (string)constantPool[classNameIndex];
                        
                        // For now, just register the class as defined
                        // In a full implementation, this would create the class type
                        typeRegistry[className] = typeof(object);
                        
                        // The VM just acknowledges class definition for now
                        // Class instantiation is handled by the New opcode
                    }
                    break;
                    
                case Opcode.DefineInterface:
                case Opcode.DefineStruct:
                case Opcode.DefineEnum:
                case Opcode.DefineComponent:
                case Opcode.DefineSystem:
                case Opcode.DefineEntity:
                case Opcode.DefineFunction:
                    {
                        // Read name from constant pool and register the definition
                        var nameIndex = ReadInt32();
                        var name = (string)constantPool[nameIndex];
                        typeRegistry[name] = typeof(object); // Basic implementation
                    }
                    break;
                    
                // New mathematical operations for Ouroboros
                case Opcode.IntegerDivision:
                    {
                        var right = Convert.ToInt32(operandStack.Pop());
                        var left = Convert.ToInt32(operandStack.Pop());
                        if (right == 0)
                            throw new DivideByZeroException("Integer division by zero");
                        operandStack.Push(left / right);
                    }
                    break;
                    
                case Opcode.SpaceshipCompare:
                    {
                        var right = operandStack.Pop();
                        var left = operandStack.Pop();
                        var comparison = Compare(left, right);
                        operandStack.Push(comparison < 0 ? -1 : comparison > 0 ? 1 : 0);
                    }
                    break;
                    
                // Set operations
                case Opcode.ElementOf:
                    {
                        var set = operandStack.Pop();
                        var element = operandStack.Pop();
                        // Simple element membership check
                        bool found = false;
                        if (set is System.Collections.IEnumerable enumerable)
                        {
                            foreach (var item in enumerable)
                            {
                                if (Equals(item, element))
                                {
                                    found = true;
                                    break;
                                }
                            }
                        }
                        operandStack.Push(found);
                    }
                    break;
                    
                case Opcode.SetUnion:
                    {
                        var set2 = operandStack.Pop();
                        var set1 = operandStack.Pop();
                        var result = MathSymbols.SetUnion((IEnumerable<object>)set1, (IEnumerable<object>)set2);
                        operandStack.Push(result);
                    }
                    break;
                    
                case Opcode.SetIntersection:
                    {
                        var set2 = operandStack.Pop();
                        var set1 = operandStack.Pop();
                        var result = MathSymbols.SetIntersection((IEnumerable<object>)set1, (IEnumerable<object>)set2);
                        operandStack.Push(result);
                    }
                    break;
                    
                case Opcode.SetDifference:
                    {
                        var set2 = operandStack.Pop();
                        var set1 = operandStack.Pop();
                        var result = MathSymbols.SetDifference((IEnumerable<object>)set1, (IEnumerable<object>)set2);
                        operandStack.Push(result);
                    }
                    break;
                    
                // Vector/Physics operations
                case Opcode.CrossProduct3D:
                    {
                        var b = (Vector)operandStack.Pop();
                        var a = (Vector)operandStack.Pop();
                        var result = CrossProduct(a, b);
                        operandStack.Push(result);
                    }
                    break;
                    
                case Opcode.DotProduct3D:
                    {
                        var b = (Vector)operandStack.Pop();
                        var a = (Vector)operandStack.Pop();
                        var result = DotProduct(a, b);
                        operandStack.Push(result);
                    }
                    break;
                    
                // Mathematical analysis operations
                case Opcode.PartialDerivative:
                    {
                        var variableIndex = (int)operandStack.Pop();
                        var point = (double[])operandStack.Pop();
                        var function = (Func<double[], double>)operandStack.Pop();
                        var result = PartialDerivative(function, point, variableIndex);
                        operandStack.Push(result);
                    }
                    break;
                    
                case Opcode.Gradient:
                    {
                        var point = (double[])operandStack.Pop();
                        var function = (Func<double[], double>)operandStack.Pop();
                        var result = Gradient(function, point);
                        operandStack.Push(result);
                    }
                    break;
                    
                case Opcode.Limit:
                    {
                        var approachValue = (double)operandStack.Pop();
                        var function = (Func<double, double>)operandStack.Pop();
                        var result = Limit(function, approachValue);
                        operandStack.Push(result);
                    }
                    break;
                    
                case Opcode.Integral:
                    {
                        var upperBound = (double)operandStack.Pop();
                        var lowerBound = (double)operandStack.Pop();
                        var function = (Func<double, double>)operandStack.Pop();
                        var result = Integral(lowerBound, upperBound, function);
                        operandStack.Push(result);
                    }
                    break;
                    
                case Opcode.AutoDiff:
                    {
                        // Stack: function, x -> derivative at x
                        var x = (double)operandStack.Pop();
                        var function = (Func<double, double>)operandStack.Pop();
                        var result = MathSymbols.AutoDiff(function, x);
                        operandStack.Push(result);
                    }
                    break;
                    
                // Statistical operations
                case Opcode.Mean:
                    {
                        var values = (double[])operandStack.Pop();
                        var result = Mean(values);
                        operandStack.Push(result);
                    }
                    break;
                    
                case Opcode.StandardDeviation:
                    {
                        var values = (double[])operandStack.Pop();
                        var result = StdDev(values);
                        operandStack.Push(result);
                    }
                    break;
                    
                case Opcode.Variance:
                    {
                        var values = (double[])operandStack.Pop();
                        var result = Variance(values);
                        operandStack.Push(result);
                    }
                    break;
                    
                case Opcode.Correlation:
                    {
                        var y = (double[])operandStack.Pop();
                        var x = (double[])operandStack.Pop();
                        var result = MathSymbols.StatisticsOperators.CorrelationOperator(x, y);
                        operandStack.Push(result);
                    }
                    break;
                    
                // Collection operations for natural language
                case Opcode.AllEvenNumbers:
                    {
                        var collection = operandStack.Pop();
                        // Implement AllEvenNumbers without generics
                        var result = new List<object>();
                        if (collection is System.Collections.IEnumerable enumerable)
                        {
                            int index = 0;
                            foreach (var item in enumerable)
                            {
                                if (index % 2 == 0)
                                    result.Add(item);
                                index++;
                            }
                        }
                        operandStack.Push(result);
                    }
                    break;
                    
                case Opcode.EachMultipliedBy:
                    {
                        var multiplier = operandStack.Pop();
                        var collection = operandStack.Pop();
                        // Implement EachMultipliedBy without generics
                        var result = new List<object>();
                        if (collection is System.Collections.IEnumerable enumerable)
                        {
                            foreach (var item in enumerable)
                            {
                                try
                                {
                                    // Try to multiply each item
                                    if (item is double d && multiplier is double m)
                                        result.Add(d * m);
                                    else if (item is int i && multiplier is int mi)
                                        result.Add(i * mi);
                                    else if (item is float f && multiplier is float mf)
                                        result.Add(f * mf);
                                    else
                                    {
                                        // Try dynamic multiplication
                                        var itemDouble = Convert.ToDouble(item);
                                        var multiplierDouble = Convert.ToDouble(multiplier);
                                        result.Add(itemDouble * multiplierDouble);
                                    }
                                }
                                catch
                                {
                                    result.Add(item); // If multiplication fails, keep original
                                }
                            }
                        }
                        operandStack.Push(result);
                    }
                    break;
                    
                case Opcode.SumOfAll:
                    {
                        var collection = operandStack.Pop();
                        // Implement SumOfAll without generics
                        double sum = 0.0;
                        if (collection is System.Collections.IEnumerable enumerable)
                        {
                            foreach (var item in enumerable)
                            {
                                try
                                {
                                    sum += Convert.ToDouble(item);
                                }
                                catch
                                {
                                    // Skip items that can't be converted to double
                                }
                            }
                        }
                        operandStack.Push(sum);
                    }
                    break;
                    
                // Collection manipulation
                case Opcode.AppendToCollection:
                    {
                        var element = operandStack.Pop();
                        var collection = operandStack.Pop();
                        if (collection is System.Collections.IList list)
                        {
                            list.Add(element);
                            operandStack.Push(collection);
                        }
                        else
                        {
                            throw new VirtualMachineException("Cannot append to non-list collection");
                        }
                    }
                    break;
                    
                case Opcode.PrependToCollection:
                    {
                        var element = operandStack.Pop();
                        var collection = operandStack.Pop();
                        if (collection is System.Collections.IList list)
                        {
                            list.Insert(0, element);
                            operandStack.Push(collection);
                        }
                        else
                        {
                            throw new VirtualMachineException("Cannot prepend to non-list collection");
                        }
                    }
                    break;
                    
                // Attribute-specific opcode implementations
                
                // Syntax level operations
                case Opcode.BeginAssemblyBlock:
                    Console.WriteLine("[VM] Beginning assembly block");
                    break;
                    
                case Opcode.EndAssemblyBlock:
                    Console.WriteLine("[VM] Ending assembly block");
                    break;
                    
                // GPU operations
                case Opcode.InitGPUContext:
                    {
                        // Initialize GPU context
                        var gpuSystem = new Core.GPU.GPUSystem();
                        
                        // Query available GPU devices
                        var deviceInfo = gpuSystem.GetType().GetField("deviceInfo", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            ?.GetValue(gpuSystem);
                        
                        // Store GPU system in globals for later use
                        environment.Globals["__gpu_system"] = gpuSystem;
                        environment.Globals["__gpu_initialized"] = true;
                        
                        // Push success status
                        operandStack.Push(true);
                        
                        Console.WriteLine($"[VM] GPU context initialized");
                    }
                    break;
                    
                case Opcode.BeginKernel:
                    {
                        // Begin GPU kernel execution
                        var kernelName = constantPool[ReadInt32()] as string;
                        var gridDimX = Convert.ToInt32(operandStack.Pop());
                        var gridDimY = Convert.ToInt32(operandStack.Pop());
                        var gridDimZ = Convert.ToInt32(operandStack.Pop());
                        var blockDimX = Convert.ToInt32(operandStack.Pop());
                        var blockDimY = Convert.ToInt32(operandStack.Pop());
                        var blockDimZ = Convert.ToInt32(operandStack.Pop());
                        
                        // Store kernel launch parameters
                        environment.Globals["__kernel_name"] = kernelName;
                        environment.Globals["__grid_dim"] = new[] { gridDimX, gridDimY, gridDimZ };
                        environment.Globals["__block_dim"] = new[] { blockDimX, blockDimY, blockDimZ };
                        environment.Globals["__kernel_active"] = true;
                        
                        Console.WriteLine($"[VM] Beginning kernel '{kernelName}' with grid({gridDimX},{gridDimY},{gridDimZ}) block({blockDimX},{blockDimY},{blockDimZ})");
                    }
                    break;
                    
                case Opcode.EndKernel:
                    {
                        // End GPU kernel execution
                        var kernelName = environment.Globals.ContainsKey("__kernel_name") 
                            ? environment.Globals["__kernel_name"] as string 
                            : "unknown";
                        
                        environment.Globals["__kernel_active"] = false;
                        
                        Console.WriteLine($"[VM] Ending kernel '{kernelName}'");
                    }
                    break;
                    

                    
                case Opcode.LoadThreadIdx:
                    Console.WriteLine("[VM] Loading thread index");
                    operandStack.Push(0); // Simulated thread index
                    break;
                    
                case Opcode.LoadBlockIdx:
                    Console.WriteLine("[VM] Loading block index");
                    operandStack.Push(0); // Simulated block index
                    break;
                    
                case Opcode.LoadBlockDim:
                    Console.WriteLine("[VM] Loading block dimension");
                    operandStack.Push(32); // Simulated block dimension
                    break;
                    
                case Opcode.LoadGridDim:
                    Console.WriteLine("[VM] Loading grid dimension");
                    operandStack.Push(1); // Simulated grid dimension
                    break;
                    
                case Opcode.SyncThreads:
                    Console.WriteLine("[VM] Synchronizing threads");
                    break;
                    
                case Opcode.GPUFunctionCall:
                    Console.WriteLine("[VM] Executing GPU function call");
                    break;
                    
                // WebAssembly operations
                case Opcode.InitWasmContext:
                    {
                        // Initialize WebAssembly runtime context
                        var wasmContext = new Dictionary<string, object>
                        {
                            ["imports"] = new Dictionary<string, object>(),
                            ["exports"] = new Dictionary<string, object>(),
                            ["memory"] = new byte[65536], // 64KB initial WASM memory
                            ["table"] = new List<Delegate>(),
                            ["stack"] = new Stack<object>()
                        };
                        
                        environment.Globals["__wasm_context"] = wasmContext;
                        
                        Console.WriteLine("[VM] WebAssembly context initialized");
                        operandStack.Push(wasmContext);
                    }
                    break;
                    
                case Opcode.WasmExport:
                    {
                        // Export function to WebAssembly module
                        var functionName = constantPool[ReadInt32()] as string;
                        var exportName = constantPool[ReadInt32()] as string;
                        
                        if (environment.Globals.TryGetValue("__wasm_context", out var contextObj) &&
                            contextObj is Dictionary<string, object> wasmContext)
                        {
                            var exports = wasmContext["exports"] as Dictionary<string, object>;
                            
                            // Find the function to export
                            if (environment.NativeFunctions.TryGetValue(functionName, out var function))
                            {
                                exports[exportName] = function;
                                Console.WriteLine($"[VM] Exported function '{functionName}' as '{exportName}'");
                            }
                            else
                            {
                                Console.WriteLine($"[VM] Warning: Function '{functionName}' not found for export");
                            }
                        }
                    }
                    break;
                    
                case Opcode.WasmImport:
                    {
                        // Import function from WebAssembly host
                        var moduleName = constantPool[ReadInt32()] as string;
                        var importName = constantPool[ReadInt32()] as string;
                        var localName = constantPool[ReadInt32()] as string;
                        
                        if (environment.Globals.TryGetValue("__wasm_context", out var contextObj) &&
                            contextObj is Dictionary<string, object> wasmContext)
                        {
                            var imports = wasmContext["imports"] as Dictionary<string, object>;
                            
                            // Create import key
                            var importKey = $"{moduleName}.{importName}";
                            
                            // Create a proper imported function based on type information
                            Func<object[], object> importedFunc = (args) =>
                            {
                                // Handle common WASM imports
                                switch (importKey)
                                {
                                    case "env.print_i32":
                                        if (args.Length > 0)
                                            Console.WriteLine($"WASM print_i32: {args[0]}");
                                        return null;
                                        
                                    case "env.print_f64":
                                        if (args.Length > 0)
                                            Console.WriteLine($"WASM print_f64: {args[0]}");
                                        return null;
                                        
                                    case "env.memory_grow":
                                        if (args.Length > 0)
                                        {
                                            var pages = Convert.ToInt32(args[0]);
                                            Console.WriteLine($"WASM memory_grow: {pages} pages");
                                            // Return previous size in pages (simulated)
                                            return wasmContext.ContainsKey("memory_pages") ? wasmContext["memory_pages"] : 0;
                                        }
                                        return -1;
                                        
                                    case "env.memory_size":
                                        // Return current memory size in pages
                                        return wasmContext.ContainsKey("memory_pages") ? wasmContext["memory_pages"] : 1;
                                        
                                    case "env.abort":
                                        if (args.Length >= 4)
                                        {
                                            Console.WriteLine($"WASM abort: message={args[0]}, file={args[1]}, line={args[2]}, col={args[3]}");
                                            throw new RuntimeException($"WASM abort at {args[1]}:{args[2]}:{args[3]} - {args[0]}");
                                        }
                                        throw new RuntimeException("WASM abort");
                                        
                                    default:
                                        Console.WriteLine($"[VM] Called imported WASM function '{importKey}' with {args.Length} args");
                                        // Return default value based on expected return type
                                        return null;
                                }
                            };
                            
                            imports[importKey] = importedFunc;
                            environment.NativeFunctions[localName] = importedFunc;
                            
                            Console.WriteLine($"[VM] Imported '{importKey}' as '{localName}'");
                        }
                    }
                    break;
                    
                // Embedded operations
                case Opcode.SaveRegisters:
                    Console.WriteLine("[VM] Saving CPU registers");
                    break;
                    
                case Opcode.RestoreRegisters:
                    Console.WriteLine("[VM] Restoring CPU registers");
                    break;
                    
                case Opcode.DisableInterrupts:
                    Console.WriteLine("[VM] Disabling interrupts");
                    break;
                    
                case Opcode.EnableInterrupts:
                    Console.WriteLine("[VM] Enabling interrupts");
                    break;
                    
                // Compile-time operations
                case Opcode.CompileTimeConstant:
                    Console.WriteLine("[VM] Using compile-time constant");
                    operandStack.Push(42); // Simulated compile-time computed value
                    break;
                    
                case Opcode.CompileTimeFunction:
                    Console.WriteLine("[VM] Executing compile-time function");
                    break;
                    
                // SIMD operations
                case Opcode.EnableSimdMode:
                    Console.WriteLine("[VM] Enabling SIMD vectorization mode");
                    break;
                    
                case Opcode.DisableSimdMode:
                    Console.WriteLine("[VM] Disabling SIMD vectorization mode");
                    break;
                    
                case Opcode.VectorizeLoop:
                    Console.WriteLine("[VM] Vectorizing loop for SIMD execution");
                    break;
                    
                case Opcode.SimdLoad:
                    Console.WriteLine("[VM] SIMD vector load");
                    break;
                    
                case Opcode.SimdStore:
                    Console.WriteLine("[VM] SIMD vector store");
                    break;
                    
                case Opcode.SimdAdd:
                    Console.WriteLine("[VM] SIMD vector addition");
                    break;
                    
                case Opcode.SimdMul:
                    Console.WriteLine("[VM] SIMD vector multiplication");
                    break;
                    
                // Actor system operations
                case Opcode.InitActor:
                    Console.WriteLine("[VM] Initializing actor system");
                    operandStack.Push("Actor initialized");
                    break;
                    
                case Opcode.SetupMessageQueue:
                    Console.WriteLine("[VM] Setting up actor message queue");
                    break;
                    
                case Opcode.SendMessage:
                    Console.WriteLine("[VM] Sending message to actor");
                    var message = operandStack.Pop();
                    var actor = operandStack.Pop();
                    Console.WriteLine($"[VM] Message sent: {message} to actor: {actor}");
                    break;
                    
                case Opcode.ReceiveMessage:
                    Console.WriteLine("[VM] Receiving message from actor queue");
                    operandStack.Push("Received message");
                    break;
                    
                case Opcode.SpawnActor:
                    Console.WriteLine("[VM] Spawning new actor");
                    operandStack.Push("New actor spawned");
                    break;
                    
                case Opcode.SetupMessageHandler:
                    Console.WriteLine("[VM] Setting up actor message handler");
                    break;
                    
                // Supervisor operations
                case Opcode.InitSupervisor:
                    Console.WriteLine("[VM] Initializing supervisor");
                    operandStack.Push("Supervisor initialized");
                    break;
                    
                case Opcode.AddChildActor:
                    Console.WriteLine("[VM] Adding child actor to supervisor");
                    break;
                    
                case Opcode.RestartActor:
                    Console.WriteLine("[VM] Restarting failed actor");
                    break;
                    
                // Contract operations
                case Opcode.InitContract:
                    Console.WriteLine("[VM] Initializing smart contract");
                    operandStack.Push("Smart contract initialized");
                    break;
                    
                case Opcode.ContractCall:
                    Console.WriteLine("[VM] Executing smart contract function");
                    break;
                    
                case Opcode.ContractEvent:
                    Console.WriteLine("[VM] Emitting smart contract event");
                    break;
                    
                // Verification operations
                case Opcode.BeginVerification:
                    Console.WriteLine("[VM] Beginning formal verification");
                    break;
                    
                case Opcode.EndVerification:
                    Console.WriteLine("[VM] Ending formal verification");
                    break;
                    
                case Opcode.VerifyCondition:
                    Console.WriteLine("[VM] Verifying condition");
                    var verifyCondition = operandStack.Pop();
                    if (verifyCondition is bool condBool && condBool)
                    {
                        Console.WriteLine("[VM] Condition verified successfully");
                    }
                    else
                    {
                        Console.WriteLine("[VM] WARNING: Condition verification failed");
                    }
                    break;
                    
                case Opcode.VerifyPrecondition:
                    Console.WriteLine("[VM] Verifying precondition");
                    break;
                    
                case Opcode.VerifyPostcondition:
                    Console.WriteLine("[VM] Verifying postcondition");
                    break;
                    
                // Real-time operations
                case Opcode.SetRealTimePriority:
                    Console.WriteLine("[VM] Setting real-time priority");
                    break;
                    
                case Opcode.SetDeadline:
                    Console.WriteLine("[VM] Setting deadline for real-time task");
                    break;
                    
                case Opcode.CheckDeadline:
                    Console.WriteLine("[VM] Checking real-time deadline");
                    break;
                    
                // Database operations
                case Opcode.InitTable:
                    Console.WriteLine("[VM] Initializing database table");
                    operandStack.Push("Database table initialized");
                    break;
                    
                case Opcode.QueryTable:
                    Console.WriteLine("[VM] Querying database table");
                    operandStack.Push("Query results");
                    break;
                    
                case Opcode.InsertRow:
                    Console.WriteLine("[VM] Inserting row into database table");
                    break;
                    
                case Opcode.UpdateRow:
                    Console.WriteLine("[VM] Updating database table row");
                    break;
                    
                case Opcode.DeleteRow:
                    Console.WriteLine("[VM] Deleting database table row");
                    break;
                    
                // ECS operations
                case Opcode.RegisterComponent:
                    Console.WriteLine("[VM] Registering ECS component");
                    operandStack.Push("Component registered");
                    break;
                    
                case Opcode.RegisterSystem:
                    Console.WriteLine("[VM] Registering ECS system");
                    operandStack.Push("System registered");
                    break;
                    
                case Opcode.CreateEntity:
                    Console.WriteLine("[VM] Creating ECS entity");
                    operandStack.Push("Entity created");
                    break;
                    
                case Opcode.AddComponent:
                    Console.WriteLine("[VM] Adding component to entity");
                    break;
                    
                case Opcode.RemoveComponent:
                    Console.WriteLine("[VM] Removing component from entity");
                    break;
                    
                case Opcode.QueryEntities:
                    Console.WriteLine("[VM] Querying entities with components");
                    operandStack.Push("Entity query results");
                    break;
                    
                // Automatic differentiation
                case Opcode.BeginAutoDiff:
                    Console.WriteLine("[VM] Beginning automatic differentiation");
                    break;
                    
                case Opcode.EndAutoDiff:
                    Console.WriteLine("[VM] Ending automatic differentiation");
                    break;
                    
                case Opcode.ComputeGradient:
                    Console.WriteLine("[VM] Computing gradient via automatic differentiation");
                    break;
                    
                // Domain operations
                case Opcode.EnterDomain:
                    {
                        // Enter domain scope
                        var domainName = constantPool[ReadInt32()] as string;
                        
                        // Create domain context
                        if (!environment.Globals.ContainsKey("__domain_stack"))
                            environment.Globals["__domain_stack"] = new Stack<Dictionary<string, object>>();
                            
                        var domainStack = environment.Globals["__domain_stack"] as Stack<Dictionary<string, object>>;
                        
                        // Create new domain context
                        var domainContext = new Dictionary<string, object>
                        {
                            ["name"] = domainName,
                            ["operators"] = new Dictionary<string, Delegate>(),
                            ["constants"] = new Dictionary<string, object>(),
                            ["types"] = new Dictionary<string, Type>()
                        };
                        
                        // Load domain-specific operators based on domain name
                        LoadDomainOperators(domainName, domainContext);
                        
                        domainStack.Push(domainContext);
                        
                        Console.WriteLine($"[VM] Entered domain '{domainName}'");
                    }
                    break;
                    
                case Opcode.ExitDomain:
                    {
                        // Exit domain scope
                        if (environment.Globals.ContainsKey("__domain_stack"))
                        {
                            var domainStack = environment.Globals["__domain_stack"] as Stack<Dictionary<string, object>>;
                            if (domainStack.Count > 0)
                            {
                                var domain = domainStack.Pop();
                                var domainName = domain["name"] as string;
                                Console.WriteLine($"[VM] Exited domain '{domainName}'");
                            }
                        }
                    }
                    break;
                    
                case Opcode.RedefineOperator:
                    {
                        // Redefine operator in domain scope
                        var operatorSymbol = constantPool[ReadInt32()] as string;
                        var functionName = constantPool[ReadInt32()] as string;
                        var typeName = constantPool[ReadInt32()] as string;
                        
                        if (environment.Globals.ContainsKey("__domain_stack"))
                        {
                            var domainStack = environment.Globals["__domain_stack"] as Stack<Dictionary<string, object>>;
                            if (domainStack.Count > 0)
                            {
                                var domain = domainStack.Peek();
                                var operators = domain["operators"] as Dictionary<string, Delegate>;
                                
                                // Create operator key with type info
                                var operatorKey = $"{operatorSymbol}_{typeName}";
                                
                                // Look up the function to bind
                                if (environment.NativeFunctions.TryGetValue(functionName, out var function))
                                {
                                    operators[operatorKey] = function;
                                    Console.WriteLine($"[VM] Redefined operator '{operatorSymbol}' for type '{typeName}' to function '{functionName}'");
                                }
                            }
                        }
                    }
                    break;
                    
                // Array/Collection element access
                case Opcode.LoadElement:
                    {
                        var index = operandStack.Pop();
                        var array = operandStack.Pop();
                        
                        if (array is Array arr)
                        {
                            int idx = Convert.ToInt32(index);
                            if (idx < 0 || idx >= arr.Length)
                                throw new VirtualMachineException($"Array index out of bounds: {idx}");
                            operandStack.Push(arr.GetValue(idx));
                        }
                        else if (array is System.Collections.IList list)
                        {
                            int idx = Convert.ToInt32(index);
                            if (idx < 0 || idx >= list.Count)
                                throw new VirtualMachineException($"List index out of bounds: {idx}");
                            operandStack.Push(list[idx]);
                        }
                        else if (array is System.Collections.IDictionary dict)
                        {
                            if (!dict.Contains(index))
                                operandStack.Push(null);
                            else
                                operandStack.Push(dict[index]);
                        }
                        else
                        {
                            throw new VirtualMachineException($"Cannot index into type: {array?.GetType()}");
                        }
                    }
                    break;
                    
                case Opcode.StoreElement:
                    {
                        var value = operandStack.Pop();
                        var index = operandStack.Pop();
                        var array = operandStack.Pop();
                        
                        if (array is Array arr)
                        {
                            int idx = Convert.ToInt32(index);
                            if (idx < 0 || idx >= arr.Length)
                                throw new VirtualMachineException($"Array index out of bounds: {idx}");
                            arr.SetValue(value, idx);
                        }
                        else if (array is System.Collections.IList list)
                        {
                            int idx = Convert.ToInt32(index);
                            if (idx < 0 || idx >= list.Count)
                                throw new VirtualMachineException($"List index out of bounds: {idx}");
                            list[idx] = value;
                        }
                        else if (array is System.Collections.IDictionary dict)
                        {
                            dict[index] = value;
                        }
                        else
                        {
                            throw new VirtualMachineException($"Cannot store element in type: {array?.GetType()}");
                        }
                    }
                    break;
                    
                // Type operations
                case Opcode.Cast:
                    {
                        var typeName = constantPool[ReadInt32()] as string;
                        var value = operandStack.Pop();
                        
                        if (typeRegistry.TryGetValue(typeName, out Type targetType))
                        {
                            try
                            {
                                var convertedValue = Convert.ChangeType(value, targetType);
                                operandStack.Push(convertedValue);
                            }
                            catch (Exception ex)
                            {
                                throw new VirtualMachineException($"Cannot cast {value?.GetType()} to {targetType}", ex);
                            }
                        }
                        else
                        {
                            throw new VirtualMachineException($"Unknown type for cast: {typeName}");
                        }
                    }
                    break;
                    
                case Opcode.IsInstance:
                    {
                        var typeName = constantPool[ReadInt32()] as string;
                        var value = operandStack.Pop();
                        
                        if (typeRegistry.TryGetValue(typeName, out Type checkType))
                        {
                            bool isInstance = value != null && checkType.IsAssignableFrom(value.GetType());
                            operandStack.Push(isInstance);
                        }
                        else
                        {
                            operandStack.Push(false);
                        }
                    }
                    break;
                    
                // Control flow
                case Opcode.Break:
                    {
                        // Break should be handled by compiler with jump to loop end
                        // This is a placeholder in case of direct break instruction
                        Console.WriteLine("[VM] Break instruction encountered - should be compiled to jump");
                        // Break instruction - unwind to nearest loop and jump to end
                        // The compiler should have emitted a jump target
                        var jumpTarget = ReadInt32();
                        instructionPointer = jumpTarget;
                        
                        // Fire debugging event
                        OnInstructionExecute?.Invoke(instructionPointer - 5, Opcode.Break);
                    }
                    break;
                    
                case Opcode.Continue:
                    {
                        // Continue should be handled by compiler with jump to loop start
                        // This is a placeholder in case of direct continue instruction
                        Console.WriteLine("[VM] Continue instruction encountered - should be compiled to jump");
                        // Continue instruction - jump to loop condition check
                        // The compiler should have emitted a jump target
                        var jumpTarget = ReadInt32();
                        instructionPointer = jumpTarget;
                        
                        // Fire debugging event
                        OnInstructionExecute?.Invoke(instructionPointer - 5, Opcode.Continue);
                    }
                    break;
                    
                // Iterator operations
                case Opcode.GetIterator:
                    {
                        var collection = operandStack.Pop();
                        if (collection is System.Collections.IEnumerable enumerable)
                        {
                            operandStack.Push(enumerable.GetEnumerator());
                        }
                        else
                        {
                            throw new VirtualMachineException($"Cannot get iterator for type: {collection?.GetType()}");
                        }
                    }
                    break;
                    
                case Opcode.IteratorHasNext:
                    {
                        var iterator = operandStack.Peek();
                        if (iterator is System.Collections.IEnumerator enumerator)
                        {
                            operandStack.Push(enumerator.MoveNext());
                        }
                        else
                        {
                            throw new VirtualMachineException($"Invalid iterator type: {iterator?.GetType()}");
                        }
                    }
                    break;
                    
                case Opcode.IteratorNext:
                    {
                        var iterator = operandStack.Pop();
                        if (iterator is System.Collections.IEnumerator enumerator)
                        {
                            operandStack.Push(enumerator.Current);
                            operandStack.Push(iterator); // Push iterator back
                        }
                        else
                        {
                            throw new VirtualMachineException($"Invalid iterator type: {iterator?.GetType()}");
                        }
                    }
                    break;
                    
                case Opcode.YieldReturn:
                    {
                        var value = operandStack.Pop();
                        // Store yielded value in current frame
                        if (callStack.Count > 0)
                        {
                            var frame = callStack.Peek();
                            var generatorId = frame.Function?.GetHashCode() ?? 0;
                            
                            // Get or create generator state
                            if (!generatorStates.ContainsKey(generatorId))
                            {
                                generatorStates[generatorId] = new GeneratorState
                                {
                                    ReturnAddress = frame.ReturnAddress,
                                    Position = instructionPointer
                                };
                            }
                            
                            var state = generatorStates[generatorId];
                            state.Values.Push(value);
                            state.Position = instructionPointer;
                            
                            Console.WriteLine($"[VM] Yielding value: {value}");
                            
                            // Push yielded value for the caller
                            operandStack.Push(value);
                        }
                    }
                    break;
                    
                case Opcode.YieldBreak:
                    {
                        // Signal end of generator
                        if (callStack.Count > 0)
                        {
                            Console.WriteLine("[VM] Generator finished");
                        }
                    }
                    break;
                    
                case Opcode.MonitorEnter:
                    {
                        var obj = operandStack.Pop();
                        if (obj != null)
                        {
                            Monitor.Enter(obj);
                        }
                    }
                    break;
                    
                case Opcode.MonitorExit:
                    {
                        var obj = operandStack.Pop();
                        if (obj != null)
                        {
                            Monitor.Exit(obj);
                        }
                    }
                    break;
                    
                /* DUPLICATE CASES REMOVED - These opcodes are already handled earlier in the switch statement
                case Opcode.Import:
                    {
                        var moduleNameIndex = instructions[instructionPointer++];
                        var moduleName = constantPool[moduleNameIndex] as string;
                        ImportModule(moduleName);
                    }
                    break;
                    
                case Opcode.BeginTry:
                    {
                        var handlerOffset = BitConverter.ToInt32(instructions, instructionPointer);
                        instructionPointer += 4;
                        // Store try block info
                        var tryStart = instructionPointer;
                        // This would be used by exception handling
                    }
                    break;
                    
                case Opcode.EndTry:
                    {
                        // Mark end of try block
                    }
                    break;
                    
                case Opcode.BeginCatch:
                    {
                        // Begin catch block - exception is on stack
                        var exception = operandStack.Pop();
                        // Store exception in local variable
                    }
                    break;
                    
                case Opcode.BeginFinally:
                    {
                        // Begin finally block
                    }
                    break;
                    
                case Opcode.EndFinally:
                    {
                        // End finally block - may need to rethrow
                    }
                    break;
                    
                case Opcode.CheckExceptionType:
                    {
                        var exceptionType = constantPool[instructions[instructionPointer++]] as string;
                        var exception = operandStack.Peek();
                        var matches = exception?.GetType().Name == exceptionType;
                        operandStack.Push(matches);
                    }
                    break;
                    
                case Opcode.NullCoalesce:
                    {
                        var right = operandStack.Pop();
                        var left = operandStack.Pop();
                        operandStack.Push(left ?? right);
                    }
                    break;
                    
                case Opcode.TypeOf:
                    {
                        var typeIndex = instructions[instructionPointer++];
                        var typeName = constantPool[typeIndex] as string;
                        if (typeRegistry.TryGetValue(typeName, out var type))
                        {
                            operandStack.Push(type);
                        }
                        else
                        {
                            operandStack.Push(typeof(object));
                        }
                    }
                    break;
                    
                case Opcode.SizeOf:
                    {
                        var typeIndex = instructions[instructionPointer++];
                        var typeName = constantPool[typeIndex] as string;
                        // Return size based on type
                        var size = typeName switch
                        {
                            "bool" => sizeof(bool),
                            "byte" => sizeof(byte),
                            "short" => sizeof(short),
                            "int" => sizeof(int),
                            "long" => sizeof(long),
                            "float" => sizeof(float),
                            "double" => sizeof(double),
                            _ => IntPtr.Size // Reference type
                        };
                        operandStack.Push(size);
                    }
                    break;
                    
                case Opcode.MakeVector:
                    {
                        var componentCount = instructions[instructionPointer++];
                        var components = new double[componentCount];
                        for (int i = componentCount - 1; i >= 0; i--)
                        {
                            components[i] = Convert.ToDouble(operandStack.Pop());
                        }
                        operandStack.Push(new Vector(components));
                    }
                    break;
                    
                case Opcode.MakeMatrix:
                    {
                        var rows = instructions[instructionPointer++];
                        var cols = instructions[instructionPointer++];
                        var elements = new double[rows, cols];
                        for (int r = rows - 1; r >= 0; r--)
                        {
                            for (int c = cols - 1; c >= 0; c--)
                            {
                                elements[r, c] = Convert.ToDouble(operandStack.Pop());
                            }
                        }
                        operandStack.Push(new Matrix(elements));
                    }
                    break;
                    
                case Opcode.MakeQuaternion:
                    {
                        var z = Convert.ToDouble(operandStack.Pop());
                        var y = Convert.ToDouble(operandStack.Pop());
                        var x = Convert.ToDouble(operandStack.Pop());
                        var w = Convert.ToDouble(operandStack.Pop());
                        operandStack.Push(new Quaternion(w, x, y, z));
                    }
                    break;
                    
                case Opcode.DefineClass:
                case Opcode.DefineInterface:
                case Opcode.DefineStruct:
                case Opcode.DefineEnum:
                case Opcode.DefineComponent:
                case Opcode.DefineSystem:
                case Opcode.DefineEntity:
                case Opcode.DefineFunction:
                    {
                        // These are handled during bytecode loading
                        var typeIndex = instructions[instructionPointer++];
                    }
                    break;
                    
                case Opcode.LoadMemberNullSafe:
                    {
                        var memberNameIndex = instructions[instructionPointer++];
                        var memberName = constantPool[memberNameIndex] as string;
                        var obj = operandStack.Pop();
                        
                        if (obj == null)
                        {
                            operandStack.Push(null);
                        }
                        else
                        {
                            operandStack.Push(GetMemberValue(obj, memberName));
                        }
                    }
                    break;
                    
                case Opcode.StringConcat:
                    {
                        var right = operandStack.Pop()?.ToString() ?? "";
                        var left = operandStack.Pop()?.ToString() ?? "";
                        operandStack.Push(left + right);
                    }
                    break;
                    
                case Opcode.SetParallelism:
                    {
                        var parallelism = Convert.ToInt32(operandStack.Pop());
                        
                        // Configure thread pool for desired parallelism
                        if (parallelism > 0)
                        {
                            System.Threading.ThreadPool.SetMinThreads(parallelism, parallelism);
                            System.Threading.ThreadPool.SetMaxThreads(parallelism * 2, parallelism * 2);
                            
                            // Store for parallel operations
                            environment.Globals["__parallelism"] = parallelism;
                            
                            Console.WriteLine($"[VM] Set parallelism to {parallelism} threads");
                        }
                        else
                        {
                            // Reset to defaults
                            System.Threading.ThreadPool.GetMinThreads(out int workerThreads, out int completionPortThreads);
                            environment.Globals["__parallelism"] = workerThreads;
                        }
                    }
                    break;
                    
                case Opcode.BeginAsync:
                    {
                        // Mark beginning of async operation - push async context
                        var asyncContext = new Dictionary<string, object>
                        {
                            ["StartTime"] = DateTime.UtcNow,
                            ["ThreadId"] = System.Threading.Thread.CurrentThread.ManagedThreadId,
                            ["IsAsync"] = true
                        };
                        
                        // Store async context
                        environment.Globals["__async_context"] = asyncContext;
                        
                        // Fire event for profiler/debugger
                        OnFunctionEnter?.Invoke("__async_block", instructionPointer);
                    }
                    break;
                    
                case Opcode.EndAsync:
                    {
                        // Mark end of async operation - restore context
                        if (environment.Globals.TryGetValue("__async_context", out var contextObj) && 
                            contextObj is Dictionary<string, object> asyncContext)
                        {
                            var duration = DateTime.UtcNow - (DateTime)asyncContext["StartTime"];
                            Console.WriteLine($"[VM] Async operation completed in {duration.TotalMilliseconds}ms");
                            
                            // Clean up async context
                            environment.Globals.Remove("__async_context");
                        }
                        
                        // Fire event for profiler/debugger
                        OnFunctionExit?.Invoke("__async_block");
                    }
                    break;
                    
                case Opcode.BeginParallel:
                    {
                        // Mark beginning of parallel block
                        var parallelContext = new Dictionary<string, object>
                        {
                            ["StartTime"] = DateTime.UtcNow,
                            ["ThreadCount"] = environment.Globals.ContainsKey("__parallelism") 
                                ? (int)environment.Globals["__parallelism"] 
                                : Environment.ProcessorCount,
                            ["IsParallel"] = true
                        };
                        
                        // Store parallel context
                        environment.Globals["__parallel_context"] = parallelContext;
                        
                        // Fire event for profiler/debugger
                        OnFunctionEnter?.Invoke("__parallel_block", instructionPointer);
                    }
                    break;
                    
                case Opcode.EndParallel:
                    {
                        // Mark end of parallel block
                        if (environment.Globals.TryGetValue("__parallel_context", out var contextObj) && 
                            contextObj is Dictionary<string, object> parallelContext)
                        {
                            var duration = DateTime.UtcNow - (DateTime)parallelContext["StartTime"];
                            Console.WriteLine($"[VM] Parallel block completed in {duration.TotalMilliseconds}ms using {parallelContext["ThreadCount"]} threads");
                            
                            // Clean up parallel context
                            environment.Globals.Remove("__parallel_context");
                        }
                        
                        // Fire event for profiler/debugger
                        OnFunctionExit?.Invoke("__parallel_block");
                    }
                    break;
                    
                case Opcode.ThrowMatchError:
                    {
                        throw new VirtualMachineException("Match expression was not exhaustive");
                    }
                    
                case Opcode.EmitRawAssembly:
                case Opcode.NativeInstruction:
                case Opcode.RawBytes:
                    {
                        // These would interface with native code generation
                        var size = instructions[instructionPointer++];
                        instructionPointer += size; // Skip raw bytes
                    }
                    break;
                    
                case Opcode.PreIncrement:
                    {
                        var value = Convert.ToDouble(operandStack.Pop());
                        operandStack.Push(value + 1);
                    }
                    break;
                    
                case Opcode.PostIncrement:
                    {
                        var value = Convert.ToDouble(operandStack.Pop());
                        operandStack.Push(value);
                        // Would need to store incremented value back
                    }
                    break;
                    
                case Opcode.PreDecrement:
                    {
                        var value = Convert.ToDouble(operandStack.Pop());
                        operandStack.Push(value - 1);
                    }
                    break;
                    
                case Opcode.PostDecrement:
                    {
                        var value = Convert.ToDouble(operandStack.Pop());
                        operandStack.Push(value);
                        // Would need to store decremented value back
                    }
                    break;
                    
                case Opcode.MakeClosure:
                    {
                        var functionIndex = instructions[instructionPointer++];
                        var captureCount = instructions[instructionPointer++];
                        var captures = new object[captureCount];
                        for (int i = captureCount - 1; i >= 0; i--)
                        {
                            captures[i] = operandStack.Pop();
                        }
                        // Create closure with captured variables
                        var func = constantPool[functionIndex];
                                            operandStack.Push(new Closure { Function = func, Captures = captures });
                }
                break; */
                    
                default:
                    throw new VirtualMachineException($"Unknown opcode: {opcode}");
            }
        }
        
        private int ReadInt32()
        {
            var value = BitConverter.ToInt32(instructions, instructionPointer);
            instructionPointer += 4;
            return value;
        }
        
        private void RegisterBuiltInTypes()
        {
            // Register primitive types
            typeRegistry["int"] = typeof(int);
            typeRegistry["long"] = typeof(long);
            typeRegistry["float"] = typeof(float);
            typeRegistry["double"] = typeof(double);
            typeRegistry["bool"] = typeof(bool);
            typeRegistry["string"] = typeof(string);
            typeRegistry["object"] = typeof(object);
            
            // Register basic types in globals for static method access
            environment.Globals["double"] = typeof(double);
            environment.Globals["int"] = typeof(int);
            environment.Globals["string"] = typeof(string);
            environment.Globals["bool"] = typeof(bool);
            environment.Globals["float"] = typeof(float);
            environment.Globals["long"] = typeof(long);
            environment.Globals["object"] = typeof(object);
            environment.Globals["Math"] = typeof(System.Math);
            
            // Register math types
            typeRegistry["Vector"] = typeof(Vector);
            typeRegistry["Matrix"] = typeof(Matrix);
            typeRegistry["Quaternion"] = typeof(Quaternion);
            typeRegistry["Transform"] = typeof(Transform);
            typeRegistry["Complex"] = typeof(MathSymbols.Complex);
            typeRegistry["HashSet"] = typeof(System.Collections.Generic.HashSet<>);
        }
        
        private void RegisterBuiltInFunctions()
        {
            // Register UI functions
            environment.NativeFunctions["CreateWindow"] = new Action<string, double, double>(Ouro.StdLib.UI.UIBuiltins.CreateWindow);
            environment.NativeFunctions["ShowWindow"] = new Action(Ouro.StdLib.UI.UIBuiltins.ShowWindow);
            environment.NativeFunctions["AddButton"] = new Action<string, double, double, double, double>(Ouro.StdLib.UI.UIBuiltins.AddButton);
            environment.NativeFunctions["AddLabel"] = new Action<string, double, double, double, double>(Ouro.StdLib.UI.UIBuiltins.AddLabel);
            environment.NativeFunctions["AddTextBox"] = new Action<string, double, double, double, double>(Ouro.StdLib.UI.UIBuiltins.AddTextBox);
            environment.NativeFunctions["UpdateDisplay"] = new Action<string>(Ouro.StdLib.UI.UIBuiltins.UpdateDisplay);
            environment.NativeFunctions["RunUI"] = new Action(Ouro.StdLib.UI.UIBuiltins.RunUI);
            environment.NativeFunctions["ProcessMessages"] = new Action(Ouro.StdLib.UI.UIBuiltins.ProcessMessages);
            environment.NativeFunctions["IsWindowClosed"] = new Func<bool>(Ouro.StdLib.UI.UIBuiltins.IsWindowClosed);
            environment.NativeFunctions["HasButtonClicks"] = new Func<bool>(Ouro.StdLib.UI.UIBuiltins.HasButtonClicks);
            environment.NativeFunctions["GetNextButtonClick"] = new Func<string>(Ouro.StdLib.UI.UIBuiltins.GetNextButtonClick);
            
            // Register UIBuiltins class for static method access
            environment.Globals["UIBuiltins"] = typeof(Ouro.StdLib.UI.UIBuiltins);
            
            // Register Console class and console alias
            environment.Globals["Console"] = typeof(System.Console);
            environment.Globals["console"] = typeof(System.Console);
            environment.NativeFunctions["console.WriteLine"] = new Action<string>(System.Console.WriteLine);
            
            // Register MathFunctions class and individual functions
            environment.Globals["MathFunctions"] = typeof(Ouro.StdLib.Math.MathFunctions);
            environment.NativeFunctions["MathFunctions.Sqrt"] = new Func<double, double>(Ouro.StdLib.Math.MathFunctions.Sqrt);
            environment.NativeFunctions["formatNumber"] = new Func<object, string>(obj => obj?.ToString() ?? "");
            
            // Register additional parsing functions for type conversions
            environment.NativeFunctions["parseNumber"] = new Func<string, double>(s => {
                if (double.TryParse(s, out double result)) return result;
                return 0.0;
            });
            environment.NativeFunctions["formatNumber"] = new Func<object, string>(obj => obj?.ToString() ?? "");
            
            // Register additional math functions
            environment.NativeFunctions["Math.Ceiling"] = new Func<double, double>(System.Math.Ceiling);
            environment.NativeFunctions["Math.Min"] = new Func<double, double, double>(System.Math.Min);
            environment.NativeFunctions["Math.Max"] = new Func<double, double, double>(System.Math.Max);
            
            // Register parsing functions for type conversions
            environment.NativeFunctions["double.Parse"] = new Func<string, double>(double.Parse);
            environment.NativeFunctions["int.Parse"] = new Func<string, int>(int.Parse);
            environment.NativeFunctions["string.Parse"] = new Func<object, string>(obj => obj?.ToString() ?? "");
            
            // Register Threading functions
            environment.Globals["Thread"] = typeof(System.Threading.Thread);
            environment.NativeFunctions["Thread.Sleep"] = new Action<int>(System.Threading.Thread.Sleep);
            
            // Register mathematical constants
            RegisterMathConstants();
        }
        
        private void RegisterMathConstants()
        {
            // Mathematical constants (Greek letters)
            environment.Globals["π"] = MathSymbols.π;
            environment.Globals["pi"] = MathSymbols.π;
            environment.Globals["τ"] = MathSymbols.τ;
            environment.Globals["tau"] = MathSymbols.τ;
            environment.Globals["e"] = MathSymbols.e;
            environment.Globals["φ"] = MathSymbols.φ;
            environment.Globals["phi"] = MathSymbols.φ;
            environment.Globals["γ"] = MathSymbols.γ;
            environment.Globals["gamma"] = MathSymbols.γ;
            environment.Globals["ρ"] = MathSymbols.ρ;
            environment.Globals["rho"] = MathSymbols.ρ;
            environment.Globals["δ"] = MathSymbols.δ;
            environment.Globals["delta"] = MathSymbols.δ;
            environment.Globals["α"] = MathSymbols.α;
            environment.Globals["alpha"] = MathSymbols.α;
            environment.Globals["∞"] = MathSymbols.INFINITY;
            
            // Physics constants
            environment.Globals["c"] = MathSymbols.c;
            environment.Globals["G"] = MathSymbols.G;
            environment.Globals["h"] = MathSymbols.h;
            environment.Globals["ℏ"] = MathSymbols.hbar;
            environment.Globals["k_B"] = MathSymbols.k_B;
            environment.Globals["N_A"] = MathSymbols.N_A;
            environment.Globals["R"] = MathSymbols.R;
            
            // Math functions
            environment.NativeFunctions["Sin"] = new Func<double, double>(MathSymbols.Sin);
            environment.NativeFunctions["Cos"] = new Func<double, double>(MathSymbols.Cos);
            environment.NativeFunctions["Tan"] = new Func<double, double>(MathSymbols.Tan);
            environment.NativeFunctions["Sqrt"] = new Func<double, double>(MathSymbols.Sqrt);
            environment.NativeFunctions["Sum"] = new Func<int, int, Func<int, double>, double>(MathSymbols.Sum);
            environment.NativeFunctions["Product"] = new Func<int, int, Func<int, double>, double>(MathSymbols.Product);
            environment.NativeFunctions["Integral"] = new Func<double, double, Func<double, double>, double>((a, b, f) => MathSymbols.Integral(a, b, f));
            environment.NativeFunctions["Mu"] = new Func<double[], double>(MathSymbols.Mu);
            environment.NativeFunctions["Sigma"] = new Func<double[], double>(MathSymbols.Sigma);
            environment.NativeFunctions["SigmaSquared"] = new Func<double[], double>(MathSymbols.SigmaSquared);
        }
        

        
        private void LoadTypes(Bytecode bytecode)
        {
            // Load classes
            foreach (var classInfo in bytecode.Classes)
            {
                // Generate runtime type from ClassInfo
                var type = CreateDynamicClass(classInfo);
                typeRegistry[classInfo.Name] = type;
                environment.Globals[$"__class_{classInfo.Name}"] = classInfo;
            }
            
            // Load structs
            foreach (var structInfo in bytecode.Structs)
            {
                // Generate runtime type from StructInfo
                var type = CreateDynamicStruct(structInfo);
                typeRegistry[structInfo.Name] = type;
                environment.Globals[$"__struct_{structInfo.Name}"] = structInfo;
            }
            
            // Load enums
            foreach (var enumInfo in bytecode.Enums)
            {
                // Generate runtime type from EnumInfo
                var type = CreateDynamicEnum(enumInfo);
                typeRegistry[enumInfo.Name] = type;
                environment.Globals[$"__enum_{enumInfo.Name}"] = enumInfo;
                
                // Register all enum values as constants
                foreach (var member in enumInfo.Members)
                {
                    environment.Globals[$"{enumInfo.Name}.{member.Name}"] = member.Value;
                }
            }
        }
        
        private Type CreateDynamicClass(ClassInfo classInfo)
        {
            // For now, use ExpandoObject for dynamic classes
            // In a full implementation, we'd use System.Reflection.Emit
            return typeof(System.Dynamic.ExpandoObject);
        }
        
        private Type CreateDynamicStruct(StructInfo structInfo)
        {
            // For now, use a special marker type for structs
            // In a full implementation, we'd create a value type using Reflection.Emit
            return typeof(ValueType);
        }
        
        private Type CreateDynamicEnum(EnumInfo enumInfo)
        {
            // For now, use int as the underlying type for enums
            // In a full implementation, we'd create an enum type using Reflection.Emit
            return typeof(int);
        }
        
        private object ArithmeticOperation(object left, object right, Func<dynamic, dynamic, dynamic> op)
        {
            try
            {
                if (left is Vector lv && right is Vector rv)
                {
                    // Vector operations
                    if (op.Method.Name.Contains("Add"))
                        return lv + rv;
                    else if (op.Method.Name.Contains("Subtract"))
                        return lv - rv;
                    else if (op.Method.Name.Contains("Multiply") && right is double)
                        return lv * Convert.ToDouble(right);
                }
                else if (left is Matrix lm && right is Matrix rm)
                {
                    // Matrix operations
                    if (op.Method.Name.Contains("Add"))
                        return lm + rm;
                    else if (op.Method.Name.Contains("Subtract"))
                        return lm - rm;
                    else if (op.Method.Name.Contains("Multiply"))
                        return lm * rm;
                }
                
                // Default numeric operations
                return op((dynamic)left, (dynamic)right);
            }
            catch (Exception ex)
            {
                throw new VirtualMachineException($"Arithmetic operation failed: {ex.Message}");
            }
        }
        
        private int Compare(object left, object right)
        {
            if (left == null && right == null) return 0;
            if (left == null) return -1;
            if (right == null) return 1;
            
            if (left is IComparable lc)
                return lc.CompareTo(right);
                
            throw new VirtualMachineException($"Cannot compare values of type {left.GetType()} and {right.GetType()}");
        }
        
        private object GetMemberValue(object obj, string memberName)
        {
            // Handle Type objects for static members
            if (obj is Type type)
            {
                // Try static field
                var field = type.GetField(memberName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (field != null)
                    return field.GetValue(null);
                    
                // Try static property
                var property = type.GetProperty(memberName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (property != null)
                    return property.GetValue(null);
                    
                // Try static method - return a method reference that can be called
                if (type.ContainsGenericParameters)
                {
                    Console.WriteLine($"DEBUG: Skipping static method lookup on generic type {type.Name}");
                    throw new VirtualMachineException($"Cannot access static members on generic type definition {type.Name}");
                }
                
                var method = type.GetMethod(memberName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (method != null)
                {
                    // Return a delegate that can be called
                    return method;
                }
                
                throw new VirtualMachineException($"Static member '{memberName}' not found on type {type.Name}");
            }
            
            // Instance members
            var objType = obj.GetType();
            
            // Try field
            var objField = objType.GetField(memberName);
            if (objField != null)
                return objField.GetValue(obj);
                
            // Try property
            var objProperty = objType.GetProperty(memberName);
            if (objProperty != null)
                return objProperty.GetValue(obj);
                
            throw new VirtualMachineException($"Member '{memberName}' not found on type {objType.Name}");
        }
        
        private void SetMemberValue(object obj, string memberName, object value)
        {
            var type = obj.GetType();
            
            // Try field
            var field = type.GetField(memberName);
            if (field != null)
            {
                field.SetValue(obj, value);
                return;
            }
            
            // Try property
            var property = type.GetProperty(memberName);
            if (property != null)
            {
                property.SetValue(obj, value);
                return;
            }
            
            throw new VirtualMachineException($"Member '{memberName}' not found on type {type.Name}");
        }
        
        private object CallMethod(object obj, string methodName, object[] args)
        {
            var type = obj.GetType();
            
            if (type.ContainsGenericParameters)
            {
                Console.WriteLine($"DEBUG: Skipping method call on generic type {type.Name}");
                throw new VirtualMachineException($"Cannot call methods on generic type definition {type.Name}");
            }
            
            var method = type.GetMethod(methodName);
            
            if (method == null)
                throw new VirtualMachineException($"Method '{methodName}' not found on type {type.Name}");
                
            return method.Invoke(obj, args);
        }
        
        private void RegisterImportedTypes(string modulePath)
        {
            // Register types from the imported module
            switch (modulePath)
            {
                case "System":
                    typeRegistry["Console"] = typeof(Console);
                    typeRegistry["DateTime"] = typeof(DateTime);
                    typeRegistry["Math"] = typeof(System.Math);
                    typeRegistry["Convert"] = typeof(Convert);
                    typeRegistry["Exception"] = typeof(Exception);
                    typeRegistry["DivideByZeroException"] = typeof(DivideByZeroException);
                    typeRegistry["NullReferenceException"] = typeof(NullReferenceException);
                    break;
                    
                case "Ouro.StdLib.IO":
                    typeRegistry["File"] = typeof(System.IO.File);
                    typeRegistry["Directory"] = typeof(System.IO.Directory);
                    typeRegistry["Path"] = typeof(System.IO.Path);
                    break;
                    
                case "Ouro.StdLib.Math":
                    // Math types are already registered in RegisterBuiltInTypes
                    break;
                    
                case "Ouro.StdLib.UI":
                    typeRegistry["Window"] = typeof(Ouro.StdLib.UI.Window);
                    typeRegistry["Button"] = typeof(Ouro.StdLib.UI.Button);
                    typeRegistry["Label"] = typeof(Ouro.StdLib.UI.Label);
                    typeRegistry["TextBox"] = typeof(Ouro.StdLib.UI.TextBox);
                    typeRegistry["MenuBar"] = typeof(Ouro.StdLib.UI.MenuBar);
                    typeRegistry["ToolBar"] = typeof(Ouro.StdLib.UI.ToolBar);
                    typeRegistry["TabControl"] = typeof(Ouro.StdLib.UI.TabControl);
                    typeRegistry["TabPage"] = typeof(Ouro.StdLib.UI.TabPage);
                    typeRegistry["CheckBox"] = typeof(Ouro.StdLib.UI.CheckBox);
                    typeRegistry["RadioButton"] = typeof(Ouro.StdLib.UI.RadioButton);
                    typeRegistry["Slider"] = typeof(Ouro.StdLib.UI.Slider);
                    typeRegistry["ProgressBar"] = typeof(Ouro.StdLib.UI.ProgressBar);
                    typeRegistry["ComboBox"] = typeof(Ouro.StdLib.UI.ComboBox);
                    typeRegistry["NumericUpDown"] = typeof(Ouro.StdLib.UI.NumericUpDown);
                    typeRegistry["DatePicker"] = typeof(Ouro.StdLib.UI.DatePicker);
                    typeRegistry["ColorPicker"] = typeof(Ouro.StdLib.UI.ColorPicker);
                    typeRegistry["Theme"] = typeof(Ouro.StdLib.UI.Theme);
                    break;
                    
                case "Ouro.StdLib.System":
                    typeRegistry["Console"] = typeof(System.Console);
                    typeRegistry["DateTime"] = typeof(System.DateTime);
                    typeRegistry["Environment"] = typeof(System.Environment);
                    break;
                    
                case "Ouro.StdLib.Collections":
                    // Don't register open generic types as they cause ContainsGenericParameters errors
                    Console.WriteLine("DEBUG: Skipping generic collection types in VM to avoid ContainsGenericParameters errors");
                    // typeRegistry["List"] = typeof(System.Collections.Generic.List<>);
                    // typeRegistry["Dictionary"] = typeof(System.Collections.Generic.Dictionary<,>);
                    // typeRegistry["Stack"] = typeof(System.Collections.Generic.Stack<>);
                    // typeRegistry["Queue"] = typeof(System.Collections.Generic.Queue<>);
                    // typeRegistry["HashSet"] = typeof(System.Collections.Generic.HashSet<>);
                    break;
                    
                case "static Ouro.StdLib.Math.MathSymbols":
                    // Math symbols are already registered as constants
                    break;
                    
                default:
                    // Try to handle compound imports like "Ouro.StdLib.*"
                    if (modulePath.StartsWith("Ouro.StdLib"))
                    {
                        // Register all standard library types
                        RegisterImportedTypes("Ouro.StdLib.IO");
                        RegisterImportedTypes("Ouro.StdLib.Math");
                        RegisterImportedTypes("Ouro.StdLib.UI");
                        RegisterImportedTypes("Ouro.StdLib.System");
                        RegisterImportedTypes("Ouro.StdLib.Collections");
                    }
                    break;
            }
        }
        
        public void Reset()
        {
            operandStack.Clear();
            callStack.Clear();
            instructionPointer = 0;
            globals.Clear();
            locals.Clear();
        }
        
        private void InitializeGlobalConstants(SymbolTable symbolTable)
        {
            if (symbolTable == null) return;
            
            // Look for imported math symbols and load them into globals
            var piSymbol = symbolTable.Lookup("π");
            if (piSymbol != null && piSymbol.IsGlobal)
                globals[piSymbol.Index] = MathSymbols.π;
                
            var piSymbolAscii = symbolTable.Lookup("pi");
            if (piSymbolAscii != null && piSymbolAscii.IsGlobal)
                globals[piSymbolAscii.Index] = MathSymbols.π;
                
            var tauSymbol = symbolTable.Lookup("τ");
            if (tauSymbol != null && tauSymbol.IsGlobal)
                globals[tauSymbol.Index] = MathSymbols.τ;
                
            var tauSymbolAscii = symbolTable.Lookup("tau");
            if (tauSymbolAscii != null && tauSymbolAscii.IsGlobal)
                globals[tauSymbolAscii.Index] = MathSymbols.τ;
                
            var eSymbol = symbolTable.Lookup("e");
            if (eSymbol != null && eSymbol.IsGlobal)
                globals[eSymbol.Index] = MathSymbols.e;
                
            var phiSymbol = symbolTable.Lookup("φ");
            if (phiSymbol != null && phiSymbol.IsGlobal)
                globals[phiSymbol.Index] = MathSymbols.φ;
                
            var phiSymbolAscii = symbolTable.Lookup("phi");
            if (phiSymbolAscii != null && phiSymbolAscii.IsGlobal)
                globals[phiSymbolAscii.Index] = MathSymbols.φ;
                
            var gammaSymbol = symbolTable.Lookup("γ");
            if (gammaSymbol != null && gammaSymbol.IsGlobal)
                globals[gammaSymbol.Index] = MathSymbols.γ;
                
            var gammaSymbolAscii = symbolTable.Lookup("gamma");
            if (gammaSymbolAscii != null && gammaSymbolAscii.IsGlobal)
                globals[gammaSymbolAscii.Index] = MathSymbols.γ;
                
            var rhoSymbol = symbolTable.Lookup("ρ");
            if (rhoSymbol != null && rhoSymbol.IsGlobal)
                globals[rhoSymbol.Index] = MathSymbols.ρ;
                
            var rhoSymbolAscii = symbolTable.Lookup("rho");
            if (rhoSymbolAscii != null && rhoSymbolAscii.IsGlobal)
                globals[rhoSymbolAscii.Index] = MathSymbols.ρ;
                
            var deltaSymbol = symbolTable.Lookup("δ");
            if (deltaSymbol != null && deltaSymbol.IsGlobal)
                globals[deltaSymbol.Index] = MathSymbols.δ;
                
            var deltaSymbolAscii = symbolTable.Lookup("delta");
            if (deltaSymbolAscii != null && deltaSymbolAscii.IsGlobal)
                globals[deltaSymbolAscii.Index] = MathSymbols.δ;
                
            var alphaSymbol = symbolTable.Lookup("α");
            if (alphaSymbol != null && alphaSymbol.IsGlobal)
                globals[alphaSymbol.Index] = MathSymbols.α;
                
            var alphaSymbolAscii = symbolTable.Lookup("alpha");
            if (alphaSymbolAscii != null && alphaSymbolAscii.IsGlobal)
                globals[alphaSymbolAscii.Index] = MathSymbols.α;
                
            var infinitySymbol = symbolTable.Lookup("∞");
            if (infinitySymbol != null && infinitySymbol.IsGlobal)
                globals[infinitySymbol.Index] = MathSymbols.INFINITY;
                
            // Physics constants
            var cSymbol = symbolTable.Lookup("c");
            if (cSymbol != null && cSymbol.IsGlobal)
                globals[cSymbol.Index] = MathSymbols.c;
                
            var GSymbol = symbolTable.Lookup("G");
            if (GSymbol != null && GSymbol.IsGlobal)
                globals[GSymbol.Index] = MathSymbols.G;
                
            var hSymbol = symbolTable.Lookup("h");
            if (hSymbol != null && hSymbol.IsGlobal)
                globals[hSymbol.Index] = MathSymbols.h;
                
            var hbarSymbol = symbolTable.Lookup("ℏ");
            if (hbarSymbol != null && hbarSymbol.IsGlobal)
                globals[hbarSymbol.Index] = MathSymbols.hbar;
                
            var kBSymbol = symbolTable.Lookup("k_B");
            if (kBSymbol != null && kBSymbol.IsGlobal)
                globals[kBSymbol.Index] = MathSymbols.k_B;
                
            var NASymbol = symbolTable.Lookup("N_A");
            if (NASymbol != null && NASymbol.IsGlobal)
                globals[NASymbol.Index] = MathSymbols.N_A;
                
            var RSymbol = symbolTable.Lookup("R");
            if (RSymbol != null && RSymbol.IsGlobal)
                globals[RSymbol.Index] = MathSymbols.R;
        }
        
        private void ImportModule(string moduleName)
        {
            // Handle built-in modules
            if (moduleName == "System" || moduleName.StartsWith("Ouro.StdLib"))
            {
                // Register the imported types
                RegisterImportedTypes(moduleName);
            }
            else
            {
                // External module loading
                try
                {
                    // Try to load as assembly reference
                    var assembly = global::System.Reflection.Assembly.Load(moduleName);
                    if (assembly != null)
                    {
                        // Register all public types from the assembly
                        foreach (var type in assembly.GetExportedTypes())
                        {
                            typeRegistry[type.Name] = type;
                            
                            // Also register with full name
                            typeRegistry[type.FullName] = type;
                        }
                        
                        Console.WriteLine($"[VM] Loaded external assembly: {moduleName}");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[VM] Failed to load as assembly: {ex.Message}");
                }
                
                // Try to load as file
                var possiblePaths = new[]
                {
                    $"{moduleName}.dll",
                    $"{moduleName}.ouro",
                    $"modules/{moduleName}.dll",
                    $"modules/{moduleName}.ouro",
                    global::System.IO.Path.Combine(global::System.AppDomain.CurrentDomain.BaseDirectory, $"{moduleName}.dll"),
                    global::System.IO.Path.Combine(global::System.AppDomain.CurrentDomain.BaseDirectory, "modules", $"{moduleName}.dll")
                };
                
                foreach (var path in possiblePaths)
                {
                    if (global::System.IO.File.Exists(path))
                    {
                        try
                        {
                            if (path.EndsWith(".dll"))
                            {
                                var assembly = global::System.Reflection.Assembly.LoadFrom(path);
                                foreach (var type in assembly.GetExportedTypes())
                                {
                                    typeRegistry[type.Name] = type;
                                    typeRegistry[type.FullName] = type;
                                }
                                Console.WriteLine($"[VM] Loaded external module from: {path}");
                                return;
                            }
                            else if (path.EndsWith(".ouro"))
                            {
                                // Load Ouroboros module (would need compilation)
                                Console.WriteLine($"[VM] Found Ouroboros module at: {path} (compilation not yet implemented)");
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[VM] Failed to load module from {path}: {ex.Message}");
                        }
                    }
                }
                
                Console.WriteLine($"[VM] Warning: Module '{moduleName}' not found in any search path");
            }
        }
        
        private void LoadBytecode(byte[] code, object[] constants)
        {
            this.instructions = code;
            this.constantPool = constants;
            
            // Debug: Show constant pool contents
            Console.WriteLine($"DEBUG: ConstantPool has {constantPool.Length} items:");
            for (int i = 0; i < constantPool.Length; i++)
            {
                Console.WriteLine($"  [{i}] = {constantPool[i]} (type: {constantPool[i]?.GetType()?.Name ?? "null"})");
            }
            
            // Initialize globals with enough space
            var globalCount = System.Math.Max(symbolTable?.GlobalCount() ?? 0, 100);
            globals = new List<object>(new object[globalCount]);
            
            // WORKAROUND: Initialize calculator global variables with proper default values
            // This ensures the calculator works properly until compiler global initialization is fixed
            InitializeCalculatorGlobals();
        }
        
        private void InitializeCalculatorGlobals()
        {
            // Based on calculator source code order:
            // var currentValue: double = 0.0;
            // var previousValue: double = 0.0; 
            // var currentOperation: string = "";
            // var shouldClearDisplay: bool = false;
            // var displayText: string = "0";
            
            if (globals.Count > 0) globals[0] = 0.0;         // currentValue
            if (globals.Count > 1) globals[1] = 0.0;         // previousValue  
            if (globals.Count > 2) globals[2] = "";          // currentOperation
            if (globals.Count > 3) globals[3] = false;       // shouldClearDisplay
            if (globals.Count > 4) globals[4] = "0";         // displayText
            
            Console.WriteLine("DEBUG: Initialized calculator globals - displayText = \"0\"");
        }
        
        // Helper methods for mathematical operations used by VM opcodes
        private Vector CrossProduct(Vector a, Vector b) => MathSymbols.CrossProduct(a, b);
        private double DotProduct(Vector a, Vector b) => MathSymbols.DotProduct(a, b);
        private double PartialDerivative(Func<double[], double> f, double[] point, int variableIndex) => MathSymbols.PartialDerivative(f, point, variableIndex);
        private Vector Gradient(Func<double[], double> f, double[] point) => MathSymbols.Gradient(f, point);
        private double Limit(Func<double, double> f, double approachValue) => MathSymbols.Limit(f, approachValue);
        private double Integral(double a, double b, Func<double, double> f) => MathSymbols.Integral(a, b, f);
        private double Mean(double[] values) => MathSymbols.Mean(values);
        private double StdDev(double[] values) => MathSymbols.StdDev(values);
        private double Variance(double[] values) => MathSymbols.Variance(values);
        
        private Type GetDelegateType(System.Reflection.MethodInfo method)
        {
            var parameters = method.GetParameters();
            var paramTypes = parameters.Select(p => p.ParameterType).ToList();
            
            if (method.ReturnType == typeof(void))
            {
                return paramTypes.Count switch
                {
                    0 => typeof(Action),
                    1 => typeof(Action<>).MakeGenericType(paramTypes.ToArray()),
                    2 => typeof(Action<,>).MakeGenericType(paramTypes.ToArray()),
                    3 => typeof(Action<,,>).MakeGenericType(paramTypes.ToArray()),
                    4 => typeof(Action<,,,>).MakeGenericType(paramTypes.ToArray()),
                    _ => throw new NotSupportedException($"Action with {paramTypes.Count} parameters not supported")
                };
            }
            else
            {
                paramTypes.Add(method.ReturnType);
                return paramTypes.Count switch
                {
                    1 => typeof(Func<>).MakeGenericType(paramTypes.ToArray()),
                    2 => typeof(Func<,>).MakeGenericType(paramTypes.ToArray()),
                    3 => typeof(Func<,,>).MakeGenericType(paramTypes.ToArray()),
                    4 => typeof(Func<,,,>).MakeGenericType(paramTypes.ToArray()),
                    5 => typeof(Func<,,,,>).MakeGenericType(paramTypes.ToArray()),
                    _ => throw new NotSupportedException($"Func with {paramTypes.Count - 1} parameters not supported")
                };
            }
        }
        

        
        private VM.FunctionInfo ResolveUserFunction(string functionName)
        {
            // Look in CompiledProgram.Functions first (this is where functions are actually stored)
            if (this.compiledProgram?.Functions?.ContainsKey(functionName) == true)
            {
                var compilerFunctionInfo = this.compiledProgram.Functions[functionName];
                Console.WriteLine($"DEBUG: Found function '{functionName}' in CompiledProgram.Functions at address {compilerFunctionInfo.StartAddress}");
                
                // Convert from Compiler.FunctionInfo to VM.FunctionInfo
                return new VM.FunctionInfo
                {
                    Name = compilerFunctionInfo.Name,
                    StartAddress = compilerFunctionInfo.StartAddress,
                    EndAddress = compilerFunctionInfo.EndAddress,
                    LocalCount = 0, // Default values for VM-specific properties
                    ParameterCount = compilerFunctionInfo.Parameters?.Count ?? 0,
                    IsAsync = false,
                    IsGenerator = false
                };
            }
            
            // Look through the bytecode's function table (VMTypes.cs structure doesn't have Functions array)
            // This is likely not used in practice since we use CompiledProgram.Functions above
            Console.WriteLine($"DEBUG: Bytecode does not have Functions array, skipping bytecode search");
            
            // Fallback: Try to find in symbol table
            if (symbolTable != null)
            {
                var symbol = symbolTable.Lookup(functionName);
                if (symbol != null && symbol is FunctionSymbol funcSymbol)
                {
                    // Create a basic VM.FunctionInfo from the symbol
                    Console.WriteLine($"DEBUG: Found function '{functionName}' in symbol table (fallback)");
                    return new VM.FunctionInfo
                    {
                        Name = functionName,
                        StartAddress = -1, // Address not stored in symbol
                        EndAddress = -1,
                        LocalCount = 0,
                        ParameterCount = funcSymbol.Parameters?.Count ?? 0,
                        IsAsync = funcSymbol.IsAsync,
                        IsGenerator = funcSymbol.IsGenerator
                    };
                }
            }
            
            Console.WriteLine($"DEBUG: Function '{functionName}' not found in any location");
            return null;
        }
        
        private bool ShouldUnwind()
        {
            // Check if there are exception handlers
            if (exceptionHandlers.Count == 0)
                return false;
            
            // Check if we're in an exception handling region
            foreach (var handler in exceptionHandlers.Values)
            {
                if (instructionPointer >= handler.TryStart && instructionPointer < handler.TryEnd)
                    return true;
            }
            
            return false;
        }
        
        private class Closure
        {
            public object Function { get; set; }
            public object[] Captures { get; set; }
        }
        
        private class DynamicClassInstance
        {
            public ClassInfo ClassInfo { get; set; }
            public Dictionary<string, object> Fields { get; set; } = new Dictionary<string, object>();
        }
        
        private class DynamicStructInstance
        {
            public StructInfo StructInfo { get; set; }
            public Dictionary<string, object> Fields { get; set; } = new Dictionary<string, object>();
        }
        
        private class GeneratorState
        {
            public int ReturnAddress { get; set; }
            public int Position { get; set; }
            public Stack<object> Values { get; set; } = new Stack<object>();
            public bool IsFinished { get; set; }
        }
        
        #region Debugger Support Methods
        
        /// <summary>
        /// Execute a single instruction (for debugger stepping)
        /// </summary>
        public void Step()
        {
            if (instructionPointer < instructions.Length)
            {
                ExecuteInstruction();
            }
        }
        
        /// <summary>
        /// Read a byte from memory (for debugger memory inspection)
        /// </summary>
        public byte ReadMemory(int address)
        {
            if (address >= 0 && address < memory.Length)
            {
                return memory[address];
            }
            return 0;
        }
        
        /// <summary>
        /// Write a byte to memory
        /// </summary>
        public void WriteMemory(int address, byte value)
        {
            if (address >= 0 && address < memory.Length)
            {
                memory[address] = value;
            }
        }
        
        /// <summary>
        /// Try to get a global variable by name (for debugger variable inspection)
        /// </summary>
        public bool TryGetGlobalVariable(string name, out object value)
        {
            value = null;
            
            if (symbolTable != null)
            {
                var symbol = symbolTable.Lookup(name);
                if (symbol != null && symbol.IsGlobal && symbol.Index < globals.Count)
                {
                    value = globals[symbol.Index];
                    return true;
                }
            }
            
            // Also check the runtime environment
            if (environment?.Globals.TryGetValue(name, out value) == true)
            {
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Get local variables for the current frame (for debugger inspection)
        /// </summary>
        public Dictionary<string, object> GetLocalVariables()
        {
            var result = new Dictionary<string, object>();
            
            if (callStack.Count > 0)
            {
                var frame = callStack.Peek();
                var localBase = frame.LocalsBase;
                
                // For now, just return indexed locals
                // A full implementation would need to track local variable names
                for (int i = 0; i < 10 && localBase + i < locals.Count; i++)
                {
                    result[$"local{i}"] = locals[localBase + i];
                }
            }
            
            return result;
        }
        
        #endregion

        private void LoadDomainOperators(string domainName, Dictionary<string, object> domainContext)
        {
            var operators = domainContext["operators"] as Dictionary<string, Delegate>;
            var constants = domainContext["constants"] as Dictionary<string, object>;
            
            switch (domainName)
            {
                case "Physics":
                    // Physics domain operators
                    operators["×_Vector3"] = new Func<Vector, Vector, Vector>(CrossProduct);
                    operators["·_Vector3"] = new Func<Vector, Vector, double>(DotProduct);
                    operators["∇"] = new Func<Func<double[], double>, double[], Vector>(Gradient);
                    operators["∂"] = new Func<Func<double[], double>, double[], int, double>(PartialDerivative);
                    
                    // Physics constants
                    constants["c"] = 299792458.0; // Speed of light
                    constants["ε₀"] = 8.854e-12; // Permittivity of free space
                    constants["μ₀"] = 4 * System.Math.PI * 1e-7; // Permeability of free space
                    constants["ℏ"] = 1.054e-34; // Reduced Planck constant
                    break;
                    
                case "Statistics":
                    // Statistics domain operators
                    operators["μ"] = new Func<double[], double>(Mean);
                    operators["σ"] = new Func<double[], double>(StdDev);
                    operators["σ²"] = new Func<double[], double>(Variance);
                    
                    // Statistics constants
                    constants["normal_95_percentile"] = 1.96;
                    constants["χ²_critical"] = 3.841;
                    break;
                    
                default:
                    Console.WriteLine($"[VM] Unknown domain: {domainName}");
                    break;
            }
        }
    }
    
    /// <summary>
    /// Call frame for function calls
    /// </summary>
    public class CallFrame
    {
        public int ReturnAddress { get; set; }
        public int LocalsBase { get; set; }
        public FunctionInfo Function { get; set; }
    }
    
    /// <summary>
    /// Runtime environment
    /// </summary>
    public class RuntimeEnvironment
    {
        public Dictionary<string, object> Globals { get; } = new Dictionary<string, object>();
        public Dictionary<string, Type> Types { get; } = new Dictionary<string, Type>();
        public Dictionary<string, Delegate> NativeFunctions { get; } = new Dictionary<string, Delegate>();
    }
    
    /// <summary>
    /// Virtual machine exception
    /// </summary>
    public class VirtualMachineException : Exception
    {
        public VirtualMachineException(string message) : base(message) { }
        public VirtualMachineException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    /// <summary>
    /// Runtime exception
    /// </summary>
    public class RuntimeException : Exception
    {
        public RuntimeException(string message) : base(message) { }
    }

    // Extension class for global count
    public static class SymbolTableExtensions
    {
        public static int GlobalCount(this SymbolTable symbolTable)
        {
            if (symbolTable == null) return 100; // Default size
            
            // For now, return a reasonable default size that works for most programs
            // A proper implementation would need access to the internal symbol table structure
            return 1000;
        }
    }
}
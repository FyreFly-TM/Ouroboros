using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ouroboros.Core;
using Ouroboros.Core.VM;

namespace Ouroboros.Tools.Debug
{
    /// <summary>
    /// Interactive debugger for Ouroboros programs
    /// </summary>
    public class Debugger
    {
        private VirtualMachine vm;
        private Dictionary<int, Breakpoint> breakpoints;
        private Dictionary<string, WatchExpression> watches;
        private Stack<CallFrame> callStack;
        private bool isRunning;
        private bool stepMode;
        private int currentLine;
        private string currentFile;
        
        public Debugger(VirtualMachine virtualMachine)
        {
            vm = virtualMachine;
            breakpoints = new Dictionary<int, Breakpoint>();
            watches = new Dictionary<string, WatchExpression>();
            callStack = new Stack<CallFrame>();
            isRunning = false;
            stepMode = false;
        }
        
        /// <summary>
        /// Start the debugger session
        /// </summary>
        public void Start(string programFile)
        {
            Console.WriteLine("Ouroboros Debugger v1.0");
            Console.WriteLine("Type 'help' for available commands");
            Console.WriteLine($"Loading program: {programFile}");
            
            // Load the program
            LoadProgram(programFile);
            
            // Enter interactive mode
            InteractiveLoop();
        }
        
        private void LoadProgram(string programFile)
        {
            try
            {
                // This would load and prepare the program for debugging
                currentFile = programFile;
                Console.WriteLine($"Program loaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading program: {ex.Message}");
            }
        }
        
        private void InteractiveLoop()
        {
            while (true)
            {
                Console.Write("(ouro-dbg) ");
                string input = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(input))
                    continue;
                
                string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string command = parts[0].ToLower();
                string[] args = parts.Skip(1).ToArray();
                
                try
                {
                    switch (command)
                    {
                        case "help":
                        case "h":
                            ShowHelp();
                            break;
                            
                        case "run":
                        case "r":
                            Run();
                            break;
                            
                        case "continue":
                        case "c":
                            Continue();
                            break;
                            
                        case "step":
                        case "s":
                            Step();
                            break;
                            
                        case "next":
                        case "n":
                            Next();
                            break;
                            
                        case "break":
                        case "b":
                            SetBreakpoint(args);
                            break;
                            
                        case "delete":
                        case "d":
                            DeleteBreakpoint(args);
                            break;
                            
                        case "list":
                        case "l":
                            ListCode(args);
                            break;
                            
                        case "print":
                        case "p":
                            PrintVariable(args);
                            break;
                            
                        case "watch":
                        case "w":
                            AddWatch(args);
                            break;
                            
                        case "unwatch":
                            RemoveWatch(args);
                            break;
                            
                        case "stack":
                        case "bt":
                            ShowCallStack();
                            break;
                            
                        case "locals":
                            ShowLocalVariables();
                            break;
                            
                        case "registers":
                        case "reg":
                            ShowRegisters();
                            break;
                            
                        case "memory":
                        case "mem":
                            ShowMemory(args);
                            break;
                            
                        case "disassemble":
                        case "dis":
                            Disassemble(args);
                            break;
                            
                        case "quit":
                        case "q":
                            Console.WriteLine("Exiting debugger...");
                            return;
                            
                        default:
                            Console.WriteLine($"Unknown command: {command}. Type 'help' for available commands.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
        
        private void ShowHelp()
        {
            Console.WriteLine(@"
Ouroboros Debugger Commands:

Execution Control:
  run, r              Start or restart program execution
  continue, c         Continue execution until next breakpoint
  step, s             Step into next instruction
  next, n             Step over next instruction
  finish              Run until current function returns

Breakpoints:
  break, b <line>     Set breakpoint at line number
  break, b <func>     Set breakpoint at function
  delete, d <num>     Delete breakpoint number
  delete, d all       Delete all breakpoints
  info breakpoints    List all breakpoints

Code Display:
  list, l             List source code around current line
  list, l <line>      List source code around specified line
  list, l <func>      List source code for function
  disassemble, dis    Show disassembled bytecode

Data Inspection:
  print, p <var>      Print variable value
  print, p <expr>     Evaluate and print expression
  locals              Show all local variables
  globals             Show all global variables
  watch, w <expr>     Add expression to watch list
  unwatch <expr>      Remove expression from watch list
  info watches        Show all watches

Stack and Memory:
  stack, bt           Show call stack backtrace
  frame <num>         Select stack frame
  registers, reg      Show virtual machine registers
  memory, mem <addr>  Show memory at address

Other:
  help, h             Show this help message
  quit, q             Exit debugger
");
        }
        
        private void Run()
        {
            Console.WriteLine("Starting program execution...");
            isRunning = true;
            currentLine = 1;
            
            // Reset VM state
            callStack.Clear();
            
            // Start execution
            ExecuteUntilBreakpoint();
        }
        
        private void Continue()
        {
            if (!isRunning)
            {
                Console.WriteLine("Program is not running. Use 'run' to start.");
                return;
            }
            
            Console.WriteLine("Continuing execution...");
            ExecuteUntilBreakpoint();
        }
        
        private void Step()
        {
            if (!isRunning)
            {
                Console.WriteLine("Program is not running. Use 'run' to start.");
                return;
            }
            
            stepMode = true;
            ExecuteSingleStep();
            ShowCurrentLocation();
        }
        
        private void Next()
        {
            if (!isRunning)
            {
                Console.WriteLine("Program is not running. Use 'run' to start.");
                return;
            }
            
            int currentDepth = callStack.Count;
            
            do
            {
                ExecuteSingleStep();
            } while (callStack.Count > currentDepth && isRunning);
            
            ShowCurrentLocation();
        }
        
        private void SetBreakpoint(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: break <line> or break <function>");
                return;
            }
            
            if (int.TryParse(args[0], out int line))
            {
                var bp = new Breakpoint
                {
                    Id = breakpoints.Count + 1,
                    Line = line,
                    File = currentFile,
                    Enabled = true
                };
                
                breakpoints[bp.Id] = bp;
                Console.WriteLine($"Breakpoint {bp.Id} set at line {line}");
            }
            else
            {
                // Function breakpoint
                var bp = new Breakpoint
                {
                    Id = breakpoints.Count + 1,
                    Function = args[0],
                    Enabled = true
                };
                
                breakpoints[bp.Id] = bp;
                Console.WriteLine($"Breakpoint {bp.Id} set at function '{args[0]}'");
            }
        }
        
        private void DeleteBreakpoint(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: delete <number> or delete all");
                return;
            }
            
            if (args[0].ToLower() == "all")
            {
                breakpoints.Clear();
                Console.WriteLine("All breakpoints deleted");
            }
            else if (int.TryParse(args[0], out int id))
            {
                if (breakpoints.Remove(id))
                {
                    Console.WriteLine($"Breakpoint {id} deleted");
                }
                else
                {
                    Console.WriteLine($"No breakpoint with id {id}");
                }
            }
        }
        
        private void ListCode(string[] args)
        {
            int startLine = currentLine - 5;
            int endLine = currentLine + 5;
            
            if (args.Length > 0 && int.TryParse(args[0], out int line))
            {
                startLine = line - 5;
                endLine = line + 5;
            }
            
            // Read source file and display lines
            try
            {
                string[] lines = File.ReadAllLines(currentFile);
                
                for (int i = Math.Max(0, startLine - 1); i < Math.Min(lines.Length, endLine); i++)
                {
                    string marker = (i + 1) == currentLine ? "=>" : "  ";
                    string breakpointMarker = HasBreakpointAtLine(i + 1) ? "*" : " ";
                    
                    Console.WriteLine($"{breakpointMarker}{marker} {i + 1,4}: {lines[i]}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading source file: {ex.Message}");
            }
        }
        
        private bool HasBreakpointAtLine(int line)
        {
            return breakpoints.Values.Any(bp => bp.Line == line && bp.Enabled);
        }
        
        private void PrintVariable(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: print <variable> or print <expression>");
                return;
            }
            
            string expr = string.Join(" ", args);
            
            try
            {
                // Evaluate expression in current context
                object value = EvaluateExpression(expr);
                Console.WriteLine($"{expr} = {FormatValue(value)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error evaluating expression: {ex.Message}");
            }
        }
        
        private void AddWatch(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: watch <expression>");
                return;
            }
            
            string expr = string.Join(" ", args);
            
            watches[expr] = new WatchExpression
            {
                Expression = expr,
                LastValue = null
            };
            
            Console.WriteLine($"Watch added: {expr}");
        }
        
        private void RemoveWatch(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: unwatch <expression>");
                return;
            }
            
            string expr = string.Join(" ", args);
            
            if (watches.Remove(expr))
            {
                Console.WriteLine($"Watch removed: {expr}");
            }
            else
            {
                Console.WriteLine($"No watch found for: {expr}");
            }
        }
        
        private void ShowCallStack()
        {
            if (callStack.Count == 0)
            {
                Console.WriteLine("Call stack is empty");
                return;
            }
            
            int frameNum = 0;
            foreach (var frame in callStack)
            {
                Console.WriteLine($"#{frameNum++} {frame.Function} at {frame.File}:{frame.Line}");
            }
        }
        
        private void ShowLocalVariables()
        {
            if (callStack.Count == 0)
            {
                Console.WriteLine("No active stack frame");
                return;
            }
            
            var currentFrame = callStack.Peek();
            
            Console.WriteLine("Local variables:");
            foreach (var local in currentFrame.Locals)
            {
                Console.WriteLine($"  {local.Key} = {FormatValue(local.Value)}");
            }
        }
        
        private void ShowRegisters()
        {
            Console.WriteLine("Virtual Machine Registers:");
            Console.WriteLine($"  PC (Program Counter): {vm.ProgramCounter}");
            Console.WriteLine($"  SP (Stack Pointer): {vm.StackPointer}");
            Console.WriteLine($"  FP (Frame Pointer): {vm.FramePointer}");
            Console.WriteLine($"  Accumulator: {FormatValue(vm.Accumulator)}");
        }
        
        private void ShowMemory(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: memory <address> [count]");
                return;
            }
            
            if (!int.TryParse(args[0], out int address))
            {
                Console.WriteLine("Invalid address");
                return;
            }
            
            int count = args.Length > 1 && int.TryParse(args[1], out int c) ? c : 16;
            
            Console.WriteLine($"Memory at 0x{address:X4}:");
            
            for (int i = 0; i < count; i += 16)
            {
                Console.Write($"0x{address + i:X4}: ");
                
                // Hex values
                for (int j = 0; j < 16 && i + j < count; j++)
                {
                    Console.Write($"{vm.ReadMemory(address + i + j):X2} ");
                }
                
                Console.WriteLine();
            }
        }
        
        private void Disassemble(string[] args)
        {
            int count = args.Length > 0 && int.TryParse(args[0], out int c) ? c : 10;
            
            Console.WriteLine("Disassembled bytecode:");
            
            for (int i = 0; i < count; i++)
            {
                int pc = vm.ProgramCounter + i;
                Opcode opcode = (Opcode)vm.ReadMemory(pc);
                
                Console.WriteLine($"0x{pc:X4}: {opcode}");
            }
        }
        
        private void ExecuteUntilBreakpoint()
        {
            while (isRunning && !CheckBreakpoint())
            {
                ExecuteSingleStep();
                UpdateWatches();
            }
            
            if (isRunning)
            {
                ShowCurrentLocation();
                ShowWatchedValues();
            }
        }
        
        private void ExecuteSingleStep()
        {
            try
            {
                // Execute one VM instruction
                vm.Step();
                
                // Update current location
                UpdateCurrentLocation();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Runtime error: {ex.Message}");
                isRunning = false;
            }
        }
        
        private bool CheckBreakpoint()
        {
            foreach (var bp in breakpoints.Values.Where(b => b.Enabled))
            {
                if (bp.Line == currentLine && bp.File == currentFile)
                {
                    Console.WriteLine($"Breakpoint {bp.Id} hit at line {bp.Line}");
                    return true;
                }
            }
            
            return false;
        }
        
        private void UpdateCurrentLocation()
        {
            // This would get the current source location from the VM
            // For now, just increment line
            currentLine++;
        }
        
        private void ShowCurrentLocation()
        {
            Console.WriteLine($"Stopped at {currentFile}:{currentLine}");
            ListCode(new[] { currentLine.ToString() });
        }
        
        private void UpdateWatches()
        {
            foreach (var watch in watches.Values)
            {
                try
                {
                    object newValue = EvaluateExpression(watch.Expression);
                    if (!Equals(newValue, watch.LastValue))
                    {
                        Console.WriteLine($"Watch '{watch.Expression}' changed: {FormatValue(watch.LastValue)} -> {FormatValue(newValue)}");
                        watch.LastValue = newValue;
                    }
                }
                catch
                {
                    // Ignore evaluation errors during execution
                }
            }
        }
        
        private void ShowWatchedValues()
        {
            if (watches.Count > 0)
            {
                Console.WriteLine("\nWatched expressions:");
                foreach (var watch in watches.Values)
                {
                    try
                    {
                        object value = EvaluateExpression(watch.Expression);
                        Console.WriteLine($"  {watch.Expression} = {FormatValue(value)}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  {watch.Expression} = <error: {ex.Message}>");
                    }
                }
            }
        }
        
        private object EvaluateExpression(string expr)
        {
            // Evaluate the expression in the current context
            
            // First check if it's a simple variable name
            if (IsSimpleIdentifier(expr))
            {
                // Check locals first
                if (callStack.Count > 0)
                {
                    var frame = callStack.Peek();
                    if (frame.Locals != null && frame.Locals.TryGetValue(expr, out object localValue))
                    {
                        return localValue;
                    }
                    if (frame.Arguments != null && frame.Arguments.TryGetValue(expr, out object argValue))
                    {
                        return argValue;
                    }
                }
                
                // Check globals
                if (vm.TryGetGlobalVariable(expr, out object globalValue))
                {
                    return globalValue;
                }
                
                throw new Exception($"Variable '{expr}' not found");
            }
            
            // Handle array/property access (e.g., "arr[0]" or "obj.field")
            if (expr.Contains('[') || expr.Contains('.'))
            {
                return EvaluateComplexExpression(expr);
            }
            
            // Handle arithmetic expressions
            if (ContainsOperator(expr))
            {
                return EvaluateArithmeticExpression(expr);
            }
            
            // Try to parse as literal
            if (int.TryParse(expr, out int intVal))
                return intVal;
            if (double.TryParse(expr, out double doubleVal))
                return doubleVal;
            if (expr.StartsWith("\"") && expr.EndsWith("\""))
                return expr.Substring(1, expr.Length - 2);
            if (expr == "true")
                return true;
            if (expr == "false")
                return false;
            if (expr == "null")
                return null;
            
            throw new Exception($"Unable to evaluate expression: {expr}");
        }
        
        private bool IsSimpleIdentifier(string expr)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(expr, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
        }
        
        private bool ContainsOperator(string expr)
        {
            return expr.Contains('+') || expr.Contains('-') || expr.Contains('*') || 
                   expr.Contains('/') || expr.Contains('%') || expr.Contains('=');
        }
        
        private object EvaluateComplexExpression(string expr)
        {
            // Simplified implementation for array/property access
            if (expr.Contains('['))
            {
                int bracketIndex = expr.IndexOf('[');
                string varName = expr.Substring(0, bracketIndex);
                string indexStr = expr.Substring(bracketIndex + 1, expr.IndexOf(']') - bracketIndex - 1);
                
                object array = EvaluateExpression(varName);
                int index = (int)EvaluateExpression(indexStr);
                
                if (array is Array arr)
                {
                    return arr.GetValue(index);
                }
                else if (array is System.Collections.IList list)
                {
                    return list[index];
                }
                
                throw new Exception($"Cannot index into non-array type");
            }
            else if (expr.Contains('.'))
            {
                // Property access - simplified implementation
                int dotIndex = expr.IndexOf('.');
                string objName = expr.Substring(0, dotIndex);
                string propName = expr.Substring(dotIndex + 1);
                
                object obj = EvaluateExpression(objName);
                if (obj != null)
                {
                    var type = obj.GetType();
                    var prop = type.GetProperty(propName);
                    if (prop != null)
                    {
                        return prop.GetValue(obj);
                    }
                    
                    var field = type.GetField(propName);
                    if (field != null)
                    {
                        return field.GetValue(obj);
                    }
                }
                
                throw new Exception($"Property '{propName}' not found");
            }
            
            return expr;
        }
        
        private object EvaluateArithmeticExpression(string expr)
        {
            // Very simplified arithmetic evaluation
            // In a real implementation, this would use a proper expression parser
            
            // For now, just handle simple binary operations
            foreach (char op in new[] { '+', '-', '*', '/', '%' })
            {
                int opIndex = expr.IndexOf(op);
                if (opIndex > 0 && opIndex < expr.Length - 1)
                {
                    string leftStr = expr.Substring(0, opIndex).Trim();
                    string rightStr = expr.Substring(opIndex + 1).Trim();
                    
                    object left = EvaluateExpression(leftStr);
                    object right = EvaluateExpression(rightStr);
                    
                    if (left is int li && right is int ri)
                    {
                        return op switch
                        {
                            '+' => li + ri,
                            '-' => li - ri,
                            '*' => li * ri,
                            '/' => li / ri,
                            '%' => li % ri,
                            _ => throw new Exception($"Unknown operator: {op}")
                        };
                    }
                    else if ((left is double || left is int) && (right is double || right is int))
                    {
                        double ld = Convert.ToDouble(left);
                        double rd = Convert.ToDouble(right);
                        
                        return op switch
                        {
                            '+' => ld + rd,
                            '-' => ld - rd,
                            '*' => ld * rd,
                            '/' => ld / rd,
                            '%' => ld % rd,
                            _ => throw new Exception($"Unknown operator: {op}")
                        };
                    }
                }
            }
            
            throw new Exception($"Unable to evaluate arithmetic expression: {expr}");
        }
        
        private string FormatValue(object value)
        {
            if (value == null)
                return "null";
            
            if (value is string str)
                return $"\"{str}\"";
            
            if (value is Array arr)
                return $"[{string.Join(", ", arr.Cast<object>().Take(10))}...]";
            
            return value.ToString();
        }
        
        private class Breakpoint
        {
            public int Id { get; set; }
            public int Line { get; set; }
            public string File { get; set; }
            public string Function { get; set; }
            public bool Enabled { get; set; }
            public string Condition { get; set; }
            public int HitCount { get; set; }
        }
        
        private class WatchExpression
        {
            public string Expression { get; set; }
            public object LastValue { get; set; }
        }
        
        private class CallFrame
        {
            public string Function { get; set; }
            public string File { get; set; }
            public int Line { get; set; }
            public Dictionary<string, object> Locals { get; set; }
            public Dictionary<string, object> Arguments { get; set; }
        }
    }
    
    public class DebuggerProgram
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: ouro-debug <program-file>");
                return;
            }
            
            var vm = new VirtualMachine();
            var debugger = new Debugger(vm);
            debugger.Start(args[0]);
        }
    }
} 
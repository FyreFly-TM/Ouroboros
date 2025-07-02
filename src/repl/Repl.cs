using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ouroboros.Core.Lexer;
using Ouroboros.Core.Parser;
using Ouroboros.Core.Compiler;
using Ouroboros.Core.VM;
using Ouroboros.Runtime;
using Ouroboros.Tokens;

namespace Ouroboros.REPL
{
    /// <summary>
    /// Ouroboros REPL (Read-Eval-Print Loop)
    /// </summary>
    public class Repl
    {
        private readonly ReplContext context;
        private readonly VirtualMachine vm;
        private readonly Compiler compiler;
        private readonly ReplCommands commands;
        private readonly History history;
        private readonly CompletionProvider completionProvider;
        private bool isRunning;
        private CancellationTokenSource? cancellationTokenSource;

        public Repl()
        {
            context = new ReplContext();
            vm = new VirtualMachine();
            compiler = new Compiler();
            commands = new ReplCommands(this);
            history = new History(100);
            completionProvider = new CompletionProvider(context);
        }

        public ReplContext Context => context;

        /// <summary>
        /// Run the REPL
        /// </summary>
        public async Task RunAsync()
        {
            PrintWelcome();
            isRunning = true;
            cancellationTokenSource = new CancellationTokenSource();

            while (isRunning && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var input = await ReadInputAsync();
                    if (input == null)
                        break;

                    if (ProcessCommand(input))
                        continue;

                    var result = await EvaluateAsync(input);
                    if (result != null)
                    {
                        PrintResult(result);
                    }
                }
                catch (Exception ex)
                {
                    PrintError(ex);
                }
            }

            PrintGoodbye();
        }

        /// <summary>
        /// Stop the REPL
        /// </summary>
        public void Stop()
        {
            isRunning = false;
            cancellationTokenSource?.Cancel();
        }

        private void PrintWelcome()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║         Ouroboros Interactive REPL v1.0                   ║");
            Console.WriteLine("║                                                           ║");
            Console.WriteLine("║   Type 'help' for commands, 'exit' to quit               ║");
            Console.WriteLine("║   Multi-line input: end with blank line                  ║");
            Console.WriteLine("║   Tab completion available                               ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
        }

        private void PrintGoodbye()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\nGoodbye!");
            Console.ResetColor();
        }

        private async Task<string?> ReadInputAsync()
        {
            var inputBuilder = new StringBuilder();
            var lineCount = 0;
            var isMultiline = false;

            while (true)
            {
                // Print prompt
                Console.ForegroundColor = ConsoleColor.Green;
                if (lineCount == 0)
                {
                    Console.Write("ouroboros> ");
                }
                else
                {
                    Console.Write("        ...> ");
                }
                Console.ResetColor();

                // Read line with completion support
                var line = await ReadLineWithCompletionAsync();
                
                if (line == null) // Ctrl+C or EOF
                {
                    return null;
                }

                if (string.IsNullOrWhiteSpace(line) && isMultiline)
                {
                    // Empty line ends multi-line input
                    break;
                }

                inputBuilder.AppendLine(line);
                lineCount++;

                // Check if we need more input
                var partial = inputBuilder.ToString();
                if (!RequiresMoreInput(partial))
                {
                    break;
                }
                
                isMultiline = true;
            }

            var input = inputBuilder.ToString().Trim();
            if (!string.IsNullOrEmpty(input))
            {
                history.Add(input);
            }

            return input;
        }

        private async Task<string?> ReadLineWithCompletionAsync()
        {
            var input = new StringBuilder();
            var cursorPosition = 0;
            var completions = new List<string>();
            var completionIndex = -1;

            while (true)
            {
                var key = Console.ReadKey(intercept: true);

                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        Console.WriteLine();
                        return input.ToString();

                    case ConsoleKey.Tab:
                        var completionState = new CompletionState 
                        { 
                            CursorPosition = cursorPosition, 
                            CompletionIndex = completionIndex 
                        };
                        await HandleTabCompletionAsync(input, completions, completionState);
                        cursorPosition = completionState.CursorPosition;
                        completionIndex = completionState.CompletionIndex;
                        break;

                    case ConsoleKey.Backspace:
                        if (cursorPosition > 0)
                        {
                            input.Remove(cursorPosition - 1, 1);
                            cursorPosition--;
                            RedrawLine(input.ToString(), cursorPosition);
                        }
                        break;

                    case ConsoleKey.Delete:
                        if (cursorPosition < input.Length)
                        {
                            input.Remove(cursorPosition, 1);
                            RedrawLine(input.ToString(), cursorPosition);
                        }
                        break;

                    case ConsoleKey.LeftArrow:
                        if (cursorPosition > 0)
                        {
                            cursorPosition--;
                            Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        }
                        break;

                    case ConsoleKey.RightArrow:
                        if (cursorPosition < input.Length)
                        {
                            cursorPosition++;
                            Console.SetCursorPosition(Console.CursorLeft + 1, Console.CursorTop);
                        }
                        break;

                    case ConsoleKey.UpArrow:
                        var previous = history.GetPrevious();
                        if (previous != null)
                        {
                            input.Clear();
                            input.Append(previous);
                            cursorPosition = input.Length;
                            RedrawLine(input.ToString(), cursorPosition);
                        }
                        break;

                    case ConsoleKey.DownArrow:
                        var next = history.GetNext();
                        if (next != null)
                        {
                            input.Clear();
                            input.Append(next);
                            cursorPosition = input.Length;
                            RedrawLine(input.ToString(), cursorPosition);
                        }
                        break;

                    case ConsoleKey.Home:
                        cursorPosition = 0;
                        Console.SetCursorPosition(11, Console.CursorTop); // After prompt
                        break;

                    case ConsoleKey.End:
                        cursorPosition = input.Length;
                        Console.SetCursorPosition(11 + input.Length, Console.CursorTop);
                        break;

                    case ConsoleKey.Escape:
                        input.Clear();
                        cursorPosition = 0;
                        RedrawLine("", 0);
                        break;

                    case ConsoleKey.C when key.Modifiers.HasFlag(ConsoleModifiers.Control):
                        Console.WriteLine("^C");
                        return null;

                    default:
                        if (!char.IsControl(key.KeyChar))
                        {
                            input.Insert(cursorPosition, key.KeyChar);
                            cursorPosition++;
                            RedrawLine(input.ToString(), cursorPosition);
                        }
                        break;
                }
            }
        }

        private class CompletionState
        {
            public int CursorPosition { get; set; }
            public int CompletionIndex { get; set; }
        }

        private async Task HandleTabCompletionAsync(StringBuilder input, 
            List<string> completions, CompletionState state)
        {
            if (state.CompletionIndex == -1)
            {
                // First tab - get completions
                var partial = input.ToString().Substring(0, state.CursorPosition);
                completions = await completionProvider.GetCompletionsAsync(partial);
                state.CompletionIndex = 0;
            }
            else
            {
                // Subsequent tabs - cycle through completions
                state.CompletionIndex = (state.CompletionIndex + 1) % completions.Count;
            }

            if (completions.Any())
            {
                // Apply completion
                var completion = completions[state.CompletionIndex];
                var lastWord = GetLastWord(input.ToString(), state.CursorPosition);
                
                // Replace last word with completion
                var startPos = state.CursorPosition - lastWord.Length;
                input.Remove(startPos, lastWord.Length);
                input.Insert(startPos, completion);
                state.CursorPosition = startPos + completion.Length;
                
                RedrawLine(input.ToString(), state.CursorPosition);
            }
        }

        private string GetLastWord(string input, int position)
        {
            var start = position;
            while (start > 0 && !char.IsWhiteSpace(input[start - 1]))
            {
                start--;
            }
            return input.Substring(start, position - start);
        }

        private void RedrawLine(string line, int cursorPosition)
        {
            var currentLeft = Console.CursorLeft;
            var currentTop = Console.CursorTop;
            
            // Clear current line from prompt position
            Console.SetCursorPosition(11, currentTop);
            Console.Write(new string(' ', Console.WindowWidth - 11));
            
            // Write new line
            Console.SetCursorPosition(11, currentTop);
            Console.Write(line);
            
            // Set cursor position
            Console.SetCursorPosition(11 + cursorPosition, currentTop);
        }

        private bool RequiresMoreInput(string input)
        {
            // Simple heuristic - check for unmatched braces/parentheses
            var openBraces = 0;
            var openParens = 0;
            var openBrackets = 0;
            var inString = false;
            var escape = false;

            foreach (var ch in input)
            {
                if (escape)
                {
                    escape = false;
                    continue;
                }

                if (ch == '\\')
                {
                    escape = true;
                    continue;
                }

                if (ch == '"' && !inString)
                {
                    inString = true;
                }
                else if (ch == '"' && inString)
                {
                    inString = false;
                }
                else if (!inString)
                {
                    switch (ch)
                    {
                        case '{': openBraces++; break;
                        case '}': openBraces--; break;
                        case '(': openParens++; break;
                        case ')': openParens--; break;
                        case '[': openBrackets++; break;
                        case ']': openBrackets--; break;
                    }
                }
            }

            return openBraces > 0 || openParens > 0 || openBrackets > 0 || inString;
        }

        private bool ProcessCommand(string input)
        {
            if (!input.StartsWith(':'))
                return false;

            var parts = input.Substring(1).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return true;

            return commands.Execute(parts[0], parts.Skip(1).ToArray());
        }

        public async Task<object?> EvaluateAsync(string input)
        {
            try
            {
                // Lex
                var lexer = new Lexer(input, "<repl>");
                var tokens = lexer.ScanTokens();

                // Simple error check for lexer (since HadError doesn't exist)
                if (tokens.Any(t => t.Type == TokenType.Error))
                {
                    var errorTokens = tokens.Where(t => t.Type == TokenType.Error);
                    var errorMessages = string.Join("\n", errorTokens.Select(t => 
                        $"  Line {t.Line}, Column {t.Column}: Unexpected character '{t.Lexeme}'"));
                    throw new ReplException($"Lexical errors found:\n{errorMessages}");
                }

                // Parse
                var parser = new Parser(tokens);
                var ast = parser.Parse();

                if (parser.HadError)
                {
                    var errors = parser.Errors;
                    throw new ReplException($"Parse errors found:\n{FormatParseErrors(errors)}");
                }

                // Type check
                var typeChecker = new Core.Compiler.TypeChecker();
                try
                {
                    ast = typeChecker.Check(ast);
                }
                catch (Core.Compiler.TypeCheckException ex)
                {
                    throw new ReplException($"Type errors found:\n{FormatTypeErrors(ex.Errors)}");
                }

                // Compile
                var compilerProgram = compiler.Compile(ast);

                // Convert from Compiler.CompiledProgram to VM.CompiledProgram
                var vmProgram = new Core.VM.CompiledProgram
                {
                    Bytecode = new Core.VM.Bytecode
                    {
                        Instructions = compilerProgram.Bytecode.Code.ToArray(),
                        ConstantPool = compilerProgram.Bytecode.Constants.ToArray(),
                        Classes = compilerProgram.Bytecode.Classes?.Select(c => new Core.VM.ClassInfo
                        {
                            Name = c.Name,
                            BaseClass = c.BaseClass,
                            Interfaces = c.Interfaces,
                            Fields = c.Fields?.Select(f => new Core.VM.FieldInfo
                            {
                                Name = f.Name,
                                Type = f.Type,
                                Modifiers = f.Modifiers
                            }).ToList() ?? new List<Core.VM.FieldInfo>(),
                            Methods = c.Methods?.Select(m => new Core.VM.MethodInfo
                            {
                                Name = m.Name,
                                StartAddress = m.StartAddress,
                                EndAddress = m.EndAddress,
                                Modifiers = m.Modifiers
                            }).ToList() ?? new List<Core.VM.MethodInfo>(),
                            Properties = c.Properties?.Select(p => new Core.VM.PropertyInfo
                            {
                                Name = p.Name,
                                Type = p.Type,
                                GetterAddress = p.GetterAddress,
                                GetterEndAddress = p.GetterEndAddress,
                                SetterAddress = p.SetterAddress,
                                SetterEndAddress = p.SetterEndAddress
                            }).ToList() ?? new List<Core.VM.PropertyInfo>()
                        }).ToArray() ?? Array.Empty<Core.VM.ClassInfo>(),
                        Structs = compilerProgram.Bytecode.Structs?.Select(s => new Core.VM.StructInfo
                        {
                            Name = s.Name,
                            Interfaces = s.Interfaces,
                            Fields = s.Fields?.Select(f => new Core.VM.FieldInfo
                            {
                                Name = f.Name,
                                Type = f.Type,
                                Modifiers = f.Modifiers
                            }).ToList() ?? new List<Core.VM.FieldInfo>(),
                            Methods = s.Methods?.Select(m => new Core.VM.MethodInfo
                            {
                                Name = m.Name,
                                StartAddress = m.StartAddress,
                                EndAddress = m.EndAddress,
                                Modifiers = m.Modifiers
                            }).ToList() ?? new List<Core.VM.MethodInfo>()
                        }).ToArray() ?? Array.Empty<Core.VM.StructInfo>(),
                        Enums = compilerProgram.Bytecode.Enums?.Select(e => new Core.VM.EnumInfo
                        {
                            Name = e.Name,
                            UnderlyingType = e.UnderlyingType,
                            Members = e.Members?.Select(m => new Core.VM.EnumMemberInfo
                            {
                                Name = m.Name,
                                Value = m.Value
                            }).ToList() ?? new List<Core.VM.EnumMemberInfo>()
                        }).ToArray() ?? Array.Empty<Core.VM.EnumInfo>()
                    },
                    SymbolTable = new Core.VM.SymbolTable(), // Create new VM SymbolTable - conversion would be complex
                    SourceFile = compilerProgram.SourceFile,
                    Metadata = new Core.VM.ProgramMetadata
                    {
                        Version = compilerProgram.Metadata?.Version ?? "1.0.0",
                        CompilerVersion = "Ouroboros Compiler 1.0",
                        CompileTime = compilerProgram.Metadata?.CompileTime ?? DateTime.Now
                    }
                };

                // Execute with better error handling
                try
                {
                    // Execute the compiled program
                    var result = await Task.Run(() => vm.Execute(vmProgram));
                    
                    // Update context
                    context.LastResult = result;
                    context.UpdateBindings(ast);

                    return result;
                }
                catch (Exception vmEx)
                {
                    throw new ReplException($"Runtime error: {vmEx.Message}", vmEx);
                }
            }
            catch (ReplException)
            {
                // Re-throw REPL exceptions as-is
                throw;
            }
            catch (Exception ex)
            {
                // Wrap unexpected exceptions
                throw new ReplException($"Unexpected error during evaluation: {ex.Message}", ex);
            }
        }

        private string FormatParseErrors(IEnumerable<Core.Parser.ParseException> errors)
        {
            var sb = new StringBuilder();
            foreach (var error in errors.Take(5))
            {
                var location = error.Token != null 
                    ? $"Line {error.Token.Line}, Column {error.Token.Column}" 
                    : "Unknown location";
                sb.AppendLine($"  {location}: {error.Message}");
            }
            if (errors.Count() > 5)
            {
                sb.AppendLine($"  ... and {errors.Count() - 5} more errors");
            }
            return sb.ToString();
        }

        private string FormatTypeErrors(IEnumerable<Core.Compiler.TypeCheckError> errors)
        {
            var sb = new StringBuilder();
            foreach (var error in errors.Take(5))
            {
                sb.AppendLine($"  Line {error.Line}, Column {error.Column}: {error.Message}");
            }
            if (errors.Count() > 5)
            {
                sb.AppendLine($"  ... and {errors.Count() - 5} more errors");
            }
            return sb.ToString();
        }

        public void PrintResult(object result)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("=> ");
            Console.ResetColor();
            
            try
            {
                var formatted = FormatValue(result);
                Console.WriteLine(formatted);
            }
            catch (Exception ex)
            {
                // Fallback if formatting fails
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"<Unable to format result: {ex.Message}>");
                Console.ResetColor();
            }
        }

        public void PrintError(Exception ex)
        {
            if (ex is ReplException replEx)
            {
                // REPL-specific exceptions get special formatting
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error:");
                Console.ResetColor();
                Console.WriteLine(replEx.Message);

                if (replEx.InnerException != null && context.ShowStackTrace)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("\nStack trace:");
                    Console.WriteLine(replEx.InnerException.StackTrace);
                    Console.ResetColor();
                }
            }
            else
            {
                // Generic exceptions
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("Unexpected error: ");
                Console.ResetColor();
                Console.WriteLine(ex.Message);

                if (context.ShowStackTrace)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("\nStack trace:");
                    Console.WriteLine(ex.StackTrace);
                    Console.ResetColor();
                }
            }

            // Suggest help
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("\nType ':help' for assistance or ':clear' to reset.");
            Console.ResetColor();
        }

        private string FormatValue(object? value)
        {
            if (value == null)
                return "null";

            return value switch
            {
                string s => $"\"{s}\"",
                char c => $"'{c}'",
                bool b => b.ToString().ToLower(),
                float f => $"{f}f",
                double d => d.ToString(),
                decimal m => $"{m}m",
                Array arr => FormatArray(arr),
                _ => value.ToString() ?? "null"
            };
        }

        private string FormatArray(Array array)
        {
            var elements = new List<string>();
            var count = Math.Min(array.Length, 10);

            for (int i = 0; i < count; i++)
            {
                elements.Add(FormatValue(array.GetValue(i)));
            }

            if (array.Length > 10)
            {
                elements.Add("...");
            }

            return $"[{string.Join(", ", elements)}]";
        }
    }

    /// <summary>
    /// REPL context
    /// </summary>
    public class ReplContext
    {
        public Dictionary<string, object> Bindings { get; } = new();
        public object? LastResult { get; set; }
        public List<string> ImportedModules { get; } = new();
        public bool ShowStackTrace { get; set; } = false;
        public Dictionary<string, object> Variables { get; } = new();

        public void UpdateBindings(Core.AST.Program ast)
        {
            // Extract variable bindings from the AST
            var bindingVisitor = new BindingExtractor();
            bindingVisitor.Visit(ast);
            
            foreach (var binding in bindingVisitor.Bindings)
            {
                Variables[binding.Key] = binding.Value;
            }
        }
    }

    /// <summary>
    /// Helper to extract variable bindings from AST
    /// </summary>
    internal class BindingExtractor
    {
        public Dictionary<string, object> Bindings { get; } = new();

        public void Visit(Core.AST.Program program)
        {
            foreach (var stmt in program.Statements)
            {
                if (stmt is Core.AST.VariableDeclaration varDecl)
                {
                    // Store variable declaration info
                    Bindings[varDecl.Name] = new VariableInfo
                    {
                        Name = varDecl.Name,
                        Type = varDecl.Type.Name,
                        IsConst = varDecl.IsConst,
                        IsReadonly = varDecl.IsReadonly
                    };
                }
            }
        }
    }

    internal class VariableInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsConst { get; set; }
        public bool IsReadonly { get; set; }

        public override string ToString()
        {
            var modifiers = new List<string>();
            if (IsConst) modifiers.Add("const");
            if (IsReadonly) modifiers.Add("readonly");
            var modStr = modifiers.Any() ? string.Join(" ", modifiers) + " " : "";
            return $"{modStr}{Type} {Name}";
        }
    }

    /// <summary>
    /// REPL commands
    /// </summary>
    public class ReplCommands
    {
        private readonly Repl repl;
        private readonly Dictionary<string, Action<string[]>> commands;

        public ReplCommands(Repl repl)
        {
            this.repl = repl;
            
            commands = new Dictionary<string, Action<string[]>>
            {
                ["help"] = ShowHelp,
                ["exit"] = Exit,
                ["quit"] = Exit,
                ["clear"] = Clear,
                ["reset"] = Reset,
                ["bindings"] = ShowBindings,
                ["imports"] = ShowImports,
                ["history"] = ShowHistory,
                ["save"] = SaveSession,
                ["load"] = LoadSession,
                ["time"] = TimeExecution,
                ["memory"] = ShowMemory,
                ["gc"] = RunGarbageCollection
            };
        }

        public bool Execute(string command, string[] args)
        {
            if (commands.TryGetValue(command.ToLower(), out var action))
            {
                action(args);
                return true;
            }

            Console.WriteLine($"Unknown command: {command}. Type ':help' for available commands.");
            return true;
        }

        private void ShowHelp(string[] args)
        {
            Console.WriteLine("Available commands:");
            Console.WriteLine("  :help              - Show this help message");
            Console.WriteLine("  :exit, :quit       - Exit the REPL");
            Console.WriteLine("  :clear             - Clear the screen");
            Console.WriteLine("  :reset             - Reset the REPL context");
            Console.WriteLine("  :bindings          - Show current variable bindings");
            Console.WriteLine("  :imports           - Show imported modules");
            Console.WriteLine("  :history           - Show command history");
            Console.WriteLine("  :save <file>       - Save session to file");
            Console.WriteLine("  :load <file>       - Load session from file");
            Console.WriteLine("  :time <expr>       - Time expression execution");
            Console.WriteLine("  :memory            - Show memory usage");
            Console.WriteLine("  :gc                - Run garbage collection");
        }

        private void Exit(string[] args)
        {
            repl.Stop();
        }

        private void Clear(string[] args)
        {
            Console.Clear();
        }

        private void Reset(string[] args)
        {
            // Reset REPL context
            Console.WriteLine("REPL context reset.");
        }

        private void ShowBindings(string[] args)
        {
            if (repl.Context.Variables.Count == 0)
            {
                Console.WriteLine("No variables defined.");
                return;
            }

            Console.WriteLine("Variables:");
            foreach (var kvp in repl.Context.Variables)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }
        }

        private void ShowImports(string[] args)
        {
            Console.WriteLine("Imported modules:");
            // Show imports implementation
        }

        private void ShowHistory(string[] args)
        {
            Console.WriteLine("Command history:");
            // Show history implementation
        }

        private void SaveSession(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: :save <filename>");
                return;
            }
            // Save session implementation
        }

        private void LoadSession(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: :load <filename>");
                return;
            }
            // Load session implementation
        }

        private void TimeExecution(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: :time <expression>");
                return;
            }

            var expression = string.Join(" ", args);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                var task = repl.EvaluateAsync(expression);
                task.Wait();
                stopwatch.Stop();
                
                Console.WriteLine($"Execution time: {stopwatch.ElapsedMilliseconds}ms");
                if (task.Result != null)
                {
                    repl.PrintResult(task.Result);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"Execution failed after {stopwatch.ElapsedMilliseconds}ms");
                repl.PrintError(ex.InnerException ?? ex);
            }
        }

        private void ShowMemory(string[] args)
        {
            var gcMemory = GC.GetTotalMemory(false);
            Console.WriteLine($"Memory usage: {gcMemory / 1024 / 1024:F2} MB");
        }

        private void RunGarbageCollection(string[] args)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Console.WriteLine("Garbage collection completed.");
        }
    }

    /// <summary>
    /// Command history
    /// </summary>
    public class History
    {
        private readonly List<string> items = new();
        private readonly int maxSize;
        private int currentIndex = -1;

        public History(int maxSize)
        {
            this.maxSize = maxSize;
        }

        public void Add(string item)
        {
            items.Add(item);
            if (items.Count > maxSize)
            {
                items.RemoveAt(0);
            }
            currentIndex = items.Count;
        }

        public string? GetPrevious()
        {
            if (items.Count == 0 || currentIndex <= 0)
                return null;

            currentIndex--;
            return items[currentIndex];
        }

        public string? GetNext()
        {
            if (items.Count == 0 || currentIndex >= items.Count - 1)
                return null;

            currentIndex++;
            return items[currentIndex];
        }
    }

    /// <summary>
    /// Completion provider
    /// </summary>
    public class CompletionProvider
    {
        private readonly ReplContext context;
        private readonly List<string> keywords = new()
        {
            "if", "else", "while", "for", "function", "class", "return",
            "var", "let", "const", "import", "export", "async", "await",
            "try", "catch", "finally", "throw", "new", "this", "true", "false", "null"
        };

        public CompletionProvider(ReplContext context)
        {
            this.context = context;
        }

        public Task<List<string>> GetCompletionsAsync(string partial)
        {
            var completions = new List<string>();

            // Add matching keywords
            completions.AddRange(keywords.Where(k => k.StartsWith(partial, StringComparison.OrdinalIgnoreCase)));

            // Add matching bindings
            completions.AddRange(context.Bindings.Keys.Where(k => k.StartsWith(partial, StringComparison.OrdinalIgnoreCase)));

            return Task.FromResult(completions.Distinct().OrderBy(c => c).ToList());
        }
    }

    /// <summary>
    /// REPL exception
    /// </summary>
    public class ReplException : Exception
    {
        public ReplException(string message) : base(message) { }
        public ReplException(string message, Exception inner) : base(message, inner) { }
    }
} 
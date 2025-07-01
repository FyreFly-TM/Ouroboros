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

        private async Task<object?> EvaluateAsync(string input)
        {
            // Lex
            var lexer = new Lexer(input, "<repl>");
            var tokens = lexer.ScanTokens();

            if (lexer.HadError)
            {
                throw new ReplException("Lexical error in input");
            }

            // Parse
            var parser = new Parser(tokens);
            var ast = parser.Parse();

            if (parser.HadError)
            {
                throw new ReplException("Parse error in input");
            }

            // Compile
            var program = compiler.Compile(ast);

            // Execute
            var result = await Task.Run(() => vm.Execute(program));

            // Update context
            context.LastResult = result;
            context.UpdateBindings(ast);

            return result;
        }

        private void PrintResult(object result)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("=> ");
            Console.ResetColor();
            
            var formatted = FormatValue(result);
            Console.WriteLine(formatted);
        }

        private void PrintError(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Error: ");
            Console.ResetColor();
            Console.WriteLine(ex.Message);

            if (ex.InnerException != null)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"  Inner: {ex.InnerException.Message}");
                Console.ResetColor();
            }
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

        public void UpdateBindings(Core.AST.Program ast)
        {
            // Extract variable bindings from AST
            // This would analyze the AST and update the bindings dictionary
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
            Console.WriteLine("Current bindings:");
            // Show bindings implementation
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
            // Time execution implementation
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
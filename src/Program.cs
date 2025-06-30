using System;
using System.IO;
using System.Text;
using Ouroboros.Core.Lexer;
using Ouroboros.Core.Parser;
using Ouroboros.Core.Compiler;
using Ouroboros.Runtime;

namespace Ouroboros
{
    /// <summary>
    /// Main entry point for the Ouroboros programming language
    /// </summary>
    public class Program
    {
        private static Runtime.Runtime runtime;
        
        public static void Main(string[] args)
        {
            Console.WriteLine("DEBUG: Ouroboros compiler starting...");
            
            System.Console.WriteLine("Ouroboros Programming Language v1.0.0");
            System.Console.WriteLine("=====================================\n");

            if (args.Length == 0)
            {
                ShowHelp();
                return;
            }

            var command = args[0].ToLower();
            
            switch (command)
            {
                case "-h":
                case "--help":
                    ShowHelp();
                    break;
                    
                case "-v":
                case "--version":
                    ShowVersion();
                    break;
                    
                default:
                    // Try to execute the file
                    ExecuteFile(args[0]);
                    break;
            }
        }

        static void ExecuteFile(string filePath)
        {
            try
            {
                Console.WriteLine("DEBUG: Starting ExecuteFile method");
                
                // Check if file exists
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Error: File '{filePath}' not found.");
                    Environment.Exit(1);
                    return;
                }

                Console.WriteLine("DEBUG: File exists, checking extension");
                
                // Check file extension
                if (!filePath.EndsWith(".ouro", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Warning: File '{filePath}' does not have .ouro extension.");
                }

                Console.WriteLine($"Executing: {filePath}");
                Console.WriteLine();

                Console.WriteLine("DEBUG: Reading source code");
                
                // Read source code
                string sourceCode = File.ReadAllText(filePath, Encoding.UTF8);
                Console.WriteLine($"DEBUG: Read {sourceCode.Length} characters from file");

                Console.WriteLine("DEBUG: Initializing runtime");
                
                // Initialize runtime
                var runtimeOptions = new RuntimeOptions
                {
                    EnableJit = true,
                    EnableDebugging = false,
                    EnableProfiling = false
                };
                runtime = new Runtime.Runtime(runtimeOptions);

                Console.WriteLine("DEBUG: Starting compilation");
                
                // Compile and execute
                var compiledProgram = CompileSource(sourceCode, filePath);
                
                Console.WriteLine($"DEBUG: Compilation result: {(compiledProgram != null ? "Success" : "Failed")}");
                
                if (compiledProgram != null)
                {
                    Console.WriteLine("Compilation successful. Starting execution...");
                    Console.WriteLine(new string('=', 50));
                    
                    // Debug: Show bytecode info
                    Console.WriteLine($"Bytecode length: {compiledProgram.Bytecode.Code.Count} bytes");
                    if (compiledProgram.Bytecode.Code.Count > 0)
                    {
                        Console.Write("Bytecode: ");
                        for (int i = 0; i < Math.Min(20, compiledProgram.Bytecode.Code.Count); i++)
                        {
                            Console.Write($"{compiledProgram.Bytecode.Code[i]:X2} ");
                        }
                        if (compiledProgram.Bytecode.Code.Count > 20) Console.Write("...");
                        Console.WriteLine();
                    }
                    Console.WriteLine();
                    
                    // Execute the compiled program
                    var result = runtime.Execute(compiledProgram);
                    
                    Console.WriteLine();
                    if (result != null)
                    {
                        Console.WriteLine($"Program result: {result}");
                    }
                    
                    Console.WriteLine(new string('=', 50));
                    Console.WriteLine("Execution completed.");
                }
                else
                {
                    Console.WriteLine("DEBUG: Compilation failed, no program to execute");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing file '{filePath}': {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                
                // Show stack trace in debug mode
                #if DEBUG
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                #endif
                
                Environment.Exit(1);
            }
            finally
            {
                Console.WriteLine("DEBUG: Cleanup - shutting down runtime");
                
                // Clean up runtime
                runtime?.Shutdown();
            }
        }

        static CompiledProgram CompileSource(string sourceCode, string fileName)
        {
            try
            {
                Console.WriteLine("Starting compilation...");
                Console.WriteLine("DEBUG: Entering CompileSource method");
                
                // Step 1: Lexical Analysis
                Console.WriteLine("1. Lexical analysis...");
                Console.WriteLine("DEBUG: Creating lexer");
                
                var lexer = new Lexer(sourceCode, fileName);
                Console.WriteLine("DEBUG: Calling ScanTokens");
                
                var tokens = lexer.ScanTokens();
                Console.WriteLine($"   Generated {tokens.Count} tokens");
                Console.WriteLine("DEBUG: Lexical analysis completed successfully");
                
                // Step 2: Syntax Analysis
                Console.WriteLine("2. Syntax analysis...");
                Console.WriteLine("DEBUG: Creating parser");
                
                var parser = new Parser(tokens);
                Console.WriteLine("DEBUG: Calling Parse");
                
                var ast = parser.Parse();
                Console.WriteLine($"   Generated AST with {ast.Statements.Count} top-level statements");
                Console.WriteLine("DEBUG: Syntax analysis completed successfully");

                // Step 3: Code Generation
                Console.WriteLine("3. Code generation...");
                Console.WriteLine("DEBUG: Creating compiler");
                
                var compiler = new Compiler(OptimizationLevel.Release);
                Console.WriteLine("DEBUG: Calling Compile");
                
                var compiledProgram = compiler.Compile(ast);
                Console.WriteLine($"   Generated {compiledProgram.Bytecode.Code.Count} bytes of bytecode");
                Console.WriteLine("DEBUG: Code generation completed successfully");

                return compiledProgram;
            }
            catch (ParseException ex)
            {
                Console.WriteLine($"Parse Error: {ex.Message}");
                Console.WriteLine("DEBUG: ParseException caught");
                return null;
            }
            catch (CompilerException ex)
            {
                Console.WriteLine($"Compiler Error: {ex.Message}");
                if (ex.Line > 0)
                {
                    Console.WriteLine($"   at line {ex.Line}, column {ex.Column}");
                }
                Console.WriteLine("DEBUG: CompilerException caught");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Compilation Error: {ex.Message}");
                Console.WriteLine($"DEBUG: General Exception caught: {ex.GetType().Name}");
                Console.WriteLine($"DEBUG: Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("Usage: ouroboros [options] [file]");
            Console.WriteLine("\nOptions:");
            Console.WriteLine("  -h, --help     Show this help message");
            Console.WriteLine("  -v, --version  Show version information");
            Console.WriteLine("  [file]         Run the specified .ouro file");
            Console.WriteLine("\nExamples:");
            Console.WriteLine("  ouroboros hello.ouro       Run a file");
            Console.WriteLine("  ouroboros examples/UIDemo.ouro  Run the UI demo");
            Console.WriteLine("\nSupported Features:");
            Console.WriteLine("  • Three syntax levels (high, medium, low)");
            Console.WriteLine("  • Greek letters and mathematical symbols");
            Console.WriteLine("  • Custom loop constructs");
            Console.WriteLine("  • Modern UI framework");
            Console.WriteLine("  • Data-oriented programming");
            Console.WriteLine("  • Async/await support");
            Console.WriteLine();
        }

        static void ShowVersion()
        {
            Console.WriteLine("Ouroboros Language v1.0.0");
            Console.WriteLine("Copyright (c) 2025 Ouroboros Project");
            Console.WriteLine("Licensed under MIT License");
            Console.WriteLine("\nRuntime Information:");
            Console.WriteLine($"  .NET Version: {Environment.Version}");
            Console.WriteLine($"  Platform: {Environment.OSVersion}");
            Console.WriteLine($"  Architecture: {Environment.Is64BitProcess switch { true => "x64", false => "x86" }}");
            Console.WriteLine($"  Processor Count: {Environment.ProcessorCount}");
            Console.WriteLine("\nSupported features:");
            Console.WriteLine("  • Three syntax levels (high, medium, low)");
            Console.WriteLine("  • Greek letters and mathematical symbols");
            Console.WriteLine("  • Custom loop constructs");
            Console.WriteLine("  • Modern UI framework");
            Console.WriteLine("  • Data-oriented programming");
            Console.WriteLine("  • JIT compilation");
            Console.WriteLine("  • Garbage collection");
            Console.WriteLine("  • Multi-threading support");
        }
    }
}

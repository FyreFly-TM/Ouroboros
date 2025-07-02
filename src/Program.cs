using System;
using System.IO;
using System.Text;
using Ouro.Core.Lexer;
using Ouro.Core.Parser;
using Ouro.Core.Compiler;
using Ouro.Runtime;

namespace Ouro
{
    /// <summary>
    /// Main entry point for the Ouro programming language
    /// </summary>
    public class Program
    {
        private static Runtime.Runtime runtime;
        
        private static bool debugMode = false;
        
        private static void LogDebug(string message)
        {
            if (debugMode)
            {
                Console.WriteLine($"[DEBUG] {message}");
            }
        }
        
        public static void Main(string[] args)
        {
            // Check for debug flag
            debugMode = args.Length > 0 && (args[0] == "--debug" || Environment.GetEnvironmentVariable("OURO_DEBUG") == "1");
            if (debugMode && args.Length > 0 && args[0] == "--debug")
            {
                // Remove debug flag from args
                args = args.Length > 1 ? args[1..] : Array.Empty<string>();
            }
            
            LogDebug("Ouro compiler starting...");
            
            System.Console.WriteLine("Ouro Programming Language v1.0.0");
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
                LogDebug("Starting ExecuteFile method");
                
                // Check if file exists
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Error: File '{filePath}' not found.");
                    Environment.Exit(1);
                    return;
                }

                LogDebug("File exists, checking extension");
                
                // Check file extension
                if (!filePath.EndsWith(".ouro", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Warning: File '{filePath}' does not have .ouro extension.");
                }

                Console.WriteLine($"Executing: {filePath}");
                Console.WriteLine();

                LogDebug("Reading source code");
                
                // Read source code
                string sourceCode = File.ReadAllText(filePath, Encoding.UTF8);
                LogDebug($"Read {sourceCode.Length} characters from file");

                LogDebug("Initializing runtime");
                
                // Initialize runtime
                var runtimeOptions = new RuntimeOptions
                {
                    EnableJit = true,
                    EnableDebugging = debugMode,
                    EnableProfiling = false
                };
                runtime = new Runtime.Runtime(runtimeOptions);

                LogDebug("Starting compilation");
                
                // Compile and execute
                var compiledProgram = CompileSource(sourceCode, filePath);
                
                LogDebug($"Compilation result: {(compiledProgram != null ? "Success" : "Failed")}");
                
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
                    
                    // Convert Compiler.CompiledProgram to VM.CompiledProgram
                    var vmProgram = new Ouro.Core.VM.CompiledProgram
                    {
                        Bytecode = new Ouro.Core.VM.Bytecode
                        {
                            Instructions = compiledProgram.Bytecode.Code.ToArray(),
                            ConstantPool = compiledProgram.Bytecode.Constants.ToArray(),
                            Functions = new Ouro.Core.VM.FunctionInfo[0],
                            Classes = new Ouro.Core.VM.ClassInfo[0],
                            Interfaces = new Ouro.Core.VM.InterfaceInfo[0],
                            Structs = new Ouro.Core.VM.StructInfo[0],
                            Enums = new Ouro.Core.VM.EnumInfo[0],
                            Components = new Ouro.Core.VM.ComponentInfo[0],
                            Systems = new Ouro.Core.VM.SystemInfo[0],
                            Entities = new Ouro.Core.VM.EntityInfo[0],
                            ExceptionHandlers = new Ouro.Core.VM.ExceptionHandler[0]
                        },
                        SymbolTable = new Ouro.Core.VM.SymbolTable(),
                        SourceFile = compiledProgram.SourceFile,
                        Metadata = new Ouro.Core.VM.ProgramMetadata
                        {
                            Version = "1.0.0",
                            CompilerVersion = compiledProgram.Metadata?.Version ?? "1.0.0",
                            OptimizationLevel = 1,
                            SourceFiles = new[] { compiledProgram.SourceFile },
                            CompileTime = compiledProgram.Metadata?.CompileTime ?? DateTime.Now,
                            TargetPlatform = "Windows"
                        }
                    };
                    
                    // Execute the compiled program (use the original compiledProgram)
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
                    LogDebug("Compilation failed, no program to execute");
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
                LogDebug("Cleanup - shutting down runtime");
                
                // Clean up runtime
                runtime?.Shutdown();
            }
        }

        static CompiledProgram CompileSource(string sourceCode, string fileName)
        {
            try
            {
                Console.WriteLine("Starting compilation...");
                LogDebug("Entering CompileSource method");
                
                // Step 1: Lexical Analysis
                Console.WriteLine("1. Lexical analysis...");
                LogDebug("Creating lexer");
                
                var lexer = new Lexer(sourceCode, fileName);
                LogDebug("Calling ScanTokens");
                
                var tokens = lexer.ScanTokens();
                Console.WriteLine($"   Generated {tokens.Count} tokens");
                LogDebug("Lexical analysis completed successfully");
                
                // Step 2: Syntax Analysis
                Console.WriteLine("2. Syntax analysis...");
                LogDebug("Creating parser");
                
                var parser = new Parser(tokens);
                LogDebug("Calling Parse");
                
                var ast = parser.Parse();
                Console.WriteLine($"   Generated AST with {ast.Statements.Count} top-level statements");
                LogDebug("Syntax analysis completed successfully");

                // Step 3: Code Generation
                Console.WriteLine("3. Code generation...");
                LogDebug("Creating compiler");
                
                var compiler = new Compiler(OptimizationLevel.Release);
                LogDebug("Calling Compile");
                
                var compiledProgram = compiler.Compile(ast);
                Console.WriteLine($"   Generated {compiledProgram.Bytecode.Code.Count} bytes of bytecode");
                LogDebug("Code generation completed successfully");

                return compiledProgram;
            }
            catch (ParseException ex)
            {
                Console.WriteLine($"Parse Error: {ex.Message}");
                LogDebug("ParseException caught");
                return null;
            }
            catch (CompilerException ex)
            {
                Console.WriteLine($"Compiler Error: {ex.Message}");
                if (ex.Line > 0)
                {
                    Console.WriteLine($"   at line {ex.Line}, column {ex.Column}");
                }
                LogDebug("CompilerException caught");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Compilation Error: {ex.Message}");
                LogDebug($"General Exception caught: {ex.GetType().Name}");
                LogDebug($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("Usage: ouro [options] [file]");
            Console.WriteLine("\nOptions:");
            Console.WriteLine("  -h, --help     Show this help message");
            Console.WriteLine("  -v, --version  Show version information");
            Console.WriteLine("  --debug        Enable debug output");
            Console.WriteLine("  [file]         Run the specified .ouro file");
            Console.WriteLine("\nExamples:");
            Console.WriteLine("  ouro hello.ouro       Run a file");
            Console.WriteLine("  ouro examples/UIDemo.ouro  Run the UI demo");
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
            Console.WriteLine("Ouro Language v1.0.0");
            Console.WriteLine("Copyright (c) 2025 Ouro Project");
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

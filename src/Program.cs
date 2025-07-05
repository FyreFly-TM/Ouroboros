using System;
using System.IO;
using System.Text;
using Ouro.Core.Lexer;
using Ouro.Core.Parser;
using Ouro.Core.Compiler;
using Ouro.Runtime;
using Ouro.src.tools;

namespace Ouro
{
    /// <summary>
    /// Main entry point for the Ouro programming language
    /// </summary>

    public class Program
    {
        private static Runtime.Runtime runtime;
        
        private static bool debugMode = false;
        
        public static void Main(string[] args)
        {
            // Check for debug flag
            debugMode = args.Length > 0 && (args[0] == "--debug" || Environment.GetEnvironmentVariable("OURO_DEBUG") == "1");
            if (debugMode && args.Length > 0 && args[0] == "--debug")
            {
                // Remove debug flag from args
                args = args.Length > 1 ? args[1..] : Array.Empty<string>();
            }

            Logger.SetDebugMode(debugMode);

            Logger.Debug("Ouro compiler starting...");
            
            Logger.Info("Ouro Programming Language v1.0.0");
            Logger.Info("=====================================\n");

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
                Logger.Debug("Starting ExecuteFile method");
                
                // Check if file exists
                if (!File.Exists(filePath))
                {
                    Logger.Error($"Error: File '{filePath}' not found.");
                    Environment.Exit(1);
                    return;
                }

                Logger.Debug("File exists, checking extension");
                
                // Check file extension
                if (!filePath.EndsWith(".ouro", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Warn($"Warning: File '{filePath}' does not have .ouro extension.");
                }

                Logger.Info($"Executing: {filePath}\n");

                Logger.Debug("Reading source code");
                
                // Read source code
                string sourceCode = File.ReadAllText(filePath, Encoding.UTF8);
                Logger.Debug($"Read {sourceCode.Length} characters from file");

                Logger.Debug("Initializing runtime");
                
                // Initialize runtime
                var runtimeOptions = new RuntimeOptions
                {
                    EnableJit = true,
                    EnableDebugging = debugMode,
                    EnableProfiling = false
                };
                runtime = new Runtime.Runtime(runtimeOptions);

                Logger.Debug("Starting compilation");
                
                // Compile and execute
                var compiledProgram = CompileSource(sourceCode, filePath);
                
                Logger.Debug($"Compilation result: {(compiledProgram != null ? "Success" : "Failed")}");
                
                if (compiledProgram != null)
                {
                    Logger.Info("Compilation successful. Starting execution...");
                    Console.WriteLine(new string('=', 50)); // TODO Look into
                    
                    // Debug: Show bytecode info
                    Logger.Debug($"Bytecode length: {compiledProgram.Bytecode.Code.Count} bytes");
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
                        Logger.Info($"Program result: {result}"); // TODO
                    }
                    
                    Console.WriteLine(new string('=', 50));
                    Logger.Info("Execution completed.");
                }
                else
                {
                    Logger.Error("Compilation failed, no program to execute");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error executing file '{filePath}': {ex.Message}");
                if (ex.InnerException != null)
                {
                    Logger.Error($"Inner exception: {ex.InnerException.Message}");
                }
                
                // Show stack trace in debug mode
                #if DEBUG
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                #endif
                
                Environment.Exit(1);
            }
            finally
            {
                Logger.Debug("Cleanup - shutting down runtime"); // TODO
                
                // Clean up runtime
                runtime?.Shutdown();
            }
        }

        static CompiledProgram CompileSource(string sourceCode, string fileName)
        {
            try
            {
                Logger.Info("Starting compilation...");
                Logger.Debug("Entering CompileSource method");

                // Step 1: Lexical Analysis
                Logger.Info("1. Lexical analysis...");
                Logger.Debug("Creating lexer");
                
                var lexer = new Lexer(sourceCode, fileName);
                Logger.Debug("Calling ScanTokens");
                
                var tokens = lexer.ScanTokens();
                Logger.Info($"   Generated {tokens.Count} tokens");
                Logger.Debug("Lexical analysis completed successfully");

                // Step 2: Syntax Analysis
                Logger.Info("2. Syntax analysis...");
                Logger.Debug("Creating parser");
                
                var parser = new Parser(tokens);
                Logger.Debug("Calling Parse");
                
                var ast = parser.Parse();
                Logger.Info($"   Generated AST with {ast.Statements.Count} top-level statements");
                Logger.Debug("Syntax analysis completed successfully");

                // Step 3: Code Generation
                Logger.Info("3. Code generation...");
                Logger.Debug("Creating compiler");
                
                var compiler = new Compiler(OptimizationLevel.Release);
                Logger.Debug("Calling Compile");
                
                var compiledProgram = compiler.Compile(ast);
                Logger.Info($"   Generated {compiledProgram.Bytecode.Code.Count} bytes of bytecode");
                Logger.Debug("Code generation completed successfully");

                return compiledProgram;
            }
            catch (ParseException ex)
            {
                Logger.Error($"Parse Error: {ex.Message}");
                Logger.Debug("ParseException caught");
                return null;
            }
            catch (CompilerException ex)
            {
                Logger.Error($"Compiler Error: {ex.Message}");
                if (ex.Line > 0)
                {
                    Console.WriteLine($"   at line {ex.Line}, column {ex.Column}");
                }
                Logger.Debug("CompilerException caught");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"Compilation Error: {ex.Message}");
                Logger.Debug($"General Exception caught: {ex.GetType().Name}");
                Logger.Debug($"Stack trace: {ex.StackTrace}");
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

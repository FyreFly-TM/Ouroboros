using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LLVMSharp;
using LLVMSharp.Interop;
using Ouroboros.Core.AST;
using Ouroboros.Core.Compiler;
using Ouroboros.Core.VM;

namespace Ouroboros.CodeGen
{
    /// <summary>
    /// LLVM backend for native code generation
    /// </summary>
    public class LLVMBackend : ICodeGenerator
    {
        private readonly CompilerOptions options;
        private LLVMContext llvmContext;
        private LLVMIRBuilder irBuilder;
        private LLVMOptimizer optimizer;

        public LLVMBackend(CompilerOptions options)
        {
            this.options = options ?? new CompilerOptions();
        }

        public void GenerateCode(Core.AST.Program program, string outputPath)
        {
            using (llvmContext = new LLVMContext(Path.GetFileNameWithoutExtension(outputPath)))
            {
                irBuilder = new LLVMIRBuilder(llvmContext);
                optimizer = new LLVMOptimizer(llvmContext, options.OptimizationLevel);

                // Generate code for all functions
                foreach (var statement in program.Statements)
                {
                    if (statement is FunctionDeclaration funcDecl)
                    {
                        GenerateFunction(funcDecl);
                    }
                }

                // Generate main entry point if needed
                GenerateMainEntryPoint(program);

                // Verify module
                if (!llvmContext.VerifyModule())
                {
                    throw new CodeGenerationException("Module verification failed");
                }

                // Optimize if requested
                if (options.OptimizationLevel > 0)
                {
                    optimizer.Optimize();
                }

                // Generate output
                GenerateOutput(outputPath);
            }
        }

        private void GenerateFunction(FunctionDeclaration funcDecl)
        {
            // For now, generate stub bytecode
            // In real implementation, would get bytecode from compiler
            var stubBytecode = new byte[] { (byte)Opcode.Return };
            irBuilder.BuildFunction(funcDecl, stubBytecode);
        }

        private void GenerateMainEntryPoint(Core.AST.Program program)
        {
            // Create main function that calls Ouroboros entry point
            var mainType = LLVM.FunctionType(
                LLVM.Int32TypeInContext(llvmContext.Context),
                new[] { LLVM.Int32TypeInContext(llvmContext.Context), 
                       LLVM.PointerType(LLVM.PointerType(LLVM.Int8TypeInContext(llvmContext.Context), 0), 0) },
                false);

            var mainFunc = LLVM.AddFunction(llvmContext.Module, "main", mainType);
            var entryBlock = LLVM.AppendBasicBlockInContext(llvmContext.Context, mainFunc, "entry");
            LLVM.PositionBuilderAtEnd(llvmContext.Builder, entryBlock);

            // Initialize Ouroboros runtime
            var initFunc = LLVM.GetNamedFunction(llvmContext.Module, "ouroboros_init");
            if (initFunc == null)
            {
                var initType = LLVM.FunctionType(LLVM.VoidTypeInContext(llvmContext.Context), new LLVMTypeRef[0], false);
                initFunc = LLVM.AddFunction(llvmContext.Module, "ouroboros_init", initType);
            }
            LLVM.BuildCall(llvmContext.Builder, initFunc, new LLVMValueRef[0], "");

            // Call user's main function if it exists
            var userMain = LLVM.GetNamedFunction(llvmContext.Module, "Main");
            if (userMain != null)
            {
                LLVM.BuildCall(llvmContext.Builder, userMain, new LLVMValueRef[0], "");
            }

            // Return 0
            LLVM.BuildRet(llvmContext.Builder, LLVM.ConstInt(LLVM.Int32TypeInContext(llvmContext.Context), 0, false));
        }

        private void GenerateOutput(string outputPath)
        {
            var targetTriple = LLVM.GetDefaultTargetTriple();
            LLVM.GetTargetFromTriple(targetTriple, out var target, out var error);

            if (!string.IsNullOrEmpty(error))
            {
                throw new CodeGenerationException($"Failed to get target: {error}");
            }

            var cpu = "generic";
            var features = "";
            var optLevel = options.OptimizationLevel switch
            {
                0 => LLVMCodeGenOptLevel.LLVMCodeGenLevelNone,
                1 => LLVMCodeGenOptLevel.LLVMCodeGenLevelLess,
                2 => LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault,
                _ => LLVMCodeGenOptLevel.LLVMCodeGenLevelAggressive
            };

            var targetMachine = LLVM.CreateTargetMachine(
                target,
                targetTriple,
                cpu,
                features,
                optLevel,
                LLVMRelocMode.LLVMRelocPIC,
                LLVMCodeModel.LLVMCodeModelDefault
            );

            // Determine output type based on file extension
            var extension = Path.GetExtension(outputPath).ToLower();
            LLVMCodeGenFileType fileType;
            
            switch (extension)
            {
                case ".o":
                case ".obj":
                    fileType = LLVMCodeGenFileType.LLVMObjectFile;
                    break;
                case ".s":
                case ".asm":
                    fileType = LLVMCodeGenFileType.LLVMAssemblyFile;
                    break;
                case ".bc":
                    // Bitcode file
                    LLVM.WriteBitcodeToFile(llvmContext.Module, outputPath);
                    return;
                case ".ll":
                    // LLVM IR text file
                    LLVM.PrintModuleToFile(llvmContext.Module, outputPath, out error);
                    if (!string.IsNullOrEmpty(error))
                    {
                        throw new CodeGenerationException($"Failed to write LLVM IR: {error}");
                    }
                    return;
                default:
                    // Default to object file
                    fileType = LLVMCodeGenFileType.LLVMObjectFile;
                    if (!extension.Equals(".o") && !extension.Equals(".obj"))
                    {
                        outputPath = Path.ChangeExtension(outputPath, ".o");
                    }
                    break;
            }

            // Emit machine code
            LLVM.TargetMachineEmitToFile(targetMachine, llvmContext.Module, outputPath, fileType, out error);
            if (!string.IsNullOrEmpty(error))
            {
                throw new CodeGenerationException($"Failed to emit code: {error}");
            }

            LLVM.DisposeTargetMachine(targetMachine);

            // If we generated an object file and want an executable, invoke linker
            if (fileType == LLVMCodeGenFileType.LLVMObjectFile && options.OutputType == OutputType.Executable)
            {
                LinkExecutable(outputPath, Path.ChangeExtension(outputPath, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : ""));
            }
        }

        private void LinkExecutable(string objectFile, string executablePath)
        {
            // Invoke system linker
            var linker = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "link.exe" : "ld";
            var runtimeLib = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libouroboros_runtime.a");
            
            var args = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? $"/OUT:{executablePath} {objectFile} {runtimeLib}"
                : $"-o {executablePath} {objectFile} {runtimeLib} -lm -lpthread";

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = linker,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                throw new CodeGenerationException($"Linking failed: {error}");
            }
        }
    }

    public interface ICodeGenerator
    {
        void GenerateCode(Core.AST.Program program, string outputPath);
    }

    public class CompilerOptions
    {
        public int OptimizationLevel { get; set; } = 2;
        public OutputType OutputType { get; set; } = OutputType.Executable;
        public bool GenerateDebugInfo { get; set; } = true;
        public string TargetTriple { get; set; }
        public string CPU { get; set; } = "generic";
        public string Features { get; set; } = "";
    }

    public enum OutputType
    {
        Executable,
        SharedLibrary,
        StaticLibrary,
        ObjectFile,
        LLVMIR,
        Bitcode,
        Assembly
    }

    public class CodeGenerationException : Exception
    {
        public CodeGenerationException(string message) : base(message) { }
        public CodeGenerationException(string message, Exception inner) : base(message, inner) { }
    }
} 
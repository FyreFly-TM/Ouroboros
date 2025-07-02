using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LLVMSharp;
using LLVMSharp.Interop;
using Ouroboros.Core.AST;
using Ouroboros.Core.Compiler;
using Ouroboros.Core.VM;
using Ouroboros.Tokens;

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
        private readonly Compiler compiler;
        private readonly Dictionary<string, FunctionDeclaration> functionMap;
        private readonly Dictionary<string, byte[]> functionBytecode;

        public LLVMBackend(CompilerOptions options)
        {
            this.options = options ?? new CompilerOptions();
            this.compiler = new Compiler();
            this.functionMap = new Dictionary<string, FunctionDeclaration>();
            this.functionBytecode = new Dictionary<string, byte[]>();
        }

        public void GenerateCode(Core.AST.Program program, string outputPath)
        {
            using (llvmContext = new LLVMContext(Path.GetFileNameWithoutExtension(outputPath)))
            {
                irBuilder = new LLVMIRBuilder(llvmContext);
                optimizer = new LLVMOptimizer(llvmContext, options.OptimizationLevel);

                // First pass: compile all functions to bytecode
                CompileFunctionsToBytecode(program);

                // Generate debug information if requested
                if (options.GenerateDebugInfo)
                {
                    InitializeDebugInfo(outputPath);
                }

                // Generate code for all functions
                foreach (var statement in program.Statements)
                {
                    if (statement is FunctionDeclaration funcDecl)
                    {
                        GenerateFunction(funcDecl);
                    }
                    else if (statement is ClassDeclaration classDecl)
                    {
                        GenerateClass(classDecl);
                    }
                }

                // Generate runtime initialization functions
                GenerateRuntimeInitialization();

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

                // Finalize debug info
                if (options.GenerateDebugInfo)
                {
                    FinalizeDebugInfo();
                }

                // Generate output
                GenerateOutput(outputPath);
            }
        }

        private void CompileFunctionsToBytecode(Core.AST.Program program)
        {
            foreach (var statement in program.Statements)
            {
                if (statement is FunctionDeclaration funcDecl)
                {
                    functionMap[funcDecl.Name] = funcDecl;
                    
                    // Compile function to bytecode
                    var bytecodeBuilder = new BytecodeBuilder();
                    CompileFunctionBody(funcDecl, bytecodeBuilder);
                    functionBytecode[funcDecl.Name] = bytecodeBuilder.Build();
                }
            }
        }

        private void CompileFunctionBody(FunctionDeclaration funcDecl, BytecodeBuilder builder)
        {
            // Create local variable scope
            builder.EnterScope();
            
            // Add parameters to scope
            foreach (var param in funcDecl.Parameters)
            {
                builder.DeclareLocal(param.Name, param.Type);
            }
            
            // Compile function body
            CompileStatement(funcDecl.Body, builder);
            
            // Ensure return instruction
            if (!builder.HasReturn())
            {
                if (funcDecl.ReturnType?.Name == "void")
                {
                    builder.EmitReturn();
                }
                else
                {
                    builder.EmitLoadNull();
                    builder.EmitReturn();
                }
            }
            
            builder.ExitScope();
        }

        private void CompileStatement(Statement stmt, BytecodeBuilder builder)
        {
            switch (stmt)
            {
                case BlockStatement block:
                    builder.EnterScope();
                    foreach (var s in block.Statements)
                    {
                        CompileStatement(s, builder);
                    }
                    builder.ExitScope();
                    break;
                    
                case ExpressionStatement exprStmt:
                    CompileExpression(exprStmt.Expression, builder);
                    builder.EmitPop(); // Discard result
                    break;
                    
                case ReturnStatement retStmt:
                    if (retStmt.Value != null)
                    {
                        CompileExpression(retStmt.Value, builder);
                    }
                    builder.EmitReturn();
                    break;
                    
                case IfStatement ifStmt:
                    CompileExpression(ifStmt.Condition, builder);
                    var elseLabel = builder.CreateLabel();
                    var endLabel = builder.CreateLabel();
                    
                    builder.EmitJumpIfFalse(elseLabel);
                    CompileStatement(ifStmt.ThenBranch, builder);
                    builder.EmitJump(endLabel);
                    
                    builder.MarkLabel(elseLabel);
                    if (ifStmt.ElseBranch != null)
                    {
                        CompileStatement(ifStmt.ElseBranch, builder);
                    }
                    
                    builder.MarkLabel(endLabel);
                    break;
                    
                case WhileStatement whileStmt:
                    var loopStart = builder.CreateLabel();
                    var loopEnd = builder.CreateLabel();
                    
                    builder.MarkLabel(loopStart);
                    CompileExpression(whileStmt.Condition, builder);
                    builder.EmitJumpIfFalse(loopEnd);
                    
                    CompileStatement(whileStmt.Body, builder);
                    builder.EmitJump(loopStart);
                    
                    builder.MarkLabel(loopEnd);
                    break;
                    
                case VariableDeclaration varDecl:
                    builder.DeclareLocal(varDecl.Name, varDecl.Type);
                    if (varDecl.Initializer != null)
                    {
                        CompileExpression(varDecl.Initializer, builder);
                        builder.EmitStoreVar(varDecl.Name);
                    }
                    break;
            }
        }

        private void CompileExpression(Expression expr, BytecodeBuilder builder)
        {
            switch (expr)
            {
                case LiteralExpression lit:
                    builder.EmitLoadConst(lit.Value);
                    break;
                    
                case IdentifierExpression id:
                    builder.EmitLoadVar(id.Name);
                    break;
                    
                case BinaryExpression bin:
                    CompileExpression(bin.Left, builder);
                    CompileExpression(bin.Right, builder);
                    
                    switch (bin.Operator.Type)
                    {
                        case TokenType.Plus: builder.EmitAdd(); break;
                        case TokenType.Minus: builder.EmitSubtract(); break;
                        case TokenType.Multiply: builder.EmitMultiply(); break;
                        case TokenType.Divide: builder.EmitDivide(); break;
                        case TokenType.Less: builder.EmitLess(); break;
                        case TokenType.Greater: builder.EmitGreater(); break;
                        case TokenType.Equal: builder.EmitEqual(); break;
                        case TokenType.NotEqual: builder.EmitNotEqual(); break;
                    }
                    break;
                    
                case CallExpression call:
                    // Compile arguments
                    foreach (var arg in call.Arguments)
                    {
                        CompileExpression(arg, builder);
                    }
                    
                    // Emit call
                    if (call.Callee is IdentifierExpression funcId)
                    {
                        builder.EmitCall(funcId.Name, call.Arguments.Count);
                    }
                    break;
                    
                case AssignmentExpression assign:
                    CompileExpression(assign.Value, builder);
                    builder.EmitDup(); // Keep value on stack
                    if (assign.Target is IdentifierExpression targetId)
                    {
                        builder.EmitStoreVar(targetId.Name);
                    }
                    break;
            }
        }

        private void InitializeDebugInfo(string outputPath)
        {
            // Debug info generation will be implemented in a future phase
            // For now, we'll focus on getting basic code generation working
        }
        
        private void FinalizeDebugInfo()
        {
            // Debug info finalization will be implemented in a future phase
        }

        private void GenerateFunction(FunctionDeclaration funcDecl)
        {
            if (functionBytecode.TryGetValue(funcDecl.Name, out var bytecode))
            {
                irBuilder.BuildFunction(funcDecl, bytecode);
            }
        }

        private void GenerateClass(ClassDeclaration classDecl)
        {
            // Generate struct type for class
            var fieldTypes = new List<LLVMTypeRef>();
            
            // Add vtable pointer as first field
            fieldTypes.Add(llvmContext.GetType("ptr"));
            
            // Add fields
            foreach (var member in classDecl.Members)
            {
                if (member is FieldDeclaration field)
                {
                    fieldTypes.Add(MapTypeToLLVM(field.Type));
                }
            }
            
            unsafe
            {
                var fieldTypesArray = fieldTypes.ToArray();
                fixed (LLVMTypeRef* pFieldTypes = fieldTypesArray)
                {
                    var structType = LLVM.StructTypeInContext(llvmContext.Context, (LLVMOpaqueType**)pFieldTypes, (uint)fieldTypesArray.Length, 0);
                    // Store type mapping for later use
                    var structName = classDecl.Name + "_struct";
                    // Note: StructSetName may not be available in newer LLVM versions
                }
            }
            
            // Generate methods
            foreach (var member in classDecl.Members)
            {
                if (member is FunctionDeclaration method)
                {
                    GenerateMethod(classDecl, method);
                }
            }
            
            // Generate vtable
            GenerateVTable(classDecl);
        }
        
        private void GenerateMethod(ClassDeclaration classDecl, FunctionDeclaration method)
        {
            // Methods are compiled as functions with 'this' as first parameter
            var mangledName = $"{classDecl.Name}_{method.Name}";
            
            // Create function declaration with 'this' parameter
            var thisParam = new Parameter(
                new TypeNode(classDecl.Name), 
                "this", 
                null,
                ParameterModifier.None
            );
            
            var allParams = new List<Parameter> { thisParam }.Concat(method.Parameters).ToList();
            
            var funcDecl = new FunctionDeclaration(
                method.Token,
                method.ReturnType,
                allParams,
                method.Body,
                method.TypeParameters,
                method.IsAsync,
                method.Modifiers
            );
            
            // Compile and generate
            var bytecodeBuilder = new BytecodeBuilder();
            CompileFunctionBody(funcDecl, bytecodeBuilder);
            var bytecode = bytecodeBuilder.Build();
            
            irBuilder.BuildFunction(funcDecl, bytecode);
        }

        private void GenerateVTable(ClassDeclaration classDecl)
        {
            // Generate virtual method table
            var vtableEntries = new List<LLVMValueRef>();
            
            foreach (var member in classDecl.Members)
            {
                if (member is FunctionDeclaration method && method.Modifiers != null && method.Modifiers.Contains(Modifier.Virtual))
                {
                    var mangledName = $"{classDecl.Name}_{method.Name}";
                    if (irBuilder.TryGetFunction(mangledName, out var funcRef))
                    {
                        vtableEntries.Add(funcRef);
                    }
                }
            }
            
            if (vtableEntries.Count > 0)
            {
                unsafe
                {
                    var vtableType = LLVM.ArrayType(llvmContext.GetType("ptr"), (uint)vtableEntries.Count);
                    var mangledVTableName = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi($"{classDecl.Name}_vtable");
                    var vtable = LLVM.AddGlobal(llvmContext.Module, vtableType, (sbyte*)mangledVTableName);
                    
                    var vtableEntriesArray = vtableEntries.ToArray();
                    fixed (LLVMValueRef* pEntries = vtableEntriesArray)
                    {
                        var constantArray = LLVM.ConstArray(llvmContext.GetType("ptr"), (LLVMOpaqueValue**)pEntries, (uint)vtableEntriesArray.Length);
                        LLVM.SetInitializer(vtable, constantArray);
                    }
                    LLVM.SetGlobalConstant(vtable, 1);
                    
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(mangledVTableName);
                }
            }
        }

        private void GenerateRuntimeInitialization()
        {
            // Generate memory management functions
            GenerateAllocFunction();
            GenerateFreeFunction();
            GenerateGCFunctions();
            
            // Generate exception handling
            GenerateExceptionFunctions();
            
            // Generate IO functions
            GenerateIOFunctions();
        }

        private void GenerateAllocFunction()
        {
            unsafe
            {
                var allocType = LLVM.FunctionType(
                    llvmContext.GetType("ptr"),
                    new[] { llvmContext.GetType("i64") },
                    0); // IsVarArg = false
                
                var allocFuncName = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi("ouroboros_alloc");
                var allocFunc = LLVM.AddFunction(llvmContext.Module, (sbyte*)allocFuncName, allocType);
                var entryName = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi("entry");
                var entry = LLVM.AppendBasicBlockInContext(llvmContext.Context, allocFunc, (sbyte*)entryName);
                LLVM.PositionBuilderAtEnd(llvmContext.Builder, entry);
                
                // Call system malloc
                var mallocType = LLVM.FunctionType(
                    llvmContext.GetType("ptr"),
                    new[] { llvmContext.GetType("i64") },
                    0); // IsVarArg = false
                var mallocName = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi("malloc");
                var malloc = LLVM.AddFunction(llvmContext.Module, (sbyte*)mallocName, mallocType);
                
                var size = LLVM.GetParam(allocFunc, 0);
                var allocResultName = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi("alloc_result");
                var result = LLVM.BuildCall2(llvmContext.Builder, mallocType, malloc, new[] { size }, 1, (sbyte*)allocResultName);
                
                // Add GC tracking here in the future
                
                LLVM.BuildRet(llvmContext.Builder, result);
                
                System.Runtime.InteropServices.Marshal.FreeHGlobal(allocFuncName);
                System.Runtime.InteropServices.Marshal.FreeHGlobal(entryName);
                System.Runtime.InteropServices.Marshal.FreeHGlobal(mallocName);
                System.Runtime.InteropServices.Marshal.FreeHGlobal(allocResultName);
            }
        }

        private void GenerateFreeFunction()
        {
            unsafe
            {
                var freeType = LLVM.FunctionType(
                    llvmContext.GetType("void"),
                    new[] { llvmContext.GetType("ptr") },
                    0); // IsVarArg = false
                
                var freeFuncName = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi("ouroboros_free");
                var freeFunc = LLVM.AddFunction(llvmContext.Module, (sbyte*)freeFuncName, freeType);
                var entryName = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi("entry");
                var entry = LLVM.AppendBasicBlockInContext(llvmContext.Context, freeFunc, (sbyte*)entryName);
                LLVM.PositionBuilderAtEnd(llvmContext.Builder, entry);
                
                // Call system free
                var sysFreeType = LLVM.FunctionType(
                    llvmContext.GetType("void"),
                    new[] { llvmContext.GetType("ptr") },
                    0); // IsVarArg = false
                var sysFreeName = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi("free");
                var sysFree = LLVM.AddFunction(llvmContext.Module, (sbyte*)sysFreeName, sysFreeType);
                
                var ptr = LLVM.GetParam(freeFunc, 0);
                var emptyName = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi("");
                LLVM.BuildCall2(llvmContext.Builder, sysFreeType, sysFree, new[] { ptr }, 1, (sbyte*)emptyName);
                
                LLVM.BuildRetVoid(llvmContext.Builder);
                
                System.Runtime.InteropServices.Marshal.FreeHGlobal(freeFuncName);
                System.Runtime.InteropServices.Marshal.FreeHGlobal(entryName);
                System.Runtime.InteropServices.Marshal.FreeHGlobal(sysFreeName);
                System.Runtime.InteropServices.Marshal.FreeHGlobal(emptyName);
            }
        }

        private void GenerateGCFunctions()
        {
            unsafe
            {
                // Generate garbage collection stub
                var gcCollectType = LLVM.FunctionType(llvmContext.GetType("void"), new LLVMTypeRef[0], 0); // IsVarArg = false
                var gcCollectName = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi("ouroboros_gc_collect");
                var gcCollect = LLVM.AddFunction(llvmContext.Module, (sbyte*)gcCollectName, gcCollectType);
                var entryName = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi("entry");
                var entry = LLVM.AppendBasicBlockInContext(llvmContext.Context, gcCollect, (sbyte*)entryName);
                LLVM.PositionBuilderAtEnd(llvmContext.Builder, entry);
                
                // For now, just return - implement real GC later
                LLVM.BuildRetVoid(llvmContext.Builder);
                
                System.Runtime.InteropServices.Marshal.FreeHGlobal(gcCollectName);
                System.Runtime.InteropServices.Marshal.FreeHGlobal(entryName);
            }
        }

        private void GenerateExceptionFunctions()
        {
            unsafe
            {
                // Generate exception throwing function
                var throwType = LLVM.FunctionType(
                    llvmContext.GetType("void"),
                    new[] { llvmContext.GetType("ptr") },
                    0); // IsVarArg = false
                
                var throwFuncName = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi("ouroboros_throw");
                var throwFunc = LLVM.AddFunction(llvmContext.Module, (sbyte*)throwFuncName, throwType);
                var entryName = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi("entry");
                var entry = LLVM.AppendBasicBlockInContext(llvmContext.Context, throwFunc, (sbyte*)entryName);
                LLVM.PositionBuilderAtEnd(llvmContext.Builder, entry);
                
                // Print error and exit
                var printfType = LLVM.FunctionType(
                    llvmContext.GetType("i32"),
                    new[] { llvmContext.GetType("ptr") },
                    1); // varargs
                var printfName = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi("printf");
                var printf = LLVM.AddFunction(llvmContext.Module, (sbyte*)printfName, printfType);
                
                var formatStrName = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi("exception_format");
                var formatStr = LLVM.BuildGlobalStringPtr(llvmContext.Builder, "Exception: %s\n", (sbyte*)formatStrName);
                var exceptionMsg = LLVM.GetParam(throwFunc, 0);
                
                var printfArgs = new[] { formatStr, exceptionMsg };
                fixed (LLVMValueRef* pArgs = printfArgs)
                {
                    var emptyName = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi("");
                    LLVM.BuildCall2(llvmContext.Builder, printfType, printf, (LLVMOpaqueValue**)pArgs, (uint)printfArgs.Length, (sbyte*)emptyName);
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(emptyName);
                }
                
                // Exit with error code
                var exitType = LLVM.FunctionType(
                    llvmContext.GetType("void"),
                    new[] { llvmContext.GetType("i32") },
                    0); // IsVarArg = false
                var exitName = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi("exit");
                var exit = LLVM.AddFunction(llvmContext.Module, (sbyte*)exitName, exitType);
                var exitArg = LLVM.ConstInt(llvmContext.GetType("i32"), 1, 0);
                var exitEmptyName = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi("");
                LLVM.BuildCall2(llvmContext.Builder, exitType, exit, new[] { exitArg }, 1, (sbyte*)exitEmptyName);
                
                LLVM.BuildUnreachable(llvmContext.Builder);
                
                System.Runtime.InteropServices.Marshal.FreeHGlobal(throwFuncName);
                System.Runtime.InteropServices.Marshal.FreeHGlobal(entryName);
                System.Runtime.InteropServices.Marshal.FreeHGlobal(printfName);
                System.Runtime.InteropServices.Marshal.FreeHGlobal(formatStrName);
                System.Runtime.InteropServices.Marshal.FreeHGlobal(exitName);
                System.Runtime.InteropServices.Marshal.FreeHGlobal(exitEmptyName);
            }
        }

        private void GenerateIOFunctions()
        {
            unsafe
            {
                // Generate print function
                var printType = LLVM.FunctionType(
                    llvmContext.GetType("void"),
                    new[] { llvmContext.GetType("ptr") },
                    0); // IsVarArg = false
                
                var printFuncName = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi("ouroboros_print");
                var printFunc = LLVM.AddFunction(llvmContext.Module, (sbyte*)printFuncName, printType);
                var entryName = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi("entry");
                var entry = LLVM.AppendBasicBlockInContext(llvmContext.Context, printFunc, (sbyte*)entryName);
                LLVM.PositionBuilderAtEnd(llvmContext.Builder, entry);
                
                // Call printf
                var printfType = LLVM.FunctionType(
                    llvmContext.GetType("i32"),
                    new[] { llvmContext.GetType("ptr") },
                    1); // varargs
                var printfName = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi("printf");
                var printf = LLVM.AddFunction(llvmContext.Module, (sbyte*)printfName, printfType);
                
                var formatStrName = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi("print_format");
                var formatStr = LLVM.BuildGlobalStringPtr(llvmContext.Builder, "%s\n", (sbyte*)formatStrName);
                var str = LLVM.GetParam(printFunc, 0);
                
                var printfArgs = new[] { formatStr, str };
                fixed (LLVMValueRef* pArgs = printfArgs)
                {
                    var emptyName = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi("");
                    LLVM.BuildCall2(llvmContext.Builder, printfType, printf, (LLVMOpaqueValue**)pArgs, (uint)printfArgs.Length, (sbyte*)emptyName);
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(emptyName);
                }
                
                LLVM.BuildRetVoid(llvmContext.Builder);
                
                System.Runtime.InteropServices.Marshal.FreeHGlobal(printFuncName);
                System.Runtime.InteropServices.Marshal.FreeHGlobal(entryName);
                System.Runtime.InteropServices.Marshal.FreeHGlobal(printfName);
                System.Runtime.InteropServices.Marshal.FreeHGlobal(formatStrName);
            }
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
                GenerateRuntimeInit(initFunc);
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

        private void GenerateRuntimeInit(LLVMValueRef initFunc)
        {
            var entry = LLVM.AppendBasicBlockInContext(llvmContext.Context, initFunc, "entry");
            LLVM.PositionBuilderAtEnd(llvmContext.Builder, entry);
            
            // Initialize GC
            // Initialize thread pool
            // Initialize other runtime systems
            
            LLVM.BuildRetVoid(llvmContext.Builder);
        }

        private LLVMTypeRef MapTypeToLLVM(TypeNode type)
        {
            if (type == null) return llvmContext.GetType("void");
            
            return type.Name switch
            {
                "void" => llvmContext.GetType("void"),
                "bool" => llvmContext.GetType("bool"),
                "byte" => llvmContext.GetType("i8"),
                "short" => llvmContext.GetType("i16"),
                "int" => llvmContext.GetType("i32"),
                "long" => llvmContext.GetType("i64"),
                "float" => llvmContext.GetType("f32"),
                "double" => llvmContext.GetType("f64"),
                "string" => llvmContext.GetType("ptr"),
                _ when type.IsArray => llvmContext.GetType(type.Name),
                _ => llvmContext.GetType(type.Name)
            };
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
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LLVMSharp;
using LLVMSharp.Interop;

namespace Ouroboros.CodeGen
{
    /// <summary>
    /// Manages LLVM context and module creation for native code generation
    /// </summary>
    public class LLVMContext : IDisposable
    {
        private readonly LLVMContextRef context;
        private readonly LLVMModuleRef module;
        private readonly LLVMBuilderRef builder;
        private readonly Dictionary<string, LLVMValueRef> namedValues;
        private readonly Dictionary<string, LLVMTypeRef> typeCache;
        private bool disposed;

        public LLVMContextRef Context => context;
        public LLVMModuleRef Module => module;
        public LLVMBuilderRef Builder => builder;

        public LLVMContext(string moduleName)
        {
            context = LLVM.ContextCreate();
            module = LLVM.ModuleCreateWithNameInContext(moduleName, context);
            builder = LLVM.CreateBuilderInContext(context);
            namedValues = new Dictionary<string, LLVMValueRef>();
            typeCache = new Dictionary<string, LLVMTypeRef>();
            
            InitializeBuiltinTypes();
            InitializeTargetInfo();
        }

        private void InitializeBuiltinTypes()
        {
            // Cache common LLVM types
            typeCache["void"] = LLVM.VoidTypeInContext(context);
            typeCache["bool"] = LLVM.Int1TypeInContext(context);
            typeCache["i8"] = LLVM.Int8TypeInContext(context);
            typeCache["i16"] = LLVM.Int16TypeInContext(context);
            typeCache["i32"] = LLVM.Int32TypeInContext(context);
            typeCache["i64"] = LLVM.Int64TypeInContext(context);
            typeCache["f32"] = LLVM.FloatTypeInContext(context);
            typeCache["f64"] = LLVM.DoubleTypeInContext(context);
            typeCache["ptr"] = LLVM.PointerType(LLVM.Int8TypeInContext(context), 0);
        }

        private void InitializeTargetInfo()
        {
            // Initialize target information
            LLVM.InitializeAllTargetInfos();
            LLVM.InitializeAllTargets();
            LLVM.InitializeAllTargetMCs();
            LLVM.InitializeAllAsmParsers();
            LLVM.InitializeAllAsmPrinters();

            // Set target triple
            var targetTriple = LLVM.GetDefaultTargetTriple();
            LLVM.SetTarget(module, targetTriple);

            // Set data layout
            var target = LLVM.GetTargetFromTriple(targetTriple, out var error, out var errorMsg);
            if (!string.IsNullOrEmpty(errorMsg))
            {
                throw new InvalidOperationException($"Failed to get target: {errorMsg}");
            }

            var targetMachine = LLVM.CreateTargetMachine(
                target,
                targetTriple,
                "generic",
                "",
                LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault,
                LLVMRelocMode.LLVMRelocDefault,
                LLVMCodeModel.LLVMCodeModelDefault
            );

            var dataLayout = LLVM.CreateTargetDataLayout(targetMachine);
            LLVM.SetModuleDataLayout(module, dataLayout);
        }

        public LLVMTypeRef GetType(string typeName)
        {
            if (typeCache.TryGetValue(typeName, out var type))
            {
                return type;
            }

            // Handle array types
            if (typeName.EndsWith("[]"))
            {
                var elementTypeName = typeName.Substring(0, typeName.Length - 2);
                var elementType = GetType(elementTypeName);
                var arrayType = LLVM.PointerType(elementType, 0);
                typeCache[typeName] = arrayType;
                return arrayType;
            }

            // Handle custom types - for now, treat as opaque pointers
            var opaqueType = LLVM.PointerType(LLVM.Int8TypeInContext(context), 0);
            typeCache[typeName] = opaqueType;
            return opaqueType;
        }

        public LLVMValueRef GetNamedValue(string name)
        {
            return namedValues.TryGetValue(name, out var value) ? value : null;
        }

        public void SetNamedValue(string name, LLVMValueRef value)
        {
            namedValues[name] = value;
        }

        public void ClearNamedValues()
        {
            namedValues.Clear();
        }

        public bool VerifyModule()
        {
            return LLVM.VerifyModule(module, LLVMVerifierFailureAction.LLVMPrintMessageAction, out var error);
        }

        public void DumpModule()
        {
            LLVM.DumpModule(module);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                LLVM.DisposeBuilder(builder);
                LLVM.DisposeModule(module);
                LLVM.ContextDispose(context);
                disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
} 
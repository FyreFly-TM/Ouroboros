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
    public unsafe class LLVMContext : IDisposable
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
            unsafe
            {
                context = LLVM.ContextCreate();
                
                // Convert string to sbyte* for LLVM API
                var moduleNameBytes = System.Text.Encoding.UTF8.GetBytes(moduleName + '\0');
                fixed (byte* pModuleName = moduleNameBytes)
                {
                    module = LLVM.ModuleCreateWithNameInContext((sbyte*)pModuleName, context);
                }
                
                builder = LLVM.CreateBuilderInContext(context);
                namedValues = new Dictionary<string, LLVMValueRef>();
                typeCache = new Dictionary<string, LLVMTypeRef>();
                
                InitializeBuiltinTypes();
                InitializeTargetInfo();
            }
        }

        private unsafe void InitializeBuiltinTypes()
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

        private unsafe void InitializeTargetInfo()
        {
            // Initialize target information
            LLVM.InitializeAllTargetInfos();
            LLVM.InitializeAllTargets();
            LLVM.InitializeAllTargetMCs();
            LLVM.InitializeAllAsmParsers();
            LLVM.InitializeAllAsmPrinters();

            // Set target triple
            var targetTriplePtr = LLVM.GetDefaultTargetTriple();
            var targetTriple = Marshal.PtrToStringAnsi((IntPtr)targetTriplePtr);
            
            LLVM.DisposeMessage(targetTriplePtr);

            // Set data layout
            LLVMTargetRef target;
            sbyte* errorString = null;
            
            var targetTripleCStr = Marshal.StringToHGlobalAnsi(targetTriple);
            try
            {
                LLVMTargetRef* targetPtr = &target;
                LLVM.GetTargetFromTriple((sbyte*)targetTripleCStr, (LLVMTarget**)targetPtr, &errorString);
                
                if (errorString != null)
                {
                    var errorMessage = Marshal.PtrToStringAnsi((IntPtr)errorString);
                    LLVM.DisposeMessage(errorString);
                    throw new InvalidOperationException($"Failed to get target: {errorMessage}");
                }

                // Convert strings to sbyte* for LLVM API
                var cpuCStr = Marshal.StringToHGlobalAnsi("generic");
                var featuresCStr = Marshal.StringToHGlobalAnsi("");
                var targetTripleForMachine = Marshal.StringToHGlobalAnsi(targetTriple);
                
                try
                {
                    var targetMachine = LLVM.CreateTargetMachine(
                        target,
                        (sbyte*)targetTripleForMachine,
                        (sbyte*)cpuCStr,
                        (sbyte*)featuresCStr,
                        LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault,
                        LLVMRelocMode.LLVMRelocDefault,
                        LLVMCodeModel.LLVMCodeModelDefault
                    );

                    var dataLayout = LLVM.CreateTargetDataLayout(targetMachine);
                    LLVM.SetModuleDataLayout(module, dataLayout);
                    
                    LLVM.DisposeTargetMachine(targetMachine);
                }
                finally
                {
                    Marshal.FreeHGlobal(cpuCStr);
                    Marshal.FreeHGlobal(featuresCStr);
                    Marshal.FreeHGlobal(targetTripleForMachine);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(targetTripleCStr);
            }
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
            sbyte* error = null;
            var result = LLVM.VerifyModule(module, LLVMVerifierFailureAction.LLVMPrintMessageAction, &error);
            if (error != null)
            {
                LLVM.DisposeMessage(error);
            }
            return result == 0;
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
using System;
using LLVMSharp;
using LLVMSharp.Interop;

namespace Ouroboros.CodeGen
{
    /// <summary>
    /// LLVM optimization pass manager
    /// </summary>
    public class LLVMOptimizer
    {
        private readonly LLVMContext context;
        private readonly int optimizationLevel;
        private LLVMPassManagerRef functionPassManager;
        private LLVMPassManagerRef modulePassManager;

        public LLVMOptimizer(LLVMContext context, int optimizationLevel)
        {
            this.context = context;
            this.optimizationLevel = optimizationLevel;
            InitializePasses();
        }

        private void InitializePasses()
        {
            unsafe
            {
                // Create function pass manager
                functionPassManager = LLVM.CreateFunctionPassManagerForModule(context.Module);
                
                // Create module pass manager
                modulePassManager = LLVM.CreatePassManager();
            }

            // Add passes based on optimization level
            switch (optimizationLevel)
            {
                case 0:
                    // No optimization
                    break;
                    
                case 1:
                    AddBasicOptimizations();
                    break;
                    
                case 2:
                    AddStandardOptimizations();
                    break;
                    
                case 3:
                default:
                    AddAggressiveOptimizations();
                    break;
            }

            unsafe
            {
                // Initialize function pass manager
                LLVM.InitializeFunctionPassManager(functionPassManager);
            }
        }

        private void AddBasicOptimizations()
        {
            unsafe
            {
                // Basic cleanup passes
                LLVM.AddPromoteMemoryToRegisterPass(functionPassManager);
                LLVM.AddInstructionCombiningPass(functionPassManager);
                LLVM.AddReassociatePass(functionPassManager);
                LLVM.AddCFGSimplificationPass(functionPassManager);
                
                // Module-level optimizations
                LLVM.AddGlobalDCEPass(modulePassManager);
                LLVM.AddConstantMergePass(modulePassManager);
            }
        }

        private void AddStandardOptimizations()
        {
            // Include basic optimizations
            AddBasicOptimizations();
            
            unsafe
            {
                // Additional function-level optimizations
                LLVM.AddGVNPass(functionPassManager);
                LLVM.AddDeadStoreEliminationPass(functionPassManager);
                LLVM.AddSCCPPass(functionPassManager);
                LLVM.AddTailCallEliminationPass(functionPassManager);
                LLVM.AddJumpThreadingPass(functionPassManager);
                LLVM.AddLoopUnrollPass(functionPassManager);
                LLVM.AddLoopVectorizePass(functionPassManager);
                LLVM.AddSLPVectorizePass(functionPassManager);
                
                // Module-level optimizations
                LLVM.AddFunctionInliningPass(modulePassManager);
                LLVM.AddDeadArgEliminationPass(modulePassManager);
                LLVM.AddGlobalOptimizerPass(modulePassManager);
            }
        }

        private void AddAggressiveOptimizations()
        {
            // Include standard optimizations
            AddStandardOptimizations();
            
            unsafe
            {
                // Aggressive function-level optimizations
                LLVM.AddAggressiveDCEPass(functionPassManager);
                LLVM.AddLoopUnrollAndJamPass(functionPassManager);
                LLVM.AddIndVarSimplifyPass(functionPassManager);
                LLVM.AddLoopDeletionPass(functionPassManager);
                LLVM.AddLoopIdiomPass(functionPassManager);
                LLVM.AddLoopRotatePass(functionPassManager);
                LLVM.AddLICMPass(functionPassManager);
                
                // More aggressive inlining
                LLVM.AddAlwaysInlinerPass(modulePassManager);
                LLVM.AddIPSCCPPass(modulePassManager);
            }
        }

        public void Optimize()
        {
            unsafe
            {
                // Run function passes on all functions
                var function = LLVM.GetFirstFunction(context.Module);
                while (function != null)
                {
                    LLVM.RunFunctionPassManager(functionPassManager, function);
                    function = LLVM.GetNextFunction(function);
                }
                
                // Finalize function pass manager
                LLVM.FinalizeFunctionPassManager(functionPassManager);
                
                // Run module passes
                LLVM.RunPassManager(modulePassManager, context.Module);
            }
        }

        public void Dispose()
        {
            unsafe
            {
                LLVM.DisposePassManager(functionPassManager);
                LLVM.DisposePassManager(modulePassManager);
            }
        }
    }
} 
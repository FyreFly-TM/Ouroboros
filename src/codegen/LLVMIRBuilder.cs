using System;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp;
using LLVMSharp.Interop;
using Ouroboros.Core.VM;
using Ouroboros.Core.AST;

namespace Ouroboros.CodeGen
{
    /// <summary>
    /// Builds LLVM IR from Ouroboros bytecode and AST
    /// </summary>
    public class LLVMIRBuilder
    {
        private readonly LLVMContext llvmContext;
        private readonly Dictionary<string, LLVMValueRef> functions;
        private readonly Dictionary<int, LLVMBasicBlockRef> basicBlocks;
        private readonly Stack<LLVMValueRef> valueStack;
        private LLVMValueRef currentFunction;
        private LLVMBasicBlockRef currentBlock;

        public LLVMIRBuilder(LLVMContext context)
        {
            llvmContext = context;
            functions = new Dictionary<string, LLVMValueRef>();
            basicBlocks = new Dictionary<int, LLVMBasicBlockRef>();
            valueStack = new Stack<LLVMValueRef>();
            
            InitializeRuntimeFunctions();
        }

        private void InitializeRuntimeFunctions()
        {
            // Declare runtime functions that will be linked
            DeclareRuntimeFunction("ouroboros_alloc", llvmContext.GetType("ptr"), llvmContext.GetType("i64"));
            DeclareRuntimeFunction("ouroboros_free", llvmContext.GetType("void"), llvmContext.GetType("ptr"));
            DeclareRuntimeFunction("ouroboros_gc_collect", llvmContext.GetType("void"));
            DeclareRuntimeFunction("ouroboros_throw", llvmContext.GetType("void"), llvmContext.GetType("ptr"));
            DeclareRuntimeFunction("ouroboros_print", llvmContext.GetType("void"), llvmContext.GetType("ptr"));
        }

        private void DeclareRuntimeFunction(string name, LLVMTypeRef returnType, params LLVMTypeRef[] paramTypes)
        {
            var functionType = LLVM.FunctionType(returnType, paramTypes, false);
            var function = LLVM.AddFunction(llvmContext.Module, name, functionType);
            functions[name] = function;
        }

        public void BuildFunction(FunctionDeclaration funcDecl, byte[] bytecode)
        {
            // Create function type
            var paramTypes = funcDecl.Parameters
                .Select(p => MapTypeToLLVM(p.Type))
                .ToArray();
            var returnType = MapTypeToLLVM(funcDecl.ReturnType);
            var functionType = LLVM.FunctionType(returnType, paramTypes, false);

            // Create function
            currentFunction = LLVM.AddFunction(llvmContext.Module, funcDecl.Name.Lexeme, functionType);
            functions[funcDecl.Name.Lexeme] = currentFunction;

            // Create entry block
            currentBlock = LLVM.AppendBasicBlockInContext(llvmContext.Context, currentFunction, "entry");
            LLVM.PositionBuilderAtEnd(llvmContext.Builder, currentBlock);

            // Set parameter names and store in named values
            for (int i = 0; i < funcDecl.Parameters.Count; i++)
            {
                var param = LLVM.GetParam(currentFunction, (uint)i);
                var paramName = funcDecl.Parameters[i].Name;
                LLVM.SetValueName(param, paramName);
                
                // Allocate stack space for parameter
                var alloca = CreateAlloca(paramTypes[i], paramName);
                LLVM.BuildStore(llvmContext.Builder, param, alloca);
                llvmContext.SetNamedValue(paramName, alloca);
            }

            // Build function body from bytecode
            BuildFromBytecode(bytecode);

            // Add return if not present
            if (!LLVM.GetBasicBlockTerminator(currentBlock).HasValue)
            {
                if (returnType.Kind == LLVMTypeKind.LLVMVoidTypeKind)
                {
                    LLVM.BuildRetVoid(llvmContext.Builder);
                }
                else
                {
                    LLVM.BuildRet(llvmContext.Builder, LLVM.ConstNull(returnType));
                }
            }

            // Verify function
            LLVM.VerifyFunction(currentFunction, LLVMVerifierFailureAction.LLVMPrintMessageAction);
        }

        private void BuildFromBytecode(byte[] bytecode)
        {
            int pc = 0;
            while (pc < bytecode.Length)
            {
                var opcode = (Opcode)bytecode[pc++];
                BuildInstruction(opcode, bytecode, ref pc);
            }
        }

        private void BuildInstruction(Opcode opcode, byte[] bytecode, ref int pc)
        {
            switch (opcode)
            {
                case Opcode.LoadConst:
                    BuildLoadConst(bytecode, ref pc);
                    break;
                    
                case Opcode.LoadVar:
                    BuildLoadVar(bytecode, ref pc);
                    break;
                    
                case Opcode.StoreVar:
                    BuildStoreVar(bytecode, ref pc);
                    break;
                    
                case Opcode.Add:
                    BuildBinaryOp(LLVM.BuildAdd, LLVM.BuildFAdd);
                    break;
                    
                case Opcode.Subtract:
                    BuildBinaryOp(LLVM.BuildSub, LLVM.BuildFSub);
                    break;
                    
                case Opcode.Multiply:
                    BuildBinaryOp(LLVM.BuildMul, LLVM.BuildFMul);
                    break;
                    
                case Opcode.Divide:
                    BuildBinaryOp(LLVM.BuildSDiv, LLVM.BuildFDiv);
                    break;
                    
                case Opcode.Call:
                    BuildCall(bytecode, ref pc);
                    break;
                    
                case Opcode.Return:
                    BuildReturn();
                    break;
                    
                case Opcode.Jump:
                    BuildJump(bytecode, ref pc);
                    break;
                    
                case Opcode.JumpIfFalse:
                    BuildConditionalJump(bytecode, ref pc, false);
                    break;
                    
                case Opcode.JumpIfTrue:
                    BuildConditionalJump(bytecode, ref pc, true);
                    break;
                    
                // Add more opcodes as needed
                default:
                    // For now, skip unknown opcodes
                    break;
            }
        }

        private void BuildLoadConst(byte[] bytecode, ref int pc)
        {
            var constIndex = BitConverter.ToInt32(bytecode, pc);
            pc += 4;
            
            // For now, push a dummy constant
            // In real implementation, would look up from constant pool
            var constValue = LLVM.ConstInt(llvmContext.GetType("i32"), (ulong)constIndex, false);
            valueStack.Push(constValue);
        }

        private void BuildLoadVar(byte[] bytecode, ref int pc)
        {
            var varIndex = BitConverter.ToInt32(bytecode, pc);
            pc += 4;
            
            // In real implementation, would map var index to name
            var varName = $"var_{varIndex}";
            var varPtr = llvmContext.GetNamedValue(varName);
            
            if (varPtr != null)
            {
                var value = LLVM.BuildLoad(llvmContext.Builder, varPtr, varName + "_load");
                valueStack.Push(value);
            }
        }

        private void BuildStoreVar(byte[] bytecode, ref int pc)
        {
            var varIndex = BitConverter.ToInt32(bytecode, pc);
            pc += 4;
            
            if (valueStack.Count > 0)
            {
                var value = valueStack.Pop();
                var varName = $"var_{varIndex}";
                var varPtr = llvmContext.GetNamedValue(varName);
                
                if (varPtr == null)
                {
                    // Create alloca for new variable
                    varPtr = CreateAlloca(LLVM.TypeOf(value), varName);
                    llvmContext.SetNamedValue(varName, varPtr);
                }
                
                LLVM.BuildStore(llvmContext.Builder, value, varPtr);
            }
        }

        private void BuildBinaryOp(
            Func<LLVMBuilderRef, LLVMValueRef, LLVMValueRef, string, LLVMValueRef> intOp,
            Func<LLVMBuilderRef, LLVMValueRef, LLVMValueRef, string, LLVMValueRef> floatOp)
        {
            if (valueStack.Count >= 2)
            {
                var right = valueStack.Pop();
                var left = valueStack.Pop();
                
                var leftType = LLVM.TypeOf(left);
                LLVMValueRef result;
                
                if (leftType.Kind == LLVMTypeKind.LLVMFloatTypeKind || 
                    leftType.Kind == LLVMTypeKind.LLVMDoubleTypeKind)
                {
                    result = floatOp(llvmContext.Builder, left, right, "binop_result");
                }
                else
                {
                    result = intOp(llvmContext.Builder, left, right, "binop_result");
                }
                
                valueStack.Push(result);
            }
        }

        private void BuildCall(byte[] bytecode, ref int pc)
        {
            var funcIndex = BitConverter.ToInt32(bytecode, pc);
            pc += 4;
            var argCount = BitConverter.ToInt32(bytecode, pc);
            pc += 4;
            
            // Pop arguments from stack
            var args = new LLVMValueRef[argCount];
            for (int i = argCount - 1; i >= 0; i--)
            {
                if (valueStack.Count > 0)
                    args[i] = valueStack.Pop();
            }
            
            // In real implementation, would look up function name from index
            var funcName = $"func_{funcIndex}";
            if (functions.TryGetValue(funcName, out var function))
            {
                var result = LLVM.BuildCall(llvmContext.Builder, function, args, "call_result");
                if (LLVM.GetReturnType(LLVM.GetElementType(LLVM.TypeOf(function))).Kind != LLVMTypeKind.LLVMVoidTypeKind)
                {
                    valueStack.Push(result);
                }
            }
        }

        private void BuildReturn()
        {
            if (valueStack.Count > 0)
            {
                var returnValue = valueStack.Pop();
                LLVM.BuildRet(llvmContext.Builder, returnValue);
            }
            else
            {
                LLVM.BuildRetVoid(llvmContext.Builder);
            }
        }

        private void BuildJump(byte[] bytecode, ref int pc)
        {
            var target = BitConverter.ToInt32(bytecode, pc);
            pc += 4;
            
            var targetBlock = GetOrCreateBasicBlock(target);
            LLVM.BuildBr(llvmContext.Builder, targetBlock);
        }

        private void BuildConditionalJump(byte[] bytecode, ref int pc, bool jumpIfTrue)
        {
            var target = BitConverter.ToInt32(bytecode, pc);
            pc += 4;
            
            if (valueStack.Count > 0)
            {
                var condition = valueStack.Pop();
                
                // Convert to bool if needed
                if (LLVM.TypeOf(condition).Kind != LLVMTypeKind.LLVMIntegerTypeKind ||
                    LLVM.GetIntTypeWidth(LLVM.TypeOf(condition)) != 1)
                {
                    condition = LLVM.BuildICmp(llvmContext.Builder, LLVMIntPredicate.LLVMIntNE,
                        condition, LLVM.ConstNull(LLVM.TypeOf(condition)), "tobool");
                }
                
                if (!jumpIfTrue)
                {
                    condition = LLVM.BuildNot(llvmContext.Builder, condition, "not_cond");
                }
                
                var thenBlock = GetOrCreateBasicBlock(target);
                var contBlock = GetOrCreateBasicBlock(pc);
                
                LLVM.BuildCondBr(llvmContext.Builder, condition, thenBlock, contBlock);
                currentBlock = contBlock;
                LLVM.PositionBuilderAtEnd(llvmContext.Builder, currentBlock);
            }
        }

        private LLVMBasicBlockRef GetOrCreateBasicBlock(int label)
        {
            if (!basicBlocks.TryGetValue(label, out var block))
            {
                block = LLVM.AppendBasicBlockInContext(llvmContext.Context, currentFunction, $"label_{label}");
                basicBlocks[label] = block;
            }
            return block;
        }

        private LLVMValueRef CreateAlloca(LLVMTypeRef type, string name)
        {
            var entryBlock = LLVM.GetEntryBasicBlock(currentFunction);
            var tmpBuilder = LLVM.CreateBuilderInContext(llvmContext.Context);
            
            if (LLVM.GetFirstInstruction(entryBlock).HasValue)
            {
                LLVM.PositionBuilderBefore(tmpBuilder, LLVM.GetFirstInstruction(entryBlock).Value);
            }
            else
            {
                LLVM.PositionBuilderAtEnd(tmpBuilder, entryBlock);
            }
            
            var alloca = LLVM.BuildAlloca(tmpBuilder, type, name);
            LLVM.DisposeBuilder(tmpBuilder);
            
            return alloca;
        }

        private LLVMTypeRef MapTypeToLLVM(TypeNode type)
        {
            return type.Name switch
            {
                "void" => llvmContext.GetType("void"),
                "bool" => llvmContext.GetType("bool"),
                "int" => llvmContext.GetType("i32"),
                "long" => llvmContext.GetType("i64"),
                "float" => llvmContext.GetType("f32"),
                "double" => llvmContext.GetType("f64"),
                "string" => llvmContext.GetType("ptr"),
                _ => llvmContext.GetType(type.Name)
            };
        }
    }
} 
using System;
using System.Collections.Generic;
using System.Linq;
using Ouro.Core.VM;

namespace Ouro.Core.Compiler
{
    /// <summary>
    /// Optimizes bytecode for better performance
    /// </summary>
    public class BytecodeOptimizer
    {
        private OptimizationLevel level;
        private List<IOptimizationPass> passes;
        
        public BytecodeOptimizer(OptimizationLevel level)
        {
            this.level = level;
            this.passes = new List<IOptimizationPass>();
            
            // Add optimization passes based on level
            if (level >= OptimizationLevel.Debug)
            {
                passes.Add(new DeadCodeElimination());
                passes.Add(new ConstantFolding());
                passes.Add(new PeepholeOptimization());
            }
            
            if (level >= OptimizationLevel.Release)
            {
                passes.Add(new InstructionCombining());
                passes.Add(new JumpThreading());
                passes.Add(new CommonSubexpressionElimination());
            }
            
            if (level >= OptimizationLevel.Aggressive)
            {
                passes.Add(new LoopOptimization());
                passes.Add(new InliningOptimization());
                passes.Add(new RegisterAllocation());
            }
        }
        
        public BytecodeBuilder Optimize(BytecodeBuilder builder)
        {
            var optimized = builder;
            
            foreach (var pass in passes)
            {
                optimized = pass.Apply(optimized);
            }
            
            return optimized;
        }
    }
    
    /// <summary>
    /// Interface for optimization passes
    /// </summary>
    public interface IOptimizationPass
    {
        BytecodeBuilder Apply(BytecodeBuilder builder);
    }
    
    /// <summary>
    /// Removes unreachable code
    /// </summary>
    public class DeadCodeElimination : IOptimizationPass
    {
        public BytecodeBuilder Apply(BytecodeBuilder builder)
        {
            var bytecode = builder.GetBytecode();
            var code = bytecode.Code;
            var reachable = new HashSet<int>();
            var workList = new Queue<int>();
            
            // Start from entry point
            workList.Enqueue(0);
            reachable.Add(0);
            
            // Mark all reachable instructions
            while (workList.Count > 0)
            {
                var pos = workList.Dequeue();
                if (pos >= code.Count) continue;
                
                var opcode = (Opcode)code[pos];
                var nextPos = pos + 1 + GetOpcodeOperandSize(opcode);
                
                switch (opcode)
                {
                    case Opcode.Jump:
                    case Opcode.JMP:
                        // Unconditional jump - only follow jump target
                        var jumpTarget = ReadInt32(code, pos + 1);
                        if (!reachable.Contains(jumpTarget))
                        {
                            reachable.Add(jumpTarget);
                            workList.Enqueue(jumpTarget);
                        }
                        break;
                        
                    case Opcode.JumpIf:
                    case Opcode.JumpIfTrue:
                    case Opcode.JumpIfFalse:
                    case Opcode.JZ:
                    case Opcode.JNZ:
                        // Conditional jump - follow both paths
                        var condTarget = ReadInt32(code, pos + 1);
                        if (!reachable.Contains(condTarget))
                        {
                            reachable.Add(condTarget);
                            workList.Enqueue(condTarget);
                        }
                        if (!reachable.Contains(nextPos))
                        {
                            reachable.Add(nextPos);
                            workList.Enqueue(nextPos);
                        }
                        break;
                        
                    case Opcode.Return:
                    case Opcode.ReturnVoid:
                    case Opcode.Halt:
                    case Opcode.Throw:
                    case Opcode.Rethrow:
                        // Terminal instructions - don't follow
                        break;
                        
                    default:
                        // Normal instruction - continue to next
                        if (!reachable.Contains(nextPos))
                        {
                            reachable.Add(nextPos);
                            workList.Enqueue(nextPos);
                        }
                        break;
                }
            }
            
            // Create new bytecode with only reachable instructions
            var newBuilder = new BytecodeBuilder();
            var remapping = new Dictionary<int, int>();
            var newPos = 0;
            
            for (int i = 0; i < code.Count; )
            {
                if (reachable.Contains(i))
                {
                    remapping[i] = newPos;
                    var opcode = (Opcode)code[i];
                    var operandSize = GetOpcodeOperandSize(opcode);
                    
                    // Copy instruction
                    newBuilder.Emit(opcode);
                    for (int j = 0; j < operandSize; j++)
                    {
                        newBuilder.GetBytecode().Code.Add(code[i + 1 + j]);
                    }
                    
                    newPos += 1 + operandSize;
                    i += 1 + operandSize;
                }
                else
                {
                    // Skip unreachable instruction
                    var opcode = (Opcode)code[i];
                    i += 1 + GetOpcodeOperandSize(opcode);
                }
            }
            
            // Fix jump targets using remapping
            FixJumpTargets(newBuilder, remapping);
            
            return newBuilder;
        }
        
        public int GetOpcodeOperandSize(Opcode opcode)
        {
            switch (opcode)
            {
                case Opcode.Push:
                case Opcode.LoadLocal:
                case Opcode.StoreLocal:
                case Opcode.LoadGlobal:
                case Opcode.StoreGlobal:
                case Opcode.LoadConstant:
                case Opcode.Jump:
                case Opcode.JumpIf:
                case Opcode.JumpIfTrue:
                case Opcode.JumpIfFalse:
                case Opcode.Call:
                case Opcode.LoadField:
                case Opcode.StoreField:
                    return 4; // One 32-bit operand
                    
                case Opcode.LoadRegister:
                case Opcode.StoreRegister:
                    return 4; // Register index
                    
                default:
                    return 0; // No operands
            }
        }
        
        private int ReadInt32(List<byte> code, int pos)
        {
            return code[pos] | (code[pos + 1] << 8) | (code[pos + 2] << 16) | (code[pos + 3] << 24);
        }
        
        private void FixJumpTargets(BytecodeBuilder builder, Dictionary<int, int> remapping)
        {
            var code = builder.GetBytecode().Code;
            
            for (int i = 0; i < code.Count; )
            {
                var opcode = (Opcode)code[i];
                
                // Check if this is a jump instruction
                if (IsJumpOpcode(opcode))
                {
                    // Read the jump target
                    int oldTarget = ReadInt32(code, i + 1);
                    
                    // Find the new target position
                    int newTarget = oldTarget;
                    if (remapping.ContainsKey(oldTarget))
                    {
                        newTarget = remapping[oldTarget];
                    }
                    else
                    {
                        // Find the closest mapped position before the target
                        int closestMapped = 0;
                        foreach (var kvp in remapping)
                        {
                            if (kvp.Key <= oldTarget && kvp.Key > closestMapped)
                            {
                                closestMapped = kvp.Key;
                                newTarget = kvp.Value + (oldTarget - kvp.Key);
                            }
                        }
                    }
                    
                    // Write the new target
                    code[i + 1] = (byte)(newTarget & 0xFF);
                    code[i + 2] = (byte)((newTarget >> 8) & 0xFF);
                    code[i + 3] = (byte)((newTarget >> 16) & 0xFF);
                    code[i + 4] = (byte)((newTarget >> 24) & 0xFF);
                }
                
                i += 1 + GetOpcodeOperandSize(opcode);
            }
        }
        
        private bool IsJumpOpcode(Opcode opcode)
        {
            return opcode == Opcode.Jump || opcode == Opcode.JMP ||
                   opcode == Opcode.JumpIf || opcode == Opcode.JumpIfTrue || opcode == Opcode.JZ ||
                   opcode == Opcode.JumpIfFalse || opcode == Opcode.JumpIfNot || opcode == Opcode.JNZ ||
                   opcode == Opcode.JE || opcode == Opcode.JNE ||
                   opcode == Opcode.JL || opcode == Opcode.JG ||
                   opcode == Opcode.JLE || opcode == Opcode.JGE;
        }
    }
    
    /// <summary>
    /// Evaluates constant expressions at compile time
    /// </summary>
    public class ConstantFolding : IOptimizationPass
    {
        public BytecodeBuilder Apply(BytecodeBuilder builder)
        {
            var bytecode = builder.GetBytecode();
            var code = bytecode.Code;
            var constants = bytecode.Constants;
            var newBuilder = new BytecodeBuilder();
            
            // Copy constants
            foreach (var constant in constants)
            {
                newBuilder.AddConstant(constant);
            }
            
            var stack = new Stack<object>();
            
            for (int i = 0; i < code.Count; )
            {
                var opcode = (Opcode)code[i];
                bool folded = false;
                
                switch (opcode)
                {
                    case Opcode.LoadConstant:
                        var constIndex = ReadInt32(code, i + 1);
                        var value = constants[constIndex];
                        
                        // Look ahead for arithmetic operations
                        if (i + 5 < code.Count)
                        {
                            var nextOp = (Opcode)code[i + 5];
                            if (IsArithmeticOp(nextOp) && i + 10 < code.Count && (Opcode)code[i + 6] == Opcode.LoadConstant)
                            {
                                // Two constants followed by arithmetic op
                                var const2Index = ReadInt32(code, i + 7);
                                var value2 = constants[const2Index];
                                
                                if (value is int v1 && value2 is int v2)
                                {
                                    int result = 0;
                                    switch (nextOp)
                                    {
                                        case Opcode.Add:
                                        case Opcode.ADD:
                                            result = v1 + v2;
                                            break;
                                        case Opcode.Subtract:
                                        case Opcode.Sub:
                                        case Opcode.SUB:
                                            result = v1 - v2;
                                            break;
                                        case Opcode.Multiply:
                                        case Opcode.Mul:
                                        case Opcode.MUL:
                                            result = v1 * v2;
                                            break;
                                        case Opcode.Divide:
                                        case Opcode.Div:
                                        case Opcode.DIV:
                                            if (v2 != 0)
                                                result = v1 / v2;
                                            else
                                                goto skip_fold;
                                            break;
                                        case Opcode.Modulo:
                                        case Opcode.Mod:
                                        case Opcode.MOD:
                                            if (v2 != 0)
                                                result = v1 % v2;
                                            else
                                                goto skip_fold;
                                            break;
                                    }
                                    
                                    // Replace with single constant load
                                    newBuilder.Emit(Opcode.LoadConstant, newBuilder.AddConstant(result));
                                    i += 11; // Skip all three instructions
                                    folded = true;
                                }
                                else if (value is double d1 && value2 is double d2)
                                {
                                    double result = 0;
                                    switch (nextOp)
                                    {
                                        case Opcode.Add:
                                        case Opcode.ADD:
                                            result = d1 + d2;
                                            break;
                                        case Opcode.Subtract:
                                        case Opcode.Sub:
                                        case Opcode.SUB:
                                            result = d1 - d2;
                                            break;
                                        case Opcode.Multiply:
                                        case Opcode.Mul:
                                        case Opcode.MUL:
                                            result = d1 * d2;
                                            break;
                                        case Opcode.Divide:
                                        case Opcode.Div:
                                        case Opcode.DIV:
                                            if (d2 != 0)
                                                result = d1 / d2;
                                            else
                                                goto skip_fold;
                                            break;
                                    }
                                    
                                    // Replace with single constant load
                                    newBuilder.Emit(Opcode.LoadConstant, newBuilder.AddConstant(result));
                                    i += 11; // Skip all three instructions
                                    folded = true;
                                }
                                else if (value is string s1 && value2 is string s2 && nextOp == Opcode.StringConcat)
                                {
                                    // Fold string concatenation
                                    newBuilder.Emit(Opcode.LoadConstant, newBuilder.AddConstant(s1 + s2));
                                    i += 11;
                                    folded = true;
                                }
                            }
                        }
                        skip_fold:
                        break;
                        
                    case Opcode.LoadTrue:
                        // Look ahead for logical operations
                        if (i + 1 < code.Count)
                        {
                            var nextOp = (Opcode)code[i + 1];
                            if (nextOp == Opcode.Not || nextOp == Opcode.LogicalNot)
                            {
                                newBuilder.Emit(Opcode.LoadFalse);
                                i += 2;
                                folded = true;
                            }
                        }
                        break;
                        
                    case Opcode.LoadFalse:
                        // Look ahead for logical operations
                        if (i + 1 < code.Count)
                        {
                            var nextOp = (Opcode)code[i + 1];
                            if (nextOp == Opcode.Not || nextOp == Opcode.LogicalNot)
                            {
                                newBuilder.Emit(Opcode.LoadTrue);
                                i += 2;
                                folded = true;
                            }
                        }
                        break;
                }
                
                if (!folded)
                {
                    // Copy instruction as-is
                    newBuilder.GetBytecode().Code.Add(code[i]);
                    var operandSize = GetOpcodeOperandSize(opcode);
                    for (int j = 0; j < operandSize; j++)
                    {
                        newBuilder.GetBytecode().Code.Add(code[i + 1 + j]);
                    }
                    i += 1 + operandSize;
                }
            }
            
            return newBuilder;
        }
        
        private bool IsArithmeticOp(Opcode opcode)
        {
            return opcode == Opcode.Add || opcode == Opcode.ADD ||
                   opcode == Opcode.Subtract || opcode == Opcode.Sub || opcode == Opcode.SUB ||
                   opcode == Opcode.Multiply || opcode == Opcode.Mul || opcode == Opcode.MUL ||
                   opcode == Opcode.Divide || opcode == Opcode.Div || opcode == Opcode.DIV ||
                   opcode == Opcode.Modulo || opcode == Opcode.Mod || opcode == Opcode.MOD ||
                   opcode == Opcode.StringConcat;
        }
        
        private int GetOpcodeOperandSize(Opcode opcode)
        {
            // Reuse from DeadCodeElimination
            return new DeadCodeElimination().GetOpcodeOperandSize(opcode);
        }
        
        private int ReadInt32(List<byte> code, int pos)
        {
            return code[pos] | (code[pos + 1] << 8) | (code[pos + 2] << 16) | (code[pos + 3] << 24);
        }
    }
    
    /// <summary>
    /// Local optimizations on small instruction sequences
    /// </summary>
    public class PeepholeOptimization : IOptimizationPass
    {
        public BytecodeBuilder Apply(BytecodeBuilder builder)
        {
            var bytecode = builder.GetBytecode();
            var code = bytecode.Code;
            var newBuilder = new BytecodeBuilder();
            
            // Copy constants
            foreach (var constant in bytecode.Constants)
            {
                newBuilder.AddConstant(constant);
            }
            
            for (int i = 0; i < code.Count; )
            {
                var opcode = (Opcode)code[i];
                bool optimized = false;
                
                // Pattern: Load + Store to same location = no-op
                if (opcode == Opcode.LoadLocal && i + 5 < code.Count)
                {
                    var nextOp = (Opcode)code[i + 5];
                    if (nextOp == Opcode.StoreLocal)
                    {
                        var loadIndex = ReadInt32(code, i + 1);
                        var storeIndex = ReadInt32(code, i + 6);
                        if (loadIndex == storeIndex)
                        {
                            // Skip both instructions
                            i += 10;
                            optimized = true;
                        }
                    }
                }
                
                // Pattern: Dup + Pop = no-op
                else if ((opcode == Opcode.Dup || opcode == Opcode.Duplicate || opcode == Opcode.DUP) && 
                         i + 1 < code.Count && 
                         ((Opcode)code[i + 1] == Opcode.Pop || (Opcode)code[i + 1] == Opcode.POP))
                {
                    // Skip both instructions
                    i += 2;
                    optimized = true;
                }
                
                // Pattern: Push 0 + Add = no-op
                else if (opcode == Opcode.LoadConstant && i + 5 < code.Count)
                {
                    var constIndex = ReadInt32(code, i + 1);
                    var value = bytecode.Constants[constIndex];
                    var nextOp = (Opcode)code[i + 5];
                    
                    if ((value is int iv && iv == 0 || value is double dv && dv == 0.0) &&
                        (nextOp == Opcode.Add || nextOp == Opcode.ADD))
                    {
                        // Skip both instructions
                        i += 6;
                        optimized = true;
                    }
                    // Pattern: Push 1 + Mul = no-op
                    else if ((value is int iv2 && iv2 == 1 || value is double dv2 && dv2 == 1.0) &&
                             (nextOp == Opcode.Multiply || nextOp == Opcode.Mul || nextOp == Opcode.MUL))
                    {
                        // Skip both instructions
                        i += 6;
                        optimized = true;
                    }
                }
                
                // Pattern: Double negation
                else if ((opcode == Opcode.Negate || opcode == Opcode.Neg || opcode == Opcode.NEG) &&
                         i + 1 < code.Count &&
                         ((Opcode)code[i + 1] == Opcode.Negate || (Opcode)code[i + 1] == Opcode.Neg || (Opcode)code[i + 1] == Opcode.NEG))
                {
                    // Skip both instructions
                    i += 2;
                    optimized = true;
                }
                
                // Pattern: Not + Not = no-op
                else if ((opcode == Opcode.Not || opcode == Opcode.LogicalNot || opcode == Opcode.NOT) &&
                         i + 1 < code.Count &&
                         ((Opcode)code[i + 1] == Opcode.Not || (Opcode)code[i + 1] == Opcode.LogicalNot || (Opcode)code[i + 1] == Opcode.NOT))
                {
                    // Skip both instructions
                    i += 2;
                    optimized = true;
                }
                
                // Pattern: Jump to next instruction
                else if ((opcode == Opcode.Jump || opcode == Opcode.JMP) && i + 5 < code.Count)
                {
                    var target = ReadInt32(code, i + 1);
                    if (target == i + 5)
                    {
                        // Skip jump
                        i += 5;
                        optimized = true;
                    }
                }
                
                if (!optimized)
                {
                    // Copy instruction as-is
                    newBuilder.GetBytecode().Code.Add(code[i]);
                    var operandSize = GetOpcodeOperandSize(opcode);
                    for (int j = 0; j < operandSize; j++)
                    {
                        newBuilder.GetBytecode().Code.Add(code[i + 1 + j]);
                    }
                    i += 1 + operandSize;
                }
            }
            
            return newBuilder;
        }
        
        private int GetOpcodeOperandSize(Opcode opcode)
        {
            return new DeadCodeElimination().GetOpcodeOperandSize(opcode);
        }
        
        private int ReadInt32(List<byte> code, int pos)
        {
            return code[pos] | (code[pos + 1] << 8) | (code[pos + 2] << 16) | (code[pos + 3] << 24);
        }
    }
    
    /// <summary>
    /// Combines multiple instructions into single operations
    /// </summary>
    public class InstructionCombining : IOptimizationPass
    {
        public BytecodeBuilder Apply(BytecodeBuilder builder)
        {
            var bytecode = builder.GetBytecode();
            var code = bytecode.Code;
            var newBuilder = new BytecodeBuilder();
            
            // Copy constants
            foreach (var constant in bytecode.Constants)
            {
                newBuilder.AddConstant(constant);
            }
            
            for (int i = 0; i < code.Count; )
            {
                var opcode = (Opcode)code[i];
                bool combined = false;
                
                // Pattern: LoadLocal + Add/Sub 1 + StoreLocal = Pre/PostIncrement/Decrement
                if (opcode == Opcode.LoadLocal && i + 15 < code.Count)
                {
                    var localIndex = ReadInt32(code, i + 1);
                    var nextOp = (Opcode)code[i + 5];
                    
                    if (nextOp == Opcode.LoadConstant)
                    {
                        var constIndex = ReadInt32(code, i + 6);
                        var value = bytecode.Constants[constIndex];
                        var arithmeticOp = (Opcode)code[i + 10];
                        var storeOp = (Opcode)code[i + 11];
                        
                        if (value is int iv && Math.Abs(iv) == 1 && storeOp == Opcode.StoreLocal)
                        {
                            var storeIndex = ReadInt32(code, i + 12);
                            if (localIndex == storeIndex)
                            {
                                if ((arithmeticOp == Opcode.Add || arithmeticOp == Opcode.ADD) && iv == 1)
                                {
                                    newBuilder.Emit(Opcode.PostIncrement, localIndex);
                                    i += 16;
                                    combined = true;
                                }
                                else if ((arithmeticOp == Opcode.Subtract || arithmeticOp == Opcode.Sub || arithmeticOp == Opcode.SUB) && iv == 1)
                                {
                                    newBuilder.Emit(Opcode.PostDecrement, localIndex);
                                    i += 16;
                                    combined = true;
                                }
                            }
                        }
                    }
                }
                
                // Pattern: LoadMember + LoadMember = optimize to single complex load
                else if (opcode == Opcode.LoadMember && i + 5 < code.Count && (Opcode)code[i + 5] == Opcode.LoadMember)
                {
                    // Could combine into a single LoadNestedMember instruction if VM supports it
                    // For now, keep as-is
                }
                
                // Pattern: Multiple Dup operations = optimize to Dup2, Dup3, etc.
                else if ((opcode == Opcode.Dup || opcode == Opcode.Duplicate || opcode == Opcode.DUP) &&
                         i + 1 < code.Count &&
                         ((Opcode)code[i + 1] == Opcode.Dup || (Opcode)code[i + 1] == Opcode.Duplicate || (Opcode)code[i + 1] == Opcode.DUP))
                {
                    newBuilder.Emit(Opcode.Dup2);
                    i += 2;
                    combined = true;
                }
                
                if (!combined)
                {
                    // Copy instruction as-is
                    newBuilder.GetBytecode().Code.Add(code[i]);
                    var operandSize = GetOpcodeOperandSize(opcode);
                    for (int j = 0; j < operandSize; j++)
                    {
                        newBuilder.GetBytecode().Code.Add(code[i + 1 + j]);
                    }
                    i += 1 + operandSize;
                }
            }
            
            return newBuilder;
        }
        
        private int GetOpcodeOperandSize(Opcode opcode)
        {
            return new DeadCodeElimination().GetOpcodeOperandSize(opcode);
        }
        
        private int ReadInt32(List<byte> code, int pos)
        {
            return code[pos] | (code[pos + 1] << 8) | (code[pos + 2] << 16) | (code[pos + 3] << 24);
        }
    }
    
    /// <summary>
    /// Optimizes jump targets
    /// </summary>
    public class JumpThreading : IOptimizationPass
    {
        public BytecodeBuilder Apply(BytecodeBuilder builder)
        {
            var bytecode = builder.GetBytecode();
            var code = bytecode.Code;
            var newBuilder = new BytecodeBuilder();
            
            // Copy constants
            foreach (var constant in bytecode.Constants)
            {
                newBuilder.AddConstant(constant);
            }
            
            // First pass: identify jump targets
            var jumpTargets = new Dictionary<int, int>();
            for (int i = 0; i < code.Count; )
            {
                var opcode = (Opcode)code[i];
                if (IsJumpOpcode(opcode))
                {
                    var target = ReadInt32(code, i + 1);
                    // Check if target is another jump
                    if (target < code.Count && IsJumpOpcode((Opcode)code[target]))
                    {
                        var finalTarget = ReadInt32(code, target + 1);
                        jumpTargets[i] = finalTarget;
                    }
                }
                i += 1 + GetOpcodeOperandSize(opcode);
            }
            
            // Second pass: apply optimizations
            for (int i = 0; i < code.Count; )
            {
                var opcode = (Opcode)code[i];
                
                if (IsJumpOpcode(opcode) && jumpTargets.ContainsKey(i))
                {
                    // Replace with optimized jump
                    newBuilder.GetBytecode().Code.Add(code[i]);
                    WriteInt32(newBuilder.GetBytecode().Code, jumpTargets[i]);
                    i += 5;
                }
                else
                {
                    // Copy instruction as-is
                    newBuilder.GetBytecode().Code.Add(code[i]);
                    var operandSize = GetOpcodeOperandSize(opcode);
                    for (int j = 0; j < operandSize; j++)
                    {
                        newBuilder.GetBytecode().Code.Add(code[i + 1 + j]);
                    }
                    i += 1 + operandSize;
                }
            }
            
            return newBuilder;
        }
        
        public bool IsJumpOpcode(Opcode opcode)
        {
            return opcode == Opcode.Jump || opcode == Opcode.JMP ||
                   opcode == Opcode.JumpIf || opcode == Opcode.JumpIfTrue || opcode == Opcode.JumpIfFalse ||
                   opcode == Opcode.JZ || opcode == Opcode.JNZ || 
                   opcode == Opcode.JE || opcode == Opcode.JNE ||
                   opcode == Opcode.JL || opcode == Opcode.JG ||
                   opcode == Opcode.JLE || opcode == Opcode.JGE;
        }
        
        private int GetOpcodeOperandSize(Opcode opcode)
        {
            return new DeadCodeElimination().GetOpcodeOperandSize(opcode);
        }
        
        private int ReadInt32(List<byte> code, int pos)
        {
            return code[pos] | (code[pos + 1] << 8) | (code[pos + 2] << 16) | (code[pos + 3] << 24);
        }
        
        private void WriteInt32(List<byte> code, int value)
        {
            code.Add((byte)(value & 0xFF));
            code.Add((byte)((value >> 8) & 0xFF));
            code.Add((byte)((value >> 16) & 0xFF));
            code.Add((byte)((value >> 24) & 0xFF));
        }
    }
    
    /// <summary>
    /// Eliminates duplicate computations
    /// </summary>
    public class CommonSubexpressionElimination : IOptimizationPass
    {
        public BytecodeBuilder Apply(BytecodeBuilder builder)
        {
            var bytecode = builder.GetBytecode();
            var code = bytecode.Code;
            var newBuilder = new BytecodeBuilder();
            
            // Copy constants
            foreach (var constant in bytecode.Constants)
            {
                newBuilder.AddConstant(constant);
            }
            
            // Track expressions and their results
            var expressions = new Dictionary<string, int>(); // expression hash -> local var index
            var nextLocal = 0;
            
            for (int i = 0; i < code.Count; )
            {
                var opcode = (Opcode)code[i];
                
                // Check for arithmetic expressions
                if (IsArithmeticOp(opcode) && i >= 10)
                {
                    // Look back for operands
                    var prevOp1 = (Opcode)code[i - 10];
                    var prevOp2 = (Opcode)code[i - 5];
                    
                    if (prevOp1 == Opcode.LoadConstant && prevOp2 == Opcode.LoadConstant)
                    {
                        var const1 = ReadInt32(code, i - 9);
                        var const2 = ReadInt32(code, i - 4);
                        var exprHash = $"{const1}_{opcode}_{const2}";
                        
                        if (expressions.ContainsKey(exprHash))
                        {
                            // Replace with load of cached result
                            // Remove the two loads and the operation
                            newBuilder.GetBytecode().Code.RemoveRange(newBuilder.GetBytecode().Code.Count - 10, 10);
                            newBuilder.Emit(Opcode.LoadLocal, expressions[exprHash]);
                            i += 1;
                        }
                        else
                        {
                            // Store result for future use
                            newBuilder.GetBytecode().Code.Add(code[i]);
                            newBuilder.Emit(Opcode.Dup); // Duplicate result
                            newBuilder.Emit(Opcode.StoreLocal, nextLocal);
                            expressions[exprHash] = nextLocal++;
                            i += 1;
                        }
                    }
                    else
                    {
                        // Copy as-is
                        newBuilder.GetBytecode().Code.Add(code[i]);
                        i += 1;
                    }
                }
                else
                {
                    // Copy instruction as-is
                    newBuilder.GetBytecode().Code.Add(code[i]);
                    var operandSize = GetOpcodeOperandSize(opcode);
                    for (int j = 0; j < operandSize; j++)
                    {
                        if (i + 1 + j < code.Count)
                            newBuilder.GetBytecode().Code.Add(code[i + 1 + j]);
                    }
                    i += 1 + operandSize;
                }
            }
            
            return newBuilder;
        }
        
        private bool IsArithmeticOp(Opcode opcode)
        {
            return opcode == Opcode.Add || opcode == Opcode.ADD ||
                   opcode == Opcode.Subtract || opcode == Opcode.Sub || opcode == Opcode.SUB ||
                   opcode == Opcode.Multiply || opcode == Opcode.Mul || opcode == Opcode.MUL ||
                   opcode == Opcode.Divide || opcode == Opcode.Div || opcode == Opcode.DIV;
        }
        
        private int GetOpcodeOperandSize(Opcode opcode)
        {
            return new DeadCodeElimination().GetOpcodeOperandSize(opcode);
        }
        
        private int ReadInt32(List<byte> code, int pos)
        {
            return code[pos] | (code[pos + 1] << 8) | (code[pos + 2] << 16) | (code[pos + 3] << 24);
        }
    }
    
    /// <summary>
    /// Optimizes loop performance
    /// </summary>
    public class LoopOptimization : IOptimizationPass
    {
        public BytecodeBuilder Apply(BytecodeBuilder builder)
        {
            var bytecode = builder.GetBytecode();
            var code = bytecode.Code;
            var newBuilder = new BytecodeBuilder();
            
            // Copy constants
            foreach (var constant in bytecode.Constants)
            {
                newBuilder.AddConstant(constant);
            }
            
            // Identify loops
            var loops = IdentifyLoops(code);
            
            for (int i = 0; i < code.Count; )
            {
                bool optimized = false;
                
                // Check if we're at a loop start
                foreach (var loop in loops)
                {
                    if (i == loop.Start)
                    {
                        // Apply loop optimizations
                        if (CanUnrollLoop(code, loop))
                        {
                            // Simple loop unrolling for small constant loops
                            UnrollLoop(newBuilder, code, loop);
                            i = loop.End + 1;
                            optimized = true;
                            break;
                        }
                        else if (HasLoopInvariant(code, loop))
                        {
                            // Move loop-invariant code outside
                            HoistInvariantCode(newBuilder, code, loop);
                            i = loop.End + 1;
                            optimized = true;
                            break;
                        }
                    }
                }
                
                if (!optimized)
                {
                    // Copy instruction as-is
                    var opcode = (Opcode)code[i];
                    newBuilder.GetBytecode().Code.Add(code[i]);
                    var operandSize = GetOpcodeOperandSize(opcode);
                    for (int j = 0; j < operandSize; j++)
                    {
                        if (i + 1 + j < code.Count)
                            newBuilder.GetBytecode().Code.Add(code[i + 1 + j]);
                    }
                    i += 1 + operandSize;
                }
            }
            
            return newBuilder;
        }
        
        private List<Loop> IdentifyLoops(List<byte> code)
        {
            var loops = new List<Loop>();
            
            // Simple loop detection - look for backward jumps
            for (int i = 0; i < code.Count; )
            {
                var opcode = (Opcode)code[i];
                if (IsBackwardJump(opcode, i, code))
                {
                    var target = ReadInt32(code, i + 1);
                    loops.Add(new Loop { Start = target, End = i + 4 });
                }
                i += 1 + GetOpcodeOperandSize(opcode);
            }
            
            return loops;
        }
        
        private bool IsBackwardJump(Opcode opcode, int position, List<byte> code)
        {
            if (!IsJumpOpcode(opcode)) return false;
            var target = ReadInt32(code, position + 1);
            return target < position;
        }
        
        private bool CanUnrollLoop(List<byte> code, Loop loop)
        {
            // Only unroll very small loops
            return (loop.End - loop.Start) < 20;
        }
        
        private void UnrollLoop(BytecodeBuilder builder, List<byte> code, Loop loop)
        {
            // Simple 2x unrolling
            for (int unroll = 0; unroll < 2; unroll++)
            {
                for (int i = loop.Start; i <= loop.End - 5; i++)
                {
                    builder.GetBytecode().Code.Add(code[i]);
                }
            }
        }
        
        private bool HasLoopInvariant(List<byte> code, Loop loop)
        {
            // Check for loads of constants or globals that don't change
            for (int i = loop.Start; i < loop.End; i++)
            {
                var opcode = (Opcode)code[i];
                if (opcode == Opcode.LoadConstant || opcode == Opcode.LoadGlobal)
                    return true;
            }
            return false;
        }
        
        private void HoistInvariantCode(BytecodeBuilder builder, List<byte> code, Loop loop)
        {
            // Move invariant loads before the loop
            for (int i = loop.Start; i < loop.End; )
            {
                var opcode = (Opcode)code[i];
                if (opcode == Opcode.LoadConstant || opcode == Opcode.LoadGlobal)
                {
                    // Copy to before loop
                    builder.GetBytecode().Code.Add(code[i]);
                    var operandSize = GetOpcodeOperandSize(opcode);
                    for (int j = 0; j < operandSize; j++)
                    {
                        builder.GetBytecode().Code.Add(code[i + 1 + j]);
                    }
                    i += 1 + operandSize;
                }
                else
                {
                    i++;
                }
            }
            
            // Copy the rest of the loop
            for (int i = loop.Start; i <= loop.End; i++)
            {
                builder.GetBytecode().Code.Add(code[i]);
            }
        }
        
        private bool IsJumpOpcode(Opcode opcode)
        {
            return new JumpThreading().IsJumpOpcode(opcode);
        }
        
        private int GetOpcodeOperandSize(Opcode opcode)
        {
            return new DeadCodeElimination().GetOpcodeOperandSize(opcode);
        }
        
        private int ReadInt32(List<byte> code, int pos)
        {
            return code[pos] | (code[pos + 1] << 8) | (code[pos + 2] << 16) | (code[pos + 3] << 24);
        }
        
        private class Loop
        {
            public int Start { get; set; }
            public int End { get; set; }
        }
    }
    
    /// <summary>
    /// Inlines small functions
    /// </summary>
    public class InliningOptimization : IOptimizationPass
    {
        private const int MaxInlineSize = 10; // Max instructions for inlining
        
        public BytecodeBuilder Apply(BytecodeBuilder builder)
        {
            var bytecode = builder.GetBytecode();
            var code = bytecode.Code;
            var newBuilder = new BytecodeBuilder();
            
            // Copy constants
            foreach (var constant in bytecode.Constants)
            {
                newBuilder.AddConstant(constant);
            }
            
            // Build function table (simplified - would need actual function info)
            var functions = new Dictionary<int, FunctionInfo>();
            
            for (int i = 0; i < code.Count; )
            {
                var opcode = (Opcode)code[i];
                
                if (opcode == Opcode.Call && functions.ContainsKey(ReadInt32(code, i + 1)))
                {
                    var funcIndex = ReadInt32(code, i + 1);
                    var func = functions[funcIndex];
                    
                    if (func.Size <= MaxInlineSize && !func.IsRecursive)
                    {
                        // Inline the function
                        InlineFunction(newBuilder, func);
                        i += 5; // Skip call instruction
                    }
                    else
                    {
                        // Copy call as-is
                        CopyInstruction(newBuilder, code, i);
                        i += 5;
                    }
                }
                else
                {
                    // Copy instruction as-is
                    var operandSize = GetOpcodeOperandSize(opcode);
                    CopyInstruction(newBuilder, code, i);
                    i += 1 + operandSize;
                }
            }
            
            return newBuilder;
        }
        
        private void InlineFunction(BytecodeBuilder builder, FunctionInfo func)
        {
            // Copy function body (simplified - would need proper parameter handling)
            foreach (var instruction in func.Instructions)
            {
                if (instruction.Opcode != Opcode.Return && instruction.Opcode != Opcode.ReturnVoid)
                {
                    builder.GetBytecode().Code.Add((byte)instruction.Opcode);
                    foreach (var operand in instruction.Operands)
                    {
                        builder.GetBytecode().Code.Add(operand);
                    }
                }
            }
        }
        
        private void CopyInstruction(BytecodeBuilder builder, List<byte> code, int position)
        {
            var opcode = (Opcode)code[position];
            builder.GetBytecode().Code.Add(code[position]);
            var operandSize = GetOpcodeOperandSize(opcode);
            for (int j = 0; j < operandSize; j++)
            {
                if (position + 1 + j < code.Count)
                    builder.GetBytecode().Code.Add(code[position + 1 + j]);
            }
        }
        
        private int GetOpcodeOperandSize(Opcode opcode)
        {
            return new DeadCodeElimination().GetOpcodeOperandSize(opcode);
        }
        
        private int ReadInt32(List<byte> code, int pos)
        {
            return code[pos] | (code[pos + 1] << 8) | (code[pos + 2] << 16) | (code[pos + 3] << 24);
        }
        
        private class FunctionInfo
        {
            public int Index { get; set; }
            public int Size { get; set; }
            public bool IsRecursive { get; set; }
            public List<Instruction> Instructions { get; set; } = new List<Instruction>();
        }
        
        private class Instruction
        {
            public Opcode Opcode { get; set; }
            public List<byte> Operands { get; set; } = new List<byte>();
        }
    }
    
    /// <summary>
    /// Allocates variables to registers
    /// </summary>
    public class RegisterAllocation : IOptimizationPass
    {
        private const int NumRegisters = 16; // x64 has 16 general purpose registers
        
        public BytecodeBuilder Apply(BytecodeBuilder builder)
        {
            var bytecode = builder.GetBytecode();
            var code = bytecode.Code;
            var newBuilder = new BytecodeBuilder();
            
            // Copy constants
            foreach (var constant in bytecode.Constants)
            {
                newBuilder.AddConstant(constant);
            }
            
            // Build variable lifetime information
            var variables = AnalyzeVariableLifetimes(code);
            
            // Build interference graph
            var interferenceGraph = BuildInterferenceGraph(variables);
            
            // Color the graph (assign registers)
            var allocation = ColorGraph(interferenceGraph, NumRegisters);
            
            // Apply register allocation
            for (int i = 0; i < code.Count; )
            {
                var opcode = (Opcode)code[i];
                
                if (opcode == Opcode.LoadLocal || opcode == Opcode.StoreLocal)
                {
                    var localIndex = ReadInt32(code, i + 1);
                    if (allocation.ContainsKey(localIndex))
                    {
                        // Replace with register operation
                        var regOpcode = opcode == Opcode.LoadLocal ? Opcode.LoadRegister : Opcode.StoreRegister;
                        newBuilder.Emit(regOpcode, allocation[localIndex]);
                        i += 5;
                    }
                    else
                    {
                        // Keep as memory access (spilled)
                        CopyInstruction(newBuilder, code, i);
                        i += 5;
                    }
                }
                else
                {
                    // Copy instruction as-is
                    var operandSize = GetOpcodeOperandSize(opcode);
                    CopyInstruction(newBuilder, code, i);
                    i += 1 + operandSize;
                }
            }
            
            return newBuilder;
        }
        
        private Dictionary<int, VariableLifetime> AnalyzeVariableLifetimes(List<byte> code)
        {
            var lifetimes = new Dictionary<int, VariableLifetime>();
            
            for (int i = 0; i < code.Count; )
            {
                var opcode = (Opcode)code[i];
                
                if (opcode == Opcode.LoadLocal || opcode == Opcode.StoreLocal)
                {
                    var localIndex = ReadInt32(code, i + 1);
                    if (!lifetimes.ContainsKey(localIndex))
                    {
                        lifetimes[localIndex] = new VariableLifetime { Index = localIndex };
                    }
                    
                    lifetimes[localIndex].FirstUse = Math.Min(lifetimes[localIndex].FirstUse, i);
                    lifetimes[localIndex].LastUse = Math.Max(lifetimes[localIndex].LastUse, i);
                }
                
                i += 1 + GetOpcodeOperandSize(opcode);
            }
            
            return lifetimes;
        }
        
        private Dictionary<int, HashSet<int>> BuildInterferenceGraph(Dictionary<int, VariableLifetime> lifetimes)
        {
            var graph = new Dictionary<int, HashSet<int>>();
            
            foreach (var var1 in lifetimes.Values)
            {
                graph[var1.Index] = new HashSet<int>();
                
                foreach (var var2 in lifetimes.Values)
                {
                    if (var1.Index != var2.Index && var1.InterferesWith(var2))
                    {
                        graph[var1.Index].Add(var2.Index);
                    }
                }
            }
            
            return graph;
        }
        
        private Dictionary<int, int> ColorGraph(Dictionary<int, HashSet<int>> graph, int numColors)
        {
            var allocation = new Dictionary<int, int>();
            var availableColors = new bool[numColors];
            
            // Simple greedy coloring
            foreach (var node in graph.Keys.OrderByDescending(n => graph[n].Count))
            {
                // Reset available colors
                for (int i = 0; i < numColors; i++)
                    availableColors[i] = true;
                
                // Mark colors used by neighbors
                foreach (var neighbor in graph[node])
                {
                    if (allocation.ContainsKey(neighbor))
                    {
                        availableColors[allocation[neighbor]] = false;
                    }
                }
                
                // Find first available color
                for (int color = 0; color < numColors; color++)
                {
                    if (availableColors[color])
                    {
                        allocation[node] = color;
                        break;
                    }
                }
                // If no color available, variable will be spilled to memory
            }
            
            return allocation;
        }
        
        private void CopyInstruction(BytecodeBuilder builder, List<byte> code, int position)
        {
            var opcode = (Opcode)code[position];
            builder.GetBytecode().Code.Add(code[position]);
            var operandSize = GetOpcodeOperandSize(opcode);
            for (int j = 0; j < operandSize; j++)
            {
                if (position + 1 + j < code.Count)
                    builder.GetBytecode().Code.Add(code[position + 1 + j]);
            }
        }
        
        private int GetOpcodeOperandSize(Opcode opcode)
        {
            return new DeadCodeElimination().GetOpcodeOperandSize(opcode);
        }
        
        private int ReadInt32(List<byte> code, int pos)
        {
            return code[pos] | (code[pos + 1] << 8) | (code[pos + 2] << 16) | (code[pos + 3] << 24);
        }
        
        private class VariableLifetime
        {
            public int Index { get; set; }
            public int FirstUse { get; set; } = int.MaxValue;
            public int LastUse { get; set; } = 0;
            
            public bool InterferesWith(VariableLifetime other)
            {
                return !(LastUse < other.FirstUse || other.LastUse < FirstUse);
            }
        }
    }
} 
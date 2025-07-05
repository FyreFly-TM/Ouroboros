using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ouro.Core.Lexer;
using Ouro.Core.VM;

namespace Ouro.Core.Assembly
{
    /// <summary>
    /// Assembler for Ouroboros inline assembly support
    /// </summary>
    public class Assembler
    {
        private readonly Dictionary<string, ushort> opcodeMap;
        private readonly Dictionary<string, int> labelMap;
        private readonly List<AssemblyInstruction> instructions;
        private readonly List<byte> output;
        private int currentAddress;
        
        public Assembler()
        {
            opcodeMap = InitializeOpcodeMap();
            labelMap = new Dictionary<string, int>();
            instructions = new List<AssemblyInstruction>();
            output = new List<byte>();
            currentAddress = 0;
        }
        
        private Dictionary<string, ushort> InitializeOpcodeMap()
        {
            var map = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase);
            
            // Stack operations
            map["push"] = (ushort)Opcode.PUSH;
            map["pop"] = (ushort)Opcode.POP;
            map["dup"] = (ushort)Opcode.DUP;
            map["swap"] = (ushort)Opcode.SWAP;
            
            // Arithmetic
            map["add"] = (ushort)Opcode.ADD;
            map["sub"] = (ushort)Opcode.SUB;
            map["mul"] = (ushort)Opcode.MUL;
            map["div"] = (ushort)Opcode.DIV;
            map["mod"] = (ushort)Opcode.MOD;
            map["neg"] = (ushort)Opcode.NEG;
            
            // Extended arithmetic
            map["imul"] = (ushort)ExtendedOpcode.IMUL;
            map["idiv"] = (ushort)ExtendedOpcode.IDIV;
            map["shl"] = (ushort)Opcode.SHL;
            map["shr"] = (ushort)Opcode.SHR;
            map["sar"] = (ushort)ExtendedOpcode.SAR;
            map["rol"] = (ushort)ExtendedOpcode.ROL;
            map["ror"] = (ushort)ExtendedOpcode.ROR;
            
            // Bitwise
            map["and"] = (ushort)Opcode.AND;
            map["or"] = (ushort)Opcode.OR;
            map["xor"] = (ushort)Opcode.XOR;
            map["not"] = (ushort)Opcode.NOT;
            map["shl"] = (ushort)Opcode.SHL;
            map["shr"] = (ushort)Opcode.SHR;
            
            // Comparison
            map["eq"] = (ushort)Opcode.EQ;
            map["ne"] = (ushort)Opcode.NE;
            map["lt"] = (ushort)Opcode.LT;
            map["gt"] = (ushort)Opcode.GT;
            map["le"] = (ushort)Opcode.LE;
            map["ge"] = (ushort)Opcode.GE;
            
            // Control flow
            map["jmp"] = (ushort)Opcode.JMP;
            map["jz"] = (ushort)Opcode.JZ;
            map["jnz"] = (ushort)Opcode.JNZ;
            map["call"] = (ushort)Opcode.CALL;
            map["ret"] = (ushort)Opcode.RET;
            
            // Memory
            map["load"] = (ushort)Opcode.LOAD;
            map["store"] = (ushort)Opcode.STORE;
            map["alloc"] = (ushort)Opcode.ALLOC;
            map["free"] = (ushort)Opcode.FREE;
            
            // x86-style mnemonics
            map["mov"] = (ushort)Opcode.LOAD;
            map["movb"] = (ushort)Opcode.LOAD_BYTE;
            map["movw"] = (ushort)Opcode.LOAD_WORD;
            map["movl"] = (ushort)Opcode.LOAD_DWORD;
            map["movq"] = (ushort)Opcode.LOAD_QWORD;
            
            // x86 store variants
            map["movb.s"] = (ushort)ExtendedOpcode.STORE_BYTE;
            map["movw.s"] = (ushort)ExtendedOpcode.STORE_WORD;
            map["movl.s"] = (ushort)ExtendedOpcode.STORE_DWORD;
            map["movq.s"] = (ushort)ExtendedOpcode.STORE_QWORD;
            
            map["inc"] = (ushort)Opcode.INC;
            map["dec"] = (ushort)Opcode.DEC;
            
            map["cmp"] = (ushort)Opcode.CMP;
            map["test"] = (ushort)Opcode.TEST;
            
            map["je"] = (ushort)Opcode.JE;
            map["jne"] = (ushort)Opcode.JNE;
            map["jl"] = (ushort)Opcode.JL;
            map["jg"] = (ushort)Opcode.JG;
            map["jle"] = (ushort)Opcode.JLE;
            map["jge"] = (ushort)Opcode.JGE;
            
            // Additional x86 jumps
            map["ja"] = (ushort)ExtendedOpcode.JA;    // Jump if above
            map["jae"] = (ushort)ExtendedOpcode.JAE;  // Jump if above or equal
            map["jb"] = (ushort)ExtendedOpcode.JB;    // Jump if below
            map["jbe"] = (ushort)ExtendedOpcode.JBE;  // Jump if below or equal
            map["jo"] = (ushort)ExtendedOpcode.JO;    // Jump if overflow
            map["jno"] = (ushort)ExtendedOpcode.JNO;  // Jump if not overflow
            map["js"] = (ushort)ExtendedOpcode.JS;    // Jump if sign
            map["jns"] = (ushort)ExtendedOpcode.JNS;  // Jump if not sign
            
            // String operations
            map["movsb"] = (ushort)ExtendedOpcode.MOVSB;
            map["movsw"] = (ushort)ExtendedOpcode.MOVSW;
            map["movsd"] = (ushort)ExtendedOpcode.MOVSD;
            map["rep"] = (ushort)ExtendedOpcode.REP;
            
            // Stack frame
            map["enter"] = (ushort)ExtendedOpcode.ENTER;
            map["leave"] = (ushort)ExtendedOpcode.LEAVE;
            
            // Other
            map["nop"] = (ushort)Opcode.NOP;
            map["halt"] = (ushort)Opcode.HALT;
            map["hlt"] = (ushort)Opcode.HALT;
            map["int"] = (ushort)ExtendedOpcode.INT;
            map["syscall"] = (ushort)ExtendedOpcode.SYSCALL;
            
            return map;
        }
        
        /// <summary>
        /// Assemble source code to bytecode
        /// </summary>
        public byte[] Assemble(string source)
        {
            // Parse assembly source
            ParseSource(source);
            
            // First pass: collect labels
            CollectLabels();
            
            // Second pass: generate bytecode
            GenerateBytecode();
            
            return output.ToArray();
        }
        
        private void ParseSource(string source)
        {
            var lines = source.Split('\n');
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                
                // Skip empty lines and comments
                if (string.IsNullOrEmpty(line) || line.StartsWith(";"))
                    continue;
                
                // Remove inline comments
                int commentIndex = line.IndexOf(';');
                if (commentIndex >= 0)
                    line = line.Substring(0, commentIndex).Trim();
                
                // Parse instruction
                var instruction = ParseInstruction(line, i + 1);
                if (instruction != null)
                    instructions.Add(instruction);
            }
        }
        
        private AssemblyInstruction? ParseInstruction(string line, int lineNumber)
        {
            // Check for label
            if (line.EndsWith(":"))
            {
                return new AssemblyInstruction
                {
                    Type = InstructionType.Label,
                    Label = line.Substring(0, line.Length - 1),
                    LineNumber = lineNumber
                };
            }
            
            // Parse mnemonic and operands
            var parts = SplitInstruction(line);
            if (parts.Length == 0)
                return null;
            
            var mnemonic = parts[0].ToLower();
            var operands = parts.Skip(1).ToArray();
            
            // Check for directives
            if (mnemonic.StartsWith("."))
            {
                return ParseDirective(mnemonic, operands, lineNumber);
            }
            
            // Regular instruction
            if (!opcodeMap.ContainsKey(mnemonic))
            {
                throw new AssemblerException($"Unknown mnemonic: {mnemonic}", lineNumber);
            }
            
            return new AssemblyInstruction
            {
                Type = InstructionType.Opcode,
                Mnemonic = mnemonic,
                Opcode = opcodeMap[mnemonic],
                Operands = ParseOperands(operands, lineNumber),
                LineNumber = lineNumber
            };
        }
        
        private string[] SplitInstruction(string line)
        {
            var parts = new List<string>();
            var current = new StringBuilder();
            bool inString = false;
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"' && (i == 0 || line[i - 1] != '\\'))
                {
                    inString = !inString;
                    current.Append(c);
                }
                else if (!inString && (char.IsWhiteSpace(c) || c == ','))
                {
                    if (current.Length > 0)
                    {
                        parts.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            
            if (current.Length > 0)
                parts.Add(current.ToString());
            
            return parts.ToArray();
        }
        
        private AssemblyInstruction ParseDirective(string directive, string[] operands, int lineNumber)
        {
            switch (directive)
            {
                case ".byte":
                case ".db":
                    return new AssemblyInstruction
                    {
                        Type = InstructionType.Data,
                        DataType = DataType.Byte,
                        Data = ParseByteData(operands, lineNumber),
                        LineNumber = lineNumber
                    };
                
                case ".word":
                case ".dw":
                    return new AssemblyInstruction
                    {
                        Type = InstructionType.Data,
                        DataType = DataType.Word,
                        Data = ParseWordData(operands, lineNumber),
                        LineNumber = lineNumber
                    };
                
                case ".dword":
                case ".dd":
                    return new AssemblyInstruction
                    {
                        Type = InstructionType.Data,
                        DataType = DataType.DWord,
                        Data = ParseDWordData(operands, lineNumber),
                        LineNumber = lineNumber
                    };
                
                case ".string":
                case ".ascii":
                    return new AssemblyInstruction
                    {
                        Type = InstructionType.Data,
                        DataType = DataType.String,
                        Data = ParseStringData(string.Join(" ", operands), lineNumber),
                        LineNumber = lineNumber
                    };
                
                case ".align":
                    return new AssemblyInstruction
                    {
                        Type = InstructionType.Align,
                        AlignValue = (int)ParseNumber(operands[0], lineNumber),
                        LineNumber = lineNumber
                    };
                
                default:
                    throw new AssemblerException($"Unknown directive: {directive}", lineNumber);
            }
        }
        
        private List<Operand> ParseOperands(string[] operands, int lineNumber)
        {
            var result = new List<Operand>();
            
            foreach (var operand in operands)
            {
                result.Add(ParseOperand(operand, lineNumber));
            }
            
            return result;
        }
        
        private Operand ParseOperand(string operand, int lineNumber)
        {
            // Register
            if (IsRegister(operand))
            {
                return new Operand
                {
                    Type = OperandType.Register,
                    Register = ParseRegister(operand, lineNumber)
                };
            }
            
            // Memory reference [address]
            if (operand.StartsWith("[") && operand.EndsWith("]"))
            {
                var inner = operand.Substring(1, operand.Length - 2);
                return new Operand
                {
                    Type = OperandType.Memory,
                    MemoryOperand = ParseMemoryOperand(inner, lineNumber)
                };
            }
            
            // Immediate value
            if (IsNumber(operand))
            {
                return new Operand
                {
                    Type = OperandType.Immediate,
                    ImmediateValue = ParseNumber(operand, lineNumber)
                };
            }
            
            // Label reference
            return new Operand
            {
                Type = OperandType.Label,
                Label = operand
            };
        }
        
        private bool IsRegister(string operand)
        {
            // Check for register names
            var registers = new[] 
            { 
                // General purpose
                "eax", "ebx", "ecx", "edx", "esi", "edi", "esp", "ebp",
                "ax", "bx", "cx", "dx", "si", "di", "sp", "bp",
                "al", "ah", "bl", "bh", "cl", "ch", "dl", "dh",
                
                // 64-bit
                "rax", "rbx", "rcx", "rdx", "rsi", "rdi", "rsp", "rbp",
                "r8", "r9", "r10", "r11", "r12", "r13", "r14", "r15",
                
                // VM registers
                "r0", "r1", "r2", "r3", "r4", "r5", "r6", "r7",
                "pc", "sp", "fp", "acc"
            };
            
            return registers.Contains(operand.ToLower());
        }
        
        private Register ParseRegister(string name, int lineNumber)
        {
            var lower = name.ToLower();
            
            // Map register names to VM registers
            return lower switch
            {
                "eax" or "rax" or "ax" or "al" or "r0" => Register.R0,
                "ebx" or "rbx" or "bx" or "bl" or "r1" => Register.R1,
                "ecx" or "rcx" or "cx" or "cl" or "r2" => Register.R2,
                "edx" or "rdx" or "dx" or "dl" or "r3" => Register.R3,
                "esi" or "rsi" or "si" or "r4" => Register.R4,
                "edi" or "rdi" or "di" or "r5" => Register.R5,
                "esp" or "rsp" or "sp" => Register.SP,
                "ebp" or "rbp" or "fp" => Register.FP,
                "pc" => Register.PC,
                "acc" => Register.ACC,
                _ => throw new AssemblerException($"Unknown register: {name}", lineNumber)
            };
        }
        
        private MemoryOperand ParseMemoryOperand(string operand, int lineNumber)
        {
            // Simple forms: [reg], [reg+offset], [reg+reg*scale+offset]
            var mem = new MemoryOperand();
            
            // Remove spaces for easier parsing
            operand = operand.Replace(" ", "");
            
            // Parse comprehensive addressing modes
            // Support formats: [reg], [num], [reg+num], [reg-num], [reg+reg*scale], [reg+reg*scale+num]
            if (IsRegister(operand))
            {
                mem.BaseRegister = ParseRegister(operand, lineNumber);
            }
            else if (IsNumber(operand))
            {
                mem.Displacement = ParseNumber(operand, lineNumber);
            }
            else
            {
                // Enhanced parsing for complex addressing modes
                var parts = new List<string>();
                var current = new StringBuilder();
                bool inBrackets = false;
                
                for (int i = 0; i < operand.Length; i++)
                {
                    char c = operand[i];
                    
                    if (c == '[')
                    {
                        inBrackets = true;
                    }
                    else if (c == ']')
                    {
                        inBrackets = false;
                    }
                    else if ((c == '+' || c == '-') && !inBrackets)
                    {
                        if (current.Length > 0)
                        {
                            parts.Add(current.ToString());
                            current.Clear();
                        }
                        if (c == '-')
                        {
                            current.Append(c);
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                
                if (current.Length > 0)
                {
                    parts.Add(current.ToString());
                }
                
                // Parse each part
                foreach (var part in parts)
                {
                    if (part.Contains('*'))
                    {
                        // Index*Scale
                        var scaleParts = part.Split('*');
                        if (scaleParts.Length == 2)
                        {
                            mem.IndexRegister = ParseRegister(scaleParts[0], lineNumber);
                            mem.Scale = (int)ParseNumber(scaleParts[1], lineNumber);
                            
                            // Validate scale
                            if (mem.Scale != 1 && mem.Scale != 2 && mem.Scale != 4 && mem.Scale != 8)
                            {
                                throw new AssemblerException($"Invalid scale factor: {mem.Scale}. Must be 1, 2, 4, or 8", lineNumber);
                            }
                        }
                    }
                    else if (IsRegister(part))
                    {
                        if (mem.BaseRegister == null)
                        {
                            mem.BaseRegister = ParseRegister(part, lineNumber);
                        }
                        else if (mem.IndexRegister == null)
                        {
                            mem.IndexRegister = ParseRegister(part, lineNumber);
                            mem.Scale = 1;
                        }
                        else
                        {
                            throw new AssemblerException($"Too many registers in memory operand: [{operand}]", lineNumber);
                        }
                    }
                    else if (IsNumber(part))
                    {
                        mem.Displacement = ParseNumber(part, lineNumber);
                    }
                    else
                    {
                        // Might be a label
                        mem.Displacement = 0; // Will be resolved later
                    }
                }
            }
            
            return mem;
        }
        
        private bool IsNumber(string operand)
        {
            if (string.IsNullOrEmpty(operand))
                return false;
            
            // Hexadecimal
            if (operand.StartsWith("0x") || operand.StartsWith("0X"))
                return true;
            
            // Binary
            if (operand.StartsWith("0b") || operand.StartsWith("0B"))
                return true;
            
            // Decimal
            return operand.All(static c => char.IsDigit(c) || c == '-' || c == '+');
        }
        
        private long ParseNumber(string operand, int lineNumber)
        {
            try
            {
                // Hexadecimal
                if (operand.StartsWith("0x") || operand.StartsWith("0X"))
                {
                    return Convert.ToInt64(operand.Substring(2), 16);
                }
                
                // Binary
                if (operand.StartsWith("0b") || operand.StartsWith("0B"))
                {
                    return Convert.ToInt64(operand.Substring(2), 2);
                }
                
                // Decimal
                return long.Parse(operand);
            }
            catch (Exception ex)
            {
                throw new AssemblerException($"Invalid number format: {operand}", lineNumber, ex);
            }
        }
        
        private byte[] ParseByteData(string[] operands, int lineNumber)
        {
            var data = new List<byte>();
            
            foreach (var operand in operands)
            {
                data.Add((byte)ParseNumber(operand, lineNumber));
            }
            
            return data.ToArray();
        }
        
        private byte[] ParseWordData(string[] operands, int lineNumber)
        {
            var data = new List<byte>();
            
            foreach (var operand in operands)
            {
                ushort value = (ushort)ParseNumber(operand, lineNumber);
                data.Add((byte)(value & 0xFF));
                data.Add((byte)(value >> 8));
            }
            
            return data.ToArray();
        }
        
        private byte[] ParseDWordData(string[] operands, int lineNumber)
        {
            var data = new List<byte>();
            
            foreach (var operand in operands)
            {
                uint value = (uint)ParseNumber(operand, lineNumber);
                data.Add((byte)(value & 0xFF));
                data.Add((byte)((value >> 8) & 0xFF));
                data.Add((byte)((value >> 16) & 0xFF));
                data.Add((byte)(value >> 24));
            }
            
            return data.ToArray();
        }
        
        private byte[] ParseStringData(string operand, int lineNumber)
        {
            if (!operand.StartsWith("\"") || !operand.EndsWith("\""))
            {
                throw new AssemblerException("String must be enclosed in quotes", lineNumber);
            }
            
            var str = operand.Substring(1, operand.Length - 2);
            var data = new List<byte>();
            
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '\\' && i + 1 < str.Length)
                {
                    i++;
                    char escaped = str[i];
                    data.Add((byte)(escaped switch
                    {
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        '0' => '\0',
                        '\\' => '\\',
                        '"' => '"',
                        _ => escaped
                    }));
                }
                else
                {
                    data.Add((byte)str[i]);
                }
            }
            
            return data.ToArray();
        }
        
        private void CollectLabels()
        {
            currentAddress = 0;
            
            foreach (var instruction in instructions)
            {
                if (instruction.Type == InstructionType.Label)
                {
                    if (labelMap.ContainsKey(instruction.Label ?? string.Empty))
                    {
                        throw new AssemblerException($"Duplicate label: {instruction.Label}", instruction.LineNumber);
                    }
                    
                    labelMap[instruction.Label ?? string.Empty] = currentAddress;
                }
                else
                {
                    currentAddress += GetInstructionSize(instruction);
                }
            }
        }
        
        private void GenerateBytecode()
        {
            currentAddress = 0;
            output.Clear();
            
            foreach (var instruction in instructions)
            {
                switch (instruction.Type)
                {
                    case InstructionType.Label:
                        // Labels don't generate code
                        break;
                    
                    case InstructionType.Opcode:
                        GenerateOpcode(instruction);
                        break;
                    
                    case InstructionType.Data:
                        output.AddRange(instruction.Data ?? Array.Empty<byte>());
                        currentAddress += instruction.Data?.Length ?? 0;
                        break;
                    
                    case InstructionType.Align:
                        GenerateAlignment(instruction.AlignValue);
                        break;
                }
            }
        }
        
        private void GenerateOpcode(AssemblyInstruction instruction)
        {
            // Emit opcode as two bytes (little-endian)
            output.Add((byte)(instruction.Opcode & 0xFF));
            output.Add((byte)(instruction.Opcode >> 8));
            currentAddress += 2;
            
            foreach (var operand in instruction.Operands ?? System.Linq.Enumerable.Empty<Operand>())
            {
                GenerateOperand(operand, instruction);
            }
        }
        
        private void GenerateOperand(Operand operand, AssemblyInstruction instruction)
        {
            switch (operand.Type)
            {
                case OperandType.Register:
                    output.Add((byte)operand.Register);
                    break;
                
                case OperandType.Immediate:
                    GenerateImmediate(operand.ImmediateValue);
                    break;
                
                case OperandType.Label:
                    if (labelMap.TryGetValue(operand.Label ?? string.Empty, out int address))
                    {
                        // Calculate relative offset for jumps/calls
                        if (IsJumpInstruction(instruction.Mnemonic ?? string.Empty))
                        {
                            int offset = address - (currentAddress + GetInstructionSize(instruction));
                            GenerateImmediate(offset);
                        }
                        else
                        {
                            GenerateAddress(address);
                        }
                    }
                    else
                    {
                        throw new AssemblerException($"Undefined label: {operand.Label}", instruction.LineNumber);
                    }
                    break;
                
                case OperandType.Memory:
                    GenerateMemoryOperand(operand.MemoryOperand ?? new MemoryOperand());
                    break;
            }
        }
        
        private void GenerateImmediate(long value)
        {
            // Generate appropriately sized immediate based on value range
            if (value >= sbyte.MinValue && value <= sbyte.MaxValue)
            {
                // 8-bit immediate
                output.Add((byte)value);
                currentAddress += 1;
            }
            else if (value >= short.MinValue && value <= short.MaxValue)
            {
                // 16-bit immediate
                output.Add((byte)(value & 0xFF));
                output.Add((byte)((value >> 8) & 0xFF));
                currentAddress += 2;
            }
            else if (value >= int.MinValue && value <= int.MaxValue)
            {
                // 32-bit immediate
                output.Add((byte)(value & 0xFF));
                output.Add((byte)((value >> 8) & 0xFF));
                output.Add((byte)((value >> 16) & 0xFF));
                output.Add((byte)((value >> 24) & 0xFF));
                currentAddress += 4;
            }
            else
            {
                // 64-bit immediate
                output.Add((byte)(value & 0xFF));
                output.Add((byte)((value >> 8) & 0xFF));
                output.Add((byte)((value >> 16) & 0xFF));
                output.Add((byte)((value >> 24) & 0xFF));
                output.Add((byte)((value >> 32) & 0xFF));
                output.Add((byte)((value >> 40) & 0xFF));
                output.Add((byte)((value >> 48) & 0xFF));
                output.Add((byte)((value >> 56) & 0xFF));
                currentAddress += 8;
            }
        }
        
        private void GenerateAddress(int address)
        {
            // Generate relative address for jumps
            int relative = address - (currentAddress + 4);
            GenerateImmediate(relative);
        }
        
        private void GenerateMemoryOperand(MemoryOperand mem)
        {
            // Encode memory operand
            byte modRM = 0;
            
            if (mem.BaseRegister.HasValue)
            {
                modRM |= (byte)mem.BaseRegister.Value;
            }
            
            output.Add(modRM);
            currentAddress++;
            
            if (mem.Displacement != 0)
            {
                GenerateImmediate(mem.Displacement);
            }
        }
        
        private void GenerateAlignment(int alignment)
        {
            while (currentAddress % alignment != 0)
            {
                ushort nopOpcode = (ushort)Opcode.NOP;
                output.Add((byte)(nopOpcode & 0xFF));
                output.Add((byte)(nopOpcode >> 8));
                currentAddress += 2;
            }
        }
        
        private int GetInstructionSize(AssemblyInstruction instruction)
        {
            switch (instruction.Type)
            {
                case InstructionType.Label:
                    return 0;
                
                case InstructionType.Opcode:
                    int size = 2; // Opcode ushort (2 bytes)
                    if (instruction.Operands is not null)
                    {
                        foreach (var operand in instruction.Operands)
                        {
                            size += GetOperandSize(operand);
                        }
                    }
                    return size;
                
                case InstructionType.Data:
                    return instruction.Data?.Length ?? 0;
                
                case InstructionType.Align:
                    // Calculate padding needed
                    int padding = instruction.AlignValue - (currentAddress % instruction.AlignValue);
                    return padding == instruction.AlignValue ? 0 : padding;
                
                default:
                    return 0;
            }
        }
        
        private int GetOperandSize(Operand operand)
        {
            return operand.Type switch
            {
                OperandType.Register => 1,
                OperandType.Immediate => GetImmediateSize(operand.ImmediateValue),
                OperandType.Label => 4,     // 32-bit address
                OperandType.Memory => 1 + (operand.MemoryOperand?.Displacement != 0 ? GetImmediateSize(operand.MemoryOperand!.Displacement) : 0),
                _ => 0
            };
        }
        
        private int GetImmediateSize(long value)
        {
            if (value >= sbyte.MinValue && value <= sbyte.MaxValue)
                return 1;
            else if (value >= short.MinValue && value <= short.MaxValue)
                return 2;
            else if (value >= int.MinValue && value <= int.MaxValue)
                return 4;
            else
                return 8;
        }
        
        private bool IsJumpInstruction(string mnemonic)
        {
            var lower = mnemonic.ToLower();
            return lower.StartsWith("j") || lower == "call" || lower == "loop";
        }
    }
    
    /// <summary>
    /// Assembly instruction representation
    /// </summary>
    public class AssemblyInstruction
    {
        public InstructionType Type { get; set; }
        public string? Mnemonic { get; set; }
        public ushort Opcode { get; set; }
        public List<Operand>? Operands { get; set; }
        public string? Label { get; set; }
        public DataType DataType { get; set; }
        public byte[]? Data { get; set; }
        public int AlignValue { get; set; }
        public int LineNumber { get; set; }
    }
    
    public enum InstructionType
    {
        Label,
        Opcode,
        Data,
        Align
    }
    
    public enum DataType
    {
        Byte,
        Word,
        DWord,
        QWord,
        String
    }
    
    /// <summary>
    /// Operand representation
    /// </summary>
    public class Operand
    {
        public OperandType Type { get; set; }
        public Register Register { get; set; }
        public long ImmediateValue { get; set; }
        public string?   Label { get; set; }
        public MemoryOperand? MemoryOperand { get; set; }
    }
    
    public enum OperandType
    {
        Register,
        Immediate,
        Label,
        Memory
    }
    
    /// <summary>
    /// Memory operand representation
    /// </summary>
    public class MemoryOperand
    {
        public Register? BaseRegister { get; set; }
        public Register? IndexRegister { get; set; }
        public int Scale { get; set; } = 1;
        public long Displacement { get; set; }
    }
    
    /// <summary>
    /// VM registers
    /// </summary>
    public enum Register
    {
        R0 = 0,
        R1 = 1,
        R2 = 2,
        R3 = 3,
        R4 = 4,
        R5 = 5,
        R6 = 6,
        R7 = 7,
        SP = 8,   // Stack pointer
        FP = 9,   // Frame pointer
        PC = 10,  // Program counter
        ACC = 11  // Accumulator
    }
    
    /// <summary>
    /// Extended opcodes for assembly
    /// </summary>
    public enum ExtendedOpcode : byte
    {
        // Load variants
        LOAD_BYTE = 0x80,
        LOAD_WORD = 0x81,
        LOAD_DWORD = 0x82,
        LOAD_QWORD = 0x83,
        
        // Store variants
        STORE_BYTE = 0x84,
        STORE_WORD = 0x85,
        STORE_DWORD = 0x86,
        STORE_QWORD = 0x87,
        
        // Increment/Decrement
        INC = 0x88,
        DEC = 0x89,
        
        // Compare and test
        CMP = 0x8A,
        TEST = 0x8B,
        
        // Conditional jumps
        JE = 0x8C,   // Jump if equal
        JNE = 0x8D,  // Jump if not equal
        JL = 0x8E,   // Jump if less
        JG = 0x8F,   // Jump if greater
        JLE = 0x90,  // Jump if less or equal
        JGE = 0x91,  // Jump if greater or equal
        
        // Additional x86 jumps
        JA = 0x92,    // Jump if above
        JAE = 0x93,   // Jump if above or equal
        JB = 0x94,    // Jump if below
        JBE = 0x95,   // Jump if below or equal
        JO = 0x96,    // Jump if overflow
        JNO = 0x97,   // Jump if not overflow
        JS = 0x98,    // Jump if sign
        JNS = 0x99,   // Jump if not sign
        
        // Extended arithmetic
        IMUL = 0x9A,  // Signed multiply
        IDIV = 0x9B,  // Signed divide
        SAR = 0x9C,   // Shift arithmetic right
        ROL = 0x9D,   // Rotate left
        ROR = 0x9E,   // Rotate right
        
        // String operations
        MOVSB = 0xA0,
        MOVSW = 0xA1,
        MOVSD = 0xA2,
        REP = 0xA3,
        
        // Stack frame
        ENTER = 0xC8,
        LEAVE = 0xC9,
        
        // Other
        NOP = 0x90,
        HALT = 0xF4,
        INT = 0xCD,
        SYSCALL = 0x05
    }
    
    /// <summary>
    /// Assembler exception
    /// </summary>
    public class AssemblerException : Exception
    {
        public int LineNumber { get; }
        
        public AssemblerException(string message, int lineNumber) 
            : base($"Line {lineNumber}: {message}")
        {
            LineNumber = lineNumber;
        }
        
        public AssemblerException(string message, int lineNumber, Exception innerException) 
            : base($"Line {lineNumber}: {message}", innerException)
        {
            LineNumber = lineNumber;
        }
    }
} 

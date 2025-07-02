using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ouro.Core.AST;
using Ouro.Core.VM;
using Ouro.Core.Compiler;
using Ouro.Tokens;

namespace Ouro.CodeGen
{
    /// <summary>
    /// Direct native code generator for x86-64
    /// </summary>
    public class NativeCodeGen
    {
        private readonly List<byte> code;
        private readonly Dictionary<string, int> functionOffsets;
        private readonly Dictionary<string, List<int>> functionCalls;

        public NativeCodeGen()
        {
            code = new List<byte>();
            functionOffsets = new Dictionary<string, int>();
            functionCalls = new Dictionary<string, List<int>>();
        }

        public void GenerateCode(Core.AST.Program program, string outputPath)
        {
            // Generate PE/ELF header based on platform
            if (OperatingSystem.IsWindows())
            {
                GeneratePEHeader();
            }
            else
            {
                GenerateELFHeader();
            }

            // Generate code for all functions
            foreach (var statement in program.Statements)
            {
                if (statement is FunctionDeclaration funcDecl)
                {
                    GenerateFunction(funcDecl);
                }
            }

            // Generate main entry point
            GenerateMainEntryPoint();

            // Resolve function calls
            ResolveFunctionCalls();

            // Write output file
            File.WriteAllBytes(outputPath, code.ToArray());
            
            // Make executable on Unix
            if (!OperatingSystem.IsWindows())
            {
                MakeExecutable(outputPath);
            }
        }

        private void GeneratePEHeader()
        {
            // DOS Header
            code.AddRange(new byte[] {
                0x4D, 0x5A, // MZ signature
                0x90, 0x00, // Bytes on last page
                0x03, 0x00, // Pages in file
                0x00, 0x00, // Relocations
                0x04, 0x00, // Size of header in paragraphs
                0x00, 0x00, // Minimum extra paragraphs
                0xFF, 0xFF, // Maximum extra paragraphs
                0x00, 0x00, // Initial SS
                0xB8, 0x00, // Initial SP
                0x00, 0x00, // Checksum
                0x00, 0x00, // Initial IP
                0x00, 0x00, // Initial CS
                0x40, 0x00, // File address of relocation table
                0x00, 0x00, // Overlay number
            });

            // Reserved
            code.AddRange(new byte[32]);

            // PE offset
            code.AddRange(BitConverter.GetBytes(0x80)); // PE header at 0x80

            // DOS stub
            code.AddRange(new byte[] {
                0x0E, 0x1F, 0xBA, 0x0E, 0x00, 0xB4, 0x09, 0xCD,
                0x21, 0xB8, 0x01, 0x4C, 0xCD, 0x21, 0x54, 0x68,
                0x69, 0x73, 0x20, 0x70, 0x72, 0x6F, 0x67, 0x72,
                0x61, 0x6D, 0x20, 0x63, 0x61, 0x6E, 0x6E, 0x6F,
                0x74, 0x20, 0x62, 0x65, 0x20, 0x72, 0x75, 0x6E,
                0x20, 0x69, 0x6E, 0x20, 0x44, 0x4F, 0x53, 0x20,
                0x6D, 0x6F, 0x64, 0x65, 0x2E, 0x0D, 0x0D, 0x0A,
                0x24, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            });

            // Align to PE header
            while (code.Count < 0x80) code.Add(0);

            // PE signature
            code.AddRange(new byte[] { 0x50, 0x45, 0x00, 0x00 });

            // COFF header
            code.AddRange(new byte[] {
                0x64, 0x86, // Machine (x64)
                0x01, 0x00, // Number of sections
                0x00, 0x00, 0x00, 0x00, // TimeDateStamp
                0x00, 0x00, 0x00, 0x00, // PointerToSymbolTable
                0x00, 0x00, 0x00, 0x00, // NumberOfSymbols
                0xF0, 0x00, // SizeOfOptionalHeader
                0x22, 0x00  // Characteristics
            });

            // Optional header (simplified)
            code.AddRange(new byte[] {
                0x0B, 0x02, // Magic (PE32+)
                0x01, 0x00, // MajorLinkerVersion
                0x00, 0x00, // MinorLinkerVersion
                0x00, 0x10, 0x00, 0x00, // SizeOfCode
                0x00, 0x00, 0x00, 0x00, // SizeOfInitializedData
                0x00, 0x00, 0x00, 0x00, // SizeOfUninitializedData
                0x00, 0x10, 0x00, 0x00, // AddressOfEntryPoint
                0x00, 0x10, 0x00, 0x00, // BaseOfCode
            });

            // Continue with simplified PE header...
            // This is a minimal implementation for demonstration
        }

        private void GenerateELFHeader()
        {
            // ELF header for x86-64
            code.AddRange(new byte[] {
                0x7F, 0x45, 0x4C, 0x46, // Magic number
                0x02, // 64-bit
                0x01, // Little endian
                0x01, // Current version
                0x00, // System V ABI
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Padding
                0x02, 0x00, // Executable file
                0x3E, 0x00, // x86-64
                0x01, 0x00, 0x00, 0x00, // Version 1
                0x80, 0x00, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, // Entry point
                0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Program header offset
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Section header offset
                0x00, 0x00, 0x00, 0x00, // Flags
                0x40, 0x00, // ELF header size
                0x38, 0x00, // Program header size
                0x01, 0x00, // Number of program headers
                0x40, 0x00, // Section header size
                0x00, 0x00, // Number of section headers
                0x00, 0x00  // Section header string table index
            });

            // Program header
            code.AddRange(new byte[] {
                0x01, 0x00, 0x00, 0x00, // PT_LOAD
                0x05, 0x00, 0x00, 0x00, // PF_X | PF_R (executable, readable)
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Offset
                0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, // Virtual address
                0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, // Physical address
                0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // File size
                0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Memory size
                0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00  // Alignment
            });

            // Align to code section
            while (code.Count < 0x80) code.Add(0);
        }

        private void GenerateFunction(FunctionDeclaration funcDecl)
        {
            functionOffsets[funcDecl.Name] = code.Count;

            // Function prologue
            code.AddRange(new byte[] {
                0x55,                   // push rbp
                0x48, 0x89, 0xE5,       // mov rbp, rsp
                0x48, 0x83, 0xEC, 0x20  // sub rsp, 32 (stack space)
            });

            // Generate function body (simplified)
            // In real implementation, would translate AST to machine code
            if (funcDecl.Body != null)
            {
                // For now, just generate a return
                GenerateStatement(funcDecl.Body);
            }

            // Function epilogue
            code.AddRange(new byte[] {
                0x48, 0x83, 0xC4, 0x20, // add rsp, 32
                0x5D,                   // pop rbp
                0xC3                    // ret
            });
        }

        private void GenerateStatement(Statement stmt)
        {
            switch (stmt)
            {
                case BlockStatement block:
                    foreach (var s in block.Statements)
                    {
                        GenerateStatement(s);
                    }
                    break;

                case ReturnStatement ret:
                    if (ret.Value != null)
                    {
                        GenerateExpression(ret.Value);
                        // Result is in RAX
                    }
                    break;

                case ExpressionStatement expr:
                    GenerateExpression(expr.Expression);
                    break;

                // Add more statement types as needed
            }
        }

        private void GenerateExpression(Expression expr)
        {
            switch (expr)
            {
                case LiteralExpression lit:
                    if (lit.Value is int intVal)
                    {
                        // mov eax, immediate
                        code.Add(0xB8);
                        code.AddRange(BitConverter.GetBytes(intVal));
                    }
                    break;

                case BinaryExpression bin:
                    GenerateExpression(bin.Left);
                    // Push RAX
                    code.Add(0x50);
                    GenerateExpression(bin.Right);
                    // Pop RCX
                    code.Add(0x59);
                    
                    switch (bin.Operator.Type)
                    {
                        case TokenType.Plus:
                            // add eax, ecx
                            code.AddRange(new byte[] { 0x01, 0xC8 });
                            break;
                        case TokenType.Minus:
                            // sub ecx, eax; mov eax, ecx
                            code.AddRange(new byte[] { 0x29, 0xC1, 0x89, 0xC8 });
                            break;
                        // Add more operators
                    }
                    break;

                // Add more expression types
            }
        }

        private void GenerateMainEntryPoint()
        {
            functionOffsets["_start"] = code.Count;

            // Entry point that calls Main function
            if (functionOffsets.ContainsKey("Main"))
            {
                // call Main
                code.Add(0xE8);
                int offset = functionOffsets["Main"] - (code.Count + 4);
                code.AddRange(BitConverter.GetBytes(offset));
            }

            // Exit syscall
            if (OperatingSystem.IsWindows())
            {
                // Windows: ExitProcess
                code.AddRange(new byte[] {
                    0x48, 0x89, 0xC1,       // mov rcx, rax (exit code)
                    0x48, 0x83, 0xEC, 0x28, // sub rsp, 40
                    0xFF, 0x15, 0x00, 0x00, 0x00, 0x00 // call [ExitProcess]
                });
            }
            else
            {
                // Linux: exit syscall
                code.AddRange(new byte[] {
                    0x48, 0x89, 0xC7,       // mov rdi, rax (exit code)
                    0x48, 0xC7, 0xC0, 0x3C, 0x00, 0x00, 0x00, // mov rax, 60 (exit)
                    0x0F, 0x05              // syscall
                });
            }
        }

        private void ResolveFunctionCalls()
        {
            // Resolve function call offsets
            foreach (var kvp in functionCalls)
            {
                var functionName = kvp.Key;
                var callSites = kvp.Value;

                if (functionOffsets.TryGetValue(functionName, out var targetOffset))
                {
                    foreach (var callSite in callSites)
                    {
                        int offset = targetOffset - (callSite + 4);
                        var offsetBytes = BitConverter.GetBytes(offset);
                        for (int i = 0; i < 4; i++)
                        {
                            code[callSite + i] = offsetBytes[i];
                        }
                    }
                }
            }
        }

        private void MakeExecutable(string path)
        {
            // Use chmod to make file executable on Unix
            var chmod = System.Diagnostics.Process.Start("chmod", $"+x {path}");
            chmod?.WaitForExit();
        }
    }
} 
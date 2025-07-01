using System;
using System.Collections.Generic;
using Ouroboros.Core.VM;
using Ouroboros.Core.AST;

namespace Ouroboros.Core.Compiler
{
    // Symbol table for managing identifiers
    public class SymbolTable
    {
        public bool IsGlobal { get; set; }
        private Dictionary<string, Symbol> symbols = new Dictionary<string, Symbol>();
        private Stack<Dictionary<string, Symbol>> scopeStack = new Stack<Dictionary<string, Symbol>>();
        private int globalCount = 0;
        
        public void Define(string name, Symbol symbol)
        {
            if (scopeStack.Count > 0)
                scopeStack.Peek()[name] = symbol;
            else
                symbols[name] = symbol;
        }
        
        public Symbol Lookup(string name)
        {
            // Check global scope FIRST (for built-ins and global variables)
            if (symbols.ContainsKey(name))
                return symbols[name];
            
            // Check current scope 
            if (scopeStack.Count > 0 && scopeStack.Peek().ContainsKey(name))
                return scopeStack.Peek()[name];
                
            // Check each scope in order (from innermost to outermost)
            foreach (var scope in scopeStack)
            {
                if (scope.ContainsKey(name))
                    return scope[name];
            }
            
            // Symbol not found
            return null;
        }
        
        public void EnterScope()
        {
            scopeStack.Push(new Dictionary<string, Symbol>());
        }
        
        public void ExitScope()
        {
            if (scopeStack.Count > 0)
                scopeStack.Pop();
        }
        
        public void DefineAlias(string alias, string target)
        {
            Define(alias, new Symbol { Name = alias, Type = "alias", Value = target });
        }
        
        public void DefineTypeAlias(string alias, object typeInfo)
        {
            Define(alias, new Symbol { Name = alias, Type = "type_alias", Value = typeInfo });
        }
        
        // Helper method to define a variable with type information
        public Symbol Define(string name, object type)
        {
            var symbol = new Symbol 
            { 
                Name = name, 
                Type = type,
                IsGlobal = scopeStack.Count == 0,
                Index = GetNextIndex()
            };
            Define(name, symbol);
            return symbol;
        }
        
        private int GetNextIndex()
        {
            if (scopeStack.Count == 0)  // Global scope
            {
                return globalCount++;  // Return current value and increment
            }
            else
            {
                // Function scope: local variables start at index 0 within the function
                // Parameters should be indexed 0, 1, 2, etc. within the function scope
                return scopeStack.Peek().Count;
            }
        }
        
        public int GlobalCount()
        {
            return globalCount;
        }
        
        // Debug method to list all symbols
        public List<string> GetAllSymbolNames()
        {
            var allSymbols = new List<string>();
            
            // Add symbols from current scopes
            foreach (var scope in scopeStack)
            {
                allSymbols.AddRange(scope.Keys);
            }
            
            // Add global symbols
            allSymbols.AddRange(symbols.Keys);
            
            return allSymbols;
        }

        /// <summary>
        /// Get the index of a global variable by name
        /// </summary>
        public int GetGlobalIndex(string name)
        {
            if (symbols.TryGetValue(name, out var symbol))
            {
                return symbol.Index;
            }
            return -1;
        }

        /// <summary>
        /// Get names of all local variables in the current scope
        /// </summary>
        public List<string> GetLocalNames()
        {
            var localNames = new List<string>();
            
            // Get local variables from all scope levels
            foreach (var scope in scopeStack)
            {
                localNames.AddRange(scope.Keys);
            }
            
            return localNames;
        }
    }
    
    public class Symbol
    {
        public bool IsGlobal { get; set; }
        public string Name { get; set; } = "";
        public object Type { get; set; } = "";
        public object? Value { get; set; }
        public int Index { get; set; }  // Added for local variable indexing
        public int Address { get; set; } = -1;  // Added for function addresses
    }
    
    // Type information classes
    public class ClassInfo
    {
        public string Name { get; set; }
        public string BaseClass { get; set; }
        public List<string> Interfaces { get; set; } = new List<string>();
        public List<FieldInfo> Fields { get; set; } = new List<FieldInfo>();
        public List<MethodInfo> Methods { get; set; } = new List<MethodInfo>();
        public List<PropertyInfo> Properties { get; set; } = new List<PropertyInfo>();
        public Dictionary<string, List<string>> TypeParameters { get; set; } = new Dictionary<string, List<string>>();
    }
    
    public class FieldInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public List<string> Modifiers { get; set; } = new List<string>();
    }
    
    public class MethodInfo
    {
        public string Name { get; set; }
        public int StartAddress { get; set; }
        public int EndAddress { get; set; }
        public List<string> Modifiers { get; set; } = new List<string>();
    }
    
    public class PropertyInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int GetterAddress { get; set; }
        public int GetterEndAddress { get; set; }
        public int SetterAddress { get; set; }
        public int SetterEndAddress { get; set; }
        public List<string> Modifiers { get; set; } = new List<string>();
    }
    
    public class InterfaceInfo
    {
        public string Name { get; set; }
        public List<string> BaseInterfaces { get; set; } = new List<string>();
        public List<InterfaceMethodInfo> Methods { get; set; } = new List<InterfaceMethodInfo>();
        public List<InterfacePropertyInfo> Properties { get; set; } = new List<InterfacePropertyInfo>();
    }
    
    public class InterfaceMethodInfo
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public List<ParameterInfo> Parameters { get; set; } = new List<ParameterInfo>();
    }
    
    public class InterfacePropertyInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool HasGetter { get; set; }
        public bool HasSetter { get; set; }
    }
    
    public class ParameterInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }
    
    public class StructInfo
    {
        public string Name { get; set; }
        public List<string> Interfaces { get; set; } = new List<string>();
        public List<FieldInfo> Fields { get; set; } = new List<FieldInfo>();
        public List<MethodInfo> Methods { get; set; } = new List<MethodInfo>();
    }
    
    public class EnumInfo
    {
        public string Name { get; set; }
        public string UnderlyingType { get; set; }
        public List<EnumMemberInfo> Members { get; set; } = new List<EnumMemberInfo>();
    }
    
    public class EnumMemberInfo
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }
    
    public class ComponentInfo
    {
        public string Name { get; set; }
        public List<ComponentFieldInfo> Fields { get; set; } = new List<ComponentFieldInfo>();
    }
    
    public class ComponentFieldInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }
    
    public class SystemInfo
    {
        public string Name { get; set; }
        public List<string> RequiredComponents { get; set; } = new List<string>();
    }
    
    public class EntityInfo
    {
        public string Name { get; set; }
        public List<string> Components { get; set; } = new List<string>();
    }
    
    public class FunctionInfo
    {
        public string Name { get; set; }
        public List<Parameter> Parameters { get; set; } = new List<Parameter>();
        public TypeNode ReturnType { get; set; }
        public int StartAddress { get; set; }  // Added for VM execution
        public int EndAddress { get; set; }     // Added for VM execution
    }
    
    public class ExceptionHandler
    {
        public int HandlerStart { get; set; }
        public int TryStart { get; set; }
        public int TryEnd { get; set; }
        public int CatchStart { get; set; }
        public string ExceptionType { get; set; }
    }
    
    public class Bytecode
    {
        public List<byte> Code { get; set; } = new List<byte>();
        public List<object> Constants { get; set; } = new List<object>();
        public List<ClassInfo> Classes { get; set; } = new List<ClassInfo>();
        public List<StructInfo> Structs { get; set; } = new List<StructInfo>();
        public List<EnumInfo> Enums { get; set; } = new List<EnumInfo>();
    }
    
    public class CompiledProgram
    {
        public Bytecode Bytecode { get; set; }
        public Dictionary<string, FunctionInfo> Functions { get; set; } = new Dictionary<string, FunctionInfo>();
        public SymbolTable SymbolTable { get; set; }
        public string SourceFile { get; set; }
        public CompilerMetadata Metadata { get; set; }
    }
    
    public class LoopInfo
    {
        public int StartLabel { get; set; }
        public int ContinueLabel { get; set; }
        public int BreakLabel { get; set; }
    }
    
    public class SwitchInfo
    {
        public List<int> BreakJumps { get; set; } = new List<int>();
    }
    
    public class CompilerContext
    {
        public SymbolTable Symbols { get; set; } = new SymbolTable();
        public Stack<string> ScopeStack { get; set; } = new Stack<string>();
        public Stack<LoopInfo> LoopStack { get; set; } = new Stack<LoopInfo>();
        public Stack<SwitchInfo> SwitchStack { get; set; } = new Stack<SwitchInfo>();
        public string CurrentClass { get; set; }
        public string CurrentNamespace { get; set; }
        public string SourceFile { get; set; }
        
        public void PushLoop(LoopInfo loop)
        {
            LoopStack.Push(loop);
        }
        
        public void PopLoop(BytecodeBuilder builder)
        {
            if (LoopStack.Count > 0)
                LoopStack.Pop();
        }
        
        public void PushSwitch(SwitchInfo switchInfo)
        {
            SwitchStack.Push(switchInfo);
        }
        
        public void PopSwitch()
        {
            if (SwitchStack.Count > 0)
                SwitchStack.Pop();
        }
        
        public void EmitBreak(BytecodeBuilder builder)
        {
            // Check if we're in a switch context first
            if (SwitchStack.Count > 0)
            {
                var switchInfo = SwitchStack.Peek();
                switchInfo.BreakJumps.Add(builder.EmitJump(VM.Opcode.Jump));
                return;
            }
            
            // Otherwise check for loop context
            if (LoopStack.Count == 0)
            {
                // Log error but don't throw - emit a no-op to continue compilation
                Console.WriteLine("WARNING: Break statement outside of loop or switch - ignoring");
                builder.Emit(VM.Opcode.Nop);
                return;
            }
                
            var loop = LoopStack.Peek();
            builder.Emit(VM.Opcode.Jump, loop.BreakLabel);
        }
        
        public void EmitContinue(BytecodeBuilder builder)
        {
            if (LoopStack.Count == 0)
                throw new InvalidOperationException("Continue statement outside of loop");
                
            var loop = LoopStack.Peek();
            builder.Emit(VM.Opcode.Jump, loop.ContinueLabel);
        }
        
        public void EnterClass(string className)
        {
            CurrentClass = className;
        }
        
        public void ExitClass()
        {
            CurrentClass = null;
        }
        
        public void EnterStruct(string structName)
        {
            CurrentClass = structName;  // Structs are similar to classes for compilation
        }
        
        public void ExitStruct()
        {
            CurrentClass = null;
        }
        
        public void EnterNamespace(string namespaceName)
        {
            CurrentNamespace = namespaceName;
        }
        
        public void ExitNamespace()
        {
            CurrentNamespace = null;
        }
    }
    
    public class ProgramMetadata
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public DateTime CompileTime { get; set; }
        public string CompilerVersion { get; set; }
        public int OptimizationLevel { get; set; }
        public List<string> SourceFiles { get; set; } = new List<string>();
        public string TargetPlatform { get; set; }
    }

public class CompilerMetadata
{
    public string Version { get; set; }
    public DateTime CompileTime { get; set; }
    public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
}
}

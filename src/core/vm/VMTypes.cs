using System;
using System.Collections.Generic;
using Ouro.Core.AST;

namespace Ouro.Core.VM
{
    /// <summary>
    /// VM instruction representation
    /// </summary>
    public struct Instruction
    {
        public Opcode Opcode { get; set; }
        public object Operand { get; set; }
        
        public Instruction(Opcode opcode, object operand = null)
        {
            Opcode = opcode;
            Operand = operand;
        }
    }
    
    /// <summary>
    /// Compiled bytecode representation
    /// </summary>
    public class Bytecode
    {
        public byte[] Instructions { get; set; }
        public object[] ConstantPool { get; set; }
        public FunctionInfo[] Functions { get; set; }
        public ClassInfo[] Classes { get; set; }
        public InterfaceInfo[] Interfaces { get; set; }
        public StructInfo[] Structs { get; set; }
        public EnumInfo[] Enums { get; set; }
        public ComponentInfo[] Components { get; set; }
        public SystemInfo[] Systems { get; set; }
        public EntityInfo[] Entities { get; set; }
        public ExceptionHandler[] ExceptionHandlers { get; set; }
        
        // Legacy properties for compatibility
        public List<byte> Code 
        { 
            get => Instructions?.ToList() ?? new List<byte>();
            set => Instructions = value?.ToArray() ?? Array.Empty<byte>();
        }
        
        public List<object> Constants 
        { 
            get => ConstantPool?.ToList() ?? new List<object>();
            set => ConstantPool = value?.ToArray() ?? Array.Empty<object>();
        }
    }
    
    /// <summary>
    /// Compiled program with metadata
    /// </summary>
    public class CompiledProgram
    {
        public Bytecode Bytecode { get; set; }
        public SymbolTable SymbolTable { get; set; }
        public string SourceFile { get; set; }
        public ProgramMetadata Metadata { get; set; }
    }
    
    /// <summary>
    /// Program metadata
    /// </summary>
    public class ProgramMetadata
    {
        public string Version { get; set; }
        public string CompilerVersion { get; set; }
        public int OptimizationLevel { get; set; } // 0=None, 1=Basic, 2=Full
        public string[] SourceFiles { get; set; }
        public DateTime CompileTime { get; set; }
        public string TargetPlatform { get; set; }
    }
    
    /// <summary>
    /// Function information
    /// </summary>
    public class FunctionInfo
    {
        public string Name { get; set; }
        public int StartAddress { get; set; }
        public int EndAddress { get; set; }
        public int LocalCount { get; set; }
        public int ParameterCount { get; set; }
        public bool IsAsync { get; set; }
        public bool IsGenerator { get; set; }
    }
    
    /// <summary>
    /// Class information
    /// </summary>
    public class ClassInfo
    {
        public string Name { get; set; }
        public string BaseClass { get; set; }
        public List<string> Interfaces { get; set; }
        public List<FieldInfo> Fields { get; set; }
        public List<MethodInfo> Methods { get; set; }
        public List<PropertyInfo> Properties { get; set; }
        public int ConstructorAddress { get; set; }
        public int VTableOffset { get; set; }
    }
    
    /// <summary>
    /// Field information
    /// </summary>
    public class FieldInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public List<string> Modifiers { get; set; }
        public int Offset { get; set; }
        public object DefaultValue { get; set; }
    }
    
    /// <summary>
    /// Method information
    /// </summary>
    public class MethodInfo
    {
        public string Name { get; set; }
        public int StartAddress { get; set; }
        public int EndAddress { get; set; }
        public List<string> Modifiers { get; set; }
        public int VTableIndex { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsAbstract { get; set; }
    }
    
    /// <summary>
    /// Property information
    /// </summary>
    public class PropertyInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int GetterAddress { get; set; }
        public int GetterEndAddress { get; set; }
        public int SetterAddress { get; set; }
        public int SetterEndAddress { get; set; }
    }
    
    /// <summary>
    /// Interface information
    /// </summary>
    public class InterfaceInfo
    {
        public string Name { get; set; }
        public List<string> BaseInterfaces { get; set; }
        public List<InterfaceMethodInfo> Methods { get; set; }
        public List<InterfacePropertyInfo> Properties { get; set; }
    }
    
    /// <summary>
    /// Interface method information
    /// </summary>
    public class InterfaceMethodInfo
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public List<ParameterInfo> Parameters { get; set; }
        public int VTableIndex { get; set; }
    }
    
    /// <summary>
    /// Interface property information
    /// </summary>
    public class InterfacePropertyInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool HasGetter { get; set; }
        public bool HasSetter { get; set; }
    }
    
    /// <summary>
    /// Parameter information
    /// </summary>
    public class ParameterInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsOptional { get; set; }
        public object DefaultValue { get; set; }
        public bool IsParams { get; set; }
        public bool IsRef { get; set; }
        public bool IsOut { get; set; }
    }
    
    /// <summary>
    /// Struct information
    /// </summary>
    public class StructInfo
    {
        public string Name { get; set; }
        public List<string> Interfaces { get; set; }
        public List<FieldInfo> Fields { get; set; }
        public List<MethodInfo> Methods { get; set; }
        public int Size { get; set; }
        public int Alignment { get; set; }
    }
    
    /// <summary>
    /// Enum information
    /// </summary>
    public class EnumInfo
    {
        public string Name { get; set; }
        public string UnderlyingType { get; set; }
        public List<EnumMemberInfo> Members { get; set; }
    }
    
    /// <summary>
    /// Enum member information
    /// </summary>
    public class EnumMemberInfo
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }
    
    /// <summary>
    /// Component information for ECS
    /// </summary>
    public class ComponentInfo
    {
        public string Name { get; set; }
        public List<ComponentFieldInfo> Fields { get; set; }
        public int Size { get; set; }
        public int Alignment { get; set; }
    }
    
    /// <summary>
    /// Component field information
    /// </summary>
    public class ComponentFieldInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int Offset { get; set; }
    }
    
    /// <summary>
    /// System information for ECS
    /// </summary>
    public class SystemInfo
    {
        public string Name { get; set; }
        public List<string> RequiredComponents { get; set; }
        public int UpdateMethodAddress { get; set; }
        public int Priority { get; set; }
    }
    
    /// <summary>
    /// Entity archetype information
    /// </summary>
    public class EntityInfo
    {
        public string Name { get; set; }
        public List<string> Components { get; set; }
    }
    
    /// <summary>
    /// Exception handler information
    /// </summary>
    public class ExceptionHandler
    {
        public int TryStart { get; set; }
        public int TryEnd { get; set; }
        public int HandlerStart { get; set; }
        public int CatchStart { get; set; }
        public string ExceptionType { get; set; }
        public int FilterStart { get; set; }
    }
    
    /// <summary>
    /// Symbol table for variables and functions
    /// </summary>
    public class SymbolTable
    {
        private class Scope
        {
            public Dictionary<string, Symbol> Symbols { get; } = new Dictionary<string, Symbol>();
            public Scope Parent { get; set; }
        }
        
        private Scope currentScope;
        private int nextLocalIndex = 0;
        private int nextGlobalIndex = 0;
        private Dictionary<string, TypeAlias> typeAliases = new Dictionary<string, TypeAlias>();
        private Dictionary<string, string> moduleAliases = new Dictionary<string, string>();
        
        public SymbolTable()
        {
            currentScope = new Scope();
        }
        
        public void EnterScope()
        {
            var newScope = new Scope { Parent = currentScope };
            currentScope = newScope;
        }
        
        public void ExitScope()
        {
            if (currentScope.Parent != null)
            {
                currentScope = currentScope.Parent;
            }
        }
        
        public Symbol Define(string name, TypeNode type)
        {
            if (currentScope.Symbols.ContainsKey(name))
            {
                throw new InvalidOperationException($"Symbol '{name}' already defined in current scope");
            }
            
            var symbol = new Symbol
            {
                Name = name,
                Type = type?.Name,
                IsGlobal = currentScope.Parent == null,
                Index = currentScope.Parent == null ? nextGlobalIndex++ : nextLocalIndex++
            };
            
            currentScope.Symbols[name] = symbol;
            return symbol;
        }
        
        public Symbol Lookup(string name)
        {
            var scope = currentScope;
            while (scope != null)
            {
                if (scope.Symbols.TryGetValue(name, out var symbol))
                {
                    return symbol;
                }
                scope = scope.Parent;
            }
            return null;
        }
        
        public void DefineFunction(string name, TypeNode returnType, List<AST.Parameter> parameters)
        {
            var funcSymbol = new FunctionSymbol
            {
                Name = name,
                ReturnType = returnType?.Name,
                Parameters = parameters.Select(p => new ParameterInfo 
                { 
                    Name = p.Name, 
                    Type = p.Type?.Name,
                    IsOptional = p.DefaultValue != null,
                    DefaultValue = p.DefaultValue
                }).ToList(),
                IsGlobal = true,
                Index = nextGlobalIndex++
            };
            
            currentScope.Symbols[name] = funcSymbol;
        }
        
        public void DefineTypeAlias(string name, TypeNode aliasedType)
        {
            typeAliases[name] = new TypeAlias
            {
                Name = name,
                AliasedType = aliasedType?.Name
            };
        }
        
        public void DefineAlias(string alias, string modulePath)
        {
            moduleAliases[alias] = modulePath;
        }
        
        public TypeAlias GetTypeAlias(string name)
        {
            return typeAliases.TryGetValue(name, out var alias) ? alias : null;
        }
        
        public string GetModuleAlias(string alias)
        {
            return moduleAliases.TryGetValue(alias, out var path) ? path : null;
        }
    }
    
    /// <summary>
    /// Symbol information
    /// </summary>
    public class Symbol
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsGlobal { get; set; }
        public int Index { get; set; }
        public bool IsConst { get; set; }
        public bool IsMutable { get; set; }
    }
    
    /// <summary>
    /// Function symbol information
    /// </summary>
    public class FunctionSymbol : Symbol
    {
        public string ReturnType { get; set; }
        public List<ParameterInfo> Parameters { get; set; }
        public bool IsAsync { get; set; }
        public bool IsGenerator { get; set; }
    }
    
    /// <summary>
    /// Type alias information
    /// </summary>
    public class TypeAlias
    {
        public string Name { get; set; }
        public string AliasedType { get; set; }
    }
    
    /// <summary>
    /// Pattern for pattern matching
    /// </summary>
    public abstract class Pattern
    {
        public abstract bool Match(object value);
    }
    
    /// <summary>
    /// Constant pattern
    /// </summary>
    public class ConstantPattern : Pattern
    {
        public Expression Value { get; set; }
        
        public override bool Match(object value)
        {
            // For now, we need to evaluate the expression and compare
            // In a full implementation, this would use the expression evaluator
            if (Value is LiteralExpression lit)
            {
                return Equals(lit.Value, value);
            }
            
            // For other expression types, we'd need an expression evaluator
            // For now, return false for non-literal expressions
            return false;
        }
    }
    
    /// <summary>
    /// Type pattern
    /// </summary>
    public class TypePattern : Pattern
    {
        public TypeNode Type { get; set; }
        public string VariableName { get; set; }
        
        public override bool Match(object value)
        {
            if (value == null)
                return Type == null || Type.Name == "null";
                
            // Get the runtime type of the value
            var valueType = value.GetType();
            
            // Simple type name matching
            // In a full implementation, this would handle generics, inheritance, etc.
            return valueType.Name == Type.Name || 
                   valueType.FullName == Type.Name ||
                   IsAssignableToType(valueType, Type.Name);
        }
        
        private bool IsAssignableToType(Type valueType, string typeName)
        {
            // Check if value type matches or inherits from the pattern type
            if (valueType.Name == typeName || valueType.FullName == typeName)
                return true;
                
            // Check base types
            var baseType = valueType.BaseType;
            while (baseType != null)
            {
                if (baseType.Name == typeName || baseType.FullName == typeName)
                    return true;
                baseType = baseType.BaseType;
            }
            
            // Check interfaces
            foreach (var iface in valueType.GetInterfaces())
            {
                if (iface.Name == typeName || iface.FullName == typeName)
                    return true;
            }
            
            return false;
        }
    }
    
    /// <summary>
    /// Loop context for break/continue
    /// </summary>
    public class LoopContext
    {
        public int LoopStart { get; set; }
        public int? ContinuePoint { get; set; }
        public List<int> BreakJumps { get; } = new List<int>();
    }
}
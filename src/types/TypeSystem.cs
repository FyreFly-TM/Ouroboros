using System;
using System.Collections.Generic;
using System.Linq;
using Ouroboros.Core;

namespace Ouroboros.Types
{
    /// <summary>
    /// Ouroboros type system
    /// </summary>
    public class TypeSystem
    {
        private readonly Dictionary<string, Type> types;
        private readonly Dictionary<string, TypeAlias> aliases;
        private readonly TypeInference inference;
        
        public TypeSystem()
        {
            types = new Dictionary<string, Type>();
            aliases = new Dictionary<string, TypeAlias>();
            inference = new TypeInference(this);
            
            RegisterBuiltinTypes();
        }
        
        private void RegisterBuiltinTypes()
        {
            // Primitive types
            RegisterType(PrimitiveType.Bool);
            RegisterType(PrimitiveType.Byte);
            RegisterType(PrimitiveType.SByte);
            RegisterType(PrimitiveType.Short);
            RegisterType(PrimitiveType.UShort);
            RegisterType(PrimitiveType.Int);
            RegisterType(PrimitiveType.UInt);
            RegisterType(PrimitiveType.Long);
            RegisterType(PrimitiveType.ULong);
            RegisterType(PrimitiveType.Float);
            RegisterType(PrimitiveType.Double);
            RegisterType(PrimitiveType.Decimal);
            RegisterType(PrimitiveType.Char);
            RegisterType(PrimitiveType.String);
            RegisterType(PrimitiveType.Object);
            RegisterType(PrimitiveType.Void);
            
            // Special types
            RegisterType(new DynamicType());
            RegisterType(new VarType());
            RegisterType(new NullType());
        }
        
        public void RegisterType(Type type)
        {
            types[type.Name] = type;
        }
        
        public void RegisterAlias(string alias, Type type)
        {
            aliases[alias] = new TypeAlias { Name = alias, Type = type };
        }
        
        public Type GetType(string name)
        {
            if (types.TryGetValue(name, out var type))
                return type;
            
            if (aliases.TryGetValue(name, out var alias))
                return alias.Type;
            
            return null;
        }
        
        public Type InferType(Expression expr)
        {
            return inference.Infer(expr);
        }
        
        public bool IsAssignableFrom(Type target, Type source)
        {
            if (target == source)
                return true;
            
            if (target is DynamicType || source is DynamicType)
                return true;
            
            if (target is NullableType nullable)
                return source is NullType || IsAssignableFrom(nullable.UnderlyingType, source);
            
            if (source is NullType)
                return target.IsReference;
            
            return source.IsSubtypeOf(target);
        }
        
        public Type GetCommonType(Type type1, Type type2)
        {
            if (type1 == type2)
                return type1;
            
            if (IsAssignableFrom(type1, type2))
                return type1;
            
            if (IsAssignableFrom(type2, type1))
                return type2;
            
            // Find common base type
            var baseType = type1.GetCommonBase(type2);
            if (baseType != null)
                return baseType;
            
            return PrimitiveType.Object;
        }
    }
    
    /// <summary>
    /// Base class for all types
    /// </summary>
    public abstract class Type
    {
        public string Name { get; protected set; }
        public string FullName { get; protected set; }
        public TypeKind Kind { get; protected set; }
        public bool IsReference { get; protected set; }
        public bool IsNullable { get; protected set; }
        public Type BaseType { get; protected set; }
        public List<Type> Interfaces { get; protected set; }
        
        protected Type()
        {
            Interfaces = new List<Type>();
        }
        
        public abstract bool IsSubtypeOf(Type other);
        public abstract Type GetCommonBase(Type other);
        public abstract string ToString();
    }
    
    /// <summary>
    /// Type kinds
    /// </summary>
    public enum TypeKind
    {
        Primitive,
        Class,
        Interface,
        Struct,
        Enum,
        Array,
        Tuple,
        Generic,
        Nullable,
        Dynamic,
        Var,
        Null,
        Function,
        Component,
        Entity
    }
    
    /// <summary>
    /// Primitive types
    /// </summary>
    public class PrimitiveType : Type
    {
        // Singleton instances
        public static readonly PrimitiveType Bool = new PrimitiveType("bool", typeof(bool), false);
        public static readonly PrimitiveType Byte = new PrimitiveType("byte", typeof(byte), false);
        public static readonly PrimitiveType SByte = new PrimitiveType("sbyte", typeof(sbyte), false);
        public static readonly PrimitiveType Short = new PrimitiveType("short", typeof(short), false);
        public static readonly PrimitiveType UShort = new PrimitiveType("ushort", typeof(ushort), false);
        public static readonly PrimitiveType Int = new PrimitiveType("int", typeof(int), false);
        public static readonly PrimitiveType UInt = new PrimitiveType("uint", typeof(uint), false);
        public static readonly PrimitiveType Long = new PrimitiveType("long", typeof(long), false);
        public static readonly PrimitiveType ULong = new PrimitiveType("ulong", typeof(ulong), false);
        public static readonly PrimitiveType Float = new PrimitiveType("float", typeof(float), false);
        public static readonly PrimitiveType Double = new PrimitiveType("double", typeof(double), false);
        public static readonly PrimitiveType Decimal = new PrimitiveType("decimal", typeof(decimal), false);
        public static readonly PrimitiveType Char = new PrimitiveType("char", typeof(char), false);
        public static readonly PrimitiveType String = new PrimitiveType("string", typeof(string), true);
        public static readonly PrimitiveType Object = new PrimitiveType("object", typeof(object), true);
        public static readonly PrimitiveType Void = new PrimitiveType("void", typeof(void), false);
        
        public System.Type ClrType { get; }
        
        private PrimitiveType(string name, System.Type clrType, bool isReference)
        {
            Name = name;
            FullName = $"Ouroboros.{name}";
            Kind = TypeKind.Primitive;
            ClrType = clrType;
            IsReference = isReference;
            BaseType = name == "object" ? null : Object;
        }
        
        public override bool IsSubtypeOf(Type other)
        {
            if (other == this)
                return true;
            
            if (other == Object)
                return true;
            
            // Numeric conversions
            if (IsNumeric() && other is PrimitiveType otherPrim && otherPrim.IsNumeric())
            {
                return CanConvertTo(otherPrim);
            }
            
            return false;
        }
        
        public override Type GetCommonBase(Type other)
        {
            if (other is PrimitiveType)
                return Object;
            
            return null;
        }
        
        public override string ToString() => Name;
        
        public bool IsNumeric()
        {
            return this == Byte || this == SByte || this == Short || this == UShort ||
                   this == Int || this == UInt || this == Long || this == ULong ||
                   this == Float || this == Double || this == Decimal;
        }
        
        public bool IsIntegral()
        {
            return this == Byte || this == SByte || this == Short || this == UShort ||
                   this == Int || this == UInt || this == Long || this == ULong;
        }
        
        public bool IsFloatingPoint()
        {
            return this == Float || this == Double;
        }
        
        private bool CanConvertTo(PrimitiveType target)
        {
            // Implicit numeric conversions
            var conversions = new Dictionary<PrimitiveType, PrimitiveType[]>
            {
                [Byte] = new[] { Short, UShort, Int, UInt, Long, ULong, Float, Double, Decimal },
                [SByte] = new[] { Short, Int, Long, Float, Double, Decimal },
                [Short] = new[] { Int, Long, Float, Double, Decimal },
                [UShort] = new[] { Int, UInt, Long, ULong, Float, Double, Decimal },
                [Int] = new[] { Long, Float, Double, Decimal },
                [UInt] = new[] { Long, ULong, Float, Double, Decimal },
                [Long] = new[] { Float, Double, Decimal },
                [ULong] = new[] { Float, Double, Decimal },
                [Float] = new[] { Double },
                [Char] = new[] { UShort, Int, UInt, Long, ULong, Float, Double, Decimal }
            };
            
            return conversions.TryGetValue(this, out var targets) && targets.Contains(target);
        }
    }
    
    /// <summary>
    /// Class type
    /// </summary>
    public class ClassType : Type
    {
        public List<MemberInfo> Members { get; }
        public List<MethodInfo> Methods { get; }
        public List<PropertyInfo> Properties { get; }
        public List<EventInfo> Events { get; }
        public bool IsAbstract { get; set; }
        public bool IsSealed { get; set; }
        public bool IsPartial { get; set; }
        
        public ClassType(string name, string namespaceName = null)
        {
            Name = name;
            FullName = namespaceName != null ? $"{namespaceName}.{name}" : name;
            Kind = TypeKind.Class;
            IsReference = true;
            BaseType = PrimitiveType.Object;
            Members = new List<MemberInfo>();
            Methods = new List<MethodInfo>();
            Properties = new List<PropertyInfo>();
            Events = new List<EventInfo>();
        }
        
        public override bool IsSubtypeOf(Type other)
        {
            if (other == this)
                return true;
            
            if (BaseType != null && BaseType.IsSubtypeOf(other))
                return true;
            
            return Interfaces.Any(i => i.IsSubtypeOf(other));
        }
        
        public override Type GetCommonBase(Type other)
        {
            if (other is ClassType)
            {
                // Walk up inheritance chain
                Type current = this;
                while (current != null)
                {
                    if (other.IsSubtypeOf(current))
                        return current;
                    current = current.BaseType;
                }
            }
            
            return PrimitiveType.Object;
        }
        
        public override string ToString() => FullName;
    }
    
    /// <summary>
    /// Interface type
    /// </summary>
    public class InterfaceType : Type
    {
        public List<MethodInfo> Methods { get; }
        public List<PropertyInfo> Properties { get; }
        public List<EventInfo> Events { get; }
        
        public InterfaceType(string name, string namespaceName = null)
        {
            Name = name;
            FullName = namespaceName != null ? $"{namespaceName}.{name}" : name;
            Kind = TypeKind.Interface;
            IsReference = true;
            Methods = new List<MethodInfo>();
            Properties = new List<PropertyInfo>();
            Events = new List<EventInfo>();
        }
        
        public override bool IsSubtypeOf(Type other)
        {
            if (other == this)
                return true;
            
            return Interfaces.Any(i => i.IsSubtypeOf(other));
        }
        
        public override Type GetCommonBase(Type other)
        {
            // Find common interface
            foreach (var iface in Interfaces)
            {
                if (other.IsSubtypeOf(iface))
                    return iface;
            }
            
            return PrimitiveType.Object;
        }
        
        public override string ToString() => FullName;
    }
    
    /// <summary>
    /// Struct type
    /// </summary>
    public class StructType : Type
    {
        public List<MemberInfo> Members { get; }
        public List<MethodInfo> Methods { get; }
        public List<PropertyInfo> Properties { get; }
        
        public StructType(string name, string namespaceName = null)
        {
            Name = name;
            FullName = namespaceName != null ? $"{namespaceName}.{name}" : name;
            Kind = TypeKind.Struct;
            IsReference = false;
            Members = new List<MemberInfo>();
            Methods = new List<MethodInfo>();
            Properties = new List<PropertyInfo>();
        }
        
        public override bool IsSubtypeOf(Type other)
        {
            if (other == this)
                return true;
            
            if (other == PrimitiveType.Object)
                return true;
            
            return Interfaces.Any(i => i.IsSubtypeOf(other));
        }
        
        public override Type GetCommonBase(Type other)
        {
            return PrimitiveType.Object;
        }
        
        public override string ToString() => FullName;
    }
    
    /// <summary>
    /// Array type
    /// </summary>
    public class ArrayType : Type
    {
        public Type ElementType { get; }
        public int Rank { get; }
        
        public ArrayType(Type elementType, int rank = 1)
        {
            ElementType = elementType;
            Rank = rank;
            Name = rank == 1 ? $"{elementType.Name}[]" : $"{elementType.Name}[{new string(',', rank - 1)}]";
            FullName = Name;
            Kind = TypeKind.Array;
            IsReference = true;
            BaseType = PrimitiveType.Object;
        }
        
        public override bool IsSubtypeOf(Type other)
        {
            if (other == this)
                return true;
            
            if (other == PrimitiveType.Object)
                return true;
            
            if (other is ArrayType otherArray && Rank == otherArray.Rank)
            {
                return ElementType.IsSubtypeOf(otherArray.ElementType);
            }
            
            return false;
        }
        
        public override Type GetCommonBase(Type other)
        {
            if (other is ArrayType otherArray && Rank == otherArray.Rank)
            {
                var commonElement = ElementType.GetCommonBase(otherArray.ElementType);
                return new ArrayType(commonElement, Rank);
            }
            
            return PrimitiveType.Object;
        }
        
        public override string ToString() => Name;
    }
    
    /// <summary>
    /// Tuple type
    /// </summary>
    public class TupleType : Type
    {
        public List<Type> ElementTypes { get; }
        public List<string> ElementNames { get; }
        
        public TupleType(List<Type> elementTypes, List<string> elementNames = null)
        {
            ElementTypes = elementTypes;
            ElementNames = elementNames ?? new List<string>();
            
            var elements = elementTypes.Select((t, i) => 
                i < ElementNames.Count && !string.IsNullOrEmpty(ElementNames[i]) 
                    ? $"{ElementNames[i]}: {t}" 
                    : t.ToString());
            
            Name = $"({string.Join(", ", elements)})";
            FullName = Name;
            Kind = TypeKind.Tuple;
            IsReference = false;
            BaseType = PrimitiveType.Object;
        }
        
        public override bool IsSubtypeOf(Type other)
        {
            if (other == this)
                return true;
            
            if (other == PrimitiveType.Object)
                return true;
            
            if (other is TupleType otherTuple && ElementTypes.Count == otherTuple.ElementTypes.Count)
            {
                for (int i = 0; i < ElementTypes.Count; i++)
                {
                    if (!ElementTypes[i].IsSubtypeOf(otherTuple.ElementTypes[i]))
                        return false;
                }
                return true;
            }
            
            return false;
        }
        
        public override Type GetCommonBase(Type other)
        {
            if (other is TupleType otherTuple && ElementTypes.Count == otherTuple.ElementTypes.Count)
            {
                var commonTypes = new List<Type>();
                for (int i = 0; i < ElementTypes.Count; i++)
                {
                    commonTypes.Add(ElementTypes[i].GetCommonBase(otherTuple.ElementTypes[i]));
                }
                return new TupleType(commonTypes, ElementNames);
            }
            
            return PrimitiveType.Object;
        }
        
        public override string ToString() => Name;
    }
    
    /// <summary>
    /// Generic type
    /// </summary>
    public class GenericType : Type
    {
        public Type Definition { get; }
        public List<Type> TypeArguments { get; }
        
        public GenericType(Type definition, List<Type> typeArguments)
        {
            Definition = definition;
            TypeArguments = typeArguments;
            Name = $"{definition.Name}<{string.Join(", ", typeArguments)}>";
            FullName = $"{definition.FullName}<{string.Join(", ", typeArguments.Select(t => t.FullName))}>";
            Kind = TypeKind.Generic;
            IsReference = definition.IsReference;
            BaseType = definition.BaseType;
            Interfaces.AddRange(definition.Interfaces);
        }
        
        public override bool IsSubtypeOf(Type other)
        {
            if (other == this)
                return true;
            
            if (other is GenericType otherGeneric && 
                Definition == otherGeneric.Definition &&
                TypeArguments.Count == otherGeneric.TypeArguments.Count)
            {
                // Check variance
                for (int i = 0; i < TypeArguments.Count; i++)
                {
                    if (!CheckVariance(TypeArguments[i], otherGeneric.TypeArguments[i], i))
                        return false;
                }
                return true;
            }
            
            return Definition.IsSubtypeOf(other);
        }
        
        public override Type GetCommonBase(Type other)
        {
            if (other is GenericType otherGeneric && Definition == otherGeneric.Definition)
            {
                var commonArgs = new List<Type>();
                for (int i = 0; i < TypeArguments.Count; i++)
                {
                    commonArgs.Add(TypeArguments[i].GetCommonBase(otherGeneric.TypeArguments[i]));
                }
                return new GenericType(Definition, commonArgs);
            }
            
            return Definition.GetCommonBase(other);
        }
        
        public override string ToString() => Name;
        
        private bool CheckVariance(Type arg1, Type arg2, int position)
        {
            // Simplified variance checking
            // Would need to check actual variance annotations (in/out)
            return arg1.IsSubtypeOf(arg2);
        }
    }
    
    /// <summary>
    /// Nullable type
    /// </summary>
    public class NullableType : Type
    {
        public Type UnderlyingType { get; }
        
        public NullableType(Type underlyingType)
        {
            UnderlyingType = underlyingType;
            Name = $"{underlyingType.Name}?";
            FullName = $"{underlyingType.FullName}?";
            Kind = TypeKind.Nullable;
            IsReference = true;
            IsNullable = true;
            BaseType = underlyingType.BaseType;
            Interfaces.AddRange(underlyingType.Interfaces);
        }
        
        public override bool IsSubtypeOf(Type other)
        {
            if (other == this)
                return true;
            
            if (other is NullableType otherNullable)
                return UnderlyingType.IsSubtypeOf(otherNullable.UnderlyingType);
            
            return UnderlyingType.IsSubtypeOf(other);
        }
        
        public override Type GetCommonBase(Type other)
        {
            if (other is NullableType otherNullable)
                return new NullableType(UnderlyingType.GetCommonBase(otherNullable.UnderlyingType));
            
            if (other is NullType)
                return this;
            
            return new NullableType(UnderlyingType.GetCommonBase(other));
        }
        
        public override string ToString() => Name;
    }
    
    /// <summary>
    /// Dynamic type
    /// </summary>
    public class DynamicType : Type
    {
        public DynamicType()
        {
            Name = "dynamic";
            FullName = "dynamic";
            Kind = TypeKind.Dynamic;
            IsReference = true;
        }
        
        public override bool IsSubtypeOf(Type other) => true;
        public override Type GetCommonBase(Type other) => this;
        public override string ToString() => Name;
    }
    
    /// <summary>
    /// Var type (for type inference)
    /// </summary>
    public class VarType : Type
    {
        public VarType()
        {
            Name = "var";
            FullName = "var";
            Kind = TypeKind.Var;
            IsReference = true;
        }
        
        public override bool IsSubtypeOf(Type other) => false;
        public override Type GetCommonBase(Type other) => PrimitiveType.Object;
        public override string ToString() => Name;
    }
    
    /// <summary>
    /// Null type
    /// </summary>
    public class NullType : Type
    {
        public NullType()
        {
            Name = "null";
            FullName = "null";
            Kind = TypeKind.Null;
            IsReference = true;
            IsNullable = true;
        }
        
        public override bool IsSubtypeOf(Type other) => other.IsReference && other.IsNullable;
        public override Type GetCommonBase(Type other) => other.IsReference ? other : PrimitiveType.Object;
        public override string ToString() => Name;
    }
    
    /// <summary>
    /// Function type
    /// </summary>
    public class FunctionType : Type
    {
        public List<Type> ParameterTypes { get; }
        public Type ReturnType { get; }
        
        public FunctionType(List<Type> parameterTypes, Type returnType)
        {
            ParameterTypes = parameterTypes;
            ReturnType = returnType;
            Name = $"({string.Join(", ", parameterTypes)}) => {returnType}";
            FullName = Name;
            Kind = TypeKind.Function;
            IsReference = true;
            BaseType = PrimitiveType.Object;
        }
        
        public override bool IsSubtypeOf(Type other)
        {
            if (other == this)
                return true;
            
            if (other == PrimitiveType.Object)
                return true;
            
            if (other is FunctionType otherFunc && 
                ParameterTypes.Count == otherFunc.ParameterTypes.Count)
            {
                // Contravariant in parameters
                for (int i = 0; i < ParameterTypes.Count; i++)
                {
                    if (!otherFunc.ParameterTypes[i].IsSubtypeOf(ParameterTypes[i]))
                        return false;
                }
                
                // Covariant in return type
                return ReturnType.IsSubtypeOf(otherFunc.ReturnType);
            }
            
            return false;
        }
        
        public override Type GetCommonBase(Type other)
        {
            if (other is FunctionType)
                return PrimitiveType.Object;
            
            return PrimitiveType.Object;
        }
        
        public override string ToString() => Name;
    }
    
    /// <summary>
    /// Type alias
    /// </summary>
    public class TypeAlias
    {
        public string Name { get; set; }
        public Type Type { get; set; }
    }
    
    /// <summary>
    /// Member information
    /// </summary>
    public class MemberInfo
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public AccessModifier Access { get; set; }
        public bool IsStatic { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsConst { get; set; }
    }
    
    /// <summary>
    /// Method information
    /// </summary>
    public class MethodInfo : MemberInfo
    {
        public List<ParameterInfo> Parameters { get; set; }
        public Type ReturnType { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsOverride { get; set; }
        public bool IsAbstract { get; set; }
        public bool IsAsync { get; set; }
        public List<string> GenericParameters { get; set; }
    }
    
    /// <summary>
    /// Property information
    /// </summary>
    public class PropertyInfo : MemberInfo
    {
        public bool HasGetter { get; set; }
        public bool HasSetter { get; set; }
        public AccessModifier GetterAccess { get; set; }
        public AccessModifier SetterAccess { get; set; }
    }
    
    /// <summary>
    /// Event information
    /// </summary>
    public class EventInfo : MemberInfo
    {
        public Type DelegateType { get; set; }
    }
    
    /// <summary>
    /// Parameter information
    /// </summary>
    public class ParameterInfo
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public bool IsOptional { get; set; }
        public object DefaultValue { get; set; }
        public bool IsRef { get; set; }
        public bool IsOut { get; set; }
        public bool IsParams { get; set; }
    }
    
    /// <summary>
    /// Access modifiers
    /// </summary>
    public enum AccessModifier
    {
        Private,
        Protected,
        Internal,
        Public
    }
    
    /// <summary>
    /// Type inference engine
    /// </summary>
    public class TypeInference
    {
        private readonly TypeSystem typeSystem;
        private readonly Dictionary<string, Type> inferredTypes;
        
        public TypeInference(TypeSystem typeSystem)
        {
            this.typeSystem = typeSystem;
            inferredTypes = new Dictionary<string, Type>();
        }
        
        public Type Infer(Expression expr)
        {
            return expr switch
            {
                LiteralExpression lit => InferLiteral(lit),
                VariableExpression var => InferVariable(var),
                BinaryExpression bin => InferBinary(bin),
                UnaryExpression un => InferUnary(un),
                CallExpression call => InferCall(call),
                LambdaExpression lambda => InferLambda(lambda),
                ArrayExpression arr => InferArray(arr),
                _ => PrimitiveType.Object
            };
        }
        
        private Type InferLiteral(LiteralExpression expr)
        {
            return expr.Value switch
            {
                null => new NullType(),
                bool => PrimitiveType.Bool,
                byte => PrimitiveType.Byte,
                sbyte => PrimitiveType.SByte,
                short => PrimitiveType.Short,
                ushort => PrimitiveType.UShort,
                int => PrimitiveType.Int,
                uint => PrimitiveType.UInt,
                long => PrimitiveType.Long,
                ulong => PrimitiveType.ULong,
                float => PrimitiveType.Float,
                double => PrimitiveType.Double,
                decimal => PrimitiveType.Decimal,
                char => PrimitiveType.Char,
                string => PrimitiveType.String,
                _ => PrimitiveType.Object
            };
        }
        
        private Type InferVariable(VariableExpression expr)
        {
            if (inferredTypes.TryGetValue(expr.Name, out var type))
                return type;
            
            return PrimitiveType.Object;
        }
        
        private Type InferBinary(BinaryExpression expr)
        {
            var leftType = Infer(expr.Left);
            var rightType = Infer(expr.Right);
            
            switch (expr.Operator)
            {
                case TokenType.Plus:
                    if (leftType == PrimitiveType.String || rightType == PrimitiveType.String)
                        return PrimitiveType.String;
                    return GetNumericResultType(leftType, rightType);
                
                case TokenType.Minus:
                case TokenType.Star:
                case TokenType.Slash:
                case TokenType.Percent:
                    return GetNumericResultType(leftType, rightType);
                
                case TokenType.Less:
                case TokenType.Greater:
                case TokenType.LessEqual:
                case TokenType.GreaterEqual:
                case TokenType.EqualEqual:
                case TokenType.BangEqual:
                    return PrimitiveType.Bool;
                
                case TokenType.AmpersandAmpersand:
                case TokenType.PipePipe:
                    return PrimitiveType.Bool;
                
                default:
                    return typeSystem.GetCommonType(leftType, rightType);
            }
        }
        
        private Type InferUnary(UnaryExpression expr)
        {
            var operandType = Infer(expr.Operand);
            
            switch (expr.Operator)
            {
                case TokenType.Bang:
                    return PrimitiveType.Bool;
                
                case TokenType.Minus:
                case TokenType.Plus:
                    return operandType;
                
                default:
                    return operandType;
            }
        }
        
        private Type InferCall(CallExpression expr)
        {
            // Would need to look up method signature
            return PrimitiveType.Object;
        }
        
        private Type InferLambda(LambdaExpression expr)
        {
            // Infer parameter and return types
            var paramTypes = new List<Type>();
            foreach (var param in expr.Parameters)
            {
                paramTypes.Add(PrimitiveType.Object);
            }
            
            var returnType = Infer(expr.Body);
            return new FunctionType(paramTypes, returnType);
        }
        
        private Type InferArray(ArrayExpression expr)
        {
            if (expr.Elements.Count == 0)
                return new ArrayType(PrimitiveType.Object);
            
            Type elementType = null;
            foreach (var element in expr.Elements)
            {
                var type = Infer(element);
                elementType = elementType == null ? type : typeSystem.GetCommonType(elementType, type);
            }
            
            return new ArrayType(elementType);
        }
        
        private Type GetNumericResultType(Type left, Type right)
        {
            if (left == PrimitiveType.Double || right == PrimitiveType.Double)
                return PrimitiveType.Double;
            
            if (left == PrimitiveType.Float || right == PrimitiveType.Float)
                return PrimitiveType.Float;
            
            if (left == PrimitiveType.Decimal || right == PrimitiveType.Decimal)
                return PrimitiveType.Decimal;
            
            if (left == PrimitiveType.Long || right == PrimitiveType.Long)
                return PrimitiveType.Long;
            
            if (left == PrimitiveType.ULong || right == PrimitiveType.ULong)
                return PrimitiveType.ULong;
            
            return PrimitiveType.Int;
        }
    }
} 
using System;
using System.Collections.Generic;
using System.Linq;
using Ouroboros.Core.AST;
using Ouroboros.Tokens;

namespace Ouroboros.Core
{
    /// <summary>
    /// Domain system for Ouroboros - enables scoped operator redefinition
    /// Allows different domains (Physics, Statistics, etc.) to redefine operators
    /// </summary>
    public class DomainSystem
    {
        private readonly Dictionary<string, Domain> domains = new();
        private readonly Stack<Domain> domainStack = new();
        
        public DomainSystem()
        {
            // Register built-in domains
            RegisterBuiltinDomains();
        }
        
        /// <summary>
        /// Register a new domain in the system
        /// </summary>
        public void RegisterDomain(Domain domain)
        {
            domains[domain.Name] = domain;
        }
        
        /// <summary>
        /// Enter a domain scope - brings domain operators into scope
        /// </summary>
        public void EnterDomain(string domainName)
        {
            if (!domains.ContainsKey(domainName))
            {
                throw new InvalidOperationException($"Domain '{domainName}' not found");
            }
            
            domainStack.Push(domains[domainName]);
        }
        
        /// <summary>
        /// Exit the current domain scope
        /// </summary>
        public void ExitDomain()
        {
            if (domainStack.Count > 0)
            {
                domainStack.Pop();
            }
        }
        
        /// <summary>
        /// Resolve an operator in the current domain context
        /// </summary>
        public OperatorDefinition? ResolveOperator(string operatorSymbol, TypeNode leftType, TypeNode? rightType = null)
        {
            // Check current domain stack from top to bottom
            foreach (var domain in domainStack.Reverse())
            {
                var operatorDef = domain.ResolveOperator(operatorSymbol, leftType, rightType);
                if (operatorDef != null)
                {
                    return operatorDef;
                }
            }
            
            // Fall back to default operators
            return ResolveDefaultOperator(operatorSymbol, leftType, rightType);
        }
        
        /// <summary>
        /// Get the current domain (top of stack)
        /// </summary>
        public Domain? CurrentDomain => domainStack.Count > 0 ? domainStack.Peek() : null;
        
        /// <summary>
        /// Check if we're currently in a specific domain
        /// </summary>
        public bool IsInDomain(string domainName)
        {
            return domainStack.Any(d => d.Name == domainName);
        }
        
        private void RegisterBuiltinDomains()
        {
            RegisterPhysicsDomain();
            RegisterStatisticsDomain();
            RegisterMathematicalDomain();
        }
        
        private void RegisterPhysicsDomain()
        {
            var physics = new Domain("Physics");
            
            // Register Physics domain operators
            // × means cross_product for Vector3
            physics.RegisterOperator("×", "Vector3", "Vector3", 
                new OperatorDefinition("cross_product", "Vector3", OperatorAssociativity.Left, 6));
            
            // · means dot_product for Vector3  
            physics.RegisterOperator("·", "Vector3", "Vector3",
                new OperatorDefinition("dot_product", "float", OperatorAssociativity.Left, 6));
            
            // ∇ means gradient_operator
            physics.RegisterOperator("∇", "Function", null,
                new OperatorDefinition("gradient_operator", "Vector3", OperatorAssociativity.Right, 9));
            
            // ∂ means partial_derivative
            physics.RegisterOperator("∂", "Function", "Variable",
                new OperatorDefinition("partial_derivative", "Function", OperatorAssociativity.Right, 9));
            
            // Register physical constants
            physics.RegisterConstant("c", "299792458", "double"); // Speed of light
            physics.RegisterConstant("ε₀", "8.854e-12", "double"); // Permittivity
            physics.RegisterConstant("μ₀", "4π × 1e-7", "double"); // Permeability
            physics.RegisterConstant("ℏ", "1.054e-34", "double"); // Reduced Planck constant
            
            RegisterDomain(physics);
        }
        
        private void RegisterStatisticsDomain()
        {
            var statistics = new Domain("Statistics");
            
            // Register Statistics domain operators
            // μ means mean
            statistics.RegisterOperator("μ", "Array", null,
                new OperatorDefinition("mean", "double", OperatorAssociativity.Right, 9));
            
            // σ means standard_deviation
            statistics.RegisterOperator("σ", "Array", null,
                new OperatorDefinition("standard_deviation", "double", OperatorAssociativity.Right, 9));
            
            // σ² means variance (compound operator)
            statistics.RegisterOperator("σ²", "Array", null,
                new OperatorDefinition("variance", "double", OperatorAssociativity.Right, 9));
            
            // ρ means correlation
            statistics.RegisterOperator("ρ", "Array", "Array",
                new OperatorDefinition("correlation", "double", OperatorAssociativity.Left, 6));
            
            // Register statistical constants
            statistics.RegisterConstant("normal_95_percentile", "1.96", "double");
            statistics.RegisterConstant("χ²_critical", "3.841", "double");
            
            RegisterDomain(statistics);
        }
        
        private void RegisterMathematicalDomain()
        {
            var math = new Domain("Mathematics");
            
            // Register set theory operators
            // ∪ means union
            math.RegisterOperator("∪", "Set", "Set",
                new OperatorDefinition("union", "Set", OperatorAssociativity.Left, 4));
            
            // ∩ means intersection
            math.RegisterOperator("∩", "Set", "Set",
                new OperatorDefinition("intersection", "Set", OperatorAssociativity.Left, 5));
            
            // ∈ means element_of
            math.RegisterOperator("∈", "Element", "Set",
                new OperatorDefinition("element_of", "bool", OperatorAssociativity.Left, 3));
            
            // ∫ means integral
            math.RegisterOperator("∫", "Function", "Range",
                new OperatorDefinition("integrate", "double", OperatorAssociativity.Right, 9));
            
            // ∑ means summation
            math.RegisterOperator("∑", "Function", "Range",
                new OperatorDefinition("sum", "double", OperatorAssociativity.Right, 8));
            
            // ∏ means product
            math.RegisterOperator("∏", "Function", "Range",
                new OperatorDefinition("product", "double", OperatorAssociativity.Right, 8));
            
            RegisterDomain(math);
        }
        
        private OperatorDefinition? ResolveDefaultOperator(string operatorSymbol, TypeNode leftType, TypeNode? rightType)
        {
            // Default operator resolution for standard operators
            return operatorSymbol switch
            {
                "+" => new OperatorDefinition("add", InferResultType(leftType, rightType), OperatorAssociativity.Left, 4),
                "-" => new OperatorDefinition("subtract", InferResultType(leftType, rightType), OperatorAssociativity.Left, 4),
                "*" => new OperatorDefinition("multiply", InferResultType(leftType, rightType), OperatorAssociativity.Left, 5),
                "/" => new OperatorDefinition("divide", InferResultType(leftType, rightType), OperatorAssociativity.Left, 5),
                "//" => new OperatorDefinition("integer_divide", InferResultType(leftType, rightType), OperatorAssociativity.Left, 5),
                "**" => new OperatorDefinition("power", InferResultType(leftType, rightType), OperatorAssociativity.Right, 7),
                "%" => new OperatorDefinition("modulo", InferResultType(leftType, rightType), OperatorAssociativity.Left, 5),
                "==" => new OperatorDefinition("equals", "bool", OperatorAssociativity.Left, 3),
                "!=" => new OperatorDefinition("not_equals", "bool", OperatorAssociativity.Left, 3),
                "<" => new OperatorDefinition("less_than", "bool", OperatorAssociativity.Left, 3),
                ">" => new OperatorDefinition("greater_than", "bool", OperatorAssociativity.Left, 3),
                "<=" => new OperatorDefinition("less_equal", "bool", OperatorAssociativity.Left, 3),
                ">=" => new OperatorDefinition("greater_equal", "bool", OperatorAssociativity.Left, 3),
                "<=>" => new OperatorDefinition("spaceship", "int", OperatorAssociativity.Left, 3),
                "&&" => new OperatorDefinition("logical_and", "bool", OperatorAssociativity.Left, 2),
                "||" => new OperatorDefinition("logical_or", "bool", OperatorAssociativity.Left, 1),
                "&" => new OperatorDefinition("bitwise_and", InferResultType(leftType, rightType), OperatorAssociativity.Left, 5),
                "|" => new OperatorDefinition("bitwise_or", InferResultType(leftType, rightType), OperatorAssociativity.Left, 3),
                "^" => new OperatorDefinition("bitwise_xor", InferResultType(leftType, rightType), OperatorAssociativity.Left, 4),
                "<<" => new OperatorDefinition("left_shift", InferResultType(leftType, rightType), OperatorAssociativity.Left, 6),
                ">>" => new OperatorDefinition("right_shift", InferResultType(leftType, rightType), OperatorAssociativity.Left, 6),
                "??" => new OperatorDefinition("null_coalesce", InferResultType(leftType, rightType), OperatorAssociativity.Right, 2),
                ".." => new OperatorDefinition("inclusive_range", "Range", OperatorAssociativity.Left, 4),
                "..." => new OperatorDefinition("exclusive_range", "Range", OperatorAssociativity.Left, 4),
                _ => null
            };
        }
        
        private string InferResultType(TypeNode leftType, TypeNode? rightType)
        {
            // Simple type inference - in a real implementation this would be more sophisticated
            if (rightType == null) return leftType.Name;
            
            // Numeric type promotion rules
            if (IsNumericType(leftType.Name) && IsNumericType(rightType.Name))
            {
                return PromoteNumericTypes(leftType.Name, rightType.Name);
            }
            
            return leftType.Name;
        }
        
        private bool IsNumericType(string typeName)
        {
            return typeName switch
            {
                "int" or "i32" or "i64" or "float" or "f32" or "f64" or "double" => true,
                _ => false
            };
        }
        
        private string PromoteNumericTypes(string type1, string type2)
        {
            // Type promotion hierarchy: double > float > i64 > i32 > int
            var hierarchy = new[] { "int", "i32", "i64", "float", "f32", "double", "f64" };
            
            var index1 = Array.IndexOf(hierarchy, type1);
            var index2 = Array.IndexOf(hierarchy, type2);
            
            if (index1 == -1) index1 = 0;
            if (index2 == -1) index2 = 0;
            
            return hierarchy[Math.Max(index1, index2)];
        }
    }
    
    /// <summary>
    /// Represents a domain with its operator definitions and constants
    /// </summary>
    public class Domain
    {
        public string Name { get; }
        private readonly Dictionary<string, Dictionary<string, OperatorDefinition>> operators = new();
        private readonly Dictionary<string, DomainConstant> constants = new();
        
        public Domain(string name)
        {
            Name = name;
        }
        
        /// <summary>
        /// Register an operator for specific types in this domain
        /// </summary>
        public void RegisterOperator(string symbol, string leftType, string? rightType, OperatorDefinition definition)
        {
            var key = CreateOperatorKey(leftType, rightType);
            
            if (!operators.ContainsKey(symbol))
            {
                operators[symbol] = new Dictionary<string, OperatorDefinition>();
            }
            
            operators[symbol][key] = definition;
        }
        
        /// <summary>
        /// Register a constant in this domain
        /// </summary>
        public void RegisterConstant(string name, string value, string type)
        {
            constants[name] = new DomainConstant(name, value, type);
        }
        
        /// <summary>
        /// Resolve an operator for given types
        /// </summary>
        public OperatorDefinition? ResolveOperator(string symbol, TypeNode leftType, TypeNode? rightType)
        {
            if (!operators.ContainsKey(symbol))
                return null;
            
            var key = CreateOperatorKey(leftType.Name, rightType?.Name);
            
            return operators[symbol].TryGetValue(key, out var definition) ? definition : null;
        }
        
        /// <summary>
        /// Get a constant from this domain
        /// </summary>
        public DomainConstant? GetConstant(string name)
        {
            return constants.TryGetValue(name, out var constant) ? constant : null;
        }
        
        /// <summary>
        /// Get all operators defined in this domain
        /// </summary>
        public IEnumerable<string> GetOperators()
        {
            return operators.Keys;
        }
        
        /// <summary>
        /// Get all constants defined in this domain
        /// </summary>
        public IEnumerable<DomainConstant> GetConstants()
        {
            return constants.Values;
        }
        
        private string CreateOperatorKey(string leftType, string? rightType)
        {
            return rightType == null ? leftType : $"{leftType}:{rightType}";
        }
    }
    
    /// <summary>
    /// Represents an operator definition within a domain
    /// </summary>
    public class OperatorDefinition
    {
        public string FunctionName { get; }
        public string ResultType { get; }
        public OperatorAssociativity Associativity { get; }
        public int Precedence { get; }
        
        public OperatorDefinition(string functionName, string resultType, OperatorAssociativity associativity, int precedence)
        {
            FunctionName = functionName;
            ResultType = resultType;
            Associativity = associativity;
            Precedence = precedence;
        }
    }
    
    /// <summary>
    /// Represents a constant defined within a domain
    /// </summary>
    public class DomainConstant
    {
        public string Name { get; }
        public string Value { get; }
        public string Type { get; }
        
        public DomainConstant(string name, string value, string type)
        {
            Name = name;
            Value = value;
            Type = type;
        }
    }
    
    /// <summary>
    /// Operator associativity
    /// </summary>
    public enum OperatorAssociativity
    {
        Left,
        Right,
        None
    }
} 
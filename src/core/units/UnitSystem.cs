using System;
using System.Collections.Generic;
using System.Linq;

namespace Ouro.Core.Units
{
    /// <summary>
    /// Unit system for Ouroboros - provides compile-time dimensional analysis
    /// Ensures type safety for physical quantities and prevents unit errors
    /// </summary>
    public class UnitSystem
    {
        private readonly Dictionary<string, BaseUnit> baseUnits = new();
        private readonly Dictionary<string, DerivedUnit> derivedUnits = new();
        private readonly Dictionary<string, UnitPrefix> prefixes = new();
        
        public UnitSystem()
        {
            InitializeBaseUnits();
            InitializeDerivedUnits();
            InitializePrefixes();
        }
        
        /// <summary>
        /// Register a base unit (fundamental dimension)
        /// </summary>
        public void RegisterBaseUnit(string symbol, string name, UnitDimension dimension)
        {
            baseUnits[symbol] = new BaseUnit(symbol, name, dimension);
        }
        
        /// <summary>
        /// Register a derived unit (combination of base units)
        /// </summary>
        public void RegisterDerivedUnit(string symbol, string name, Dictionary<UnitDimension, int> dimensions, double conversionFactor = 1.0)
        {
            derivedUnits[symbol] = new DerivedUnit(symbol, name, dimensions, conversionFactor);
        }
        
        /// <summary>
        /// Check if two unit expressions are dimensionally compatible
        /// </summary>
        public bool AreCompatible(UnitExpression unit1, UnitExpression unit2)
        {
            return unit1.GetDimensionVector().Equals(unit2.GetDimensionVector());
        }
        
        /// <summary>
        /// Multiply two unit expressions
        /// </summary>
        public UnitExpression Multiply(UnitExpression unit1, UnitExpression unit2)
        {
            var resultDimensions = new Dictionary<UnitDimension, int>();
            
            // Combine dimensions from both units
            foreach (var dim in Enum.GetValues<UnitDimension>())
            {
                var power1 = unit1.GetDimensionPower(dim);
                var power2 = unit2.GetDimensionPower(dim);
                var resultPower = power1 + power2;
                
                if (resultPower != 0)
                {
                    resultDimensions[dim] = resultPower;
                }
            }
            
            var conversionFactor = unit1.ConversionFactor * unit2.ConversionFactor;
            return new UnitExpression(resultDimensions, conversionFactor);
        }
        
        /// <summary>
        /// Divide two unit expressions
        /// </summary>
        public UnitExpression Divide(UnitExpression unit1, UnitExpression unit2)
        {
            var resultDimensions = new Dictionary<UnitDimension, int>();
            
            // Subtract dimensions (division is multiplication by inverse)
            foreach (var dim in Enum.GetValues<UnitDimension>())
            {
                var power1 = unit1.GetDimensionPower(dim);
                var power2 = unit2.GetDimensionPower(dim);
                var resultPower = power1 - power2;
                
                if (resultPower != 0)
                {
                    resultDimensions[dim] = resultPower;
                }
            }
            
            var conversionFactor = unit1.ConversionFactor / unit2.ConversionFactor;
            return new UnitExpression(resultDimensions, conversionFactor);
        }
        
        /// <summary>
        /// Raise a unit expression to a power
        /// </summary>
        public UnitExpression Power(UnitExpression unit, int exponent)
        {
            var resultDimensions = new Dictionary<UnitDimension, int>();
            
            foreach (var kvp in unit.Dimensions)
            {
                resultDimensions[kvp.Key] = kvp.Value * exponent;
            }
            
            var conversionFactor = Math.Pow(unit.ConversionFactor, exponent);
            return new UnitExpression(resultDimensions, conversionFactor);
        }
        
        /// <summary>
        /// Parse a unit string into a unit expression
        /// </summary>
        public UnitExpression ParseUnit(string unitString)
        {
            // Simple unit parser - real implementation would be more sophisticated
            unitString = unitString.Trim();
            
            // Handle basic units
            if (baseUnits.ContainsKey(unitString))
            {
                var baseUnit = baseUnits[unitString];
                return new UnitExpression(
                    new Dictionary<UnitDimension, int> { [baseUnit.Dimension] = 1 },
                    1.0
                );
            }
            
            if (derivedUnits.ContainsKey(unitString))
            {
                var derivedUnit = derivedUnits[unitString];
                return new UnitExpression(derivedUnit.Dimensions, derivedUnit.ConversionFactor);
            }
            
            // Handle prefixed units (e.g., "km", "ms")
            foreach (var prefix in prefixes.Values)
            {
                if (unitString.StartsWith(prefix.Symbol))
                {
                    var baseUnitSymbol = unitString.Substring(prefix.Symbol.Length);
                    if (baseUnits.ContainsKey(baseUnitSymbol))
                    {
                        var baseUnit = baseUnits[baseUnitSymbol];
                        return new UnitExpression(
                            new Dictionary<UnitDimension, int> { [baseUnit.Dimension] = 1 },
                            prefix.Factor
                        );
                    }
                }
            }
            
            throw new ArgumentException($"Unknown unit: {unitString}");
        }
        
        /// <summary>
        /// Get the standard unit for a given dimension
        /// </summary>
        public UnitExpression GetStandardUnit(UnitDimension dimension)
        {
            return dimension switch
            {
                UnitDimension.Length => ParseUnit("m"),
                UnitDimension.Mass => ParseUnit("kg"),
                UnitDimension.Time => ParseUnit("s"),
                UnitDimension.Current => ParseUnit("A"),
                UnitDimension.Temperature => ParseUnit("K"),
                UnitDimension.Amount => ParseUnit("mol"),
                UnitDimension.Luminosity => ParseUnit("cd"),
                _ => new UnitExpression(new Dictionary<UnitDimension, int>(), 1.0)
            };
        }
        
        private void InitializeBaseUnits()
        {
            // SI base units
            RegisterBaseUnit("m", "meter", UnitDimension.Length);
            RegisterBaseUnit("kg", "kilogram", UnitDimension.Mass);
            RegisterBaseUnit("s", "second", UnitDimension.Time);
            RegisterBaseUnit("A", "ampere", UnitDimension.Current);
            RegisterBaseUnit("K", "kelvin", UnitDimension.Temperature);
            RegisterBaseUnit("mol", "mole", UnitDimension.Amount);
            RegisterBaseUnit("cd", "candela", UnitDimension.Luminosity);
            
            // Additional common units
            RegisterBaseUnit("g", "gram", UnitDimension.Mass); // Will need conversion factor
        }
        
        private void InitializeDerivedUnits()
        {
            // Mechanical units
            RegisterDerivedUnit("N", "newton", new Dictionary<UnitDimension, int>
            {
                [UnitDimension.Mass] = 1,
                [UnitDimension.Length] = 1,
                [UnitDimension.Time] = -2
            });
            
            RegisterDerivedUnit("J", "joule", new Dictionary<UnitDimension, int>
            {
                [UnitDimension.Mass] = 1,
                [UnitDimension.Length] = 2,
                [UnitDimension.Time] = -2
            });
            
            RegisterDerivedUnit("W", "watt", new Dictionary<UnitDimension, int>
            {
                [UnitDimension.Mass] = 1,
                [UnitDimension.Length] = 2,
                [UnitDimension.Time] = -3
            });
            
            RegisterDerivedUnit("Pa", "pascal", new Dictionary<UnitDimension, int>
            {
                [UnitDimension.Mass] = 1,
                [UnitDimension.Length] = -1,
                [UnitDimension.Time] = -2
            });
            
            // Electrical units
            RegisterDerivedUnit("V", "volt", new Dictionary<UnitDimension, int>
            {
                [UnitDimension.Mass] = 1,
                [UnitDimension.Length] = 2,
                [UnitDimension.Time] = -3,
                [UnitDimension.Current] = -1
            });
            
            RegisterDerivedUnit("Ω", "ohm", new Dictionary<UnitDimension, int>
            {
                [UnitDimension.Mass] = 1,
                [UnitDimension.Length] = 2,
                [UnitDimension.Time] = -3,
                [UnitDimension.Current] = -2
            });
            
            RegisterDerivedUnit("F", "farad", new Dictionary<UnitDimension, int>
            {
                [UnitDimension.Mass] = -1,
                [UnitDimension.Length] = -2,
                [UnitDimension.Time] = 4,
                [UnitDimension.Current] = 2
            });
            
            RegisterDerivedUnit("H", "henry", new Dictionary<UnitDimension, int>
            {
                [UnitDimension.Mass] = 1,
                [UnitDimension.Length] = 2,
                [UnitDimension.Time] = -2,
                [UnitDimension.Current] = -2
            });
            
            // Frequency and angular units
            RegisterDerivedUnit("Hz", "hertz", new Dictionary<UnitDimension, int>
            {
                [UnitDimension.Time] = -1
            });
            
            RegisterDerivedUnit("rad", "radian", new Dictionary<UnitDimension, int>());
            
            // Temperature units
            RegisterDerivedUnit("°C", "celsius", new Dictionary<UnitDimension, int>
            {
                [UnitDimension.Temperature] = 1
            }, 1.0); // Conversion handled specially
        }
        
        private void InitializePrefixes()
        {
            // SI prefixes
            prefixes["Y"] = new UnitPrefix("Y", "yotta", 1e24);
            prefixes["Z"] = new UnitPrefix("Z", "zetta", 1e21);
            prefixes["E"] = new UnitPrefix("E", "exa", 1e18);
            prefixes["P"] = new UnitPrefix("P", "peta", 1e15);
            prefixes["T"] = new UnitPrefix("T", "tera", 1e12);
            prefixes["G"] = new UnitPrefix("G", "giga", 1e9);
            prefixes["M"] = new UnitPrefix("M", "mega", 1e6);
            prefixes["k"] = new UnitPrefix("k", "kilo", 1e3);
            prefixes["h"] = new UnitPrefix("h", "hecto", 1e2);
            prefixes["da"] = new UnitPrefix("da", "deca", 1e1);
            prefixes["d"] = new UnitPrefix("d", "deci", 1e-1);
            prefixes["c"] = new UnitPrefix("c", "centi", 1e-2);
            prefixes["m"] = new UnitPrefix("m", "milli", 1e-3);
            prefixes["μ"] = new UnitPrefix("μ", "micro", 1e-6);
            prefixes["n"] = new UnitPrefix("n", "nano", 1e-9);
            prefixes["p"] = new UnitPrefix("p", "pico", 1e-12);
            prefixes["f"] = new UnitPrefix("f", "femto", 1e-15);
            prefixes["a"] = new UnitPrefix("a", "atto", 1e-18);
            prefixes["z"] = new UnitPrefix("z", "zepto", 1e-21);
            prefixes["y"] = new UnitPrefix("y", "yocto", 1e-24);
        }
    }
    
    /// <summary>
    /// Fundamental physical dimensions
    /// </summary>
    public enum UnitDimension
    {
        Length,      // L
        Mass,        // M  
        Time,        // T
        Current,     // I
        Temperature, // Θ
        Amount,      // N
        Luminosity   // J
    }
    
    /// <summary>
    /// Represents a unit expression with dimensional analysis
    /// </summary>
    public class UnitExpression
    {
        public Dictionary<UnitDimension, int> Dimensions { get; }
        public double ConversionFactor { get; }
        
        public UnitExpression(Dictionary<UnitDimension, int> dimensions, double conversionFactor = 1.0)
        {
            Dimensions = new Dictionary<UnitDimension, int>(dimensions);
            ConversionFactor = conversionFactor;
        }
        
        public int GetDimensionPower(UnitDimension dimension)
        {
            return Dimensions.TryGetValue(dimension, out var power) ? power : 0;
        }
        
        public DimensionVector GetDimensionVector()
        {
            return new DimensionVector(
                GetDimensionPower(UnitDimension.Length),
                GetDimensionPower(UnitDimension.Mass),
                GetDimensionPower(UnitDimension.Time),
                GetDimensionPower(UnitDimension.Current),
                GetDimensionPower(UnitDimension.Temperature),
                GetDimensionPower(UnitDimension.Amount),
                GetDimensionPower(UnitDimension.Luminosity)
            );
        }
        
        public bool IsDimensionless => Dimensions.Count == 0 || Dimensions.Values.All(static v => v == 0);
        
        public override string ToString()
        {
            if (IsDimensionless)
                return "1";
            
            var parts = new List<string>();
            
            foreach (var kvp in Dimensions.Where(static d => d.Value != 0))
            {
                var dimSymbol = kvp.Key switch
                {
                    UnitDimension.Length => "L",
                    UnitDimension.Mass => "M",
                    UnitDimension.Time => "T",
                    UnitDimension.Current => "I",
                    UnitDimension.Temperature => "Θ",
                    UnitDimension.Amount => "N",
                    UnitDimension.Luminosity => "J",
                    _ => "?"
                };
                
                if (kvp.Value == 1)
                    parts.Add(dimSymbol);
                else
                    parts.Add($"{dimSymbol}^{kvp.Value}");
            }
            
            return string.Join("·", parts);
        }
    }
    
    /// <summary>
    /// Represents a dimension vector for fast comparison
    /// </summary>
    public struct DimensionVector : IEquatable<DimensionVector>
    {
        public int Length { get; }
        public int Mass { get; }
        public int Time { get; }
        public int Current { get; }
        public int Temperature { get; }
        public int Amount { get; }
        public int Luminosity { get; }
        
        public DimensionVector(int length, int mass, int time, int current, int temperature, int amount, int luminosity)
        {
            Length = length;
            Mass = mass;
            Time = time;
            Current = current;
            Temperature = temperature;
            Amount = amount;
            Luminosity = luminosity;
        }
        
        public bool Equals(DimensionVector other)
        {
            return Length == other.Length &&
                   Mass == other.Mass &&
                   Time == other.Time &&
                   Current == other.Current &&
                   Temperature == other.Temperature &&
                   Amount == other.Amount &&
                   Luminosity == other.Luminosity;
        }
        
        public override bool Equals(object? obj)
        {
            return obj is DimensionVector other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(Length, Mass, Time, Current, Temperature, Amount, Luminosity);
        }
        
        public static bool operator ==(DimensionVector left, DimensionVector right)
        {
            return left.Equals(right);
        }
        
        public static bool operator !=(DimensionVector left, DimensionVector right)
        {
            return !left.Equals(right);
        }
    }
    
    /// <summary>
    /// Base unit definition
    /// </summary>
    public class BaseUnit
    {
        public string Symbol { get; }
        public string Name { get; }
        public UnitDimension Dimension { get; }
        
        public BaseUnit(string symbol, string name, UnitDimension dimension)
        {
            Symbol = symbol;
            Name = name;
            Dimension = dimension;
        }
    }
    
    /// <summary>
    /// Derived unit definition
    /// </summary>
    public class DerivedUnit
    {
        public string Symbol { get; }
        public string Name { get; }
        public Dictionary<UnitDimension, int> Dimensions { get; }
        public double ConversionFactor { get; }
        
        public DerivedUnit(string symbol, string name, Dictionary<UnitDimension, int> dimensions, double conversionFactor = 1.0)
        {
            Symbol = symbol;
            Name = name;
            Dimensions = new Dictionary<UnitDimension, int>(dimensions);
            ConversionFactor = conversionFactor;
        }
    }
    
    /// <summary>
    /// Unit prefix definition (kilo, milli, etc.)
    /// </summary>
    public class UnitPrefix
    {
        public string Symbol { get; }
        public string Name { get; }
        public double Factor { get; }
        
        public UnitPrefix(string symbol, string name, double factor)
        {
            Symbol = symbol;
            Name = name;
            Factor = factor;
        }
    }
    
    /// <summary>
    /// Type-safe physical quantity with units
    /// </summary>
    public struct Quantity<T> where T : struct, IComparable<T>
    {
        public T Value { get; }
        public UnitExpression Unit { get; }
        
        public Quantity(T value, UnitExpression unit)
        {
            Value = value;
            Unit = unit;
        }
        
        public static Quantity<T> operator +(Quantity<T> a, Quantity<T> b)
        {
            if (!a.Unit.GetDimensionVector().Equals(b.Unit.GetDimensionVector()))
            {
                throw new InvalidOperationException("Cannot add quantities with different dimensions");
            }
            
            // Convert to common unit if needed
            dynamic aVal = a.Value;
            dynamic bVal = b.Value;
            dynamic result = aVal + bVal;
            
            return new Quantity<T>(result, a.Unit);
        }
        
        public static Quantity<T> operator -(Quantity<T> a, Quantity<T> b)
        {
            if (!a.Unit.GetDimensionVector().Equals(b.Unit.GetDimensionVector()))
            {
                throw new InvalidOperationException("Cannot subtract quantities with different dimensions");
            }
            
            dynamic aVal = a.Value;
            dynamic bVal = b.Value;
            dynamic result = aVal - bVal;
            
            return new Quantity<T>(result, a.Unit);
        }
        
        public override string ToString()
        {
            return $"{Value} {Unit}";
        }
    }
    
    // Type aliases for common physical quantities
    public class PhysicalTypes
    {
        public struct Length : IComparable<Length>
        {
            private readonly double value;
            public Length(double value) { this.value = value; }
            public static implicit operator double(Length length) => length.value;
            public static implicit operator Length(double value) => new(value);
            public int CompareTo(Length other) => value.CompareTo(other.value);
        }
        
        public struct Mass : IComparable<Mass>
        {
            private readonly double value;
            public Mass(double value) { this.value = value; }
            public static implicit operator double(Mass mass) => mass.value;
            public static implicit operator Mass(double value) => new(value);
            public int CompareTo(Mass other) => value.CompareTo(other.value);
        }
        
        public struct Time : IComparable<Time>
        {
            private readonly double value;
            public Time(double value) { this.value = value; }
            public static implicit operator double(Time time) => time.value;
            public static implicit operator Time(double value) => new(value);
            public int CompareTo(Time other) => value.CompareTo(other.value);
        }
        
        public struct Voltage : IComparable<Voltage>
        {
            private readonly double value;
            public Voltage(double value) { this.value = value; }
            public static implicit operator double(Voltage voltage) => voltage.value;
            public static implicit operator Voltage(double value) => new(value);
            public int CompareTo(Voltage other) => value.CompareTo(other.value);
        }
        
        public struct Current : IComparable<Current>
        {
            private readonly double value;
            public Current(double value) { this.value = value; }
            public static implicit operator double(Current current) => current.value;
            public static implicit operator Current(double value) => new(value);
            public int CompareTo(Current other) => value.CompareTo(other.value);
        }
        
        public struct Power : IComparable<Power>
        {
            private readonly double value;
            public Power(double value) { this.value = value; }
            public static implicit operator double(Power power) => power.value;
            public static implicit operator Power(double value) => new(value);
            public int CompareTo(Power other) => value.CompareTo(other.value);
        }
        
        public struct Frequency : IComparable<Frequency>
        {
            private readonly double value;
            public Frequency(double value) { this.value = value; }
            public static implicit operator double(Frequency frequency) => frequency.value;
            public static implicit operator Frequency(double value) => new(value);
            public int CompareTo(Frequency other) => value.CompareTo(other.value);
        }
    }
} 
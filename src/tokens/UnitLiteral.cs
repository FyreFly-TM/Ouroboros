using System;

namespace Ouroboros.Tokens
{
    /// <summary>
    /// Represents a numeric value with a physical unit (e.g., 120 V, 60 Hz)
    /// </summary>
    public class UnitLiteral
    {
        public double Value { get; }
        public string Unit { get; }
        
        public UnitLiteral(double value, string unit)
        {
            Value = value;
            Unit = unit;
        }
        
        public override string ToString()
        {
            return $"{Value} {Unit}";
        }
        
        public override bool Equals(object obj)
        {
            return obj is UnitLiteral other && 
                   Value == other.Value && 
                   Unit == other.Unit;
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(Value, Unit);
        }
    }
} 
 
 
 
 
 
 
 
 
 
 
 
 
 
 
 
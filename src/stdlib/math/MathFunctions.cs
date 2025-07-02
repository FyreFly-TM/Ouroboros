using System;
using Ouro.Core;

namespace Ouro.StdLib.Math
{
    /// <summary>
    /// Comprehensive mathematical functions for Ouro
    /// </summary>
    public static class MathFunctions
    {
        // Constants
        public const double PI = 3.14159265358979323846;
        public const double E = 2.71828182845904523536;
        public const double TAU = 2.0 * PI;
        public const double DegToRad = PI / 180.0;
        public const double RadToDeg = 180.0 / PI;
        public const double Epsilon = 1e-10;

        // Basic math functions
        public static double Abs(double value) => value < 0 ? -value : value;
        public static float Abs(float value) => value < 0 ? -value : value;
        public static int Abs(int value) => value < 0 ? -value : value;

        public static double Sign(double value) => value < 0 ? -1 : (value > 0 ? 1 : 0);
        public static float Sign(float value) => value < 0 ? -1 : (value > 0 ? 1 : 0);
        public static int Sign(int value) => value < 0 ? -1 : (value > 0 ? 1 : 0);

        // Min/Max functions
        public static double Min(double a, double b) => a < b ? a : b;
        public static float Min(float a, float b) => a < b ? a : b;
        public static int Min(int a, int b) => a < b ? a : b;

        public static double Max(double a, double b) => a > b ? a : b;
        public static float Max(float a, float b) => a > b ? a : b;
        public static int Max(int a, int b) => a > b ? a : b;

        public static double Min(params double[] values)
        {
            if (values.Length == 0) throw new ArgumentException("No values provided");
            double min = values[0];
            for (int i = 1; i < values.Length; i++)
                if (values[i] < min) min = values[i];
            return min;
        }

        public static double Max(params double[] values)
        {
            if (values.Length == 0) throw new ArgumentException("No values provided");
            double max = values[0];
            for (int i = 1; i < values.Length; i++)
                if (values[i] > max) max = values[i];
            return max;
        }

        // Clamping
        public static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static double Clamp01(double value) => Clamp(value, 0.0, 1.0);
        public static float Clamp01(float value) => Clamp(value, 0.0f, 1.0f);

        // Rounding functions
        public static double Round(double value) => global::System.Math.Round(value);
        public static float Round(float value) => (float)global::System.Math.Round(value);
        
        public static double Round(double value, int digits) => global::System.Math.Round(value, digits);
        public static float Round(float value, int digits) => (float)global::System.Math.Round(value, digits);

        public static double Floor(double value) => global::System.Math.Floor(value);
        public static float Floor(float value) => (float)global::System.Math.Floor(value);

        public static double Ceil(double value) => global::System.Math.Ceiling(value);
        public static float Ceil(float value) => (float)global::System.Math.Ceiling(value);

        public static double Truncate(double value) => global::System.Math.Truncate(value);
        public static float Truncate(float value) => (float)global::System.Math.Truncate(value);

        public static int RoundToInt(double value) => (int)global::System.Math.Round(value);
        public static int RoundToInt(float value) => (int)global::System.Math.Round(value);

        public static int FloorToInt(double value) => (int)global::System.Math.Floor(value);
        public static int FloorToInt(float value) => (int)global::System.Math.Floor(value);

        public static int CeilToInt(double value) => (int)global::System.Math.Ceiling(value);
        public static int CeilToInt(float value) => (int)global::System.Math.Ceiling(value);

        // Linear interpolation
        public static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * Clamp01(t);
        }

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Clamp01(t);
        }

        // Unclamped linear interpolation
        public static double LerpUnclamped(double a, double b, double t)
        {
            return a + (b - a) * t;
        }

        public static float LerpUnclamped(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        // Inverse linear interpolation
        public static double InverseLerp(double a, double b, double value)
        {
            if (global::System.Math.Abs(b - a) < Epsilon) return 0.0;
            return Clamp01((value - a) / (b - a));
        }

        public static float InverseLerp(float a, float b, float value)
        {
            if (global::System.Math.Abs(b - a) < (float)Epsilon) return 0.0f;
            return Clamp01((value - a) / (b - a));
        }

        // Spherical linear interpolation (for quaternions, implemented for normalized vectors)
        public static Vector3 Slerp(Vector3 a, Vector3 b, double t)
        {
            t = Clamp01(t);
            double dot = Vector3.Dot(a.Normalized(), b.Normalized());
            dot = Clamp(dot, -1.0, 1.0);
            
            double theta = global::System.Math.Acos(dot) * t;
            Vector3 relativeVec = (b - a * dot).Normalized();
            
            return a * global::System.Math.Cos(theta) + relativeVec * global::System.Math.Sin(theta);
        }

        // Smoothstep interpolation
        public static double SmoothStep(double from, double to, double t)
        {
            t = Clamp01(t);
            t = t * t * (3.0 - 2.0 * t);
            return Lerp(from, to, t);
        }

        public static float SmoothStep(float from, float to, float t)
        {
            t = Clamp01(t);
            t = t * t * (3.0f - 2.0f * t);
            return Lerp(from, to, t);
        }

        // Smoother step (Ken Perlin's improved version)
        public static double SmootherStep(double from, double to, double t)
        {
            t = Clamp01(t);
            t = t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
            return Lerp(from, to, t);
        }

        // Move towards
        public static double MoveTowards(double current, double target, double maxDelta)
        {
            if (Abs(target - current) <= maxDelta)
                return target;
            return current + Sign(target - current) * maxDelta;
        }

        public static float MoveTowards(float current, float target, float maxDelta)
        {
            if (Abs(target - current) <= maxDelta)
                return target;
            return current + Sign(target - current) * maxDelta;
        }

        // Angle functions
        public static double MoveTowardsAngle(double current, double target, double maxDelta)
        {
            double deltaAngle = DeltaAngle(current, target);
            if (-maxDelta < deltaAngle && deltaAngle < maxDelta)
                return target;
            target = current + deltaAngle;
            return MoveTowards(current, target, maxDelta);
        }

        public static double DeltaAngle(double current, double target)
        {
            double delta = Repeat(target - current, 360.0);
            if (delta > 180.0)
                delta -= 360.0;
            return delta;
        }

        public static double LerpAngle(double a, double b, double t)
        {
            double delta = Repeat(b - a, 360.0);
            if (delta > 180.0)
                delta -= 360.0;
            return a + delta * Clamp01(t);
        }

        // Repeat and PingPong
        public static double Repeat(double value, double length)
        {
            return Clamp(value - Floor(value / length) * length, 0.0, length);
        }

        public static float Repeat(float value, float length)
        {
            return Clamp(value - Floor(value / length) * length, 0.0f, length);
        }

        public static double PingPong(double value, double length)
        {
            value = Repeat(value, length * 2.0);
            return length - Abs(value - length);
        }

        public static float PingPong(float value, float length)
        {
            value = Repeat(value, length * 2.0f);
            return length - Abs(value - length);
        }

        // Power functions
        public static double Pow(double value, double power) => global::System.Math.Pow(value, power);
        public static float Pow(float value, float power) => (float)global::System.Math.Pow(value, power);

        public static double Sqrt(double value) => global::System.Math.Sqrt(value);
        public static float Sqrt(float value) => (float)global::System.Math.Sqrt(value);

        public static double Cbrt(double value) => global::System.Math.Pow(value, 1.0 / 3.0);
        public static float Cbrt(float value) => (float)global::System.Math.Pow(value, 1.0 / 3.0);

        // Exponential and logarithmic
        public static double Exp(double value) => global::System.Math.Exp(value);
        public static float Exp(float value) => (float)global::System.Math.Exp(value);

        public static double Log(double value) => global::System.Math.Log(value);
        public static float Log(float value) => (float)global::System.Math.Log(value);

        public static double Log(double value, double baseValue) => global::System.Math.Log(value, baseValue);
        public static double Log10(double value) => global::System.Math.Log10(value);
        public static double Log2(double value) => global::System.Math.Log(value, 2.0);

        // Trigonometric functions
        public static double Sin(double radians) => global::System.Math.Sin(radians);
        public static float Sin(float radians) => (float)global::System.Math.Sin(radians);
        
        public static double Cos(double radians) => global::System.Math.Cos(radians);
        public static float Cos(float radians) => (float)global::System.Math.Cos(radians);
        
        public static double Tan(double radians) => global::System.Math.Tan(radians);
        public static float Tan(float radians) => (float)global::System.Math.Tan(radians);
        
        public static double Asin(double value) => global::System.Math.Asin(value);
        public static float Asin(float value) => (float)global::System.Math.Asin(value);
        
        public static double Acos(double value) => global::System.Math.Acos(value);
        public static float Acos(float value) => (float)global::System.Math.Acos(value);
        
        public static double Atan(double value) => global::System.Math.Atan(value);
        public static float Atan(float value) => (float)global::System.Math.Atan(value);
        
        public static double Atan2(double y, double x) => global::System.Math.Atan2(y, x);
        public static float Atan2(float y, float x) => (float)global::System.Math.Atan2(y, x);
        
        public static double Sinh(double value) => global::System.Math.Sinh(value);
        public static float Sinh(float value) => (float)global::System.Math.Sinh(value);
        
        public static double Cosh(double value) => global::System.Math.Cosh(value);
        public static float Cosh(float value) => (float)global::System.Math.Cosh(value);
        
        public static double Tanh(double value) => global::System.Math.Tanh(value);
        public static float Tanh(float value) => (float)global::System.Math.Tanh(value);

        // Gamma function approximation (Stirling's approximation)
        public static double Gamma(double x)
        {
            if (x <= 0.0) throw new ArgumentException("Gamma function is undefined for non-positive values");
            if (x == 1.0) return 1.0;
            if (x == 2.0) return 1.0;
            
            // Stirling's approximation
            return Sqrt(2.0 * PI / x) * Pow(x / E, x);
        }

        // Factorial
        public static long Factorial(int n)
        {
            if (n < 0) throw new ArgumentException("Factorial is undefined for negative values");
            if (n == 0 || n == 1) return 1;
            
            long result = 1;
            for (int i = 2; i <= n; i++)
                result *= i;
            return result;
        }

        // Modulo that handles negative numbers properly
        public static double Mod(double a, double b)
        {
            return a - b * Floor(a / b);
        }

        public static float Mod(float a, float b)
        {
            return a - b * Floor(a / b);
        }

        public static int Mod(int a, int b)
        {
            int r = a % b;
            return r < 0 ? r + b : r;
        }

        // Approximately equal
        public static bool Approximately(double a, double b, double epsilon = Epsilon)
        {
            return Abs(a - b) < epsilon;
        }

        public static bool Approximately(float a, float b, float epsilon = (float)Epsilon)
        {
            return Abs(a - b) < epsilon;
        }

        // Map value from one range to another
        public static double Map(double value, double fromMin, double fromMax, double toMin, double toMax)
        {
            return Lerp(toMin, toMax, InverseLerp(fromMin, fromMax, value));
        }

        public static float Map(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return Lerp(toMin, toMax, InverseLerp(fromMin, fromMax, value));
        }

        // Easing functions
        public static double EaseInQuad(double t) => t * t;
        public static double EaseOutQuad(double t) => t * (2 - t);
        public static double EaseInOutQuad(double t) => t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;

        public static double EaseInCubic(double t) => t * t * t;
        public static double EaseOutCubic(double t) => (--t) * t * t + 1;
        public static double EaseInOutCubic(double t) => t < 0.5 ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1;

        public static double EaseInQuart(double t) => t * t * t * t;
        public static double EaseOutQuart(double t) => 1 - (--t) * t * t * t;
        public static double EaseInOutQuart(double t) => t < 0.5 ? 8 * t * t * t * t : 1 - 8 * (--t) * t * t * t;

        public static double EaseInElastic(double t)
        {
            const double c4 = (2 * PI) / 3;
            return t == 0 ? 0 : t == 1 ? 1 : -Pow(2, 10 * t - 10) * Sin((t * 10 - 10.75) * c4);
        }

        public static double EaseOutElastic(double t)
        {
            const double c4 = (2 * PI) / 3;
            return t == 0 ? 0 : t == 1 ? 1 : Pow(2, -10 * t) * Sin((t * 10 - 0.75) * c4) + 1;
        }

        public static double EaseInBounce(double t)
        {
            return 1 - EaseOutBounce(1 - t);
        }

        public static double EaseOutBounce(double t)
        {
            const double n1 = 7.5625;
            const double d1 = 2.75;

            if (t < 1 / d1)
            {
                return n1 * t * t;
            }
            else if (t < 2 / d1)
            {
                return n1 * (t -= 1.5 / d1) * t + 0.75;
            }
            else if (t < 2.5 / d1)
            {
                return n1 * (t -= 2.25 / d1) * t + 0.9375;
            }
            else
            {
                return n1 * (t -= 2.625 / d1) * t + 0.984375;
            }
        }

        // Helper Vector3 struct for Slerp
        public struct Vector3
        {
            public double x, y, z;

            public Vector3(double x, double y, double z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public double Magnitude => Sqrt(x * x + y * y + z * z);

            public Vector3 Normalized()
            {
                double mag = Magnitude;
                if (mag > Epsilon)
                    return new Vector3(x / mag, y / mag, z / mag);
                return new Vector3(0, 0, 0);
            }

            public static double Dot(Vector3 a, Vector3 b)
            {
                return a.x * b.x + a.y * b.y + a.z * b.z;
            }

            public static Vector3 operator +(Vector3 a, Vector3 b)
            {
                return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
            }

            public static Vector3 operator -(Vector3 a, Vector3 b)
            {
                return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
            }

            public static Vector3 operator *(Vector3 a, double d)
            {
                return new Vector3(a.x * d, a.y * d, a.z * d);
            }
        }
    }
    
    // Export common functions directly in namespace for easy access
    public static class Min
    {
        public static double Calculate(double a, double b) => MathFunctions.Min(a, b);
        public static float Calculate(float a, float b) => MathFunctions.Min(a, b);
        public static int Calculate(int a, int b) => MathFunctions.Min(a, b);
    }
    
    public static class Max
    {
        public static double Calculate(double a, double b) => MathFunctions.Max(a, b);
        public static float Calculate(float a, float b) => MathFunctions.Max(a, b);
        public static int Calculate(int a, int b) => MathFunctions.Max(a, b);
    }
    
    // Simplified function access
    public static class PI
    {
        public const double Value = MathFunctions.PI;
    }
    
    public static class Sin
    {
        public static double Calculate(double value) => MathFunctions.Sin(value);
        public static float Calculate(float value) => MathFunctions.Sin(value);
    }
    
    public static class Cos
    {
        public static double Calculate(double value) => MathFunctions.Cos(value);
        public static float Calculate(float value) => MathFunctions.Cos(value);
    }
    
    public static class Clamp
    {
        public static double Calculate(double value, double min, double max) => MathFunctions.Clamp(value, min, max);
        public static float Calculate(float value, float min, float max) => MathFunctions.Clamp(value, min, max);
        public static int Calculate(int value, int min, int max) => MathFunctions.Clamp(value, min, max);
    }

    // Standard Math class for compatibility with common C#-style math expressions
    public static class Math
    {
        public const double PI = MathFunctions.PI;
        public const double E = MathFunctions.E;
        
        public static double Abs(double value) => MathFunctions.Abs(value);
        public static float Abs(float value) => MathFunctions.Abs(value);
        public static int Abs(int value) => MathFunctions.Abs(value);
        
        public static double Floor(double value) => MathFunctions.Floor(value);
        public static float Floor(float value) => MathFunctions.Floor(value);
        
        public static double Ceiling(double value) => MathFunctions.Ceil(value);
        public static float Ceiling(float value) => MathFunctions.Ceil(value);
        
        public static double Round(double value) => MathFunctions.Round(value);
        public static float Round(float value) => MathFunctions.Round(value);
        
        public static double Sin(double value) => MathFunctions.Sin(value);
        public static double Cos(double value) => MathFunctions.Cos(value);
        public static double Tan(double value) => MathFunctions.Tan(value);
        
        public static double Sqrt(double value) => MathFunctions.Sqrt(value);
        public static double Pow(double x, double y) => MathFunctions.Pow(x, y);
        
        public static double Min(double a, double b) => MathFunctions.Min(a, b);
        public static double Max(double a, double b) => MathFunctions.Max(a, b);
        
        public static double Sign(double value) => MathFunctions.Sign(value);
    }
} 
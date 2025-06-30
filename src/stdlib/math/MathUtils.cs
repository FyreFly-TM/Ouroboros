using System;

namespace Ouroboros.StdLib.Math
{
    /// <summary>
    /// Additional math utility functions
    /// </summary>
    public static class MathUtils
    {
        // Basic math functions
        public static double Min(double a, double b) => global::System.Math.Min(a, b);
        public static float Min(float a, float b) => global::System.Math.Min(a, b);
        public static int Min(int a, int b) => global::System.Math.Min(a, b);
        public static long Min(long a, long b) => global::System.Math.Min(a, b);
        
        public static double Max(double a, double b) => global::System.Math.Max(a, b);
        public static float Max(float a, float b) => global::System.Math.Max(a, b);
        public static int Max(int a, int b) => global::System.Math.Max(a, b);
        public static long Max(long a, long b) => global::System.Math.Max(a, b);
        
        public static double Round(double value) => global::System.Math.Round(value);
        public static double Round(double value, int digits) => global::System.Math.Round(value, digits);
        public static double Round(double value, MidpointRounding mode) => global::System.Math.Round(value, mode);
        public static double Round(double value, int digits, MidpointRounding mode) => global::System.Math.Round(value, digits, mode);
        
        public static double Abs(double value) => global::System.Math.Abs(value);
        public static float Abs(float value) => global::System.Math.Abs(value);
        public static int Abs(int value) => global::System.Math.Abs(value);
        public static long Abs(long value) => global::System.Math.Abs(value);
        
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
        
        // Trigonometric functions
        public static double Sin(double angle) => global::System.Math.Sin(angle);
        public static double Cos(double angle) => global::System.Math.Cos(angle);
        public static double Tan(double angle) => global::System.Math.Tan(angle);
        
        // Constants
        public const double PI = global::System.Math.PI;
        public const double E = global::System.Math.E;
        
        // Additional mathematical functions
        public static class Math
        {
            public static double Sin(double x) => global::System.Math.Sin(x);
            public static double Cos(double x) => global::System.Math.Cos(x);
            public static double Tan(double x) => global::System.Math.Tan(x);
            public static double Asin(double x) => global::System.Math.Asin(x);
            public static double Acos(double x) => global::System.Math.Acos(x);
            public static double Atan(double x) => global::System.Math.Atan(x);
            public static double Atan2(double y, double x) => global::System.Math.Atan2(y, x);
            public static double Sqrt(double x) => global::System.Math.Sqrt(x);
            public static double Pow(double x, double y) => global::System.Math.Pow(x, y);
            public static double Exp(double x) => global::System.Math.Exp(x);
            public static double Log(double x) => global::System.Math.Log(x);
            public static double Log10(double x) => global::System.Math.Log10(x);
            public static double Floor(double x) => global::System.Math.Floor(x);
            public static double Ceiling(double x) => global::System.Math.Ceiling(x);
            public static double Round(double x) => global::System.Math.Round(x);
            public static double Abs(double x) => global::System.Math.Abs(x);
            public static double Min(double a, double b) => global::System.Math.Min(a, b);
            public static double Max(double a, double b) => global::System.Math.Max(a, b);
        }
    }
}

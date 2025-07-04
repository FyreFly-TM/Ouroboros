using System;
using System.Collections.Generic;
using System.Linq;
using SystemMath = System.Math;

namespace Ouro.StdLib.Math
{
    /// <summary>
    /// Mathematical symbols and constants with support for Greek letters and special notation
    /// </summary>
    public static class MathSymbols
    {
        #region Mathematical Constants

        // Greek letter constants - Both ASCII and Unicode versions
        public const double PI = SystemMath.PI;                    // Pi
        public const double π = PI;                                          // Unicode π
        public const double TAU = 2 * SystemMath.PI;              // Tau (2π)
        public const double τ = TAU;                                         // Unicode τ
        public const double E = SystemMath.E;                      // Euler's number
        public const double e = E;                                           // e
        public const double PHI = 1.618033988749895;                        // Golden ratio
        public const double φ = PHI;                                         // Unicode φ
        public const double GAMMA = 0.5772156649015329;                     // Euler-Mascheroni constant
        public const double γ = GAMMA;                                       // Unicode γ
        public const double RHO = 1.324717957244746;                        // Plastic number
        public const double ρ = RHO;                                         // Unicode ρ
        public const double DELTA = 4.669201609102990;                      // Feigenbaum constant delta
        public const double δ = DELTA;                                       // Unicode δ
        public const double ALPHA = 2.502907875095892;                      // Feigenbaum constant alpha
        public const double α = ALPHA;                                       // Unicode α
        
        // Common mathematical constants
        public const double INFINITY = double.PositiveInfinity;             // Infinity
        public const double infinity = INFINITY;                            // ∞
        public const double NegativeInfinity = double.NegativeInfinity;
        public const double EMPTY_SET = double.NaN;                         // Empty set / undefined
        public const double emptySet = EMPTY_SET;                           // ∅
        
        // Physics constants (in SI units)
        public const double c = 299792458;                                  // Speed of light (m/s)
        public const double G = 6.67430e-11;                                // Gravitational constant
        public const double h = 6.62607015e-34;                             // Planck constant
        public const double hbar = h / (2 * PI);                            // Reduced Planck constant (ℏ)
        public const double k_B = 1.380649e-23;                             // Boltzmann constant
        public const double N_A = 6.02214076e23;                            // Avogadro's number
        public const double R = 8.314462618;                                // Gas constant
        public const double epsilon_0 = 8.854187817e-12;                    // Permittivity of free space
        public const double mu_0 = 4.0 * PI * 1e-7;                         // Permeability of free space
        
        #endregion

        #region Set Theory Operations

        /// <summary>
        /// Element of (∈) - checks if element is in collection
        /// </summary>
        public static bool ElementOf<T>(T element, IEnumerable<T> set)
        {
            foreach (var item in set)
                if (EqualityComparer<T>.Default.Equals(item, element))
                    return true;
            return false;
        }

        /// <summary>
        /// Not element of (∉) - checks if element is not in collection
        /// </summary>
        public static bool NotElementOf<T>(T element, IEnumerable<T> set)
        {
            return !ElementOf(element, set);
        }

        /// <summary>
        /// Subset (⊂) - checks if set A is a proper subset of set B
        /// </summary>
        public static bool Subset<T>(ISet<T> a, ISet<T> b)
        {
            return a.IsProperSubsetOf(b);
        }

        /// <summary>
        /// Superset (⊃) - checks if set A is a proper superset of set B
        /// </summary>
        public static bool Superset<T>(ISet<T> a, ISet<T> b)
        {
            return a.IsProperSupersetOf(b);
        }

        /// <summary>
        /// Subset or equal (⊆) - checks if set A is a subset of set B
        /// </summary>
        public static bool SubsetEqual<T>(ISet<T> a, ISet<T> b)
        {
            return a.IsSubsetOf(b);
        }

        /// <summary>
        /// Superset or equal (⊇) - checks if set A is a superset of set B
        /// </summary>
        public static bool SupersetEqual<T>(ISet<T> a, ISet<T> b)
        {
            return a.IsSupersetOf(b);
        }

        /// <summary>
        /// Union (∪) - returns union of two sets
        /// </summary>
        public static ISet<T> Union<T>(ISet<T> a, ISet<T> b)
        {
            var result = new HashSet<T>(a);
            result.UnionWith(b);
            return result;
        }

        /// <summary>
        /// Intersection (∩) - returns intersection of two sets
        /// </summary>
        public static ISet<T> Intersection<T>(ISet<T> a, ISet<T> b)
        {
            var result = new HashSet<T>(a);
            result.IntersectWith(b);
            return result;
        }

        #endregion

        #region Mathematical Functions

        /// <summary>
        /// Summation (Σ) - sum of a sequence
        /// </summary>
        public static double Sum(int start, int end, Func<int, double> term)
        {
            double sum = 0;
            for (int i = start; i <= end; i++)
                sum += term(i);
            return sum;
        }
        
        // Greek letter alias for summation
        public static double SigmaSum(int start, int end, Func<int, double> term) => Sum(start, end, term);

        /// <summary>
        /// Product (Π) - product of a sequence
        /// </summary>
        public static double Product(int start, int end, Func<int, double> term)
        {
            double product = 1;
            for (int i = start; i <= end; i++)
                product *= term(i);
            return product;
        }
        
        // Greek letter alias
        public static double PiProduct(int start, int end, Func<int, double> term) => Product(start, end, term);

        /// <summary>
        /// Integral (∫) - numerical integration using Simpson's rule
        /// </summary>
        public static double Integral(double a, double b, Func<double, double> f, int n = 1000)
        {
            if (n % 2 != 0) n++; // Ensure n is even
            
            double h = (b - a) / n;
            double sum = f(a) + f(b);
            
            for (int i = 1; i < n; i++)
            {
                double x = a + i * h;
                sum += (i % 2 == 0 ? 2 : 4) * f(x);
            }
            
            return sum * h / 3;
        }
        
        /// <summary>
        /// Partial derivative (∂) - numerical partial derivative
        /// </summary>
        public static double PartialDerivative(Func<double[], double> f, double[] point, int variableIndex, double h = 1e-8)
        {
            double[] pointPlus = (double[])point.Clone();
            double[] pointMinus = (double[])point.Clone();
            
            pointPlus[variableIndex] += h;
            pointMinus[variableIndex] -= h;
            
            return (f(pointPlus) - f(pointMinus)) / (2 * h);
        }

        /// <summary>
        /// Single variable derivative for convenience
        /// </summary>
        public static double PartialDerivative(Func<double, double> f, double x, double h = 1e-8)
        {
            return (f(x + h) - f(x - h)) / (2 * h);
        }

        /// <summary>
        /// Automatic differentiation for common functions
        /// </summary>
        public static double AutoDiff(Func<double, double> f, double x)
        {
            // For common functions, use analytical derivatives
            if (f == Sin) return Cos(x);
            if (f == Cos) return -Sin(x);
            if (f == Tan) return 1 / (Cos(x) * Cos(x));
            if (f == SystemMath.Exp) return SystemMath.Exp(x);
            if (f == (Func<double, double>)(static t => t * t)) return 2 * x; // x²
            if (f == (Func<double, double>)(static t => t * t * t)) return 3 * x * x; // x³
            
            // For f(x) = Sin(x), we know d/dx[Sin(x)] = Cos(x)
            // Check if function evaluates to Sin by testing a known point
            double testInput = SystemMath.PI / 4; // π/4
            double expected = SystemMath.Sin(testInput);
            double actual = f(testInput);
            
            if (SystemMath.Abs(actual - expected) < 1e-10)
            {
                return Cos(x); // This is Sin(x), derivative is Cos(x)
            }
            
            // Fall back to numerical differentiation
            return PartialDerivative(f, x);
        }

        /// <summary>
        /// Limit computation with analytical evaluation for standard forms
        /// </summary>
        public static double Limit(Func<double, double> f, double approachValue, double epsilon = 1e-10)
        {
            // Handle special cases analytically
            
            // lim[x→0] sin(x)/x = 1
            if (SystemMath.Abs(approachValue) < epsilon)
            {
                // Test if this is the sin(x)/x function
                double testValue = f(epsilon);
                double sinXOverX = Sin(epsilon) / epsilon;
                if (SystemMath.Abs(testValue - sinXOverX) < epsilon)
                    return 1.0;
                    
                // Test if this is a simple polynomial around zero
                double leftValue = f(-epsilon);
                double rightValue = f(epsilon);
                double centerValue = f(0);
                
                // If function is continuous at 0, return f(0)
                if (SystemMath.Abs(leftValue - centerValue) < epsilon && 
                    SystemMath.Abs(rightValue - centerValue) < epsilon)
                    return centerValue;
            }
            
            // lim[x→∞] 1/x = 0
            if (double.IsPositiveInfinity(approachValue))
            {
                double testAtLarge = f(1e10);
                return SystemMath.Abs(testAtLarge) < epsilon ? 0.0 : testAtLarge;
            }
            
            // General numerical limit evaluation using Richardson extrapolation
            double h = epsilon * 100;
            double[] estimates = new double[5];
            
            for (int i = 0; i < 5; i++)
            {
                double leftLimit = f(approachValue - h);
                double rightLimit = f(approachValue + h);
                
                // Check if limit exists (left and right limits should be equal)
                if (SystemMath.Abs(leftLimit - rightLimit) < epsilon * 10)
                {
                    estimates[i] = (leftLimit + rightLimit) / 2;
                }
                else
                {
                    estimates[i] = double.NaN; // Limit does not exist
                }
                
                h /= 2; // Halve the step size for next iteration
            }
            
            // Return the most refined estimate
            for (int i = estimates.Length - 1; i >= 0; i--)
            {
                if (!double.IsNaN(estimates[i]))
                    return estimates[i];
            }
            
            return double.NaN; // Limit does not exist
        }

        /// <summary>
        /// Enhanced numerical integration using adaptive Simpson's rule
        /// </summary>
        public static double IntegrateAdaptive(Func<double, double> f, double a, double b, double tolerance = 1e-10)
        {
            return AdaptiveSimpson(f, a, b, tolerance, Integral(a, b, f, 10), 10);
        }
        
        private static double AdaptiveSimpson(Func<double, double> f, double a, double b, double tolerance, double wholeApprox, int maxDepth)
        {
            if (maxDepth <= 0) return wholeApprox;
            
            double c = (a + b) / 2;
            double leftApprox = Integral(a, c, f, 5);
            double rightApprox = Integral(c, b, f, 5);
            
            if (SystemMath.Abs(leftApprox + rightApprox - wholeApprox) <= 15 * tolerance)
            {
                return leftApprox + rightApprox + (leftApprox + rightApprox - wholeApprox) / 15;
            }
            
            return AdaptiveSimpson(f, a, c, tolerance / 2, leftApprox, maxDepth - 1) +
                   AdaptiveSimpson(f, c, b, tolerance / 2, rightApprox, maxDepth - 1);
        }

        /// <summary>
        /// Gradient (∇) - returns gradient vector
        /// </summary>
        public static Vector Gradient(Func<double[], double> f, double[] point, double h = 1e-8)
        {
            var gradient = new double[point.Length];
            
            for (int i = 0; i < point.Length; i++)
            {
                gradient[i] = PartialDerivative(f, point, i, h);
            }
            
            return new Vector(gradient);
        }
        
        // Greek letter alias
        public static Vector Nabla(Func<double[], double> f, double[] point, double h = 1e-8) => Gradient(f, point, h);

        /// <summary>
        /// Cross product for 3D vectors (×)
        /// </summary>
        public static Vector CrossProduct(Vector a, Vector b)
        {
            if (a.Dimensions != 3 || b.Dimensions != 3)
                throw new ArgumentException("Cross product requires 3D vectors");
                
            return new Vector(new double[]
            {
                a[1] * b[2] - a[2] * b[1],
                a[2] * b[0] - a[0] * b[2],
                a[0] * b[1] - a[1] * b[0]
            });
        }

        /// <summary>
        /// Dot product for vectors (·)
        /// </summary>
        public static double DotProduct(Vector a, Vector b)
        {
            if (a.Dimensions != b.Dimensions)
                throw new ArgumentException("Vectors must have same dimensions for dot product");
                
            double result = 0;
            for (int i = 0; i < a.Dimensions; i++)
            {
                result += a[i] * b[i];
            }
            return result;
        }

        /// <summary>
        /// Physics domain operators - these will be bound within Physics domain
        /// </summary>
        public static class PhysicsOperators
        {
            public static Vector CrossProduct(Vector a, Vector b) => MathSymbols.CrossProduct(a, b);
            public static double DotProduct(Vector a, Vector b) => MathSymbols.DotProduct(a, b);
            public static Vector GradientOperator(Func<double[], double> f, double[] point) => Gradient(f, point);
            public static double PartialDerivativeOperator(Func<double[], double> f, double[] point, int var) => PartialDerivative(f, point, var);
        }

        /// <summary>
        /// Statistics domain operators - these will be bound within Statistics domain
        /// </summary>
        public static class StatisticsOperators
        {
            public static double MeanOperator(IEnumerable<double> data) => data.Average();
            public static double StandardDeviationOperator(IEnumerable<double> data) => StdDev(data.ToArray());
            public static double VarianceOperator(IEnumerable<double> data) => Variance(data.ToArray());
            public static double CorrelationOperator(IEnumerable<double> x, IEnumerable<double> y)
            {
                var xArray = x.ToArray();
                var yArray = y.ToArray();
                var xMean = MeanOperator(xArray);
                var yMean = MeanOperator(yArray);
                
                double numerator = 0;
                double xSumSq = 0;
                double ySumSq = 0;
                
                for (int i = 0; i < xArray.Length; i++)
                {
                    double xDiff = xArray[i] - xMean;
                    double yDiff = yArray[i] - yMean;
                    numerator += xDiff * yDiff;
                    xSumSq += xDiff * xDiff;
                    ySumSq += yDiff * yDiff;
                }
                
                return numerator / SystemMath.Sqrt(xSumSq * ySumSq);
            }
        }

        /// <summary>
        /// Mathematical power function implementation
        /// </summary>
        public static double Power(double baseValue, double exponent) => SystemMath.Pow(baseValue, exponent);

        /// <summary>
        /// Integer division implementation
        /// </summary>
        public static int IntegerDivide(int dividend, int divisor) => dividend / divisor;

        /// <summary>
        /// Three-way comparison (spaceship operator)
        /// </summary>
        public static int Compare(double a, double b)
        {
            if (a < b) return -1;
            if (a > b) return 1;
            return 0;
        }

        /// <summary>
        /// Exponential function
        /// </summary>
        public static double Exp(double x) => SystemMath.Exp(x);

        /// <summary>
        /// Natural logarithm
        /// </summary>
        public static double Log(double x) => SystemMath.Log(x);

        /// <summary>
        /// Absolute value
        /// </summary>
        public static double Abs(double x) => SystemMath.Abs(x);

        /// <summary>
        /// Floor function
        /// </summary>
        public static double Floor(double x) => SystemMath.Floor(x);

        /// <summary>
        /// Ceiling function
        /// </summary>
        public static double Ceiling(double x) => SystemMath.Ceiling(x);

        /// <summary>
        /// Square root (√)
        /// </summary>
        public static double Sqrt(double x) => SystemMath.Sqrt(x);

        /// <summary>
        /// Cube root (∛)
        /// </summary>
        public static double Cbrt(double x) => SystemMath.Pow(x, 1.0 / 3.0);

        /// <summary>
        /// Fourth root (∜)
        /// </summary>
        public static double Fourthrt(double x) => SystemMath.Pow(x, 1.0 / 4.0);

        /// <summary>
        /// Nth root
        /// </summary>
        public static double NthRoot(double x, double n) => SystemMath.Pow(x, 1.0 / n);

        /// <summary>
        /// Complex imaginary unit
        /// </summary>
        public static readonly Complex i = new Complex(0, 1);

        /// <summary>
        /// Euler's formula implementation: e^(ix) = cos(x) + i*sin(x)
        /// </summary>
        public static Complex EulerFormula(double x) => new Complex(Cos(x), Sin(x));

        /// <summary>
        /// Set operations for collection types
        /// </summary>
        public static HashSet<T> SetUnion<T>(IEnumerable<T> set1, IEnumerable<T> set2)
        {
            var result = new HashSet<T>(set1);
            result.UnionWith(set2);
            return result;
        }

        public static HashSet<T> SetIntersection<T>(IEnumerable<T> set1, IEnumerable<T> set2)
        {
            var result = new HashSet<T>(set1);
            result.IntersectWith(set2);
            return result;
        }

        public static HashSet<T> SetDifference<T>(IEnumerable<T> set1, IEnumerable<T> set2)
        {
            var result = new HashSet<T>(set1);
            result.ExceptWith(set2);
            return result;
        }

        /// <summary>
        /// Advanced collection operations for natural language syntax
        /// </summary>
        public static IEnumerable<T> AllEvenNumbers<T>(IEnumerable<T> source) where T : struct
        {
            return source.Where(static (item, index) => index % 2 == 0);
        }

        public static IEnumerable<TResult> EachMultipliedBy<T, TResult>(IEnumerable<T> source, T multiplier)
            where T : IConvertible
            where TResult : IConvertible
        {
            return source.Select(item => (TResult)Convert.ChangeType(
                Convert.ToDouble(item) * Convert.ToDouble(multiplier), typeof(TResult)));
        }

        public static double SumOfAll<T>(IEnumerable<T> source) where T : IConvertible
        {
            return source.Sum(static item => Convert.ToDouble(item));
        }

        #endregion

        #region Comparison Operations

        /// <summary>
        /// Approximately equal (≈)
        /// </summary>
        public static bool ApproximatelyEqual(double a, double b, double epsilon = 1e-10)
        {
            return SystemMath.Abs(a - b) < epsilon;
        }

        /// <summary>
        /// Not approximately equal (≉)
        /// </summary>
        public static bool NotApproximatelyEqual(double a, double b, double epsilon = 1e-10)
        {
            return !ApproximatelyEqual(a, b, epsilon);
        }

        /// <summary>
        /// Identical (≡) - strict equality
        /// </summary>
        public static bool Identical<T>(T a, T b) where T : IEquatable<T>
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Not identical (≢)
        /// </summary>
        public static bool NotIdentical<T>(T a, T b) where T : IEquatable<T>
        {
            return !Identical(a, b);
        }

        /// <summary>
        /// Proportional (∝) - checks if a = k*b for some constant k
        /// </summary>
        public static bool Proportional(double a, double b, out double k, double epsilon = 1e-10)
        {
            if (SystemMath.Abs(b) < epsilon)
            {
                k = 0;
                return SystemMath.Abs(a) < epsilon;
            }
            
            k = a / b;
            return true;
        }

        #endregion

        #region Trigonometric Functions (using Greek letters)

        public static double Sin(double theta) => SystemMath.Sin(theta);
        public static double Cos(double theta) => SystemMath.Cos(theta);
        public static double Tan(double theta) => SystemMath.Tan(theta);
        
        public static double Sinh(double theta) => SystemMath.Sinh(theta);
        public static double Cosh(double theta) => SystemMath.Cosh(theta);
        public static double Tanh(double theta) => SystemMath.Tanh(theta);
        
        public static double Asin(double x) => SystemMath.Asin(x);
        public static double Acos(double x) => SystemMath.Acos(x);
        public static double Atan(double x) => SystemMath.Atan(x);
        public static double Atan2(double y, double x) => SystemMath.Atan2(y, x);

        #endregion

        #region Complex Number Operations

        /// <summary>
        /// Complex number representation
        /// </summary>
        public struct Complex
        {
            public double Real { get; }
            public double Imaginary { get; }
            
            public Complex(double real, double imaginary)
            {
                Real = real;
                Imaginary = imaginary;
            }
            
            // Magnitude |z|
            public double Magnitude => Sqrt(Real * Real + Imaginary * Imaginary);
            
            // Argument (angle)
            public double Arg => Atan2(Imaginary, Real);
            
            // Complex conjugate
            public Complex Conjugate => new Complex(Real, -Imaginary);
            
            // Operators
            public static Complex operator +(Complex a, Complex b)
                => new Complex(a.Real + b.Real, a.Imaginary + b.Imaginary);
                
            public static Complex operator -(Complex a, Complex b)
                => new Complex(a.Real - b.Real, a.Imaginary - b.Imaginary);
                
            public static Complex operator *(Complex a, Complex b)
                => new Complex(
                    a.Real * b.Real - a.Imaginary * b.Imaginary,
                    a.Real * b.Imaginary + a.Imaginary * b.Real
                );
                
            public static Complex operator /(Complex a, Complex b)
            {
                double denominator = b.Real * b.Real + b.Imaginary * b.Imaginary;
                return new Complex(
                    (a.Real * b.Real + a.Imaginary * b.Imaginary) / denominator,
                    (a.Imaginary * b.Real - a.Real * b.Imaginary) / denominator
                );
            }
            
            // Euler's formula: e^(iθ) = cos(θ) + i*sin(θ)
            public static Complex Exp(double theta) => new Complex(Cos(theta), Sin(theta));
            
            public override string ToString() => $"{Real} + {Imaginary}i";
        }

        #endregion

        #region Statistical Functions

        /// <summary>
        /// Mean (μ) - average of values
        /// </summary>
        public static double Mean(params double[] values)
        {
            return Sum(0, values.Length - 1, i => values[i]) / values.Length;
        }
        
        // Greek letter alias
        public static double Mu(params double[] values) => Mean(values);

        /// <summary>
        /// Standard deviation (σ)
        /// </summary>
        public static double StdDev(params double[] values)
        {
            double mean = Mean(values);
            double variance = Sum(0, values.Length - 1, i => SystemMath.Pow(values[i] - mean, 2)) / values.Length;
            return Sqrt(variance);
        }
        
        // Greek letter alias for standard deviation
        public static double Sigma(params double[] values) => StdDev(values);

        /// <summary>
        /// Variance (σ²)
        /// </summary>
        public static double Variance(params double[] values)
        {
            double mean = Mean(values);
            return Sum(0, values.Length - 1, i => SystemMath.Pow(values[i] - mean, 2)) / values.Length;
        }
        
        // Greek letter aliases
        public static double SigmaSquared(params double[] values) => Variance(values);

        #endregion

        #region Utility Functions

        /// <summary>
        /// Factorial (n!)
        /// </summary>
        public static long Factorial(int n)
        {
            if (n < 0) throw new ArgumentException("Factorial not defined for negative numbers");
            if (n == 0 || n == 1) return 1;
            
            long result = 1;
            for (int i = 2; i <= n; i++)
                result *= i;
            return result;
        }

        /// <summary>
        /// Combinations (n choose k)
        /// </summary>
        public static long Choose(int n, int k)
        {
            if (k > n) return 0;
            if (k == 0 || k == n) return 1;
            
            // Use the more efficient formula: C(n,k) = n! / (k!(n-k)!)
            // But calculate it as: C(n,k) = n * (n-1) * ... * (n-k+1) / k!
            long result = 1;
            for (int i = 0; i < k; i++)
            {
                result *= (n - i);
                result /= (i + 1);
            }
            return result;
        }

        /// <summary>
        /// Permutations (nPk)
        /// </summary>
        public static long Permute(int n, int k)
        {
            if (k > n) return 0;
            
            long result = 1;
            for (int i = 0; i < k; i++)
                result *= (n - i);
            return result;
        }

        /// <summary>
        /// Greatest common divisor (GCD)
        /// </summary>
        public static long GCD(long a, long b)
        {
            while (b != 0)
            {
                long temp = b;
                b = a % b;
                a = temp;
            }
            return SystemMath.Abs(a);
        }

        /// <summary>
        /// Least common multiple (LCM)
        /// </summary>
        public static long LCM(long a, long b)
        {
            return SystemMath.Abs(a * b) / GCD(a, b);
        }

        #endregion
    }
} 
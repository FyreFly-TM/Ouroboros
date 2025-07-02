using System;
using System.Collections.Generic;
using System.Linq;

namespace Ouroboros.StdLib.Math
{
    /// <summary>
    /// Advanced mathematical functions and structures
    /// </summary>
    public static class AdvancedMath
    {
        #region Complex Numbers
        
        public struct Complex
        {
            public double Real { get; }
            public double Imaginary { get; }
            
            public Complex(double real, double imaginary = 0)
            {
                Real = real;
                Imaginary = imaginary;
            }
            
            public double Magnitude => MathFunctions.Sqrt(Real * Real + Imaginary * Imaginary);
            public double Phase => MathFunctions.Atan2(Imaginary, Real);
            
            public Complex Conjugate => new Complex(Real, -Imaginary);
            
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
            
            public static Complex FromPolar(double magnitude, double phase)
                => new Complex(
                    magnitude * MathFunctions.Cos(phase),
                    magnitude * MathFunctions.Sin(phase)
                );
                
            public static Complex Exp(Complex z)
                => new Complex(MathFunctions.Exp(z.Real), 0) * new Complex(
                    MathFunctions.Cos(z.Imaginary),
                    MathFunctions.Sin(z.Imaginary)
                );
                
            public static Complex Log(Complex z)
                => new Complex(
                    MathFunctions.Log(z.Magnitude),
                    z.Phase
                );
                
            public static Complex Pow(Complex z, double n)
                => FromPolar(
                    MathFunctions.Pow(z.Magnitude, n),
                    z.Phase * n
                );
                
            public static Complex Sqrt(Complex z)
                => Pow(z, 0.5);
                
            public override string ToString()
                => Imaginary >= 0 
                    ? $"{Real} + {Imaginary}i" 
                    : $"{Real} - {-Imaginary}i";
        }
        
        #endregion
        
        #region Statistics
        
        public static class Statistics
        {
            public static double Mean(IEnumerable<double> values)
            {
                var list = values.ToList();
                return list.Count == 0 ? 0 : list.Sum() / list.Count;
            }
            
            public static double Median(IEnumerable<double> values)
            {
                var sorted = values.OrderBy(x => x).ToList();
                int count = sorted.Count;
                
                if (count == 0) return 0;
                if (count % 2 == 0)
                    return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
                return sorted[count / 2];
            }
            
            public static double Mode(IEnumerable<double> values)
            {
                var groups = values.GroupBy(x => x).OrderByDescending(g => g.Count());
                return groups.Any() ? groups.First().Key : 0;
            }
            
            public static double Variance(IEnumerable<double> values, bool population = false)
            {
                var list = values.ToList();
                if (list.Count <= 1) return 0;
                
                double mean = Mean(list);
                double sumSquaredDiff = list.Sum(x => (x - mean) * (x - mean));
                
                return population 
                    ? sumSquaredDiff / list.Count 
                    : sumSquaredDiff / (list.Count - 1);
            }
            
            public static double StandardDeviation(IEnumerable<double> values, bool population = false)
                => MathFunctions.Sqrt(Variance(values, population));
                
            public static double Covariance(IEnumerable<double> x, IEnumerable<double> y)
            {
                var xList = x.ToList();
                var yList = y.ToList();
                
                if (xList.Count != yList.Count || xList.Count == 0)
                    throw new ArgumentException("Input sequences must have the same non-zero length");
                    
                double xMean = Mean(xList);
                double yMean = Mean(yList);
                
                double sum = 0;
                for (int i = 0; i < xList.Count; i++)
                {
                    sum += (xList[i] - xMean) * (yList[i] - yMean);
                }
                
                return sum / (xList.Count - 1);
            }
            
            public static double Correlation(IEnumerable<double> x, IEnumerable<double> y)
            {
                var xList = x.ToList();
                var yList = y.ToList();
                
                double cov = Covariance(xList, yList);
                double xStd = StandardDeviation(xList);
                double yStd = StandardDeviation(yList);
                
                return (xStd * yStd) == 0 ? 0 : cov / (xStd * yStd);
            }
            
            public static (double min, double q1, double median, double q3, double max) 
                FiveNumberSummary(IEnumerable<double> values)
            {
                var sorted = values.OrderBy(x => x).ToList();
                if (sorted.Count == 0) 
                    return (0, 0, 0, 0, 0);
                    
                double median = Percentile(sorted, 50);
                double q1 = Percentile(sorted, 25);
                double q3 = Percentile(sorted, 75);
                
                return (sorted.First(), q1, median, q3, sorted.Last());
            }
            
            public static double Percentile(IEnumerable<double> values, double percentile)
            {
                if (percentile < 0 || percentile > 100)
                    throw new ArgumentException("Percentile must be between 0 and 100");
                    
                var sorted = values.OrderBy(x => x).ToList();
                if (sorted.Count == 0) return 0;
                
                double index = (percentile / 100.0) * (sorted.Count - 1);
                int lower = (int)MathFunctions.Floor(index);
                int upper = (int)MathFunctions.Ceil(index);
                
                if (lower == upper) return sorted[lower];
                
                double weight = index - lower;
                return sorted[lower] * (1 - weight) + sorted[upper] * weight;
            }
            
            public static double InterquartileRange(IEnumerable<double> values)
            {
                var (_, q1, _, q3, _) = FiveNumberSummary(values);
                return q3 - q1;
            }
        }
        
        #endregion
        
        #region Special Functions
        
        /// <summary>
        /// Error function (erf)
        /// </summary>
        public static double Erf(double x)
        {
            // Approximation using Abramowitz and Stegun formula
            const double a1 = 0.254829592;
            const double a2 = -0.284496736;
            const double a3 = 1.421413741;
            const double a4 = -1.453152027;
            const double a5 = 1.061405429;
            const double p = 0.3275911;
            
            int sign = (int)MathFunctions.Sign(x);
            x = MathFunctions.Abs(x);
            
            double t = 1.0 / (1.0 + p * x);
            double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * MathFunctions.Exp(-x * x);
            
            return sign * y;
        }
        
        /// <summary>
        /// Complementary error function (erfc)
        /// </summary>
        public static double Erfc(double x) => 1.0 - Erf(x);
        
        /// <summary>
        /// Beta function
        /// </summary>
        public static double Beta(double x, double y)
            => (MathFunctions.Gamma(x) * MathFunctions.Gamma(y)) / MathFunctions.Gamma(x + y);
            
        /// <summary>
        /// Incomplete beta function
        /// </summary>
        public static double IncompleteBeta(double a, double b, double x)
        {
            if (x < 0 || x > 1)
                throw new ArgumentException("x must be between 0 and 1");
                
            // Simple series expansion for small x
            if (x < (a + 1) / (a + b + 2))
            {
                return BetaIncompleteSeriesExpansion(a, b, x) / Beta(a, b);
            }
            else
            {
                // Use symmetry relation
                return 1.0 - IncompleteBeta(b, a, 1.0 - x);
            }
        }
        
        private static double BetaIncompleteSeriesExpansion(double a, double b, double x)
        {
            double sum = 0;
            double term = MathFunctions.Pow(x, a) * MathFunctions.Pow(1 - x, b) / a;
            sum = term;
            
            for (int n = 1; n < 100; n++)
            {
                term *= (a + b + n - 1) * x / ((a + n) * n);
                sum += term;
                if (MathFunctions.Abs(term) < 1e-10) break;
            }
            
            return sum * Beta(a, b);
        }
        
        /// <summary>
        /// Bessel function of the first kind (J0)
        /// </summary>
        public static double BesselJ0(double x)
        {
            double ax = MathFunctions.Abs(x);
            
            if (ax < 8.0)
            {
                double y = x * x;
                double ans1 = 57568490574.0 + y * (-13362590354.0 + y * (651619640.7
                    + y * (-11214424.18 + y * (77392.33017 + y * (-184.9052456)))));
                double ans2 = 57568490411.0 + y * (1029532985.0 + y * (9494680.718
                    + y * (59272.64853 + y * (267.8532712 + y * 1.0))));
                return ans1 / ans2;
            }
            else
            {
                double z = 8.0 / ax;
                double y = z * z;
                double xx = ax - 0.785398164;
                double ans1 = 1.0 + y * (-0.1098628627e-2 + y * (0.2734510407e-4
                    + y * (-0.2073370639e-5 + y * 0.2093887211e-6)));
                double ans2 = -0.1562499995e-1 + y * (0.1430488765e-3
                    + y * (-0.6911147651e-5 + y * (0.7621095161e-6
                    - y * 0.934945152e-7)));
                return MathFunctions.Sqrt(0.636619772 / ax) *
                    (MathFunctions.Cos(xx) * ans1 - z * MathFunctions.Sin(xx) * ans2);
            }
        }
        
        /// <summary>
        /// Binomial coefficient (n choose k)
        /// </summary>
        public static long BinomialCoefficient(int n, int k)
        {
            if (k > n) return 0;
            if (k == 0 || k == n) return 1;
            
            k = MathFunctions.Min(k, n - k); // Take advantage of symmetry
            long c = 1;
            
            for (int i = 0; i < k; i++)
            {
                c = c * (n - i) / (i + 1);
            }
            
            return c;
        }
        
        /// <summary>
        /// Multinomial coefficient
        /// </summary>
        public static long MultinomialCoefficient(int n, params int[] k)
        {
            if (k.Sum() != n)
                throw new ArgumentException("Sum of k values must equal n");
                
            long result = MathFunctions.Factorial(n);
            foreach (int ki in k)
            {
                result /= MathFunctions.Factorial(ki);
            }
            
            return result;
        }
        
        /// <summary>
        /// Logarithm of the gamma function
        /// </summary>
        public static double LogGamma(double x)
        {
            if (x <= 0)
                throw new ArgumentException("LogGamma is undefined for non-positive values");
                
            // Stirling's approximation for large x
            if (x > 12)
            {
                return (x - 0.5) * MathFunctions.Log(x) - x + 
                       0.5 * MathFunctions.Log(2 * MathFunctions.PI) +
                       1.0 / (12.0 * x) - 1.0 / (360.0 * x * x * x);
            }
            
            // For small x, use recursion and table lookup
            double result = 0;
            while (x > 2)
            {
                x -= 1;
                result += MathFunctions.Log(x);
            }
            
            // Use polynomial approximation for x in [1, 2]
            double z = x - 1;
            return result + z * (0.5772157 + z * (-0.6558781 + 
                   z * (0.0420026 + z * (0.1665386 + z * (-0.0421977)))));
        }
        
        #endregion
        
        #region Numerical Methods
        
        /// <summary>
        /// Numerical integration using Simpson's rule
        /// </summary>
        public static double Integrate(Func<double, double> f, double a, double b, int n = 1000)
        {
            if (n % 2 != 0) n++; // Ensure n is even
            
            double h = (b - a) / n;
            double sum = f(a) + f(b);
            
            for (int i = 1; i < n; i++)
            {
                double x = a + i * h;
                sum += (i % 2 == 0 ? 2 : 4) * f(x);
            }
            
            return sum * h / 3.0;
        }
        
        /// <summary>
        /// Numerical differentiation using central difference
        /// </summary>
        public static double Derivative(Func<double, double> f, double x, double h = 1e-8)
        {
            return (f(x + h) - f(x - h)) / (2 * h);
        }
        
        /// <summary>
        /// Find root using Newton-Raphson method
        /// </summary>
        public static double FindRoot(Func<double, double> f, Func<double, double> df, 
            double x0, double tolerance = 1e-10, int maxIterations = 100)
        {
            double x = x0;
            
            for (int i = 0; i < maxIterations; i++)
            {
                double fx = f(x);
                if (MathFunctions.Abs(fx) < tolerance)
                    return x;
                    
                double dfx = df(x);
                if (MathFunctions.Abs(dfx) < tolerance)
                    throw new InvalidOperationException("Derivative too small");
                    
                x = x - fx / dfx;
            }
            
            throw new InvalidOperationException("Failed to converge");
        }
        
        /// <summary>
        /// Find minimum using golden section search
        /// </summary>
        public static double FindMinimum(Func<double, double> f, double a, double b, 
            double tolerance = 1e-8)
        {
            const double phi = 1.618033988749895; // Golden ratio
            double resphi = 2 - phi;
            
            double x1 = a + resphi * (b - a);
            double x2 = b - resphi * (b - a);
            double f1 = f(x1);
            double f2 = f(x2);
            
            while (MathFunctions.Abs(b - a) > tolerance)
            {
                if (f1 < f2)
                {
                    b = x2;
                    x2 = x1;
                    f2 = f1;
                    x1 = a + resphi * (b - a);
                    f1 = f(x1);
                }
                else
                {
                    a = x1;
                    x1 = x2;
                    f1 = f2;
                    x2 = b - resphi * (b - a);
                    f2 = f(x2);
                }
            }
            
            return (a + b) / 2;
        }
        
        #endregion
    }
} 
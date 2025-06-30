import CodeBlock from '../components/CodeBlock'
import Callout from '../components/Callout'

export default function MathSymbolsPage() {
  return (
    <div className="max-w-4xl">
      <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
        Mathematical Symbols
      </h1>
      <p className="text-xl text-gray-600 dark:text-gray-300 mb-8">
        Ouroboros brings mathematical notation directly into your code. Use Greek letters, mathematical constants, 
        and operators as first-class citizens in your programs.
      </p>

      <Callout type="tip" title="Pro Tip">
        To use mathematical symbols, import the MathSymbols module: 
        <code className="bg-gray-100 dark:bg-gray-700 px-2 py-1 rounded mx-1">
          using static Ouroboros.StdLib.Math.MathSymbols;
        </code>
      </Callout>

      {/* Greek Letter Constants */}
      <section className="mb-12">
        <h2 id="constants" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Mathematical Constants
        </h2>

        <h3 id="pi" className="text-2xl font-semibold text-gray-800 dark:text-gray-200 mb-4">
          π (Pi) - Circle Constant
        </h3>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          The ratio of a circle's circumference to its diameter, approximately 3.14159265358979323846.
        </p>
        <CodeBlock
          code={`// Basic usage of π
double radius = 5.0;
double circumference = 2 * π * radius;
double area = π * radius * radius;

// Using π in trigonometry
double halfCircle = π;
double quarterCircle = π / 2;
double fullCircle = 2 * π;

// Converting degrees to radians
double degrees = 180;
double radians = degrees * (π / 180);  // equals π

// Calculating sphere volume
double sphereVolume = (4.0 / 3.0) * π * Math.Pow(radius, 3);

// Using π in complex calculations
var sinWave = Sin(2 * π * frequency * time);
var cosWave = Cos(2 * π * frequency * time);`}
        />

        <h3 id="tau" className="text-2xl font-semibold text-gray-800 dark:text-gray-200 mb-4 mt-8">
          τ (Tau) - Full Circle Constant
        </h3>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Equal to 2π, representing a full turn in radians. Some mathematicians argue τ is more natural than π.
        </p>
        <CodeBlock
          code={`// τ = 2π, representing a full circle
double fullRotation = τ;
double halfRotation = τ / 2;  // equals π
double quarterTurn = τ / 4;   // equals π/2

// Natural circle calculations with τ
double circumference = τ * radius;  // More intuitive than 2πr

// Angular calculations
double degreesToRadians = degrees * (τ / 360);
double radiansToDegrees = radians * (360 / τ);

// Periodic functions
double period = τ / frequency;
double phase = τ * time * frequency;

// Animation rotation
double rotationAngle = τ * animationProgress;  // 0 to 1 = full rotation`}
        />

        <h3 id="e" className="text-2xl font-semibold text-gray-800 dark:text-gray-200 mb-4 mt-8">
          e (Euler's Number)
        </h3>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          The base of natural logarithm, approximately 2.71828182845904523536.
        </p>
        <CodeBlock
          code={`// Natural exponential growth
double principal = 1000;
double rate = 0.05;
double time = 10;
double amount = principal * Math.Pow(e, rate * time);

// Natural logarithm
double ln_e = Math.Log(e);  // equals 1
double ln_e_squared = Math.Log(e * e);  // equals 2

// Compound interest (continuous)
double continuousCompound = principal * Math.Pow(e, rate * years);

// Probability distributions
double normalDistribution = (1 / Math.Sqrt(2 * π)) * Math.Pow(e, -0.5 * x * x);

// Euler's formula applications
var exponential = Math.Pow(e, x);
var decay = initialValue * Math.Pow(e, -decayRate * time);`}
        />

        <h3 id="phi" className="text-2xl font-semibold text-gray-800 dark:text-gray-200 mb-4 mt-8">
          φ (Phi) - Golden Ratio
        </h3>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          The golden ratio, approximately 1.61803398874989484820. Found throughout nature and art.
        </p>
        <CodeBlock
          code={`// Golden ratio calculations
double φ = (1 + Math.Sqrt(5)) / 2;  // Definition

// Golden rectangle
double width = 100;
double height = width / φ;  // Golden rectangle proportions
double goldenHeight = width * φ;  // Alternative proportion

// Fibonacci approximation
double fib_n = Math.Round(Math.Pow(φ, n) / Math.Sqrt(5));

// Golden spiral
double spiralRadius = initialRadius * Math.Pow(φ, angle / (π/2));

// Design applications
double goldenWidth = screenHeight * φ;
double smallerSection = totalLength / φ;
double largerSection = totalLength - smallerSection;

// Golden angle (in radians)
double goldenAngle = π * (3 - Math.Sqrt(5));  // ≈ 137.5°`}
        />

        <h3 className="text-2xl font-semibold text-gray-800 dark:text-gray-200 mb-4 mt-8">
          Other Mathematical Constants
        </h3>
        <CodeBlock
          code={`// γ (Euler-Mascheroni constant) ≈ 0.5772156649
double eulerMascheroni = γ;
// Used in number theory and analysis

// ρ (Plastic number) ≈ 1.324717957
double plasticNumber = ρ;
// Solution to x³ = x + 1

// δ (Feigenbaum delta) ≈ 4.669201609
double feigenbaumDelta = δ;
// Appears in chaos theory

// α (Feigenbaum alpha) ≈ 2.502907875
double feigenbaumAlpha = α;
// Related to period-doubling

// ∞ (Infinity)
double positiveInfinity = ∞;
double negativeInfinity = -∞;
bool isInfinite = result == ∞;

// Check for infinity
if (calculation >= ∞) {
    Console.WriteLine("Result is infinite");
}`}
        />
      </section>

      {/* Physics Constants */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Physics Constants
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Ouroboros includes fundamental physics constants for scientific computing.
        </p>
        <CodeBlock
          code={`// Speed of light in vacuum (m/s)
double c = 299792458;
double relativisticMass = restMass / Math.Sqrt(1 - (v*v)/(c*c));

// Gravitational constant (m³/kg·s²)
double G = 6.67430e-11;
double gravitationalForce = G * mass1 * mass2 / (distance * distance);

// Planck constant (J·s)
double h = 6.62607015e-34;
double photonEnergy = h * frequency;

// Reduced Planck constant (ħ = h/2π)
double ℏ = h / (2 * π);
double uncertaintyPrinciple = ℏ / 2;  // Minimum uncertainty

// Boltzmann constant (J/K)
double k_B = 1.380649e-23;
double thermalEnergy = k_B * temperature;

// Avogadro's number (1/mol)
double N_A = 6.02214076e23;
double moleculesInMole = N_A;

// Universal gas constant (J/mol·K)
double R = 8.314462618;
double idealGasPressure = (n * R * T) / V;`}
        />
      </section>

      {/* Complex Numbers */}
      <section className="mb-12">
        <h2 id="complex" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Complex Numbers
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Work with complex numbers using the imaginary unit i and Euler's formula.
        </p>
        <CodeBlock
          code={`// Creating complex numbers
var i = new Complex(0, 1);  // Imaginary unit
var z1 = new Complex(3, 4);  // 3 + 4i
var z2 = new Complex(1, -2); // 1 - 2i

// Complex arithmetic
var sum = z1 + z2;       // (4, 2) = 4 + 2i
var product = z1 * z2;   // (11, -2) = 11 - 2i
var quotient = z1 / z2;  // (-1, 2) = -1 + 2i

// Euler's identity: e^(iπ) + 1 = 0
var euler = Complex.Exp(i * π);
Console.WriteLine($"e^(iπ) = {euler}");  // -1 + 0i
Console.WriteLine($"e^(iπ) + 1 = {euler + 1}");  // ≈ 0

// More Euler's formula examples
var e_i_tau = Complex.Exp(i * τ);  // e^(i·2π) = 1
var e_i_pi_2 = Complex.Exp(i * π / 2);  // e^(iπ/2) = i

// Complex exponentials for rotation
double angle = π / 4;  // 45 degrees
var rotation = Complex.Exp(i * angle);
var rotatedPoint = originalPoint * rotation;

// De Moivre's theorem
var z = new Complex(1, 1);  // 1 + i
var z_cubed = Complex.Pow(z, 3);  // (1+i)³ = -2 + 2i

// Roots of unity
var nthRoot = Complex.Exp(i * 2 * π / n);  // nth root of unity`}
        />
      </section>

      {/* Trigonometric Functions */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Trigonometric Functions with Greek Symbols
        </h2>
        <CodeBlock
          code={`// Using θ (theta) for angles
double θ = π / 4;  // 45 degrees
double sinθ = Sin(θ);
double cosθ = Cos(θ);
double tanθ = Tan(θ);

// Pythagorean identity
double identity = Sin(θ) * Sin(θ) + Cos(θ) * Cos(θ);  // Always 1

// Using φ (phi) for phase
double φ = π / 6;  // Phase shift
double wave = A * Sin(ω * t + φ);

// Using ω (omega) for angular frequency
double ω = 2 * π * frequency;
double x = A * Cos(ω * t);
double y = A * Sin(ω * t);

// Multiple angle formulas
double sin2θ = 2 * Sin(θ) * Cos(θ);
double cos2θ = Cos(θ) * Cos(θ) - Sin(θ) * Sin(θ);

// Inverse trigonometry
double α = Asin(0.5);  // π/6
double β = Acos(0.5);  // π/3
double γ = Atan(1);    // π/4

// Hyperbolic functions
double sinhx = Sinh(x);
double coshx = Cosh(x);
double tanhx = Tanh(x);`}
        />
      </section>

      {/* Set Operations */}
      <section className="mb-12">
        <h2 id="sets" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Set Operations
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Use mathematical set notation directly in your code for intuitive set operations.
        </p>
        <CodeBlock
          code={`// Creating sets
var A = new HashSet<int> { 1, 2, 3, 4, 5 };
var B = new HashSet<int> { 4, 5, 6, 7, 8 };
var C = new HashSet<int> { 1, 3, 5, 7, 9 };

// Union (∪)
var AunionB = A ∪ B;  // {1, 2, 3, 4, 5, 6, 7, 8}
var allElements = A ∪ B ∪ C;  // {1, 2, 3, 4, 5, 6, 7, 8, 9}

// Intersection (∩)
var AintersectB = A ∩ B;  // {4, 5}
var commonToAll = A ∩ B ∩ C;  // {5}

// Element membership (∈, ∉)
bool contains3 = 3 ∈ A;     // true
bool missing6 = 6 ∉ A;      // true
bool has7 = 7 ∈ B;          // true

// Practical examples
var activeUsers = new HashSet<string> { "Alice", "Bob", "Charlie" };
var premiumUsers = new HashSet<string> { "Bob", "Charlie", "David" };

var allUsers = activeUsers ∪ premiumUsers;
var activePremium = activeUsers ∩ premiumUsers;

if ("Alice" ∈ activeUsers && "Alice" ∉ premiumUsers) {
    Console.WriteLine("Alice is active but not premium");
}

// Set differences (using traditional methods)
var AminusB = A.Except(B);  // {1, 2, 3}
var BminusA = B.Except(A);  // {6, 7, 8}

// Subset checking
bool isSubset = A.IsSubsetOf(B);  // false
bool isSuperset = A.IsSupersetOf(new HashSet<int> { 1, 2 });  // true`}
        />
      </section>

      {/* Calculus Operations */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Calculus Operations
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Symbolic representations for integrals, summations, and products.
        </p>
        <CodeBlock
          code={`// Integral notation (simplified - actual implementation may vary)
// ∫ f(x) dx from a to b
double integral = Integral(0, π, x => Sin(x));  // ∫₀^π sin(x) dx = 2

// Common integrals
double area = Integral(0, 1, x => x * x);  // ∫₀¹ x² dx = 1/3
double circleArea = Integral(-r, r, x => 2 * Math.Sqrt(r*r - x*x));  // πr²

// Summation (Σ)
double sum = Sum(1, 100, i => i);  // Σᵢ₌₁¹⁰⁰ i = 5050
double sumOfSquares = Sum(1, 10, i => i * i);  // Σᵢ₌₁¹⁰ i² = 385
double geometricSeries = Sum(0, 10, n => Math.Pow(0.5, n));  // Σₙ₌₀¹⁰ (1/2)ⁿ

// Product (Π)
double factorial = Product(1, 5, i => i);  // Πᵢ₌₁⁵ i = 5! = 120
double doubleFactorial = Product(1, 10, i => 2 * i);  // Πᵢ₌₁¹⁰ 2i

// Series expansions
double taylorSin = Sum(0, 10, n => 
    Math.Pow(-1, n) * Math.Pow(x, 2*n + 1) / Factorial(2*n + 1)
);  // Taylor series for sin(x)

// Numerical derivatives (approximation)
double derivative = (f(x + h) - f(x)) / h;  // f'(x)
double secondDerivative = (f(x + h) - 2*f(x) + f(x - h)) / (h * h);  // f''(x)`}
        />
      </section>

      {/* Statistical Functions */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Statistical Functions
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Use Greek letters for statistical operations following mathematical conventions.
        </p>
        <CodeBlock
          code={`// Sample data
double[] data = { 2.5, 3.7, 1.8, 4.2, 3.1, 5.5, 2.9, 4.8, 3.3, 4.1 };
double[] population = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

// μ (mu) - Mean/Average
double μ_sample = Mu(data);  // Sample mean
double μ_pop = Mu(population);  // Population mean

// σ (sigma) - Standard Deviation
double σ_sample = Sigma(data);  // Sample standard deviation
double σ_pop = Sigma(population);  // Population standard deviation

// σ² (sigma squared) - Variance
double σ²_sample = SigmaSquared(data);  // Sample variance
double σ²_pop = SigmaSquared(population);  // Population variance

// Normal distribution calculations
double z_score = (x - μ_sample) / σ_sample;  // Standardized score

// Confidence intervals
double confidence = 0.95;
double z_critical = 1.96;  // For 95% confidence
double margin_of_error = z_critical * (σ_sample / Math.Sqrt(n));
double lower_bound = μ_sample - margin_of_error;
double upper_bound = μ_sample + margin_of_error;

// Correlation coefficient (ρ - rho)
double ρ = Correlation(xData, yData);

// More statistical operations
double median = Median(data);
double mode = Mode(data);
double skewness = Skewness(data);
double kurtosis = Kurtosis(data);

// Probability calculations
double p_value = NormalCDF((x - μ_sample) / σ_sample);
double critical_value = NormalInverse(0.975) * σ_sample + μ_sample;`}
        />
      </section>

      {/* Best Practices */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Best Practices
        </h2>
        
        <Callout type="tip" title="Naming Conventions">
          Use Greek letters for variables that represent their traditional mathematical meanings:
          <ul className="list-disc list-inside mt-2">
            <li>θ, φ, ψ for angles</li>
            <li>ω for angular frequency</li>
            <li>λ for wavelength</li>
            <li>ρ for density or correlation</li>
            <li>μ for mean, σ for standard deviation</li>
          </ul>
        </Callout>

        <CodeBlock
          code={`// Good: Clear mathematical intent
double θ = angleInRadians;
double ω = 2 * π * frequency;
double λ = c / frequency;  // wavelength = speed of light / frequency

// Good: Statistical clarity
double[] samples = GetData();
double μ_samples = Mu(samples);
double σ_samples = Sigma(samples);

// Good: Physics calculations
double ρ = mass / volume;  // density
double F = m * a;  // force
double E = m * c * c;  // energy

// Avoid: Confusing usage
double π = 3.14;  // Don't redefine constants!
double μ = 42;    // Use meaningful names if not statistical mean`}
        />

        <Callout type="warning" title="Performance Considerations">
          While mathematical symbols make code more readable, remember:
          <ul className="list-disc list-inside mt-2">
            <li>Complex calculations may need optimization for performance-critical code</li>
            <li>Use built-in Math functions for standard operations</li>
            <li>Consider numerical stability for scientific calculations</li>
          </ul>
        </Callout>
      </section>

      {/* Complete Example */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Complete Example: Signal Processing
        </h2>
        <CodeBlock
          code={`using static Ouroboros.StdLib.Math.MathSymbols;

public class SignalProcessor
{
    // Generate a sine wave with frequency and phase
    public double[] GenerateSineWave(double frequency, double φ, double duration, int sampleRate)
    {
        double ω = 2 * π * frequency;  // Angular frequency
        int samples = (int)(duration * sampleRate);
        double[] signal = new double[samples];
        
        for (int i = 0; i < samples; i++)
        {
            double t = i / (double)sampleRate;
            signal[i] = Sin(ω * t + φ);
        }
        
        return signal;
    }
    
    // Apply Gaussian filter
    public double[] GaussianFilter(double[] signal, double σ_filter)
    {
        int kernelSize = (int)(6 * σ_filter) + 1;
        double[] kernel = new double[kernelSize];
        double sum = 0;
        
        // Generate Gaussian kernel
        for (int i = 0; i < kernelSize; i++)
        {
            double x = i - kernelSize / 2;
            kernel[i] = (1 / (σ_filter * Math.Sqrt(2 * π))) * 
                       Math.Exp(-(x * x) / (2 * σ_filter * σ_filter));
            sum += kernel[i];
        }
        
        // Normalize kernel
        for (int i = 0; i < kernelSize; i++)
        {
            kernel[i] /= sum;
        }
        
        // Apply convolution
        return Convolve(signal, kernel);
    }
    
    // Calculate signal statistics
    public void AnalyzeSignal(double[] signal)
    {
        double μ_signal = Mu(signal);
        double σ_signal = Sigma(signal);
        double σ²_signal = SigmaSquared(signal);
        
        Console.WriteLine($"Signal Statistics:");
        Console.WriteLine($"  Mean (μ): {μ_signal:F4}");
        Console.WriteLine($"  Std Dev (σ): {σ_signal:F4}");
        Console.WriteLine($"  Variance (σ²): {σ²_signal:F4}");
        
        // Check if signal is approximately zero-mean
        if (Math.Abs(μ_signal) < 0.01)
        {
            Console.WriteLine("  Signal is zero-mean ✓");
        }
    }
}`}
        />
      </section>
    </div>
  )
} 
import { Link } from 'react-router-dom'
import { ArrowRight, Code, Layers, Cpu, Hash, Package, Calculator } from 'lucide-react'
import CodeBlock from '../components/CodeBlock'

export default function HomePage() {
  return (
    <div>
      {/* Hero Section */}
      <div className="text-center mb-12">
        <h1 className="text-5xl font-bold text-gray-900 dark:text-white mb-4">
          Welcome to Ouroboros
        </h1>
        <p className="text-xl text-gray-600 dark:text-gray-300 max-w-3xl mx-auto">
          A revolutionary multi-paradigm programming language that seamlessly blends natural language syntax, 
          mathematical symbols, and systems programming capabilities into one powerful toolset.
        </p>
      </div>

      {/* Quick Start */}
      <div className="bg-gradient-to-r from-primary-500 to-primary-700 rounded-2xl p-8 text-white mb-12">
        <h2 className="text-3xl font-bold mb-4">Quick Start</h2>
        <p className="mb-6 text-lg">
          Ouroboros offers three syntax levels to match your programming style and needs:
        </p>
        <CodeBlock
          title="hello_world.ouro"
          code={`// High-level natural syntax
@high
print "Hello, World!"

// Medium-level C-like syntax
@medium
Console.WriteLine("Hello, World!");

// Low-level systems syntax
@low
unsafe {
    byte* msg = stackalloc byte[14];
    // ... manual memory operations
}`}
        />
      </div>

      {/* Key Features Grid */}
      <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-8">Key Features</h2>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-12">
        <FeatureCard
          icon={Code}
          title="Multi-Syntax Support"
          description="Write code in natural language, C-like syntax, or low-level systems programming style"
          link="/high-level-syntax"
        />
        <FeatureCard
          icon={Hash}
          title="Mathematical Symbols"
          description="Use Greek letters and mathematical symbols directly in your code: π, ∞, ∑, ∏, and more"
          link="/math-symbols"
        />
        <FeatureCard
          icon={Layers}
          title="Modern UI Framework"
          description="Build beautiful desktop applications with our comprehensive UI toolkit"
          link="/ui-framework"
        />
        <FeatureCard
          icon={Package}
          title="Rich Collections"
          description="Powerful data structures with intuitive APIs and set operations"
          link="/collections"
        />
        <FeatureCard
          icon={Calculator}
          title="Linear Algebra"
          description="Built-in support for vectors, matrices, quaternions, and transforms"
          link="/linear-algebra"
        />
        <FeatureCard
          icon={Cpu}
          title="Systems Programming"
          description="Direct memory access, SIMD operations, and unsafe code when you need it"
          link="/low-level-syntax"
        />
      </div>

      {/* Example Showcase */}
      <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-8">Example Showcase</h2>
      
      <div className="space-y-8 mb-12">
        <div>
          <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-3">
            Natural Language Programming
          </h3>
          <CodeBlock
            code={`@high
// Declare variables naturally
name := "Alice"
age := 25
hobbies := ["coding", "reading", "gaming"]

// Natural conditionals
if age is greater than 18 then
    print "\${name} is an adult"

// Intuitive loops
repeat 3 times
    print "Ouroboros is awesome!"

// Pattern matching
match age
    when 0 to 12 => print "Child"
    when 13 to 19 => print "Teenager"
    when 20 to 65 => print "Adult"
    default => print "Senior"

// Iterate naturally
for each hobby in hobbies
    print "I enjoy \${hobby}"`}
          />
        </div>

        <div>
          <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-3">
            Mathematical Computing
          </h3>
          <CodeBlock
            code={`using static Ouroboros.StdLib.Math.MathSymbols;

// Use mathematical constants
double circumference = 2 * π * radius;
double goldenRectangle = width * φ;

// Complex numbers with Euler's identity
var i = new Complex(0, 1);
var euler = Complex.Exp(i * π);  // e^(iπ) = -1

// Set operations
var A = new HashSet<int> { 1, 2, 3, 4, 5 };
var B = new HashSet<int> { 4, 5, 6, 7, 8 };
var union = A ∪ B;          // {1, 2, 3, 4, 5, 6, 7, 8}
var intersection = A ∩ B;   // {4, 5}
bool contains = 3 ∈ A;      // true

// Statistical functions
double[] data = { 1.0, 2.0, 3.0, 4.0, 5.0 };
double mean = μ(data);      // Mean
double stdDev = σ(data);    // Standard deviation`}
          />
        </div>

        <div>
          <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-3">
            Modern UI Development
          </h3>
          <CodeBlock
            code={`// Create a window with material design
var window = new Window("My App", 800, 600) {
    Theme = Theme.Material
};

// Add a menu bar
var menuBar = new MenuBar();
var fileMenu = menuBar.AddMenu("File");
fileMenu.AddItem("New", () => CreateNewDocument());
fileMenu.AddItem("Open", () => OpenDocument());
fileMenu.AddSeparator();
fileMenu.AddItem("Exit", () => Application.Exit());

// Create interactive controls
var button = new Button("Click Me!") {
    Position = new Vector(10, 40)
};
button.Click += (s, e) => {
    Console.WriteLine("Button clicked!");
};

// Advanced controls
var datePicker = new DatePicker();
var colorPicker = new ColorPicker();
var slider = new Slider() { 
    Minimum = 0, 
    Maximum = 100 
};`}
          />
        </div>
      </div>

      {/* Getting Started */}
      <div className="bg-gray-100 dark:bg-gray-800 rounded-2xl p-8 mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-4">Getting Started</h2>
        <ol className="space-y-4 text-lg text-gray-700 dark:text-gray-300">
          <li>
            <span className="font-semibold">1. Installation:</span> Download the Ouroboros compiler and runtime
          </li>
          <li>
            <span className="font-semibold">2. Choose Your Style:</span> Start with high-level syntax for ease, or dive into medium/low-level for control
          </li>
          <li>
            <span className="font-semibold">3. Import Libraries:</span> Use the comprehensive standard library for math, UI, I/O, and more
          </li>
          <li>
            <span className="font-semibold">4. Build Amazing Things:</span> Create everything from scripts to full applications
          </li>
        </ol>
      </div>

      {/* Next Steps */}
      <div className="text-center">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-4">Ready to Learn More?</h2>
        <p className="text-lg text-gray-600 dark:text-gray-300 mb-8">
          Explore the documentation to master all of Ouroboros's powerful features
        </p>
        <div className="flex flex-wrap justify-center gap-4">
          <Link
            to="/high-level-syntax"
            className="inline-flex items-center px-6 py-3 bg-primary-600 hover:bg-primary-700 text-white font-medium rounded-lg transition-colors"
          >
            Start with High-Level Syntax
            <ArrowRight className="ml-2 h-5 w-5" />
          </Link>
          <Link
            to="/math-symbols"
            className="inline-flex items-center px-6 py-3 bg-gray-200 hover:bg-gray-300 dark:bg-gray-700 dark:hover:bg-gray-600 text-gray-900 dark:text-white font-medium rounded-lg transition-colors"
          >
            Explore Math Symbols
            <ArrowRight className="ml-2 h-5 w-5" />
          </Link>
        </div>
      </div>
    </div>
  )
}

function FeatureCard({ icon: Icon, title, description, link }) {
  return (
    <Link
      to={link}
      className="block p-6 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 hover:border-primary-300 dark:hover:border-primary-700 hover:shadow-lg transition-all"
    >
      <Icon className="h-10 w-10 text-primary-600 dark:text-primary-400 mb-4" />
      <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">{title}</h3>
      <p className="text-gray-600 dark:text-gray-300">{description}</p>
    </Link>
  )
} 
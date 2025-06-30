# Getting Started with Ouroboros

Welcome to Ouroboros! This tutorial will guide you through installing Ouroboros, writing your first program, and understanding the core concepts of the language.

## Table of Contents
1. [Installation](#installation)
2. [Your First Program](#your-first-program)
3. [Understanding Syntax Levels](#understanding-syntax-levels)
4. [Variables and Types](#variables-and-types)
5. [Functions](#functions)
6. [Control Flow](#control-flow)
7. [Collections](#collections)
8. [Next Steps](#next-steps)

## Installation

### Windows

1. Download the latest Ouroboros installer from the releases page
2. Run the installer and follow the prompts
3. Add Ouroboros to your PATH:
   ```powershell
   $env:Path += ";C:\Program Files\Ouroboros\bin"
   ```
4. Verify installation:
   ```powershell
   ouro --version
   ```

### macOS

```bash
# Using Homebrew
brew tap ouroboros-lang/tap
brew install ouroboros

# Verify installation
ouro --version
```

### Linux

```bash
# Ubuntu/Debian
wget https://github.com/ouroboros-lang/releases/latest/ouroboros-linux-x64.tar.gz
tar -xzf ouroboros-linux-x64.tar.gz
sudo mv ouroboros /usr/local/bin/

# Verify installation
ouro --version
```

### Building from Source

```bash
git clone https://github.com/ouroboros-lang/ouroboros.git
cd ouroboros
./build.sh  # or build.ps1 on Windows
```

## Your First Program

Create a file named `hello.ouro`:

```ouro
// hello.ouro
function main() {
    console.WriteLine("Hello, Ouroboros!");
}
```

Run the program:
```bash
ouro hello.ouro
```

Output:
```
Hello, Ouroboros!
```

## Understanding Syntax Levels

Ouroboros unique feature is its three syntax levels. Let's explore each:

### High-Level Syntax (@high)

Natural language-like syntax for beginners and rapid prototyping:

```ouro
@high
define function greet taking name returning nothing as
    print "Hello, " plus name plus "!" to console;
end function

let userName equals "Alice";
call greet with userName;
```

### Medium-Level Syntax (@medium)

Traditional C-like syntax for general programming:

```ouro
@medium
function greet(name: string) {
    console.WriteLine($"Hello, {name}!");
}

let userName = "Alice";
greet(userName);
```

### Low-Level Syntax (@low)

System programming with manual memory control:

```ouro
@low
function greet(name: char*) {
    write(1, "Hello, ", 7);
    write(1, name, strlen(name));
    write(1, "!\n", 2);
}

char* userName = "Alice";
greet(userName);
```

### Mixing Syntax Levels

You can switch between levels within the same file:

```ouro
@high
let message equals "Starting program";
print message to console;

@medium
for (int i = 0; i < 5; i++) {
    console.WriteLine($"Iteration {i}");
}

@low
int* ptr = allocate<int>(1);
*ptr = 42;
console.WriteLine($"Value: {*ptr}");
free(ptr);
```

## Variables and Types

### Variable Declaration

```ouro
// Type inference
let age = 25;              // int
let name = "Bob";          // string
let pi = 3.14159;         // double
let isReady = true;       // bool

// Explicit types
int count = 0;
string message = "Hello";
double temperature = 98.6;
bool isActive = false;

// Constants
const int MAX_USERS = 100;
const string VERSION = "1.0.0";
```

### Greek Variables

Ouroboros supports Greek letters as identifiers:

```ouro
let π = 3.14159;
let θ = π / 4;
let Σ = 0;

for (int i = 1; i <= 10; i++) {
    Σ += i;  // Sum from 1 to 10
}
```

### Type Conversion

```ouro
// Implicit conversion
int x = 10;
double y = x;  // int to double

// Explicit conversion
double pi = 3.14159;
int rounded = (int)pi;  // 3

// String conversion
string text = x.ToString();
int parsed = int.Parse("123");

// Safe parsing
if (int.TryParse("123", out number)) {
    console.WriteLine($"Parsed: {number}");
}
```

## Functions

### Basic Functions

```ouro
// Function with return value
function add(a: int, b: int): int {
    return a + b;
}

// Void function
function printSum(a: int, b: int) {
    console.WriteLine($"{a} + {b} = {a + b}");
}

// Expression body
function square(x: int): int => x * x;

// Usage
let result = add(5, 3);
printSum(10, 20);
let squared = square(7);
```

### Parameters

```ouro
// Optional parameters
function greet(name: string, greeting: string = "Hello") {
    console.WriteLine($"{greeting}, {name}!");
}

greet("Alice");                    // "Hello, Alice!"
greet("Bob", "Good morning");      // "Good morning, Bob!"

// Variable parameters
function sum(params numbers: int[]): int {
    let total = 0;
    foreach (n in numbers) {
        total += n;
    }
    return total;
}

let total = sum(1, 2, 3, 4, 5);  // 15

// Reference parameters
function swap(ref a: int, ref b: int) {
    let temp = a;
    a = b;
    b = temp;
}

int x = 10, y = 20;
swap(ref x, ref y);  // x=20, y=10
```

### Lambda Functions

```ouro
// Simple lambda
let multiply = (a, b) => a * b;
let result = multiply(4, 5);  // 20

// Lambda with types
let divide = (a: double, b: double): double => a / b;

// Block body lambda
let process = (items) => {
    let filtered = [];
    foreach (item in items) {
        if (item > 0) {
            filtered.Add(item * 2);
        }
    }
    return filtered;
};
```

## Control Flow

### If Statements

```ouro
let score = 85;

if (score >= 90) {
    console.WriteLine("Grade: A");
} else if (score >= 80) {
    console.WriteLine("Grade: B");
} else if (score >= 70) {
    console.WriteLine("Grade: C");
} else {
    console.WriteLine("Grade: F");
}

// Ternary operator
let grade = score >= 60 ? "Pass" : "Fail";
```

### Switch Statements

```ouro
// Traditional switch
let day = 3;
switch (day) {
    case 1:
        console.WriteLine("Monday");
        break;
    case 2:
        console.WriteLine("Tuesday");
        break;
    case 3:
        console.WriteLine("Wednesday");
        break;
    default:
        console.WriteLine("Other day");
        break;
}

// Switch expression
let dayName = day switch {
    1 => "Monday",
    2 => "Tuesday",
    3 => "Wednesday",
    4 => "Thursday",
    5 => "Friday",
    6 or 7 => "Weekend",
    _ => "Invalid day"
};
```

### Loops

```ouro
// For loop
for (int i = 0; i < 5; i++) {
    console.WriteLine($"Count: {i}");
}

// While loop
int count = 0;
while (count < 5) {
    console.WriteLine($"Count: {count}");
    count++;
}

// Do-while loop
do {
    console.WriteLine("At least once");
} while (false);

// Foreach loop
let numbers = [1, 2, 3, 4, 5];
foreach (num in numbers) {
    console.WriteLine(num);
}
```

### Custom Loops

Ouroboros provides unique loop constructs:

```ouro
// Repeat loop
repeat 3 times {
    console.WriteLine("Hello!");
}

// Iterate loop
iterate i: 1..5 {
    console.WriteLine($"Number: {i}");
}

// Iterate with step
iterate i: 0..10:2 {  // 0, 2, 4, 6, 8
    console.WriteLine(i);
}

// Forever loop
let counter = 0;
forever {
    console.WriteLine($"Counter: {counter}");
    counter++;
    if (counter >= 5) break;
}
```

## Collections

### Lists

```ouro
using Ouroboros.StdLib.Collections;

// Create a list
let numbers = new List<int>();

// Add elements
numbers.Add(10);
numbers.Add(20);
numbers.Add(30);

// Access elements
console.WriteLine(numbers[0]);  // 10

// Iterate
foreach (num in numbers) {
    console.WriteLine(num);
}

// List with initialization
let fruits = new List<string> { "Apple", "Banana", "Orange" };

// Common operations
numbers.Remove(20);
numbers.Insert(1, 15);
let count = numbers.Count;
let index = numbers.IndexOf(15);
```

### Dictionaries

```ouro
// Create dictionary
let scores = new Dictionary<string, int>();

// Add key-value pairs
scores["Alice"] = 95;
scores["Bob"] = 87;
scores["Charlie"] = 92;

// Access values
console.WriteLine(scores["Alice"]);  // 95

// Check if key exists
if (scores.ContainsKey("David")) {
    console.WriteLine(scores["David"]);
}

// Safe access
if (scores.TryGetValue("Eve", out score)) {
    console.WriteLine($"Eve's score: {score}");
}

// Iterate
foreach (kvp in scores) {
    console.WriteLine($"{kvp.Key}: {kvp.Value}");
}
```

### Arrays

```ouro
// Single-dimensional array
int[] numbers = new int[5];
numbers[0] = 10;
numbers[1] = 20;

// Array initialization
int[] primes = [2, 3, 5, 7, 11];

// Multi-dimensional array
int[,] matrix = new int[3, 3];
matrix[0, 0] = 1;
matrix[1, 1] = 1;
matrix[2, 2] = 1;

// Jagged array
int[][] jagged = new int[3][];
jagged[0] = new int[] { 1, 2 };
jagged[1] = new int[] { 3, 4, 5 };
jagged[2] = new int[] { 6 };
```

## Pattern Matching

Ouroboros supports advanced pattern matching:

```ouro
// Type patterns
object value = GetValue();

let description = value switch {
    int n => $"Integer: {n}",
    string s => $"String: {s}",
    double d => $"Double: {d}",
    null => "Null value",
    _ => "Unknown type"
};

// Conditional patterns
let category = age switch {
    < 13 => "Child",
    >= 13 and < 20 => "Teenager",
    >= 20 and < 60 => "Adult",
    >= 60 => "Senior",
    _ => "Unknown"
};

// Tuple patterns
let point = (10, 20);
let position = point switch {
    (0, 0) => "Origin",
    (0, _) => "On Y-axis",
    (_, 0) => "On X-axis",
    (var x, var y) when x == y => "On diagonal",
    _ => "General position"
};
```

## Error Handling

```ouro
try {
    let content = FileSystem.ReadAllText("data.txt");
    let data = parseData(content);
    processData(data);
} catch (FileNotFoundException e) {
    console.WriteLine($"File not found: {e.Message}");
} catch (ParseException e) {
    console.WriteLine($"Parse error: {e.Message}");
} catch (Exception e) {
    console.WriteLine($"Unexpected error: {e.Message}");
} finally {
    console.WriteLine("Cleanup complete");
}
```

## Next Steps

Now that you understand the basics, explore these topics:

1. **[Object-Oriented Programming](oop_tutorial.md)** - Classes, inheritance, and interfaces
2. **[Mathematical Programming](math_tutorial.md)** - Using Greek symbols and math functions
3. **[Data-Oriented Design](dod_tutorial.md)** - Components, systems, and entities
4. **[Async Programming](async_tutorial.md)** - Asynchronous operations and parallel processing
5. **[Advanced Features](advanced_tutorial.md)** - Generics, LINQ, and more

### Example Projects

Try building these projects to practice:

1. **Calculator** - A simple calculator using pattern matching
2. **Todo List** - Console app with file persistence
3. **Game** - Simple text-based game using OOP
4. **Data Processor** - File processing with parallel operations

### Resources

- [Language Reference](../reference/language_reference.md)
- [API Documentation](../api/stdlib_api.md)
- [Example Programs](../examples/example_programs.md)
- [Community Forum](https://forum.ouroboros-lang.org)
- [GitHub Repository](https://github.com/ouroboros-lang/ouroboros)

Happy coding with Ouroboros! 🐍 
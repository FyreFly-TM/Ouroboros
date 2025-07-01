# Getting Started with Ouroboros

Welcome to Ouroboros! This guide will help you get up and running with the Ouroboros programming language.

## Table of Contents
1. [Installation](#installation)
2. [Your First Program](#your-first-program)
3. [Basic Concepts](#basic-concepts)
4. [Syntax Levels](#syntax-levels)
5. [Variables and Types](#variables-and-types)
6. [Functions](#functions)
7. [Control Flow](#control-flow)
8. [Classes and Objects](#classes-and-objects)
9. [Error Handling](#error-handling)
10. [Next Steps](#next-steps)

## Installation

### Prerequisites
- .NET 6.0 SDK or later
- A text editor or IDE (VS Code recommended)
- Git (optional, for cloning the repository)

### Installing Ouroboros

#### From Source
```bash
# Clone the repository
git clone https://github.com/ouroboros-lang/ouroboros.git
cd ouroboros

# Build the project
dotnet build

# Run the REPL
dotnet run
```

#### Using Package Manager
```bash
# Install globally
dotnet tool install -g ouroboros

# Verify installation
ouro --version
```

### Setting up VS Code
1. Install the Ouroboros extension from the marketplace
2. Open a `.ouro` file
3. The extension provides:
   - Syntax highlighting
   - IntelliSense
   - Error checking
   - Debugging support

## Your First Program

Create a file called `hello.ouro`:

```ouro
// Traditional syntax
function main() {
    print("Hello, Ouroboros!");
}
```

Run it:
```bash
ouro hello.ouro
```

You can also use the natural language syntax:

```ouro
@high
create a function called main that
    prints "Hello, Ouroboros!" to the console.
end function
```

## Basic Concepts

### Comments
```ouro
// Single-line comment

/* 
   Multi-line comment
   Can span multiple lines
*/

/// Documentation comment
/// Used for generating API docs
```

### Program Structure
Every Ouroboros program needs a `main` function as the entry point:

```ouro
function main() {
    // Your program starts here
}
```

For larger programs, you can organize code into modules:

```ouro
// math_utils.ouro
export function add(a: int, b: int): int {
    return a + b;
}

// main.ouro
import { add } from "./math_utils";

function main() {
    let result = add(5, 3);
    print($"5 + 3 = {result}");
}
```

## Syntax Levels

Ouroboros supports multiple syntax levels that can be mixed in the same file:

### High-Level (Natural Language)
```ouro
@high
let age equal 25.
if age is greater than or equal to 18 then
    print "You are an adult" to console.
else
    print "You are a minor" to console.
end if
```

### Medium-Level (Traditional)
```ouro
@medium
let age = 25;
if (age >= 18) {
    console.WriteLine("You are an adult");
} else {
    console.WriteLine("You are a minor");
}
```

### Low-Level (System Programming)
```ouro
@low
int* age_ptr = allocate<int>(1);
*age_ptr = 25;
if (*age_ptr >= 18) {
    write(1, "You are an adult\n", 17);
} else {
    write(1, "You are a minor\n", 16);
}
free(age_ptr);
```

### Assembly
```ouro
@asm {
    mov eax, 25      ; age = 25
    cmp eax, 18      ; compare with 18
    jge adult        ; jump if greater or equal
    ; minor code here
    jmp end
adult:
    ; adult code here
end:
}
```

## Variables and Types

### Variable Declaration
```ouro
// Type inference
let name = "Alice";          // string
let age = 25;                // int
let height = 5.7;            // double
let isStudent = true;        // bool

// Explicit types
string city = "New York";
int population = 8_000_000;
double temperature = 72.5;
bool isRaining = false;

// Constants
const double PI = 3.14159;
const string APP_NAME = "MyApp";

// Multiple declarations
let x = 1, y = 2, z = 3;
```

### Basic Types
- `bool`: true or false
- `int`: 32-bit integer
- `long`: 64-bit integer
- `float`: 32-bit floating point
- `double`: 64-bit floating point
- `string`: text
- `char`: single character

### Collections
```ouro
// Arrays
int[] numbers = [1, 2, 3, 4, 5];
string[] names = ["Alice", "Bob", "Charlie"];

// Lists (dynamic arrays)
List<int> scores = new List<int>();
scores.Add(95);
scores.Add(87);

// Dictionaries
Dictionary<string, int> ages = new Dictionary<string, int>();
ages["Alice"] = 25;
ages["Bob"] = 30;

// Tuples
(string name, int age) person = ("Alice", 25);
print($"{person.name} is {person.age} years old");
```

## Functions

### Basic Functions
```ouro
// Simple function
function greet(name: string) {
    print($"Hello, {name}!");
}

// Function with return value
function add(a: int, b: int): int {
    return a + b;
}

// Expression body
function square(x: int): int => x * x;

// Optional parameters
function createUser(name: string, age: int = 0): User {
    return new User { Name = name, Age = age };
}

// Variable arguments
function sum(params numbers: int[]): int {
    let total = 0;
    foreach (n in numbers) {
        total += n;
    }
    return total;
}

// Call examples
greet("Alice");
let result = add(5, 3);
let sq = square(4);
let user1 = createUser("Bob");
let user2 = createUser("Alice", 25);
let total = sum(1, 2, 3, 4, 5);
```

### Lambda Functions
```ouro
// Simple lambda
let multiply = (a: int, b: int) => a * b;

// Lambda with body
let processData = (data: string) => {
    let processed = data.ToUpper();
    return processed.Trim();
};

// Using lambdas with higher-order functions
let numbers = [1, 2, 3, 4, 5];
let doubled = numbers.Select(x => x * 2);
let evens = numbers.Where(x => x % 2 == 0);
```

## Control Flow

### If Statements
```ouro
let score = 85;

if (score >= 90) {
    print("Grade: A");
} else if (score >= 80) {
    print("Grade: B");
} else if (score >= 70) {
    print("Grade: C");
} else {
    print("Grade: F");
}

// Ternary operator
let status = score >= 60 ? "Pass" : "Fail";
```

### Switch Statements
```ouro
let day = "Monday";

switch (day) {
    case "Monday":
    case "Tuesday":
    case "Wednesday":
    case "Thursday":
    case "Friday":
        print("Weekday");
        break;
    case "Saturday":
    case "Sunday":
        print("Weekend");
        break;
    default:
        print("Invalid day");
        break;
}

// Switch expression
let dayType = day switch {
    "Monday" or "Tuesday" or "Wednesday" or "Thursday" or "Friday" => "Weekday",
    "Saturday" or "Sunday" => "Weekend",
    _ => "Invalid"
};
```

### Loops
```ouro
// For loop
for (let i = 0; i < 10; i++) {
    print(i);
}

// While loop
let count = 0;
while (count < 5) {
    print($"Count: {count}");
    count++;
}

// Do-while loop
do {
    print("This runs at least once");
} while (false);

// Foreach loop
let fruits = ["apple", "banana", "orange"];
foreach (fruit in fruits) {
    print($"I like {fruit}");
}

// Break and continue
for (let i = 0; i < 10; i++) {
    if (i == 3) continue;  // Skip 3
    if (i == 7) break;     // Stop at 7
    print(i);
}
```

## Classes and Objects

### Basic Classes
```ouro
class Person {
    // Properties
    public string Name { get; set; }
    public int Age { get; set; }
    private string ssn;
    
    // Constructor
    public Person(name: string, age: int) {
        Name = name;
        Age = age;
    }
    
    // Methods
    public void Birthday() {
        Age++;
        print($"{Name} is now {Age} years old!");
    }
    
    public string GetInfo() {
        return $"{Name}, {Age} years old";
    }
}

// Using the class
let person = new Person("Alice", 25);
person.Birthday();
print(person.GetInfo());
```

### Inheritance
```ouro
class Animal {
    public string Name { get; set; }
    
    public Animal(name: string) {
        Name = name;
    }
    
    public virtual void MakeSound() {
        print($"{Name} makes a sound");
    }
}

class Dog : Animal {
    public string Breed { get; set; }
    
    public Dog(name: string, breed: string) : base(name) {
        Breed = breed;
    }
    
    public override void MakeSound() {
        print($"{Name} barks!");
    }
    
    public void WagTail() {
        print($"{Name} wags tail");
    }
}

// Using inheritance
let dog = new Dog("Buddy", "Golden Retriever");
dog.MakeSound();  // "Buddy barks!"
dog.WagTail();    // "Buddy wags tail"
```

### Interfaces
```ouro
interface IDrawable {
    void Draw();
    property Color Color { get; set; }
}

interface IMovable {
    void Move(x: int, y: int);
    property Position Position { get; }
}

class Shape : IDrawable, IMovable {
    public Color Color { get; set; }
    public Position Position { get; private set; }
    
    public Shape() {
        Position = new Position(0, 0);
    }
    
    public void Draw() {
        print($"Drawing shape at {Position}");
    }
    
    public void Move(x: int, y: int) {
        Position = new Position(x, y);
    }
}
```

## Error Handling

### Try-Catch
```ouro
try {
    let result = riskyOperation();
    print($"Success: {result}");
} catch (SpecificException e) {
    print($"Specific error: {e.Message}");
} catch (Exception e) {
    print($"General error: {e.Message}");
} finally {
    print("Cleanup code runs always");
}
```

### Throwing Exceptions
```ouro
function divide(a: int, b: int): double {
    if (b == 0) {
        throw new DivideByZeroException("Cannot divide by zero!");
    }
    return (double)a / b;
}

// Custom exceptions
class ValidationException : Exception {
    public string Field { get; }
    
    public ValidationException(field: string, message: string) : base(message) {
        Field = field;
    }
}

function validateAge(age: int) {
    if (age < 0 || age > 150) {
        throw new ValidationException("age", "Age must be between 0 and 150");
    }
}
```

### Using Statement
```ouro
// Automatic resource disposal
using (let file = File.Open("data.txt")) {
    let content = file.ReadAllText();
    print(content);
}  // file is automatically closed

// Multiple resources
using (let input = File.OpenRead("input.txt"))
using (let output = File.OpenWrite("output.txt")) {
    input.CopyTo(output);
}
```

## Next Steps

### Learn More
1. **[Language Reference](../reference/language_reference.md)** - Complete language specification
2. **[Standard Library](../api/stdlib_api.md)** - Built-in functions and types
3. **[UI Framework Tutorial](ui_framework.md)** - Build graphical applications
4. **[Examples](../examples/example_programs.md)** - Sample programs

### Advanced Topics
- **Async Programming** - Using async/await for concurrent code
- **Pattern Matching** - Advanced pattern matching techniques
- **Generics** - Writing reusable, type-safe code
- **Memory Management** - Low-level memory control
- **GPU Programming** - CUDA and compute shaders
- **Contract Programming** - Design by contract

### Community Resources
- **GitHub**: https://github.com/ouroboros-lang/ouroboros
- **Discord**: Join our community server
- **Forums**: https://forums.ouroboros-lang.org
- **Stack Overflow**: Tag your questions with `ouroboros`

### Sample Projects
Try building these projects to practice:
1. **Calculator** - Basic arithmetic operations
2. **Todo List** - File I/O and data structures
3. **Simple Game** - Graphics and user input
4. **Web Scraper** - Network programming
5. **Data Analyzer** - Working with CSV/JSON

Happy coding with Ouroboros! 🐍 
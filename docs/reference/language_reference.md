# Ouroboros Language Reference

## Table of Contents
1. [Introduction](#introduction)
2. [Syntax Levels](#syntax-levels)
3. [Lexical Structure](#lexical-structure)
4. [Types](#types)
5. [Variables and Constants](#variables-and-constants)
6. [Operators](#operators)
7. [Control Flow](#control-flow)
8. [Functions](#functions)
9. [Classes and Objects](#classes-and-objects)
10. [Interfaces](#interfaces)
11. [Generics](#generics)
12. [Pattern Matching](#pattern-matching)
13. [Error Handling](#error-handling)
14. [Memory Management](#memory-management)
15. [Concurrency](#concurrency)
16. [Modules and Namespaces](#modules-and-namespaces)

## Introduction

Ouroboros is a multi-paradigm programming language that supports three syntax levels, seamlessly integrated to provide flexibility from high-level abstractions to low-level control.

### File Extensions
- `.ouro` - Standard Ouroboros source files
- `.ob` - Compact Ouroboros source files

## Syntax Levels

Ouroboros provides three syntax levels that can be switched dynamically:

### @high - Natural Language Syntax
```ouro
@high
let x equals 10;
if x is greater than 5 then
    print "x is large" to console;
end if
```

### @medium - Traditional Syntax
```ouro
@medium
int x = 10;
if (x > 5) {
    console.WriteLine("x is large");
}
```

### @low - System Programming Syntax
```ouro
@low
int* ptr = allocate<int>(1);
*ptr = 10;
if (*ptr > 5) {
    write(1, "x is large\n", 11);
}
free(ptr);
```

### @asm - Inline Assembly
```ouro
@asm {
    mov eax, 10
    cmp eax, 5
    jg large
}
```

## Lexical Structure

### Comments
```ouro
// Single-line comment

/* Multi-line
   comment */

/// Documentation comment
/// Used for generating API documentation
```

### Identifiers
- Start with letter or underscore
- Can contain letters, digits, underscores
- Greek letters are valid identifiers
- Case-sensitive

```ouro
let myVariable = 10;
let _private = 20;
let π = 3.14159;
let θ_angle = 45.0;
```

### Keywords

Reserved keywords cannot be used as identifiers:

```
abstract    as          async       await       base
break       case        catch       class       component
const       continue    default     do          else
entity      enum        event       false       finally
for         foreach     function    if          in
interface   internal    is          let         namespace
new         null        operator    out         override
private     protected   public      readonly    ref
return      sealed      static      struct      switch
system      this        throw       true        try
typeof      using       var         virtual     while
yield
```

### Literals

#### Numeric Literals
```ouro
// Integers
42          // Decimal
0x2A        // Hexadecimal
0b101010    // Binary
0o52        // Octal

// Floating-point
3.14159     // Double
3.14f       // Float
3.14m       // Decimal

// Scientific notation
1.23e-4     // 0.000123
6.02e23     // Avogadro's number
```

#### String Literals
```ouro
"Hello, World!"              // Regular string
'A'                          // Character literal
$"Hello, {name}!"           // Interpolated string
@"C:\Users\path"            // Verbatim string
$@"Path: {path}\file.txt"   // Verbatim interpolated

// Multi-line strings
"""
This is a
multi-line string
"""

// Raw strings
r"This is a \raw\ string"
```

#### Boolean Literals
```ouro
true
false
```

## Types

### Primitive Types

| Type | Description | Size | Range |
|------|-------------|------|-------|
| bool | Boolean | 1 byte | true/false |
| byte | Unsigned byte | 1 byte | 0 to 255 |
| sbyte | Signed byte | 1 byte | -128 to 127 |
| short | Short integer | 2 bytes | -32,768 to 32,767 |
| ushort | Unsigned short | 2 bytes | 0 to 65,535 |
| int | Integer | 4 bytes | -2³¹ to 2³¹-1 |
| uint | Unsigned integer | 4 bytes | 0 to 2³²-1 |
| long | Long integer | 8 bytes | -2⁶³ to 2⁶³-1 |
| ulong | Unsigned long | 8 bytes | 0 to 2⁶⁴-1 |
| float | Single precision | 4 bytes | ±1.5e-45 to ±3.4e38 |
| double | Double precision | 8 bytes | ±5.0e-324 to ±1.7e308 |
| decimal | High precision | 16 bytes | ±1.0e-28 to ±7.9e28 |
| char | Unicode character | 2 bytes | U+0000 to U+FFFF |

### Composite Types

#### Arrays
```ouro
int[] numbers = new int[10];
int[,] matrix = new int[3, 3];
int[][] jagged = new int[3][];

// Array initialization
int[] primes = [2, 3, 5, 7, 11];
string[] names = ["Alice", "Bob", "Charlie"];
```

#### Tuples
```ouro
(int, string) person = (25, "Alice");
let (age, name) = person;  // Deconstruction

// Named tuples
(int x, int y) point = (10, 20);
console.WriteLine($"Point: ({point.x}, {point.y})");
```

#### Nullable Types
```ouro
int? nullableInt = null;
string? nullableString = null;

// Null-coalescing
int value = nullableInt ?? 0;

// Null-conditional
int? length = nullableString?.Length;
```

### Type Inference
```ouro
let x = 10;          // int
let y = 3.14;        // double
let name = "Alice";  // string
let list = [1, 2, 3]; // List<int>
```

## Variables and Constants

### Variable Declaration
```ouro
// Type inference
let x = 10;
var y = "Hello";

// Explicit type
int count = 0;
string message = "Welcome";
List<int> numbers = new List<int>();

// Multiple declarations
int a = 1, b = 2, c = 3;
```

### Constants
```ouro
const int MAX_SIZE = 100;
const string APP_NAME = "Ouroboros";
const double PI = 3.14159265359;
```

### Readonly Fields
```ouro
class Config {
    public readonly string Version = "1.0.0";
    public readonly DateTime CreatedAt;
    
    public Config() {
        CreatedAt = DateTime.Now;  // Can set in constructor
    }
}
```

## Operators

### Arithmetic Operators
| Operator | Description | Example |
|----------|-------------|---------|
| + | Addition | a + b |
| - | Subtraction | a - b |
| * | Multiplication | a * b |
| / | Division | a / b |
| % | Modulo | a % b |
| ** | Exponentiation | a ** b |

### Comparison Operators
| Operator | Description | Example |
|----------|-------------|---------|
| == | Equal | a == b |
| != | Not equal | a != b |
| < | Less than | a < b |
| > | Greater than | a > b |
| <= | Less than or equal | a <= b |
| >= | Greater than or equal | a >= b |
| ≤ | Less than or equal (Unicode) | a ≤ b |
| ≥ | Greater than or equal (Unicode) | a ≥ b |
| ≠ | Not equal (Unicode) | a ≠ b |

### Logical Operators
| Operator | Description | Example |
|----------|-------------|---------|
| && | Logical AND | a && b |
| \|\| | Logical OR | a \|\| b |
| ! | Logical NOT | !a |

### Bitwise Operators
| Operator | Description | Example |
|----------|-------------|---------|
| & | Bitwise AND | a & b |
| \| | Bitwise OR | a \| b |
| ^ | Bitwise XOR | a ^ b |
| ~ | Bitwise NOT | ~a |
| << | Left shift | a << 2 |
| >> | Right shift | a >> 2 |

### Assignment Operators
```ouro
x = 10;      // Assignment
x += 5;      // Addition assignment
x -= 3;      // Subtraction assignment
x *= 2;      // Multiplication assignment
x /= 4;      // Division assignment
x %= 3;      // Modulo assignment
x **= 2;     // Exponentiation assignment
x &= 0xFF;   // Bitwise AND assignment
x |= 0x01;   // Bitwise OR assignment
x ^= 0x0F;   // Bitwise XOR assignment
x <<= 2;     // Left shift assignment
x >>= 1;     // Right shift assignment
x ??= 5;     // Null-coalescing assignment
```

### Special Operators
```ouro
// Ternary conditional
let result = condition ? trueValue : falseValue;

// Null-coalescing
let value = nullable ?? defaultValue;

// Null-conditional
let length = str?.Length;
let item = array?[index];
let result = obj?.Method();

// Type testing
if (obj is string str) {
    console.WriteLine(str.ToUpper());
}

// Type casting
string text = obj as string;
int number = (int)doubleValue;

// Range operator
let slice = array[1..5];     // Elements 1 through 4
let tail = array[2..];       // Elements from 2 to end
let head = array[..3];       // Elements 0 through 2
```

## Control Flow

### If Statement
```ouro
if (condition) {
    // code
} else if (otherCondition) {
    // code
} else {
    // code
}

// Single line
if (x > 0) console.WriteLine("Positive");
```

### Switch Statement
```ouro
switch (value) {
    case 1:
        console.WriteLine("One");
        break;
    case 2:
    case 3:
        console.WriteLine("Two or Three");
        break;
    default:
        console.WriteLine("Other");
        break;
}

// Switch expression
let result = value switch {
    1 => "One",
    2 or 3 => "Two or Three",
    > 10 => "Large",
    _ => "Other"
};
```

### Loops

#### For Loop
```ouro
for (int i = 0; i < 10; i++) {
    console.WriteLine(i);
}
```

#### While Loop
```ouro
while (condition) {
    // code
}

do {
    // code
} while (condition);
```

#### Foreach Loop
```ouro
foreach (item in collection) {
    console.WriteLine(item);
}

// With index
foreach ((item, index) in collection.WithIndex()) {
    console.WriteLine($"{index}: {item}");
}
```

#### Custom Loops
```ouro
// Repeat loop
repeat 10 times {
    console.WriteLine("Hello");
}

// Iterate loop
iterate i: 0..10 {
    console.WriteLine(i);
}

// With step
iterate i: 0..10:2 {  // 0, 2, 4, 6, 8
    console.WriteLine(i);
}

// Forever loop
forever {
    if (shouldStop) break;
    // code
}

// Parallel for
parallel for (i in 0..1000) {
    processItem(items[i]);
}

// Async for
async for (item in asyncCollection) {
    await processAsync(item);
}
```

### Jump Statements
```ouro
break;      // Exit loop
continue;   // Next iteration
return;     // Exit function
return value; // Return with value
yield return value; // Iterator
goto label; // Jump to label (use sparingly)
```

## Functions

### Function Declaration
```ouro
// Basic function
function add(a: int, b: int): int {
    return a + b;
}

// Void function
function printMessage(message: string) {
    console.WriteLine(message);
}

// Expression body
function square(x: int): int => x * x;
```

### Parameters

#### Optional Parameters
```ouro
function greet(name: string, greeting: string = "Hello"): string {
    return $"{greeting}, {name}!";
}
```

#### Named Parameters
```ouro
function createUser(name: string, age: int, city: string) {
    // ...
}

// Call with named parameters
createUser(name: "Alice", city: "NYC", age: 25);
```

#### Variable Parameters
```ouro
function sum(params numbers: int[]): int {
    let total = 0;
    foreach (n in numbers) {
        total += n;
    }
    return total;
}

// Call
let result = sum(1, 2, 3, 4, 5);
```

#### Reference Parameters
```ouro
function swap(ref a: int, ref b: int) {
    let temp = a;
    a = b;
    b = temp;
}

// Call
int x = 10, y = 20;
swap(ref x, ref y);
```

#### Out Parameters
```ouro
function tryParse(text: string, out value: int): bool {
    // parsing logic
    if (success) {
        value = parsedValue;
        return true;
    }
    value = 0;
    return false;
}

// Call
if (tryParse("123", out number)) {
    console.WriteLine($"Parsed: {number}");
}
```

### Lambda Functions
```ouro
// Single parameter
let square = x => x * x;

// Multiple parameters
let add = (a, b) => a + b;

// With types
let multiply = (a: int, b: int): int => a * b;

// Block body
let process = (items) => {
    let result = [];
    foreach (item in items) {
        result.Add(transform(item));
    }
    return result;
};
```

### Local Functions
```ouro
function processData(data: List<int>): int {
    // Local function
    function validate(item: int): bool {
        return item > 0 && item < 100;
    }
    
    let sum = 0;
    foreach (item in data) {
        if (validate(item)) {
            sum += item;
        }
    }
    return sum;
}
```

## Classes and Objects

### Class Declaration
```ouro
class Person {
    // Fields
    private string name;
    private int age;
    
    // Properties
    public string Name {
        get => name;
        set => name = value ?? "Unknown";
    }
    
    public int Age {
        get => age;
        set {
            if (value >= 0) age = value;
        }
    }
    
    // Auto-property
    public string Email { get; set; }
    
    // Constructor
    public Person(name: string, age: int) {
        this.name = name;
        this.age = age;
    }
    
    // Methods
    public void Greet() {
        console.WriteLine($"Hello, I'm {name}!");
    }
    
    // Static members
    public static int Count { get; private set; }
    
    public static Person CreateDefault() {
        return new Person("Default", 0);
    }
}
```

### Inheritance
```ouro
class Employee : Person {
    public string Department { get; set; }
    public decimal Salary { get; set; }
    
    public Employee(name: string, age: int, department: string) 
        : base(name, age) {
        Department = department;
    }
    
    // Override method
    public override void Greet() {
        base.Greet();
        console.WriteLine($"I work in {Department}");
    }
}
```

### Abstract Classes
```ouro
abstract class Shape {
    public abstract double Area { get; }
    public abstract double Perimeter { get; }
    
    public void Display() {
        console.WriteLine($"Area: {Area}, Perimeter: {Perimeter}");
    }
}

class Circle : Shape {
    public double Radius { get; set; }
    
    public override double Area => Math.PI * Radius * Radius;
    public override double Perimeter => 2 * Math.PI * Radius;
}
```

### Sealed Classes
```ouro
sealed class FinalClass {
    // Cannot be inherited
}
```

### Partial Classes
```ouro
// File1.ouro
partial class DataModel {
    public string Name { get; set; }
}

// File2.ouro
partial class DataModel {
    public int Id { get; set; }
}
```

## Interfaces

### Interface Declaration
```ouro
interface IDrawable {
    void Draw();
    property Color Color { get; set; }
}

interface IResizable {
    void Resize(width: int, height: int);
    property Size Size { get; }
}

// Multiple inheritance
class Widget : IDrawable, IResizable {
    public Color Color { get; set; }
    public Size Size { get; private set; }
    
    public void Draw() {
        // Implementation
    }
    
    public void Resize(width: int, height: int) {
        Size = new Size(width, height);
    }
}
```

### Default Interface Methods
```ouro
interface ILogger {
    void Log(message: string);
    
    // Default implementation
    void LogError(message: string) {
        Log($"ERROR: {message}");
    }
}
```

## Generics

### Generic Classes
```ouro
class Box<T> {
    private T value;
    
    public Box(value: T) {
        this.value = value;
    }
    
    public T GetValue() => value;
    public void SetValue(value: T) => this.value = value;
}

// Usage
let intBox = new Box<int>(42);
let stringBox = new Box<string>("Hello");
```

### Generic Methods
```ouro
function swap<T>(ref a: T, ref b: T) {
    T temp = a;
    a = b;
    b = temp;
}

// Type inference
int x = 10, y = 20;
swap(ref x, ref y);  // T inferred as int
```

### Generic Constraints
```ouro
// Single constraint
function max<T>(a: T, b: T): T where T : IComparable<T> {
    return a.CompareTo(b) > 0 ? a : b;
}

// Multiple constraints
class Repository<T> where T : Entity, new() {
    public T Create() {
        return new T();
    }
}

// Constraint types
where T : class          // Reference type
where T : struct         // Value type
where T : new()          // Has parameterless constructor
where T : BaseClass      // Derives from BaseClass
where T : IInterface     // Implements IInterface
where T : U              // T same as or derives from U
```

## Pattern Matching

### Type Patterns
```ouro
object obj = GetValue();

if (obj is string str) {
    console.WriteLine($"String: {str}");
} else if (obj is int num) {
    console.WriteLine($"Number: {num}");
} else if (obj is null) {
    console.WriteLine("Null value");
}
```

### Switch Expressions
```ouro
let description = shape switch {
    Circle { Radius: var r } => $"Circle with radius {r}",
    Rectangle { Width: var w, Height: var h } => $"Rectangle {w}x{h}",
    Triangle t => $"Triangle with area {t.Area}",
    null => "No shape",
    _ => "Unknown shape"
};
```

### Property Patterns
```ouro
let category = person switch {
    { Age: < 13 } => "Child",
    { Age: >= 13 and < 20 } => "Teenager",
    { Age: >= 20 and < 60 } => "Adult",
    { Age: >= 60 } => "Senior",
    _ => "Unknown"
};
```

### Tuple Patterns
```ouro
let point = (x, y);
let location = point switch {
    (0, 0) => "Origin",
    (0, _) => "On Y-axis",
    (_, 0) => "On X-axis",
    (var a, var b) when a == b => "On diagonal",
    _ => "General position"
};
```

### List Patterns
```ouro
let sequence = [1, 2, 3, 4, 5];
let description = sequence switch {
    [] => "Empty",
    [var single] => $"Single: {single}",
    [var first, var second] => $"Pair: {first}, {second}",
    [var first, .., var last] => $"First: {first}, Last: {last}",
    _ => "Multiple elements"
};
```

## Error Handling

### Try-Catch-Finally
```ouro
try {
    // Code that may throw
    let result = riskyOperation();
} catch (SpecificException e) {
    console.WriteLine($"Specific error: {e.Message}");
} catch (Exception e) when (e.Message.Contains("timeout")) {
    console.WriteLine("Timeout occurred");
} catch (Exception e) {
    console.WriteLine($"General error: {e.Message}");
} finally {
    // Always executed
    cleanup();
}
```

### Throwing Exceptions
```ouro
if (value < 0) {
    throw new ArgumentException("Value must be non-negative", nameof(value));
}

// Rethrow
catch (Exception e) {
    LogError(e);
    throw;  // Preserves stack trace
}
```

### Custom Exceptions
```ouro
class ValidationException : Exception {
    public string Field { get; }
    public object Value { get; }
    
    public ValidationException(field: string, value: object, message: string) 
        : base(message) {
        Field = field;
        Value = value;
    }
}
```

### Using Statement
```ouro
// Automatic disposal
using (let file = new FileStream("data.txt", FileMode.Open)) {
    // Use file
}  // file.Dispose() called automatically

// Multiple resources
using (let file = new FileStream("data.txt", FileMode.Open))
using (let reader = new StreamReader(file)) {
    let content = reader.ReadToEnd();
}
```

## Memory Management

### Stack vs Heap
```ouro
// Stack allocation (value types)
int x = 10;
struct Point { int X; int Y; }
Point p = new Point { X = 10, Y = 20 };

// Heap allocation (reference types)
string str = "Hello";
Person person = new Person("Alice", 25);
int[] array = new int[100];
```

### Manual Memory Management (@low)
```ouro
@low
// Allocate memory
int* buffer = allocate<int>(100);

// Use memory
for (int i = 0; i < 100; i++) {
    buffer[i] = i * i;
}

// Free memory
free(buffer);

// Stack allocation
stackalloc int[10] temp;
```

### Garbage Collection
```ouro
// Force garbage collection (use sparingly)
GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();

// Weak references
WeakReference<LargeObject> weakRef = new WeakReference<LargeObject>(largeObj);

if (weakRef.TryGetTarget(out obj)) {
    // Object still alive
}
```

## Concurrency

### Async/Await
```ouro
async function fetchDataAsync(url: string): Task<string> {
    using (let client = new HttpClient()) {
        return await client.GetStringAsync(url);
    }
}

// Usage
async function processAsync() {
    let data = await fetchDataAsync("https://api.example.com");
    console.WriteLine(data);
}
```

### Tasks
```ouro
// Create and run task
let task = Task.Run(() => {
    // Background work
    return computeResult();
});

// Wait for completion
let result = await task;

// Multiple tasks
let tasks = urls.Select(url => fetchDataAsync(url));
let results = await Task.WhenAll(tasks);
```

### Parallel Programming
```ouro
// Parallel for
Parallel.For(0, 1000, i => {
    processItem(items[i]);
});

// Parallel foreach
Parallel.ForEach(items, item => {
    processItem(item);
});

// PLINQ
let results = items
    .AsParallel()
    .Where(x => x.IsValid)
    .Select(x => transform(x))
    .ToList();
```

### Thread Safety
```ouro
class Counter {
    private int count;
    private readonly object lockObj = new object();
    
    public void Increment() {
        lock (lockObj) {
            count++;
        }
    }
    
    public int Value {
        get {
            lock (lockObj) {
                return count;
            }
        }
    }
}

// Atomic operations
@low
atomic {
    sharedCounter++;
}
```

## Modules and Namespaces

### Namespace Declaration
```ouro
namespace MyApp.Core {
    class Service {
        // ...
    }
}

namespace MyApp.Core.Utils {
    class Helper {
        // ...
    }
}
```

### Using Directives
```ouro
using System;
using System.Collections.Generic;
using Ouroboros.StdLib.Math;

// Alias
using Dict = System.Collections.Generic.Dictionary;

// Static using
using static System.Math;
using static Ouroboros.StdLib.Math.MathFunctions;

// Global using (applies to entire project)
global using System;
global using Ouroboros.Core;
```

### Module System
```ouro
// Export
export class PublicClass {
    // ...
}

export function publicFunction() {
    // ...
}

// Import
import { PublicClass, publicFunction } from "./module";
import * as MyModule from "./module";
```

## Attributes

### Built-in Attributes
```ouro
[Obsolete("Use NewMethod instead")]
function oldMethod() { }

[Serializable]
class DataModel {
    [Required]
    public string Name { get; set; }
    
    [Range(0, 100)]
    public int Score { get; set; }
}

[TestClass]
class MyTests {
    [TestMethod]
    function testAddition() {
        assert(1 + 1 == 2);
    }
}
```

### Custom Attributes
```ouro
[AttributeUsage(AttributeTargets.Method)]
class BenchmarkAttribute : Attribute {
    public int Iterations { get; set; } = 1000;
}

class PerformanceTests {
    [Benchmark(Iterations = 10000)]
    function testMethod() {
        // ...
    }
}
```

## Preprocessor Directives

```ouro
#if DEBUG
    console.WriteLine("Debug mode");
#elif RELEASE
    console.WriteLine("Release mode");
#else
    console.WriteLine("Unknown mode");
#endif

#region Helper Methods
function helper1() { }
function helper2() { }
#endregion

#warning This code needs review
#error This code is not implemented yet
``` 
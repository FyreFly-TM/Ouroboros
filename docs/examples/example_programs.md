# Ouroboros Example Programs

This directory contains example programs demonstrating various features of the Ouroboros programming language.

## Basic Examples

### Hello World (All Syntax Levels)

```ouro
// High-level syntax
@high
print "Hello, World!" to console;

// Medium-level syntax
@medium
console.WriteLine("Hello, World!");

// Low-level syntax
@low
char* message = "Hello, World!\n";
write(1, message, 14);  // Write to stdout
```

### Variables and Types

```ouro
// Variable declarations
let name = "Ouroboros";        // Type inference
string language = "Ouroboros"; // Explicit type
int version = 1;
double pi = 3.14159;
bool isAwesome = true;

// Greek variables
let π = 3.14159;
let θ = π / 4;
let Σ = 0;

// Constants
const MAX_SIZE = 100;
const string GREETING = "Hello";
```

### Functions

```ouro
// Simple function
function add(a: int, b: int): int {
    return a + b;
}

// Generic function
function swap<T>(ref a: T, ref b: T) {
    T temp = a;
    a = b;
    b = temp;
}

// Lambda expressions
let multiply = (x, y) => x * y;
let square = x => x * x;

// High-level function definition
@high
define function calculateArea taking radius returning number as
    return π times radius squared;
end function
```

### Control Flow

```ouro
// If-else statement
if (score >= 90) {
    grade = "A";
} else if (score >= 80) {
    grade = "B";
} else {
    grade = "C";
}

// Pattern matching
let result = value switch {
    case 0 => "zero",
    case 1 => "one",
    case var x when x < 0 => "negative",
    case var x when x > 100 => "large",
    default => "other"
};

// Custom loops
repeat 5 times {
    console.WriteLine("Hello!");
}

iterate i: 1..10:2 {  // 1, 3, 5, 7, 9
    console.WriteLine(i);
}

forever {
    if (condition) break;
    // Do something
}
```

## Advanced Examples

### Object-Oriented Programming

```ouro
// Class definition
class Animal {
    protected string name;
    protected int age;
    
    public Animal(name: string, age: int) {
        this.name = name;
        this.age = age;
    }
    
    public virtual void Speak() {
        console.WriteLine($"{name} makes a sound");
    }
}

// Inheritance
class Dog : Animal {
    private string breed;
    
    public Dog(name: string, age: int, breed: string) : base(name, age) {
        this.breed = breed;
    }
    
    public override void Speak() {
        console.WriteLine($"{name} barks!");
    }
}

// Interface
interface IDrawable {
    void Draw();
    property Color Color { get; set; }
}

// Generic class
class Box<T> {
    private T value;
    
    public Box(value: T) {
        this.value = value;
    }
    
    public T GetValue() => value;
    public void SetValue(value: T) => this.value = value;
}
```

### Data-Oriented Programming

```ouro
// Component definition
component Position {
    float x;
    float y;
    float z;
}

component Velocity {
    float dx;
    float dy;
    float dz;
}

component Health {
    int current;
    int max;
}

// System definition
system MovementSystem {
    query<Position, Velocity> entities;
    
    function Update(deltaTime: float) {
        foreach (entity in entities) {
            entity.Position.x += entity.Velocity.dx * deltaTime;
            entity.Position.y += entity.Velocity.dy * deltaTime;
            entity.Position.z += entity.Velocity.dz * deltaTime;
        }
    }
}

// Entity creation
entity player {
    Position { x: 0, y: 0, z: 0 }
    Velocity { dx: 1, dy: 0, dz: 0 }
    Health { current: 100, max: 100 }
}
```

### Mathematical Operations

```ouro
using Ouroboros.StdLib.Math;

// Vector operations
let v1 = new Vector(3, 1.0, 2.0, 3.0);
let v2 = new Vector(3, 4.0, 5.0, 6.0);

let dot = v1.Dot(v2);
let cross = v1.Cross(v2);
let magnitude = v1.Magnitude;
let normalized = v1.Normalized();

// Matrix operations
let m1 = Matrix.Identity(4);
let m2 = Matrix.FromRotation(π/4, Vector3.Up);
let result = m1 * m2;

// Mathematical functions with Greek symbols
let α = 30.0 * MathFunctions.DegToRad;
let sinα = MathFunctions.Sin(α);
let cosα = MathFunctions.Cos(α);

// Summation using Greek notation
let Σ = ∑(i: 1..100) { i * i };  // Sum of squares
let Π = ∏(i: 1..5) { i };        // Factorial
```

### Parallel Programming

```ouro
// Parallel for loop
parallel for (i in 0..1000) {
    processItem(items[i]);
}

// Async operations
async function fetchData(url: string): Task<string> {
    let client = new HttpClient();
    return await client.GetStringAsync(url);
}

// Async for loop
async for (url in urls) {
    let data = await fetchData(url);
    processData(data);
}

// Thread-safe operations
@low
atomic {
    counter++;
    if (counter > MAX_COUNT) {
        counter = 0;
    }
}
```

### Error Handling

```ouro
// Try-catch-finally
try {
    let file = FileSystem.ReadAllText("data.txt");
    let data = parseData(file);
    processData(data);
} catch (FileNotFoundException e) {
    console.WriteLine($"File not found: {e.Message}");
} catch (ParseException e) {
    console.WriteLine($"Parse error: {e.Message}");
} finally {
    cleanup();
}

// Custom exceptions
class ValidationException : Exception {
    public string Field { get; }
    
    public ValidationException(field: string, message: string) : base(message) {
        Field = field;
    }
}

// Result type pattern
enum Result<T, E> {
    Ok(T),
    Error(E)
}

function divide(a: double, b: double): Result<double, string> {
    if (b == 0) {
        return Result.Error("Division by zero");
    }
    return Result.Ok(a / b);
}
```

### String Interpolation and Formatting

```ouro
let name = "Ouroboros";
let version = 1.0;

// Basic interpolation
let message = $"Welcome to {name} v{version}!";

// Format specifiers
let pi = 3.14159;
let formatted = $"Pi is approximately {pi:F2}";  // "3.14"

// Complex expressions
let result = $"Sum of 1 to 10 is {∑(i: 1..10) { i }}";

// Multi-line strings
let sql = $"""
    SELECT * FROM users
    WHERE age > {minAge}
    AND status = '{status}'
    ORDER BY created_at DESC
    """;
```

### UI Programming

```ouro
using Ouroboros.StdLib.UI;

// Create a window
let window = new Window {
    Title = "Ouroboros UI Example",
    Width = 800,
    Height = 600
};

// Create widgets
let button = new Button {
    Text = "Click Me!",
    X = 100,
    Y = 100,
    Width = 100,
    Height = 30
};

button.OnClick += (sender, args) => {
    console.WriteLine("Button clicked!");
};

let textBox = new TextBox {
    X = 100,
    Y = 150,
    Width = 200,
    Height = 25,
    Placeholder = "Enter text..."
};

// Layout
let panel = new Panel {
    Layout = new FlowLayout(Orientation.Vertical, spacing: 10)
};

panel.AddChild(button);
panel.AddChild(textBox);

window.RootWidget = panel;
window.Show();
```

### Low-Level Programming

```ouro
@low
// Manual memory management
int* array = allocate<int>(100);
for (int i = 0; i < 100; i++) {
    array[i] = i * i;
}

// Use the array
int sum = 0;
for (int i = 0; i < 100; i++) {
    sum += array[i];
}

// Clean up
free(array);

// Inline assembly
@asm {
    mov eax, [sum]
    add eax, 42
    mov [sum], eax
}

// Bit manipulation
uint flags = 0b00000000;
flags |= (1 << 3);  // Set bit 3
flags &= ~(1 << 2); // Clear bit 2
bool isSet = (flags & (1 << 3)) != 0;
```

## Complete Programs

### Calculator

```ouro
// Simple calculator with pattern matching
function calculate(op: string, a: double, b: double): double {
    return op switch {
        "+" => a + b,
        "-" => a - b,
        "*" => a * b,
        "/" => b != 0 ? a / b : throw new DivideByZeroException(),
        "^" => MathFunctions.Pow(a, b),
        "%" => a % b,
        _ => throw new InvalidOperationException($"Unknown operator: {op}")
    };
}

function main() {
    console.WriteLine("Simple Calculator");
    
    forever {
        console.Write("Enter expression (e.g., 5 + 3) or 'quit': ");
        let input = console.ReadLine();
        
        if (input.ToLower() == "quit") break;
        
        try {
            let parts = input.Split(' ');
            if (parts.Length != 3) {
                console.WriteLine("Invalid format. Use: number operator number");
                continue;
            }
            
            let a = double.Parse(parts[0]);
            let op = parts[1];
            let b = double.Parse(parts[2]);
            
            let result = calculate(op, a, b);
            console.WriteLine($"Result: {result}");
        } catch (Exception e) {
            console.WriteLine($"Error: {e.Message}");
        }
    }
}
```

### File Processor

```ouro
using Ouroboros.StdLib.IO;

// Process text files in a directory
function processFiles(directory: string, pattern: string = "*.txt") {
    let files = FileSystem.GetFiles(directory, pattern);
    
    console.WriteLine($"Found {files.Length} files to process");
    
    parallel for (file in files) {
        try {
            let content = FileSystem.ReadAllText(file);
            let lines = content.Split('\n');
            
            // Process each line
            let processed = lines
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .Select(line => processLine(line))
                .ToArray();
            
            // Save processed content
            let outputFile = file.Replace(".txt", "_processed.txt");
            FileSystem.WriteAllLines(outputFile, processed);
            
            console.WriteLine($"Processed: {FileSystem.GetFileName(file)}");
        } catch (Exception e) {
            console.WriteLine($"Error processing {file}: {e.Message}");
        }
    }
}

function processLine(line: string): string {
    // Example: Convert to uppercase and add line numbers
    static int lineNumber = 0;
    return $"{++lineNumber}: {line.ToUpper()}";
}
```

### Game Loop Example

```ouro
// Simple game loop with ECS
class Game {
    private List<System> systems;
    private bool running;
    private double targetFPS = 60.0;
    
    public Game() {
        systems = new List<System>();
        InitializeSystems();
    }
    
    private void InitializeSystems() {
        systems.Add(new MovementSystem());
        systems.Add(new RenderSystem());
        systems.Add(new CollisionSystem());
        systems.Add(new InputSystem());
    }
    
    public void Run() {
        running = true;
        let targetFrameTime = 1.0 / targetFPS;
        let lastTime = GetTime();
        
        while (running) {
            let currentTime = GetTime();
            let deltaTime = currentTime - lastTime;
            
            if (deltaTime >= targetFrameTime) {
                Update(deltaTime);
                Render();
                lastTime = currentTime;
            }
            
            // Handle events
            ProcessEvents();
        }
    }
    
    private void Update(deltaTime: double) {
        foreach (system in systems) {
            system.Update(deltaTime);
        }
    }
    
    private void Render() {
        // Clear screen
        Graphics.Clear(Color.Black);
        
        // Render all entities
        let renderSystem = systems.Find(s => s is RenderSystem) as RenderSystem;
        renderSystem?.Render();
        
        // Present
        Graphics.Present();
    }
    
    private void ProcessEvents() {
        while (Event.Poll(out event)) {
            if (event.Type == EventType.Quit) {
                running = false;
            }
        }
    }
}
``` 
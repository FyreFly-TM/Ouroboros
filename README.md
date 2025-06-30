# Ouroboros Programming Language

Ouroboros is a multi-paradigm programming language that seamlessly blends natural language programming, traditional syntax, and advanced mathematical notation. It features multiple syntax levels, a sophisticated type system, and built-in support for mathematical operations, UI development, and GPU computing.

## Features

### Multi-Level Syntax
- **High-Level (`@high`)**: Natural language-like syntax for intuitive programming
- **Medium-Level (`@medium`)**: Traditional programming syntax with modern features
- **Low-Level (`@low`)**: Systems programming with direct memory control
- **Assembly-Level (`@assembly`)**: Inline assembly and low-level optimizations

### Advanced Type System
- Static typing with type inference
- Generic types and constraints
- Custom units with automatic conversions
- Domain-specific type systems

### Mathematical Capabilities
- Native support for Unicode mathematical operators (∫, ∑, ∀, ∃, etc.)
- Built-in Vector, Matrix, and Quaternion types
- Complex number support
- Unit-aware calculations with automatic conversions

### Modern Language Features
- Pattern matching
- Lambda expressions
- Async/await
- Component-Entity-System architecture
- Memory safety features
- GPU kernel programming

## Example Programs

### Hello World
```ouroboros
@high
print "Hello, World!"
```

### Scientific Calculator
See the `calculator_*.ouro` files for comprehensive examples:
- `calculator.ouro` - Full GUI scientific calculator
- `calculator_simple.ouro` - Natural language console calculator
- `calculator_ui.ouro` - Component-based GUI calculator
- `calculator_advanced.ouro` - Advanced calculator with units and domains

### Mathematical Operations
```ouroboros
@medium
// Natural mathematical notation
var integral = ∫(0, π) sin(x) dx;
var sum = ∑(i=1, 100) i²;
var limit = lim(x→0) (sin(x)/x);

// Vector operations
var v1 = Vector(1, 2, 3);
var v2 = Vector(4, 5, 6);
var dot = v1 · v2;
var cross = v1 × v2;

// Unit-aware calculations
var distance = 100[m];
var time = 10[s];
var speed = distance / time; // 10[m/s]
var mph = speed in [mph];     // Convert to miles per hour
```

## Project Structure

```
Ouroboros/
├── src/
│   ├── core/           # Core compiler components
│   │   ├── ast/        # Abstract Syntax Tree
│   │   ├── compiler/   # Compiler implementation
│   │   ├── lexer/      # Lexical analyzer
│   │   ├── parser/     # Parser implementation
│   │   └── vm/         # Virtual machine
│   ├── stdlib/         # Standard library
│   │   ├── math/       # Mathematical functions
│   │   ├── ui/         # UI components
│   │   ├── collections/# Data structures
│   │   └── system/     # System utilities
│   └── optimization/   # Code optimization
├── tools/              # Development tools
│   ├── debug/          # Debugger
│   ├── profile/        # Profiler
│   └── opm/            # Package manager
└── examples/           # Example programs

```

## Building and Running

### Prerequisites
- .NET 6.0 or later
- Windows (for UI features)

### Build
```bash
# Build the compiler
dotnet build

# Run a program
ouro run program.ouro

# Compile to bytecode
ouro compile program.ouro -o program.ob
```

### Tools
```bash
# Debug a program
ouro-debug program.ouro

# Profile performance
ouro-profile program.ouro

# Manage packages
opm install package-name
```

## Language Syntax Examples

### High-Level Syntax
```ouroboros
@high

define fibonacci taking n:
    if n ≤ 1 then return n
    return fibonacci of (n - 1) + fibonacci of (n - 2)
end

print "Fibonacci of 10 is " + fibonacci of 10
```

### Medium-Level Syntax
```ouroboros
@medium

class Person {
    name: string;
    age: int;
    
    function greet() {
        print($"Hello, I'm {name} and I'm {age} years old");
    }
}

var person = new Person { name = "Alice", age = 30 };
person.greet();
```

### Low-Level Syntax
```ouroboros
@low

unsafe {
    int* ptr = stackalloc int[10];
    for (int i = 0; i < 10; i++) {
        ptr[i] = i * i;
    }
}
```

## Documentation

For detailed documentation, see:
- [Language Reference](docs/language-reference.md)
- [Standard Library](docs/stdlib-reference.md)
- [Tutorial](docs/tutorial.md)

## Contributing

Contributions are welcome! Please read our [Contributing Guide](CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Inspired by various programming languages including Python, C#, Rust, and mathematical notation systems
- Special thanks to all contributors who have helped shape Ouroboros

---

**Note**: Ouroboros is a research/educational language demonstrating advanced programming language concepts and features. 
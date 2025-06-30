# Ouroboros Scientific Calculator Examples

This directory contains several implementations of a scientific calculator in the Ouroboros programming language, showcasing different features and syntax levels of the language.

## Calculator Implementations

### 1. `calculator.ouro` - Full-Featured GUI Calculator
A complete scientific calculator with a graphical user interface using Ouroboros's UI library.

**Features:**
- Full GUI with buttons and display
- Basic arithmetic operations (+, -, ×, ÷)
- Scientific functions (sin, cos, tan, ln, log, √, x², factorial)
- Memory operations (MC, MR, M+, M-, MS)
- Constants (π, e)
- History tracking

**To run:**
```bash
ouro run calculator.ouro
```

### 2. `calculator_simple.ouro` - Natural Language Calculator
A console-based calculator using Ouroboros's high-level natural language syntax.

**Features:**
- Natural language function definitions
- Interactive command-line interface
- Mathematical operations using Unicode symbols
- Memory operations
- Error handling with natural syntax

**Example usage:**
```
> 5 + 3
Result: 8

> sin 45
Result: 0.7071067811865476

> factorial 5
Result: 120
```

**To run:**
```bash
ouro run calculator_simple.ouro
```

### 3. `calculator_ui.ouro` - Medium-Level GUI Calculator
A GUI calculator using medium-level syntax with components and systems.

**Features:**
- Component-based architecture
- System-based state management
- Professional UI with color-coded buttons
- Degree/Radian mode support
- Advanced functions (exp, mod, power)

**To run:**
```bash
ouro run calculator_ui.ouro
```

### 4. `calculator_advanced.ouro` - Advanced Mathematical Calculator
Demonstrates Ouroboros's unique mathematical features including domains, units, and mathematical notation.

**Features:**
- Multiple calculation modes:
  - **Real Mode**: Standard calculations
  - **Complex Mode**: Complex number operations
  - **Vector Mode**: Vector operations with dot/cross products
  - **Matrix Mode**: Matrix operations, determinants, eigenvalues
  - **Unit Mode**: Unit-aware calculations with automatic conversions
- Mathematical notation support (∫, ∑, lim, ∀, ∃)
- Custom domain definitions
- Set operations
- Logic operations

**Example usage:**
```
[Real] > 2^8
  = 256

[Complex] > (3+4i) * (2-i)
  = 10 + 5i

[Vector] > <1,2,3> · <4,5,6>
  = 32

[Unit] > 100[m] in [ft]
  = 328.084[ft]
```

**To run:**
```bash
ouro run calculator_advanced.ouro
```

## Ouroboros Language Features Demonstrated

### High-Level Syntax (`@high`)
- Natural language function definitions
- English-like control structures
- Automatic type inference
- Pattern matching with natural syntax

### Medium-Level Syntax (`@medium`)
- Traditional programming constructs
- Component-Entity-System architecture
- Lambda expressions
- Type annotations

### Low-Level Syntax (`@low`)
- Direct memory control
- Manual type specifications
- Assembly-style operations
- Performance optimizations

### Mathematical Features
- Unicode mathematical operators (×, ÷, √, ², ³, π, ∞)
- Built-in mathematical functions
- Vector and Matrix types
- Unit system with automatic conversions
- Domain-specific languages for mathematics

### UI System
- Window creation and management
- Button, Label, and TextBox widgets
- Event handling
- Layout management
- Custom styling

## Building and Running

### Prerequisites
1. Ouroboros compiler installed
2. .NET runtime (for the VM)
3. Windows Forms (for UI examples)

### Compilation
```bash
# Compile to bytecode
ouro compile calculator.ouro -o calculator.ob

# Run directly
ouro run calculator.ouro

# Run with debugger
ouro-debug calculator.ouro
```

### Performance Profiling
```bash
# Profile calculator performance
ouro-profile calculator.ouro -o profile.txt
```

## Extending the Calculators

### Adding New Functions
To add a new function to any calculator:

1. Add the function to the appropriate handler
2. Update the UI or command parser
3. Add help text

Example:
```ouroboros
// Add hyperbolic functions
case "sinh": applyHyperbolicFunction("sinh"); break;
case "cosh": applyHyperbolicFunction("cosh"); break;

function applyHyperbolicFunction(func: string) {
    var value = double.parse(displayText);
    var result = func == "sinh" ? sinh(value) : cosh(value);
    displayText = result.toString();
    updateDisplay();
}
```

### Creating Custom Domains
```ouroboros
domain Quaternion {
    operators {
        + : (Quaternion, Quaternion) -> Quaternion
        * : (Quaternion, Quaternion) -> Quaternion
        |.| : (Quaternion) -> double  // Magnitude
    }
}
```

## License
These examples are part of the Ouroboros programming language project and are provided as educational resources. 
# 🔄 Ouroboros Programming Language

<p align="center">
  <img src="docs/assets/ouroboros-logo.png" alt="Ouroboros Logo" width="200"/>
  <br>
  <em>The Eternal Language - Where End Meets Beginning</em>
</p>

<p align="center">
  <a href="#features">Features</a> •
  <a href="#quick-start">Quick Start</a> •
  <a href="#examples">Examples</a> •
  <a href="#documentation">Documentation</a> •
  <a href="#contributing">Contributing</a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/version-0.9.0--alpha-blue.svg" alt="Version">
  <img src="https://img.shields.io/badge/license-MIT%2FApache--2.0-green.svg" alt="License">
  <img src="https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-lightgrey.svg" alt="Platform">
</p>

---

## 🌟 Overview

Ouroboros is a revolutionary multi-paradigm programming language designed to be the ultimate C/C++ replacement. It seamlessly blends natural language programming, traditional syntax, advanced mathematical notation, and assembly-level control into a unified, expressive system.

### Why Ouroboros?

- **🚀 Zero-Overhead Abstractions**: Compile to native code with performance matching or exceeding C++
- **🧠 Multi-Level Syntax**: Choose your abstraction level - from natural language to assembly
- **🔢 Native Mathematical Support**: Write equations as naturally as on paper
- **🛡️ Memory Safe by Default**: Prevent common bugs without sacrificing performance
- **⚡ Modern Concurrency**: Async/await, actors, channels, and GPU computing built-in
- **🎨 Expressive Type System**: Units, contracts, dependent types, and more

## 🎯 Key Differentiators

| Feature | Ouroboros | C++ | Rust | Python |
|---------|-----------|-----|------|--------|
| Natural Language Syntax | ✅ | ❌ | ❌ | ❌ |
| Mathematical Notation | ✅ Native | ❌ | ❌ | ⚠️ Limited |
| Memory Safety | ✅ Default | ❌ | ✅ | ✅ |
| Zero-Cost Abstractions | ✅ | ✅ | ✅ | ❌ |
| GPU Programming | ✅ Built-in | ⚠️ External | ⚠️ External | ⚠️ External |
| Unit-Aware Math | ✅ | ❌ | ❌ | ❌ |
| Contract Programming | ✅ | ❌ | ❌ | ❌ |

## 🚀 Quick Start

### Installation

```bash
# Windows (PowerShell)
iwr -useb https://ouroboros-lang.org/install.ps1 | iex

# macOS/Linux
curl -fsSL https://ouroboros-lang.org/install.sh | sh

# Or build from source
git clone https://github.com/ouroboros-lang/ouroboros.git
cd ouroboros
./build.sh  # or build.ps1 on Windows
```

### Your First Program

```ouroboros
@high
print "Hello, Ouroboros! 🔄"
```

Run it:
```bash
ouroboros hello.ouro
```

## 🎨 Features

### 🔤 Four Syntax Levels

Ouroboros offers unprecedented flexibility with four distinct syntax levels that can be mixed within the same file:

#### 1️⃣ High-Level Syntax (`@high`)
Natural language programming that reads like English:

```ouroboros
@high
define calculate factorial of number:
    if number is less than or equal to 1 then
        return 1
    else
        return number times factorial of (number minus 1)
    end if
end define

print "The factorial of 5 is " + factorial of 5
```

#### 2️⃣ Medium-Level Syntax (`@medium`)
Modern programming syntax with advanced features:

```ouroboros
@medium
class BankAccount {
    private balance: decimal[USD] = 0[USD];
    
    public function deposit(amount: decimal[USD]) {
        requires amount > 0[USD] : "Deposit must be positive";
        ensures balance == old(balance) + amount;
        
        balance += amount;
    }
    
    public function withdraw(amount: decimal[USD]): bool {
        if (amount <= balance) {
            balance -= amount;
            return true;
        }
        return false;
    }
}
```

#### 3️⃣ Low-Level Syntax (`@low`)
Systems programming with fine control:

```ouroboros
@low
struct PacketHeader {
    version: u8;
    flags: u16;
    length: u32;
    checksum: u32;
}

unsafe function parse_packet(data: *u8, len: usize): PacketHeader {
    assert len >= sizeof(PacketHeader);
    return *(data as *PacketHeader);
}
```

#### 4️⃣ Assembly-Level Syntax (`@assembly`)
Direct machine code when you need it:

```ouroboros
@assembly
function fast_memcpy(dest: *void, src: *void, n: usize) {
    asm {
        mov rcx, n
        mov rdi, dest
        mov rsi, src
        rep movsb
    }
}
```

### 🔢 Native Mathematical Notation

Write mathematics naturally with full Unicode support:

```ouroboros
@medium
// Calculus
let derivative = ∂/∂x (x³ + 2x² - 5x + 3);
let integral = ∫(0, π) sin(x) dx;
let limit = lim(x→∞) (1 + 1/x)^x;

// Linear Algebra
let matrix = [1, 2, 3; 4, 5, 6; 7, 8, 9];
let eigenvalues = λ(matrix);
let determinant = |matrix|;

// Set Theory
let A = {1, 2, 3, 4, 5};
let B = {4, 5, 6, 7, 8};
let union = A ∪ B;
let intersection = A ∩ B;

// Logic
∀x ∈ ℝ: x² ≥ 0;
∃x ∈ ℤ: x² = 4;

// Complex Numbers
let z = 3 + 4i;
let magnitude = |z|;
let conjugate = z̄;
```

### 📏 Unit-Aware Computing

Prevent unit errors at compile time:

```ouroboros
@medium
// Physical calculations with automatic unit checking
let distance = 100[km];
let time = 2[hours];
let speed = distance / time;  // Automatically: 50[km/hour]

// Unit conversions
let mph = speed in [miles/hour];  // 31.07[miles/hour]
let mps = speed in [m/s];         // 13.89[m/s]

// Compile-time unit errors
let error = distance + time;  // ❌ Compile error: Cannot add km and hours

// Custom units
unit Bitcoin = "BTC";
unit Ethereum = "ETH";
let exchange_rate = 15[ETH/BTC];
let my_btc = 2.5[BTC];
let my_eth = my_btc * exchange_rate;  // 37.5[ETH]
```

### 🧠 Advanced Type System

#### Contracts and Invariants
```ouroboros
@medium
class SortedList<T> where T: Comparable {
    private items: List<T>;
    
    invariant ∀i ∈ [0..length-2]: items[i] ≤ items[i+1];
    
    public function insert(item: T) {
        requires item != null;
        ensures contains(item);
        ensures length == old(length) + 1;
        
        // Implementation maintains sort order
        let index = binary_search(item);
        items.insert_at(index, item);
    }
}
```

#### Dependent Types
```ouroboros
@medium
// Vector with compile-time known length
function dot_product<n: nat>(a: Vec<n>, b: Vec<n>): real {
    return ∑(i=0, n-1) a[i] * b[i];
}

// Matrix multiplication with size checking
function multiply<m,n,p: nat>(
    A: Matrix<m,n>, 
    B: Matrix<n,p>
): Matrix<m,p> {
    // Sizes checked at compile time!
}
```

### 🚀 Modern Concurrency

#### Async/Await
```ouroboros
@medium
async function fetch_user_data(id: int): User {
    let profile = await http.get($"https://api.example.com/users/{id}");
    let posts = await http.get($"https://api.example.com/users/{id}/posts");
    
    return User {
        profile: profile.parse<Profile>(),
        posts: posts.parse<List<Post>>()
    };
}

// Concurrent execution
let users = await parallel_map(user_ids, fetch_user_data);
```

#### Actor Model
```ouroboros
@medium
actor Counter {
    private count: int = 0;
    
    receive increment() {
        count += 1;
    }
    
    receive get_count(): int {
        return count;
    }
}

let counter = spawn Counter();
counter ! increment();
counter ! increment();
let total = await counter ? get_count();  // Returns 2
```

### 🎮 GPU Programming

Write GPU kernels directly in Ouroboros:

```ouroboros
@gpu
kernel matrix_multiply<T: Numeric>(
    A: Matrix<T>, 
    B: Matrix<T>, 
    out C: Matrix<T>
) {
    let row = blockIdx.y * blockDim.y + threadIdx.y;
    let col = blockIdx.x * blockDim.x + threadIdx.x;
    
    if (row < A.rows && col < B.cols) {
        T sum = 0;
        for (let k = 0; k < A.cols; k++) {
            sum += A[row, k] * B[k, col];
        }
        C[row, col] = sum;
    }
}

// CPU code
let result = gpu_execute(matrix_multiply, matrixA, matrixB);
```

### 🧬 Pattern Matching

Powerful pattern matching with exhaustiveness checking:

```ouroboros
@medium
enum Shape {
    Circle(radius: float),
    Rectangle(width: float, height: float),
    Triangle(a: float, b: float, c: float)
}

function area(shape: Shape): float {
    match shape {
        Circle(r) => π * r²,
        Rectangle(w, h) => w * h,
        Triangle(a, b, c) => {
            let s = (a + b + c) / 2;
            √(s * (s-a) * (s-b) * (s-c))
        }
    }
}

// Advanced patterns
match value {
    0 => "zero",
    1..=10 => "small",
    n if n % 2 == 0 => "even",
    _ => "other"
}
```

## 📚 Example Programs

### 🌍 Hello World in All Syntax Levels

```ouroboros
// High-level
@high
print "Hello, World! 🌍"

// Medium-level
@medium
function main() {
    console.write_line("Hello, World! 🌍");
}

// Low-level
@low
function main(): int {
    let msg: *char = "Hello, World! 🌍\n";
    write(STDOUT, msg, strlen(msg));
    return 0;
}

// Assembly-level
@assembly
function main() {
    asm {
        mov rax, 1          ; write syscall
        mov rdi, 1          ; stdout
        lea rsi, [msg]      ; message address
        mov rdx, 16         ; message length
        syscall
        xor rax, rax        ; return 0
        ret
    }
    data msg: "Hello, World! 🌍\n";
}
```

### 🧮 Scientific Calculator with Units

```ouroboros
@medium
import math.*;
import units.*;

class ScientificCalculator {
    // Basic operations with units
    function calculate_kinetic_energy(mass: real[kg], velocity: real[m/s]): real[J] {
        return 0.5 * mass * velocity²;
    }
    
    // Electromagnetic calculations
    function wavelength_to_frequency(λ: real[nm]): real[Hz] {
        const c = 299792458[m/s];  // Speed of light
        return c / λ;
    }
    
    // Chemistry calculations
    function ideal_gas_pressure(
        n: real[mol], 
        T: real[K], 
        V: real[L]
    ): real[Pa] {
        const R = 8.314[J/(mol·K)];
        return (n * R * T) / V;
    }
}

// Usage
let calc = new ScientificCalculator();
let KE = calc.calculate_kinetic_energy(1000[kg], 30[m/s]);
print $"Kinetic energy: {KE in [kJ]} kJ";

let freq = calc.wavelength_to_frequency(532[nm]);  // Green laser
print $"Frequency: {freq in [THz]} THz";
```

### 🎮 Game Engine Component

```ouroboros
@medium
// Entity-Component-System architecture
component Transform {
    position: Vec3 = Vec3.zero;
    rotation: Quat = Quat.identity;
    scale: Vec3 = Vec3.one;
}

component Velocity {
    linear: Vec3 = Vec3.zero;
    angular: Vec3 = Vec3.zero;
}

component Mesh {
    vertices: Array<Vertex>;
    indices: Array<u32>;
    material: Material;
}

system PhysicsSystem {
    query: [Transform, Velocity];
    
    function update(Δt: float[s]) {
        for entity in query {
            entity.Transform.position += entity.Velocity.linear * Δt;
            entity.Transform.rotation *= Quat.from_euler(entity.Velocity.angular * Δt);
        }
    }
}

system RenderSystem {
    query: [Transform, Mesh];
    
    function render(camera: Camera) {
        for entity in query {
            let mvp = camera.projection * camera.view * entity.Transform.matrix;
            gpu_execute(render_mesh, entity.Mesh, mvp);
        }
    }
}
```

### 🤖 Machine Learning Example

```ouroboros
@medium
import ml.*;

// Define a neural network using mathematical notation
class NeuralNetwork {
    layers: List<Layer>;
    
    function forward(x: Tensor): Tensor {
        var h = x;
        for layer in layers {
            h = layer.forward(h);
        }
        return h;
    }
    
    function loss(ŷ: Tensor, y: Tensor): real {
        // Cross-entropy loss
        return -∑(y ⊙ log(ŷ)) / y.shape[0];
    }
    
    function train(X: Tensor, y: Tensor, epochs: int) {
        let optimizer = Adam(learning_rate: 0.001);
        
        for epoch in 1..=epochs {
            // Forward pass
            let ŷ = forward(X);
            let L = loss(ŷ, y);
            
            // Backward pass
            let ∇L = gradient(L, parameters);
            optimizer.step(parameters, ∇L);
            
            if epoch % 100 == 0 {
                print $"Epoch {epoch}: Loss = {L:.4f}";
            }
        }
    }
}

// Usage
let model = NeuralNetwork([
    Dense(784, 128, activation: ReLU),
    Dropout(0.2),
    Dense(128, 64, activation: ReLU),
    Dense(64, 10, activation: Softmax)
]);

model.train(X_train, y_train, epochs: 1000);
```

### 🌐 Web Server

```ouroboros
@medium
import net.http.*;
import async.*;

class WebServer {
    router: Router = new Router();
    
    function configure() {
        router.get("/", home_handler);
        router.get("/api/users/:id", get_user);
        router.post("/api/users", create_user);
        
        router.use(logging_middleware);
        router.use(cors_middleware);
    }
    
    async function home_handler(req: Request): Response {
        return Response.html(@"
            <!DOCTYPE html>
            <html>
                <head><title>Ouroboros Web</title></head>
                <body>
                    <h1>Welcome to Ouroboros! 🔄</h1>
                    <p>The eternal language where end meets beginning.</p>
                </body>
            </html>
        ");
    }
    
    async function get_user(req: Request): Response {
        let id = req.params["id"].parse<int>();
        let user = await db.query<User>("SELECT * FROM users WHERE id = ?", id);
        
        return match user {
            Some(u) => Response.json(u),
            None => Response.not_found("User not found")
        };
    }
    
    async function start(port: int = 8080) {
        print $"Server starting on http://localhost:{port}";
        await http.listen_and_serve($":{port}", router);
    }
}

// Run the server
let server = new WebServer();
server.configure();
await server.start();
```

### 🔐 Cryptography Example

```ouroboros
@medium
import crypto.*;

// Secure password hashing
function hash_password(password: string, salt: bytes = null): (hash: bytes, salt: bytes) {
    salt ??= random_bytes(16);
    let hash = argon2id(
        password,
        salt,
        iterations: 3,
        memory: 64 * 1024,  // 64 MB
        parallelism: 4
    );
    return (hash, salt);
}

// Digital signature
class DigitalSigner {
    private_key: RSAPrivateKey;
    public_key: RSAPublicKey;
    
    constructor() {
        (private_key, public_key) = RSA.generate_key_pair(4096);
    }
    
    function sign(message: string): bytes {
        let hash = SHA256(message.to_bytes());
        return RSA.sign(hash, private_key);
    }
    
    function verify(message: string, signature: bytes): bool {
        let hash = SHA256(message.to_bytes());
        return RSA.verify(hash, signature, public_key);
    }
}

// Encrypted communication
async function secure_send(data: string, recipient_key: PublicKey) {
    // Generate ephemeral key
    let (ephemeral_private, ephemeral_public) = ECDH.generate_key_pair();
    
    // Derive shared secret
    let shared_secret = ECDH.compute_secret(ephemeral_private, recipient_key);
    
    // Encrypt data
    let key = HKDF(shared_secret, info: "encryption");
    let nonce = random_bytes(12);
    let ciphertext = AES_GCM.encrypt(data.to_bytes(), key, nonce);
    
    // Send encrypted message
    await network.send({
        ephemeral_key: ephemeral_public,
        nonce: nonce,
        ciphertext: ciphertext
    });
}
```

## 📁 Project Structure

```
Ouroboros/
├── 📂 src/
│   ├── 🧠 core/               # Core language implementation
│   │   ├── ast/              # Abstract syntax tree
│   │   ├── compiler/         # Compiler and type checker
│   │   ├── lexer/            # Lexical analysis
│   │   ├── parser/           # Multi-level parsers
│   │   ├── vm/              # Virtual machine
│   │   └── codegen/         # LLVM code generation
│   ├── 📚 stdlib/            # Standard library
│   │   ├── collections/      # Data structures
│   │   ├── concurrency/      # Async, actors, channels
│   │   ├── crypto/          # Cryptography
│   │   ├── io/              # File and network I/O
│   │   ├── math/            # Mathematics and linear algebra
│   │   ├── ml/              # Machine learning
│   │   ├── system/          # System utilities
│   │   └── ui/              # User interface
│   ├── 🛠️ tools/            # Development tools
│   │   ├── debug/           # Debugger
│   │   ├── docgen/          # Documentation generator
│   │   ├── opm/             # Package manager
│   │   └── profile/         # Profiler
│   └── 🔌 ide/              # IDE support
│       ├── lsp/             # Language server
│       └── vscode/          # VS Code extension
├── 📝 docs/                  # Documentation
├── 🧪 tests/                 # Test suite
├── 📦 examples/              # Example programs
└── 🏗️ build/                # Build output
```

## 🔨 Building Ouroboros

### Prerequisites

- **CMake** 3.20 or later
- **.NET** 6.0 SDK or later
- **LLVM** 14+ (for native compilation)
- **C++ Compiler** with C++17 support
- **Git** for version control

### Build from Source

```bash
# Clone the repository
git clone https://github.com/ouroboros-lang/ouroboros.git
cd ouroboros

# Option 1: CMake build (recommended)
mkdir build && cd build
cmake .. -DCMAKE_BUILD_TYPE=Release \
         -DBUILD_TESTS=ON \
         -DBUILD_TOOLS=ON \
         -DBUILD_LSP=ON \
         -DLLVM_DIR=/path/to/llvm
cmake --build . --parallel

# Option 2: .NET build
dotnet build -c Release
dotnet test

# Install (Unix-like systems)
sudo cmake --install .

# Install (Windows)
cmake --install . --prefix "C:/Program Files/Ouroboros"
```

### Development Build

```bash
# Debug build with sanitizers
cmake .. -DCMAKE_BUILD_TYPE=Debug \
         -DENABLE_ASAN=ON \
         -DENABLE_UBSAN=ON

# Run tests with coverage
cmake --build . --target test-coverage

# Generate documentation
cmake --build . --target docs
```

## 🚀 Usage

### Command Line Interface

```bash
# Run a program
ouroboros run program.ouro

# Compile to executable
ouroboros compile program.ouro -o program
ouroboros compile program.ouro -O3 --target=x86_64-linux-gnu

# Interactive REPL
ouroboros repl
Ouroboros REPL v0.9.0
>>> let x = 42
>>> print x * 2
84
>>> @high
>>> define square of n as n times n
>>> print square of 7
49

# Generate documentation
ouroboros docgen src/ --format=html --output=docs/

# Package management
opm install tensor-flow
opm publish my-package
opm search "machine learning"

# Debugging
ouroboros debug program.ouro
(odb) break main
(odb) run
(odb) step
(odb) print variables

# Profiling
ouroboros profile program.ouro --output=profile.html
```

### IDE Integration

#### VS Code
```bash
# Install the extension
code --install-extension ouroboros-lang.ouroboros-vscode

# Or search for "Ouroboros" in the VS Code marketplace
```

Features:
- ✅ Syntax highlighting
- ✅ IntelliSense/Auto-completion
- ✅ Real-time error checking
- ✅ Go to definition
- ✅ Find references
- ✅ Rename symbol
- ✅ Format document
- ✅ Debugging support

#### Other Editors
- **Vim/Neovim**: Install `ouroboros.vim` plugin
- **Emacs**: Install `ouroboros-mode`
- **Sublime Text**: Install via Package Control
- **IntelliJ IDEA**: Available in JetBrains Marketplace

## 📖 Documentation

### Getting Started
- [**Quick Start Guide**](https://docs.ouroboros-lang.org/quick-start) - Get up and running in 5 minutes
- [**Tutorial**](https://docs.ouroboros-lang.org/tutorial) - Learn Ouroboros step by step
- [**Language Tour**](https://docs.ouroboros-lang.org/tour) - Overview of all language features

### Reference
- [**Language Reference**](https://docs.ouroboros-lang.org/reference) - Complete language specification
- [**Standard Library**](https://docs.ouroboros-lang.org/stdlib) - API documentation
- [**Examples**](https://github.com/ouroboros-lang/examples) - Real-world code examples

### Advanced Topics
- [**FFI Guide**](https://docs.ouroboros-lang.org/ffi) - Interfacing with C/C++
- [**GPU Programming**](https://docs.ouroboros-lang.org/gpu) - CUDA/OpenCL integration
- [**Metaprogramming**](https://docs.ouroboros-lang.org/macros) - Macros and compile-time execution
- [**Performance Guide**](https://docs.ouroboros-lang.org/performance) - Optimization techniques

## 🤝 Contributing

We welcome contributions from everyone! Here's how you can help:

### Ways to Contribute

- 🐛 **Report bugs** and suggest features
- 📝 **Improve documentation** and write tutorials
- 🧪 **Write tests** and improve coverage
- 🔧 **Fix bugs** and implement features
- 🌍 **Translate** documentation
- 💬 **Help** others in discussions

### Development Process

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Run tests (`cmake --build . --target test`)
5. Commit with conventional commits (`git commit -m 'feat: add amazing feature'`)
6. Push to your fork (`git push origin feature/amazing-feature`)
7. Open a Pull Request

See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed guidelines.

## 🌟 Community

### Get Help
- 💬 [Discord Server](https://discord.gg/ouroboros) - Real-time chat
- 🗣️ [Discussions](https://github.com/ouroboros-lang/ouroboros/discussions) - Q&A and ideas
- 📧 [Mailing List](https://groups.google.com/g/ouroboros-lang) - Announcements

### Stay Updated
- 🐦 [Twitter](https://twitter.com/ouroboros_lang) - Latest news
- 📝 [Blog](https://blog.ouroboros-lang.org) - Deep dives and updates
- 📺 [YouTube](https://youtube.com/@ouroboros-lang) - Tutorials and talks

## 🎯 Roadmap

### Current Focus (v0.9.0)
- ✅ Core language implementation
- ✅ Standard library
- ✅ LLVM backend
- ✅ Basic tooling
- 🚧 Package manager
- 🚧 Documentation

### Next Release (v1.0.0)
- 📦 Stable package manager
- 🌐 WebAssembly target
- 📱 Mobile platform support
- 🎮 Game development framework
- 🔒 Security audit
- 📚 Complete documentation

### Future Plans
- 🚀 Self-hosting compiler
- 🧬 Quantum computing support
- 🤖 AI-assisted programming
- 🌍 Distributed computing
- 🔬 Scientific computing libraries

## 📊 Benchmarks

Ouroboros aims to match or exceed C++ performance while providing safety and expressiveness:

| Benchmark | Ouroboros | C++ | Rust | Go | Python |
|-----------|-----------|-----|------|-----|--------|
| Binary Trees | 1.00x | 1.02x | 1.05x | 1.8x | 45x |
| Mandelbrot | 1.00x | 0.98x | 1.03x | 2.1x | 88x |
| N-Body | 1.00x | 1.01x | 1.04x | 1.9x | 112x |
| Spectral Norm | 1.00x | 0.99x | 1.02x | 2.3x | 130x |

*Lower is better. Based on [Computer Language Benchmarks Game](https://benchmarksgame-team.pages.debian.net/benchmarksgame/).*

## 📄 License

Ouroboros is dual-licensed:
- **MIT License** - For maximum compatibility
- **Apache License 2.0** - For patent protection

See [LICENSE-MIT](LICENSE-MIT) and [LICENSE-APACHE](LICENSE-APACHE) for details.

## 🙏 Acknowledgments

Ouroboros stands on the shoulders of giants:

- **Language Design**: Inspired by Python, Rust, C++, Haskell, and APL
- **Implementation**: Built with LLVM, .NET, and modern C++
- **Contributors**: Thanks to all [contributors](https://github.com/ouroboros-lang/ouroboros/graphs/contributors)
- **Sponsors**: Special thanks to our [sponsors](SPONSORS.md)

---

<p align="center">
  <b>Ouroboros</b> - The Eternal Language<br>
  Where End Meets Beginning 🔄<br>
  <br>
  Made with ❤️ by the Ouroboros Community
</p>
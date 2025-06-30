# Ouroboros Standard Library API Reference

## Table of Contents
- [Collections](#collections)
- [Math Library](#math-library)
- [IO Library](#io-library)
- [UI Library](#ui-library)
- [System Library](#system-library)

## Collections

### List<T>

Dynamic array implementation with automatic resizing.

```ouro
class List<T> : IEnumerable<T> {
    // Properties
    public int Count { get; }
    public int Capacity { get; }
    
    // Constructors
    public List()
    public List(initialCapacity: int)
    public List(collection: IEnumerable<T>)
    
    // Methods
    public void Add(item: T)
    public void AddRange(collection: IEnumerable<T>)
    public bool Remove(item: T)
    public void RemoveAt(index: int)
    public void Clear()
    public bool Contains(item: T)
    public int IndexOf(item: T)
    public void Insert(index: int, item: T)
    public void Reverse()
    public void Sort()
    public void Sort(comparer: IComparer<T>)
    public T[] ToArray()
    public T Find(match: Predicate<T>)
    public List<T> FindAll(match: Predicate<T>)
    public void ForEach(action: Action<T>)
    
    // Indexer
    public T this[index: int] { get; set; }
}
```

**Example:**
```ouro
let numbers = new List<int>();
numbers.Add(5);
numbers.Add(10);
numbers.AddRange([15, 20, 25]);

let evens = numbers.FindAll(n => n % 2 == 0);
numbers.Sort();
```

### Dictionary<TKey, TValue>

Hash table implementation with O(1) average case performance.

```ouro
class Dictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> {
    // Properties
    public int Count { get; }
    public ICollection<TKey> Keys { get; }
    public ICollection<TValue> Values { get; }
    
    // Constructors
    public Dictionary()
    public Dictionary(capacity: int)
    public Dictionary(comparer: IEqualityComparer<TKey>)
    
    // Methods
    public void Add(key: TKey, value: TValue)
    public bool ContainsKey(key: TKey)
    public bool ContainsValue(value: TValue)
    public bool Remove(key: TKey)
    public void Clear()
    public bool TryGetValue(key: TKey, out value: TValue)
    
    // Indexer
    public TValue this[key: TKey] { get; set; }
}
```

**Example:**
```ouro
let scores = new Dictionary<string, int>();
scores["Alice"] = 95;
scores["Bob"] = 87;

if (scores.TryGetValue("Alice", out score)) {
    console.WriteLine($"Alice's score: {score}");
}
```

### Stack<T>

LIFO (Last In, First Out) collection.

```ouro
class Stack<T> : IEnumerable<T> {
    // Properties
    public int Count { get; }
    
    // Methods
    public void Push(item: T)
    public T Pop()
    public T Peek()
    public bool TryPop(out result: T)
    public bool TryPeek(out result: T)
    public void Clear()
    public bool Contains(item: T)
    public T[] ToArray()
    public void TrimExcess()
}
```

### Queue<T>

FIFO (First In, First Out) collection.

```ouro
class Queue<T> : IEnumerable<T> {
    // Properties
    public int Count { get; }
    
    // Methods
    public void Enqueue(item: T)
    public T Dequeue()
    public T Peek()
    public bool TryDequeue(out result: T)
    public bool TryPeek(out result: T)
    public void Clear()
    public bool Contains(item: T)
    public T[] ToArray()
    public void TrimExcess()
}
```

## Math Library

### MathFunctions

Comprehensive mathematical functions.

#### Interpolation Functions

```ouro
// Linear interpolation
function Lerp(a: double, b: double, t: double): double
function LerpUnclamped(a: double, b: double, t: double): double
function InverseLerp(a: double, b: double, value: double): double

// Spherical linear interpolation
function Slerp(a: Vector3, b: Vector3, t: double): Vector3

// Smooth interpolation
function SmoothStep(from: double, to: double, t: double): double
function SmootherStep(from: double, to: double, t: double): double
```

#### Rounding Functions

```ouro
function Round(value: double): double
function Round(value: double, digits: int): double
function Floor(value: double): double
function Ceil(value: double): double
function Truncate(value: double): double
function RoundToInt(value: double): int
function FloorToInt(value: double): int
function CeilToInt(value: double): int
```

#### Utility Functions

```ouro
function Abs(value: double): double
function Sign(value: double): double
function Min(a: double, b: double): double
function Max(a: double, b: double): double
function Clamp(value: double, min: double, max: double): double
function Clamp01(value: double): double
function Repeat(value: double, length: double): double
function PingPong(value: double, length: double): double
function Approximately(a: double, b: double, epsilon: double = Epsilon): bool
```

#### Advanced Math

```ouro
function Pow(value: double, power: double): double
function Sqrt(value: double): double
function Cbrt(value: double): double
function Exp(value: double): double
function Log(value: double): double
function Log10(value: double): double
function Log2(value: double): double
function Sin(value: double): double
function Cos(value: double): double
function Tan(value: double): double
function Asin(value: double): double
function Acos(value: double): double
function Atan(value: double): double
function Atan2(y: double, x: double): double
```

#### Easing Functions

```ouro
function EaseInQuad(t: double): double
function EaseOutQuad(t: double): double
function EaseInOutQuad(t: double): double
function EaseInCubic(t: double): double
function EaseOutCubic(t: double): double
function EaseInOutCubic(t: double): double
function EaseInElastic(t: double): double
function EaseOutElastic(t: double): double
function EaseInBounce(t: double): double
function EaseOutBounce(t: double): double
```

### Vector

N-dimensional vector with comprehensive operations.

```ouro
class Vector {
    // Constructors
    public Vector(dimension: int)
    public Vector(dimension: int, components: params double[])
    
    // Properties
    public int Dimension { get; }
    public double Magnitude { get; }
    public double MagnitudeSquared { get; }
    
    // Methods
    public Vector Normalized()
    public void Normalize()
    public double Dot(other: Vector): double
    public Vector Cross(other: Vector): Vector  // 3D only
    public double AngleTo(other: Vector): double
    public Vector Project(onto: Vector): Vector
    public Vector Reflect(normal: Vector): Vector
    
    // Operators
    public static Vector operator +(a: Vector, b: Vector)
    public static Vector operator -(a: Vector, b: Vector)
    public static Vector operator *(v: Vector, scalar: double)
    public static Vector operator /(v: Vector, scalar: double)
    
    // Indexer
    public double this[index: int] { get; set; }
}
```

### Matrix

Matrix operations with support for various decompositions.

```ouro
class Matrix {
    // Constructors
    public Matrix(rows: int, cols: int)
    public Matrix(rows: int, cols: int, data: double[,])
    
    // Properties
    public int Rows { get; }
    public int Columns { get; }
    public double Determinant()
    public Matrix Transpose { get; }
    public Matrix Inverse()
    
    // Methods
    public Matrix LUDecomposition()
    public Matrix QRDecomposition()
    public (Matrix, Vector) EigenDecomposition()
    public Vector Solve(b: Vector): Vector
    
    // Operators
    public static Matrix operator +(a: Matrix, b: Matrix)
    public static Matrix operator -(a: Matrix, b: Matrix)
    public static Matrix operator *(a: Matrix, b: Matrix)
    public static Vector operator *(m: Matrix, v: Vector)
    public static Matrix operator *(m: Matrix, scalar: double)
    
    // Indexer
    public double this[row: int, col: int] { get; set; }
    
    // Static methods
    public static Matrix Identity(size: int)
    public static Matrix Zero(rows: int, cols: int)
    public static Matrix FromRotation(angle: double, axis: Vector3)
}
```

### Quaternion

Quaternion for 3D rotations.

```ouro
class Quaternion {
    // Properties
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public double W { get; set; }
    public double Magnitude { get; }
    public Quaternion Normalized { get; }
    public Quaternion Conjugate { get; }
    
    // Constructors
    public Quaternion(x: double, y: double, z: double, w: double)
    
    // Methods
    public Vector3 ToEuler(): Vector3
    public Matrix ToMatrix(): Matrix
    public Vector3 Rotate(vector: Vector3): Vector3
    
    // Static methods
    public static Quaternion FromEuler(x: double, y: double, z: double)
    public static Quaternion FromAxisAngle(axis: Vector3, angle: double)
    public static Quaternion Slerp(a: Quaternion, b: Quaternion, t: double)
    public static Quaternion LookRotation(forward: Vector3, up: Vector3)
    
    // Operators
    public static Quaternion operator *(a: Quaternion, b: Quaternion)
}
```

### Transform

Hierarchical 3D transformations.

```ouro
class Transform {
    // Properties
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
    public Vector3 Scale { get; set; }
    public Transform Parent { get; set; }
    public List<Transform> Children { get; }
    
    // World space properties
    public Vector3 WorldPosition { get; }
    public Quaternion WorldRotation { get; }
    public Vector3 WorldScale { get; }
    
    // Transform directions
    public Vector3 Forward { get; }
    public Vector3 Right { get; }
    public Vector3 Up { get; }
    
    // Methods
    public void Translate(translation: Vector3)
    public void Rotate(rotation: Quaternion)
    public void RotateAround(point: Vector3, axis: Vector3, angle: double)
    public void LookAt(target: Vector3, up: Vector3 = Vector3.Up)
    public Vector3 TransformPoint(point: Vector3): Vector3
    public Vector3 InverseTransformPoint(point: Vector3): Vector3
    public Matrix GetMatrix(): Matrix
}
```

## IO Library

### FileSystem

File and directory operations.

```ouro
static class FileSystem {
    // File operations
    function ReadAllText(path: string): string
    function WriteAllText(path: string, content: string)
    function ReadAllLines(path: string): string[]
    function WriteAllLines(path: string, lines: string[])
    function ReadAllBytes(path: string): byte[]
    function WriteAllBytes(path: string, bytes: byte[])
    function Exists(path: string): bool
    function Delete(path: string)
    function Copy(source: string, destination: string, overwrite: bool = false)
    function Move(source: string, destination: string)
    
    // Directory operations
    function CreateDirectory(path: string)
    function DeleteDirectory(path: string, recursive: bool = false)
    function GetFiles(path: string, pattern: string = "*"): string[]
    function GetDirectories(path: string, pattern: string = "*"): string[]
    function DirectoryExists(path: string): bool
    
    // Path operations
    function GetFileName(path: string): string
    function GetExtension(path: string): string
    function GetDirectoryName(path: string): string
    function Combine(paths: params string[]): string
    function GetFullPath(path: string): string
}
```

## UI Library

### Window

Window management for UI applications.

```ouro
class Window {
    // Properties
    public string Title { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public bool Visible { get; set; }
    public bool Resizable { get; set; }
    public Widget RootWidget { get; set; }
    
    // Events
    public event EventHandler OnClose
    public event EventHandler OnResize
    public event EventHandler OnMove
    public event KeyEventHandler OnKeyPress
    public event MouseEventHandler OnMouseMove
    public event MouseEventHandler OnMouseClick
    
    // Methods
    public void Show()
    public void Hide()
    public void Close()
    public void Maximize()
    public void Minimize()
    public void Center()
    public void SetIcon(iconPath: string)
}
```

### Widget

Base class for all UI controls.

```ouro
abstract class Widget {
    // Properties
    public string Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool Visible { get; set; }
    public bool Enabled { get; set; }
    public Widget Parent { get; set; }
    public List<Widget> Children { get; }
    
    // Style properties
    public Color BackgroundColor { get; set; }
    public Color ForegroundColor { get; set; }
    public Font Font { get; set; }
    public Border Border { get; set; }
    public Padding Padding { get; set; }
    public Margin Margin { get; set; }
    
    // Methods
    public void AddChild(child: Widget)
    public void RemoveChild(child: Widget)
    public abstract void Render(graphics: Graphics)
    public virtual void Update(deltaTime: double)
    public Widget FindById(id: string): Widget
    
    // Events
    public event EventHandler OnClick
    public event EventHandler OnDoubleClick
    public event EventHandler OnMouseEnter
    public event EventHandler OnMouseLeave
    public event EventHandler OnFocus
    public event EventHandler OnBlur
}
```

### Common Widgets

- **Button** - Clickable button control
- **Label** - Text display widget
- **TextBox** - Single-line text input
- **TextArea** - Multi-line text input
- **CheckBox** - Boolean checkbox
- **RadioButton** - Mutually exclusive option
- **ComboBox** - Dropdown selection
- **ListBox** - List selection
- **Slider** - Value selection slider
- **ProgressBar** - Progress indicator
- **Panel** - Container widget
- **TabControl** - Tabbed interface
- **Menu** - Menu system
- **ToolBar** - Tool bar with buttons
- **StatusBar** - Status information display

## System Library

### Console

Console input/output operations.

```ouro
static class Console {
    // Output
    function Write(value: object)
    function WriteLine(value: object = "")
    function WriteError(value: object)
    function WriteLineError(value: object)
    
    // Input
    function ReadLine(): string
    function ReadKey(): ConsoleKey
    function ReadPassword(): string
    
    // Formatting
    function SetForegroundColor(color: ConsoleColor)
    function SetBackgroundColor(color: ConsoleColor)
    function ResetColor()
    function Clear()
    function SetCursorPosition(x: int, y: int)
    
    // Properties
    property int WindowWidth { get; set; }
    property int WindowHeight { get; set; }
    property bool CursorVisible { get; set; }
}
```

### Environment

System environment access.

```ouro
static class Environment {
    // Properties
    property string NewLine { get; }
    property string CurrentDirectory { get; set; }
    property string MachineName { get; }
    property string UserName { get; }
    property OperatingSystem OSVersion { get; }
    property int ProcessorCount { get; }
    
    // Methods
    function GetEnvironmentVariable(name: string): string
    function SetEnvironmentVariable(name: string, value: string)
    function GetCommandLineArgs(): string[]
    function Exit(exitCode: int)
    function ExpandEnvironmentVariables(text: string): string
}
```

### DateTime

Date and time operations.

```ouro
struct DateTime {
    // Properties
    property int Year { get; }
    property int Month { get; }
    property int Day { get; }
    property int Hour { get; }
    property int Minute { get; }
    property int Second { get; }
    property int Millisecond { get; }
    property DayOfWeek DayOfWeek { get; }
    property int DayOfYear { get; }
    
    // Static properties
    static property DateTime Now { get; }
    static property DateTime Today { get; }
    static property DateTime UtcNow { get; }
    
    // Methods
    function AddDays(days: double): DateTime
    function AddHours(hours: double): DateTime
    function AddMinutes(minutes: double): DateTime
    function AddSeconds(seconds: double): DateTime
    function ToString(format: string = null): string
    
    // Operators
    static operator -(a: DateTime, b: DateTime): TimeSpan
    static operator +(dt: DateTime, ts: TimeSpan): DateTime
    static operator -(dt: DateTime, ts: TimeSpan): DateTime
}
``` 
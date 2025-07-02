import CodeBlock from '../components/CodeBlock'
import Callout from '../components/Callout'

export default function MediumLevelSyntaxPage() {
  return (
    <div className="max-w-4xl">
      <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
        Medium-Level C-Like Syntax
      </h1>
      <p className="text-xl text-gray-600 dark:text-gray-300 mb-8">
        Familiar C-style syntax with modern enhancements. Perfect for developers coming from C#, Java, or C++, 
        offering type inference, lambda expressions, LINQ-style operations, and more.
      </p>

      <Callout type="info" title="Syntax Mode">
        Use the <code className="bg-gray-100 dark:bg-gray-700 px-2 py-1 rounded">@medium</code> decorator to enable 
        medium-level syntax, or it's the default when no decorator is specified.
      </Callout>

      {/* Type System and Variables */}
      <section className="mb-12">
        <h2 id="type-inference" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Type System and Variable Declaration
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Strong typing with powerful type inference. Declare variables with explicit types or let the compiler infer them.
        </p>
        <CodeBlock
          code={`@medium
// Type inference with var
var message = "Hello, World!";  // string
var count = 42;                 // int
var price = 19.99;              // double
var isReady = true;             // bool

// Explicit type declarations
string name = "Alice";
int age = 25;
double height = 5.6;
bool isStudent = false;

// Constants
const double PI = 3.14159;
const string APP_NAME = "Ouroboros";

// Multiple declarations
int x = 10, y = 20, z = 30;
string firstName = "John", lastName = "Doe";

// Nullable types
int? nullableInt = null;
string? nullableString = null;
DateTime? optionalDate = null;

// Checking for null
if (nullableInt.HasValue)
{
    Console.WriteLine($"Value: {nullableInt.Value}");
}

// Null-coalescing operators
string displayName = userName ?? "Guest";
int count = nullableInt ?? 0;

// Type aliases
using Point = (double X, double Y);
using StudentMap = Dictionary<int, Student>;

Point origin = (0.0, 0.0);
StudentMap students = new StudentMap();

// Dynamic types (use sparingly)
dynamic flexibleVar = 42;
flexibleVar = "Now I'm a string";
flexibleVar = new List<int>();`}
        />
      </section>

      {/* Arrays and Collections */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Arrays and Collections
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Rich collection types with intuitive initialization syntax.
        </p>
        <CodeBlock
          code={`@medium
// Arrays
int[] numbers = new int[5];
int[] primes = { 2, 3, 5, 7, 11 };
string[] names = new string[] { "Alice", "Bob", "Charlie" };

// Multidimensional arrays
int[,] matrix = new int[3, 3];
int[,] grid = { 
    { 1, 2, 3 }, 
    { 4, 5, 6 }, 
    { 7, 8, 9 } 
};

// Jagged arrays
int[][] jagged = new int[3][];
jagged[0] = new int[] { 1, 2 };
jagged[1] = new int[] { 3, 4, 5 };
jagged[2] = new int[] { 6 };

// Lists
var list = new List<string>();
list.Add("Apple");
list.Add("Banana");
list.AddRange(new[] { "Cherry", "Date" });

// List initializers
var fruits = new List<string> { "Apple", "Banana", "Cherry" };
var scores = new List<int> { 95, 87, 92, 88, 91 };

// Dictionaries
var dict = new Dictionary<string, int>();
dict["Alice"] = 25;
dict["Bob"] = 30;

// Dictionary initializers
var ages = new Dictionary<string, int>
{
    ["Alice"] = 25,
    ["Bob"] = 30,
    ["Charlie"] = 35
};

// Object initializer syntax
var config = new Dictionary<string, object>
{
    { "theme", "dark" },
    { "fontSize", 14 },
    { "autoSave", true }
};

// Sets
var uniqueNumbers = new HashSet<int> { 1, 2, 3, 4, 5 };
var commonItems = set1.Intersect(set2);

// Queues and Stacks
var queue = new Queue<string>();
queue.Enqueue("First");
queue.Enqueue("Second");
string next = queue.Dequeue();

var stack = new Stack<int>();
stack.Push(10);
stack.Push(20);
int top = stack.Pop();

// Collection properties
Console.WriteLine($"Count: {list.Count}");
Console.WriteLine($"Contains: {list.Contains("Apple")}");
Console.WriteLine($"Index: {list.IndexOf("Banana")}");`}
        />
      </section>

      {/* Control Flow */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Control Flow Statements
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Traditional control flow with modern enhancements.
        </p>
        <CodeBlock
          code={`@medium
// If-else statements
int score = 85;
if (score >= 90)
{
    Console.WriteLine("Grade: A");
}
else if (score >= 80)
{
    Console.WriteLine("Grade: B");
}
else if (score >= 70)
{
    Console.WriteLine("Grade: C");
}
else
{
    Console.WriteLine("Grade: F");
}

// Ternary operator
string status = age >= 18 ? "Adult" : "Minor";
double discount = isMember ? price * 0.9 : price;

// Switch statement
switch (dayOfWeek)
{
    case DayOfWeek.Monday:
    case DayOfWeek.Tuesday:
    case DayOfWeek.Wednesday:
    case DayOfWeek.Thursday:
    case DayOfWeek.Friday:
        Console.WriteLine("Weekday");
        break;
    case DayOfWeek.Saturday:
    case DayOfWeek.Sunday:
        Console.WriteLine("Weekend");
        break;
    default:
        Console.WriteLine("Invalid day");
        break;
}

// Switch expressions (C# 8.0 style)
string dayType = dayOfWeek switch
{
    DayOfWeek.Saturday or DayOfWeek.Sunday => "Weekend",
    _ => "Weekday"
};

// Pattern matching in switch
object obj = GetValue();
switch (obj)
{
    case int i:
        Console.WriteLine($"Integer: {i}");
        break;
    case string s when s.Length > 10:
        Console.WriteLine($"Long string: {s}");
        break;
    case string s:
        Console.WriteLine($"Short string: {s}");
        break;
    case null:
        Console.WriteLine("Null value");
        break;
    default:
        Console.WriteLine($"Other type: {obj.GetType()}");
        break;
}

// Modern switch with tuples
var result = (x, y) switch
{
    (0, 0) => "Origin",
    (0, _) => "On Y-axis",
    (_, 0) => "On X-axis",
    var (a, b) when a == b => "On diagonal",
    _ => "Somewhere else"
};`}
        />
      </section>

      {/* Loops */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Loop Constructs
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Various loop types for different scenarios.
        </p>
        <CodeBlock
          code={`@medium
// For loop
for (int i = 0; i < 10; i++)
{
    Console.WriteLine($"Iteration {i}");
}

// For loop with multiple variables
for (int i = 0, j = 10; i < j; i++, j--)
{
    Console.WriteLine($"i: {i}, j: {j}");
}

// While loop
int count = 0;
while (count < 5)
{
    Console.WriteLine($"Count: {count}");
    count++;
}

// Do-while loop
int attempts = 0;
do
{
    attempts++;
    Console.WriteLine($"Attempt {attempts}");
} while (attempts < 3);

// Foreach loop
string[] colors = { "Red", "Green", "Blue" };
foreach (string color in colors)
{
    Console.WriteLine($"Color: {color}");
}

// Foreach with index
foreach (var (item, index) in colors.Select((value, i) => (value, i)))
{
    Console.WriteLine($"{index}: {item}");
}

// Nested loops
for (int row = 0; row < 3; row++)
{
    for (int col = 0; col < 3; col++)
    {
        Console.Write($"[{row},{col}] ");
    }
    Console.WriteLine();
}

// Loop control
for (int i = 0; i < 100; i++)
{
    if (i % 10 == 0 && i > 0)
    {
        Console.WriteLine($"Milestone: {i}");
    }
    
    if (i > 50)
    {
        break;  // Exit loop
    }
    
    if (i % 2 == 0)
    {
        continue;  // Skip to next iteration
    }
    
    // Process odd numbers only
    ProcessNumber(i);
}

// Infinite loop with break condition
while (true)
{
    var input = Console.ReadLine();
    if (input == "quit")
    {
        break;
    }
    ProcessInput(input);
}`}
        />
      </section>

      {/* Functions and Methods */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Functions and Methods
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Define functions with various parameter types and return values.
        </p>
        <CodeBlock
          code={`@medium
// Basic function
public int Add(int a, int b)
{
    return a + b;
}

// Function with default parameters
public void Greet(string name, string greeting = "Hello")
{
    Console.WriteLine($"{greeting}, {name}!");
}

// Optional parameters
public double CalculateArea(double radius, double pi = 3.14159)
{
    return pi * radius * radius;
}

// Named arguments
Greet(name: "Alice", greeting: "Hi");
CalculateArea(radius: 5.0, pi: Math.PI);

// Params array
public int Sum(params int[] numbers)
{
    int total = 0;
    foreach (int n in numbers)
    {
        total += n;
    }
    return total;
}

// Usage
int result1 = Sum(1, 2, 3);
int result2 = Sum(1, 2, 3, 4, 5);

// Out parameters
public bool TryParse(string input, out int result)
{
    if (int.TryParse(input, out result))
    {
        return true;
    }
    result = 0;
    return false;
}

// Ref parameters
public void Swap(ref int a, ref int b)
{
    int temp = a;
    a = b;
    b = temp;
}

// Tuple return types
public (bool success, string message, int code) ProcessData(string data)
{
    if (string.IsNullOrEmpty(data))
    {
        return (false, "Data is empty", -1);
    }
    // Process...
    return (true, "Success", 0);
}

// Using tuple returns
var (success, message, code) = ProcessData("test");
if (success)
{
    Console.WriteLine(message);
}

// Generic functions
public T Max<T>(T a, T b) where T : IComparable<T>
{
    return a.CompareTo(b) > 0 ? a : b;
}

// Multiple generic parameters
public Dictionary<TKey, TValue> CreateDictionary<TKey, TValue>()
{
    return new Dictionary<TKey, TValue>();
}

// Extension methods
public static class StringExtensions
{
    public static bool IsNullOrEmpty(this string str)
    {
        return string.IsNullOrEmpty(str);
    }
    
    public static string Reverse(this string str)
    {
        char[] chars = str.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }
}

// Usage
string text = "Hello";
bool isEmpty = text.IsNullOrEmpty();
string reversed = text.Reverse();  // "olleH"`}
        />
      </section>

      {/* Lambda Expressions */}
      <section className="mb-12">
        <h2 id="lambda" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Lambda Expressions and Delegates
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Concise function syntax for inline operations and functional programming.
        </p>
        <CodeBlock
          code={`@medium
// Lambda expressions
Func<int, int> square = x => x * x;
Func<int, int, int> add = (x, y) => x + y;
Func<string, bool> isLong = s => s.Length > 10;

// Using lambdas
int result = square(5);  // 25
bool longString = isLong("Hello, World!");  // true

// Action delegates (no return value)
Action<string> print = message => Console.WriteLine(message);
Action<int, int> printSum = (a, b) => Console.WriteLine($"Sum: {a + b}");

// Predicate delegates
Predicate<int> isEven = n => n % 2 == 0;
Predicate<string> isEmpty = string.IsNullOrEmpty;

// Multi-line lambdas
Func<int, int, double> calculate = (x, y) =>
{
    int sum = x + y;
    int product = x * y;
    return (double)sum / product;
};

// Closures
int multiplier = 10;
Func<int, int> multiply = x => x * multiplier;
Console.WriteLine(multiply(5));  // 50

// Lambda with LINQ
var numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

var evens = numbers.Where(n => n % 2 == 0);
var squares = numbers.Select(n => n * n);
var sum = numbers.Aggregate((acc, n) => acc + n);

// Complex lambdas
var students = GetStudents();
var topStudents = students
    .Where(s => s.GPA >= 3.5)
    .OrderByDescending(s => s.GPA)
    .ThenBy(s => s.LastName)
    .Select(s => new 
    {
        FullName = $"{s.FirstName} {s.LastName}",
        s.GPA,
        Honors = s.GPA >= 3.8 ? "Summa Cum Laude" : "Magna Cum Laude"
    });

// Method group conversion
List<string> names = new List<string> { "alice", "bob", "charlie" };
var upperNames = names.Select(string.ToUpper);
var lengths = names.Select(s => s.Length);

// Delegate composition
Func<double, double> addOne = x => x + 1;
Func<double, double> multiplyByTwo = x => x * 2;
Func<double, double> combined = x => multiplyByTwo(addOne(x));

// Event handlers with lambdas
button.Click += (sender, e) => 
{
    Console.WriteLine("Button clicked!");
};

timer.Elapsed += (sender, e) =>
{
    Console.WriteLine($"Timer elapsed at {e.SignalTime}");
};`}
        />
      </section>

      {/* LINQ Operations */}
      <section className="mb-12">
        <h2 id="linq" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          LINQ (Language Integrated Query)
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Powerful query syntax for collections and data manipulation.
        </p>
        <CodeBlock
          code={`@medium
// Sample data
var products = new List<Product>
{
    new Product { Id = 1, Name = "Laptop", Price = 999.99, Category = "Electronics" },
    new Product { Id = 2, Name = "Mouse", Price = 29.99, Category = "Electronics" },
    new Product { Id = 3, Name = "Desk", Price = 299.99, Category = "Furniture" },
    new Product { Id = 4, Name = "Chair", Price = 199.99, Category = "Furniture" },
    new Product { Id = 5, Name = "Monitor", Price = 399.99, Category = "Electronics" }
};

// Query syntax
var expensiveProducts = from p in products
                       where p.Price > 200
                       orderby p.Price descending
                       select p;

// Method syntax (equivalent)
var expensiveProducts2 = products
    .Where(p => p.Price > 200)
    .OrderByDescending(p => p.Price);

// Projection
var productInfo = products.Select(p => new 
{
    p.Name,
    p.Price,
    PriceWithTax = p.Price * 1.1
});

// Grouping
var productsByCategory = products
    .GroupBy(p => p.Category)
    .Select(g => new 
    {
        Category = g.Key,
        Count = g.Count(),
        TotalValue = g.Sum(p => p.Price),
        Products = g.ToList()
    });

// Joining
var orders = GetOrders();
var orderDetails = from o in orders
                  join p in products on o.ProductId equals p.Id
                  select new 
                  {
                      o.OrderId,
                      o.Date,
                      p.Name,
                      p.Price,
                      Total = p.Price * o.Quantity
                  };

// Set operations
var electronics = products.Where(p => p.Category == "Electronics");
var affordable = products.Where(p => p.Price < 300);
var affordableElectronics = electronics.Intersect(affordable);

// Aggregation
double averagePrice = products.Average(p => p.Price);
double totalValue = products.Sum(p => p.Price);
var mostExpensive = products.MaxBy(p => p.Price);
var cheapest = products.MinBy(p => p.Price);

// Quantifiers
bool hasExpensive = products.Any(p => p.Price > 1000);
bool allAffordable = products.All(p => p.Price < 2000);

// Partitioning
var firstThree = products.Take(3);
var skipTwo = products.Skip(2);
var middleThree = products.Skip(1).Take(3);

// Complex query
var report = products
    .GroupBy(p => p.Category)
    .Select(g => new 
    {
        Category = g.Key,
        ProductCount = g.Count(),
        AveragePrice = g.Average(p => p.Price),
        PriceRange = new 
        {
            Min = g.Min(p => p.Price),
            Max = g.Max(p => p.Price)
        },
        TopProducts = g.OrderByDescending(p => p.Price)
                       .Take(2)
                       .Select(p => p.Name)
                       .ToList()
    })
    .OrderBy(r => r.Category);

// LINQ with async (hypothetical)
var data = await products
    .Where(p => p.Price > 100)
    .AsAsyncEnumerable()
    .SelectAwait(async p => await EnrichProductAsync(p))
    .ToListAsync();`}
        />
      </section>

      {/* Exception Handling */}
      <section className="mb-12">
        <h2 id="exceptions" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Exception Handling
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Robust error handling with try-catch-finally blocks.
        </p>
        <CodeBlock
          code={`@medium
// Basic try-catch
try
{
    int result = 10 / 0;
}
catch (DivideByZeroException ex)
{
    Console.WriteLine($"Division error: {ex.Message}");
}

// Multiple catch blocks
try
{
    string json = File.ReadAllText("config.json");
    var config = JsonSerializer.Deserialize<Config>(json);
    ProcessConfig(config);
}
catch (FileNotFoundException)
{
    Console.WriteLine("Configuration file not found");
}
catch (JsonException ex)
{
    Console.WriteLine($"Invalid JSON: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}

// Try-catch-finally
FileStream file = null;
try
{
    file = new FileStream("data.txt", FileMode.Open);
    // Process file
}
catch (IOException ex)
{
    Console.WriteLine($"IO Error: {ex.Message}");
}
finally
{
    file?.Dispose();  // Always runs
}

// Using statement (automatic disposal)
using (var reader = new StreamReader("file.txt"))
{
    string content = reader.ReadToEnd();
    // Reader is automatically disposed
}

// Modern using declaration
using var connection = new SqlConnection(connectionString);
connection.Open();
// Connection disposed at end of scope

// Exception filters
try
{
    await NetworkOperation();
}
catch (HttpRequestException ex) when (ex.StatusCode == 404)
{
    Console.WriteLine("Resource not found");
}
catch (HttpRequestException ex) when (ex.StatusCode >= 500)
{
    Console.WriteLine("Server error");
}

// Throwing exceptions
public void ValidateAge(int age)
{
    if (age < 0)
    {
        throw new ArgumentException("Age cannot be negative", nameof(age));
    }
    if (age > 150)
    {
        throw new ArgumentOutOfRangeException(nameof(age), "Age seems unrealistic");
    }
}

// Custom exceptions
public class ValidationException : Exception
{
    public string FieldName { get; }
    public object InvalidValue { get; }
    
    public ValidationException(string fieldName, object value, string message) 
        : base(message)
    {
        FieldName = fieldName;
        InvalidValue = value;
    }
}

// Rethrowing exceptions
try
{
    DoRiskyOperation();
}
catch (Exception ex)
{
    LogError(ex);
    throw;  // Preserves stack trace
}

// Exception wrapping
try
{
    ComplexOperation();
}
catch (Exception ex)
{
    throw new ApplicationException("Operation failed", ex);
}`}
        />
      </section>

      {/* Async/Await */}
      <section className="mb-12">
        <h2 id="async" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Asynchronous Programming
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Modern async/await pattern for non-blocking operations.
        </p>
        <CodeBlock
          code={`@medium
// Async method
public async Task<string> FetchDataAsync(string url)
{
    using var client = new HttpClient();
    string response = await client.GetStringAsync(url);
    return response;
}

// Async with return value
public async Task<int> CalculateAsync(int value)
{
    await Task.Delay(1000);  // Simulate work
    return value * 2;
}

// Void async (for event handlers)
private async void Button_Click(object sender, EventArgs e)
{
    await ProcessDataAsync();
    MessageBox.Show("Complete!");
}

// Multiple async operations
public async Task ProcessMultipleAsync()
{
    // Sequential execution
    var result1 = await Operation1Async();
    var result2 = await Operation2Async();
    
    // Parallel execution
    var task1 = Operation1Async();
    var task2 = Operation2Async();
    await Task.WhenAll(task1, task2);
    
    // Get results
    var res1 = task1.Result;
    var res2 = task2.Result;
}

// Async with cancellation
public async Task LongRunningOperationAsync(CancellationToken cancellationToken)
{
    for (int i = 0; i < 100; i++)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(100, cancellationToken);
        UpdateProgress(i);
    }
}

// Using cancellation
var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromSeconds(30));

try
{
    await LongRunningOperationAsync(cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled");
}

// Async streams
public async IAsyncEnumerable<int> GenerateNumbersAsync()
{
    for (int i = 0; i < 10; i++)
    {
        await Task.Delay(100);
        yield return i;
    }
}

// Consuming async streams
await foreach (var number in GenerateNumbersAsync())
{
    Console.WriteLine(number);
}

// Async LINQ
var results = await data
    .ToAsyncEnumerable()
    .Where(x => x.IsValid)
    .SelectAwait(async x => await TransformAsync(x))
    .ToListAsync();

// Task composition
public async Task<Result> ComplexOperationAsync()
{
    var configTask = LoadConfigAsync();
    var dataTask = FetchDataAsync();
    
    await Task.WhenAll(configTask, dataTask);
    
    var config = await configTask;
    var data = await dataTask;
    
    return await ProcessAsync(config, data);
}

// Error handling in async
public async Task<T> SafeOperationAsync<T>()
{
    try
    {
        return await RiskyOperationAsync<T>();
    }
    catch (NetworkException ex)
    {
        await LogErrorAsync(ex);
        return default(T);
    }
}

// ConfigureAwait
public async Task LibraryMethodAsync()
{
    // Don't capture context in library code
    await SomeOperationAsync().ConfigureAwait(false);
}`}
        />
      </section>

      {/* Classes and Objects */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Classes and Object-Oriented Programming
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Full object-oriented programming support with classes, inheritance, and interfaces.
        </p>
        <CodeBlock
          code={`@medium
// Basic class
public class Person
{
    // Properties
    public string Name { get; set; }
    public int Age { get; set; }
    public DateTime DateOfBirth { get; private set; }
    
    // Constructor
    public Person(string name, DateTime dateOfBirth)
    {
        Name = name;
        DateOfBirth = dateOfBirth;
        Age = CalculateAge(dateOfBirth);
    }
    
    // Methods
    private int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
    
    public override string ToString()
    {
        return $"{Name}, Age: {Age}";
    }
}

// Inheritance
public class Student : Person
{
    public string StudentId { get; set; }
    public double GPA { get; set; }
    
    public Student(string name, DateTime dateOfBirth, string studentId)
        : base(name, dateOfBirth)
    {
        StudentId = studentId;
    }
}

// Interfaces
public interface IRepository<T>
{
    Task<T> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> CreateAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}

// Interface implementation
public class StudentRepository : IRepository<Student>
{
    private readonly List<Student> _students = new();
    
    public async Task<Student> GetByIdAsync(int id)
    {
        await Task.Delay(10); // Simulate DB access
        return _students.FirstOrDefault(s => s.Id == id);
    }
    
    // ... other implementations
}

// Abstract classes
public abstract class Shape
{
    public abstract double CalculateArea();
    public abstract double CalculatePerimeter();
    
    public void Display()
    {
        Console.WriteLine($"Area: {CalculateArea()}");
        Console.WriteLine($"Perimeter: {CalculatePerimeter()}");
    }
}

public class Circle : Shape
{
    public double Radius { get; set; }
    
    public override double CalculateArea()
    {
        return Math.PI * Radius * Radius;
    }
    
    public override double CalculatePerimeter()
    {
        return 2 * Math.PI * Radius;
    }
}

// Generic classes
public class Result<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public string Error { get; set; }
    
    public static Result<T> Ok(T data) => new() { Success = true, Data = data };
    public static Result<T> Fail(string error) => new() { Success = false, Error = error };
}

// Partial classes
public partial class LargeClass
{
    public void Method1() { }
}

public partial class LargeClass
{
    public void Method2() { }
}

// Nested classes
public class Outer
{
    private int _value = 42;
    
    public class Inner
    {
        public void AccessOuter(Outer outer)
        {
            Console.WriteLine(outer._value);
        }
    }
}`}
        />
      </section>

      {/* Pattern Matching */}
      <section className="mb-12">
        <h2 id="pattern" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Pattern Matching
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Modern pattern matching for cleaner, more expressive code.
        </p>
        <CodeBlock
          code={`@medium
// Type patterns
object obj = GetValue();
if (obj is string s)
{
    Console.WriteLine($"String length: {s.Length}");
}
else if (obj is int i && i > 0)
{
    Console.WriteLine($"Positive integer: {i}");
}

// Switch expressions with patterns
string GetTypeDescription(object obj) => obj switch
{
    null => "null",
    string s => $"string: {s}",
    int i => $"int: {i}",
    List<int> list => $"int list with {list.Count} items",
    _ => "unknown type"
};

// Property patterns
string DescribeStudent(Student student) => student switch
{
    { GPA: >= 3.8 } => "Honor student",
    { GPA: >= 3.0 } => "Good standing",
    { GPA: < 2.0 } => "Academic probation",
    _ => "Regular student"
};

// Tuple patterns
string GetQuadrant(Point point) => (point.X, point.Y) switch
{
    (0, 0) => "Origin",
    (> 0, > 0) => "Quadrant I",
    (< 0, > 0) => "Quadrant II",
    (< 0, < 0) => "Quadrant III",
    (> 0, < 0) => "Quadrant IV",
    _ => "On axis"
};

// Relational patterns
string CategorizeAge(int age) => age switch
{
    < 0 => "Invalid",
    < 13 => "Child",
    < 20 => "Teenager",
    < 60 => "Adult",
    _ => "Senior"
};

// Logical patterns
string DescribeNumber(int number) => number switch
{
    > 0 and < 10 => "Single digit positive",
    >= 10 and < 100 => "Two digit positive",
    < 0 and > -10 => "Single digit negative",
    0 => "Zero",
    _ => "Large number"
};

// List patterns
string DescribeArray(int[] array) => array switch
{
    [] => "Empty array",
    [var single] => $"Single element: {single}",
    [var first, var second] => $"Two elements: {first}, {second}",
    [var first, .., var last] => $"Multiple elements from {first} to {last}",
    _ => "Complex array"
};

// Combining patterns
string ProcessData(object data) => data switch
{
    string { Length: > 10 } s => $"Long string: {s.Substring(0, 10)}...",
    int i when i % 2 == 0 => $"Even number: {i}",
    List<int> { Count: 0 } => "Empty list",
    List<int> { Count: var count } list => $"List with {count} items, sum: {list.Sum()}",
    null => "No data",
    _ => "Unknown data type"
};`}
        />
      </section>

      {/* Real-World Example */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Real-World Example: Task Management System
        </h2>
        <CodeBlock
          code={`@medium
// Task management system with modern C# features
public enum Priority { Low, Medium, High, Critical }
public enum Status { Todo, InProgress, Done, Cancelled }

public class Task
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public Priority Priority { get; set; }
    public Status Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string AssignedTo { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class TaskManager
{
    private readonly List<Task> _tasks = new();
    private int _nextId = 1;
    
    // Create task with fluent interface
    public TaskBuilder CreateTask(string title)
    {
        return new TaskBuilder(this, title);
    }
    
    // LINQ-based queries
    public IEnumerable<Task> GetTasksByStatus(Status status)
    {
        return _tasks.Where(t => t.Status == status);
    }
    
    public IEnumerable<Task> GetHighPriorityTasks()
    {
        return _tasks
            .Where(t => t.Priority >= Priority.High && t.Status != Status.Done)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt);
    }
    
    // Async operations
    public async Task<IEnumerable<Task>> SearchTasksAsync(string query)
    {
        await Task.Delay(100); // Simulate search delay
        
        return _tasks.Where(t => 
            t.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            t.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
            t.Tags.Any(tag => tag.Contains(query, StringComparison.OrdinalIgnoreCase))
        );
    }
    
    // Pattern matching for task analysis
    public string AnalyzeTask(Task task) => task switch
    {
        { Status: Status.Done } => "Completed",
        { Priority: Priority.Critical, Status: Status.Todo } => "URGENT: Not started!",
        { Priority: Priority.High, Status: Status.InProgress } => "High priority in progress",
        { CreatedAt: var created } when (DateTime.Now - created).Days > 30 => "Old task",
        _ => "Regular task"
    };
    
    // Statistics with LINQ
    public object GetStatistics()
    {
        return new
        {
            Total = _tasks.Count,
            ByStatus = _tasks.GroupBy(t => t.Status)
                            .Select(g => new { Status = g.Key, Count = g.Count() }),
            ByPriority = _tasks.GroupBy(t => t.Priority)
                              .Select(g => new { Priority = g.Key, Count = g.Count() }),
            CompletionRate = _tasks.Count > 0 
                ? (double)_tasks.Count(t => t.Status == Status.Done) / _tasks.Count 
                : 0,
            AverageCompletionTime = _tasks
                .Where(t => t.Status == Status.Done && t.CompletedAt.HasValue)
                .Select(t => (t.CompletedAt.Value - t.CreatedAt).TotalDays)
                .DefaultIfEmpty(0)
                .Average()
        };
    }
    
    // Fluent builder pattern
    public class TaskBuilder
    {
        private readonly TaskManager _manager;
        private readonly Task _task;
        
        internal TaskBuilder(TaskManager manager, string title)
        {
            _manager = manager;
            _task = new Task
            {
                Id = _manager._nextId++,
                Title = title,
                CreatedAt = DateTime.Now,
                Status = Status.Todo,
                Priority = Priority.Medium
            };
        }
        
        public TaskBuilder WithDescription(string description)
        {
            _task.Description = description;
            return this;
        }
        
        public TaskBuilder WithPriority(Priority priority)
        {
            _task.Priority = priority;
            return this;
        }
        
        public TaskBuilder AssignTo(string user)
        {
            _task.AssignedTo = user;
            return this;
        }
        
        public TaskBuilder WithTags(params string[] tags)
        {
            _task.Tags.AddRange(tags);
            return this;
        }
        
        public Task Build()
        {
            _manager._tasks.Add(_task);
            return _task;
        }
    }
}

// Usage example
var taskManager = new TaskManager();

// Create tasks fluently
var task1 = taskManager.CreateTask("Implement login system")
    .WithDescription("Add OAuth support")
    .WithPriority(Priority.High)
    .AssignTo("Alice")
    .WithTags("security", "backend")
    .Build();

var task2 = taskManager.CreateTask("Update documentation")
    .WithPriority(Priority.Low)
    .AssignTo("Bob")
    .WithTags("docs")
    .Build();

// Query tasks
var urgentTasks = taskManager.GetHighPriorityTasks();
var searchResults = await taskManager.SearchTasksAsync("security");
var stats = taskManager.GetStatistics();`}
        />
      </section>

      {/* Best Practices */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Best Practices
        </h2>
        
        <Callout type="tip" title="When to Use Medium-Level Syntax">
          Medium-level syntax is ideal for:
          <ul className="list-disc list-inside mt-2">
            <li>Building libraries and frameworks</li>
            <li>Performance-sensitive applications</li>
            <li>Complex business logic</li>
            <li>Integration with existing C#/Java codebases</li>
            <li>Team projects with developers familiar with C-style languages</li>
          </ul>
        </Callout>

        <Callout type="info" title="Modern C# Features">
          Take advantage of modern language features:
          <ul className="list-disc list-inside mt-2">
            <li>Pattern matching for cleaner conditional logic</li>
            <li>LINQ for data manipulation</li>
            <li>async/await for non-blocking operations</li>
            <li>Nullable reference types for null safety</li>
            <li>Records for immutable data structures</li>
          </ul>
        </Callout>

        <CodeBlock
          code={`@medium
// Good: Use var for obvious types
var name = "John Doe";
var numbers = new List<int>();

// Good: Use explicit types for clarity
Dictionary<string, User> userCache = GetUserCache();
IRepository<Product> repository = new ProductRepository();

// Good: Async all the way
public async Task<Result> ProcessAsync()
{
    var data = await FetchDataAsync();
    var processed = await TransformAsync(data);
    await SaveAsync(processed);
    return Result.Success();
}

// Good: Use pattern matching
public decimal CalculateDiscount(Customer customer) => customer switch
{
    PremiumCustomer { Years: > 5 } => 0.20m,
    PremiumCustomer => 0.10m,
    RegularCustomer { Orders: > 10 } => 0.05m,
    _ => 0m
};

// Good: LINQ for readability
var report = orders
    .Where(o => o.Date >= startDate)
    .GroupBy(o => o.CustomerId)
    .Select(g => new 
    {
        CustomerId = g.Key,
        TotalSpent = g.Sum(o => o.Total),
        OrderCount = g.Count()
    })
    .OrderByDescending(r => r.TotalSpent)
    .Take(10);`}
        />
      </section>
    </div>
  )
} 
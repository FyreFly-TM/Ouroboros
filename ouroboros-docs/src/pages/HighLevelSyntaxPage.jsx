import CodeBlock from '../components/CodeBlock'
import Callout from '../components/Callout'

export default function HighLevelSyntaxPage() {
  return (
    <div className="max-w-4xl">
      <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
        High-Level Natural Language Syntax
      </h1>
      <p className="text-xl text-gray-600 dark:text-gray-300 mb-8">
        Write code that reads like natural language. Ouroboros's high-level syntax makes programming intuitive 
        and accessible while maintaining full power and flexibility.
      </p>

      <Callout type="info" title="Syntax Mode">
        Use the <code className="bg-gray-100 dark:bg-gray-700 px-2 py-1 rounded">@high</code> decorator to enable 
        high-level syntax in a function or block.
      </Callout>

      {/* Variable Declaration */}
      <section className="mb-12">
        <h2 id="variables" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Natural Variable Declaration
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Use the intuitive <code>:=</code> operator to declare and initialize variables. Type inference works automatically.
        </p>
        <CodeBlock
          code={`@high
// Simple variable declaration
name := "Alice"
age := 25
height := 5.6
isStudent := true

// Multiple assignments
x := y := z := 0

// Collections
friends := ["Bob", "Charlie", "Diana"]
scores := [95, 87, 92, 88]
config := { "theme": "dark", "language": "en" }

// Complex types
today := DateTime.Now
position := Vector(10, 20, 30)
color := Color.FromRGB(255, 128, 0)

// Updating variables
age := age + 1  // Now 26
name := "Dr. " + name  // "Dr. Alice"

// Swapping variables naturally
a := 10
b := 20
a, b := b, a  // Now a=20, b=10

// Conditional assignment
status := if age >= 18 then "adult" else "minor"
discount := if isStudent then 0.2 else 0.0`}
        />
      </section>

      {/* String Operations */}
      <section className="mb-12">
        <h2 id="strings" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          String Interpolation and Operations
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Use <code>${`$\{}`}</code> for string interpolation, making string formatting natural and readable.
        </p>
        <CodeBlock
          code={`@high
// Basic string interpolation
name := "Alice"
age := 25
greeting := "Hello, \${name}! You are \${age} years old."

// Expressions in interpolation
price := 19.99
quantity := 3
message := "Total: $\${price * quantity}"

// Formatting
pi := 3.14159
formatted := "Pi is approximately \${pi:F2}"  // "Pi is approximately 3.14"

// Multi-line strings
poem := """
    Roses are red,
    Violets are blue,
    \${name} loves coding,
    And Ouroboros too!
"""

// String operations
fullName := firstName + " " + lastName
shout := message.ToUpper()
whisper := message.ToLower()
trimmed := userInput.Trim()

// Natural string checking
if name contains "Ali" then
    print "Name contains Ali"

if email ends with "@example.com" then
    print "Corporate email detected"

if phone starts with "+1" then
    print "US phone number"`}
        />
      </section>

      {/* Print Statement */}
      <section className="mb-12">
        <h2 id="print" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Print Statement
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          The <code>print</code> statement provides a simple way to output text.
        </p>
        <CodeBlock
          code={`@high
// Simple print
print "Hello, World!"

// Print with interpolation
name := "Alice"
print "Welcome, \${name}!"

// Print multiple values
x := 10
y := 20
print "Coordinates: (\${x}, \${y})"

// Print with expressions
items := ["apple", "banana", "orange"]
print "You have \${items.Length} items"

// Print to different outputs (hypothetical)
print to console "This goes to console"
print to file "log.txt" "This goes to a file"
print to debug "Debug information"

// Formatted printing
value := 123.456
print "Value: \${value:F2}"  // "Value: 123.46"
print "Hex: \${value:X}"     // "Hex: 7B"
print "Percent: \${value:P}"  // "Percent: 12,345.60%"`}
        />
      </section>

      {/* Loops */}
      <section className="mb-12">
        <h2 id="repeat" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Natural Loops
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Express loops in natural language that's easy to read and understand.
        </p>
        <CodeBlock
          code={`@high
// Repeat a specific number of times
repeat 5 times
    print "Hello!"

// Repeat with counter
repeat 10 times as i
    print "Iteration \${i}"

// Repeat with custom range
repeat from 1 to 10
    print "Counting..."

// Repeat with step
repeat from 0 to 100 step 10 as value
    print "Value: \${value}"

// Repeat while condition
count := 0
repeat while count < 10
    print "Count: \${count}"
    count := count + 1

// Repeat until condition
input := ""
repeat until input == "quit"
    input := getUserInput()
    print "You entered: \${input}"

// Nested repeats
repeat 3 times as row
    repeat 3 times as col
        print "Cell [\${row}, \${col}]"

// Break and continue
repeat 10 times as i
    if i == 5 then continue
    if i == 8 then break
    print "Number: \${i}"`}
        />
      </section>

      {/* Pattern Matching */}
      <section className="mb-12">
        <h2 id="match" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Pattern Matching
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Use <code>match</code> for elegant pattern matching and branching logic.
        </p>
        <CodeBlock
          code={`@high
// Basic pattern matching
grade := 85
match grade
    when 90 to 100 => print "A"
    when 80 to 89 => print "B"
    when 70 to 79 => print "C"
    when 60 to 69 => print "D"
    default => print "F"

// Matching with types
value := getUserInput()
match value
    when String s => print "String: \${s}"
    when Integer i => print "Integer: \${i}"
    when Double d => print "Double: \${d}"
    when List l => print "List with \${l.Count} items"
    default => print "Unknown type"

// Multiple conditions
day := "Saturday"
match day
    when "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" => 
        print "Weekday"
    when "Saturday", "Sunday" => 
        print "Weekend"

// Pattern matching with guards
age := 25
match age
    when < 13 => print "Child"
    when 13 to 17 => print "Teenager"
    when 18 to 64 => print "Adult"
    when >= 65 => print "Senior"

// Destructuring in patterns
point := { x: 10, y: 20 }
match point
    when { x: 0, y: 0 } => print "Origin"
    when { x: 0, y } => print "On Y-axis at \${y}"
    when { x, y: 0 } => print "On X-axis at \${x}"
    when { x, y } => print "Point at (\${x}, \${y})"

// Complex matching
response := getAPIResponse()
match response
    when { status: 200, data } => 
        print "Success: \${data}"
    when { status: 404 } => 
        print "Not found"
    when { status: 500, error } => 
        print "Server error: \${error}"
    default => 
        print "Unknown response"`}
        />
      </section>

      {/* Natural Conditionals */}
      <section className="mb-12">
        <h2 id="if" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Natural Conditional Statements
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Write conditions that read like English sentences.
        </p>
        <CodeBlock
          code={`@high
// Natural comparisons
age := 25
if age is greater than 18 then
    print "You are an adult"

// Multiple natural operators
score := 85
if score is greater than or equal to 90 then
    print "Excellent!"
else if score is between 70 and 89 then
    print "Good job!"
else if score is less than 70 then
    print "Need improvement"

// Natural boolean logic
hasTicket := true
age := 12
if hasTicket and age is less than 18 then
    print "Child ticket holder"

// Checking equality naturally
status := "active"
if status is "active" then
    print "User is active"

if status is not "banned" then
    print "User can access"

// Range checking
temperature := 72
if temperature is between 68 and 76 then
    print "Comfortable temperature"

// Multiple conditions
username := "alice"
password := "secret123"
if username is "alice" and password is "secret123" then
    print "Login successful"

// Natural null checking
if user is not null then
    print "User exists: \${user.name}"

if data is empty then
    print "No data available"

// Type checking
if value is String then
    print "It's a string: \${value}"
else if value is Number then
    print "It's a number: \${value}"

// Collection checking
items := ["apple", "banana", "orange"]
if "apple" is in items then
    print "Found apple!"

if items is not empty then
    print "Cart has \${items.Length} items"`}
        />
      </section>

      {/* Iteration */}
      <section className="mb-12">
        <h2 id="foreach" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Natural Iteration
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Iterate through collections using natural language constructs.
        </p>
        <CodeBlock
          code={`@high
// Basic iteration
fruits := ["apple", "banana", "orange", "grape"]
for each fruit in fruits
    print "I like \${fruit}"

// Iteration with index
for each fruit at index in fruits
    print "\${index}: \${fruit}"

// Iterating over different collections
scores := [95, 87, 92, 88, 91]
for each score in scores
    if score is greater than 90 then
        print "Excellent: \${score}"

// Dictionary iteration
ages := { "Alice": 25, "Bob": 30, "Charlie": 35 }
for each name, age in ages
    print "\${name} is \${age} years old"

// Filtered iteration
numbers := [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
for each even in numbers where even % 2 == 0
    print "Even: \${even}"

// Multiple collections
names := ["Alice", "Bob", "Charlie"]
ages := [25, 30, 35]
for each name, age in zip(names, ages)
    print "\${name} is \${age}"

// Nested iteration
matrix := [[1, 2, 3], [4, 5, 6], [7, 8, 9]]
for each row in matrix
    for each cell in row
        print "Cell: \${cell}"

// Iteration with transformation
words := ["hello", "world", "ouroboros"]
for each upper in words select word.ToUpper()
    print upper

// Early exit
for each item in collection
    if item matches criteria then
        print "Found: \${item}"
        break

// Skip items
for each number in 1 to 100
    if number % 10 != 0 then continue
    print "Milestone: \${number}"`}
        />
      </section>

      {/* Functions */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Natural Function Definitions
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Define functions using natural language (Note: Full implementation may vary).
        </p>
        <CodeBlock
          code={`@high
// Simple function definition
define function greet takes name
    return "Hello, \${name}!"

// Function with multiple parameters
define function add takes x and y
    return x + y

// Function with default values
define function power takes base and exponent = 2
    return base ** exponent

// Function with type hints
define function calculate takes Double x and Double y returns Double
    return Math.Sqrt(x * x + y * y)

// Using functions
greeting := greet("Alice")
sum := add(10, 20)
squared := power(5)  // Uses default exponent = 2
cubed := power(5, 3)

// Function with conditions
define function getGrade takes score
    if score is greater than or equal to 90 then
        return "A"
    else if score is greater than or equal to 80 then
        return "B"
    else if score is greater than or equal to 70 then
        return "C"
    else
        return "F"

// Function with pattern matching
define function describe takes animal
    match animal
        when { type: "dog", name } => 
            return "\${name} is a good dog!"
        when { type: "cat", name } => 
            return "\${name} is a curious cat!"
        default => 
            return "Unknown animal"

// Anonymous functions / lambdas
numbers := [1, 2, 3, 4, 5]
doubled := numbers map (n => n * 2)
filtered := numbers where (n => n > 3)

// Function composition
define function double takes x
    return x * 2

define function addOne takes x
    return x + 1

// Compose: first double, then add one
result := compose(double, addOne)(5)  // (5 * 2) + 1 = 11`}
        />
      </section>

      {/* Collections */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Natural Collection Operations
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Work with collections using intuitive, natural language operations.
        </p>
        <CodeBlock
          code={`@high
// Creating collections
numbers := [1, 2, 3, 4, 5]
names := ["Alice", "Bob", "Charlie"]
empty := []

// Adding elements
numbers add 6
names add "David"

// Removing elements
numbers remove 3
names remove "Bob"

// Checking membership
if 4 is in numbers then
    print "Found 4"

if "Eve" is not in names then
    names add "Eve"

// Collection operations
evens := numbers where item % 2 == 0
doubled := numbers map item * 2
sum := numbers reduce (acc, item) => acc + item

// Sorting
sorted := numbers sort ascending
reverseSorted := names sort descending
customSort := people sort by age

// Grouping
students := getStudents()
byGrade := students group by grade
byAge := students group by age

// Set operations with collections
set1 := [1, 2, 3, 4, 5]
set2 := [4, 5, 6, 7, 8]
union := set1 union set2  // [1, 2, 3, 4, 5, 6, 7, 8]
intersection := set1 intersect set2  // [4, 5]
difference := set1 except set2  // [1, 2, 3]

// Slicing
first3 := numbers take 3  // [1, 2, 3]
last2 := numbers take last 2  // [4, 5]
middle := numbers skip 1 take 3  // [2, 3, 4]

// Finding elements
firstEven := numbers first where item % 2 == 0
lastOdd := numbers last where item % 2 == 1
anyLarge := numbers any where item > 10
allPositive := numbers all where item > 0`}
        />
      </section>

      {/* Error Handling */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Natural Error Handling
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Handle errors gracefully with natural language constructs.
        </p>
        <CodeBlock
          code={`@high
// Basic try-catch
try
    result := riskyOperation()
    print "Success: \${result}"
catch error
    print "Error occurred: \${error.Message}"

// Try-catch-finally
file := null
try
    file := open("data.txt")
    content := file.ReadAll()
    process(content)
catch FileNotFound
    print "File doesn't exist"
catch PermissionDenied
    print "No permission to read file"
finally
    if file is not null then
        file.Close()

// Multiple error types
try
    data := fetchFromAPI()
    parsed := parseJSON(data)
    save(parsed)
catch NetworkError as e
    print "Network problem: \${e.Message}"
catch ParseError as e
    print "Invalid data format: \${e.Message}"
catch SaveError as e
    print "Could not save: \${e.Message}"
catch error
    print "Unexpected error: \${error}"

// Throwing errors
define function validateAge takes age
    if age is less than 0 then
        throw "Age cannot be negative"
    if age is greater than 150 then
        throw "Age seems unrealistic"
    return true

// Error propagation
define function processUser takes userId
    try
        user := getUser(userId)
        if user is null then
            throw UserNotFound(userId)
        return user
    catch error
        log("Error processing user \${userId}: \${error}")
        throw  // Re-throw the error

// Safe navigation
user := getUser(id)
name := user?.name ?? "Unknown"
email := user?.contact?.email ?? "no-email@example.com"`}
        />
      </section>

      {/* Real-World Examples */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Real-World Examples
        </h2>
        
        <h3 className="text-xl font-semibold text-gray-800 dark:text-gray-200 mb-3">
          Todo List Application
        </h3>
        <CodeBlock
          code={`@high
// Todo list manager
todos := []
nextId := 1

define function addTodo takes title
    todo := {
        id: nextId,
        title: title,
        completed: false,
        createdAt: DateTime.Now
    }
    todos add todo
    nextId := nextId + 1
    return todo

define function completeTodo takes id
    for each todo in todos where todo.id == id
        todo.completed := true
        todo.completedAt := DateTime.Now
        return true
    return false

define function getTodos takes filter = "all"
    match filter
        when "all" => return todos
        when "active" => return todos where not todo.completed
        when "completed" => return todos where todo.completed

// Usage
addTodo("Learn Ouroboros")
addTodo("Build awesome app")
addTodo("Share with friends")

completeTodo(1)

activeTodos := getTodos("active")
for each todo in activeTodos
    print "[ ] \${todo.title}"`}
        />

        <h3 className="text-xl font-semibold text-gray-800 dark:text-gray-200 mb-3 mt-8">
          Data Processing Pipeline
        </h3>
        <CodeBlock
          code={`@high
// Process sales data
salesData := loadCSV("sales_2024.csv")

// Clean and transform data
cleanedData := salesData
    where row.amount > 0
    where row.date is not null
    map {
        date: parseDate(row.date),
        product: row.product.Trim().ToUpper(),
        amount: parseDouble(row.amount),
        quantity: parseInt(row.quantity),
        region: normalizeRegion(row.region)
    }

// Analyze by region
byRegion := cleanedData group by region

for each region, sales in byRegion
    totalRevenue := sales reduce (sum, sale) => sum + sale.amount
    avgSale := totalRevenue / sales.Length
    topProduct := sales 
        group by product
        map { product: key, total: value.Sum(s => s.amount) }
        sort by total descending
        first
    
    print "Region: \${region}"
    print "  Total Revenue: $\${totalRevenue:F2}"
    print "  Average Sale: $\${avgSale:F2}"
    print "  Top Product: \${topProduct.product}"

// Find trends
monthlySales := cleanedData
    group by { year: date.Year, month: date.Month }
    map { 
        period: "\${key.year}-\${key.month:D2}",
        revenue: value.Sum(s => s.amount),
        transactions: value.Length
    }
    sort by period

for each month in monthlySales
    print "\${month.period}: $\${month.revenue:F2} (\${month.transactions} sales)"`}
        />
      </section>

      {/* Best Practices */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Best Practices
        </h2>
        
        <Callout type="tip" title="Readability First">
          High-level syntax prioritizes readability. Use it when:
          <ul className="list-disc list-inside mt-2">
            <li>Writing scripts and automation</li>
            <li>Prototyping ideas quickly</li>
            <li>Creating educational examples</li>
            <li>Building domain-specific solutions</li>
          </ul>
        </Callout>

        <Callout type="warning" title="Performance Considerations">
          While high-level syntax is expressive, consider:
          <ul className="list-disc list-inside mt-2">
            <li>Switch to medium or low-level syntax for performance-critical sections</li>
            <li>Natural language parsing may have slight overhead</li>
            <li>Some optimizations may be less obvious in high-level code</li>
          </ul>
        </Callout>

        <CodeBlock
          code={`@high
// Good: Clear intent, readable
users := database.GetUsers()
activeUsers := users where user.lastLogin > 30.DaysAgo()
for each user in activeUsers
    send notification to user.email

// Good: Natural error messages
if password.Length is less than 8 then
    throw "Password must be at least 8 characters"

// Good: Self-documenting code
if user.age is between 13 and 17 then
    category := "teenager"
else if user.age is greater than or equal to 18 then
    category := "adult"

// Consider switching to @medium for complex algorithms
@medium
// Complex sorting algorithm with specific performance requirements
public void QuickSort<T>(T[] array, int left, int right) where T : IComparable<T>
{
    // ... implementation
}`}
        />
      </section>
    </div>
  )
} 
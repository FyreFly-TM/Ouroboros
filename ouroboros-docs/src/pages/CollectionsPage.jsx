import CodeBlock from '../components/CodeBlock'
import Callout from '../components/Callout'

export default function CollectionsPage() {
  return (
    <div className="max-w-4xl">
      <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
        Collections
      </h1>
      <p className="text-xl text-gray-600 dark:text-gray-300 mb-8">
        Powerful data structures for organizing and manipulating data. Ouroboros provides a rich set of collection types 
        with intuitive APIs and excellent performance characteristics.
      </p>

      <Callout type="info" title="Collection Types">
        Import collections with <code className="bg-gray-100 dark:bg-gray-700 px-2 py-1 rounded">using Ouroboros.StdLib.Collections;</code>
      </Callout>

      {/* List */}
      <section className="mb-12">
        <h2 id="list" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          List&lt;T&gt; - Dynamic Arrays
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Resizable arrays that provide fast access by index and efficient operations at the end.
        </p>
        <CodeBlock
          code={`// Creating lists
var numbers = new List<int>();
var names = new List<string> { "Alice", "Bob", "Charlie" };
var scores = new List<double>(100);  // Initial capacity

// Adding elements
numbers.Add(42);
numbers.AddRange(new[] { 1, 2, 3, 4, 5 });

// Inserting elements
names.Insert(1, "Alex");  // Insert at index 1
names.InsertRange(2, new[] { "Amy", "Andrew" });

// Accessing elements
string first = names[0];
string last = names[names.Count - 1];

// Safe access
string name = names.ElementAtOrDefault(10) ?? "Unknown";

// Modifying elements
names[0] = "Alicia";

// Removing elements
names.Remove("Bob");          // Remove first occurrence
names.RemoveAt(2);            // Remove at index
names.RemoveAll(n => n.StartsWith("A"));  // Remove all matching
names.RemoveRange(1, 2);      // Remove range

// Searching
int index = names.IndexOf("Charlie");
int lastIndex = names.LastIndexOf("Charlie");
bool exists = names.Contains("Alice");
string found = names.Find(n => n.Length > 5);
List<string> allFound = names.FindAll(n => n.Length > 5);

// Sorting
numbers.Sort();  // Sort ascending
numbers.Sort((a, b) => b.CompareTo(a));  // Sort descending
names.Sort((a, b) => a.Length.CompareTo(b.Length));  // Sort by length

// Reversing
numbers.Reverse();
names.Reverse(1, 3);  // Reverse subset

// Converting
int[] array = numbers.ToArray();
HashSet<int> set = new HashSet<int>(numbers);
string joined = string.Join(", ", names);

// List operations
var list1 = new List<int> { 1, 2, 3, 4, 5 };
var list2 = new List<int> { 4, 5, 6, 7, 8 };

// Get unique elements
var unique = list1.Distinct().ToList();

// Combine lists
var combined = list1.Concat(list2).ToList();
var union = list1.Union(list2).ToList();
var intersection = list1.Intersect(list2).ToList();
var except = list1.Except(list2).ToList();

// Capacity management
numbers.Capacity = 1000;  // Pre-allocate
numbers.TrimExcess();     // Free unused memory

// Binary search (list must be sorted)
numbers.Sort();
int searchIndex = numbers.BinarySearch(42);
if (searchIndex >= 0)
{
    Console.WriteLine($"Found at index {searchIndex}");
}

// List patterns
// Chunking
var chunks = names.Chunk(3).ToList();  // Split into groups of 3

// Sliding window
var windows = names.Zip(names.Skip(1), (a, b) => (a, b));

// Grouping consecutive
var groups = numbers.GroupWhile((prev, curr) => curr == prev + 1);`}
        />

        <h3 className="text-xl font-semibold text-gray-800 dark:text-gray-200 mb-3 mt-6">
          Advanced List Operations
        </h3>
        <CodeBlock
          code={`// List as stack
var stack = new List<string>();
stack.Add("bottom");     // Push
stack.Add("middle");
stack.Add("top");
string popped = stack[stack.Count - 1];  // Peek
stack.RemoveAt(stack.Count - 1);         // Pop

// List as queue (less efficient than Queue<T>)
var queue = new List<string>();
queue.Add("first");      // Enqueue
queue.Add("second");
string dequeued = queue[0];  // Peek
queue.RemoveAt(0);           // Dequeue

// Circular buffer using List
public class CircularBuffer<T>
{
    private List<T> buffer;
    private int head = 0;
    private int count = 0;
    
    public CircularBuffer(int capacity)
    {
        buffer = new List<T>(capacity);
        for (int i = 0; i < capacity; i++)
            buffer.Add(default(T));
    }
    
    public void Add(T item)
    {
        buffer[(head + count) % buffer.Capacity] = item;
        if (count < buffer.Capacity)
            count++;
        else
            head = (head + 1) % buffer.Capacity;
    }
    
    public IEnumerable<T> GetAll()
    {
        for (int i = 0; i < count; i++)
        {
            yield return buffer[(head + i) % buffer.Capacity];
        }
    }
}

// List pooling for performance
public class ListPool<T>
{
    private static Stack<List<T>> pool = new Stack<List<T>>();
    
    public static List<T> Rent()
    {
        return pool.Count > 0 ? pool.Pop() : new List<T>();
    }
    
    public static void Return(List<T> list)
    {
        list.Clear();
        pool.Push(list);
    }
}

// Usage
var tempList = ListPool<int>.Rent();
try
{
    // Use the list
    tempList.AddRange(GetNumbers());
    ProcessNumbers(tempList);
}
finally
{
    ListPool<int>.Return(tempList);
}`}
        />
      </section>

      {/* Dictionary */}
      <section className="mb-12">
        <h2 id="dictionary" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Dictionary&lt;TKey, TValue&gt; - Key-Value Pairs
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Hash table implementation providing O(1) average case lookup, insertion, and deletion.
        </p>
        <CodeBlock
          code={`// Creating dictionaries
var ages = new Dictionary<string, int>();
var config = new Dictionary<string, string>
{
    ["host"] = "localhost",
    ["port"] = "8080",
    ["protocol"] = "https"
};

// Adding elements
ages["Alice"] = 25;
ages["Bob"] = 30;
ages.Add("Charlie", 35);  // Throws if key exists

// Safe adding
bool added = ages.TryAdd("David", 40);  // Returns false if exists

// Accessing values
int aliceAge = ages["Alice"];  // Throws if not found

// Safe access
if (ages.TryGetValue("Eve", out int eveAge))
{
    Console.WriteLine($"Eve is {eveAge}");
}

// Get with default
int frankAge = ages.GetValueOrDefault("Frank", -1);

// Checking existence
bool hasAlice = ages.ContainsKey("Alice");
bool hasAge30 = ages.ContainsValue(30);

// Updating values
ages["Alice"] = 26;  // Update existing
ages["Eve"] = 28;    // Add new

// Removing elements
ages.Remove("Bob");
bool removed = ages.Remove("Charlie", out int charlieAge);

// Iterating
foreach (var kvp in ages)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}

foreach (string name in ages.Keys)
{
    Console.WriteLine(name);
}

foreach (int age in ages.Values)
{
    Console.WriteLine(age);
}

// Dictionary with custom comparer
var caseInsensitive = new Dictionary<string, int>(
    StringComparer.OrdinalIgnoreCase
);
caseInsensitive["Hello"] = 1;
Console.WriteLine(caseInsensitive["HELLO"]);  // 1

// Nested dictionaries
var userPreferences = new Dictionary<int, Dictionary<string, object>>();
userPreferences[userId] = new Dictionary<string, object>
{
    ["theme"] = "dark",
    ["fontSize"] = 14,
    ["notifications"] = true
};

// Default dictionaries (custom implementation)
public class DefaultDictionary<TKey, TValue> : Dictionary<TKey, TValue>
{
    private Func<TValue> defaultFactory;
    
    public DefaultDictionary(Func<TValue> factory) : base()
    {
        defaultFactory = factory;
    }
    
    public new TValue this[TKey key]
    {
        get
        {
            if (!ContainsKey(key))
                base[key] = defaultFactory();
            return base[key];
        }
        set => base[key] = value;
    }
}

// Usage
var counts = new DefaultDictionary<string, int>(() => 0);
counts["apple"]++;  // No need to check if exists

// Multi-value dictionary
var multiDict = new Dictionary<string, List<string>>();

void AddValue(string key, string value)
{
    if (!multiDict.TryGetValue(key, out var list))
    {
        list = new List<string>();
        multiDict[key] = list;
    }
    list.Add(value);
}

// Or using LINQ grouping
var grouped = items.GroupBy(i => i.Category)
                  .ToDictionary(g => g.Key, g => g.ToList());`}
        />

        <h3 className="text-xl font-semibold text-gray-800 dark:text-gray-200 mb-3 mt-6">
          Advanced Dictionary Patterns
        </h3>
        <CodeBlock
          code={`// Caching with dictionary
public class LRUCache<TKey, TValue>
{
    private Dictionary<TKey, LinkedListNode<(TKey, TValue)>> cache;
    private LinkedList<(TKey, TValue)> lruList;
    private int capacity;
    
    public LRUCache(int capacity)
    {
        this.capacity = capacity;
        cache = new Dictionary<TKey, LinkedListNode<(TKey, TValue)>>();
        lruList = new LinkedList<(TKey, TValue)>();
    }
    
    public TValue Get(TKey key)
    {
        if (cache.TryGetValue(key, out var node))
        {
            lruList.Remove(node);
            lruList.AddFirst(node);
            return node.Value.Item2;
        }
        throw new KeyNotFoundException();
    }
    
    public void Put(TKey key, TValue value)
    {
        if (cache.TryGetValue(key, out var node))
        {
            lruList.Remove(node);
        }
        else if (cache.Count >= capacity)
        {
            var last = lruList.Last;
            cache.Remove(last.Value.Item1);
            lruList.RemoveLast();
        }
        
        var newNode = lruList.AddFirst((key, value));
        cache[key] = newNode;
    }
}

// Two-way dictionary
public class BiDictionary<T1, T2>
{
    private Dictionary<T1, T2> forward = new Dictionary<T1, T2>();
    private Dictionary<T2, T1> reverse = new Dictionary<T2, T1>();
    
    public void Add(T1 key1, T2 key2)
    {
        forward[key1] = key2;
        reverse[key2] = key1;
    }
    
    public T2 GetByFirst(T1 key) => forward[key];
    public T1 GetBySecond(T2 key) => reverse[key];
}

// Trie implementation using nested dictionaries
public class Trie
{
    private class TrieNode
    {
        public Dictionary<char, TrieNode> Children = new();
        public bool IsEndOfWord;
    }
    
    private TrieNode root = new TrieNode();
    
    public void Insert(string word)
    {
        var current = root;
        foreach (char c in word)
        {
            if (!current.Children.TryGetValue(c, out var node))
            {
                node = new TrieNode();
                current.Children[c] = node;
            }
            current = node;
        }
        current.IsEndOfWord = true;
    }
    
    public bool Search(string word)
    {
        var current = root;
        foreach (char c in word)
        {
            if (!current.Children.TryGetValue(c, out current))
                return false;
        }
        return current.IsEndOfWord;
    }
}`}
        />
      </section>

      {/* Stack */}
      <section className="mb-12">
        <h2 id="stack" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Stack&lt;T&gt; - LIFO Collection
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Last-In-First-Out (LIFO) collection ideal for tracking state, parsing, and backtracking algorithms.
        </p>
        <CodeBlock
          code={`// Creating stacks
var stack = new Stack<int>();
var operatorStack = new Stack<char>(new[] { '+', '-' });

// Push elements
stack.Push(1);
stack.Push(2);
stack.Push(3);

// Pop elements
int top = stack.Pop();  // Returns and removes 3
Console.WriteLine($"Popped: {top}");

// Peek without removing
int next = stack.Peek();  // Returns 2 but doesn't remove
Console.WriteLine($"Next: {next}");

// Safe operations
if (stack.Count > 0)
{
    int value = stack.Pop();
}

// TryPop (C# 10+)
if (stack.TryPop(out int popped))
{
    Console.WriteLine($"Got: {popped}");
}

// Check if empty
bool isEmpty = stack.Count == 0;

// Convert to array (order: top to bottom)
int[] array = stack.ToArray();

// Clear all elements
stack.Clear();

// Practical example: Balanced parentheses
public bool IsBalanced(string expression)
{
    var stack = new Stack<char>();
    var pairs = new Dictionary<char, char>
    {
        ['('] = ')',
        ['['] = ']',
        ['{'] = '}'
    };
    
    foreach (char c in expression)
    {
        if (pairs.ContainsKey(c))
        {
            stack.Push(c);
        }
        else if (pairs.ContainsValue(c))
        {
            if (stack.Count == 0 || pairs[stack.Pop()] != c)
                return false;
        }
    }
    
    return stack.Count == 0;
}

// Expression evaluation
public int EvaluatePostfix(string expression)
{
    var stack = new Stack<int>();
    var tokens = expression.Split(' ');
    
    foreach (var token in tokens)
    {
        if (int.TryParse(token, out int number))
        {
            stack.Push(number);
        }
        else
        {
            int b = stack.Pop();
            int a = stack.Pop();
            int result = token switch
            {
                "+" => a + b,
                "-" => a - b,
                "*" => a * b,
                "/" => a / b,
                _ => throw new InvalidOperationException()
            };
            stack.Push(result);
        }
    }
    
    return stack.Pop();
}

// Undo/Redo functionality
public class UndoRedoStack<T>
{
    private Stack<T> undoStack = new Stack<T>();
    private Stack<T> redoStack = new Stack<T>();
    
    public void Execute(T action)
    {
        undoStack.Push(action);
        redoStack.Clear();  // Clear redo stack on new action
    }
    
    public T Undo()
    {
        if (undoStack.Count > 0)
        {
            T action = undoStack.Pop();
            redoStack.Push(action);
            return action;
        }
        return default(T);
    }
    
    public T Redo()
    {
        if (redoStack.Count > 0)
        {
            T action = redoStack.Pop();
            undoStack.Push(action);
            return action;
        }
        return default(T);
    }
}`}
        />
      </section>

      {/* Queue */}
      <section className="mb-12">
        <h2 id="queue" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Queue&lt;T&gt; - FIFO Collection
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          First-In-First-Out (FIFO) collection perfect for task scheduling, breadth-first search, and buffering.
        </p>
        <CodeBlock
          code={`// Creating queues
var queue = new Queue<string>();
var taskQueue = new Queue<Task>(100);  // Initial capacity

// Enqueue elements
queue.Enqueue("First");
queue.Enqueue("Second");
queue.Enqueue("Third");

// Dequeue elements
string first = queue.Dequeue();  // Returns and removes "First"
Console.WriteLine($"Processed: {first}");

// Peek without removing
string next = queue.Peek();  // Returns "Second" but doesn't remove
Console.WriteLine($"Next: {next}");

// Safe operations
if (queue.Count > 0)
{
    string item = queue.Dequeue();
}

// TryDequeue (C# 10+)
if (queue.TryDequeue(out string dequeued))
{
    Console.WriteLine($"Got: {dequeued}");
}

// Convert to array (order: front to back)
string[] array = queue.ToArray();

// Clear queue
queue.Clear();

// Practical example: BFS traversal
public void BreadthFirstSearch<T>(T start, Func<T, IEnumerable<T>> getNeighbors)
{
    var visited = new HashSet<T>();
    var queue = new Queue<T>();
    
    queue.Enqueue(start);
    visited.Add(start);
    
    while (queue.Count > 0)
    {
        T current = queue.Dequeue();
        Console.WriteLine($"Visiting: {current}");
        
        foreach (T neighbor in getNeighbors(current))
        {
            if (!visited.Contains(neighbor))
            {
                visited.Add(neighbor);
                queue.Enqueue(neighbor);
            }
        }
    }
}

// Producer-Consumer pattern
public class TaskProcessor<T>
{
    private Queue<T> taskQueue = new Queue<T>();
    private object lockObj = new object();
    
    public void EnqueueTask(T task)
    {
        lock (lockObj)
        {
            taskQueue.Enqueue(task);
            Monitor.Pulse(lockObj);  // Wake up consumer
        }
    }
    
    public T DequeueTask()
    {
        lock (lockObj)
        {
            while (taskQueue.Count == 0)
            {
                Monitor.Wait(lockObj);  // Wait for tasks
            }
            return taskQueue.Dequeue();
        }
    }
}

// Circular queue with fixed size
public class CircularQueue<T>
{
    private T[] buffer;
    private int head = 0;
    private int tail = 0;
    private int count = 0;
    
    public CircularQueue(int capacity)
    {
        buffer = new T[capacity + 1];  // Extra space to distinguish full/empty
    }
    
    public bool Enqueue(T item)
    {
        if (count == buffer.Length - 1)
            return false;  // Queue full
        
        buffer[tail] = item;
        tail = (tail + 1) % buffer.Length;
        count++;
        return true;
    }
    
    public bool TryDequeue(out T item)
    {
        if (count == 0)
        {
            item = default(T);
            return false;
        }
        
        item = buffer[head];
        head = (head + 1) % buffer.Length;
        count--;
        return true;
    }
}

// Priority queue using SortedSet
public class PriorityQueue<T>
{
    private SortedSet<(int Priority, T Item, int Id)> items;
    private int idCounter = 0;
    
    public PriorityQueue()
    {
        items = new SortedSet<(int Priority, T Item, int Id)>(
            Comparer<(int Priority, T Item, int Id)>.Create((a, b) =>
            {
                int result = a.Priority.CompareTo(b.Priority);
                return result != 0 ? result : a.Id.CompareTo(b.Id);
            })
        );
    }
    
    public void Enqueue(T item, int priority)
    {
        items.Add((priority, item, idCounter++));
    }
    
    public T Dequeue()
    {
        var first = items.First();
        items.Remove(first);
        return first.Item;
    }
}`}
        />
      </section>

      {/* HashSet */}
      <section className="mb-12">
        <h2 id="hashset" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          HashSet&lt;T&gt; - Unique Elements
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Unordered collection of unique elements with O(1) operations and built-in set operations.
        </p>
        <CodeBlock
          code={`// Creating sets
var numbers = new HashSet<int>();
var names = new HashSet<string> { "Alice", "Bob", "Charlie" };
var customSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

// Adding elements
numbers.Add(1);      // Returns true
numbers.Add(2);      // Returns true
bool added = numbers.Add(1);  // Returns false (already exists)

// Adding multiple
numbers.UnionWith(new[] { 3, 4, 5 });

// Removing elements
numbers.Remove(3);   // Returns true if removed
numbers.RemoveWhere(n => n % 2 == 0);  // Remove all even

// Checking membership
bool hasOne = numbers.Contains(1);

// Set operations
var setA = new HashSet<int> { 1, 2, 3, 4, 5 };
var setB = new HashSet<int> { 4, 5, 6, 7, 8 };

// Union: A ∪ B
var union = new HashSet<int>(setA);
union.UnionWith(setB);  // {1, 2, 3, 4, 5, 6, 7, 8}

// Intersection: A ∩ B
var intersection = new HashSet<int>(setA);
intersection.IntersectWith(setB);  // {4, 5}

// Difference: A - B
var difference = new HashSet<int>(setA);
difference.ExceptWith(setB);  // {1, 2, 3}

// Symmetric difference: A △ B
var symmetric = new HashSet<int>(setA);
symmetric.SymmetricExceptWith(setB);  // {1, 2, 3, 6, 7, 8}

// Subset and superset checks
bool isSubset = setA.IsSubsetOf(setB);        // false
bool isSuperset = setA.IsSupersetOf(setB);    // false
bool isProperSubset = setA.IsProperSubsetOf(setB);  // false
bool overlaps = setA.Overlaps(setB);          // true

// Set equals
bool equals = setA.SetEquals(new[] { 5, 4, 3, 2, 1 });  // true (order doesn't matter)

// Practical example: Finding duplicates
public List<T> FindDuplicates<T>(IEnumerable<T> items)
{
    var seen = new HashSet<T>();
    var duplicates = new HashSet<T>();
    
    foreach (var item in items)
    {
        if (!seen.Add(item))
        {
            duplicates.Add(item);
        }
    }
    
    return duplicates.ToList();
}

// Remove duplicates while preserving order
public List<T> RemoveDuplicates<T>(IEnumerable<T> items)
{
    var seen = new HashSet<T>();
    var result = new List<T>();
    
    foreach (var item in items)
    {
        if (seen.Add(item))
        {
            result.Add(item);
        }
    }
    
    return result;
}

// Graph algorithms with HashSet
public class Graph<T>
{
    private Dictionary<T, HashSet<T>> adjacencyList = new();
    
    public void AddEdge(T from, T to)
    {
        if (!adjacencyList.ContainsKey(from))
            adjacencyList[from] = new HashSet<T>();
        
        adjacencyList[from].Add(to);
    }
    
    public HashSet<T> GetNeighbors(T vertex)
    {
        return adjacencyList.GetValueOrDefault(vertex, new HashSet<T>());
    }
    
    public HashSet<T> GetAllVertices()
    {
        var vertices = new HashSet<T>(adjacencyList.Keys);
        foreach (var neighbors in adjacencyList.Values)
        {
            vertices.UnionWith(neighbors);
        }
        return vertices;
    }
}

// Using HashSet for memoization
public class Memoizer<TInput, TOutput>
{
    private HashSet<TInput> computed = new();
    private Dictionary<TInput, TOutput> cache = new();
    
    public TOutput GetOrCompute(TInput input, Func<TInput, TOutput> compute)
    {
        if (!computed.Contains(input))
        {
            cache[input] = compute(input);
            computed.Add(input);
        }
        return cache[input];
    }
}`}
        />
      </section>

      {/* Advanced Collection Patterns */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Advanced Collection Patterns
        </h2>
        
        <h3 className="text-xl font-semibold text-gray-800 dark:text-gray-200 mb-3">
          Collection Combinations
        </h3>
        <CodeBlock
          code={`// MultiSet (Bag) - Elements with counts
public class MultiSet<T>
{
    private Dictionary<T, int> counts = new();
    
    public void Add(T item, int count = 1)
    {
        counts[item] = counts.GetValueOrDefault(item) + count;
    }
    
    public bool Remove(T item)
    {
        if (counts.TryGetValue(item, out int count))
        {
            if (count > 1)
                counts[item] = count - 1;
            else
                counts.Remove(item);
            return true;
        }
        return false;
    }
    
    public int GetCount(T item)
    {
        return counts.GetValueOrDefault(item);
    }
}

// OrderedSet - Maintains insertion order
public class OrderedSet<T> : IEnumerable<T>
{
    private Dictionary<T, LinkedListNode<T>> dict = new();
    private LinkedList<T> list = new();
    
    public bool Add(T item)
    {
        if (!dict.ContainsKey(item))
        {
            var node = list.AddLast(item);
            dict[item] = node;
            return true;
        }
        return false;
    }
    
    public bool Remove(T item)
    {
        if (dict.TryGetValue(item, out var node))
        {
            list.Remove(node);
            dict.Remove(item);
            return true;
        }
        return false;
    }
    
    public IEnumerator<T> GetEnumerator() => list.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

// Deque (Double-ended queue)
public class Deque<T>
{
    private LinkedList<T> list = new();
    
    public void AddFirst(T item) => list.AddFirst(item);
    public void AddLast(T item) => list.AddLast(item);
    
    public T RemoveFirst()
    {
        var first = list.First ?? throw new InvalidOperationException();
        list.RemoveFirst();
        return first.Value;
    }
    
    public T RemoveLast()
    {
        var last = list.Last ?? throw new InvalidOperationException();
        list.RemoveLast();
        return last.Value;
    }
}

// Bloom Filter - Probabilistic data structure
public class BloomFilter
{
    private BitArray bits;
    private int hashCount;
    
    public BloomFilter(int size, int hashCount)
    {
        bits = new BitArray(size);
        this.hashCount = hashCount;
    }
    
    public void Add(string item)
    {
        for (int i = 0; i < hashCount; i++)
        {
            int hash = GetHash(item, i) % bits.Length;
            bits[hash] = true;
        }
    }
    
    public bool MightContain(string item)
    {
        for (int i = 0; i < hashCount; i++)
        {
            int hash = GetHash(item, i) % bits.Length;
            if (!bits[hash]) return false;
        }
        return true;  // May have false positives
    }
    
    private int GetHash(string item, int seed)
    {
        return (item.GetHashCode() + seed).GetHashCode() & 0x7FFFFFFF;
    }
}`}
        />

        <h3 className="text-xl font-semibold text-gray-800 dark:text-gray-200 mb-3 mt-6">
          Thread-Safe Collections
        </h3>
        <CodeBlock
          code={`// Thread-safe wrapper
public class ThreadSafeList<T>
{
    private List<T> list = new();
    private ReaderWriterLockSlim rwLock = new();
    
    public void Add(T item)
    {
        rwLock.EnterWriteLock();
        try
        {
            list.Add(item);
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }
    
    public T Get(int index)
    {
        rwLock.EnterReadLock();
        try
        {
            return list[index];
        }
        finally
        {
            rwLock.ExitReadLock();
        }
    }
    
    public List<T> ToList()
    {
        rwLock.EnterReadLock();
        try
        {
            return new List<T>(list);
        }
        finally
        {
            rwLock.ExitReadLock();
        }
    }
}

// Concurrent collections (built-in)
var concurrentDict = new ConcurrentDictionary<string, int>();
concurrentDict.TryAdd("key", 1);
concurrentDict.AddOrUpdate("key", 1, (k, old) => old + 1);

var concurrentQueue = new ConcurrentQueue<string>();
concurrentQueue.Enqueue("item");
concurrentQueue.TryDequeue(out string result);

var concurrentBag = new ConcurrentBag<int>();
concurrentBag.Add(42);
concurrentBag.TryTake(out int value);`}
        />
      </section>

      {/* Performance Comparison */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Performance Characteristics
        </h2>
        
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead className="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Collection</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Add</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Remove</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Find</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Access by Index</th>
              </tr>
            </thead>
            <tbody className="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
              <tr>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-white">List&lt;T&gt;</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">O(1) amortized</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">O(n)</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">O(n)</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">O(1)</td>
              </tr>
              <tr>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-white">Dictionary&lt;K,V&gt;</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">O(1) average</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">O(1) average</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">O(1) average</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">N/A</td>
              </tr>
              <tr>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-white">HashSet&lt;T&gt;</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">O(1) average</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">O(1) average</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">O(1) average</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">N/A</td>
              </tr>
              <tr>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-white">Stack&lt;T&gt;</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">O(1)</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">O(1)</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">N/A</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">N/A</td>
              </tr>
              <tr>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-white">Queue&lt;T&gt;</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">O(1)</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">O(1)</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">N/A</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">N/A</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      {/* Best Practices */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Best Practices
        </h2>
        
        <Callout type="tip" title="Choosing the Right Collection">
          Select collections based on your use case:
          <ul className="list-disc list-inside mt-2">
            <li><strong>List&lt;T&gt;</strong>: Sequential access, indexed data</li>
            <li><strong>Dictionary&lt;K,V&gt;</strong>: Key-based lookup</li>
            <li><strong>HashSet&lt;T&gt;</strong>: Unique values, set operations</li>
            <li><strong>Stack&lt;T&gt;</strong>: LIFO operations, undo/redo</li>
            <li><strong>Queue&lt;T&gt;</strong>: FIFO operations, task processing</li>
          </ul>
        </Callout>

        <Callout type="warning" title="Performance Considerations">
          Optimize collection usage:
          <ul className="list-disc list-inside mt-2">
            <li>Pre-size collections when possible to avoid resizing</li>
            <li>Use <code>TrimExcess()</code> to free unused memory</li>
            <li>Consider <code>SortedDictionary</code> for ordered access</li>
            <li>Use concurrent collections for thread safety</li>
            <li>Avoid LINQ in performance-critical loops</li>
          </ul>
        </Callout>

        <CodeBlock
          code={`// Good: Pre-size collections
var list = new List<int>(1000);  // Avoid resizing
var dict = new Dictionary<string, int>(expectedCount);

// Good: Use appropriate collection
var uniqueIds = new HashSet<int>();  // Not List<int>
var taskQueue = new Queue<Task>();   // Not List<Task>

// Good: Efficient bulk operations
list.AddRange(items);  // Better than multiple Add()
set.UnionWith(otherSet);  // Better than foreach + Add()

// Good: Memory management
hugeList.Clear();
hugeList.TrimExcess();  // Free memory

// Consider: Lazy evaluation
var filtered = items.Where(x => x > 10);  // Not evaluated yet
var result = filtered.ToList();  // Evaluated here

// Consider: Collection pooling
private static readonly ObjectPool<List<int>> ListPool = 
    new DefaultObjectPool<List<int>>(
        new ListPooledObjectPolicy<int>());

var list = ListPool.Get();
try
{
    // Use list
}
finally
{
    list.Clear();
    ListPool.Return(list);
}`}
        />
      </section>
    </div>
  )
} 
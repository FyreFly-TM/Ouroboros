using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ouroboros.StdLib.Collections;
using Ouroboros.StdLib.Math;
using Ouroboros.StdLib.IO;
using Ouroboros.StdLib.System;
using Ouroboros.StdLib.Net;
using Ouroboros.Testing;

namespace Ouroboros.Tests.Unit
{
    [TestClass]
    public class StandardLibraryTests
    {
        [Test("List operations")]
        public void TestListOperations()
        {
            var list = new List<int>();
            
            list.Add(1);
            list.Add(2);
            list.Add(3);
            
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(2, list[1]);
            
            list.Remove(2);
            Assert.AreEqual(2, list.Count);
            Assert.IsFalse(list.Contains(2));
            
            list.Clear();
            Assert.AreEqual(0, list.Count);
        }
        
        [Test("Dictionary operations")]
        public void TestDictionaryOperations()
        {
            var dict = new Dictionary<string, int>();
            
            dict["one"] = 1;
            dict["two"] = 2;
            dict["three"] = 3;
            
            Assert.AreEqual(3, dict.Count);
            Assert.AreEqual(2, dict["two"]);
            Assert.IsTrue(dict.ContainsKey("one"));
            
            dict.Remove("two");
            Assert.AreEqual(2, dict.Count);
            Assert.IsFalse(dict.ContainsKey("two"));
        }
        
        [Test("Queue operations")]
        public void TestQueueOperations()
        {
            var queue = new Queue<string>();
            
            queue.Enqueue("first");
            queue.Enqueue("second");
            queue.Enqueue("third");
            
            Assert.AreEqual(3, queue.Count);
            Assert.AreEqual("first", queue.Peek());
            
            var item = queue.Dequeue();
            Assert.AreEqual("first", item);
            Assert.AreEqual(2, queue.Count);
        }
        
        [Test("Stack operations")]
        public void TestStackOperations()
        {
            var stack = new Stack<int>();
            
            stack.Push(10);
            stack.Push(20);
            stack.Push(30);
            
            Assert.AreEqual(3, stack.Count);
            Assert.AreEqual(30, stack.Peek());
            
            var item = stack.Pop();
            Assert.AreEqual(30, item);
            Assert.AreEqual(2, stack.Count);
        }
        
        [Test("Vector operations")]
        public void TestVectorOperations()
        {
            var v1 = new Vector(1, 2, 3);
            var v2 = new Vector(4, 5, 6);
            
            var sum = v1 + v2;
            Assert.AreEqual(5, sum.X);
            Assert.AreEqual(7, sum.Y);
            Assert.AreEqual(9, sum.Z);
            
            var dot = v1.Dot(v2);
            Assert.AreEqual(32, dot); // 1*4 + 2*5 + 3*6
            
            var cross = v1.Cross(v2);
            Assert.AreEqual(-3, cross.X);
            Assert.AreEqual(6, cross.Y);
            Assert.AreEqual(-3, cross.Z);
            
            var magnitude = v1.Magnitude();
            Assert.AreEqual(Math.Sqrt(14), magnitude, 0.001);
        }
        
        [Test("Matrix operations")]
        public void TestMatrixOperations()
        {
            var m1 = Matrix.Identity(3);
            var m2 = new Matrix(3, 3);
            m2[0, 0] = 1; m2[0, 1] = 2; m2[0, 2] = 3;
            m2[1, 0] = 4; m2[1, 1] = 5; m2[1, 2] = 6;
            m2[2, 0] = 7; m2[2, 1] = 8; m2[2, 2] = 9;
            
            var product = m1 * m2;
            Assert.AreEqual(m2, product); // Identity * M = M
            
            var transpose = m2.Transpose();
            Assert.AreEqual(2, transpose[1, 0]);
            Assert.AreEqual(4, transpose[0, 1]);
        }
        
        [Test("Math functions")]
        public void TestMathFunctions()
        {
            Assert.AreEqual(0, MathFunctions.Sin(0), 0.001);
            Assert.AreEqual(1, MathFunctions.Cos(0), 0.001);
            Assert.AreEqual(Math.PI, MathFunctions.Sin(Math.PI / 2), 0.001);
            
            Assert.AreEqual(8, MathFunctions.Pow(2, 3));
            Assert.AreEqual(2, MathFunctions.Sqrt(4));
            Assert.AreEqual(1, MathFunctions.Log(Math.E), 0.001);
            
            Assert.AreEqual(5, MathFunctions.Max(3, 5));
            Assert.AreEqual(3, MathFunctions.Min(3, 5));
            Assert.AreEqual(5, MathFunctions.Abs(-5));
        }
        
        [Test("File system operations")]
        public async Task TestFileSystemOperations()
        {
            var fs = new FileSystem();
            var testFile = "test_file.txt";
            var content = "Hello, Ouroboros!";
            
            await fs.WriteTextAsync(testFile, content);
            Assert.IsTrue(fs.Exists(testFile));
            
            var readContent = await fs.ReadTextAsync(testFile);
            Assert.AreEqual(content, readContent);
            
            fs.Delete(testFile);
            Assert.IsFalse(fs.Exists(testFile));
        }
        
        [Test("DateTime operations")]
        public void TestDateTimeOperations()
        {
            var now = DateTime.Now();
            var tomorrow = now.AddDays(1);
            var yesterday = now.AddDays(-1);
            
            Assert.Greater(tomorrow, now);
            Assert.Less(yesterday, now);
            
            var diff = tomorrow - now;
            Assert.AreEqual(1, diff.Days);
            
            var formatted = now.Format("YYYY-MM-DD");
            Assert.Contains(formatted, "-");
            Assert.AreEqual(10, formatted.Length);
        }
        
        [Test("String operations")]
        public void TestStringOperations()
        {
            var str = "Hello, World!";
            
            Assert.IsTrue(str.StartsWith("Hello"));
            Assert.IsTrue(str.EndsWith("World!"));
            Assert.IsTrue(str.Contains("World"));
            
            var upper = str.ToUpper();
            Assert.AreEqual("HELLO, WORLD!", upper);
            
            var lower = str.ToLower();
            Assert.AreEqual("hello, world!", lower);
            
            var parts = str.Split(", ");
            Assert.AreEqual(2, parts.Length);
            Assert.AreEqual("Hello", parts[0]);
            Assert.AreEqual("World!", parts[1]);
            
            var replaced = str.Replace("World", "Ouroboros");
            Assert.AreEqual("Hello, Ouroboros!", replaced);
        }
        
        [Test("Async operations")]
        public async Task TestAsyncOperations()
        {
            var task1 = Task.Delay(100).ContinueWith(_ => 42);
            var task2 = Task.Delay(50).ContinueWith(_ => 24);
            
            var results = await Task.WhenAll(task1, task2);
            Assert.AreEqual(2, results.Length);
            Assert.AreEqual(42, results[0]);
            Assert.AreEqual(24, results[1]);
            
            var firstResult = await Task.WhenAny(task1, task2);
            Assert.AreEqual(24, firstResult); // task2 completes first
        }
        
        [Test("Set operations")]
        public void TestSetOperations()
        {
            var set1 = new Set<int> { 1, 2, 3, 4, 5 };
            var set2 = new Set<int> { 3, 4, 5, 6, 7 };
            
            var union = SetOperations.Union(set1, set2);
            Assert.AreEqual(7, union.Count); // 1,2,3,4,5,6,7
            
            var intersection = SetOperations.Intersection(set1, set2);
            Assert.AreEqual(3, intersection.Count); // 3,4,5
            
            var difference = SetOperations.Difference(set1, set2);
            Assert.AreEqual(2, difference.Count); // 1,2
            
            var symmetric = SetOperations.SymmetricDifference(set1, set2);
            Assert.AreEqual(4, symmetric.Count); // 1,2,6,7
        }
        
        [Test("HTTP client operations")]
        public async Task TestHttpClient()
        {
            var client = new HttpClient();
            
            // Test GET request
            var response = await client.GetAsync("https://api.example.com/test");
            Assert.IsNotNull(response);
            
            // Test POST request with JSON
            var data = new { name = "test", value = 42 };
            var postResponse = await client.PostJsonAsync("https://api.example.com/test", data);
            Assert.IsNotNull(postResponse);
        }
        
        [Test("Console operations")]
        public void TestConsoleOperations()
        {
            // Redirect console output for testing
            var originalOut = System.Console.Out;
            var writer = new System.IO.StringWriter();
            System.Console.SetOut(writer);
            
            Console.WriteLine("Test output");
            Console.Write("No newline");
            
            var output = writer.ToString();
            Assert.Contains(output, "Test output");
            Assert.Contains(output, "No newline");
            
            System.Console.SetOut(originalOut);
        }
    }
} 
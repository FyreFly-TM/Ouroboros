using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ouro.Core.Compiler;
using Ouro.Core.Lexer;
using Ouro.Core.Parser;
using Ouro.Core.VM;
using Ouro.Runtime;
using Ouro.Testing;

namespace Ouroboros.Tests.Performance
{
    [TestClass]
    public class BenchmarkSuite
    {
        private const int WarmupIterations = 5;
        private const int BenchmarkIterations = 100;
        private Compiler compiler;
        private VirtualMachine vm;
        private Runtime.Runtime runtime;

        public void Setup()
        {
            compiler = new Compiler();
            vm = new VirtualMachine();
            runtime = new Runtime.Runtime();
        }

        [Test("Lexer performance benchmark")]
        public void BenchmarkLexer()
        {
            var source = GenerateLargeSource(10000); // 10k lines
            var lexer = new Lexer(source);

            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
            {
                lexer.Reset();
                while (lexer.NextToken().Type != TokenType.EOF) { }
            }

            // Benchmark
            var times = new List<double>();
            for (int i = 0; i < BenchmarkIterations; i++)
            {
                lexer.Reset();
                var sw = Stopwatch.StartNew();
                while (lexer.NextToken().Type != TokenType.EOF) { }
                sw.Stop();
                times.Add(sw.Elapsed.TotalMilliseconds);
            }

            ReportBenchmark("Lexer", times, source.Length);
        }

        [Test("Parser performance benchmark")]
        public void BenchmarkParser()
        {
            var source = GenerateComplexSource(1000); // 1k functions
            
            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
            {
                var lexer = new Lexer(source);
                var parser = new Parser(lexer);
                parser.Parse();
            }

            // Benchmark
            var times = new List<double>();
            for (int i = 0; i < BenchmarkIterations; i++)
            {
                var lexer = new Lexer(source);
                var parser = new Parser(lexer);
                var sw = Stopwatch.StartNew();
                parser.Parse();
                sw.Stop();
                times.Add(sw.Elapsed.TotalMilliseconds);
            }

            ReportBenchmark("Parser", times, source.Length);
        }

        [Test("Type checker performance benchmark")]
        public void BenchmarkTypeChecker()
        {
            var source = GenerateTypeIntensiveSource(500);
            var lexer = new Lexer(source);
            var parser = new Parser(lexer);
            var ast = parser.Parse();

            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
            {
                var typeChecker = new TypeChecker();
                typeChecker.Check(ast);
            }

            // Benchmark
            var times = new List<double>();
            for (int i = 0; i < BenchmarkIterations; i++)
            {
                var typeChecker = new TypeChecker();
                var sw = Stopwatch.StartNew();
                typeChecker.Check(ast);
                sw.Stop();
                times.Add(sw.Elapsed.TotalMilliseconds);
            }

            ReportBenchmark("Type Checker", times, ast.NodeCount);
        }

        [Test("Compiler end-to-end benchmark")]
        public void BenchmarkCompiler()
        {
            var source = GenerateRealWorldProgram();

            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
            {
                compiler.Compile(source);
            }

            // Benchmark
            var times = new List<double>();
            for (int i = 0; i < BenchmarkIterations; i++)
            {
                var sw = Stopwatch.StartNew();
                compiler.Compile(source);
                sw.Stop();
                times.Add(sw.Elapsed.TotalMilliseconds);
            }

            ReportBenchmark("Compiler E2E", times, source.Length);
        }

        [Test("VM execution performance benchmark")]
        public void BenchmarkVMExecution()
        {
            // Fibonacci benchmark
            var fibSource = @"
                function fibonacci(n: int): int {
                    if (n <= 1) return n;
                    return fibonacci(n - 1) + fibonacci(n - 2);
                }
                
                function main() {
                    return fibonacci(30);
                }
            ";

            var bytecode = compiler.Compile(fibSource);

            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
            {
                vm.Execute(bytecode);
            }

            // Benchmark
            var times = new List<double>();
            for (int i = 0; i < BenchmarkIterations; i++)
            {
                var sw = Stopwatch.StartNew();
                vm.Execute(bytecode);
                sw.Stop();
                times.Add(sw.Elapsed.TotalMilliseconds);
            }

            ReportBenchmark("VM Fibonacci(30)", times, 1);
        }

        [Test("Memory allocation benchmark")]
        public void BenchmarkMemoryAllocation()
        {
            var source = @"
                function allocTest() {
                    let arrays = new List<Array<int>>();
                    
                    // Allocate many small arrays
                    for (let i = 0; i < 10000; i++) {
                        arrays.add(new Array<int>(100));
                    }
                    
                    // Access them randomly
                    let sum = 0;
                    for (let i = 0; i < 1000; i++) {
                        let idx = random(0, arrays.length);
                        sum += arrays[idx].length;
                    }
                    
                    return sum;
                }
            ";

            var bytecode = compiler.Compile(source);
            
            // Get baseline memory
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var baselineMemory = GC.GetTotalMemory(false);

            // Benchmark
            var times = new List<double>();
            var memoryUsages = new List<long>();
            
            for (int i = 0; i < BenchmarkIterations; i++)
            {
                GC.Collect();
                var startMemory = GC.GetTotalMemory(false);
                
                var sw = Stopwatch.StartNew();
                vm.Execute(bytecode);
                sw.Stop();
                
                var endMemory = GC.GetTotalMemory(false);
                
                times.Add(sw.Elapsed.TotalMilliseconds);
                memoryUsages.Add(endMemory - startMemory);
            }

            ReportMemoryBenchmark("Memory Allocation", times, memoryUsages);
        }

        [Test("Optimization impact benchmark")]
        public void BenchmarkOptimizationImpact()
        {
            var source = GenerateOptimizableCode();

            // Compile without optimization
            var unoptimized = compiler.Compile(source, optimize: false);
            
            // Compile with optimization
            var optimized = compiler.Compile(source, optimize: true);

            // Benchmark unoptimized
            var unoptTimes = new List<double>();
            for (int i = 0; i < BenchmarkIterations; i++)
            {
                var sw = Stopwatch.StartNew();
                vm.Execute(unoptimized);
                sw.Stop();
                unoptTimes.Add(sw.Elapsed.TotalMilliseconds);
            }

            // Benchmark optimized
            var optTimes = new List<double>();
            for (int i = 0; i < BenchmarkIterations; i++)
            {
                var sw = Stopwatch.StartNew();
                vm.Execute(optimized);
                sw.Stop();
                optTimes.Add(sw.Elapsed.TotalMilliseconds);
            }

            ReportOptimizationImpact("Optimization Impact", unoptTimes, optTimes);
        }

        [Test("Concurrent execution benchmark")]
        public async Task BenchmarkConcurrentExecution()
        {
            var source = @"
                async function worker(id: int): Task<int> {
                    let sum = 0;
                    for (let i = 0; i < 10000; i++) {
                        sum += i * id;
                        if (i % 1000 == 0) {
                            await Task.yield();
                        }
                    }
                    return sum;
                }
                
                async function main() {
                    let tasks = new List<Task<int>>();
                    
                    for (let i = 0; i < 100; i++) {
                        tasks.add(worker(i));
                    }
                    
                    let results = await Task.whenAll(tasks);
                    return results.sum();
                }
            ";

            var bytecode = compiler.Compile(source);

            // Benchmark
            var times = new List<double>();
            for (int i = 0; i < BenchmarkIterations / 10; i++) // Fewer iterations for async
            {
                var sw = Stopwatch.StartNew();
                await runtime.ExecuteAsync(bytecode);
                sw.Stop();
                times.Add(sw.Elapsed.TotalMilliseconds);
            }

            ReportBenchmark("Concurrent Execution", times, 100); // 100 concurrent tasks
        }

        [Test("Garbage collection pressure test")]
        public void BenchmarkGarbageCollectionPressure()
        {
            var source = @"
                function gcPressure() {
                    // Create and discard many objects
                    for (let i = 0; i < 100000; i++) {
                        let obj = {
                            data: new Array(100),
                            next: null,
                            value: i
                        };
                        
                        // Create some references
                        if (i > 0 && i % 10 == 0) {
                            obj.next = obj;  // Self reference
                        }
                    }
                    
                    // Force collection
                    gc();
                    
                    return getMemoryUsage();
                }
            ";

            var bytecode = compiler.Compile(source);
            
            var gcCounts = new List<(int gen0, int gen1, int gen2)>();
            var times = new List<double>();

            for (int i = 0; i < BenchmarkIterations / 10; i++)
            {
                var gen0Before = GC.CollectionCount(0);
                var gen1Before = GC.CollectionCount(1);
                var gen2Before = GC.CollectionCount(2);

                var sw = Stopwatch.StartNew();
                vm.Execute(bytecode);
                sw.Stop();

                var gen0After = GC.CollectionCount(0);
                var gen1After = GC.CollectionCount(1);
                var gen2After = GC.CollectionCount(2);

                times.Add(sw.Elapsed.TotalMilliseconds);
                gcCounts.Add((gen0After - gen0Before, gen1After - gen1Before, gen2After - gen2Before));
            }

            ReportGCBenchmark("GC Pressure", times, gcCounts);
        }

        // Helper methods for generating test code
        private string GenerateLargeSource(int lines)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < lines; i++)
            {
                sb.AppendLine($"let var{i} = {i} + {i * 2};  // Comment {i}");
            }
            return sb.ToString();
        }

        private string GenerateComplexSource(int functionCount)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < functionCount; i++)
            {
                sb.AppendLine($@"
                    function func{i}(a: int, b: float, c: string): bool {{
                        let x = a + b.toInt();
                        let y = c.length > x;
                        if (y) {{
                            return func{(i + 1) % functionCount}(x, b * 2, c + ""!"");
                        }}
                        return false;
                    }}
                ");
            }
            return sb.ToString();
        }

        private string GenerateTypeIntensiveSource(int classCount)
        {
            var sb = new System.Text.StringBuilder();
            
            // Generate interfaces
            for (int i = 0; i < classCount / 2; i++)
            {
                sb.AppendLine($@"
                    interface IInterface{i}<T> {{
                        function method{i}(param: T): T;
                        property prop{i}: T {{ get; set; }}
                    }}
                ");
            }

            // Generate classes with complex inheritance
            for (int i = 0; i < classCount; i++)
            {
                var interfaces = string.Join(", ", 
                    Enumerable.Range(0, 3).Select(j => $"IInterface{(i + j) % (classCount / 2)}<Class{(i + j + 1) % classCount}>"));
                
                sb.AppendLine($@"
                    class Class{i} : {interfaces} {{
                        private field{i}: int = {i};
                        
                        function method{i}(param: Class{(i + 1) % classCount}): Class{(i + 1) % classCount} {{
                            return param;
                        }}
                        
                        property prop{i}: Class{(i + 1) % classCount} {{ 
                            get => new Class{(i + 1) % classCount}();
                            set => field{i} = value.field{(i + 1) % classCount};
                        }}
                    }}
                ");
            }

            return sb.ToString();
        }

        private string GenerateRealWorldProgram()
        {
            return @"
                import { HttpClient } from 'Ouroboros.Net';
                import { Json } from 'Ouroboros.Data';
                import { List, Dictionary } from 'Ouroboros.Collections';

                class User {
                    id: int;
                    name: string;
                    email: string;
                    posts: List<Post>;
                }

                class Post {
                    id: int;
                    title: string;
                    content: string;
                    authorId: int;
                    tags: List<string>;
                    createdAt: DateTime;
                }

                class BlogService {
                    private http: HttpClient;
                    private cache: Dictionary<int, User>;

                    constructor() {
                        this.http = new HttpClient();
                        this.cache = new Dictionary<int, User>();
                    }

                    async function getUser(id: int): Task<User> {
                        if (cache.containsKey(id)) {
                            return cache[id];
                        }

                        let response = await http.get($'https://api.example.com/users/{id}');
                        let user = Json.deserialize<User>(await response.text());
                        cache[id] = user;
                        return user;
                    }

                    async function getUserPosts(userId: int): Task<List<Post>> {
                        let response = await http.get($'https://api.example.com/users/{userId}/posts');
                        return Json.deserialize<List<Post>>(await response.text());
                    }

                    async function createPost(post: Post): Task<Post> {
                        let json = Json.serialize(post);
                        let response = await http.post('https://api.example.com/posts', json);
                        return Json.deserialize<Post>(await response.text());
                    }

                    function analyzeUserActivity(user: User): Dictionary<string, int> {
                        let tagFrequency = new Dictionary<string, int>();
                        
                        for (let post in user.posts) {
                            for (let tag in post.tags) {
                                if (tagFrequency.containsKey(tag)) {
                                    tagFrequency[tag]++;
                                } else {
                                    tagFrequency[tag] = 1;
                                }
                            }
                        }
                        
                        return tagFrequency;
                    }
                }

                async function main() {
                    let service = new BlogService();
                    let users = new List<User>();
                    
                    // Fetch multiple users
                    for (let i = 1; i <= 10; i++) {
                        users.add(await service.getUser(i));
                    }
                    
                    // Analyze all users
                    let totalTags = new Dictionary<string, int>();
                    for (let user in users) {
                        let userTags = service.analyzeUserActivity(user);
                        for (let [tag, count] in userTags) {
                            if (totalTags.containsKey(tag)) {
                                totalTags[tag] += count;
                            } else {
                                totalTags[tag] = count;
                            }
                        }
                    }
                    
                    // Find most popular tags
                    let popularTags = totalTags.entries()
                        .sortBy(e => e.value)
                        .reverse()
                        .take(10)
                        .select(e => e.key)
                        .toList();
                    
                    print($'Most popular tags: {popularTags.join("", "")}');
                }
            ";
        }

        private string GenerateOptimizableCode()
        {
            return @"
                function optimizableLoop() {
                    let sum = 0;
                    
                    // Constant folding opportunities
                    for (let i = 0; i < 1000; i++) {
                        sum += 2 * 3 + 4 * 5;  // Should fold to 26
                        sum += 10 / 2 - 3;     // Should fold to 2
                    }
                    
                    // Dead code elimination
                    if (false) {
                        for (let j = 0; j < 1000000; j++) {
                            sum += j * j * j;
                        }
                    }
                    
                    // Loop invariant code motion
                    let multiplier = getMultiplier();
                    for (let k = 0; k < 10000; k++) {
                        sum += k * multiplier * 2;  // multiplier * 2 should be hoisted
                    }
                    
                    // Function inlining
                    for (let m = 0; m < 1000; m++) {
                        sum += simpleAdd(m, m + 1);
                    }
                    
                    return sum;
                }
                
                inline function simpleAdd(a: int, b: int): int {
                    return a + b;
                }
                
                function getMultiplier(): int {
                    return 42;
                }
            ";
        }

        // Reporting methods
        private void ReportBenchmark(string name, List<double> times, long dataSize)
        {
            times.Sort();
            var avg = times.Average();
            var median = times[times.Count / 2];
            var p95 = times[(int)(times.Count * 0.95)];
            var p99 = times[(int)(times.Count * 0.99)];
            var min = times.First();
            var max = times.Last();

            Console.WriteLine($"\n📊 {name} Benchmark Results:");
            Console.WriteLine($"   Data Size: {dataSize:N0}");
            Console.WriteLine($"   Iterations: {times.Count}");
            Console.WriteLine($"   Average: {avg:F2}ms");
            Console.WriteLine($"   Median: {median:F2}ms");
            Console.WriteLine($"   P95: {p95:F2}ms");
            Console.WriteLine($"   P99: {p99:F2}ms");
            Console.WriteLine($"   Min: {min:F2}ms");
            Console.WriteLine($"   Max: {max:F2}ms");
            
            if (dataSize > 0)
            {
                var throughput = dataSize / (avg / 1000.0);
                Console.WriteLine($"   Throughput: {throughput:N0} units/sec");
            }
        }

        private void ReportMemoryBenchmark(string name, List<double> times, List<long> memoryUsages)
        {
            var avgTime = times.Average();
            var avgMemory = memoryUsages.Average();
            var maxMemory = memoryUsages.Max();

            Console.WriteLine($"\n💾 {name} Memory Benchmark:");
            Console.WriteLine($"   Average Time: {avgTime:F2}ms");
            Console.WriteLine($"   Average Memory: {avgMemory / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine($"   Peak Memory: {maxMemory / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine($"   Memory/Time: {(avgMemory / avgTime):F2} bytes/ms");
        }

        private void ReportOptimizationImpact(string name, List<double> unoptTimes, List<double> optTimes)
        {
            var avgUnopt = unoptTimes.Average();
            var avgOpt = optTimes.Average();
            var improvement = ((avgUnopt - avgOpt) / avgUnopt) * 100;

            Console.WriteLine($"\n⚡ {name}:");
            Console.WriteLine($"   Unoptimized: {avgUnopt:F2}ms");
            Console.WriteLine($"   Optimized: {avgOpt:F2}ms");
            Console.WriteLine($"   Improvement: {improvement:F1}%");
            Console.WriteLine($"   Speedup: {avgUnopt / avgOpt:F2}x");
        }

        private void ReportGCBenchmark(string name, List<double> times, List<(int gen0, int gen1, int gen2)> gcCounts)
        {
            var avgTime = times.Average();
            var totalGen0 = gcCounts.Sum(gc => gc.gen0);
            var totalGen1 = gcCounts.Sum(gc => gc.gen1);
            var totalGen2 = gcCounts.Sum(gc => gc.gen2);

            Console.WriteLine($"\n♻️  {name}:");
            Console.WriteLine($"   Average Time: {avgTime:F2}ms");
            Console.WriteLine($"   Gen0 Collections: {totalGen0}");
            Console.WriteLine($"   Gen1 Collections: {totalGen1}");
            Console.WriteLine($"   Gen2 Collections: {totalGen2}");
            Console.WriteLine($"   Total Collections: {totalGen0 + totalGen1 + totalGen2}");
        }
    }
} 
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ouroboros.Core.Compiler;
using Ouroboros.Core.Lexer;
using Ouroboros.Core.Parser;
using Ouroboros.Core.VM;

namespace Ouroboros.Testing.Infrastructure
{
    /// <summary>
    /// Fuzzing framework for discovering bugs through random input generation
    /// </summary>
    public class FuzzTesting
    {
        private readonly Random random = new();
        private readonly List<FuzzResult> results = new();
        private readonly object lockObject = new();
        private int totalIterations = 0;
        private int crashCount = 0;
        private int timeoutCount = 0;
        private int successCount = 0;

        public FuzzingOptions Options { get; set; } = new();

        /// <summary>
        /// Run fuzzing test
        /// </summary>
        public async Task<FuzzingReport> RunFuzzTestAsync(string name, Func<string, Task> testFunc, 
            TimeSpan duration, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"🔨 Starting fuzz test: {name}");
            Console.WriteLine($"   Duration: {duration}");
            Console.WriteLine($"   Max input size: {Options.MaxInputSize}");
            Console.WriteLine($"   Timeout: {Options.Timeout}ms");

            var startTime = DateTime.Now;
            var endTime = startTime + duration;
            var tasks = new List<Task>();

            // Run parallel fuzzing tasks
            for (int i = 0; i < Options.ParallelDegree; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    while (DateTime.Now < endTime && !cancellationToken.IsCancellationRequested)
                    {
                        await RunSingleFuzzIteration(name, testFunc);
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);

            return GenerateReport(name, DateTime.Now - startTime);
        }

        /// <summary>
        /// Run mutation-based fuzzing
        /// </summary>
        public async Task<FuzzingReport> RunMutationFuzzTestAsync(string name, 
            IEnumerable<string> seedInputs, Func<string, Task> testFunc, 
            TimeSpan duration, CancellationToken cancellationToken = default)
        {
            var corpus = new List<string>(seedInputs);
            var interestingInputs = new List<string>();

            Console.WriteLine($"🔧 Starting mutation-based fuzz test: {name}");
            Console.WriteLine($"   Seed inputs: {corpus.Count}");

            var startTime = DateTime.Now;
            var endTime = startTime + duration;

            while (DateTime.Now < endTime && !cancellationToken.IsCancellationRequested)
            {
                // Select input from corpus
                var input = corpus[random.Next(corpus.Count)];
                
                // Mutate the input
                var mutated = MutateInput(input);

                // Test the mutated input
                var result = await TestInput(name, mutated, testFunc);

                // If interesting, add to corpus
                if (result.IsInteresting)
                {
                    corpus.Add(mutated);
                    interestingInputs.Add(mutated);
                    
                    // Limit corpus size
                    if (corpus.Count > Options.MaxCorpusSize)
                    {
                        corpus.RemoveAt(random.Next(corpus.Count));
                    }
                }
            }

            var report = GenerateReport(name, DateTime.Now - startTime);
            report.InterestingInputs = interestingInputs;
            return report;
        }

        /// <summary>
        /// Grammar-based fuzzing for structured inputs
        /// </summary>
        public async Task<FuzzingReport> RunGrammarFuzzTestAsync(string name, 
            Grammar grammar, Func<string, Task> testFunc, 
            TimeSpan duration, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"📝 Starting grammar-based fuzz test: {name}");

            var startTime = DateTime.Now;
            var endTime = startTime + duration;

            while (DateTime.Now < endTime && !cancellationToken.IsCancellationRequested)
            {
                // Generate input from grammar
                var input = GenerateFromGrammar(grammar, Options.MaxDepth);
                
                // Test the generated input
                await RunSingleFuzzIteration(name, testFunc, input);
            }

            return GenerateReport(name, DateTime.Now - startTime);
        }

        private async Task RunSingleFuzzIteration(string name, Func<string, Task> testFunc, string input = null)
        {
            input ??= GenerateRandomInput();
            await TestInput(name, input, testFunc);
        }

        private async Task<FuzzTestResult> TestInput(string name, string input, Func<string, Task> testFunc)
        {
            Interlocked.Increment(ref totalIterations);
            var result = new FuzzTestResult
            {
                Input = input,
                Timestamp = DateTime.Now
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var cts = new CancellationTokenSource(Options.Timeout);
                await testFunc(input).WaitAsync(cts.Token);
                
                stopwatch.Stop();
                result.Success = true;
                result.ExecutionTime = stopwatch.Elapsed;
                Interlocked.Increment(ref successCount);
            }
            catch (OperationCanceledException)
            {
                result.Success = false;
                result.ErrorType = "Timeout";
                result.ErrorMessage = $"Test exceeded timeout of {Options.Timeout}ms";
                Interlocked.Increment(ref timeoutCount);
                result.IsInteresting = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorType = ex.GetType().Name;
                result.ErrorMessage = ex.Message;
                result.StackTrace = ex.StackTrace;
                Interlocked.Increment(ref crashCount);
                result.IsInteresting = true;
            }

            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;

            // Check for interesting behaviors
            if (result.ExecutionTime.TotalMilliseconds > Options.SlowThreshold)
            {
                result.IsInteresting = true;
                result.InterestingReason = "Slow execution";
            }

            lock (lockObject)
            {
                results.Add(result);
                
                // Save crash inputs
                if (!result.Success && Options.SaveCrashInputs)
                {
                    SaveCrashInput(name, result);
                }
            }

            // Progress reporting
            if (totalIterations % 1000 == 0)
            {
                Console.WriteLine($"   Progress: {totalIterations} iterations, {crashCount} crashes, {timeoutCount} timeouts");
            }

            return result;
        }

        private string GenerateRandomInput()
        {
            var size = random.Next(1, Options.MaxInputSize);
            var strategy = (InputGenerationStrategy)random.Next(Enum.GetValues<InputGenerationStrategy>().Length);

            return strategy switch
            {
                InputGenerationStrategy.RandomAscii => GenerateRandomAscii(size),
                InputGenerationStrategy.RandomUnicode => GenerateRandomUnicode(size),
                InputGenerationStrategy.RandomBinary => GenerateRandomBinary(size),
                InputGenerationStrategy.ValidCode => GenerateValidCode(size),
                InputGenerationStrategy.PartiallyValidCode => GeneratePartiallyValidCode(size),
                _ => GenerateRandomAscii(size)
            };
        }

        private string GenerateRandomAscii(int size)
        {
            var sb = new StringBuilder(size);
            for (int i = 0; i < size; i++)
            {
                sb.Append((char)random.Next(32, 127));
            }
            return sb.ToString();
        }

        private string GenerateRandomUnicode(int size)
        {
            var sb = new StringBuilder(size);
            for (int i = 0; i < size; i++)
            {
                sb.Append((char)random.Next(1, 0x10000));
            }
            return sb.ToString();
        }

        private string GenerateRandomBinary(int size)
        {
            var bytes = new byte[size];
            random.NextBytes(bytes);
            return Encoding.UTF8.GetString(bytes);
        }

        private string GenerateValidCode(int size)
        {
            var elements = new[]
            {
                "function test() { ",
                "let x = ",
                "if (true) { ",
                "for (let i = 0; i < 10; i++) { ",
                "while (false) { ",
                "return ",
                "}", 
                ";",
                "42",
                "\"string\"",
                "null",
                "true",
                "false",
                "[1, 2, 3]",
                "{ key: \"value\" }",
                "x + y",
                "function() { }",
                "class Test { }",
                "async function() { await x; }"
            };

            var sb = new StringBuilder();
            while (sb.Length < size)
            {
                sb.Append(elements[random.Next(elements.Length)]);
                sb.Append(' ');
            }

            return sb.ToString(0, Math.Min(sb.Length, size));
        }

        private string GeneratePartiallyValidCode(int size)
        {
            var validCode = GenerateValidCode(size * 2);
            var corruption = random.Next(3);

            return corruption switch
            {
                0 => validCode.Substring(0, size), // Truncate
                1 => validCode.Insert(random.Next(validCode.Length), GenerateRandomAscii(10)), // Insert garbage
                2 => validCode.Remove(random.Next(validCode.Length / 2), random.Next(validCode.Length / 4)), // Remove section
                _ => validCode
            };
        }

        private string MutateInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return GenerateRandomInput();

            var strategy = (MutationStrategy)random.Next(Enum.GetValues<MutationStrategy>().Length);

            return strategy switch
            {
                MutationStrategy.BitFlip => MutateBitFlip(input),
                MutationStrategy.ByteSubstitution => MutateByteSubstitution(input),
                MutationStrategy.Insertion => MutateInsertion(input),
                MutationStrategy.Deletion => MutateDeletion(input),
                MutationStrategy.Duplication => MutateDuplication(input),
                MutationStrategy.Shuffling => MutateShuffling(input),
                _ => input
            };
        }

        private string MutateBitFlip(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            if (bytes.Length == 0) return input;

            var byteIndex = random.Next(bytes.Length);
            var bitIndex = random.Next(8);
            bytes[byteIndex] ^= (byte)(1 << bitIndex);

            return Encoding.UTF8.GetString(bytes);
        }

        private string MutateByteSubstitution(string input)
        {
            var chars = input.ToCharArray();
            if (chars.Length == 0) return input;

            var index = random.Next(chars.Length);
            chars[index] = (char)random.Next(256);

            return new string(chars);
        }

        private string MutateInsertion(string input)
        {
            var insertPos = random.Next(input.Length + 1);
            var insertLen = random.Next(1, Math.Min(100, Options.MaxInputSize - input.Length));
            var insertion = GenerateRandomAscii(insertLen);

            return input.Insert(insertPos, insertion);
        }

        private string MutateDeletion(string input)
        {
            if (input.Length <= 1) return input;

            var deletePos = random.Next(input.Length);
            var deleteLen = random.Next(1, Math.Min(input.Length - deletePos, input.Length / 4));

            return input.Remove(deletePos, deleteLen);
        }

        private string MutateDuplication(string input)
        {
            if (input.Length == 0) return input;

            var start = random.Next(input.Length);
            var len = random.Next(1, Math.Min(input.Length - start, 100));
            var chunk = input.Substring(start, len);
            var insertPos = random.Next(input.Length + 1);

            return input.Insert(insertPos, chunk);
        }

        private string MutateShuffling(string input)
        {
            if (input.Length <= 2) return input;

            var start = random.Next(input.Length - 1);
            var len = random.Next(2, Math.Min(input.Length - start, 20));
            var chunk = input.Substring(start, len).ToCharArray();
            
            // Fisher-Yates shuffle
            for (int i = chunk.Length - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (chunk[i], chunk[j]) = (chunk[j], chunk[i]);
            }

            return input.Remove(start, len).Insert(start, new string(chunk));
        }

        private string GenerateFromGrammar(Grammar grammar, int maxDepth, string symbol = null)
        {
            symbol ??= grammar.StartSymbol;
            
            if (maxDepth <= 0 || !grammar.Productions.ContainsKey(symbol))
                return symbol;

            var productions = grammar.Productions[symbol];
            var production = productions[random.Next(productions.Count)];

            var result = new StringBuilder();
            foreach (var token in production)
            {
                if (grammar.IsNonTerminal(token))
                {
                    result.Append(GenerateFromGrammar(grammar, maxDepth - 1, token));
                }
                else
                {
                    result.Append(token);
                }
            }

            return result.ToString();
        }

        private void SaveCrashInput(string testName, FuzzTestResult result)
        {
            var crashDir = Path.Combine("fuzz_crashes", testName);
            Directory.CreateDirectory(crashDir);

            var filename = $"crash_{result.Timestamp:yyyyMMdd_HHmmss}_{result.ErrorType}.txt";
            var filepath = Path.Combine(crashDir, filename);

            var content = $@"Crash Report
============
Test: {testName}
Time: {result.Timestamp}
Error Type: {result.ErrorType}
Error Message: {result.ErrorMessage}

Input Length: {result.Input.Length}
Execution Time: {result.ExecutionTime}

Stack Trace:
{result.StackTrace}

Input:
{result.Input}";

            File.WriteAllText(filepath, content);
        }

        private FuzzingReport GenerateReport(string name, TimeSpan duration)
        {
            lock (lockObject)
            {
                var report = new FuzzingReport
                {
                    TestName = name,
                    Duration = duration,
                    TotalIterations = totalIterations,
                    SuccessCount = successCount,
                    CrashCount = crashCount,
                    TimeoutCount = timeoutCount,
                    IterationsPerSecond = totalIterations / duration.TotalSeconds
                };

                // Group crashes by error type
                report.CrashesByType = results
                    .Where(r => !r.Success)
                    .GroupBy(r => r.ErrorType)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Find unique crashes
                report.UniqueCrashes = results
                    .Where(r => !r.Success)
                    .GroupBy(r => r.ErrorMessage)
                    .Select(g => g.First())
                    .ToList();

                // Performance statistics
                var successfulRuns = results.Where(r => r.Success).ToList();
                if (successfulRuns.Any())
                {
                    var times = successfulRuns.Select(r => r.ExecutionTime.TotalMilliseconds).OrderBy(t => t).ToList();
                    report.MedianExecutionTime = TimeSpan.FromMilliseconds(times[times.Count / 2]);
                    report.P95ExecutionTime = TimeSpan.FromMilliseconds(times[(int)(times.Count * 0.95)]);
                    report.MaxExecutionTime = TimeSpan.FromMilliseconds(times.Last());
                }

                return report;
            }
        }
    }

    public class FuzzingOptions
    {
        public int MaxInputSize { get; set; } = 10000;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
        public int ParallelDegree { get; set; } = Environment.ProcessorCount;
        public bool SaveCrashInputs { get; set; } = true;
        public int MaxCorpusSize { get; set; } = 1000;
        public int MaxDepth { get; set; } = 10;
        public double SlowThreshold { get; set; } = 1000; // ms
    }

    public class FuzzTestResult
    {
        public string Input { get; set; }
        public bool Success { get; set; }
        public string ErrorType { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsInteresting { get; set; }
        public string InterestingReason { get; set; }
    }

    public class FuzzingReport
    {
        public string TestName { get; set; }
        public TimeSpan Duration { get; set; }
        public int TotalIterations { get; set; }
        public int SuccessCount { get; set; }
        public int CrashCount { get; set; }
        public int TimeoutCount { get; set; }
        public double IterationsPerSecond { get; set; }
        public Dictionary<string, int> CrashesByType { get; set; }
        public List<FuzzTestResult> UniqueCrashes { get; set; }
        public List<string> InterestingInputs { get; set; }
        public TimeSpan MedianExecutionTime { get; set; }
        public TimeSpan P95ExecutionTime { get; set; }
        public TimeSpan MaxExecutionTime { get; set; }

        public void PrintSummary()
        {
            Console.WriteLine("\n📊 Fuzzing Report");
            Console.WriteLine("================");
            Console.WriteLine($"Test: {TestName}");
            Console.WriteLine($"Duration: {Duration}");
            Console.WriteLine($"Total iterations: {TotalIterations:N0}");
            Console.WriteLine($"Iterations/sec: {IterationsPerSecond:F2}");
            Console.WriteLine($"Success: {SuccessCount:N0} ({(double)SuccessCount / TotalIterations * 100:F2}%)");
            Console.WriteLine($"Crashes: {CrashCount:N0} ({(double)CrashCount / TotalIterations * 100:F2}%)");
            Console.WriteLine($"Timeouts: {TimeoutCount:N0} ({(double)TimeoutCount / TotalIterations * 100:F2}%)");

            if (CrashesByType?.Any() == true)
            {
                Console.WriteLine("\nCrashes by type:");
                foreach (var (type, count) in CrashesByType.OrderByDescending(kvp => kvp.Value))
                {
                    Console.WriteLine($"  {type}: {count}");
                }
            }

            if (UniqueCrashes?.Any() == true)
            {
                Console.WriteLine($"\nUnique crashes: {UniqueCrashes.Count}");
            }

            Console.WriteLine($"\nPerformance:");
            Console.WriteLine($"  Median: {MedianExecutionTime.TotalMilliseconds:F2}ms");
            Console.WriteLine($"  P95: {P95ExecutionTime.TotalMilliseconds:F2}ms");
            Console.WriteLine($"  Max: {MaxExecutionTime.TotalMilliseconds:F2}ms");
        }
    }

    public class Grammar
    {
        public string StartSymbol { get; set; }
        public Dictionary<string, List<List<string>>> Productions { get; set; } = new();
        private HashSet<string> nonTerminals = new();

        public void AddProduction(string nonTerminal, params string[] production)
        {
            if (!Productions.ContainsKey(nonTerminal))
            {
                Productions[nonTerminal] = new List<List<string>>();
                nonTerminals.Add(nonTerminal);
            }
            Productions[nonTerminal].Add(production.ToList());
        }

        public bool IsNonTerminal(string symbol) => nonTerminals.Contains(symbol);
    }

    public enum InputGenerationStrategy
    {
        RandomAscii,
        RandomUnicode,
        RandomBinary,
        ValidCode,
        PartiallyValidCode
    }

    public enum MutationStrategy
    {
        BitFlip,
        ByteSubstitution,
        Insertion,
        Deletion,
        Duplication,
        Shuffling
    }
} 
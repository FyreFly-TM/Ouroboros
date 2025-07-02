using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Ouroboros.Testing;

namespace Ouroboros.Testing.Infrastructure
{
    /// <summary>
    /// Continuous testing framework with file watching and automatic test execution
    /// </summary>
    public class ContinuousTesting
    {
        private readonly FileSystemWatcher sourceWatcher;
        private readonly FileSystemWatcher testWatcher;
        private readonly Dictionary<string, DateTime> lastRunTimes = new();
        private readonly Queue<TestRunRequest> testQueue = new();
        private readonly object lockObject = new();
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private Task workerTask;

        public ContinuousTestingOptions Options { get; set; } = new();
        public event EventHandler<TestRunResult> TestRunCompleted;
        public event EventHandler<string> FileChanged;

        public ContinuousTesting(string sourceDirectory, string testDirectory)
        {
            // Watch source files
            sourceWatcher = new FileSystemWatcher(sourceDirectory)
            {
                Filter = "*.cs",
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
            };

            sourceWatcher.Changed += OnSourceFileChanged;
            sourceWatcher.Created += OnSourceFileChanged;
            sourceWatcher.Deleted += OnSourceFileChanged;
            sourceWatcher.Renamed += OnSourceFileRenamed;

            // Watch test files
            testWatcher = new FileSystemWatcher(testDirectory)
            {
                Filter = "*.cs",
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
            };

            testWatcher.Changed += OnTestFileChanged;
            testWatcher.Created += OnTestFileChanged;
            testWatcher.Deleted += OnTestFileChanged;
            testWatcher.Renamed += OnTestFileRenamed;
        }

        /// <summary>
        /// Start continuous testing
        /// </summary>
        public void Start()
        {
            Console.WriteLine("🔄 Starting continuous testing...");
            Console.WriteLine($"   Source directory: {sourceWatcher.Path}");
            Console.WriteLine($"   Test directory: {testWatcher.Path}");
            Console.WriteLine($"   Debounce delay: {Options.DebounceDelay}ms");

            sourceWatcher.EnableRaisingEvents = true;
            testWatcher.EnableRaisingEvents = true;

            // Start worker task
            workerTask = Task.Run(ProcessTestQueue, cancellationTokenSource.Token);

            // Run initial test suite
            QueueTestRun(new TestRunRequest
            {
                Type = TestRunType.Full,
                Reason = "Initial run"
            });

            Console.WriteLine("✅ Continuous testing started. Press Ctrl+C to stop.");
        }

        /// <summary>
        /// Stop continuous testing
        /// </summary>
        public void Stop()
        {
            Console.WriteLine("🛑 Stopping continuous testing...");

            sourceWatcher.EnableRaisingEvents = false;
            testWatcher.EnableRaisingEvents = false;
            
            cancellationTokenSource.Cancel();
            workerTask?.Wait(5000);

            sourceWatcher.Dispose();
            testWatcher.Dispose();

            Console.WriteLine("✅ Continuous testing stopped.");
        }

        private void OnSourceFileChanged(object sender, FileSystemEventArgs e)
        {
            if (ShouldIgnoreFile(e.FullPath)) return;

            FileChanged?.Invoke(this, e.FullPath);
            
            lock (lockObject)
            {
                // Debounce file changes
                lastRunTimes[e.FullPath] = DateTime.Now;
            }

            Task.Delay(Options.DebounceDelay).ContinueWith(_ =>
            {
                lock (lockObject)
                {
                    if (lastRunTimes.TryGetValue(e.FullPath, out var lastTime) &&
                        (DateTime.Now - lastTime).TotalMilliseconds >= Options.DebounceDelay)
                    {
                        var affectedTests = FindAffectedTests(e.FullPath);
                        
                        if (affectedTests.Any())
                        {
                            QueueTestRun(new TestRunRequest
                            {
                                Type = TestRunType.Affected,
                                AffectedFiles = new[] { e.FullPath },
                                TestsToRun = affectedTests,
                                Reason = $"Source file changed: {Path.GetFileName(e.FullPath)}"
                            });
                        }
                        else if (Options.RunAllTestsOnUnmappedChange)
                        {
                            QueueTestRun(new TestRunRequest
                            {
                                Type = TestRunType.Full,
                                Reason = $"Source file changed (no test mapping): {Path.GetFileName(e.FullPath)}"
                            });
                        }

                        lastRunTimes.Remove(e.FullPath);
                    }
                }
            });
        }

        private void OnTestFileChanged(object sender, FileSystemEventArgs e)
        {
            if (ShouldIgnoreFile(e.FullPath)) return;

            FileChanged?.Invoke(this, e.FullPath);

            QueueTestRun(new TestRunRequest
            {
                Type = TestRunType.Single,
                TestsToRun = new[] { e.FullPath },
                Reason = $"Test file changed: {Path.GetFileName(e.FullPath)}"
            });
        }

        private void OnSourceFileRenamed(object sender, RenamedEventArgs e)
        {
            OnSourceFileChanged(sender, e);
        }

        private void OnTestFileRenamed(object sender, RenamedEventArgs e)
        {
            OnTestFileChanged(sender, e);
        }

        private bool ShouldIgnoreFile(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            
            // Ignore temporary files
            if (fileName.StartsWith(".") || fileName.StartsWith("~") || fileName.EndsWith(".tmp"))
                return true;

            // Ignore generated files
            if (filePath.Contains("\\obj\\") || filePath.Contains("\\bin\\"))
                return true;

            // Check custom ignore patterns
            return Options.IgnorePatterns.Any(pattern => filePath.Contains(pattern));
        }

        private List<string> FindAffectedTests(string sourceFile)
        {
            var affectedTests = new List<string>();
            
            // Simple heuristic: find tests with similar names
            var sourceFileName = Path.GetFileNameWithoutExtension(sourceFile);
            var possibleTestNames = new[]
            {
                $"{sourceFileName}Tests.cs",
                $"{sourceFileName}Test.cs",
                $"{sourceFileName}Spec.cs",
                $"{sourceFileName}.Tests.cs",
                $"{sourceFileName}.Test.cs"
            };

            foreach (var testName in possibleTestNames)
            {
                var testPath = Path.Combine(testWatcher.Path, testName);
                if (File.Exists(testPath))
                {
                    affectedTests.Add(testPath);
                }
            }

            // Check for test files that reference this source file
            if (Options.UseAdvancedMapping)
            {
                var referencingTests = FindTestsReferencingSource(sourceFile);
                affectedTests.AddRange(referencingTests);
            }

            return affectedTests.Distinct().ToList();
        }

        private List<string> FindTestsReferencingSource(string sourceFile)
        {
            var className = Path.GetFileNameWithoutExtension(sourceFile);
            var referencingTests = new List<string>();

            // Search for test files that might reference this class
            var testFiles = Directory.GetFiles(testWatcher.Path, "*.cs", SearchOption.AllDirectories);
            
            Parallel.ForEach(testFiles, testFile =>
            {
                try
                {
                    var content = File.ReadAllText(testFile);
                    if (content.Contains(className))
                    {
                        lock (referencingTests)
                        {
                            referencingTests.Add(testFile);
                        }
                    }
                }
                catch { /* Ignore file access errors */ }
            });

            return referencingTests;
        }

        private void QueueTestRun(TestRunRequest request)
        {
            lock (lockObject)
            {
                // Check if similar request already queued
                if (!testQueue.Any(r => r.Type == request.Type && 
                    r.TestsToRun?.SequenceEqual(request.TestsToRun ?? Array.Empty<string>()) == true))
                {
                    testQueue.Enqueue(request);
                    Monitor.Pulse(lockObject);
                }
            }
        }

        private async Task ProcessTestQueue()
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                TestRunRequest request = null;

                lock (lockObject)
                {
                    while (testQueue.Count == 0 && !cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        Monitor.Wait(lockObject, 1000);
                    }

                    if (testQueue.Count > 0)
                    {
                        request = testQueue.Dequeue();
                    }
                }

                if (request != null)
                {
                    await RunTests(request);
                }
            }
        }

        private async Task RunTests(TestRunRequest request)
        {
            var result = new TestRunResult
            {
                Request = request,
                StartTime = DateTime.Now
            };

            try
            {
                Console.WriteLine($"\n🧪 Running tests: {request.Reason}");
                Console.WriteLine($"   Type: {request.Type}");
                
                if (request.TestsToRun?.Any() == true)
                {
                    Console.WriteLine($"   Tests: {string.Join(", ", request.TestsToRun.Select(Path.GetFileName))}");
                }

                var stopwatch = Stopwatch.StartNew();

                // Build the test project
                if (Options.BuildBeforeTest)
                {
                    var buildResult = await BuildTestProject();
                    if (!buildResult.Success)
                    {
                        result.Success = false;
                        result.ErrorMessage = "Build failed";
                        result.BuildOutput = buildResult.Output;
                        TestRunCompleted?.Invoke(this, result);
                        return;
                    }
                }

                // Run tests based on request type
                var testResult = request.Type switch
                {
                    TestRunType.Full => await RunAllTests(),
                    TestRunType.Affected => await RunSpecificTests(request.TestsToRun),
                    TestRunType.Single => await RunSpecificTests(request.TestsToRun),
                    _ => throw new ArgumentException($"Unknown test run type: {request.Type}")
                };

                stopwatch.Stop();

                result.Success = testResult.Success;
                result.TotalTests = testResult.TotalTests;
                result.PassedTests = testResult.PassedTests;
                result.FailedTests = testResult.FailedTests;
                result.SkippedTests = testResult.SkippedTests;
                result.Duration = stopwatch.Elapsed;
                result.TestOutput = testResult.Output;

                // Update test history
                UpdateTestHistory(result);

                // Print summary
                PrintTestSummary(result);

                TestRunCompleted?.Invoke(this, result);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                TestRunCompleted?.Invoke(this, result);
            }
        }

        private async Task<BuildResult> BuildTestProject()
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "build --no-restore",
                    WorkingDirectory = testWatcher.Path,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            var output = new List<string>();
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    output.Add(e.Data);
            };
            
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    output.Add(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            await process.WaitForExitAsync();

            return new BuildResult
            {
                Success = process.ExitCode == 0,
                Output = output
            };
        }

        private async Task<TestExecutionResult> RunAllTests()
        {
            var assembly = Assembly.LoadFrom(Path.Combine(testWatcher.Path, "bin", "Debug", "net6.0", "Ouroboros.Tests.dll"));
            
            // Capture output
            var output = new List<string>();
            var originalOut = Console.Out;
            var originalErr = Console.Error;
            
            try
            {
                using (var outputCapture = new StringWriter())
                using (var errorCapture = new StringWriter())
                {
                    Console.SetOut(outputCapture);
                    Console.SetError(errorCapture);
                    
                    var exitCode = await TestFramework.RunAllTests(assembly);
                    
                    output.AddRange(outputCapture.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries));
                    output.AddRange(errorCapture.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries));
                    
                    return new TestExecutionResult
                    {
                        Success = exitCode == 0,
                        TotalTests = TestFramework.TotalTests,
                        PassedTests = TestFramework.PassedTests,
                        FailedTests = TestFramework.FailedTests,
                        SkippedTests = TestFramework.SkippedTests,
                        Output = output
                    };
                }
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
            }
        }

        private async Task<TestExecutionResult> RunSpecificTests(string[] testFiles)
        {
            // Filter test classes based on file names
            var testClassNames = testFiles
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();
            
            var assembly = Assembly.LoadFrom(Path.Combine(testWatcher.Path, "bin", "Debug", "net6.0", "Ouroboros.Tests.dll"));
            
            // Capture output
            var output = new List<string>();
            var originalOut = Console.Out;
            var originalErr = Console.Error;
            
            try
            {
                using (var outputCapture = new StringWriter())
                using (var errorCapture = new StringWriter())
                {
                    Console.SetOut(outputCapture);
                    Console.SetError(errorCapture);
                    
                    // Run only tests from specified classes
                    var exitCode = await TestFramework.RunTestsByFilter(assembly, 
                        type => testClassNames.Any(name => type.Name.Contains(name)));
                    
                    output.AddRange(outputCapture.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries));
                    output.AddRange(errorCapture.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries));
                    
                    return new TestExecutionResult
                    {
                        Success = exitCode == 0,
                        TotalTests = TestFramework.TotalTests,
                        PassedTests = TestFramework.PassedTests,
                        FailedTests = TestFramework.FailedTests,
                        SkippedTests = TestFramework.SkippedTests,
                        Output = output
                    };
                }
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
            }
        }

        private void UpdateTestHistory(TestRunResult result)
        {
            // Store test history in a JSON file for trend analysis
            var historyPath = Path.Combine(testWatcher.Path, ".test-history.json");
            var history = new List<TestHistoryEntry>();
            
            // Load existing history
            if (File.Exists(historyPath))
            {
                try
                {
                    var json = File.ReadAllText(historyPath);
                    history = System.Text.Json.JsonSerializer.Deserialize<List<TestHistoryEntry>>(json) ?? new List<TestHistoryEntry>();
                }
                catch { /* Ignore corrupted history */ }
            }
            
            // Add new entry
            history.Add(new TestHistoryEntry
            {
                Timestamp = result.StartTime,
                Duration = result.Duration,
                Success = result.Success,
                TotalTests = result.TotalTests,
                PassedTests = result.PassedTests,
                FailedTests = result.FailedTests,
                SkippedTests = result.SkippedTests,
                TestType = result.Request.Type.ToString(),
                Reason = result.Request.Reason
            });
            
            // Keep only last 100 entries
            if (history.Count > 100)
            {
                history = history.Skip(history.Count - 100).ToList();
            }
            
            // Save updated history
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(history, new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(historyPath, json);
            }
            catch { /* Ignore save errors */ }
        }

        private void PrintTestSummary(TestRunResult result)
        {
            var icon = result.Success ? "✅" : "❌";
            var status = result.Success ? "PASSED" : "FAILED";
            
            Console.WriteLine($"\n{icon} Test run {status}");
            Console.WriteLine($"   Duration: {result.Duration.TotalSeconds:F2}s");
            Console.WriteLine($"   Total: {result.TotalTests}");
            Console.WriteLine($"   Passed: {result.PassedTests}");
            Console.WriteLine($"   Failed: {result.FailedTests}");
            Console.WriteLine($"   Skipped: {result.SkippedTests}");

            if (!result.Success && result.TestOutput?.Any() == true)
            {
                Console.WriteLine("\n❌ Failed test output:");
                foreach (var line in result.TestOutput.Take(10))
                {
                    Console.WriteLine($"   {line}");
                }
                if (result.TestOutput.Count > 10)
                {
                    Console.WriteLine($"   ... and {result.TestOutput.Count - 10} more lines");
                }
            }

            // Show notification if enabled
            if (Options.ShowNotifications)
            {
                ShowNotification(result);
            }
        }

        private void ShowNotification(TestRunResult result)
        {
            // Platform-specific notification
            if (OperatingSystem.IsWindows())
            {
                // Windows 10 toast notification
                var title = result.Success ? "Tests Passed ✅" : "Tests Failed ❌";
                var message = $"{result.PassedTests}/{result.TotalTests} tests passed in {result.Duration.TotalSeconds:F1}s";
                
                // Could use Windows.UI.Notifications or similar
                Console.Beep(result.Success ? 800 : 400, 200);
            }
            else if (OperatingSystem.IsMacOS())
            {
                // macOS notification using osascript
                var title = result.Success ? "Tests Passed" : "Tests Failed";
                var message = $"{result.PassedTests}/{result.TotalTests} tests passed";
                Process.Start("osascript", $"-e 'display notification \"{message}\" with title \"{title}\"'");
            }
            else if (OperatingSystem.IsLinux())
            {
                // Linux notification using notify-send
                var title = result.Success ? "Tests Passed" : "Tests Failed";
                var message = $"{result.PassedTests}/{result.TotalTests} tests passed";
                Process.Start("notify-send", $"\"{title}\" \"{message}\"");
            }
        }
    }

    public class ContinuousTestingOptions
    {
        public int DebounceDelay { get; set; } = 500; // ms
        public bool BuildBeforeTest { get; set; } = true;
        public bool ShowNotifications { get; set; } = true;
        public bool RunAllTestsOnUnmappedChange { get; set; } = false;
        public bool UseAdvancedMapping { get; set; } = true;
        public List<string> IgnorePatterns { get; set; } = new()
        {
            ".git", ".vs", "packages", "TestResults"
        };
    }

    public class TestRunRequest
    {
        public TestRunType Type { get; set; }
        public string[] AffectedFiles { get; set; }
        public string[] TestsToRun { get; set; }
        public string Reason { get; set; }
    }

    public class TestRunResult
    {
        public TestRunRequest Request { get; set; }
        public bool Success { get; set; }
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public int SkippedTests { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> BuildOutput { get; set; }
        public List<string> TestOutput { get; set; }
    }

    public class BuildResult
    {
        public bool Success { get; set; }
        public List<string> Output { get; set; }
    }

    public class TestExecutionResult
    {
        public bool Success { get; set; }
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public int SkippedTests { get; set; }
        public List<string> Output { get; set; }
    }

    public enum TestRunType
    {
        Full,      // Run all tests
        Affected,  // Run tests affected by source changes
        Single     // Run a single test file
    }

    public class TestHistoryEntry
    {
        public DateTime Timestamp { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public int SkippedTests { get; set; }
        public string TestType { get; set; }
        public string Reason { get; set; }
    }
} 
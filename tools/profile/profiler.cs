using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Ouroboros.Core;
using Ouroboros.Core.VM;

namespace Ouroboros.Tools.Profile
{
    /// <summary>
    /// Performance profiler for Ouroboros programs
    /// </summary>
    public class Profiler
    {
        private VirtualMachine vm;
        private Dictionary<string, FunctionProfile> functionProfiles;
        private Dictionary<int, LineProfile> lineProfiles;
        private Stack<CallInfo> callStack;
        private Stopwatch totalTimer;
        private ProfileOptions options;
        private long totalInstructions;
        private long totalMemoryAllocated;
        private long peakMemoryUsage;
        private Dictionary<string, string[]> sourceFileCache = new Dictionary<string, string[]>();
        private string currentSourceFile;
        
        public Profiler(VirtualMachine virtualMachine, ProfileOptions options = null)
        {
            vm = virtualMachine;
            functionProfiles = new Dictionary<string, FunctionProfile>();
            lineProfiles = new Dictionary<int, LineProfile>();
            callStack = new Stack<CallInfo>();
            totalTimer = new Stopwatch();
            this.options = options ?? new ProfileOptions();
            totalInstructions = 0;
            totalMemoryAllocated = 0;
            peakMemoryUsage = 0;
        }
        
        /// <summary>
        /// Start profiling a program
        /// </summary>
        public void StartProfiling(string programFile)
        {
            Console.WriteLine("Ouroboros Profiler v1.0");
            Console.WriteLine($"Profiling: {programFile}");
            Console.WriteLine();
            
            // Store the source file path
            currentSourceFile = programFile;
            
            // Set up VM hooks
            SetupVMHooks();
            
            // Start timing
            totalTimer.Start();
            
            try
            {
                // Run the program
                vm.Execute(programFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during execution: {ex.Message}");
            }
            finally
            {
                totalTimer.Stop();
                
                // Generate report
                GenerateReport();
            }
        }
        
        private void SetupVMHooks()
        {
            // Hook into VM events
            vm.OnFunctionEnter += OnFunctionEnter;
            vm.OnFunctionExit += OnFunctionExit;
            vm.OnInstructionExecute += OnInstructionExecute;
            vm.OnMemoryAllocate += OnMemoryAllocate;
            vm.OnMemoryFree += OnMemoryFree;
        }
        
        private void OnFunctionEnter(string functionName, int line)
        {
            var callInfo = new CallInfo
            {
                FunctionName = functionName,
                StartTime = totalTimer.Elapsed,
                StartMemory = GC.GetTotalMemory(false),
                Line = line
            };
            
            callStack.Push(callInfo);
            
            if (!functionProfiles.ContainsKey(functionName))
            {
                functionProfiles[functionName] = new FunctionProfile
                {
                    Name = functionName,
                    CallCount = 0,
                    TotalTime = TimeSpan.Zero,
                    SelfTime = TimeSpan.Zero,
                    ChildTime = TimeSpan.Zero,
                    TotalMemory = 0,
                    Children = new Dictionary<string, int>()
                };
            }
            
            functionProfiles[functionName].CallCount++;
        }
        
        private void OnFunctionExit(string functionName)
        {
            if (callStack.Count == 0)
                return;
            
            var callInfo = callStack.Pop();
            var elapsed = totalTimer.Elapsed - callInfo.StartTime;
            var memoryUsed = GC.GetTotalMemory(false) - callInfo.StartMemory;
            
            var profile = functionProfiles[functionName];
            profile.TotalTime += elapsed;
            profile.TotalMemory += memoryUsed;
            
            // Update parent's child time
            if (callStack.Count > 0)
            {
                var parent = callStack.Peek();
                var parentProfile = functionProfiles[parent.FunctionName];
                parentProfile.ChildTime += elapsed;
                
                // Track parent-child relationships
                if (!parentProfile.Children.ContainsKey(functionName))
                    parentProfile.Children[functionName] = 0;
                parentProfile.Children[functionName]++;
            }
            
            // Calculate self time
            profile.SelfTime = profile.TotalTime - profile.ChildTime;
        }
        
        private void OnInstructionExecute(int line, Opcode opcode)
        {
            totalInstructions++;
            
            if (options.ProfileLines)
            {
                if (!lineProfiles.ContainsKey(line))
                {
                    lineProfiles[line] = new LineProfile
                    {
                        Line = line,
                        HitCount = 0,
                        TotalTime = TimeSpan.Zero
                    };
                }
                
                lineProfiles[line].HitCount++;
            }
        }
        
        private void OnMemoryAllocate(long size)
        {
            totalMemoryAllocated += size;
            
            long currentUsage = GC.GetTotalMemory(false);
            if (currentUsage > peakMemoryUsage)
                peakMemoryUsage = currentUsage;
        }
        
        private void OnMemoryFree(long size)
        {
            // Track memory deallocation
            if (callStack.Count > 0)
            {
                var currentFunction = callStack.Peek().FunctionName;
                if (functionProfiles.ContainsKey(currentFunction))
                {
                    // Negative memory to indicate deallocation
                    functionProfiles[currentFunction].TotalMemory -= size;
                }
            }
            
            // Update current memory usage tracking
            long currentUsage = GC.GetTotalMemory(false);
            Console.WriteLine($"Memory freed: {FormatBytes(size)}, current usage: {FormatBytes(currentUsage)}");
        }
        
        private void GenerateReport()
        {
            var report = new StringBuilder();
            
            report.AppendLine("\n" + new string('=', 80));
            report.AppendLine("PROFILING REPORT");
            report.AppendLine(new string('=', 80));
            
            // Summary
            GenerateSummary(report);
            
            // Function profiles
            if (options.ShowFunctions)
                GenerateFunctionReport(report);
            
            // Line profiles
            if (options.ProfileLines && options.ShowLines)
                GenerateLineReport(report);
            
            // Call graph
            if (options.ShowCallGraph)
                GenerateCallGraph(report);
            
            // Memory report
            if (options.ShowMemory)
                GenerateMemoryReport(report);
            
            // Hot spots
            if (options.ShowHotSpots)
                GenerateHotSpots(report);
            
            // Output report
            Console.WriteLine(report.ToString());
            
            // Save to file if requested
            if (!string.IsNullOrEmpty(options.OutputFile))
            {
                File.WriteAllText(options.OutputFile, report.ToString());
                Console.WriteLine($"\nReport saved to: {options.OutputFile}");
            }
        }
        
        private void GenerateSummary(StringBuilder report)
        {
            report.AppendLine("\nEXECUTION SUMMARY");
            report.AppendLine(new string('-', 40));
            report.AppendLine($"Total execution time: {totalTimer.Elapsed.TotalMilliseconds:F2} ms");
            report.AppendLine($"Total instructions: {totalInstructions:N0}");
            report.AppendLine($"Instructions/second: {totalInstructions / totalTimer.Elapsed.TotalSeconds:N0}");
            report.AppendLine($"Total memory allocated: {FormatBytes(totalMemoryAllocated)}");
            report.AppendLine($"Peak memory usage: {FormatBytes(peakMemoryUsage)}");
            report.AppendLine($"Total functions called: {functionProfiles.Values.Sum(f => f.CallCount)}");
            report.AppendLine($"Unique functions: {functionProfiles.Count}");
        }
        
        private void GenerateFunctionReport(StringBuilder report)
        {
            report.AppendLine("\n\nFUNCTION PROFILES");
            report.AppendLine(new string('-', 80));
            report.AppendLine($"{"Function",-30} {"Calls",8} {"Total ms",10} {"Self ms",10} {"Avg ms",10} {"Memory",10}");
            report.AppendLine(new string('-', 80));
            
            var sortedFunctions = functionProfiles.Values
                .OrderByDescending(f => f.TotalTime)
                .Take(options.TopFunctions);
            
            foreach (var func in sortedFunctions)
            {
                double totalMs = func.TotalTime.TotalMilliseconds;
                double selfMs = func.SelfTime.TotalMilliseconds;
                double avgMs = totalMs / func.CallCount;
                
                report.AppendLine($"{func.Name,-30} {func.CallCount,8} {totalMs,10:F2} {selfMs,10:F2} {avgMs,10:F2} {FormatBytes(func.TotalMemory),10}");
            }
        }
        
        private void GenerateLineReport(StringBuilder report)
        {
            report.AppendLine("\n\nLINE PROFILES (Top 20)");
            report.AppendLine(new string('-', 50));
            report.AppendLine($"{"Line",8} {"Hits",10} {"% Time",10} {"Source",30}");
            report.AppendLine(new string('-', 50));
            
            var sortedLines = lineProfiles.Values
                .OrderByDescending(l => l.HitCount)
                .Take(20);
            
            foreach (var line in sortedLines)
            {
                double percentage = (line.HitCount * 100.0) / totalInstructions;
                string source = GetSourceLine(line.Line);
                
                report.AppendLine($"{line.Line,8} {line.HitCount,10} {percentage,10:F2} {source,30}");
            }
        }
        
        private void GenerateCallGraph(StringBuilder report)
        {
            report.AppendLine("\n\nCALL GRAPH");
            report.AppendLine(new string('-', 60));
            
            // Show top-level functions
            var topLevel = functionProfiles.Values
                .Where(f => !IsCalledByOthers(f.Name))
                .OrderByDescending(f => f.TotalTime);
            
            foreach (var func in topLevel)
            {
                GenerateCallGraphNode(report, func, 0, new HashSet<string>());
            }
        }
        
        private void GenerateCallGraphNode(StringBuilder report, FunctionProfile func, int depth, HashSet<string> visited)
        {
            if (visited.Contains(func.Name))
            {
                report.AppendLine($"{new string(' ', depth * 2)}[recursive: {func.Name}]");
                return;
            }
            
            visited.Add(func.Name);
            
            string indent = new string(' ', depth * 2);
            double totalMs = func.TotalTime.TotalMilliseconds;
            double selfMs = func.SelfTime.TotalMilliseconds;
            
            report.AppendLine($"{indent}{func.Name} - {func.CallCount} calls, {totalMs:F2}ms total, {selfMs:F2}ms self");
            
            // Show children
            var sortedChildren = func.Children
                .OrderByDescending(c => functionProfiles[c.Key].TotalTime)
                .Take(5);
            
            foreach (var child in sortedChildren)
            {
                if (functionProfiles.ContainsKey(child.Key))
                {
                    GenerateCallGraphNode(report, functionProfiles[child.Key], depth + 1, visited);
                }
            }
            
            visited.Remove(func.Name);
        }
        
        private void GenerateMemoryReport(StringBuilder report)
        {
            report.AppendLine("\n\nMEMORY ANALYSIS");
            report.AppendLine(new string('-', 60));
            
            var memoryHogs = functionProfiles.Values
                .OrderByDescending(f => f.TotalMemory)
                .Take(10);
            
            report.AppendLine($"{"Function",-30} {"Memory Allocated",20} {"Calls",10}");
            report.AppendLine(new string('-', 60));
            
            foreach (var func in memoryHogs)
            {
                report.AppendLine($"{func.Name,-30} {FormatBytes(func.TotalMemory),20} {func.CallCount,10}");
            }
            
            // Memory allocation timeline
            if (options.ShowMemoryTimeline)
            {
                report.AppendLine("\n\nMEMORY TIMELINE");
                report.AppendLine(new string('-', 40));
                // This would show memory usage over time
            }
        }
        
        private void GenerateHotSpots(StringBuilder report)
        {
            report.AppendLine("\n\nPERFORMANCE HOT SPOTS");
            report.AppendLine(new string('-', 70));
            
            // Find functions that take most time
            var hotFunctions = functionProfiles.Values
                .OrderByDescending(f => f.SelfTime)
                .Take(10);
            
            report.AppendLine("\nTop 10 Time-Consuming Functions:");
            foreach (var func in hotFunctions)
            {
                double percentage = (func.SelfTime.TotalMilliseconds / totalTimer.Elapsed.TotalMilliseconds) * 100;
                report.AppendLine($"  {func.Name,-30} {percentage,6:F2}% ({func.SelfTime.TotalMilliseconds:F2}ms)");
            }
            
            // Find most called functions
            var mostCalled = functionProfiles.Values
                .OrderByDescending(f => f.CallCount)
                .Take(10);
            
            report.AppendLine("\nMost Frequently Called Functions:");
            foreach (var func in mostCalled)
            {
                report.AppendLine($"  {func.Name,-30} {func.CallCount,10} calls");
            }
            
            // Performance recommendations
            report.AppendLine("\n\nRECOMMENDATIONS:");
            GenerateRecommendations(report, hotFunctions.ToList());
        }
        
        private void GenerateRecommendations(StringBuilder report, List<FunctionProfile> hotFunctions)
        {
            foreach (var func in hotFunctions.Take(3))
            {
                report.AppendLine($"\n• Function '{func.Name}':");
                
                // High call count
                if (func.CallCount > 10000)
                {
                    report.AppendLine($"  - Called {func.CallCount:N0} times. Consider caching results or optimizing the algorithm.");
                }
                
                // High average time
                double avgMs = func.TotalTime.TotalMilliseconds / func.CallCount;
                if (avgMs > 10)
                {
                    report.AppendLine($"  - Average execution time is {avgMs:F2}ms. Look for optimization opportunities.");
                }
                
                // High memory usage
                if (func.TotalMemory > 1_000_000)
                {
                    report.AppendLine($"  - Allocates {FormatBytes(func.TotalMemory)}. Consider reusing objects or using object pools.");
                }
                
                // Many children
                if (func.Children.Count > 20)
                {
                    report.AppendLine($"  - Calls {func.Children.Count} different functions. Consider refactoring for better cohesion.");
                }
            }
        }
        
        private bool IsCalledByOthers(string functionName)
        {
            return functionProfiles.Values.Any(f => f.Children.ContainsKey(functionName));
        }
        
        private string GetSourceLine(int line)
        {
            // Read the actual source line
            try
            {
                // Cache source files to avoid repeated file reads
                if (!sourceFileCache.TryGetValue(currentSourceFile, out string[] lines))
                {
                    if (File.Exists(currentSourceFile))
                    {
                        lines = File.ReadAllLines(currentSourceFile);
                        sourceFileCache[currentSourceFile] = lines;
                    }
                    else
                    {
                        return $"<source unavailable>";
                    }
                }
                
                // Line numbers are 1-based, array is 0-based
                if (line > 0 && line <= lines.Length)
                {
                    string sourceLine = lines[line - 1].Trim();
                    // Truncate long lines
                    if (sourceLine.Length > 30)
                    {
                        sourceLine = sourceLine.Substring(0, 27) + "...";
                    }
                    return sourceLine;
                }
                
                return $"<line {line} out of range>";
            }
            catch (Exception ex)
            {
                return $"<error: {ex.Message}>";
            }
        }
        
        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int suffixIndex = 0;
            double size = bytes;
            
            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }
            
            return $"{size:F2} {suffixes[suffixIndex]}";
        }
        
        private class CallInfo
        {
            public string FunctionName { get; set; }
            public TimeSpan StartTime { get; set; }
            public long StartMemory { get; set; }
            public int Line { get; set; }
        }
        
        private class FunctionProfile
        {
            public string Name { get; set; }
            public int CallCount { get; set; }
            public TimeSpan TotalTime { get; set; }
            public TimeSpan SelfTime { get; set; }
            public TimeSpan ChildTime { get; set; }
            public long TotalMemory { get; set; }
            public Dictionary<string, int> Children { get; set; }
        }
        
        private class LineProfile
        {
            public int Line { get; set; }
            public int HitCount { get; set; }
            public TimeSpan TotalTime { get; set; }
        }
    }
    
    public class ProfileOptions
    {
        public bool ShowFunctions { get; set; } = true;
        public bool ShowLines { get; set; } = true;
        public bool ShowCallGraph { get; set; } = true;
        public bool ShowMemory { get; set; } = true;
        public bool ShowHotSpots { get; set; } = true;
        public bool ProfileLines { get; set; } = true;
        public bool ShowMemoryTimeline { get; set; } = false;
        public int TopFunctions { get; set; } = 20;
        public string OutputFile { get; set; }
    }
    
    public class ProfilerProgram
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                ShowUsage();
                return;
            }
            
            var options = ParseOptions(args);
            var vm = new VirtualMachine();
            var profiler = new Profiler(vm, options);
            
            profiler.StartProfiling(args[args.Length - 1]);
        }
        
        private static void ShowUsage()
        {
            Console.WriteLine(@"
Ouroboros Profiler Usage:
  ouro-profile [options] <program-file>

Options:
  -o, --output <file>      Save report to file
  -f, --functions          Show function profiles (default: on)
  -l, --lines              Show line profiles (default: on)
  -g, --call-graph         Show call graph (default: on)
  -m, --memory             Show memory analysis (default: on)
  -h, --hot-spots          Show performance hot spots (default: on)
  --top <n>                Show top N functions (default: 20)
  --no-lines               Disable line profiling
  --memory-timeline        Show memory usage over time

Examples:
  ouro-profile program.ouro
  ouro-profile -o report.txt program.ouro
  ouro-profile --top 50 --memory-timeline program.ouro
");
        }
        
        private static ProfileOptions ParseOptions(string[] args)
        {
            var options = new ProfileOptions();
            
            for (int i = 0; i < args.Length - 1; i++)
            {
                switch (args[i])
                {
                    case "-o":
                    case "--output":
                        options.OutputFile = args[++i];
                        break;
                    
                    case "-f":
                    case "--functions":
                        options.ShowFunctions = true;
                        break;
                    
                    case "-l":
                    case "--lines":
                        options.ShowLines = true;
                        break;
                    
                    case "-g":
                    case "--call-graph":
                        options.ShowCallGraph = true;
                        break;
                    
                    case "-m":
                    case "--memory":
                        options.ShowMemory = true;
                        break;
                    
                    case "-h":
                    case "--hot-spots":
                        options.ShowHotSpots = true;
                        break;
                    
                    case "--top":
                        options.TopFunctions = int.Parse(args[++i]);
                        break;
                    
                    case "--no-lines":
                        options.ProfileLines = false;
                        options.ShowLines = false;
                        break;
                    
                    case "--memory-timeline":
                        options.ShowMemoryTimeline = true;
                        break;
                }
            }
            
            return options;
        }
    }
} 
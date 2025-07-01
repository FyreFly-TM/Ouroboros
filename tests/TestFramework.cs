using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Ouroboros.Testing
{
    /// <summary>
    /// Lightweight testing framework for Ouroboros
    /// </summary>
    public static class TestFramework
    {
        private static readonly List<TestResult> Results = new();
        private static readonly Stopwatch TotalTimer = new();
        private static int TotalTests = 0;
        private static int PassedTests = 0;
        private static int FailedTests = 0;
        private static int SkippedTests = 0;

        public static async Task<int> RunAllTests(Assembly assembly = null)
        {
            assembly ??= Assembly.GetExecutingAssembly();
            TotalTimer.Start();
            
            Console.WriteLine($"🧪 Ouroboros Test Runner v1.0");
            Console.WriteLine($"📦 Assembly: {assembly.GetName().Name}");
            Console.WriteLine($"🔍 Discovering tests...\n");

            var testClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttributes<TestClassAttribute>().Any())
                .OrderBy(t => t.Name);

            foreach (var testClass in testClasses)
            {
                await RunTestClass(testClass);
            }

            TotalTimer.Stop();
            PrintSummary();

            return FailedTests > 0 ? 1 : 0;
        }

        private static async Task RunTestClass(Type testClass)
        {
            Console.WriteLine($"\n📋 {testClass.Name}");
            var instance = Activator.CreateInstance(testClass);

            // Run setup method if exists
            var setupMethod = testClass.GetMethod("Setup");
            setupMethod?.Invoke(instance, null);

            var testMethods = testClass.GetMethods()
                .Where(m => m.GetCustomAttributes<TestAttribute>().Any())
                .OrderBy(m => m.Name);

            foreach (var method in testMethods)
            {
                await RunTestMethod(instance, method);
            }

            // Run teardown method if exists
            var teardownMethod = testClass.GetMethod("Teardown");
            teardownMethod?.Invoke(instance, null);
        }

        private static async Task RunTestMethod(object instance, MethodInfo method)
        {
            var testName = method.GetCustomAttribute<TestAttribute>()?.Description ?? method.Name;
            var timer = Stopwatch.StartNew();
            TotalTests++;

            try
            {
                // Check if method is async
                if (method.ReturnType == typeof(Task))
                {
                    await (Task)method.Invoke(instance, null);
                }
                else
                {
                    method.Invoke(instance, null);
                }

                timer.Stop();
                PassedTests++;
                Results.Add(new TestResult
                {
                    TestName = testName,
                    ClassName = instance.GetType().Name,
                    Passed = true,
                    Duration = timer.Elapsed
                });
                Console.WriteLine($"  ✅ {testName} ({timer.ElapsedMilliseconds}ms)");
            }
            catch (Exception ex)
            {
                timer.Stop();
                FailedTests++;
                var actualException = ex.InnerException ?? ex;
                Results.Add(new TestResult
                {
                    TestName = testName,
                    ClassName = instance.GetType().Name,
                    Passed = false,
                    Duration = timer.Elapsed,
                    ErrorMessage = actualException.Message,
                    StackTrace = actualException.StackTrace
                });
                Console.WriteLine($"  ❌ {testName} ({timer.ElapsedMilliseconds}ms)");
                Console.WriteLine($"     {actualException.GetType().Name}: {actualException.Message}");
            }
        }

        private static void PrintSummary()
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("TEST SUMMARY");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine($"Total:   {TotalTests}");
            Console.WriteLine($"Passed:  {PassedTests} ✅");
            Console.WriteLine($"Failed:  {FailedTests} ❌");
            Console.WriteLine($"Skipped: {SkippedTests} ⏭️");
            Console.WriteLine($"Time:    {TotalTimer.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine(new string('=', 60));

            if (FailedTests > 0)
            {
                Console.WriteLine("\nFAILED TESTS:");
                foreach (var failed in Results.Where(r => !r.Passed))
                {
                    Console.WriteLine($"\n❌ {failed.ClassName}.{failed.TestName}");
                    Console.WriteLine($"   {failed.ErrorMessage}");
                    if (!string.IsNullOrEmpty(failed.StackTrace))
                    {
                        var lines = failed.StackTrace.Split('\n').Take(3);
                        foreach (var line in lines)
                        {
                            Console.WriteLine($"   {line.Trim()}");
                        }
                    }
                }
            }
        }

        private class TestResult
        {
            public string TestName { get; set; }
            public string ClassName { get; set; }
            public bool Passed { get; set; }
            public TimeSpan Duration { get; set; }
            public string ErrorMessage { get; set; }
            public string StackTrace { get; set; }
        }
    }

    /// <summary>
    /// Assertion helper for tests
    /// </summary>
    public static class Assert
    {
        public static void AreEqual<T>(T expected, T actual, string message = null)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new AssertionException(
                    message ?? $"Expected: {expected}, Actual: {actual}");
            }
        }

        public static void AreNotEqual<T>(T expected, T actual, string message = null)
        {
            if (EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new AssertionException(
                    message ?? $"Expected not equal to: {expected}");
            }
        }

        public static void IsTrue(bool condition, string message = null)
        {
            if (!condition)
            {
                throw new AssertionException(message ?? "Expected: True, Actual: False");
            }
        }

        public static void IsFalse(bool condition, string message = null)
        {
            if (condition)
            {
                throw new AssertionException(message ?? "Expected: False, Actual: True");
            }
        }

        public static void IsNull(object obj, string message = null)
        {
            if (obj != null)
            {
                throw new AssertionException(message ?? $"Expected: null, Actual: {obj}");
            }
        }

        public static void IsNotNull(object obj, string message = null)
        {
            if (obj == null)
            {
                throw new AssertionException(message ?? "Expected: not null, Actual: null");
            }
        }

        public static void Throws<TException>(Action action, string message = null) 
            where TException : Exception
        {
            try
            {
                action();
                throw new AssertionException(
                    message ?? $"Expected exception of type {typeof(TException).Name} was not thrown");
            }
            catch (TException)
            {
                // Expected
            }
            catch (Exception ex)
            {
                throw new AssertionException(
                    $"Expected exception of type {typeof(TException).Name}, but got {ex.GetType().Name}");
            }
        }

        public static async Task ThrowsAsync<TException>(Func<Task> action, string message = null)
            where TException : Exception
        {
            try
            {
                await action();
                throw new AssertionException(
                    message ?? $"Expected exception of type {typeof(TException).Name} was not thrown");
            }
            catch (TException)
            {
                // Expected
            }
            catch (Exception ex)
            {
                throw new AssertionException(
                    $"Expected exception of type {typeof(TException).Name}, but got {ex.GetType().Name}");
            }
        }

        public static void Contains<T>(IEnumerable<T> collection, T item, string message = null)
        {
            if (!collection.Contains(item))
            {
                throw new AssertionException(message ?? $"Collection does not contain item: {item}");
            }
        }

        public static void DoesNotContain<T>(IEnumerable<T> collection, T item, string message = null)
        {
            if (collection.Contains(item))
            {
                throw new AssertionException(message ?? $"Collection contains item: {item}");
            }
        }

        public static void IsEmpty<T>(IEnumerable<T> collection, string message = null)
        {
            if (collection.Any())
            {
                throw new AssertionException(message ?? "Collection is not empty");
            }
        }

        public static void IsNotEmpty<T>(IEnumerable<T> collection, string message = null)
        {
            if (!collection.Any())
            {
                throw new AssertionException(message ?? "Collection is empty");
            }
        }

        public static void Greater<T>(T actual, T threshold, string message = null) 
            where T : IComparable<T>
        {
            if (actual.CompareTo(threshold) <= 0)
            {
                throw new AssertionException(
                    message ?? $"Expected greater than {threshold}, but was {actual}");
            }
        }

        public static void Less<T>(T actual, T threshold, string message = null)
            where T : IComparable<T>
        {
            if (actual.CompareTo(threshold) >= 0)
            {
                throw new AssertionException(
                    message ?? $"Expected less than {threshold}, but was {actual}");
            }
        }

        public static void InRange<T>(T actual, T min, T max, string message = null)
            where T : IComparable<T>
        {
            if (actual.CompareTo(min) < 0 || actual.CompareTo(max) > 0)
            {
                throw new AssertionException(
                    message ?? $"Expected value in range [{min}, {max}], but was {actual}");
            }
        }
    }

    public class AssertionException : Exception
    {
        public AssertionException(string message) : base(message) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TestClassAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestAttribute : Attribute
    {
        public string Description { get; set; }
        public TestAttribute(string description = null)
        {
            Description = description;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class SkipAttribute : Attribute
    {
        public string Reason { get; set; }
        public SkipAttribute(string reason)
        {
            Reason = reason;
        }
    }
} 
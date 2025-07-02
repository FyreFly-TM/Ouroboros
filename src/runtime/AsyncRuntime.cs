using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Ouro.Runtime
{
    /// <summary>
    /// Async/await runtime support for Ouroboros
    /// </summary>
    public class AsyncRuntime
    {
        private static readonly AsyncRuntime instance = new();
        private readonly TaskScheduler taskScheduler;
        private readonly ConcurrentDictionary<int, AsyncContext> contexts = new();
        private readonly AsyncLocal<AsyncContext?> currentContext = new();
        private int nextContextId = 1;

        public static AsyncRuntime Instance => instance;

        private AsyncRuntime()
        {
            taskScheduler = new OuroborosTaskScheduler();
        }

        /// <summary>
        /// Create a new async context
        /// </summary>
        public AsyncContext CreateContext()
        {
            var context = new AsyncContext(Interlocked.Increment(ref nextContextId));
            contexts[context.Id] = context;
            return context;
        }

        /// <summary>
        /// Get current async context
        /// </summary>
        public AsyncContext? CurrentContext => currentContext.Value;

        /// <summary>
        /// Run async function
        /// </summary>
        public Task<T> RunAsync<T>(Func<Task<T>> asyncFunc, AsyncContext? context = null)
        {
            context ??= CreateContext();
            currentContext.Value = context;

            var task = Task.Factory.StartNew(
                async () => await asyncFunc(),
                CancellationToken.None,
                TaskCreationOptions.None,
                taskScheduler
            ).Unwrap();

            return task;
        }

        /// <summary>
        /// Run async action
        /// </summary>
        public Task RunAsync(Func<Task> asyncAction, AsyncContext? context = null)
        {
            context ??= CreateContext();
            currentContext.Value = context;

            return Task.Factory.StartNew(
                async () => await asyncAction(),
                CancellationToken.None,
                TaskCreationOptions.None,
                taskScheduler
            ).Unwrap();
        }

        /// <summary>
        /// Create an async state machine
        /// </summary>
        public IAsyncStateMachine CreateStateMachine<TStateMachine>()
            where TStateMachine : IAsyncStateMachine, new()
        {
            return new TStateMachine();
        }

        /// <summary>
        /// Yield control to other tasks
        /// </summary>
        public YieldAwaitable Yield()
        {
            return Task.Yield();
        }

        /// <summary>
        /// Delay execution
        /// </summary>
        public Task Delay(int milliseconds)
        {
            return Task.Delay(milliseconds);
        }

        /// <summary>
        /// Run multiple tasks concurrently
        /// </summary>
        public Task WhenAll(params Task[] tasks)
        {
            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Run multiple tasks and return when any completes
        /// </summary>
        public Task<Task> WhenAny(params Task[] tasks)
        {
            return Task.WhenAny(tasks);
        }

        /// <summary>
        /// Create a completed task
        /// </summary>
        public Task<T> FromResult<T>(T result)
        {
            return Task.FromResult(result);
        }

        /// <summary>
        /// Create a failed task
        /// </summary>
        public Task<T> FromException<T>(Exception exception)
        {
            return Task.FromException<T>(exception);
        }

        /// <summary>
        /// Create a canceled task
        /// </summary>
        public Task<T> FromCanceled<T>(CancellationToken cancellationToken)
        {
            return Task.FromCanceled<T>(cancellationToken);
        }
    }

    /// <summary>
    /// Async execution context
    /// </summary>
    public class AsyncContext
    {
        public int Id { get; }
        public CancellationTokenSource CancellationTokenSource { get; }
        public Dictionary<string, object> Items { get; } = new();
        public List<Exception> UnhandledExceptions { get; } = new();

        internal AsyncContext(int id)
        {
            Id = id;
            CancellationTokenSource = new CancellationTokenSource();
        }

        public CancellationToken CancellationToken => CancellationTokenSource.Token;

        public void Cancel()
        {
            CancellationTokenSource.Cancel();
        }

        public void ReportUnhandledException(Exception ex)
        {
            UnhandledExceptions.Add(ex);
        }
    }

    /// <summary>
    /// Custom task scheduler for Ouroboros
    /// </summary>
    internal class OuroborosTaskScheduler : TaskScheduler
    {
        private readonly BlockingCollection<Task> taskQueue = new();
        private readonly Thread[] workers;
        private readonly CancellationTokenSource shutdownToken = new();

        public OuroborosTaskScheduler()
        {
            var workerCount = Environment.ProcessorCount;
            workers = new Thread[workerCount];

            for (int i = 0; i < workerCount; i++)
            {
                workers[i] = new Thread(WorkerThread)
                {
                    Name = $"Ouroboros-Worker-{i}",
                    IsBackground = true
                };
                workers[i].Start();
            }
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return taskQueue.ToArray();
        }

        protected override void QueueTask(Task task)
        {
            taskQueue.Add(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return Thread.CurrentThread.Name?.StartsWith("Ouroboros-Worker") == true &&
                   TryExecuteTask(task);
        }

        private void WorkerThread()
        {
            while (!shutdownToken.Token.IsCancellationRequested)
            {
                try
                {
                    var task = taskQueue.Take(shutdownToken.Token);
                    TryExecuteTask(task);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Log error
                    Console.Error.WriteLine($"Worker thread error: {ex}");
                }
            }
        }

        public void Shutdown()
        {
            shutdownToken.Cancel();
            taskQueue.CompleteAdding();
            
            foreach (var worker in workers)
            {
                worker.Join(TimeSpan.FromSeconds(5));
            }
        }
    }

    /// <summary>
    /// Async state machine builder
    /// </summary>
    public struct AsyncTaskMethodBuilder<T>
    {
        private AsyncTaskMethodBuilder builder;
        private T result;

        public static AsyncTaskMethodBuilder<T> Create()
        {
            return new AsyncTaskMethodBuilder<T>
            {
                builder = AsyncTaskMethodBuilder.Create()
            };
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            builder.Start(ref stateMachine);
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            builder.SetStateMachine(stateMachine);
        }

        public void SetException(Exception exception)
        {
            builder.SetException(exception);
        }

        public void SetResult(T result)
        {
            this.result = result;
            builder.SetResult();
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter,
            ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            builder.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter,
            ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }

        public Task<T> Task
        {
            get
            {
                var capturedResult = result;
                return builder.Task.ContinueWith(_ => capturedResult);
            }
        }
    }

    /// <summary>
    /// Awaitable for Ouroboros async operations
    /// </summary>
    public struct OuroborosAwaitable<T>
    {
        private readonly Task<T> task;

        public OuroborosAwaitable(Task<T> task)
        {
            this.task = task;
        }

        public OuroborosAwaiter<T> GetAwaiter()
        {
            return new OuroborosAwaiter<T>(task);
        }
    }

    /// <summary>
    /// Awaiter for Ouroboros async operations
    /// </summary>
    public struct OuroborosAwaiter<T> : INotifyCompletion, ICriticalNotifyCompletion
    {
        private readonly Task<T> task;

        public OuroborosAwaiter(Task<T> task)
        {
            this.task = task;
        }

        public bool IsCompleted => task.IsCompleted;

        public T GetResult()
        {
            return task.GetAwaiter().GetResult();
        }

        public void OnCompleted(Action continuation)
        {
            task.GetAwaiter().OnCompleted(continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            task.GetAwaiter().UnsafeOnCompleted(continuation);
        }
    }

    /// <summary>
    /// Extensions for async support
    /// </summary>
    public static class AsyncExtensions
    {
        /// <summary>
        /// Configure await behavior
        /// </summary>
        public static ConfiguredTaskAwaitable<T> ConfigureAwait<T>(this Task<T> task, bool continueOnCapturedContext)
        {
            return task.ConfigureAwait(continueOnCapturedContext);
        }

        /// <summary>
        /// Convert to Ouroboros awaitable
        /// </summary>
        public static OuroborosAwaitable<T> AsOuroborosAwaitable<T>(this Task<T> task)
        {
            return new OuroborosAwaitable<T>(task);
        }

        /// <summary>
        /// Run synchronously
        /// </summary>
        public static T RunSync<T>(this Task<T> task)
        {
            return task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Fire and forget
        /// </summary>
        public static void FireAndForget(this Task task, Action<Exception>? errorHandler = null)
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted && errorHandler != null)
                {
                    errorHandler(t.Exception!.GetBaseException());
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// Timeout after specified duration
        /// </summary>
        public static async Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource();
            var delayTask = Task.Delay(timeout, cts.Token);
            var completedTask = await Task.WhenAny(task, delayTask);
            
            if (completedTask == delayTask)
            {
                throw new TimeoutException($"Operation timed out after {timeout}");
            }
            
            cts.Cancel(); // Cancel the delay task
            return await task;
        }

        /// <summary>
        /// Retry async operation
        /// </summary>
        public static async Task<T> RetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3, TimeSpan? delay = null)
        {
            Exception? lastException = null;
            
            for (int i = 0; i <= maxRetries; i++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex) when (i < maxRetries)
                {
                    lastException = ex;
                    if (delay.HasValue)
                    {
                        await Task.Delay(delay.Value);
                    }
                }
            }
            
            throw new AggregateException($"Operation failed after {maxRetries} retries", lastException!);
        }
    }

    /// <summary>
    /// Async local storage
    /// </summary>
    public class AsyncLocalStorage<T>
    {
        private readonly AsyncLocal<T> storage = new();

        public T? Value
        {
            get => storage.Value;
            set => storage.Value = value!;
        }
    }

    /// <summary>
    /// Async semaphore for limiting concurrent operations
    /// </summary>
    public class AsyncSemaphore
    {
        private readonly SemaphoreSlim semaphore;

        public AsyncSemaphore(int initialCount, int maxCount)
        {
            semaphore = new SemaphoreSlim(initialCount, maxCount);
        }

        public async Task<IDisposable> LockAsync()
        {
            await semaphore.WaitAsync();
            return new Releaser(semaphore);
        }

        private class Releaser : IDisposable
        {
            private readonly SemaphoreSlim semaphore;

            public Releaser(SemaphoreSlim semaphore)
            {
                this.semaphore = semaphore;
            }

            public void Dispose()
            {
                semaphore.Release();
            }
        }
    }
} 
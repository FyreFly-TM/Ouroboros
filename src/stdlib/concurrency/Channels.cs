using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ouro.Stdlib.Concurrency
{
    /// <summary>
    /// Channel for inter-thread communication
    /// </summary>
    public class Channel<T>
    {
        private readonly ConcurrentQueue<T> queue;
        private readonly SemaphoreSlim itemsSemaphore;
        private readonly SemaphoreSlim spaceSemaphore;
        private readonly int capacity;
        private int closed = 0;
        private readonly List<TaskCompletionSource<bool>> waitingReceivers = new();
        private readonly List<TaskCompletionSource<bool>> waitingSenders = new();
        private readonly object lockObj = new();

        public bool IsClosed => Interlocked.CompareExchange(ref closed, 0, 0) == 1;
        public int Count => queue.Count;
        public bool IsEmpty => queue.IsEmpty;
        public bool IsFull => capacity > 0 && queue.Count >= capacity;

        /// <summary>
        /// Create an unbuffered channel
        /// </summary>
        public Channel() : this(0) { }

        /// <summary>
        /// Create a buffered channel with specified capacity
        /// </summary>
        public Channel(int capacity)
        {
            this.capacity = capacity;
            queue = new ConcurrentQueue<T>();
            itemsSemaphore = new SemaphoreSlim(0);
            spaceSemaphore = capacity > 0 ? new SemaphoreSlim(capacity) : null!;
        }

        /// <summary>
        /// Send a value to the channel
        /// </summary>
        public async Task<bool> SendAsync(T value, CancellationToken cancellationToken = default)
        {
            if (IsClosed)
                throw new ChannelClosedException("Cannot send to a closed channel");

            if (capacity > 0)
            {
                // Buffered channel
                await spaceSemaphore.WaitAsync(cancellationToken);
                
                if (IsClosed)
                {
                    spaceSemaphore.Release();
                    throw new ChannelClosedException("Cannot send to a closed channel");
                }

                queue.Enqueue(value);
                itemsSemaphore.Release();
                
                // Notify waiting receivers
                NotifyReceivers();
            }
            else
            {
                // Unbuffered channel - wait for receiver
                var tcs = new TaskCompletionSource<bool>();
                
                lock (lockObj)
                {
                    if (IsClosed)
                        throw new ChannelClosedException("Cannot send to a closed channel");

                    if (waitingReceivers.Count > 0)
                    {
                        // Direct handoff to waiting receiver
                        queue.Enqueue(value);
                        var receiver = waitingReceivers[0];
                        waitingReceivers.RemoveAt(0);
                        receiver.SetResult(true);
                        return true;
                    }
                    
                    // No receiver waiting, sender must wait
                    waitingSenders.Add(tcs);
                }

                using (cancellationToken.Register(() => tcs.TrySetCanceled()))
                {
                    await tcs.Task;
                }
                
                queue.Enqueue(value);
                return true;
            }

            return true;
        }

        /// <summary>
        /// Try to send without blocking
        /// </summary>
        public bool TrySend(T value)
        {
            if (IsClosed)
                return false;

            if (capacity > 0 && queue.Count < capacity)
            {
                queue.Enqueue(value);
                itemsSemaphore.Release();
                NotifyReceivers();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Receive a value from the channel
        /// </summary>
        public async Task<(bool success, T value)> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            if (capacity > 0)
            {
                // Buffered channel
                try
                {
                    await itemsSemaphore.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    if (IsClosed && queue.TryDequeue(out var item))
                    {
                        return (true, item);
                    }
                    throw;
                }

                if (queue.TryDequeue(out var value))
                {
                    spaceSemaphore?.Release();
                    NotifySenders();
                    return (true, value);
                }

                if (IsClosed)
                    return (false, default(T)!);

                throw new InvalidOperationException("Unexpected channel state");
            }
            else
            {
                // Unbuffered channel
                var tcs = new TaskCompletionSource<bool>();
                
                lock (lockObj)
                {
                    if (queue.TryDequeue(out var value))
                    {
                        // Notify waiting sender
                        if (waitingSenders.Count > 0)
                        {
                            var sender = waitingSenders[0];
                            waitingSenders.RemoveAt(0);
                            sender.SetResult(true);
                        }
                        return (true, value);
                    }

                    if (IsClosed)
                        return (false, default(T)!);

                    waitingReceivers.Add(tcs);
                }

                using (cancellationToken.Register(() => tcs.TrySetCanceled()))
                {
                    await tcs.Task;
                }

                if (queue.TryDequeue(out var result))
                {
                    return (true, result);
                }

                return (false, default(T)!);
            }
        }

        /// <summary>
        /// Try to receive without blocking
        /// </summary>
        public (bool success, T value) TryReceive()
        {
            if (queue.TryDequeue(out var value))
            {
                spaceSemaphore?.Release();
                NotifySenders();
                return (true, value);
            }

            return (false, default(T)!);
        }

        /// <summary>
        /// Close the channel
        /// </summary>
        public void Close()
        {
            if (Interlocked.CompareExchange(ref closed, 1, 0) == 0)
            {
                // Wake up all waiting operations
                lock (lockObj)
                {
                    foreach (var tcs in waitingReceivers)
                    {
                        tcs.TrySetResult(false);
                    }
                    waitingReceivers.Clear();

                    foreach (var tcs in waitingSenders)
                    {
                        tcs.TrySetException(new ChannelClosedException("Channel was closed"));
                    }
                    waitingSenders.Clear();
                }

                // Release all waiting semaphore operations
                itemsSemaphore.Release(int.MaxValue / 2);
                spaceSemaphore?.Release(int.MaxValue / 2);
            }
        }

        /// <summary>
        /// Async enumerable for channel
        /// </summary>
        public async IAsyncEnumerable<T> GetAsyncEnumerable(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var (success, value) = await ReceiveAsync(cancellationToken);
                if (!success)
                    break;
                    
                yield return value;
            }
        }

        private void NotifyReceivers()
        {
            lock (lockObj)
            {
                if (waitingReceivers.Count > 0)
                {
                    var receiver = waitingReceivers[0];
                    waitingReceivers.RemoveAt(0);
                    receiver.TrySetResult(true);
                }
            }
        }

        private void NotifySenders()
        {
            lock (lockObj)
            {
                if (waitingSenders.Count > 0)
                {
                    var sender = waitingSenders[0];
                    waitingSenders.RemoveAt(0);
                    sender.TrySetResult(true);
                }
            }
        }
    }

    /// <summary>
    /// Select operation for multiple channels
    /// </summary>
    public static class Select
    {
        /// <summary>
        /// Select from multiple receive operations
        /// </summary>
        public static async Task<(int index, T value)> ReceiveAsync<T>(params Channel<T>[] channels)
        {
            var cts = new CancellationTokenSource();
            var tasks = new List<Task<(bool success, T value, int index)>>();

            for (int i = 0; i < channels.Length; i++)
            {
                var index = i;
                var channel = channels[i];
                tasks.Add(ReceiveWithIndex(channel, index, cts.Token));
            }

            var completed = await Task.WhenAny(tasks);
            cts.Cancel();

            var result = await completed;
            if (result.success)
            {
                return (result.index, result.value);
            }

            throw new ChannelClosedException("All channels are closed");
        }

        private static async Task<(bool success, T value, int index)> ReceiveWithIndex<T>(
            Channel<T> channel, int index, CancellationToken cancellationToken)
        {
            try
            {
                var (success, value) = await channel.ReceiveAsync(cancellationToken);
                return (success, value, index);
            }
            catch (OperationCanceledException)
            {
                return (false, default(T)!, index);
            }
        }

        /// <summary>
        /// Select with timeout
        /// </summary>
        public static async Task<(bool success, int index, T value)> ReceiveAsync<T>(
            TimeSpan timeout, params Channel<T>[] channels)
        {
            using var cts = new CancellationTokenSource(timeout);
            
            try
            {
                var (index, value) = await ReceiveAsync(channels);
                return (true, index, value);
            }
            catch (OperationCanceledException)
            {
                return (false, -1, default(T)!);
            }
        }
    }

    /// <summary>
    /// Channel closed exception
    /// </summary>
    public class ChannelClosedException : Exception
    {
        public ChannelClosedException(string message) : base(message) { }
    }

    /// <summary>
    /// Broadcast channel for one-to-many communication
    /// </summary>
    public class BroadcastChannel<T>
    {
        private readonly List<Channel<T>> subscribers = new();
        private readonly object lockObj = new();
        private bool closed = false;

        /// <summary>
        /// Subscribe to the broadcast channel
        /// </summary>
        public Channel<T> Subscribe(int bufferSize = 10)
        {
            lock (lockObj)
            {
                if (closed)
                    throw new ChannelClosedException("Cannot subscribe to closed broadcast channel");

                var channel = new Channel<T>(bufferSize);
                subscribers.Add(channel);
                return channel;
            }
        }

        /// <summary>
        /// Unsubscribe a channel
        /// </summary>
        public void Unsubscribe(Channel<T> channel)
        {
            lock (lockObj)
            {
                subscribers.Remove(channel);
                channel.Close();
            }
        }

        /// <summary>
        /// Broadcast a value to all subscribers
        /// </summary>
        public async Task BroadcastAsync(T value)
        {
            Channel<T>[] currentSubscribers;
            
            lock (lockObj)
            {
                if (closed)
                    throw new ChannelClosedException("Cannot broadcast to closed channel");
                    
                currentSubscribers = subscribers.ToArray();
            }

            var tasks = new List<Task>();
            foreach (var subscriber in currentSubscribers)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await subscriber.SendAsync(value);
                    }
                    catch (ChannelClosedException)
                    {
                        // Subscriber closed, remove it
                        lock (lockObj)
                        {
                            subscribers.Remove(subscriber);
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Close the broadcast channel
        /// </summary>
        public void Close()
        {
            lock (lockObj)
            {
                if (closed) return;
                
                closed = true;
                foreach (var subscriber in subscribers)
                {
                    subscriber.Close();
                }
                subscribers.Clear();
            }
        }
    }

    /// <summary>
    /// Pipeline stage for channel-based data processing
    /// </summary>
    public class Pipeline<TIn, TOut>
    {
        private readonly Func<TIn, Task<TOut>> processor;
        private readonly Channel<TIn> input;
        private readonly Channel<TOut> output;
        private readonly int workers;
        private readonly CancellationTokenSource cts = new();
        private readonly List<Task> workerTasks = new();

        public Pipeline(
            Channel<TIn> input,
            Channel<TOut> output,
            Func<TIn, Task<TOut>> processor,
            int workers = 1)
        {
            this.input = input;
            this.output = output;
            this.processor = processor;
            this.workers = workers;
        }

        /// <summary>
        /// Start the pipeline
        /// </summary>
        public void Start()
        {
            for (int i = 0; i < workers; i++)
            {
                workerTasks.Add(Task.Run(WorkerLoop));
            }
        }

        /// <summary>
        /// Stop the pipeline
        /// </summary>
        public async Task StopAsync()
        {
            cts.Cancel();
            await Task.WhenAll(workerTasks);
        }

        private async Task WorkerLoop()
        {
            try
            {
                await foreach (var item in input.GetAsyncEnumerable(cts.Token))
                {
                    var result = await processor(item);
                    await output.SendAsync(result, cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
            }
        }

        /// <summary>
        /// Create a pipeline chain
        /// </summary>
        public static Pipeline<TIn, TOut> Create<TIn, TMid, TOut>(
            Channel<TIn> input,
            Channel<TOut> output,
            Func<TIn, Task<TMid>> stage1,
            Func<TMid, Task<TOut>> stage2,
            int workers = 1)
        {
            var middle = new Channel<TMid>(workers * 2);
            
            var pipeline1 = new Pipeline<TIn, TMid>(input, middle, stage1, workers);
            var pipeline2 = new Pipeline<TMid, TOut>(middle, output, stage2, workers);
            
            pipeline1.Start();
            pipeline2.Start();
            
            // Return a composite pipeline
            return new Pipeline<TIn, TOut>(input, output, 
                async (item) => await stage2(await stage1(item)), workers);
        }
    }
} 
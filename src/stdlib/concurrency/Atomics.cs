using System;
using System.Threading;
using System.Runtime.CompilerServices;

namespace Ouroboros.Stdlib.Concurrency
{
    /// <summary>
    /// Atomic operations for lock-free programming
    /// </summary>
    public static class Atomic
    {
        /// <summary>
        /// Atomic integer operations
        /// </summary>
        public class AtomicInt32
        {
            private int value;

            public int Value
            {
                get => Volatile.Read(ref value);
                set => Volatile.Write(ref this.value, value);
            }

            public AtomicInt32(int initialValue = 0)
            {
                value = initialValue;
            }

            public int Add(int addend)
            {
                return Interlocked.Add(ref value, addend);
            }

            public int Increment()
            {
                return Interlocked.Increment(ref value);
            }

            public int Decrement()
            {
                return Interlocked.Decrement(ref value);
            }

            public int Exchange(int newValue)
            {
                return Interlocked.Exchange(ref value, newValue);
            }

            public int CompareExchange(int comparand, int newValue)
            {
                return Interlocked.CompareExchange(ref value, newValue, comparand);
            }

            public bool CompareAndSwap(int expected, int newValue)
            {
                return Interlocked.CompareExchange(ref value, newValue, expected) == expected;
            }

            public static implicit operator int(AtomicInt32 atomic)
            {
                return atomic.Value;
            }
        }

        /// <summary>
        /// Atomic long operations
        /// </summary>
        public class AtomicInt64
        {
            private long value;

            public long Value
            {
                get => Volatile.Read(ref value);
                set => Volatile.Write(ref this.value, value);
            }

            public AtomicInt64(long initialValue = 0)
            {
                value = initialValue;
            }

            public long Add(long addend)
            {
                return Interlocked.Add(ref value, addend);
            }

            public long Increment()
            {
                return Interlocked.Increment(ref value);
            }

            public long Decrement()
            {
                return Interlocked.Decrement(ref value);
            }

            public long Exchange(long newValue)
            {
                return Interlocked.Exchange(ref value, newValue);
            }

            public long CompareExchange(long comparand, long newValue)
            {
                return Interlocked.CompareExchange(ref value, newValue, comparand);
            }

            public bool CompareAndSwap(long expected, long newValue)
            {
                return Interlocked.CompareExchange(ref value, newValue, expected) == expected;
            }

            public static implicit operator long(AtomicInt64 atomic)
            {
                return atomic.Value;
            }
        }

        /// <summary>
        /// Atomic boolean operations
        /// </summary>
        public class AtomicBool
        {
            private int value;

            public bool Value
            {
                get => Volatile.Read(ref value) != 0;
                set => Volatile.Write(ref this.value, value ? 1 : 0);
            }

            public AtomicBool(bool initialValue = false)
            {
                value = initialValue ? 1 : 0;
            }

            public bool Exchange(bool newValue)
            {
                return Interlocked.Exchange(ref value, newValue ? 1 : 0) != 0;
            }

            public bool CompareExchange(bool comparand, bool newValue)
            {
                return Interlocked.CompareExchange(ref value, newValue ? 1 : 0, comparand ? 1 : 0) != 0;
            }

            public bool CompareAndSwap(bool expected, bool newValue)
            {
                return Interlocked.CompareExchange(ref value, newValue ? 1 : 0, expected ? 1 : 0) == (expected ? 1 : 0);
            }

            public static implicit operator bool(AtomicBool atomic)
            {
                return atomic.Value;
            }
        }

        /// <summary>
        /// Atomic reference operations
        /// </summary>
        public class AtomicReference<T> where T : class
        {
            private T? value;

            public T? Value
            {
                get => Volatile.Read(ref value);
                set => Volatile.Write(ref this.value, value);
            }

            public AtomicReference(T? initialValue = null)
            {
                value = initialValue;
            }

            public T? Exchange(T? newValue)
            {
                return Interlocked.Exchange(ref value, newValue);
            }

            public T? CompareExchange(T? comparand, T? newValue)
            {
                return Interlocked.CompareExchange(ref value, newValue, comparand);
            }

            public bool CompareAndSwap(T? expected, T? newValue)
            {
                return Interlocked.CompareExchange(ref value, newValue, expected) == expected;
            }
        }

        /// <summary>
        /// Memory fence operations
        /// </summary>
        public static class MemoryFence
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Acquire()
            {
                Thread.MemoryBarrier();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Release()
            {
                Thread.MemoryBarrier();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Full()
            {
                Thread.MemoryBarrier();
            }
        }
    }

    /// <summary>
    /// Read-Write lock for concurrent access
    /// </summary>
    public class ReadWriteLock : IDisposable
    {
        private readonly ReaderWriterLockSlim rwLock;

        public ReadWriteLock()
        {
            rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        }

        public IDisposable ReadLock()
        {
            rwLock.EnterReadLock();
            return new LockReleaser(() => rwLock.ExitReadLock());
        }

        public IDisposable WriteLock()
        {
            rwLock.EnterWriteLock();
            return new LockReleaser(() => rwLock.ExitWriteLock());
        }

        public IDisposable UpgradeableReadLock()
        {
            rwLock.EnterUpgradeableReadLock();
            return new LockReleaser(() => rwLock.ExitUpgradeableReadLock());
        }

        public bool TryReadLock(TimeSpan timeout, out IDisposable? lockHandle)
        {
            if (rwLock.TryEnterReadLock(timeout))
            {
                lockHandle = new LockReleaser(() => rwLock.ExitReadLock());
                return true;
            }
            lockHandle = null;
            return false;
        }

        public bool TryWriteLock(TimeSpan timeout, out IDisposable? lockHandle)
        {
            if (rwLock.TryEnterWriteLock(timeout))
            {
                lockHandle = new LockReleaser(() => rwLock.ExitWriteLock());
                return true;
            }
            lockHandle = null;
            return false;
        }

        public void Dispose()
        {
            rwLock?.Dispose();
        }

        private class LockReleaser : IDisposable
        {
            private readonly Action releaseAction;

            public LockReleaser(Action releaseAction)
            {
                this.releaseAction = releaseAction;
            }

            public void Dispose()
            {
                releaseAction();
            }
        }
    }

    /// <summary>
    /// Spin lock for short critical sections
    /// </summary>
    public struct SpinLock
    {
        private int locked;

        public void Enter()
        {
            while (Interlocked.CompareExchange(ref locked, 1, 0) != 0)
            {
                Thread.SpinWait(1);
            }
        }

        public void Exit()
        {
            Volatile.Write(ref locked, 0);
        }

        public bool TryEnter()
        {
            return Interlocked.CompareExchange(ref locked, 1, 0) == 0;
        }

        public bool TryEnter(TimeSpan timeout)
        {
            var startTime = Environment.TickCount64;
            var timeoutMs = (long)timeout.TotalMilliseconds;

            while (!TryEnter())
            {
                if (Environment.TickCount64 - startTime >= timeoutMs)
                    return false;
                    
                Thread.SpinWait(1);
            }

            return true;
        }
    }

    /// <summary>
    /// Barrier for synchronizing phases
    /// </summary>
    public class Barrier : IDisposable
    {
        private readonly System.Threading.Barrier barrier;

        public int ParticipantCount => barrier.ParticipantCount;
        public long CurrentPhaseNumber => barrier.CurrentPhaseNumber;

        public Barrier(int participantCount)
        {
            barrier = new System.Threading.Barrier(participantCount);
        }

        public Barrier(int participantCount, Action<System.Threading.Barrier> postPhaseAction)
        {
            barrier = new System.Threading.Barrier(participantCount, postPhaseAction);
        }

        public void SignalAndWait()
        {
            barrier.SignalAndWait();
        }

        public bool SignalAndWait(TimeSpan timeout)
        {
            return barrier.SignalAndWait(timeout);
        }

        public long AddParticipant()
        {
            return barrier.AddParticipant();
        }

        public void RemoveParticipant()
        {
            barrier.RemoveParticipant();
        }

        public void Dispose()
        {
            barrier?.Dispose();
        }
    }

    /// <summary>
    /// Count down event for signaling completion
    /// </summary>
    public class CountdownEvent : IDisposable
    {
        private readonly System.Threading.CountdownEvent countdown;

        public int CurrentCount => countdown.CurrentCount;
        public int InitialCount => countdown.InitialCount;
        public bool IsSet => countdown.IsSet;

        public CountdownEvent(int initialCount)
        {
            countdown = new System.Threading.CountdownEvent(initialCount);
        }

        public void Signal()
        {
            countdown.Signal();
        }

        public bool Signal(int signalCount)
        {
            return countdown.Signal(signalCount);
        }

        public void AddCount()
        {
            countdown.AddCount();
        }

        public void AddCount(int signalCount)
        {
            countdown.AddCount(signalCount);
        }

        public void Reset()
        {
            countdown.Reset();
        }

        public void Reset(int count)
        {
            countdown.Reset(count);
        }

        public void Wait()
        {
            countdown.Wait();
        }

        public bool Wait(TimeSpan timeout)
        {
            return countdown.Wait(timeout);
        }

        public void Dispose()
        {
            countdown?.Dispose();
        }
    }

    /// <summary>
    /// Lock-free queue implementation
    /// </summary>
    public class LockFreeQueue<T>
    {
        private class Node
        {
            public T? Value;
            public Node? Next;
        }

        private Node head;
        private Node tail;

        public LockFreeQueue()
        {
            var dummy = new Node();
            head = tail = dummy;
        }

        public void Enqueue(T item)
        {
            var newNode = new Node { Value = item };
            
            while (true)
            {
                var last = tail;
                var next = last.Next;
                
                if (last == tail)
                {
                    if (next == null)
                    {
                        if (Interlocked.CompareExchange(ref last.Next, newNode, null) == null)
                        {
                            Interlocked.CompareExchange(ref tail, newNode, last);
                            break;
                        }
                    }
                    else
                    {
                        Interlocked.CompareExchange(ref tail, next, last);
                    }
                }
            }
        }

        public bool TryDequeue(out T? item)
        {
            while (true)
            {
                var first = head;
                var last = tail;
                var next = first.Next;
                
                if (first == head)
                {
                    if (first == last)
                    {
                        if (next == null)
                        {
                            item = default;
                            return false;
                        }
                        
                        Interlocked.CompareExchange(ref tail, next, last);
                    }
                    else
                    {
                        item = next!.Value;
                        if (Interlocked.CompareExchange(ref head, next!, first) == first)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        public bool IsEmpty
        {
            get
            {
                var first = head;
                var last = tail;
                return first == last && first.Next == null;
            }
        }
    }

    /// <summary>
    /// Thread-safe object pool
    /// </summary>
    public class ObjectPool<T> where T : class, new()
    {
        private readonly ConcurrentBag<T> objects = new();
        private readonly Func<T> objectGenerator;
        private readonly Action<T>? resetAction;
        private readonly int maxSize;
        private int currentSize = 0;

        public ObjectPool(int maxSize = 100, Func<T>? objectGenerator = null, Action<T>? resetAction = null)
        {
            this.maxSize = maxSize;
            this.objectGenerator = objectGenerator ?? (() => new T());
            this.resetAction = resetAction;
        }

        public T Rent()
        {
            if (objects.TryTake(out var item))
            {
                Interlocked.Decrement(ref currentSize);
                return item;
            }

            return objectGenerator();
        }

        public void Return(T item)
        {
            resetAction?.Invoke(item);
            
            if (currentSize < maxSize)
            {
                objects.Add(item);
                Interlocked.Increment(ref currentSize);
            }
        }
    }
} 
using System;
using System.Collections;
using System.Collections.Generic;
using Ouro.Core;
using Ouro.StdLib.Math;

namespace Ouro.StdLib.Collections
{
    /// <summary>
    /// Queue (FIFO) implementation for Ouro
    /// </summary>
    public class Queue<T> : IEnumerable<T>
    {
        private T[] items;
        private int head;       // First element in queue
        private int tail;       // Last element in queue
        private int size;       // Number of elements
        private int version;
        private const int DefaultCapacity = 4;

        public int Count => size;

        public Queue()
        {
            items = new T[DefaultCapacity];
        }

        public Queue(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            
            items = new T[capacity];
            head = 0;
            tail = 0;
            size = 0;
            version = 0;
        }

        public Queue(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            
            items = new T[DefaultCapacity];
            size = 0;
            version = 0;
            
            foreach (T item in collection)
            {
                Enqueue(item);
            }
        }

        public void Clear()
        {
            if (head < tail)
            {
                Array.Clear(items, head, size);
            }
            else
            {
                Array.Clear(items, head, items.Length - head);
                Array.Clear(items, 0, tail);
            }
            
            head = 0;
            tail = 0;
            size = 0;
            version++;
        }

        public bool Contains(T item)
        {
            int index = head;
            int count = size;
            
            EqualityComparer<T> c = EqualityComparer<T>.Default;
            while (count-- > 0)
            {
                if (item == null)
                {
                    if (items[index] == null)
                        return true;
                }
                else if (items[index] != null && c.Equals(items[index], item))
                {
                    return true;
                }
                index = (index + 1) % items.Length;
            }
            
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            
            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            
            if (array.Length - arrayIndex < size)
                throw new ArgumentException("Array too small");
            
            int numToCopy = size;
            if (numToCopy == 0) return;
            
            int firstPart = global::System.Math.Min(items.Length - head, numToCopy);
            Array.Copy(items, head, array, arrayIndex, firstPart);
            numToCopy -= firstPart;
            
            if (numToCopy > 0)
            {
                Array.Copy(items, 0, array, arrayIndex + firstPart, numToCopy);
            }
        }

        public void Enqueue(T item)
        {
            if (size == items.Length)
            {
                Grow();
            }
            
            items[tail] = item;
            tail = (tail + 1) % items.Length;
            size++;
            version++;
        }

        public T Dequeue()
        {
            if (size == 0)
                throw new InvalidOperationException("Queue is empty");
            
            T removed = items[head];
            items[head] = default(T);
            head = (head + 1) % items.Length;
            size--;
            version++;
            return removed;
        }

        public T Peek()
        {
            if (size == 0)
                throw new InvalidOperationException("Queue is empty");
            
            return items[head];
        }

        public bool TryDequeue(out T result)
        {
            if (size == 0)
            {
                result = default(T);
                return false;
            }
            
            result = items[head];
            items[head] = default(T);
            head = (head + 1) % items.Length;
            size--;
            version++;
            return true;
        }

        public bool TryPeek(out T result)
        {
            if (size == 0)
            {
                result = default(T);
                return false;
            }
            
            result = items[head];
            return true;
        }

        public T[] ToArray()
        {
            T[] arr = new T[size];
            if (size == 0)
                return arr;
            
            if (head < tail)
            {
                Array.Copy(items, head, arr, 0, size);
            }
            else
            {
                Array.Copy(items, head, arr, 0, items.Length - head);
                Array.Copy(items, 0, arr, items.Length - head, tail);
            }
            
            return arr;
        }

        public void TrimExcess()
        {
            int threshold = (int)(items.Length * 0.9);
            if (size < threshold)
            {
                SetCapacity(size);
            }
        }

        private void SetCapacity(int capacity)
        {
            T[] newarray = new T[capacity];
            if (size > 0)
            {
                if (head < tail)
                {
                    Array.Copy(items, head, newarray, 0, size);
                }
                else
                {
                    Array.Copy(items, head, newarray, 0, items.Length - head);
                    Array.Copy(items, 0, newarray, items.Length - head, tail);
                }
            }
            
            items = newarray;
            head = 0;
            tail = (size == capacity) ? 0 : size;
            version++;
        }

        private void Grow()
        {
            int newcapacity = items.Length == 0 ? DefaultCapacity : 2 * items.Length;
            SetCapacity(newcapacity);
        }

        public IEnumerator<T> GetEnumerator()
        {
            int currentVersion = version;
            int index = head;
            int count = size;
            
            while (count-- > 0)
            {
                if (currentVersion != version)
                    throw new InvalidOperationException("Collection was modified");
                
                yield return items[index];
                index = (index + 1) % items.Length;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
} 
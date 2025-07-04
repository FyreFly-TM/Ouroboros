using System;
using System.Collections;
using System.Collections.Generic;
using Ouro.Core;

namespace Ouro.StdLib.Collections
{
    /// <summary>
    /// Stack (LIFO) implementation for Ouroboros
    /// </summary>
    public class Stack<T> : IEnumerable<T>
    {
        private T[] items;
        private int count;
        private int version;
        private const int DefaultCapacity = 4;

        public int Count => count;

        public Stack()
        {
            items = new T[DefaultCapacity];
            count = 0;
            version = 0;
        }

        public Stack(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            
            items = new T[capacity];
            count = 0;
            version = 0;
        }

        public Stack(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            
            if (collection is ICollection<T> c)
            {
                int count = c.Count;
                items = new T[count];
                c.CopyTo(items, 0);
                this.count = count;
            }
            else
            {
                items = new T[DefaultCapacity];
                count = 0;
                
                foreach (T item in collection)
                {
                    Push(item);
                }
            }
            version = 0;
        }

        public void Clear()
        {
            if (count > 0)
            {
                Array.Clear(items, 0, count);
                count = 0;
            }
            version++;
        }

        public bool Contains(T item)
        {
            int index = count;
            
            EqualityComparer<T> c = EqualityComparer<T>.Default;
            while (index-- > 0)
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
            }
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            
            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            
            if (array.Length - arrayIndex < count)
                throw new ArgumentException("Array too small");
            
            int i = 0;
            int j = arrayIndex + count - 1;
            
            while (i < count)
            {
                array[j] = items[i];
                i++;
                j--;
            }
        }

        public T Peek()
        {
            if (count == 0)
                throw new InvalidOperationException("Stack is empty");
            
            return items[count - 1];
        }

        public T Pop()
        {
            if (count == 0)
                throw new InvalidOperationException("Stack is empty");
            
            version++;
            T item = items[--count];
            items[count] = default!;
            return item;
        }

        public void Push(T item)
        {
            if (count == items.Length)
            {
                T[] newArray = new T[items.Length == 0 ? DefaultCapacity : 2 * items.Length];
                Array.Copy(items, 0, newArray, 0, count);
                items = newArray;
            }
            
            items[count++] = item;
            version++;
        }

        public T[] ToArray()
        {
            T[] objArray = new T[count];
            int i = 0;
            
            while (i < count)
            {
                objArray[i] = items[count - i - 1];
                i++;
            }
            return objArray;
        }

        public void TrimExcess()
        {
            int threshold = (int)(items.Length * 0.9);
            if (count < threshold)
            {
                T[] newarray = new T[count];
                Array.Copy(items, 0, newarray, 0, count);
                items = newarray;
                version++;
            }
        }

        public bool TryPeek(out T result)
        {
            if (count == 0)
            {
                result = default!;
                return false;
            }
            
            result = items[count - 1];
            return true;
        }

        public bool TryPop(out T result)
        {
            if (count == 0)
            {
                result = default!;
                return false;
            }
            
            version++;
            result = items[--count];
            items[count] = default!;
            return true;
        }

        public IEnumerator<T> GetEnumerator()
        {
            int currentVersion = version;
            int index = count - 1;
            
            while (index >= 0)
            {
                if (currentVersion != version)
                    throw new InvalidOperationException("Collection was modified");
                
                yield return items[index];
                index--;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
} 
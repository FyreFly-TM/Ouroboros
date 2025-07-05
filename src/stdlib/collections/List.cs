using System;
using System.Collections;
using System.Collections.Generic;
using Ouro.Core;

namespace Ouro.StdLib.Collections
{
    /// <summary>
    /// Dynamic array implementation for Ouro
    /// </summary>
    public class List<T> : IEnumerable<T>
    {
        private T[] items;
        private int count;
        private int capacity;
        private const int DefaultCapacity = 4;

        public int Count => count;
        public int Capacity => capacity;

        public List()
        {
            items = new T[DefaultCapacity];
            capacity = DefaultCapacity;
            count = 0;
        }

        public List(int initialCapacity)
        {
            if (initialCapacity < 0)
                throw new ArgumentException("Capacity cannot be negative");
            
            capacity = initialCapacity > 0 ? initialCapacity : DefaultCapacity;
            items = new T[capacity];
            count = 0;
        }

        public List(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            
            items = new T[DefaultCapacity];
            capacity = DefaultCapacity;
            count = 0;
            
            foreach (var item in collection)
            {
                Add(item);
            }
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= count)
                    throw new IndexOutOfRangeException();
                return items[index];
            }
            set
            {
                if (index < 0 || index >= count)
                    throw new IndexOutOfRangeException();
                items[index] = value;
            }
        }

        public void Add(T item)
        {
            if (count == capacity)
            {
                Resize();
            }
            items[count++] = item;
        }

        public void AddRange(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            
            foreach (var item in collection)
            {
                Add(item);
            }
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= count)
                throw new IndexOutOfRangeException();
            
            count--;
            if (index < count)
            {
                Array.Copy(items, index + 1, items, index, count - index);
            }
            items[count] = default!;
        }

        public void Clear()
        {
            if (count > 0)
            {
                Array.Clear(items, 0, count);
                count = 0;
            }
        }

        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        public int IndexOf(T item)
        {
            return Array.IndexOf(items, item, 0, count);
        }

        public void Insert(int index, T item)
        {
            if (index < 0 || index > count)
                throw new IndexOutOfRangeException();
            
            if (count == capacity)
            {
                Resize();
            }
            
            if (index < count)
            {
                Array.Copy(items, index, items, index + 1, count - index);
            }
            
            items[index] = item;
            count++;
        }

        public void Reverse()
        {
            Array.Reverse(items, 0, count);
        }

        public void Sort()
        {
            Array.Sort(items, 0, count);
        }

        public void Sort(IComparer<T> comparer)
        {
            Array.Sort(items, 0, count, comparer);
        }

        public T[] ToArray()
        {
            T[] array = new T[count];
            Array.Copy(items, 0, array, 0, count);
            return array;
        }

        public T Find(Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));
            
            for (int i = 0; i < count; i++)
            {
                if (match(items[i]))
                {
                    return items[i];
                }
            }
            return default!;
        }

        public List<T> FindAll(Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));
            
            List<T> list = new List<T>();
            for (int i = 0; i < count; i++)
            {
                if (match(items[i]))
                {
                    list.Add(items[i]);
                }
            }
            return list;
        }

        public void ForEach(Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            
            for (int i = 0; i < count; i++)
            {
                action(items[i]);
            }
        }

        private void Resize()
        {
            int newCapacity = capacity == 0 ? DefaultCapacity : capacity * 2;
            T[] newItems = new T[newCapacity];
            Array.Copy(items, 0, newItems, 0, count);
            items = newItems;
            capacity = newCapacity;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < count; i++)
            {
                yield return items[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
} 
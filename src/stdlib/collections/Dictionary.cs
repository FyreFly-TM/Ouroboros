using System;
using System.Collections;
using System.Collections.Generic;
using Ouroboros.Core;

namespace Ouroboros.StdLib.Collections
{
    /// <summary>
    /// Hash table implementation for Ouroboros
    /// </summary>
    public class Dictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private struct Entry
        {
            public int hashCode;
            public int next;
            public TKey key;
            public TValue value;
        }

        private int[] buckets;
        private Entry[] entries;
        private int count;
        private int version;
        private int freeList;
        private int freeCount;
        private IEqualityComparer<TKey> comparer;
        private const int DefaultCapacity = 16;

        public int Count => count - freeCount;
        public ICollection<TKey> Keys => new KeyCollection(this);
        public ICollection<TValue> Values => new ValueCollection(this);

        public Dictionary() : this(0, null) { }

        public Dictionary(int capacity) : this(capacity, null) { }

        public Dictionary(IEqualityComparer<TKey> comparer) : this(0, comparer) { }

        public Dictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            
            if (capacity > 0)
                Initialize(capacity);
            
            this.comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        public TValue this[TKey key]
        {
            get
            {
                int i = FindEntry(key);
                if (i >= 0)
                    return entries[i].value;
                throw new KeyNotFoundException();
            }
            set
            {
                Insert(key, value, false);
            }
        }

        public void Add(TKey key, TValue value)
        {
            Insert(key, value, true);
        }

        public bool ContainsKey(TKey key)
        {
            return FindEntry(key) >= 0;
        }

        public bool ContainsValue(TValue value)
        {
            if (value == null)
            {
                for (int i = 0; i < count; i++)
                {
                    if (entries[i].hashCode >= 0 && entries[i].value == null)
                        return true;
                }
            }
            else
            {
                EqualityComparer<TValue> c = EqualityComparer<TValue>.Default;
                for (int i = 0; i < count; i++)
                {
                    if (entries[i].hashCode >= 0 && c.Equals(entries[i].value, value))
                        return true;
                }
            }
            return false;
        }

        private void Insert(TKey key, TValue value, bool add)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (buckets == null)
                Initialize(0);

            int hashCode = comparer.GetHashCode(key) & 0x7FFFFFFF;
            int targetBucket = hashCode % buckets.Length;

            for (int i = buckets[targetBucket]; i >= 0; i = entries[i].next)
            {
                if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key))
                {
                    if (add)
                        throw new ArgumentException("Key already exists");
                    
                    entries[i].value = value;
                    version++;
                    return;
                }
            }

            int index;
            if (freeCount > 0)
            {
                index = freeList;
                freeList = entries[index].next;
                freeCount--;
            }
            else
            {
                if (count == entries.Length)
                {
                    Resize();
                    targetBucket = hashCode % buckets.Length;
                }
                index = count;
                count++;
            }

            entries[index].hashCode = hashCode;
            entries[index].next = buckets[targetBucket];
            entries[index].key = key;
            entries[index].value = value;
            buckets[targetBucket] = index;
            version++;
        }

        private void Initialize(int capacity)
        {
            int size = GetPrime(capacity);
            buckets = new int[size];
            for (int i = 0; i < buckets.Length; i++)
                buckets[i] = -1;
            
            entries = new Entry[size];
            freeList = -1;
        }

        private void Resize()
        {
            int newSize = GetPrime(count * 2);
            int[] newBuckets = new int[newSize];
            for (int i = 0; i < newBuckets.Length; i++)
                newBuckets[i] = -1;
            
            Entry[] newEntries = new Entry[newSize];
            Array.Copy(entries, 0, newEntries, 0, count);
            
            for (int i = 0; i < count; i++)
            {
                if (newEntries[i].hashCode >= 0)
                {
                    int bucket = newEntries[i].hashCode % newSize;
                    newEntries[i].next = newBuckets[bucket];
                    newBuckets[bucket] = i;
                }
            }
            
            buckets = newBuckets;
            entries = newEntries;
        }

        public bool Remove(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (buckets != null)
            {
                int hashCode = comparer.GetHashCode(key) & 0x7FFFFFFF;
                int bucket = hashCode % buckets.Length;
                int last = -1;
                for (int i = buckets[bucket]; i >= 0; last = i, i = entries[i].next)
                {
                    if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key))
                    {
                        if (last < 0)
                            buckets[bucket] = entries[i].next;
                        else
                            entries[last].next = entries[i].next;
                        
                        entries[i].hashCode = -1;
                        entries[i].next = freeList;
                        entries[i].key = default(TKey);
                        entries[i].value = default(TValue);
                        freeList = i;
                        freeCount++;
                        version++;
                        return true;
                    }
                }
            }
            return false;
        }

        public void Clear()
        {
            if (count > 0)
            {
                for (int i = 0; i < buckets.Length; i++)
                    buckets[i] = -1;
                
                Array.Clear(entries, 0, count);
                freeList = -1;
                count = 0;
                freeCount = 0;
                version++;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            int i = FindEntry(key);
            if (i >= 0)
            {
                value = entries[i].value;
                return true;
            }
            value = default(TValue);
            return false;
        }

        private int FindEntry(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (buckets != null)
            {
                int hashCode = comparer.GetHashCode(key) & 0x7FFFFFFF;
                for (int i = buckets[hashCode % buckets.Length]; i >= 0; i = entries[i].next)
                {
                    if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key))
                        return i;
                }
            }
            return -1;
        }

        private static int GetPrime(int min)
        {
            // Simple prime number finder for hash table sizing
            int[] primes = { 17, 37, 79, 163, 331, 673, 1361, 2729, 5471, 10949, 21911, 43853, 87719, 175447, 350899 };
            
            for (int i = 0; i < primes.Length; i++)
            {
                if (primes[i] >= min)
                    return primes[i];
            }
            
            // If min is too large, just return the next odd number
            return (min | 1);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            for (int i = 0; i < count; i++)
            {
                if (entries[i].hashCode >= 0)
                {
                    yield return new KeyValuePair<TKey, TValue>(entries[i].key, entries[i].value);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // Key collection
        private class KeyCollection : ICollection<TKey>
        {
            private Dictionary<TKey, TValue> dictionary;

            public KeyCollection(Dictionary<TKey, TValue> dictionary)
            {
                this.dictionary = dictionary;
            }

            public int Count => dictionary.Count;
            public bool IsReadOnly => true;

            public void Add(TKey item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(TKey item) => throw new NotSupportedException();

            public bool Contains(TKey item) => dictionary.ContainsKey(item);

            public void CopyTo(TKey[] array, int arrayIndex)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));

                foreach (var kvp in dictionary)
                {
                    array[arrayIndex++] = kvp.Key;
                }
            }

            public IEnumerator<TKey> GetEnumerator()
            {
                foreach (var kvp in dictionary)
                {
                    yield return kvp.Key;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        // Value collection
        private class ValueCollection : ICollection<TValue>
        {
            private Dictionary<TKey, TValue> dictionary;

            public ValueCollection(Dictionary<TKey, TValue> dictionary)
            {
                this.dictionary = dictionary;
            }

            public int Count => dictionary.Count;
            public bool IsReadOnly => true;

            public void Add(TValue item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(TValue item) => throw new NotSupportedException();

            public bool Contains(TValue item) => dictionary.ContainsValue(item);

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));

                foreach (var kvp in dictionary)
                {
                    array[arrayIndex++] = kvp.Value;
                }
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                foreach (var kvp in dictionary)
                {
                    yield return kvp.Value;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
} 
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Zene.Structs;

namespace cgl
{
    // separately sized buckets and entries
    public class HashTable<K, V>
        where V : class
        where K : notnull
    {
        private struct Entry
        {
            public KeyValuePair<K, V> Data;
            public int Hashcode;
            public int Next;
        }
        
        public HashTable(int capacity = 0)
        {
            _capacity = HashHelpers.GetPrime(capacity);
            _entries = new Entry[_capacity];
            _buckets = new int[_capacity];
            Array.Fill(_buckets, -1);
            
#if TARGET_64BIT
            _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)_capacity);
#endif
        }
        
        private int _count;
        private int _last = -1;
        private int _capacity;
        private int _freeCount;
        private int _freeStart = -1;
        
        private int[] _buckets;
        private Entry[] _entries;
        
#if TARGET_64BIT
        private ulong _fastModMultiplier;
#endif
        
        public int Count => _count;
        public int Capacity => _capacity;
        
        private uint GetIndex(int hashcode)
        {
#if TARGET_64BIT
            return HashHelpers.FastMod((uint)hashcode, (uint)_capacity, _fastModMultiplier);
#else            
            return (uint)hashcode % (uint)_capacity;
#endif
        }
        private void Resize()
        {
            int old = _capacity;
            _capacity = HashHelpers.ExpandPrime(_capacity);
            _buckets = new int[_capacity];
            Array.Fill(_buckets, -1);
            Entry[] ne = new Entry[_capacity];
            Array.Copy(_entries, ne, old);
            _entries = ne;
            
#if TARGET_64BIT
            _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)_capacity);
#endif
            
            for (int i = 0; i < old; i++)
            {
                ref Entry e = ref _entries[i];
                // should not be case
                if (e.Data.Value == null) { continue; }
                ref int bucket = ref _buckets[GetIndex(e.Hashcode)];
                if (bucket >= 0)
                {
                    e.Next = bucket;
                }
                else
                {
                    e.Next = -1;
                }
                bucket = i;
            }
        }
        
        public void Add(K key, V value)
        {
            // Need to increase size
            if (_count == _capacity)
            {
                Resize();
            }
            
            if (value == null)
            {
                throw new ArgumentNullException();
            }
            
            int hashcode = key.GetHashCode();
            ref int bucket = ref _buckets[GetIndex(hashcode)];
            
            int index = _count;
            if (_freeCount > 0)
            {
                index = _freeStart;
                Entry f = _entries[index];
                _freeStart = f.Next;
                _freeCount--;
                // if (f.Next < 0 && _freeCount > 0)
                // {
                //     throw new Exception();
                // }
                if (index > _last)
                {
                    _last = index;
                }
            }
            else
            {
                _last++;
            }
            ref Entry current = ref _entries[index];
            
            _count++;
            int next = -1;
            // already a bucket value
            if (bucket >= 0)
            {
                next = bucket;
                if (_entries[next].Hashcode == hashcode)
                {
                    throw new Exception("Duplicate");
                }
            }
            
            // if (current.Data.Value != null)
            // {
            //     throw new Exception();
            // }
            current.Data = new KeyValuePair<K, V>(key, value);
            current.Next = next;
            current.Hashcode = hashcode;
            bucket = index;
        }
        public bool Remove(K key)
        {
            int hashcode = key.GetHashCode();
            ref int bucket = ref _buckets[GetIndex(hashcode)];
            
            if (bucket == -1) { return false; }
            
            int index = bucket;
            int lastIndex = -1;
            ref Entry current = ref _entries[index];
            while (current.Hashcode != hashcode && current.Data.Value != null && current.Next >= 0)
            {
                lastIndex = index;
                index = current.Next;
                current = ref _entries[index];
            }
            
            // already been removed
            if (current.Hashcode != hashcode || current.Data.Value == null)
            {
                return false;
            }
            
            if (lastIndex >= 0)
            {
                _entries[lastIndex].Next = current.Next;
            }
            // this is bucket entry
            else
            {
                bucket = current.Next;
            }
            
            current.Data = new KeyValuePair<K, V>();
            current.Hashcode = 0;
            _count--;
            // free list contains locations after index, so this is then ignored.
            // no need to increase _freeCount if at last 
            if (index == _last)
            {
                int i = _last - 1;
                while (_entries[i].Data.Value == null)
                {
                    i--;
                }
                _last = i;
            }
            
            current.Next = _freeStart;
            _freeStart = index;
            _freeCount++;
            return true;
        }
        public bool TryGetValue(K key, out V value)
        {
            int hashcode = key.GetHashCode();
            int bucket = _buckets[GetIndex(hashcode)];
            
            if (bucket == -1)
            {
                value = null;
                return false;
            }
            
            Entry current = _entries[bucket];
            while (current.Hashcode != hashcode && current.Data.Value != null && current.Next >= 0)
            {
                current = _entries[current.Next];
            }
            
            if (current.Hashcode != hashcode)
            {
                value = null;
                return false;
            }
            
            value = current.Data.Value;
            return true;
        }
        public void Clear() => Array.Fill(_buckets, -1);
        
        // fix local store of _entries
        public void Iterate(Action<KeyValuePair<K, V>> act, int threads)
        {
            int range = _last + 1;
            int baseSize = range / threads;
            int extras = range % threads;
            
            Task[] tasks = new Task[threads];
            
            int first = 0;
            int current = 0;
            for (int i = 0; i < threads; i++)
            {
                int length = baseSize;
                if (extras > 0)
                {
                    length++;
                    extras--;
                }
                if (i == 0)
                {
                    first = length;
                    current = length;
                    continue;
                }
                
                int c = current;
                int l = length + c;
                current = l;
                tasks[i] = Task.Run(() =>
                {
                    for (int j = c; j < l; j++)
                    {
                        KeyValuePair<K, V> k = _entries[j].Data;
                        if (k.Value == null) { continue; }
                        act(k);
                    }
                });
            }
            
            for (int j = 0; j < first; j++)
            {
                KeyValuePair<K, V> k = _entries[j].Data;
                if (k.Value == null) { continue; }
                act(k);
            }
            
            for (int i = 1; i < threads; i++)
            {
                tasks[i].Wait();
            }
        }
    }
}
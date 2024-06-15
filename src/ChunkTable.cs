using System;
using System.Runtime.CompilerServices;
using Zene.Structs;

namespace cgl
{
    public class ChunkTable
    {
        public ChunkTable()
        {
            int size = HashHelpers.GetPrime(0);
            int[] buckets = new int[size];
            Entry[] entries = new Entry[size];
 
            // Assign member variables after both arrays allocated to guard against corruption from OOM if second fails
            _freeList = -1;
            _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)size);
            
            _locations = buckets;
            _entries = entries;
        }
        
        private struct Entry
        {
            public uint hashCode;
            public IChunk chunk;
            public int next;
        }
        
        private int _count;
        private int _freeList;
        private int _freeCount;
        private ulong _fastModMultiplier;
        
        private int _capacity;
        private int[] _locations;
        private Entry[] _entries;
        
        public int Count => _count - _freeCount;
        
        public bool Add(IChunk chunk)
        {
            // NOTE: this method is mirrored in CollectionsMarshal.GetValueRefOrAddDefault below.
            // If you make any changes here, make sure to keep that version in sync as well.
            
            Vector2I key = chunk.Location;
            Entry[] entries = _entries;
            uint hashCode = (uint)key.GetHashCode();
 
            uint collisionCount = 0;
            ref int bucket = ref GetBucket(hashCode);
            int i = bucket; // Value in _buckets is 1-based
            
            
            while ((uint)i < (uint)entries.Length)
            {
                if (entries[i].chunk?.Location == key)
                {
                    throw new Exception();
                }

                i = entries[i].next;

                collisionCount++;
                if (collisionCount > (uint)entries.Length)
                {
                    // The chain of entries forms a loop; which means a concurrent update has happened.
                    // Break out of the loop and throw, rather than looping forever.
                    throw new Exception("some issue");
                }
            }
 
            int index;
            if (_freeCount > 0)
            {
                index = _freeList;
                //Debug.Assert((StartOfFreeList - entries[_freeList].next) >= -1, "shouldn't overflow because `next` cannot underflow");
                _freeList = -3 - entries[_freeList].next;
                _freeCount--;
            }
            else
            {
                int count = _count;
                if (count == entries.Length)
                {
                    Resize();
                    bucket = ref GetBucket(hashCode);
                }
                index = count;
                _count = count + 1;
                entries = _entries;
            }
 
            ref Entry entry = ref entries[index];
            entry.hashCode = hashCode;
            entry.next = bucket; // Value in _buckets is 1-based
            //entry.key = key;
            entry.chunk = chunk;
            bucket = index; // Value in _buckets is 1-based
            //_version++;
 
            // Value types never rehash
            if (collisionCount > HashHelpers.HashCollisionThreshold)
            {
                // If we hit the collision threshold we'll need to switch to the comparer which is using randomized string hashing
                // i.e. EqualityComparer<string>.Default.
                Resize(entries.Length);
            }
 
            return true;
        }
        
        private void Resize() => Resize(HashHelpers.ExpandPrime(_count));
 
        private void Resize(int newSize)
        {
            Entry[] entries = new Entry[newSize];
 
            int count = _count;
            Array.Copy(_entries, entries, count);
 
            // Assign member variables after both arrays allocated to guard against corruption from OOM if second fails
            _locations = new int[newSize];
            _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)newSize);
            
            for (int i = 0; i < count; i++)
            {
                if (entries[i].next >= -1)
                {
                    ref int bucket = ref GetBucket(entries[i].hashCode);
                    entries[i].next = bucket; // Value in _buckets is 1-based
                    bucket = i;
                }
            }
 
            _entries = entries;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref int GetBucket(uint hashCode)
        {
            int[] buckets = _locations;
            return ref buckets[HashHelpers.FastMod(hashCode, (uint)buckets.Length, _fastModMultiplier)];
        }
        
        public bool Remove(Vector2I key)
        {
            //Debug.Assert(_entries != null, "entries should be non-null");
            uint collisionCount = 0;
            uint hashCode = (uint)key.GetHashCode();

            ref int bucket = ref GetBucket(hashCode);
            Entry[] entries = _entries;
            int last = -1;
            int i = bucket; // Value in buckets is 1-based
            while (i >= 0)
            {
                ref Entry entry = ref entries[i];

                if (entry.chunk?.Location == key)
                {
                    if (last < 0)
                    {
                        bucket = entry.next; // Value in buckets is 1-based
                    }
                    else
                    {
                        entries[last].next = entry.next;
                    }

                    //Debug.Assert((StartOfFreeList - _freeList) < 0, "shouldn't underflow because max hashtable length is MaxPrimeArrayLength = 0x7FEFFFFD(2146435069) _freelist underflow threshold 2147483646");
                    entry.next = -3 - _freeList;

                    entry.chunk = null;
                    entry.hashCode = 0;

                    _freeList = i;
                    _freeCount++;
                    return true;
                }

                last = i;
                i = entry.next;

                collisionCount++;
                if (collisionCount > (uint)entries.Length)
                {
                    // The chain of entries forms a loop; which means a concurrent update has happened.
                    // Break out of the loop and throw, rather than looping forever.
                    throw new Exception("some issue");
                }
            }
            return false;
        }
        
        public bool GetValue(Vector2I key, out IChunk value)
        {
            ref Entry entry = ref Unsafe.NullRef<Entry>();
            //Debug.Assert(comparer is not null);
            uint hashCode = (uint)key.GetHashCode();
            int i = GetBucket(hashCode);
            Entry[] entries = _entries;
            uint collisionCount = 0;
            //i--; // Value in _buckets is 1-based; subtract 1 from i. We do it here so it fuses with the following conditional.
            do
            {
                // Test in if to drop range check for following array access
                if ((uint)i >= (uint)entries.Length)
                {
                    value = null;
                    return false;
                }

                entry = ref entries[i];
                if (entry.chunk?.Location == key)
                {
                    value = entry.chunk;
                    return true;
                }

                i = entry.next;

                collisionCount++;
            } while (collisionCount <= (uint)entries.Length);

            value = null;
            return false;
        }
        
        public void Iterate(Action<IChunk> action)
        {
            Span<Entry> span = _entries;
            for (int i = 0; i < span.Length; i++)
            {
                Entry e = span[i];
                if (e.chunk == null) { continue; }
                
                action(e.chunk);
            }
        }
    }
}
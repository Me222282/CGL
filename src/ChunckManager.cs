using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Zene.Structs;

namespace cgl
{
    public class ChunkManager
    {
        private class CK
        {
            public CK(IChunk ck, bool parse)
            {
                c = ck;
                it = parse;
            }
            
            public IChunk c;
            public bool it;
            public int delC;
            public void Done() => it = false;
        }
        
        public ChunkManager(Vector2I size)
        {   
            ChunkSize = size;
            _chunks = new ConcurrentDictionary<Vector2I, CK>();
            // _chunks = new HashTable<Vector2I, CK>(50);
        }
        
        public Vector2I ChunkSize { get; }
        public int NumChunks => _chunks.Count;
        public bool ApplingRules => _inIteration;
        
        private ConcurrentDictionary<Vector2I, CK> _chunks;
        // private HashTable<Vector2I, CK> _chunks;
        
        private bool _inIteration = false;
        public void ApplyRules()
        {
            _inIteration = true;
            Parallel.ForEach(_chunks, kvp =>
            {
                if (kvp.Value.it) { return; }
                IChunk c = kvp.Value.c;
                c.CalculateRules(this);
            });
            // foreach (KeyValuePair<Vector2I, CK> kvp in _chunks)
            // {
            //     if (kvp.Value.it) { continue; }
            //     IChunk c = kvp.Value.c;
            //     c.CalculateRules(this);
            // }
            _inIteration = false;
            Parallel.ForEach(_chunks, kvp =>
            {
                IChunk c = kvp.Value.c;
                if (c.ShouldDelete())
                {
                    kvp.Value.delC++;
                    if (kvp.Value.delC > 3) { goto Apply; }
                    c.InUse = false;
                    _chunks.TryRemove(kvp);
                    return;
                }
                kvp.Value.delC = 0;
            Apply:
                kvp.Value.Done();
                c.ApplyFrame();
            });
            // foreach (KeyValuePair<Vector2I, CK> kvp in _chunks)
            // {
            //     IChunk c = kvp.Value.c;
            //     if (c.ShouldDelete())
            //     {
            //         kvp.Value.delC++;
            //         if (kvp.Value.delC > 3) { goto Apply; }
            //         c.InUse = false;
            //         _chunks.TryRemove(kvp);
            //         continue;
            //     }
            //     kvp.Value.delC = 0;
            // Apply:
            //     kvp.Value.Done();
            //     c.ApplyFrame();
            // }
            // _inIteration = true;
            // _chunks.Iterate(kvp =>
            // {
            //     if (kvp.Value.it) { return; }
            //     IChunk c = kvp.Value.c;
            //     c.CalculateRules(this);
            // }, 1);
            // _inIteration = false;
            // _chunks.Iterate(kvp =>
            // {
            //     IChunk c = kvp.Value.c;
            //     if (c.ShouldDelete())
            //     {
            //         kvp.Value.delC++;
            //         if (kvp.Value.delC > 3) { goto Apply; }
            //         c.InUse = false;
            //         _chunks.TryRemove(kvp.Key);
            //         return;
            //     }
            //     kvp.Value.delC = 0;
            // Apply:
            //     kvp.Value.Done();
            //     c.ApplyFrame();
            // }, 1);
        }
        public IChunk AddChunk(Vector2I location)
        {
            IChunk c = new Chunk(ChunkSize, this, location);
            c.InUse = true;
            //c.Location = location;
            
            _chunks.TryAdd(location, new CK(c, _inIteration));
            // if (!sess)
            // {
            //     throw new Exception();
            // }
            return c;
        }
        public IChunk GetChunkRead(Vector2I location)
        {
            bool found = _chunks.TryGetValue(location, out CK c);
            if (found)
            {
                return c.c;
            }
            
            return new Empty();
        }
        public IChunk GetChunkWrite(Vector2I location)
        {
            bool found = _chunks.TryGetValue(location, out CK c);
            if (found) { return c.c; }
            
            return AddChunk(location);
        }
        
        public void PushCell(Vector2I location, byte v)
        {
            (Vector2I ch, Vector2I pos) = GetChunkPos(location);
            
            IChunk c = GetChunkWrite(ch);
            c.PushCell(pos.X, ChunkSize.Y - pos.Y - 1, v);
        }
        public (Vector2I, Vector2I) GetChunkPos(Vector2I location)
        {
            Vector2I ch = location / ChunkSize;
            
            Vector2I pos = (location.X % ChunkSize.X, location.Y % ChunkSize.Y);
            if (pos.X < 0)
            {
                pos.X = ChunkSize.X + pos.X;
                ch.X -= 1;
            }
            if (pos.Y < 0)
            {
                pos.Y = ChunkSize.Y + pos.Y;
                ch.Y -= 1;
            }
            
            return (ch, pos);
        }
        public void Iterate(Action<Vector2I, IChunk> action)
        {
            // _chunks.Iterate(kvp => action(kvp.Key, kvp.Value.c), 1);
            foreach (KeyValuePair<Vector2I, CK> kvp in _chunks)
            {
                action(kvp.Key, kvp.Value.c);
            }
        }
        public void Clear() => _chunks.Clear();
    }
}
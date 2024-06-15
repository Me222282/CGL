using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
            //Chunks = new ChunkTable();
        }
        
        public Vector2I ChunkSize { get; }
        public int NumChunks => _chunks.Count;
        
        private ConcurrentDictionary<Vector2I, CK> _chunks;
        //public ChunkTable Chunks { get; }
        
        private bool _inIteration = false;
        public void ApplyRules()
        {
            _inIteration = true;
            foreach (KeyValuePair<Vector2I, CK> kvp in _chunks)
            {
                if (kvp.Value.it) { continue; }
                IChunk c = kvp.Value.c;
                c.CalculateRules(this);
                if (c.ShouldDelete())
                {
                    kvp.Value.delC++;
                    if (kvp.Value.delC <= 3) { continue; }
                    c.InUse = false;
                    _chunks.TryRemove(kvp);
                    continue;
                }
                kvp.Value.delC = 0;
            }
            _inIteration = false;
            foreach (KeyValuePair<Vector2I, CK> kvp in _chunks)
            {
                kvp.Value.Done();
                kvp.Value.c.ApplyFrame();
            }
            // Chunks.Iterate(c =>
            // {
            //     c.CalculateRules(this);
            //     if (c.ShouldDelete())
            //     {
            //         c.InUse = false;
            //         Chunks.Remove(c.Location);
            //     }
            // });
            // Chunks.Iterate(c =>
            // {
            //     c.ApplyFrame();
            // });
        }
        public IChunk AddChunk(Vector2I location)
        {
            IChunk c = new Chunk(ChunkSize, this, location);
            c.InUse = true;
            //c.Location = location;
            
            bool sess = _chunks.TryAdd(location, new CK(c, _inIteration));
            if (!sess)
            {
                throw new Exception();
            }
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
            
            IChunk c = GetChunkWrite(ch);
            c.PushCell(pos.X, ChunkSize.Y - pos.Y - 1, v);
        }
        public void Iterate(Action<Vector2I, IChunk> action)
        {
            foreach (KeyValuePair<Vector2I, CK> kvp in _chunks)
            {
                action(kvp.Key, kvp.Value.c);
            }
        }
    }
}
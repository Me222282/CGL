using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Zene.Structs;

namespace cgl
{
    public class ChunkManager
    {
        public ChunkManager(Vector2I size)
        {
            ChunkSize = size;
            Chunks = new ConcurrentDictionary<Vector2I, IChunk>();
        }
        
        public Vector2I ChunkSize { get; }
        
        public ConcurrentDictionary<Vector2I, IChunk> Chunks { get; }
        
        public void ApplyRules()
        {
            foreach (KeyValuePair<Vector2I, IChunk> kvp in Chunks)
            {
                kvp.Value.ApplyRules(kvp.Key, this);
                if (kvp.Value.ShouldDelete())
                {
                    Chunks.TryRemove(kvp);
                    kvp.Value.InUse = false;
                }
            }
        }
        public IChunk AddChunk(Vector2I location)
        {
            IChunk c = new Chunk(ChunkSize, this);
            c.InUse = true;
            
            Chunks.TryAdd(location, c);
            return c;
        }
        public IChunk GetChunkRead(Vector2I location)
        {
            bool found = Chunks.TryGetValue(location, out IChunk c);
            if (found)
            {
                return c;
            }
            
            return new Empty();
        }
        public IChunk GetChunkWrite(Vector2I location)
        {
            bool found = Chunks.TryGetValue(location, out IChunk c);
            if (found) { return c; }
            
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
    }
}
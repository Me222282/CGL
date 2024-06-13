using System.Collections.Generic;
using Zene.Structs;

namespace cgl
{
    public class ChunkManager
    {
        public ChunkManager(Vector2I size)
        {
            ChunkSize = size;
        }
        
        public Vector2I ChunkSize { get; }
        
        public Dictionary<Vector2I, IChunk> Chunks { get; }
        
        public void ApplyRules()
        {
            foreach (KeyValuePair<Vector2I, IChunk> kvp in Chunks)
            {
                kvp.Value.ApplyRules(kvp.Key, this);
                if (kvp.Value.ShouldDelete())
                {
                    Chunks.Remove(kvp.Key);
                    kvp.Value.InUse = false;
                }
            }
        }
        public IChunk AddChunk(Vector2I location)
        {
            IChunk c = new Chunk(ChunkSize);
            c.InUse = true;
            
            Chunks.Add(location, c);
            return c;
        }
        public IChunk GetChunkRead(Vector2I location)
        {
            bool found = Chunks.TryGetValue(location, out IChunk c);
            if (found) { return c; }
            
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
            
            IChunk c = GetChunkWrite(ch);
            c.PushCell(location.X % ChunkSize.X,
                location.Y % ChunkSize.Y, v);
        }
    }
}
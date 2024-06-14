using System;
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
            Chunks = new Dictionary<Vector2I, IChunk>();
        }
        
        public Vector2I ChunkSize { get; }
        
        public Dictionary<Vector2I, IChunk> Chunks { get; }
        private List<KeyValuePair<Vector2I, IChunk>> _deletes = new List<KeyValuePair<Vector2I, IChunk>>(8);
        
        public void ApplyRules()
        {
            foreach (KeyValuePair<Vector2I, IChunk> kvp in Chunks)
            {
                kvp.Value.ApplyRules(kvp.Key, this);
                _deletes.Add(kvp);
            }
            
            Span<KeyValuePair<Vector2I, IChunk>> span = CollectionsMarshal.AsSpan(_deletes);
            for (int i = 0; i < span.Length; i++)
            {
                KeyValuePair<Vector2I, IChunk> kvp = span[i];
                Chunks.Remove(kvp.Key);
                kvp.Value.InUse = false;
            }
        }
        public IChunk AddChunk(Vector2I location)
        {
            IChunk c = new Chunk(ChunkSize, this);
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
            
            Vector2I pos = (location.X % ChunkSize.X, location.Y % ChunkSize.Y);
            IChunk c = GetChunkWrite(ch);
            c.PushCell(pos.X, pos.Y, v);
            
            //WriteAround(c, ch, pos.X, pos.Y);
        }
        // private void Write(IChunk c, Vector2I ch, int x, int y)
        // {
        //     bool lx = x < 0;
        //     bool ly = y < 0;
        //     bool gx = x >= ChunkSize.X;
        //     bool gy = y >= ChunkSize.Y;
            
        //     if (lx == ly && lx == gx && lx == gy)
        //     {
        //         c.AddCheck(x, y);
        //         return;
        //     }
            
        //     if (lx)
        //     {
        //         if (ly)
        //         {
        //             GetChunkWrite(ch - Vector2I.One)
        //                 .AddCheck(ChunkSize.X - 1, ChunkSize.Y - 1);
        //             return;
        //         }
        //         if (gy)
        //         {
        //             GetChunkWrite(ch + (-1, 1))
        //                 .AddCheck(ChunkSize.X - 1, 0);
        //             return;
        //         }
                
        //         GetChunkWrite(ch - (1, 0))
        //             .AddCheck(ChunkSize.X - 1, y);
        //         return;
        //     }
        //     if (ly)
        //     {
        //         if (gx)
        //         {
        //             GetChunkWrite(ch + (1, -1))
        //                 .AddCheck(0, ChunkSize.Y - 1);
        //             return;
        //         }
                
        //         GetChunkWrite(ch - (0, 1))
        //             .AddCheck(x, ChunkSize.Y - 1);
        //         return;
        //     }
        //     if (gx)
        //     {
        //         if (gy)
        //         {
        //             GetChunkWrite(ch + Vector2I.One)
        //                 .AddCheck(0, 0);
        //             return;
        //         }
                
        //         GetChunkWrite(ch + (1, 0))
        //             .AddCheck(0, y);
        //         return;
        //     }
            
        //     // gy
        //     GetChunkWrite(ch + (0, 1))
        //         .AddCheck(x, 0);
        //     return;
        // }
        // private void WriteAround(IChunk c, Vector2I ch, int x, int y)
        // {
        //     Write(c, ch, x + 1, y);
        //     Write(c, ch, x - 1, y);
        //     Write(c, ch, x + 1, y + 1);
        //     Write(c, ch, x - 1, y + 1);
        //     Write(c, ch, x + 1, y - 1);
        //     Write(c, ch, x - 1, y - 1);
        //     Write(c, ch, x, y + 1);
        //     Write(c, ch, x, y - 1);
        // }
    }
}
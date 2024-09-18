using System.Collections.Generic;
using Zene.Graphics;
using Zene.Structs;

namespace cgl
{
    public interface IChunk
    {
        public int this[int x, int y] { get; }
        public bool InUse { get; set; }
        public Vector2I Location { get;  set; }
        
        public void AddCheck(int x, int y);
        public void CalculateRules(ChunkManager cm);
        public void ApplyFrame();
        public bool ShouldDelete();
        public bool IsSignificant();
        
        public void PushCell(int x, int y, byte v);
        public void WriteToTexture(Vector2I location, ITexture texture, ChunkManager cm);
    }
}
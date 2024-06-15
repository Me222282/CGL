using System;
using Zene.Graphics;
using Zene.Graphics.Base.Extensions;
using Zene.Structs;

namespace cgl
{
    public class Empty : IChunk
    {
        public int this[int x, int y] => 0;
        public bool InUse { get; set; } = false;
        
        public void AddCheck(int x, int y) { }
        public void CalculateRules(Vector2I location, ChunkManager cm) { }
        public void ApplyFrame() { }
        public void PushCell(int x, int y, byte v) { }

        public bool ShouldDelete() => true;
        public void WriteToTexture(Vector2I location, ITexture texture, ChunkManager cm)
        {
            texture.TexSubImage2D(0,
                location.X, location.Y, cm.ChunkSize.X, cm.ChunkSize.Y,
                BaseFormat.R, TextureData.Byte, IntPtr.Zero);
        }
    }
}
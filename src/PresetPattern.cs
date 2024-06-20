using System;
using System.Collections.Generic;
using Zene.Graphics;
using Zene.Structs;

namespace cgl
{
    public class PresetPattern : IPlaceMode
    {
        public PresetPattern(Vector2I[] c)
        {
            Points = c;
        }
        public PresetPattern(Bitmap image)
        {
            List<Vector2I> cells = new List<Vector2I>(image.Width * image.Height);
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Vector3 c = image[x, y].ToHsl();
                    if (c.Z >= 0.5)
                    {
                        cells.Add((x, y));
                    }
                }
            }
            Points = cells.ToArray();
        }
        
        public Vector2I[] Points { get; }
        
        public bool PushAlive { get; set; }
        public bool Brush => false;
        
        public void Place(ChunkManager cm, Vector2I location)
        {
            byte v = (byte)(PushAlive ? 1 : 0);
            Span<Vector2I> span = Points;
            for (int i = 0; i < span.Length; i++)
            {
                cm.PushCell(span[i] + location, v);
            }
        }
    }
}
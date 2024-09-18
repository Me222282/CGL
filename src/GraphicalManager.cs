using System;
using Zene.Graphics;
using Zene.Structs;

namespace cgl
{
    public class GraphicalManager
    {
        public GraphicalManager(int width, int height, IDrawingContext dc)
        {
            Context = dc;
            
            _texture = new Texture2D(TextureFormat.R8, TextureData.Byte);
            _texture.SetData<byte>(width, height, BaseFormat.R, null);
            _texture.MagFilter = TextureSampling.Nearest;
            _texture.MinFilter = TextureSampling.Nearest;
            _texture.WrapStyle = WrapStyle.EdgeClamp;
            
            _clear = new Framebuffer();
            _clear[0] = _texture;
            
            _shad = new BoolShader();
            _text = new TextRenderer();
            
            dc.RenderState.Blending = true;
            dc.RenderState.SourceScaleBlending = BlendFunction.SourceAlpha;
            dc.RenderState.DestinationScaleBlending = BlendFunction.OneMinusSourceAlpha;
        }
        
        private BoolShader _shad;
        private Texture2D _texture;
        private Framebuffer _clear;
        private TextRenderer _text;
        private Vector2 _drawOffset = 0d;
        
        public IDrawingContext Context { get; set; }
        public bool SeeChunks { get; set; }
        public bool ChunkNumbers { get; set; }
        public double Scale { get; set; } = 5d;
        public Vector2 Pan { get; set; } = Vector2.Zero;
        
        private RectangleI _visableChunks;
        public Vector2I HoverPoint { get; set; }
        public bool ShowHover { get; set; } = false;
        
        public RectangleI Highlight { get; set; }
        //public bool ShowHighlight { get; set; } = false;
        
        public void Render(ChunkManager cm, Vector2I screen)
        {
            IDrawingContext dc = Context;
            
            dc.Framebuffer.Clear(BufferBit.Colour);
            dc.Shader = _shad;
            
            GenerateTexture(cm, screen);
            
            // dc.View = Matrix.Identity;
            // dc.Model = Matrix4.CreateScale(15d);
            // _text.Colour = ColourF.Pink;
            // _text.DrawCentred(e.Context, _pi.ToString(), Shapes.SampleFont, 0, 0);
            
            // e.Context.View = Matrix4.CreateTranslation(_pan) * Matrix4.CreateScale(_scale);
            dc.View = Matrix4.CreateScale(Scale);
            Draw(dc, new Box(_drawOffset, (_texture.Width, _texture.Height)));
            
            if (ShowHover)
            {
                dc.Model = null;
                Vector2 pos = GetScreenPos(cm, HoverPoint) + 0.5;
                ColourF colour = ColourF.Orange;
                colour.A = 0.5f;
                dc.DrawBorderBox(new Box(pos + _drawOffset, 1d), colour,
                    2 / Scale, ColourF.DarkOrange);
            }
            if (Highlight.Size != Vector2I.Zero)
            {
                dc.Model = null;
                Vector2 size = Highlight.Size;
                Vector2 pos = GetScreenPos(cm, Highlight.Location) + (size.X / 2d, size.Y / -2d);
                ColourF colour = ColourF.LightBlue;
                colour.A = 0.5f;
                dc.DrawBorderBox(new Box(pos + _drawOffset, size), colour,
                    2 / Scale, ColourF.Blue);
            }
        }
        private Vector2 GetScreenPos(ChunkManager cm, Vector2I worldPos)
        {
            // if (worldPos.X < 0)
            // {
            //     worldPos.X--;
            // }
            // if (worldPos.Y < 0)
            // {
            //     worldPos.Y--;
            // }
            
            return worldPos + ((_visableChunks.Location) * cm.ChunkSize) -
                (_texture.Width / 2, _texture.Height / 2);
        }
        
        private void Draw(IDrawingContext dc, IBox bounds)
        {
            dc.Shader = _shad;
            _shad.Texture = _texture;

            dc.Model = Matrix4.CreateBox(bounds);
            dc.Draw(Shapes.Square);
        }
        
        private void GenerateTexture(ChunkManager cm, Vector2I screen)
        {
            Vector2 of = Pan / cm.ChunkSize;
            Vector2I offset = ((int)Math.Round(of.X), (int)Math.Round(of.Y));
            
            _drawOffset = Pan - (offset * cm.ChunkSize);
            
            Vector2 tmp = (screen / Scale) / cm.ChunkSize;
            Vector2I chunking = (Vector2I)tmp + 2;
            offset += chunking / 2;
            _visableChunks.Location = offset;
            _visableChunks.Size = chunking;
            Vector2I size = cm.ChunkSize * chunking;
            if (_texture.Width != size.X || _texture.Height != size.Y)
            {
                _texture.SetData<byte>(size.X, size.Y, BaseFormat.R, null);
            }
            _clear.Clear(BufferBit.Colour);
            
            // Keep 0,0 in centre (when theres no panning)
            if (chunking.X % 2 == 1) { _drawOffset.X += cm.ChunkSize.X / 2d; }
            if (chunking.Y % 2 == 1) { _drawOffset.Y += cm.ChunkSize.Y / 2d; }
            
            if ((chunking.X * chunking.Y) >= (cm.NumChunks * 2))
            {
                // Iterate through all chunks
                cm.Iterate((k, c) =>
                {
                    Vector2I pos = c.Location + offset;
                    // Ignore outsiders
                    if (pos.X < 0 || pos.Y < 0 || pos.X >= chunking.X || pos.Y >= chunking.Y) { return; }
                    DrawChunk(cm, c, pos, chunking);
                });
                return;
            }
            
            // Iterate through screen chunk grid
            for (int x = 0; x < chunking.X; x++)
            {
                for (int y = 0; y < chunking.Y; y++)
                {
                    Vector2I pos = (x, y);
                    IChunk ic = cm.GetChunkRead(pos - offset);
                    if (ic is Empty) { continue; }
                    DrawChunk(cm, ic, pos, chunking);
                }
            }
        }
        private void DrawChunk(ChunkManager cm, IChunk c, Vector2I pos, Vector2I chunking)
        {
            c.WriteToTexture(pos * cm.ChunkSize, _texture, cm);
            if (!SeeChunks) { return; }
            
            Vector2 sertg = chunking / 2d;
            Context.View = Matrix4.CreateTranslation((cm.ChunkSize * (-sertg + pos + (0.5, 0.5)) + _drawOffset) * Scale);
            Context.Model = Matrix.Identity;
            Context.DrawBorderBox(new Box(0d, cm.ChunkSize * Scale), ColourF.Zero, 2, ColourF.LightGrey);
            if (!ChunkNumbers) { return; }
            Context.Model = Matrix4.CreateScale(10d);
            _text.Colour = ColourF.Blue;
            _text.DrawCentred(Context, c.Location.ToString(), Shapes.SampleFont, 0, 0);
        }
    }
}
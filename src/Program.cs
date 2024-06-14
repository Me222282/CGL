using System;
using Zene.Graphics;
using Zene.Graphics.Base.Extensions;
using Zene.Structs;
using Zene.Windowing;

namespace cgl
{
    class Program : Window
    {
        static void Main(string[] args)
        {
            Core.Init();
            
            Program p = new Program(800, 500, "erthj");
            p.Run();
            p.Dispose();
            
            Core.Terminate();
        }
        
        public Program(int width, int height, string title)
            : base(width, height, title)
        {
            _cm = new ChunkManager((12, 12));
            
            _texture = new Texture2D(TextureFormat.R8, TextureData.Byte);
            _texture.SetData<byte>(width, height, BaseFormat.R, null);
            _texture.MagFilter = TextureSampling.Nearest;
            _texture.MinFilter = TextureSampling.Nearest;
            _texture.WrapStyle = WrapStyle.EdgeClamp;
            
            _shad = new BoolShader();
            _text = new TextRenderer();
            
            DrawContext.RenderState.Blending = true;
            DrawContext.RenderState.SourceScaleBlending = BlendFunction.SourceAlpha;
            DrawContext.RenderState.DestinationScaleBlending = BlendFunction.OneMinusSourceAlpha;
        }
        
        private BoolShader _shad;
        private Texture2D _texture;
        private TextRenderer _text;
        private Vector2 _drawOffset = 0d;
        
        private ChunkManager _cm;
        private bool _applied = false;
        private bool _palying  = false;
        private bool _enter = false;
        
        private double _scale = 10d;
        private Vector2 _pan = 0d;
        private Vector2 _mp;
        private Vector2I _pi;
        
        private bool Division(double t) => Timer % t >= (t / 2d);
        
        protected override void OnUpdate(FrameEventArgs e)
        {
            base.OnUpdate(e);
            
            e.Context.Framebuffer.Clear(BufferBit.Colour);
            e.Context.Shader = _shad;
            
            if (_enter)
            {
                _cm.ApplyRules();
                //GenerateTexture();
                _enter = false;
            }
            
            if (!_palying) { goto Ignore; }
            
            if (!_applied && Division(0.1))
            {
                _cm.ApplyRules();
                //GenerateTexture();
                _applied = true;
            }
            else if (!Division(0.1))
            {
                _applied = false;
            }
            
            
            Ignore:
            GenerateTexture();
            
            e.Context.View = Matrix.Identity;
            e.Context.Model = Matrix4.CreateScale(10d);
            _text.DrawCentred(e.Context, _pi.ToString(), Shapes.SampleFont, 0, 0);
            
            e.Context.Projection = Matrix4.CreateOrthographic(Width, Height, 0d, 1d) * Matrix4.CreateScale(0.9);
            e.Context.View = Matrix4.CreateTranslation(_pan) * Matrix4.CreateScale(_scale);
            e.Context.Model = Matrix.Identity;
            e.Context.DrawBox(new Box(0d, 20d), ColourF.Orange);
            e.Context.View = Matrix4.CreateScale(_scale);
            Draw(e.Context, new Box(_drawOffset, (_texture.Width, _texture.Height)));
            e.Context.View = Matrix.Identity;
            e.Context.Model = Matrix.Identity;
            e.Context.DrawBorderBox(new Box(0d, Size), ColourF.Zero, 5d, ColourF.Grey);
        }
        
        private void Draw(IDrawingContext dc, IBox bounds)
        {
            dc.Shader = _shad;
            _shad.Texture = _texture;

            dc.Model = Matrix4.CreateBox(bounds);
            dc.Draw(Shapes.Square);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            
            Vector2 size = Size / _scale;
            
            Vector2 pos = ((_mp / _scale) - _pan) + (size / 2d);
            Vector2I pi = (Vector2I)pos;
            _pi = pi;
            //pi.Y = (int)size.Y - pi.Y - 1;
            
            if (pi.X < 0 || pi.Y < 0 || pi.X >= size.X || pi.Y >= size.Y) { return; }
            _cm.PushCell(pi, 1);
            // _map[pi.X, pi.Y] = (byte)(1 - _map[pi.X, pi.Y]);
            // _checkMap[pi.X, pi.Y] = true;
            // WriteAround(_checkMap, pi.X, pi.Y);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            _mp = e.Location - (Size / 2d);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            
            if (e[Keys.Space])
            {
                _palying = !_palying;
                return;
            }
            if (e[Keys.Enter])
            {
                _enter = true;
                return;
            }
            if (e[Keys.Equal])
            {
                _scale *= 1.1;
                return;
            }
            if (e[Keys.Minus])
            {
                _scale /= 1.1;
                return;
            }
        }
        protected override void OnScroll(ScrollEventArgs e)
        {
            base.OnScroll(e);
            
            if (this[Mods.Control])
            {
                double newZoom = _scale + (e.DeltaY * 0.03 * _scale);

                if (newZoom < 0) { return; }

                double oldZoom = _scale;
                _scale = newZoom;
                
                // Vector2 v = (_mp - _pan);
                // Vector2 pointRelOld = v / oldZoom;
                // Vector2 pointRelNew = v / newZoom;

                // _pan += (pointRelNew - pointRelOld) * newZoom;
                Vector2 pointRelOld = (_mp / oldZoom) - _pan;
                Vector2 pointRelNew = (_mp / newZoom) - _pan;
                _pan += pointRelNew - pointRelOld;
                return;
            }
            if (this[Mods.Shift])
            {
                _pan += new Vector2(-e.DeltaY, e.DeltaX) * 5d / _scale;
                return;
            }
            
            _pan += new Vector2(e.DeltaX, -e.DeltaY) * 5d / _scale;
        }
        
        private GLArray<byte> _data = new GLArray<byte>(12, 12);
        private void GenerateTexture()
        {
            Array.Fill<byte>(_data.Data, 1);
            Vector2 of = _pan / _cm.ChunkSize;
            Vector2I offset = ((int)Math.Round(of.X), (int)Math.Round(of.Y));
            
            _drawOffset = _pan - (offset * _cm.ChunkSize);
            
            Vector2 tmp = (Size / _scale) / _cm.ChunkSize;
            Vector2I chunking = (Vector2I)tmp + 2;//((int)Math.Ceiling(tm.X), (int)Math.Ceiling(tm.Y));
            Vector2I size = _cm.ChunkSize * chunking;
            _texture.SetData<byte>(size.X, size.Y, BaseFormat.R, null);
            
            for (int x = 0; x < chunking.X; x++)
            {
                for (int y = 0; y < chunking.Y; y++)
                {
                    Vector2I pos = (x, y);
                    //_cm.GetChunkRead(pos + offset).WriteToTexture(pos * _cm.ChunkSize, _texture, _cm);
                    
                    GLArray<byte> gla = _data;
                    if ((x + y) % 2 == 1) { gla = null; }
                    
                    Vector2I location = pos * _cm.ChunkSize;
                    _texture.TexSubImage2D(0,
                        location.X, location.Y, _cm.ChunkSize.X, _cm.ChunkSize.Y,
                        BaseFormat.R, TextureData.Byte, gla);
                    
                    //if ((x + y) % 2 == 1) { continue; }
                    
                    Vector2 sertg = chunking / 2d;
                    DrawContext.View = Matrix.Identity;//Matrix4.CreateScale(_scale);
                    DrawContext.Model = Matrix4.CreateScale(10d) *
                        Matrix4.CreateTranslation((_cm.ChunkSize * (-sertg + (x + 0.5, y + 0.5)) + _drawOffset) * _scale);
                    _text.Colour = ColourF.Blue;
                    _text.DrawCentred(DrawContext, (pos - offset).ToString(), Shapes.SampleFont, 0, 0);
                    DrawContext.Shader = Shapes.BasicShader;
                    Shapes.BasicShader.ColourSource = ColourSource.UniformColour;
                    Shapes.BasicShader.Colour = ColourF.White;
                    //DrawContext.Draw(Shapes.Square);
                }
            }
        }
    }
}

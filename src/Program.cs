using System;
using Zene.Graphics;
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
            _size = (width / 2, height / 2);
            
            _map = new GLArray<bool>(_size);
            _temp = new GLArray<bool>(_size);
            
            _texture = new Texture2D(TextureFormat.R8, TextureData.Byte);
            //_texture.SetData<byte>(_size.X, _size.Y, BaseFormat.R, null);
            _texture.SetData(_size.X, _size.Y, BaseFormat.R, _map);
            _texture.MagFilter = TextureSampling.Nearest;
            _texture.MinFilter = TextureSampling.Nearest;
            _texture.WrapStyle = WrapStyle.EdgeClamp;
            
            _shad = new BoolShader();
        }
        
        private BoolShader _shad;
        private Texture2D _texture;
        
        private GLArray<bool> _map;
        private GLArray<bool> _temp;
        private Vector2I _size;
        private bool _applied = false;
        private bool _palying  = false;
        
        private double _scale = 2d;
        private Vector2 _pan = 0d;
        private Vector2 _mp;
        
        private bool Division(double t) => Timer % t >= (t / 2d);
        
        protected override void OnUpdate(FrameEventArgs e)
        {
            base.OnUpdate(e);
            
            e.Context.Framebuffer.Clear(BufferBit.Colour);
            e.Context.Shader = _shad;
            
            e.Context.Projection = Matrix4.CreateOrthographic(Width, Height, 0d, 1d);
            e.Context.View = Matrix4.CreateTranslation(_pan) * Matrix4.CreateScale(_scale);
            
            if (!_palying) { goto Ignore; }
            
            if (!_applied && Division(0.1))
            {
                ApplyRules();
                _applied = true;
            }
            else if (!Division(0.1))
            {
                _applied = false;
            }
            
            Ignore:
            _texture.EditData(0, 0, _size.X, _size.Y, BaseFormat.R, _map);
            //_texture.SetData(_size.X, _size.Y, BaseFormat.R, _map);
            Draw(e.Context, new Box(0d, Size));
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
            
            Vector2 pos = ((((_mp / _scale) - _pan) + (Size / 2d)) / Size) * _size;
            Vector2I pi = (Vector2I)pos;
            pi.Y = _size.Y - pi.Y - 1;
            
            if (pi.X < 0 || pi.Y < 0 || pi.X >= _size.X || pi.Y >= _size.Y) { return; }
            _map[pi.X, pi.Y] = !_map[pi.X, pi.Y];
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
                
                Vector2 v = (_mp - _pan);
                Vector2 pointRelOld = v / oldZoom;
                Vector2 pointRelNew = v / newZoom;

                _pan += (pointRelNew - pointRelOld) * newZoom;
                return;
            }
            if (this[Mods.Shift])
            {
                _pan += new Vector2(-e.DeltaY, e.DeltaX) * 5d;
                return;
            }
            
            _pan += new Vector2(e.DeltaX, -e.DeltaY) * 5d;
        }

        private void ApplyRules()
        {
            for (int x = 0; x < _size.X; x++)
            {
                for (int y = 0; y < _size.Y; y++)
                {
                    int n = CountNeighbours(x, y);
                    bool alive = _map[x, y];
                    if (n == 3)
                    {
                        _temp[x, y] = true;
                        continue;
                    }
                    if (!alive)
                    {
                        _temp[x, y] = false;
                        continue;
                    }
                    if (n == 2)
                    {
                        _temp[x, y] = true;
                        continue;
                    }
                    _temp[x, y] = false;
                }
            }
            
            // swap memory
            GLArray<bool> gla = _map;
            _map = _temp;
            _temp = gla;
        }
        private int CountNeighbours(int x, int y)
            => Convert.ToInt32(Get(x + 1, y)) + Convert.ToInt32(Get(x - 1, y)) +
            Convert.ToInt32(Get(x + 1, y + 1)) + Convert.ToInt32(Get(x - 1, y + 1)) +
            Convert.ToInt32(Get(x + 1, y - 1)) + Convert.ToInt32(Get(x - 1, y - 1)) +
            Convert.ToInt32(Get(x, y + 1)) + Convert.ToInt32(Get(x, y - 1));
        private bool Get(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _size.X || y >= _size.Y)
            {
                return false;
            }
            
            return _map[x, y];
        }
    }
}

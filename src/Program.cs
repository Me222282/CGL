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
            //_size = (1000, 1000);
            //_size = (20, 20);
            
            _map = new GLArray<byte>(_size);
            _temp = new GLArray<byte>(_size);
            
            _checkMap = new GLArray<bool>(_size);
            _checkTemp = new GLArray<bool>(_size);
            
            _texture = new Texture2D(TextureFormat.R8, TextureData.Byte);
            _texture.SetData<byte>(_size.X, _size.Y, BaseFormat.R, null);
            _texture.SetData(_size.X, _size.Y, BaseFormat.R, _map);
            _texture.MagFilter = TextureSampling.Nearest;
            _texture.MinFilter = TextureSampling.Nearest;
            _texture.WrapStyle = WrapStyle.EdgeClamp;
            
            _shad = new BoolShader();
            
            // DrawContext.RenderState.Blending = true;
            // DrawContext.RenderState.SourceScaleBlending = BlendFunction.SourceAlpha;
            // DrawContext.RenderState.DestinationScaleBlending = BlendFunction.OneMinusSourceAlpha;
        }
        
        private BoolShader _shad;
        private Texture2D _texture;
        
        private GLArray<byte> _map;
        private GLArray<byte> _temp;
        
        private GLArray<bool> _checkMap;
        private GLArray<bool> _checkTemp;
        private Vector2I _size;
        private bool _applied = false;
        private bool _palying  = false;
        private bool _enter = false;
        
        private double _scale = 1d;
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
            
            if (_enter)
            {
                ApplyRules();
                _enter = false;
            }
            
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
            _map[pi.X, pi.Y] = (byte)(1 - _map[pi.X, pi.Y]);
            _checkMap[pi.X, pi.Y] = true;
            WriteAround(_checkMap, pi.X, pi.Y);
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
        
        private void ApplyRules()
        {
            for (int x = 0; x < _size.X; x++)
            {
                for (int y = 0; y < _size.Y; y++)
                {
                    //bool c = _checkMap[x, y];
                    if (!_checkMap[x, y])
                    {
                        _temp[x, y] = _map[x, y];
                        continue;
                    }
                    _checkMap[x, y] = false;
                    
                    int n = CountNeighbours(x, y);
                    bool alive = _map[x, y] == 1;
                    if (n == 3)
                    {
                        _temp[x, y] = 1;
                        //_checkTemp[x, y] = true;
                        if (!alive)
                        {
                            WriteAround(_checkTemp, x, y);
                        }
                        continue;
                    }
                    if (!alive)
                    {
                        _temp[x, y] = 0;
                        continue;
                    }
                    if (n == 2)
                    {
                        _temp[x, y] = 1;
                        //_checkTemp[x, y] = true;
                        continue;
                    }
                    _temp[x, y] = 0;
                    WriteAround(_checkTemp, x, y);
                }
            }
            
            // swap memory
            GLArray<byte> gla = _map;
            _map = _temp;
            _temp = gla;
            //Array.Clear(_temp.Data, 0, _temp.Length);
            
            GLArray<bool> gla2 = _checkMap;
            _checkMap = _checkTemp;
            _checkTemp = gla2;
            //Array.Clear(_checkTemp.Data, 0, _checkTemp.Length);
        }
        private int CountNeighbours(int x, int y)
            => Get(x + 1, y) + Get(x - 1, y) +
            Get(x + 1, y + 1) + Get(x - 1, y + 1) +
            Get(x + 1, y - 1) + Get(x - 1, y - 1) +
            Get(x, y + 1) + Get(x, y - 1);
        private int Get(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _size.X || y >= _size.Y)
            {
                return 0;
            }
            
            return _map[x, y];
        }
        private static void Write(GLArray<bool> map, int x, int y)
        {
            if (x < 0 || y < 0 || x >= map.Width || y >= map.Height) { return; }
            
            map[x, y] = true;
        }
        private static void WriteAround(GLArray<bool> map, int x, int y)
        {
            Write(map, x + 1, y);
            Write(map, x - 1, y);
            Write(map, x + 1, y + 1);
            Write(map, x - 1, y + 1);
            Write(map, x + 1, y - 1);
            Write(map, x - 1, y - 1);
            Write(map, x, y + 1);
            Write(map, x, y - 1);
        }
    }
}

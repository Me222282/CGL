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
            _cm = new ChunkManager((12, 12));
            
            _gm = new GraphicalManager(width, height, DrawContext);
        }
        
        private GraphicalManager _gm;
        
        private ChunkManager _cm;
        private bool _applied = false;
        private bool _palying  = false;
        private bool _enter = false;
        private double _dt = 0.1;
        
        private Vector2 _mp;
        private Vector2I _pi;
        private bool _place = false;
        private bool _mousePan = false;
        private Vector2 _panStart;
        
        private IPlaceMode _placeMode = IPlaceMode.Default;
        
        private bool Division(double t) => Timer % t >= (t / 2d);
        
        protected override void OnUpdate(FrameEventArgs e)
        {
            base.OnUpdate(e);
            
            if (_enter)
            {
                _cm.ApplyRules();
                _enter = false;
            }
            if (_mousePan)
            {
                _gm.Pan += (_mp - _panStart) / _gm.Scale;
                _panStart = _mp;
            }
            
            if (_palying)
            {
                bool time = Division(_dt);
                if (!_applied && time)
                {
                    _cm.ApplyRules();
                    _applied = true;
                }
                else if (!time)
                {
                    _applied = false;
                }
            }
            
            if (_place && _placeMode.Brush)
            {
                Vector2 pos = ((_mp / _gm.Scale) - _gm.Pan);
                Vector2I pi = ((int)Math.Abs(pos.X), (int)Math.Abs(pos.Y));
                if (pos.X < 0)
                {
                    pi.X = -pi.X - 1;
                }
                if (pos.Y < 0)
                {
                    pi.Y = -pi.Y - 1;
                }
                // if (pi != _pi)
                // {
                //     _cm.PushCell(pi, 1);
                // }
                PushLine(_pi, pi);
                _pi = pi;
                //_cm.PushCell(pi, (byte)(_placeAlive ? 1 : 0));
            }
            
            _gm.Render(_cm, Size);
        }
        private void PushLine(Vector2I start, Vector2I end)
        {
            Vector2I dif = end - start;
            
            if (dif.X <= 1 && dif.X >= -1 && dif.Y <= 1 && dif.Y >= -1)
            {
                _placeMode.Place(_cm, end);
                return;
            }
            
            int s, e;
            Line2 l = new Line2(dif, start);
            if (Math.Abs(l.Direction.X) < Math.Abs(l.Direction.Y))
            {
                s = Math.Min(start.Y, end.Y);
                e = Math.Max(start.Y, end.Y);
                for (int y = s; y <= e; y++)
                {
                    _placeMode.Place(_cm, ((int)l.GetX(y), y));
                }
                return;
            }
            
            s = Math.Min(start.X, end.X);
            e = Math.Max(start.X, end.X);
            for (int x = s; x <= e; x++)
            {
                _placeMode.Place(_cm, (x, (int)l.GetY(x)));
            }
            return;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            
            MouseButton button = e.Button;
            if (this[Mods.Shift])
            {
                // Swap left and right
                button = (3 - button);
            }
            
            if (e.Button == MouseButton.Middle)
            {
                _mousePan = true;
                _panStart = _mp;
                return;
            }
            if (button == MouseButton.Left)
            {
                _place = true;
                _placeMode.PushAlive = true;
            }
            else if (button == MouseButton.Right)
            {
                _place = true;
                _placeMode.PushAlive = false;
            }
            
            Vector2 pos = ((_mp / _gm.Scale) - _gm.Pan);
            Vector2I pi = ((int)Math.Abs(pos.X), (int)Math.Abs(pos.Y));
            if (pos.X < 0)
            {
                pi.X = -pi.X - 1;
            }
            if (pos.Y < 0)
            {
                pi.Y = -pi.Y - 1;
            }
            _pi = pi;
            
            _placeMode.Place(_cm, pi);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButton.Left)
            {
                _place = this[MouseButton.Right];
                return;
            }
            if (e.Button == MouseButton.Right)
            {
                _place = this[MouseButton.Left];
                return;
            }
            if (e.Button == MouseButton.Middle)
            {
                _mousePan = false;
                return;
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            _mp = e.Location - (Size / 2d);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            
            switch (e.Key)
            {
                case Keys.Space:
                    _palying = !_palying;
                    return;
                case Keys.Enter:
                    _enter = true;
                    return;
                case Keys.Equal:
                    _gm.Scale *= 1.1;
                    return;
                case Keys.Minus:
                    _gm.Scale /= 1.1;
                    return;
                case Keys.B:
                    _gm.SeeChunks = !_gm.SeeChunks;
                    return;
                case Keys.N:
                    _gm.ChunkNumbers = !_gm.ChunkNumbers;
                    return;
                case Keys.C:
                    _cm.Clear();
                    return;
                case Keys.D1:
                    _placeMode = IPlaceMode.Default;
                    return;
                case Keys.D2:
                    _placeMode = PlaceMode.Brush1;
                    return;
                case Keys.D3:
                    _placeMode = PlaceMode.Brush2;
                    return;
            }
        }
        protected override void OnScroll(ScrollEventArgs e)
        {
            base.OnScroll(e);
            
            double oldZoom = _gm.Scale;
            if (this[Mods.Control])
            {
                double newZoom = oldZoom + (e.DeltaY * 0.03 * oldZoom);

                if (newZoom < 0) { return; }

                _gm.Scale = newZoom;
                
                Vector2 pointRelOld = (_mp / oldZoom) - _gm.Pan;
                Vector2 pointRelNew = (_mp / newZoom) - _gm.Pan;
                _gm.Pan += pointRelNew - pointRelOld;
                return;
            }
            if (this[Mods.Shift])
            {
                _gm.Pan += new Vector2(-e.DeltaY, e.DeltaX) * 5d / oldZoom;
                return;
            }
            
            _gm.Pan += new Vector2(-e.DeltaX, e.DeltaY) * 5d / oldZoom;
        }

        protected override void OnSizeChange(VectorIEventArgs e)
        {
            base.OnSizeChange(e);
            DrawContext.Projection = Matrix4.CreateOrthographic(Width, Height, 0d, 1d);
        }
    }
}

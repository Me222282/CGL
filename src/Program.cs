using System;
using Zene.Graphics;
using Zene.Structs;
using Zene.Windowing;

namespace cgl
{
    public enum MouseMode
    {
        Place,
        Remove,
        Pan,
        Select
    }
    
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
        private MouseMode _mouseMode = MouseMode.Place;
        
        private bool _mouseSelect = false;
        private Vector2I _selectStart;
        private byte[,] _clipboard;
        
        private IPlaceMode _placeMode = IPlaceMode.Default;
        
        private bool Division(double t) => Timer % t >= (t / 2d);
        
        protected override void OnUpdate(FrameEventArgs e)
        {
            base.OnUpdate(e);
            
            Vector2 mPos = ((_mp / _gm.Scale) - _gm.Pan);
            Vector2I mPosI = (Vector2I)mPos;
            if (mPos.X < 0) { mPosI.X--; }
            if (mPos.Y < 0) { mPosI.Y--; }
            
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
            if (_mouseSelect)
            {
                _gm.Highlight = new RectangleI(
                    Math.Min(_selectStart.X, mPosI.X), Math.Max(_selectStart.Y, mPosI.Y) + 1,
                    Math.Abs(_selectStart.X - mPosI.X) + 1, Math.Abs(_selectStart.Y - mPosI.Y) + 1
                );
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
            
            _gm.HoverPoint = mPosI;
            
            if (_place && _placeMode.Brush)
            {
                Vector2I pi = GetPI(mPos);
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
        
        private static Vector2I GetPI(Vector2 pos)
        {
            Vector2I pi = ((int)Math.Abs(pos.X), (int)Math.Abs(pos.Y));
                
            if (pos.X < 0)
            {
                pi.X = -pi.X - 1;
            }
            if (pos.Y < 0)
            {
                pi.Y = -pi.Y - 1;
            }
            
            return pi;    
        }
        
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            
            if (e.Button == MouseButton.Middle)
            {
                _mousePan = true;
                _panStart = _mp;
                return;
            }
            
            Vector2 pos = ((_mp / _gm.Scale) - _gm.Pan);
            switch (_mouseMode)
            {
                case MouseMode.Place:
                    _place = true;
                    _placeMode.PushAlive = e.Button == MouseButton.Left;
                    goto Place;
                case MouseMode.Remove:
                    _place = true;
                    _placeMode.PushAlive = e.Button != MouseButton.Left;
                    goto Place;
                case MouseMode.Pan:
                    _mousePan = true;
                    _panStart = _mp;
                    return;
                case MouseMode.Select:
                    _mouseSelect = true;
                    Vector2I mmp = (Vector2I)pos;
                    if (pos.X < 0) { mmp.X--; }
                    if (pos.Y < 0) { mmp.Y--; }
                    _selectStart = mmp;
                    _gm.Highlight = new RectangleI(pos, 0d);
                    return;
            }
            
            return;
        Place:
            Vector2I pi = GetPI(pos);
            _pi = pi;
            
            _placeMode.Place(_cm, pi);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            
            if (e.Button == MouseButton.Middle)
            {
                _mousePan = false;
                return;
            }
            
            switch (_mouseMode)
            {
                case MouseMode.Place:
                    _place = this[MouseButton.Right];
                    _placeMode.PushAlive = false;
                    return;
                case MouseMode.Remove:
                    _place = this[MouseButton.Right];
                    _placeMode.PushAlive = true;
                    return;
                case MouseMode.Pan:
                    _mousePan = false;
                    return;
                case MouseMode.Select:
                    _mouseSelect = false;
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
            
            // if (e[Mods.Shift])
            // {
            //     if (_mouseMode == MouseMode.Place ||
            //         _mouseMode == MouseMode.Remove)
            //     {
            //         _mouseMode = 1 - _mouseMode;
            //     }
            // }
            
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
                    if (e[Mods.Control])
                    {
                        FillClipboard();
                        return;
                    }
                    _cm.Clear();
                    return;
                case Keys.V:
                    if (e[Mods.Control])
                    {
                        Paste();
                        return;
                    }
                    return;
                case Keys.D:
                    if (e[Mods.Control])
                    {
                        _gm.Highlight = RectangleI.Zero;
                        return;
                    }
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
                case Keys.G:
                    _placeMode = PlaceMode.Glider;
                    return;
                case Keys.P:
                    _mouseMode = MouseMode.Pan;
                    return;
                case Keys.S:
                    _mouseMode = MouseMode.Select;
                    return;
                case Keys.A:
                    _mouseMode = MouseMode.Place;
                    return;
                case Keys.R:
                    _mouseMode = MouseMode.Remove;
                    return;
                case Keys.H:
                    _gm.ShowHover = !_gm.ShowHover;
                    return;
            }
        }
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            
            if (e[Mods.Shift])
            {
                if (_mouseMode == MouseMode.Place ||
                    _mouseMode == MouseMode.Remove)
                {
                    _mouseMode = 1 - _mouseMode;
                }
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
        
        private void FillClipboard()
        {
            RectangleI bounds = _gm.Highlight;
            if (bounds.Width == 0 || bounds.Height == 0) { return; }
            
            _clipboard = new byte[bounds.Width, bounds.Height];
            
            IterateBounds(bounds.Width, bounds.Height,
                (bounds.Left, bounds.Bottom), false,
                (c, v1, v2) =>
                {
                    _clipboard[v1.X, v1.Y] = (byte)c[v2.X, v2.Y];
                });
        }
        private void Paste()
        {
            if (_clipboard == null || _clipboard.Length == 0) { return; }
            
            int width = _clipboard.GetLength(0);
            int height = _clipboard.GetLength(1);
            
            IterateBounds(width, height,
                (_selectStart.X, _selectStart.Y - height + 1), true,
                (c, v1, v2) =>
                {
                    c.PushCell(v2.X, v2.Y, _clipboard[v1.X, v1.Y]);
                });
            
            _gm.Highlight = new RectangleI(_selectStart.X, _selectStart.Y + 1, width, height);
        }
        private void IterateBounds(int width, int height, Vector2I start, bool write, Action<IChunk, Vector2I, Vector2I> act)
        {
            Func<Vector2I, IChunk> getChunk = write ? _cm.GetChunkWrite : _cm.GetChunkRead;
            
            (Vector2I cs, Vector2I sp) = _cm.GetChunkPos(start);
            int xCov = 0;
            int xStart = sp.X;
            int xChunk = Math.Min(width + xStart, _cm.ChunkSize.X);
            for (int x = 0; xChunk > 0; x++)
            {
                int yCov = 0;
                int cx = 0;
                int yStart = sp.Y;
                int yChunk = Math.Min(height + yStart, _cm.ChunkSize.Y);
                for (int y = 0; yChunk > 0; y++)
                {
                    IChunk c = getChunk(cs + (x, y));
                    int cy = 0;
                    for (cx = xStart; cx < xChunk; cx++)
                    {
                        for (cy = yStart; cy < yChunk; cy++)
                        {
                            act(c,
                                (xCov + cx - xStart, yCov + cy - yStart),
                                (cx, _cm.ChunkSize.Y - cy - 1));
                        }
                    }
                    
                    yCov += cy - yStart;
                    yStart = 0;
                    yChunk = Math.Min(height - yCov, yChunk);
                }
                xCov += cx - xStart;
                xStart = 0;
                yCov = 0;
                xChunk = Math.Min(width - xCov, xChunk);
            }
        }
    }
}

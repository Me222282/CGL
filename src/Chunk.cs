using Zene.Graphics;
using Zene.Graphics.Base.Extensions;
using Zene.Structs;

namespace cgl
{
    public class Chunk : IChunk
    {
        public Chunk(Vector2I size, ChunkManager cm, Vector2I location)
        {
            _cm = cm;
            Location = location;
            _size = size;
            _map = new GLArray<byte>(size);
            _temp = new GLArray<byte>(size);
            
            _checkMap = new GLArray<bool>(size);
            _checkTemp = new GLArray<bool>(size);
        }
        
        private GLArray<byte> _map;
        private GLArray<byte> _temp;
        
        private GLArray<bool> _checkMap;
        private GLArray<bool> _checkTemp;
        private Vector2I _size;
        
        private Vector2I Size => _size;
        
        public GLArray<byte> Map => _map;
        public GLArray<byte> Temp => _temp;
        
        public GLArray<bool> CheckMap => _checkMap;
        public GLArray<bool> CheckTemp => _checkTemp;

        public int this[int x, int y] => _map[x, y];
        public bool InUse { get; set; } = true;
        
        private int _useCount = 0;
        public void AddCheck(int x, int y)
        {
            _checkTemp[x, y] = true;
            _useCount++;
        }
        public bool ShouldDelete() => _useCount == 0;
        public void PushCell(int x, int y, byte v)
        {
            _map[x, y] = v;
            _checkMap[x, y] = true;
            WriteAround(_checkMap, x, y);
            _useCount++;
        }
        
        private IChunk _left;
        private IChunk _right;
        private IChunk _top;
        private IChunk _bottom;
        private IChunk _tl;
        private IChunk _tr;
        private IChunk _bl;
        private IChunk _br;
        private ChunkManager _cm;
        public Vector2I Location { get; set; }
        public void CalculateRules(ChunkManager cm)
        {
            _useCount = 0;
            _cm = cm;
            
            for (int x = 0; x < _size.X; x++)
            {
                for (int y = 0; y < _size.Y; y++)
                {
                    //bool c = _checkMap[x, y];
                    if (!_checkMap[x, y])
                    {
                        byte i = _map[x, y];
                        if (i > 0) { _useCount++; }
                        _temp[x, y] = i;
                        continue;
                    }
                    _checkMap[x, y] = false;
                    
                    int n = CountNeighbours(x, y);
                    bool alive = _map[x, y] == 1;
                    if (n == 3)
                    {
                        _temp[x, y] = 1;
                        _useCount++;
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
                        _useCount++;
                        //_checkTemp[x, y] = true;
                        continue;
                    }
                    _temp[x, y] = 0;
                    _useCount++;
                    WriteAround(_checkTemp, x, y);
                }
            }
        }
        public void ApplyFrame()
        {
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
            bool lx = x < 0;
            bool ly = y < 0;
            bool gx = x >= _size.X;
            bool gy = y >= _size.Y;
            
            if (lx == ly && lx == gx && lx == gy)
            {
                return _map[x, y];
            }
            if (lx)
            {
                if (ly)
                {
                    if (_bl == null || !_bl.InUse)
                    {
                        _bl = _cm.GetChunkRead(Location + (-1, 1));
                    }
                    return _bl[_size.X - 1, _size.Y - 1];
                }
                if (gy)
                {
                    if (_tl == null || !_tl.InUse)
                    {
                        _tl = _cm.GetChunkRead(Location - Vector2I.One);
                    }
                    return _tl[_size.X - 1, 0];
                }
                
                if (_left == null || !_left.InUse)
                {
                    _left = _cm.GetChunkRead(Location - (1, 0));
                }
                return _left[_size.X - 1, y];
            }
            if (ly)
            {
                if (gx)
                {
                    if (_br == null || !_br.InUse)
                    {
                        _br = _cm.GetChunkRead(Location + Vector2I.One);
                    }
                    return _br[0, _size.Y - 1];
                }
                
                if (_bottom == null || !_bottom.InUse)
                {
                    _bottom = _cm.GetChunkRead(Location + (0, 1));
                }
                return _bottom[x, _size.Y - 1];
            }
            if (gx)
            {
                if (gy)
                {
                    if (_tr == null || !_tr.InUse)
                    {
                        _tr = _cm.GetChunkRead(Location + (1, -1));
                    }
                    return _tr[0, 0];
                }
                
                if (_right == null || !_right.InUse)
                {
                    _right = _cm.GetChunkRead(Location + (1, 0));
                }
                return _right[0, y];
            }
            
            // gy
            if (_top == null || !_top.InUse)
            {
                _top = _cm.GetChunkRead(Location - (0, 1));
            }
            return _top[x, 0];
        }
        private void Write(GLArray<bool> map, int x, int y)
        {
            bool lx = x < 0;
            bool ly = y < 0;
            bool gx = x >= _size.X;
            bool gy = y >= _size.Y;
            
            if (lx == ly && lx == gx && lx == gy)
            {
                map[x, y] = true;
                return;
            }
            
            if (lx)
            {
                if (ly)
                {
                    if (_bl == null || _bl is Empty || !_bl.InUse)
                    {
                        _bl = _cm.GetChunkWrite(Location + (-1, 1));
                    }
                    _bl.AddCheck(_size.X - 1, _size.Y - 1);
                    return;
                }
                if (gy)
                {
                    if (_tl == null || _tl is Empty || !_tl.InUse)
                    {
                        _tl = _cm.GetChunkWrite(Location - Vector2I.One);
                    }
                    _tl.AddCheck(_size.X - 1, 0);
                    return;
                }
                
                if (_left == null || _left is Empty || !_left.InUse)
                {
                    _left = _cm.GetChunkWrite(Location - (1, 0));
                }
                _left.AddCheck(_size.X - 1, y);
                return;
            }
            if (ly)
            {
                if (gx)
                {
                    if (_br == null || _br is Empty || !_br.InUse)
                    {
                        _br = _cm.GetChunkWrite(Location + Vector2I.One);
                    }
                    _br.AddCheck(0, _size.Y - 1);
                    return;
                }
                
                if (_bottom == null || _bottom is Empty || !_bottom.InUse)
                {
                    _bottom = _cm.GetChunkWrite(Location + (0, 1));
                }
                _bottom.AddCheck(x, _size.Y - 1);
                return;
            }
            if (gx)
            {
                if (gy)
                {
                    if (_tr == null || _tr is Empty || !_tr.InUse)
                    {
                        _tr = _cm.GetChunkWrite(Location + (1, -1));
                    }
                    _tr.AddCheck(0, 0);
                    return;
                }
                
                if (_right == null || _right is Empty || !_right.InUse)
                {
                    _right = _cm.GetChunkWrite(Location + (1, 0));
                }
                _right.AddCheck(0, y);
                return;
            }
            
            // gy
            if (_top == null || _top is Empty || !_top.InUse)
            {
                _top = _cm.GetChunkWrite(Location - (0, 1));
            }
            _top.AddCheck(x, 0);
            return;
        }
        private void WriteAround(GLArray<bool> map, int x, int y)
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
        
        public void WriteToTexture(Vector2I location, ITexture texture, ChunkManager cm)
        {
            texture.TexSubImage2D(0,
                location.X, location.Y, cm.ChunkSize.X, cm.ChunkSize.Y,
                BaseFormat.R, TextureData.Byte, _map);
        }
    }
}
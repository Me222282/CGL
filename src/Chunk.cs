using System;
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
        // private int _checkCount = 0;
        // private int _aliveCount = 0;
        // private bool _doSwap = true;
        public void AddCheck(int x, int y)
        {
            _useCount++;
            //_checkCount++;
            if (_cm.ApplingRules)
            {
                _checkTemp[x, y] = true;
                return;
            }
            _checkMap[x, y] = true;
        }
        public bool ShouldDelete() => _useCount == 0;// && _aliveCount == 0;
        public void PushCell(int x, int y, byte v)
        {
            _map[x, y] = v;
            _checkMap[x, y] = true;
            WriteAround(_checkMap, x, y);
            _useCount++;
            //_aliveCount++;
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
            _cm = cm;
            
            // if (_checkCount == 0)
            // {
            //     _doSwap = false;
            //     return;
            // }
            // _checkCount = 0;
            // _aliveCount = 0;
            
            for (int x = 0; x < _size.X; x++)
            {
                for (int y = 0; y < _size.Y; y++)
                {
                    if (!_checkMap[x, y])
                    {
                        byte i = _map[x, y];
                        if (i > 0) { _useCount++; /*_aliveCount++;*/ }
                        _temp[x, y] = i;
                        continue;
                    }
                    _checkMap[x, y] = false;
                    
                    int n = CountNeighbours(x, y);
                    bool alive = _map[x, y] == 1;
                    if (n == 3)
                    {
                        _temp[x, y] = 1;
                        //_aliveCount++;
                        _useCount++;
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
                        //_aliveCount++;
                        _useCount++;
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
            // if (!_doSwap)
            // {
            //     _doSwap = true;
            //     return;
            // }
            _useCount = 0;
            
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
            
            Vector2I offset = Vector2I.Zero;
            if (lx)         { offset.X -= 1; }
            else if (gx)    { offset.X += 1; }
            if (ly)         { offset.Y += 1; }
            else if (gy)    { offset.Y -= 1; }
            
            ref IChunk ch = ref GetChunk(offset, x, y, out int cpx, out int cpy);
            
            if (ch == null || !ch.InUse)
            {
                ch = _cm.GetChunkRead(Location + offset);
            }
            return ch[cpx, cpy];
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
                //_checkCount++;
                return;
            }
            
            Vector2I offset = Vector2I.Zero;
            if (lx)         { offset.X -= 1; }
            else if (gx)    { offset.X += 1; }
            if (ly)         { offset.Y += 1; }
            else if (gy)    { offset.Y -= 1; }
            
            ref IChunk ch = ref GetChunk(offset, x, y, out int cpx, out int cpy);
            
            if (ch == null || ch is Empty || !ch.InUse)
            {
                ch = _cm.GetChunkWrite(Location + offset);
            }
            ch.AddCheck(cpx, cpy);
        }
        private ref IChunk GetChunk(Vector2I offset, int x, int y, out int cpx, out int cpy)
        {
            cpx = 0;
            cpy = 0;
            ref IChunk ch = ref _left;
            switch (offset.X, offset.Y)
            {
                case (-1, 0):
                    cpx = _size.X - 1;
                    cpy = y;
                    return ref _left;
                case (1, 0):
                    cpy = y;
                    return ref _right;
                case (0, 1):
                    cpx = x;
                    cpy = _size.Y - 1;
                    return ref _top;
                case (0, -1):
                    cpx = x;
                    return ref _bottom;
                case (-1, 1):
                    cpx = _size.X - 1;
                    cpy = _size.Y - 1;
                    return ref _tl;
                case (1, 1):
                    cpy = _size.Y - 1;
                    return ref _tr;
                case (-1, -1):
                    cpx = _size.X - 1;
                    return ref _bl;
                case (1, -1):
                    return ref _br;
            }
            
            throw new Exception();
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
using Zene.Graphics;
using Zene.Structs;

namespace cgl
{
    public class DataCache
    {
        public struct Block
        {
            public Block(Vector2I size)
            {
                Map = new GLArray<byte>(size);
                Temp = new GLArray<byte>(size);
                
                CheckMap = new GLArray<bool>(size);
                CheckTemp = new GLArray<bool>(size);
            }
            
            public GLArray<byte> Map;
            public GLArray<byte> Temp;
            
            public GLArray<bool> CheckMap;
            public GLArray<bool> CheckTemp;
        }
        
        public int Size { get; }
        
        private int _writeIndex = 0;
        private int _readIndex = 0;
        private Block[] _datas;
        
        public bool CanWrite => _writeIndex - _readIndex < _datas.Length;
        public bool IsEmtpy => _writeIndex == _readIndex;
        
        public void CacheData(Block b)
        {
            if (!CanWrite) { return; }
            
            _datas[_writeIndex % _datas.Length] = b;
            _writeIndex++;
        }
        public void CacheData(GLArray<byte> m, GLArray<byte> t, GLArray<bool> cm, GLArray<bool> ct)
        {
            if (!CanWrite) { return; }
            
            _datas[_writeIndex % _datas.Length] = new Block()
            {
                Map = m,
                Temp = t,
                CheckMap = cm,
                CheckTemp = ct
            };
            _writeIndex++;
        }
        public Block PullData()
        {
            if (IsEmtpy) { return new Block(); }
            
            int i = _readIndex % _datas.Length;
            _readIndex++;
            Block b = _datas[i];
            _datas[i] = new Block();
            return b;
        }
    }
}
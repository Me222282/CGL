using Zene.Structs;

namespace cgl
{
    public interface IPlaceMode
    {
        public bool PushAlive { get; set; }
        public bool Brush { get; }
        
        public void Place(ChunkManager cm, Vector2I location);
        
        public static IPlaceMode Default { get; } = new DefaultPlace();
        
        private class DefaultPlace : IPlaceMode
        {
            private byte _v = 1;
            public bool PushAlive
            {
                get => _v == 1;
                set => _v = (byte)(value ? 1 : 0);
            }
            public bool Brush => true;
            
            public void Place(ChunkManager cm, Vector2I location)
            {
                cm.PushCell(location, _v);
            }
        }
    }
}
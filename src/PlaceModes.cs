using Zene.Structs;

namespace cgl
{
    public static class PlaceMode
    {
        public static IPlaceMode Brush1 { get; } = new Brush1C();
        public static IPlaceMode Brush2 { get; } = new Brush2C();
        
        public static IPlaceMode Glider { get; } = new PresetPattern(new Vector2I[]
        {
            (0, 0), (1, 0), (-1, 0), (1, 1), (0, 2) 
        });
        
        private class Brush1C : IPlaceMode
        {
            private byte _v = 1;
            public bool PushAlive
            {
                get => _v == 1;
                set => _v = (byte)(value ? 1 : 0);
            }
            bool IPlaceMode.Brush => true;
            
            public void Place(ChunkManager cm, Vector2I location)
            {
                cm.PushCell(location, _v);
                cm.PushCell(location + (0, 1), _v);
                cm.PushCell(location - (0, 1), _v);
                cm.PushCell(location + (1, 0), _v);
                cm.PushCell(location - (1, 0), _v);
            }
        }
        private class Brush2C : IPlaceMode
        {
            private byte _v = 1;
            public bool PushAlive
            {
                get => _v == 1;
                set => _v = (byte)(value ? 1 : 0);
            }
            bool IPlaceMode.Brush => true;
            
            public void Place(ChunkManager cm, Vector2I location)
            {
                cm.PushCell(location, _v);
                
                cm.PushCell(location + (0, 1), _v);
                cm.PushCell(location - (0, 1), _v);
                cm.PushCell(location + (1, 0), _v);
                cm.PushCell(location - (1, 0), _v);
                
                cm.PushCell(location + (1, 1), _v);
                cm.PushCell(location + (1, -1), _v);
                cm.PushCell(location - (1, 1), _v);
                cm.PushCell(location + (-1, 1), _v);
                
                cm.PushCell(location + (0, 2), _v);
                cm.PushCell(location - (0, 2), _v);
                cm.PushCell(location + (2, 0), _v);
                cm.PushCell(location - (2, 0), _v);
            }
        }
    }
}
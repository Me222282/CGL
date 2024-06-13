using Zene.Structs;

namespace cgl
{
    public class Empty : IChunk
    {
        public int this[int x, int y] => 0;
        public bool InUse { get; set; } = false;
        
        public void AddCheck(int x, int y) { }
        public void ApplyRules(Vector2I location, ChunkManager cm) { }
        public void PushCell(int x, int y, byte v) { }

        public bool ShouldDelete() => true;
    }
}
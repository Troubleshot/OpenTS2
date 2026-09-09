namespace OpenTS2.SimAntics.Routing
{
    // A rectangular walkability grid for a single lot floor level. Each tile is either
    // walkable or blocked (by walls, object footprints, or lot boundaries). This is the
    // abstract input to the pathfinder, deliberately decoupled from lot/render data so it
    // can be built from any source and unit-tested in isolation.
    public class PathfindingGrid
    {
        public int Width { get; }
        public int Height { get; }

        private readonly bool[] _blocked;

        public PathfindingGrid(int width, int height)
        {
            Width = width;
            Height = height;
            _blocked = new bool[width * height];
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        // Out-of-bounds tiles count as blocked so callers never need a separate bounds check.
        public bool IsBlocked(int x, int y) => !InBounds(x, y) || _blocked[y * Width + x];

        public void SetBlocked(int x, int y, bool blocked)
        {
            if (InBounds(x, y))
                _blocked[y * Width + x] = blocked;
        }
    }
}

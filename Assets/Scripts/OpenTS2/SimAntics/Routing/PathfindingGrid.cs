namespace OpenTS2.SimAntics.Routing
{
    // A rectangular walkability grid for a single lot floor level. Tiles can be blocked (by
    // object footprints or lot boundaries), and walls can block movement across the edge
    // between two adjacent tiles without blocking the tiles themselves - this is how The Sims 2
    // represents walls. Deliberately decoupled from lot/render data so it can be built from any
    // source and unit-tested in isolation.
    public class PathfindingGrid
    {
        public int Width { get; }
        public int Height { get; }

        private readonly bool[] _blocked;
        // Wall on the edge between (x,y) and its east neighbour (x+1,y).
        private readonly bool[] _wallEast;
        // Wall on the edge between (x,y) and its north neighbour (x,y+1).
        private readonly bool[] _wallNorth;

        public PathfindingGrid(int width, int height)
        {
            Width = width;
            Height = height;
            _blocked = new bool[width * height];
            _wallEast = new bool[width * height];
            _wallNorth = new bool[width * height];
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        // Out-of-bounds tiles count as blocked so callers never need a separate bounds check.
        public bool IsBlocked(int x, int y) => !InBounds(x, y) || _blocked[y * Width + x];

        public void SetBlocked(int x, int y, bool blocked)
        {
            if (InBounds(x, y))
                _blocked[y * Width + x] = blocked;
        }

        // Places (or clears) a wall on the shared edge between two orthogonally-adjacent tiles.
        public void SetWallBetween(int ax, int ay, int bx, int by, bool wall = true)
        {
            if (!InBounds(ax, ay) || !InBounds(bx, by))
                return;
            if (bx == ax + 1 && by == ay)
                _wallEast[ay * Width + ax] = wall;
            else if (bx == ax - 1 && by == ay)
                _wallEast[by * Width + bx] = wall;
            else if (by == ay + 1 && bx == ax)
                _wallNorth[ay * Width + ax] = wall;
            else if (by == ay - 1 && bx == ax)
                _wallNorth[by * Width + bx] = wall;
        }

        // True if a wall separates two orthogonally-adjacent tiles.
        public bool IsWallBetween(int ax, int ay, int bx, int by)
        {
            if (bx == ax + 1 && by == ay)
                return InBounds(ax, ay) && _wallEast[ay * Width + ax];
            if (bx == ax - 1 && by == ay)
                return InBounds(bx, by) && _wallEast[by * Width + bx];
            if (by == ay + 1 && bx == ax)
                return InBounds(ax, ay) && _wallNorth[ay * Width + ax];
            if (by == ay - 1 && bx == ax)
                return InBounds(bx, by) && _wallNorth[by * Width + bx];
            return false;
        }

        // Whether a single step from one tile to an adjacent one (orthogonal or diagonal) is
        // allowed. Both endpoints must be walkable; orthogonal steps must not cross a wall;
        // diagonal steps additionally must not cut a blocked corner and must be wall-free along
        // both of the two L-shaped routes around the corner (so a sim never squeezes past a
        // wall end diagonally).
        public bool CanMove(int fromX, int fromY, int toX, int toY)
        {
            if (IsBlocked(fromX, fromY) || IsBlocked(toX, toY))
                return false;

            var dx = toX - fromX;
            var dy = toY - fromY;

            if (dx != 0 && dy != 0)
            {
                if (IsBlocked(fromX, toY) || IsBlocked(toX, fromY))
                    return false;
                var viaVertical = !IsWallBetween(fromX, fromY, fromX, toY) && !IsWallBetween(fromX, toY, toX, toY);
                var viaHorizontal = !IsWallBetween(fromX, fromY, toX, fromY) && !IsWallBetween(toX, fromY, toX, toY);
                return viaVertical && viaHorizontal;
            }

            return !IsWallBetween(fromX, fromY, toX, toY);
        }
    }
}

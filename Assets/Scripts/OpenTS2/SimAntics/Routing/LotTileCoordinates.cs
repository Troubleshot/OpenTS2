using System;

namespace OpenTS2.SimAntics.Routing
{
    // Converts between a lot's world-space coordinates and routing tile coordinates.
    //
    // Verified against a loaded lot (N001 Lot 82): objects sit at tile centres, so an object on
    // tile (tx, ty) has world position (tx + 0.5, ty + 0.5) on the ground plane, which is the
    // X/Y axes (Z is height/level). One world unit is one tile with no offset, and wall-graph
    // corner vertices sit at integer tile coordinates - the same space PathfindingGridBuilder
    // uses. Level is derived separately from height (LotArchitecture.GetLevelAt).
    public static class LotTileCoordinates
    {
        public static GridPosition WorldToTile(float worldX, float worldY)
        {
            return new GridPosition((int)Math.Floor(worldX), (int)Math.Floor(worldY));
        }

        // The world-space ground position of the centre of a tile (where an object on it sits).
        public static void TileCenterToWorld(GridPosition tile, out float worldX, out float worldY)
        {
            worldX = tile.X + 0.5f;
            worldY = tile.Y + 0.5f;
        }
    }
}

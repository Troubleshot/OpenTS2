using System;
using System.Collections.Generic;
using OpenTS2.Content.DBPF;

namespace OpenTS2.SimAntics.Routing
{
    // Builds a PathfindingGrid from a lot's wall graph. Tiles are addressed in wall-graph
    // corner coordinates: tile (tx, ty) is the unit square whose lower corner is the graph
    // vertex (tx, ty), the same space that LotTileCoordinates maps world positions into. A unit
    // wall segment between two adjacent vertices becomes a blocked edge between the two tiles it
    // separates.
    //
    // Only orthogonal unit wall segments on the requested level are applied. Diagonal walls
    // (which cut across a tile) are not yet modelled and are skipped; footprints and lot bounds
    // are layered in separately.
    public static class PathfindingGridBuilder
    {
        // Sizes the grid to the wall-graph corner extent. Prefer the explicit-size overload with
        // the lot's Elevation tile dimensions, since objects can sit outside the walled area.
        public static PathfindingGrid FromWallGraph(WallGraphAsset wallGraph, int level)
        {
            var maxX = 0;
            var maxY = 0;
            foreach (var position in wallGraph.Positions.Values)
            {
                var x = (int)Math.Round(position.XPos);
                var y = (int)Math.Round(position.YPos);
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }

            var grid = new PathfindingGrid(maxX + 1, maxY + 1);
            ApplyWalls(grid, wallGraph, level);
            return grid;
        }

        // Builds a grid of the given tile dimensions (e.g. Elevation.Width-1 by Elevation.Height-1)
        // with the level's walls applied.
        public static PathfindingGrid FromWallGraph(WallGraphAsset wallGraph, int level, int tileWidth, int tileHeight)
        {
            var grid = new PathfindingGrid(tileWidth, tileHeight);
            ApplyWalls(grid, wallGraph, level);
            return grid;
        }

        // Marks the given tiles as blocked, e.g. the tiles objects stand on. Which objects
        // count as obstacles, and multi-tile object footprints, are the caller's concern for
        // now (object footprint/flag data is not parsed yet); this just blocks what it is given.
        public static void BlockTiles(PathfindingGrid grid, IEnumerable<GridPosition> tiles)
        {
            if (tiles == null)
                return;
            foreach (var tile in tiles)
                grid.SetBlocked(tile.X, tile.Y, true);
        }

        private static void ApplyWalls(PathfindingGrid grid, WallGraphAsset wallGraph, int level)
        {
            foreach (var line in wallGraph.Lines)
            {
                if (!wallGraph.Positions.TryGetValue(line.FromId, out var from) ||
                    !wallGraph.Positions.TryGetValue(line.ToId, out var to))
                    continue;
                if (from.Level != level || to.Level != level)
                    continue;

                var ax = (int)Math.Round(from.XPos);
                var ay = (int)Math.Round(from.YPos);
                var bx = (int)Math.Round(to.XPos);
                var by = (int)Math.Round(to.YPos);

                var dx = Math.Abs(bx - ax);
                var dy = Math.Abs(by - ay);

                if (dy == 0 && dx == 1)
                {
                    // Horizontal wall along grid line y between the tiles below and above it.
                    var x = Math.Min(ax, bx);
                    grid.SetWallBetween(x, ay - 1, x, ay);
                }
                else if (dx == 0 && dy == 1)
                {
                    // Vertical wall along grid line x between the tiles left and right of it.
                    var y = Math.Min(ay, by);
                    grid.SetWallBetween(ax - 1, y, ax, y);
                }
                // else: diagonal or non-unit segment - not modelled yet.
            }
        }
    }
}

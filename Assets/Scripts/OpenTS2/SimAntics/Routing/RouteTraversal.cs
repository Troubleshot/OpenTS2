using System;
using System.Collections.Generic;

namespace OpenTS2.SimAntics.Routing
{
    // Walks a sim along a tile path at a given distance per step. The path (from the pathfinder)
    // is treated as a polyline through tile centres; Advance(distance) moves that far along it,
    // exposing the interpolated world position and the current heading. This is pure geometry
    // with no engine dependency - a MonoBehaviour drives it each frame (Advance(speed * dt)) and
    // copies WorldX/WorldY (the ground plane) onto the sim's transform.
    public class RouteTraversal
    {
        private readonly List<(float x, float y)> _points = new List<(float x, float y)>();
        private int _segment; // index of the waypoint the sim is currently heading towards

        public bool IsComplete { get; private set; }
        public float WorldX { get; private set; }
        public float WorldY { get; private set; }
        // Unit vector of the current facing; (0,0) once complete or for a single-tile path.
        public float HeadingX { get; private set; }
        public float HeadingY { get; private set; }

        public RouteTraversal(IReadOnlyList<GridPosition> path)
        {
            if (path != null)
            {
                foreach (var tile in path)
                {
                    LotTileCoordinates.TileCenterToWorld(tile, out var wx, out var wy);
                    _points.Add((wx, wy));
                }
            }

            if (_points.Count == 0)
            {
                IsComplete = true;
                return;
            }

            WorldX = _points[0].x;
            WorldY = _points[0].y;
            _segment = 1;
            if (_points.Count == 1)
                IsComplete = true;
        }

        public void Advance(float distance)
        {
            if (IsComplete || distance <= 0f)
                return;

            var remaining = distance;
            while (remaining > 0f && _segment < _points.Count)
            {
                var target = _points[_segment];
                var dx = target.x - WorldX;
                var dy = target.y - WorldY;
                // Distance left in this segment is simply from the current position to the
                // waypoint, since the position is advanced incrementally toward it.
                var toEnd = (float)Math.Sqrt(dx * dx + dy * dy);

                if (toEnd <= 1e-6f)
                {
                    _segment++;
                    continue;
                }

                HeadingX = dx / toEnd;
                HeadingY = dy / toEnd;

                if (remaining < toEnd)
                {
                    WorldX += HeadingX * remaining;
                    WorldY += HeadingY * remaining;
                    return;
                }

                // Reach the waypoint exactly and continue into the next segment.
                remaining -= toEnd;
                WorldX = target.x;
                WorldY = target.y;
                _segment++;
            }

            if (_segment >= _points.Count)
            {
                var last = _points[_points.Count - 1];
                WorldX = last.x;
                WorldY = last.y;
                IsComplete = true;
            }
        }
    }
}

using System;
using System.Collections.Generic;

namespace OpenTS2.SimAntics.Routing
{
    // A* pathfinding over a PathfindingGrid. Sims move on an 8-connected grid, so diagonal
    // steps are allowed but never cut a corner (a diagonal is only taken when both adjacent
    // orthogonal tiles are also walkable). Costs and heuristic use octile distance, which is
    // admissible for 8-connected movement.
    public static class AStarPathfinder
    {
        private const float Straight = 1f;
        private const float Diagonal = 1.41421356f; // sqrt(2)

        private static readonly int[] NeighborX = { 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly int[] NeighborY = { 0, 0, 1, -1, 1, -1, 1, -1 };

        // Returns the tile path from start to goal inclusive, or null if goal is unreachable
        // (or either endpoint is blocked). A start equal to goal returns a single-tile path.
        public static List<GridPosition> FindPath(PathfindingGrid grid, GridPosition start, GridPosition goal,
            bool allowDiagonal = true)
        {
            if (grid.IsBlocked(start.X, start.Y) || grid.IsBlocked(goal.X, goal.Y))
                return null;

            var width = grid.Width;
            var count = width * grid.Height;
            var goalIndex = goal.Y * width + goal.X;
            var startIndex = start.Y * width + start.X;

            var gScore = new float[count];
            var cameFrom = new int[count];
            var closed = new bool[count];
            for (var i = 0; i < count; i++)
            {
                gScore[i] = float.PositiveInfinity;
                cameFrom[i] = -1;
            }

            var open = new MinHeap(count);
            gScore[startIndex] = 0f;
            open.Push(startIndex, Heuristic(start, goal));

            var neighborLimit = allowDiagonal ? 8 : 4;

            while (open.Count > 0)
            {
                var current = open.Pop();
                if (current == goalIndex)
                    return Reconstruct(cameFrom, current, width);
                if (closed[current])
                    continue;
                closed[current] = true;

                var cx = current % width;
                var cy = current / width;

                for (var n = 0; n < neighborLimit; n++)
                {
                    var nx = cx + NeighborX[n];
                    var ny = cy + NeighborY[n];
                    // CanMove handles bounds, blocked tiles, walls and diagonal corner rules.
                    if (!grid.CanMove(cx, cy, nx, ny))
                        continue;

                    var diagonal = NeighborX[n] != 0 && NeighborY[n] != 0;
                    var neighborIndex = ny * width + nx;
                    if (closed[neighborIndex])
                        continue;

                    var tentative = gScore[current] + (diagonal ? Diagonal : Straight);
                    if (tentative >= gScore[neighborIndex])
                        continue;

                    gScore[neighborIndex] = tentative;
                    cameFrom[neighborIndex] = current;
                    open.Push(neighborIndex, tentative + Heuristic(new GridPosition(nx, ny), goal));
                }
            }

            return null;
        }

        private static float Heuristic(GridPosition a, GridPosition b)
        {
            var dx = a.X > b.X ? a.X - b.X : b.X - a.X;
            var dy = a.Y > b.Y ? a.Y - b.Y : b.Y - a.Y;
            var min = dx < dy ? dx : dy;
            var max = dx < dy ? dy : dx;
            return (max - min) * Straight + min * Diagonal;
        }

        private static List<GridPosition> Reconstruct(int[] cameFrom, int current, int width)
        {
            var path = new List<GridPosition>();
            while (current != -1)
            {
                path.Add(new GridPosition(current % width, current / width));
                current = cameFrom[current];
            }
            path.Reverse();
            return path;
        }

        // Binary min-heap keyed by float priority, storing grid tile indices. Supports lazy
        // decrease-key: a tile may be pushed multiple times and stale entries are skipped via
        // the closed set in the main loop. The backing arrays grow as needed, since live
        // entries can temporarily exceed the tile count when tiles are re-pushed.
        private class MinHeap
        {
            private int[] _items;
            private float[] _priorities;
            private int _count;

            public MinHeap(int capacity)
            {
                if (capacity < 1)
                    capacity = 1;
                _items = new int[capacity + 1];
                _priorities = new float[capacity + 1];
            }

            public int Count => _count;

            public void Push(int item, float priority)
            {
                if (_count + 1 >= _items.Length)
                {
                    var newSize = _items.Length * 2;
                    Array.Resize(ref _items, newSize);
                    Array.Resize(ref _priorities, newSize);
                }
                _count++;
                _items[_count] = item;
                _priorities[_count] = priority;
                var i = _count;
                while (i > 1)
                {
                    var parent = i / 2;
                    if (_priorities[parent] <= _priorities[i])
                        break;
                    Swap(parent, i);
                    i = parent;
                }
            }

            public int Pop()
            {
                var top = _items[1];
                _items[1] = _items[_count];
                _priorities[1] = _priorities[_count];
                _count--;
                var i = 1;
                while (true)
                {
                    var left = i * 2;
                    var right = left + 1;
                    var smallest = i;
                    if (left <= _count && _priorities[left] < _priorities[smallest])
                        smallest = left;
                    if (right <= _count && _priorities[right] < _priorities[smallest])
                        smallest = right;
                    if (smallest == i)
                        break;
                    Swap(smallest, i);
                    i = smallest;
                }
                return top;
            }

            private void Swap(int a, int b)
            {
                var ti = _items[a];
                _items[a] = _items[b];
                _items[b] = ti;
                var tp = _priorities[a];
                _priorities[a] = _priorities[b];
                _priorities[b] = tp;
            }
        }
    }
}

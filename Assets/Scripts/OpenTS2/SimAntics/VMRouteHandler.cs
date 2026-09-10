using System.Collections.Generic;
using OpenTS2.SimAntics.Routing;

namespace OpenTS2.SimAntics
{
    // Advances an entity one tile per VM tick along a route to a tile beside the target,
    // computed once when constructed from the VM's routing grid. Yields Continue while walking,
    // True on arrival, False if there is no grid or the target is unreachable. This is the
    // tick-driven mechanism the Go To Routing Slot primitive will build on, mirroring how
    // VMSleep yields across ticks via a continue handler.
    public class VMRouteHandler : VMContinueHandler
    {
        private readonly VMEntity _entity;
        private readonly List<GridPosition> _path;
        private int _index;

        public VMRouteHandler(VMEntity entity, GridPosition target)
        {
            _entity = entity;
            var grid = entity.VM?.RoutingGrid;
            if (grid != null)
            {
                var start = new GridPosition(entity.TileX, entity.TileY);
                _path = AStarPathfinder.FindPathAdjacentTo(grid, start, target);
            }
        }

        public bool HasPath => _path != null;

        public override VMExitCode Tick()
        {
            if (_path == null || _path.Count == 0)
                return VMExitCode.False;

            // Step onto the next tile (the first entry is the current tile, so it is a no-op).
            _entity.TileX = _path[_index].X;
            _entity.TileY = _path[_index].Y;
            _index++;

            return _index >= _path.Count ? VMExitCode.True : VMExitCode.Continue;
        }
    }
}

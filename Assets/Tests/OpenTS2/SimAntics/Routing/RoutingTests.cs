using System;
using NUnit.Framework;
using OpenTS2.SimAntics.Routing;

public class RoutingTests
{
    private static PathfindingGrid Grid(int w, int h) => new PathfindingGrid(w, h);

    [Test]
    public void FindsDiagonalPathOnEmptyGrid()
    {
        var path = AStarPathfinder.FindPath(Grid(5, 5), new GridPosition(0, 0), new GridPosition(4, 4));

        Assert.That(path, Is.Not.Null);
        Assert.That(path[0], Is.EqualTo(new GridPosition(0, 0)));
        Assert.That(path[path.Count - 1], Is.EqualTo(new GridPosition(4, 4)));
        Assert.That(path.Count, Is.EqualTo(5));
    }

    [Test]
    public void StartEqualsGoalReturnsSingleTile()
    {
        var path = AStarPathfinder.FindPath(Grid(5, 5), new GridPosition(2, 2), new GridPosition(2, 2));

        Assert.That(path, Is.Not.Null);
        Assert.That(path.Count, Is.EqualTo(1));
    }

    [Test]
    public void DetoursAroundWall()
    {
        var grid = Grid(5, 5);
        for (var y = 0; y <= 3; y++)
            grid.SetBlocked(2, y, true);

        var path = AStarPathfinder.FindPath(grid, new GridPosition(0, 0), new GridPosition(4, 0));

        Assert.That(path, Is.Not.Null);
        foreach (var tile in path)
            Assert.That(tile.X == 2 && tile.Y <= 3, Is.False, $"path stepped onto blocked tile {tile}");
    }

    [Test]
    public void WalledOffGoalIsUnreachable()
    {
        var grid = Grid(7, 7);
        var goal = new GridPosition(3, 3);
        for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
                if (!(dx == 0 && dy == 0))
                    grid.SetBlocked(goal.X + dx, goal.Y + dy, true);

        Assert.That(AStarPathfinder.FindPath(grid, new GridPosition(0, 0), goal), Is.Null);
    }

    [Test]
    public void DoesNotCutCornerThroughDiagonalGap()
    {
        var grid = Grid(3, 3);
        grid.SetBlocked(1, 0, true);
        grid.SetBlocked(0, 1, true);

        Assert.That(AStarPathfinder.FindPath(grid, new GridPosition(0, 0), new GridPosition(1, 1)), Is.Null);
    }

    [Test]
    public void ReroutesWhenOneOrthogonalBlocked()
    {
        var grid = Grid(3, 3);
        grid.SetBlocked(1, 0, true);

        var path = AStarPathfinder.FindPath(grid, new GridPosition(0, 0), new GridPosition(1, 1));

        Assert.That(path, Is.Not.Null);
        Assert.That(path.Count, Is.EqualTo(3));
    }

    [Test]
    public void OrthogonalOnlyPathLength()
    {
        var path = AStarPathfinder.FindPath(Grid(3, 3), new GridPosition(0, 0), new GridPosition(2, 2), allowDiagonal: false);

        Assert.That(path, Is.Not.Null);
        Assert.That(path.Count, Is.EqualTo(5));
    }

    [Test]
    public void BlockedStartIsUnreachable()
    {
        var grid = Grid(3, 3);
        grid.SetBlocked(0, 0, true);

        Assert.That(AStarPathfinder.FindPath(grid, new GridPosition(0, 0), new GridPosition(2, 2)), Is.Null);
    }

    [Test]
    public void PathIsContiguousSingleSteps()
    {
        var grid = Grid(20, 20);
        for (var y = 0; y < 18; y++)
            grid.SetBlocked(10, y, true);

        var path = AStarPathfinder.FindPath(grid, new GridPosition(0, 0), new GridPosition(19, 0));

        Assert.That(path, Is.Not.Null);
        for (var i = 1; i < path.Count; i++)
        {
            var adx = Math.Abs(path[i].X - path[i - 1].X);
            var ady = Math.Abs(path[i].Y - path[i - 1].Y);
            Assert.That(adx <= 1 && ady <= 1 && (adx + ady) > 0, Is.True, $"non-adjacent step {path[i - 1]}->{path[i]}");
        }
    }
}

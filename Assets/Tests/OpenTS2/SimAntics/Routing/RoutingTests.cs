using System;
using System.Collections.Generic;
using NUnit.Framework;
using OpenTS2.Content.DBPF;
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

    [Test]
    public void WallBlocksOrthogonalMove()
    {
        var grid = Grid(2, 1);
        grid.SetWallBetween(0, 0, 1, 0);

        Assert.That(grid.CanMove(0, 0, 1, 0), Is.False);
        Assert.That(grid.CanMove(1, 0, 0, 0), Is.False);
        Assert.That(Grid(2, 1).CanMove(0, 0, 1, 0), Is.True);
    }

    [Test]
    public void WallBlocksCorridor()
    {
        var grid = Grid(3, 1);
        grid.SetWallBetween(0, 0, 1, 0);

        Assert.That(AStarPathfinder.FindPath(grid, new GridPosition(0, 0), new GridPosition(2, 0)), Is.Null);
    }

    [Test]
    public void WallForcesLegalDetour()
    {
        var grid = Grid(3, 3);
        grid.SetWallBetween(0, 0, 1, 0);
        grid.SetWallBetween(0, 1, 1, 1); // gap at y=2

        var path = AStarPathfinder.FindPath(grid, new GridPosition(0, 0), new GridPosition(2, 0));

        Assert.That(path, Is.Not.Null);
        for (var i = 1; i < path.Count; i++)
            Assert.That(grid.CanMove(path[i - 1].X, path[i - 1].Y, path[i].X, path[i].Y), Is.True,
                $"illegal step {path[i - 1]}->{path[i]}");
    }

    [Test]
    public void DiagonalCannotSlipPastWallEnd()
    {
        var grid = Grid(2, 2);
        grid.SetWallBetween(0, 0, 1, 0);

        Assert.That(grid.CanMove(0, 0, 1, 1), Is.False);

        var path = AStarPathfinder.FindPath(grid, new GridPosition(0, 0), new GridPosition(1, 1));
        Assert.That(path, Is.Not.Null);
        Assert.That(path.Count, Is.EqualTo(3));
    }

    [Test]
    public void BuildsGridFromWallGraph()
    {
        // Corners: (1,1) id0, (2,1) id1, (1,2) id2 on level 0; (1,1) id3, (2,1) id4 on level 1.
        var positions = new[]
        {
            new WallGraphPositionEntry { Id = 0, XPos = 1, YPos = 1, Level = 0 },
            new WallGraphPositionEntry { Id = 1, XPos = 2, YPos = 1, Level = 0 },
            new WallGraphPositionEntry { Id = 2, XPos = 1, YPos = 2, Level = 0 },
            new WallGraphPositionEntry { Id = 3, XPos = 5, YPos = 5, Level = 1 },
            new WallGraphPositionEntry { Id = 4, XPos = 6, YPos = 5, Level = 1 },
        };
        var lines = new[]
        {
            new WallGraphLineEntry { LayerId = 0, FromId = 0, ToId = 1, Room1 = 1, Room2 = 2 }, // horizontal, level 0
            new WallGraphLineEntry { LayerId = 0, FromId = 0, ToId = 2, Room1 = 1, Room2 = 2 }, // vertical, level 0
            new WallGraphLineEntry { LayerId = 0, FromId = 3, ToId = 4, Room1 = 1, Room2 = 2 }, // horizontal, level 1
        };
        var wallGraph = new WallGraphAsset(10, 10, 2, 0, positions, new int[0], lines);

        var grid = PathfindingGridBuilder.FromWallGraph(wallGraph, level: 0);

        // Horizontal segment (1,1)-(2,1) separates tiles (1,0) and (1,1).
        Assert.That(grid.IsWallBetween(1, 0, 1, 1), Is.True);
        // Vertical segment (1,1)-(1,2) separates tiles (0,1) and (1,1).
        Assert.That(grid.IsWallBetween(0, 1, 1, 1), Is.True);
        // A neighbouring edge with no wall stays open.
        Assert.That(grid.IsWallBetween(0, 0, 1, 0), Is.False);
        // The level-1 wall (corners (5,5)-(6,5), tiles (5,4)-(5,5)) must not appear at level 0.
        Assert.That(grid.IsWallBetween(5, 4, 5, 5), Is.False);
    }

    [Test]
    public void AdjacentRoutingStopsBesideTarget()
    {
        var target = new GridPosition(2, 2);
        var path = AStarPathfinder.FindPathAdjacentTo(Grid(5, 5), new GridPosition(0, 0), target);

        Assert.That(path, Is.Not.Null);
        var end = path[path.Count - 1];
        Assert.That(end, Is.Not.EqualTo(target));
        Assert.That(Math.Abs(end.X - target.X) <= 1 && Math.Abs(end.Y - target.Y) <= 1, Is.True);
    }

    [Test]
    public void AdjacentRoutingWhenAlreadyBesideTargetIsSingleTile()
    {
        var path = AStarPathfinder.FindPathAdjacentTo(Grid(5, 5), new GridPosition(1, 2), new GridPosition(2, 2));

        Assert.That(path, Is.Not.Null);
        Assert.That(path.Count, Is.EqualTo(1));
        Assert.That(path[0], Is.EqualTo(new GridPosition(1, 2)));
    }

    [Test]
    public void FullyWalledTargetHasNoAdjacentSlot()
    {
        var grid = Grid(5, 5);
        var target = new GridPosition(2, 2);
        grid.SetWallBetween(2, 2, 3, 2);
        grid.SetWallBetween(2, 2, 1, 2);
        grid.SetWallBetween(2, 2, 2, 3);
        grid.SetWallBetween(2, 2, 2, 1);

        Assert.That(AStarPathfinder.FindPathAdjacentTo(grid, new GridPosition(0, 0), target), Is.Null);
    }

    [Test]
    public void MultiGoalReachesNearestGoal()
    {
        var goals = new List<GridPosition> { new GridPosition(0, 4), new GridPosition(4, 0) };

        var path = AStarPathfinder.FindPathToAny(Grid(5, 5), new GridPosition(0, 0), goals);

        Assert.That(path, Is.Not.Null);
        Assert.That(goals.Contains(path[path.Count - 1]), Is.True);
    }

    [Test]
    public void MultiGoalAllBlockedIsNull()
    {
        var grid = Grid(5, 5);
        grid.SetBlocked(0, 4, true);
        grid.SetBlocked(4, 0, true);
        var goals = new List<GridPosition> { new GridPosition(0, 4), new GridPosition(4, 0) };

        Assert.That(AStarPathfinder.FindPathToAny(grid, new GridPosition(0, 0), goals), Is.Null);
    }
}

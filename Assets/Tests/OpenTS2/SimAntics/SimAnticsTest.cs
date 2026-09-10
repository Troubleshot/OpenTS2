using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using OpenTS2.Content;
using OpenTS2.SimAntics;
using OpenTS2.Common;
using OpenTS2.Files.Formats.DBPF;
using OpenTS2.SimAntics.Primitives;
using OpenTS2.SimAntics.Routing;
using OpenTS2.Content.DBPF;

public class SimAnticsTest
{
    private uint _groupID;

    [SetUp]
    public void SetUp()
    {
        TestCore.Initialize();
        _groupID = ContentManager.Instance.AddPackage("TestAssets/SimAntics/bhav.package").GroupID;
    }

    [Test]
    public void TestLoadsBHAV()
    {
        var bhav = VM.GetBHAV(0x1001, _groupID);

        Assert.That(bhav.FileName, Is.EqualTo("OpenTS2 BHAV Test"));
        Assert.That(bhav.ArgumentCount, Is.EqualTo(1));
        Assert.That(bhav.LocalCount, Is.EqualTo(0));
    }

    [Test]
    public void TestRunBHAV()
    {
        // VM Entities need to be attached to an OBJD to be aware of private/semiglobal scope.
        var testObjectDefinition = new ObjectDefinitionAsset();
        testObjectDefinition.TGI = new ResourceKey(1, _groupID, TypeIDs.OBJD);

        var bhav = VM.GetBHAV(0x1001, _groupID);

        var vm = new VM();
        var entity = new VMEntity(testObjectDefinition);
        vm.AddEntity(entity);

        var stackFrame = new VMStackFrame(bhav, entity.MainThread);
        entity.MainThread.Frames.Push(stackFrame);

        // Test BHAV:
        // Multiplies Param0 by 2, stores it in Temp0
        // Sleeps for 1 Tick
        // Sets Temp0 to 1200
        // Sleeps for 20000 Ticks
        // Sets Temp0 to 0
        stackFrame.Arguments[0] = 10;

        vm.Tick();
        Assert.That(entity.Temps[0], Is.EqualTo(20));
        vm.Tick();
        Assert.That(entity.Temps[0], Is.EqualTo(1200));
        // Interrupt idle here, so that it doesn't sleep for 20000 ticks.
        vm.Scheduler.ScheduleInterrupt(entity.MainThread);
        vm.Tick();
        Assert.That(entity.Temps[0], Is.EqualTo(0));
    }

    [Test]
    public void TestPrimitiveRegistry()
    {
        var vmExpressionPrim = VMPrimitiveRegistry.GetPrimitive(0x2);
        Assert.That(vmExpressionPrim, Is.TypeOf(typeof(VMExpression)));
    }

    [Test]
    public void TestBHAVThrowsOnInfiniteLoop()
    {
        // VM Entities need to be attached to an OBJD to be aware of private/semiglobal scope.
        var testObjectDefinition = new ObjectDefinitionAsset();
        testObjectDefinition.TGI = new ResourceKey(1, _groupID, TypeIDs.OBJD);

        var bhav = VM.GetBHAV(0x1003, _groupID);

        var vm = new VM();
        var entity = new VMEntity(testObjectDefinition);
        vm.AddEntity(entity);

        var stackFrame = new VMStackFrame(bhav, entity.MainThread);
        entity.MainThread.Frames.Push(stackFrame);

        Exception exception = null;
        vm.ExceptionHandler += (Exception e, VMEntity ent) =>
        {
            exception = e;
        };

        vm.Tick();

        Assert.IsInstanceOf<SimAnticsException>(exception);
    }

    [Test]
    public void TestPrimitivesRegistered()
    {
        Assert.That(VMPrimitiveRegistry.GetPrimitive(0xF), Is.TypeOf(typeof(VMBreakPoint)));
        Assert.That(VMPrimitiveRegistry.GetPrimitive(0x20), Is.TypeOf(typeof(VMTestObjectType)));
        Assert.That(VMPrimitiveRegistry.GetPrimitive(0x2D), Is.TypeOf(typeof(VMGoToRoutingSlot)));
    }

    [Test]
    public void TestObjectTypePrimitive()
    {
        const uint testGUID = 0x4C29CE24;

        var vm = new VM();

        var myDefinition = new ObjectDefinitionAsset();
        myDefinition.TGI = new ResourceKey(1, _groupID, TypeIDs.OBJD);
        var myEntity = new VMEntity(myDefinition);
        vm.AddEntity(myEntity);

        var stackDefinition = new ObjectDefinitionAsset();
        stackDefinition.TGI = new ResourceKey(2, _groupID, TypeIDs.OBJD);
        stackDefinition.GUID = testGUID;
        var stackEntity = new VMEntity(stackDefinition);
        vm.AddEntity(stackEntity);

        var bhav = VM.GetBHAV(0x1001, _groupID);
        var stackFrame = new VMStackFrame(bhav, myEntity.MainThread);
        stackFrame.StackObjectID = stackEntity.ID;

        // Operands: GUID 0x4C29CE24, data value 0, source 0x0A (StackObjectID), flag 0x02 (store GUID in temps).
        var node = new BHAVAsset.Node
        {
            OpCode = 0x20,
            Operands = new byte[] { 0x24, 0xCE, 0x29, 0x4C, 0x00, 0x00, 0x0A, 0x02, 0, 0, 0, 0, 0, 0, 0, 0 }
        };

        var primitive = new VMTestObjectType();

        var matchResult = primitive.Execute(new VMContext { StackFrame = stackFrame, Node = node });
        Assert.That(matchResult.Code, Is.EqualTo(VMExitCode.True));
        Assert.That((ushort)myEntity.Temps[0], Is.EqualTo(testGUID & 0xFFFF));
        Assert.That((ushort)myEntity.Temps[1], Is.EqualTo(testGUID >> 16));

        node.Operands[0] = 0x00; // break the GUID match
        var missResult = primitive.Execute(new VMContext { StackFrame = stackFrame, Node = node });
        Assert.That(missResult.Code, Is.EqualTo(VMExitCode.False));
    }

    private VMEntity AddPositionedEntity(VM vm, int tileX, int tileY)
    {
        var definition = new ObjectDefinitionAsset();
        definition.TGI = new ResourceKey(1, _groupID, TypeIDs.OBJD);
        var entity = new VMEntity(definition);
        vm.AddEntity(entity);
        entity.TileX = tileX;
        entity.TileY = tileY;
        return entity;
    }

    [Test]
    public void RouteHandlerWalksEntityBesideTarget()
    {
        var vm = new VM();
        vm.RoutingGrid = new PathfindingGrid(5, 5);
        vm.RoutingGrid.SetBlocked(2, 2, true); // the object's own tile is blocked

        var entity = AddPositionedEntity(vm, 0, 0);
        var handler = new VMRouteHandler(entity, new GridPosition(2, 2));
        Assert.That(handler.HasPath, Is.True);

        var code = VMExitCode.Continue;
        var guard = 0;
        while (code == VMExitCode.Continue && guard++ < 100)
            code = handler.Tick();

        Assert.That(code, Is.EqualTo(VMExitCode.True));
        Assert.That(System.Math.Abs(entity.TileX - 2) + System.Math.Abs(entity.TileY - 2), Is.EqualTo(1),
            "entity should end orthogonally beside the target");
    }

    [Test]
    public void RouteHandlerReturnsFalseWhenUnreachable()
    {
        var vm = new VM();
        vm.RoutingGrid = new PathfindingGrid(5, 5);
        vm.RoutingGrid.SetBlocked(2, 2, true);
        vm.RoutingGrid.SetWallBetween(2, 2, 3, 2);
        vm.RoutingGrid.SetWallBetween(2, 2, 1, 2);
        vm.RoutingGrid.SetWallBetween(2, 2, 2, 3);
        vm.RoutingGrid.SetWallBetween(2, 2, 2, 1);

        var entity = AddPositionedEntity(vm, 0, 0);
        var handler = new VMRouteHandler(entity, new GridPosition(2, 2));

        Assert.That(handler.HasPath, Is.False);
        Assert.That(handler.Tick(), Is.EqualTo(VMExitCode.False));
    }

    [Test]
    public void RouteHandlerReturnsFalseWithNoGrid()
    {
        var vm = new VM();
        var entity = AddPositionedEntity(vm, 0, 0);
        var handler = new VMRouteHandler(entity, new GridPosition(2, 2));

        Assert.That(handler.HasPath, Is.False);
        Assert.That(handler.Tick(), Is.EqualTo(VMExitCode.False));
    }

    [Test]
    public void GoToRoutingSlotRoutesBesideStackObject()
    {
        var vm = new VM();
        vm.RoutingGrid = new PathfindingGrid(5, 5);
        vm.RoutingGrid.SetBlocked(2, 2, true); // the stack object occupies its tile

        var sim = AddPositionedEntity(vm, 0, 0);
        var stackObject = AddPositionedEntity(vm, 2, 2);

        var bhav = VM.GetBHAV(0x1001, _groupID);
        var frame = new VMStackFrame(bhav, sim.MainThread);
        frame.StackObjectID = stackObject.ID;
        var node = new BHAVAsset.Node { OpCode = 0x2D, Operands = new byte[16] };

        var result = new VMGoToRoutingSlot().Execute(new VMContext { StackFrame = frame, Node = node });
        Assert.That(result.Code, Is.EqualTo(VMExitCode.Continue));

        var code = VMExitCode.Continue;
        var guard = 0;
        while (code == VMExitCode.Continue && guard++ < 100)
            code = result.ContinueHandler.Tick();

        Assert.That(code, Is.EqualTo(VMExitCode.True));
        Assert.That(System.Math.Abs(sim.TileX - 2) + System.Math.Abs(sim.TileY - 2), Is.EqualTo(1),
            "sim should end orthogonally beside the stack object");
    }

    [Test]
    public void GoToRoutingSlotWithNoStackObjectReturnsFalse()
    {
        var vm = new VM();
        vm.RoutingGrid = new PathfindingGrid(5, 5);
        var sim = AddPositionedEntity(vm, 0, 0);

        var bhav = VM.GetBHAV(0x1001, _groupID);
        var frame = new VMStackFrame(bhav, sim.MainThread); // StackObjectID defaults to 0 (no entity)
        var node = new BHAVAsset.Node { OpCode = 0x2D, Operands = new byte[16] };

        var result = new VMGoToRoutingSlot().Execute(new VMContext { StackFrame = frame, Node = node });
        Assert.That(result.Code, Is.EqualTo(VMExitCode.False));
    }
}

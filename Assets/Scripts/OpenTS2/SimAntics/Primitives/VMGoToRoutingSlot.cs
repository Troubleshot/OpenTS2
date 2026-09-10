using OpenTS2.SimAntics.Routing;

namespace OpenTS2.SimAntics.Primitives
{
    // Go To Routing Slot (0x2D): routes the executing object (a sim) to a routing slot on the
    // stack object. Operand layout (TS2): [0-1] slot data value, [2] slot data source, [4] flags
    // (bit1 no failure trees, bit2 slot uses Temp 0, bit3 ignore dest footprint, bit4 allow
    // different altitudes, bit5 event trees), [5-6]/[8-10] post-route / event-tree BHAV.
    //
    // MVP: routes to a walkable tile beside the stack object via VMRouteHandler (Continue while
    // walking, True on arrival, False if unreachable / no stack object). Slot-precise positioning
    // and the flag behaviours need the object slot system and are not modelled yet.
    public class VMGoToRoutingSlot : VMPrimitive
    {
        public override VMReturnValue Execute(VMContext ctx)
        {
            var stackObject = ctx.StackObjectEntity;
            if (stackObject == null)
                return VMReturnValue.ReturnFalse;

            var target = new GridPosition(stackObject.TileX, stackObject.TileY);
            return new VMReturnValue(new VMRouteHandler(ctx.Entity, target));
        }
    }
}

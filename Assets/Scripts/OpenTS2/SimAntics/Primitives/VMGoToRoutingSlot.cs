using System.Linq;
using OpenTS2.Common;
using OpenTS2.Content;
using OpenTS2.Content.DBPF;
using OpenTS2.Files.Formats.DBPF;
using OpenTS2.SimAntics.Routing;

namespace OpenTS2.SimAntics.Primitives
{
    // Go To Routing Slot (0x2D): routes the executing object (a sim) to a routing slot on the
    // stack object. Operand layout (TS2): [0-1] slot data value, [2] slot data source, [4] flags
    // (bit1 no failure trees, bit2 slot uses Temp 0, bit3 ignore dest footprint, bit4 allow
    // different altitudes, bit5 event trees), [5-6]/[8-10] post-route / event-tree BHAV.
    //
    // If the stack object has a SLOT resource, routes to the requested routing slot's tile
    // (computed from the object's tile, facing and the slot offset); otherwise falls back to a
    // tile beside the object. Returns a VMRouteHandler so it yields across ticks. The slot path is
    // dormant until lot objects are VM entities with a facing set - the facing convention and the
    // slot-index mapping still need verifying on a real lot before they are relied on.
    public class VMGoToRoutingSlot : VMPrimitive
    {
        public override VMReturnValue Execute(VMContext ctx)
        {
            var stackObject = ctx.StackObjectEntity;
            if (stackObject == null)
                return VMReturnValue.ReturnFalse;

            var target = TryGetRoutingSlotTile(ctx, stackObject)
                         ?? new GridPosition(stackObject.TileX, stackObject.TileY);

            return new VMReturnValue(new VMRouteHandler(ctx.Entity, target));
        }

        private static GridPosition? TryGetRoutingSlotTile(VMContext ctx, VMEntity stackObject)
        {
            var definition = stackObject.ObjectDefinition;
            if (definition == null)
                return null;

            var slotFile = ContentManager.Instance.GetAsset<SlotFileAsset>(
                new ResourceKey(definition.SlotIDPointer, definition.GlobalTGI.GroupID, TypeIDs.SLOT));
            if (slotFile == null)
                return null;

            // Only read the slot index once we know there are slots to index into.
            var slotIndex = ctx.GetData((VMDataSource)ctx.Node.GetOperand(2), ctx.Node.GetInt16Operand(0));
            var routingSlots = slotFile.Slots.Where(s => s.IsRouting).ToList();
            if (slotIndex < 0 || slotIndex >= routingSlots.Count)
                return null;

            var slot = routingSlots[slotIndex];
            return SlotPlacement.GetSlotTile(new GridPosition(stackObject.TileX, stackObject.TileY),
                stackObject.FacingDegrees, slot.OffsetX, slot.OffsetY);
        }
    }
}

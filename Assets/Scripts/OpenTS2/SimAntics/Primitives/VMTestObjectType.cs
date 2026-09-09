namespace OpenTS2.SimAntics.Primitives
{
    // Test Object Type (0x20): tests whether an object's GUID matches a given GUID literal.
    // Operands: [0-3] GUID to test against, [4-5] data value qualifying the object source in [6],
    // [6] data source yielding the object's ID, [7] flags.
    public class VMTestObjectType : VMPrimitive
    {
        // Flag bits (1-indexed in the SimAntics documentation).
        private const byte StoreGuidInTemps = 0x2; // bit 2: store the tested object's GUID in Temp 0 and 1.

        public override VMReturnValue Execute(VMContext ctx)
        {
            var guid = (uint)ctx.Node.GetUInt16Operand(0) | ((uint)ctx.Node.GetUInt16Operand(2) << 16);
            var objectData = ctx.Node.GetInt16Operand(4);
            var objectSource = (VMDataSource)ctx.Node.GetOperand(6);
            var flags = ctx.Node.GetOperand(7);

            var objectID = ctx.GetData(objectSource, objectData);
            var entity = ctx.VM.GetEntityByID(objectID);
            if (entity == null || entity.ObjectDefinition == null)
                return VMReturnValue.ReturnFalse;

            var entityGUID = entity.ObjectDefinition.GUID;

            if ((flags & StoreGuidInTemps) != 0)
            {
                ctx.Entity.Temps[0] = (short)(entityGUID & 0xFFFF);
                ctx.Entity.Temps[1] = (short)(entityGUID >> 16);
            }

            return entityGUID == guid ? VMReturnValue.ReturnTrue : VMReturnValue.ReturnFalse;
        }
    }
}

namespace OpenTS2.SimAntics.Primitives
{
    // Break Point (0x0F) halts execution when a SimAntics debugger is attached.
    // During normal execution it is a no-op that passes straight through to True.
    public class VMBreakPoint : VMPrimitive
    {
        public override VMReturnValue Execute(VMContext ctx)
        {
            return VMReturnValue.ReturnTrue;
        }
    }
}

using System.Collections.Generic;

namespace OpenTS2.Content.DBPF
{
    public enum SlotItemType : ushort
    {
        Container = 0,
        Routing = 3,
        Target = 4,
    }

    // One slot on an object: a positional anchor relative to the object's origin, used for
    // containment (Container), sim routing/standing (Routing), or targeting (Target). The offset
    // is in the object's local space (X/Y ground, Z height); routing uses it to find where a sim
    // stands to use the object.
    public class Slot
    {
        public ushort Type;
        public float OffsetX;
        public float OffsetY;
        public float OffsetZ;

        public bool IsRouting => Type == (ushort)SlotItemType.Routing;
    }

    public class SlotFileAsset : AbstractAsset
    {
        public uint Version { get; }
        public List<Slot> Slots { get; }

        public SlotFileAsset(uint version, List<Slot> slots)
        {
            Version = version;
            Slots = slots;
        }
    }
}

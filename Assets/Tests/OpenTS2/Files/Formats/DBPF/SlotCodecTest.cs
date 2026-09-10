using System.IO;
using NUnit.Framework;
using OpenTS2.Common;
using OpenTS2.Content.DBPF;
using OpenTS2.Files.Formats.DBPF;

public class SlotCodecTest
{
    // Builds a minimal version-4 SLOT resource with two slots (a container at the origin and a
    // routing slot at a known offset), matching the SimPE layout.
    private static byte[] BuildVersion4Slot()
    {
        using (var stream = new MemoryStream())
        {
            var writer = new BinaryWriter(stream);
            writer.Write(new byte[64]);   // filename
            writer.Write((uint)0);        // id
            writer.Write((uint)4);        // version
            writer.Write((uint)0);        // unknown
            writer.Write(2);              // slot count

            // Slot 0: container at the origin.
            writer.Write((ushort)SlotItemType.Container);
            writer.Write(0f);
            writer.Write(0f);
            writer.Write(0f);
            for (var i = 0; i < 5; i++)
                writer.Write(0);

            // Slot 1: routing slot with a distinct offset.
            writer.Write((ushort)SlotItemType.Routing);
            writer.Write(1.5f);
            writer.Write(-2.0f);
            writer.Write(0.25f);
            for (var i = 0; i < 5; i++)
                writer.Write(0);

            return stream.ToArray();
        }
    }

    [Test]
    public void ParsesSlotsAndOffsets()
    {
        var asset = (SlotFileAsset)new SlotCodec().Deserialize(BuildVersion4Slot(), default(ResourceKey), null);

        Assert.That(asset.Version, Is.EqualTo(4));
        Assert.That(asset.Slots.Count, Is.EqualTo(2));

        Assert.That(asset.Slots[0].Type, Is.EqualTo((ushort)SlotItemType.Container));
        Assert.That(asset.Slots[0].IsRouting, Is.False);

        var routing = asset.Slots[1];
        Assert.That(routing.Type, Is.EqualTo((ushort)SlotItemType.Routing));
        Assert.That(routing.IsRouting, Is.True);
        Assert.That(routing.OffsetX, Is.EqualTo(1.5f));
        Assert.That(routing.OffsetY, Is.EqualTo(-2.0f));
        Assert.That(routing.OffsetZ, Is.EqualTo(0.25f));
    }
}

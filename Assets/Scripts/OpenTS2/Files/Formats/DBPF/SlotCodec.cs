using System.Collections.Generic;
using System.IO;
using OpenTS2.Common;
using OpenTS2.Content;
using OpenTS2.Content.DBPF;
using OpenTS2.Files.Utils;

namespace OpenTS2.Files.Formats.DBPF
{
    // Parses a SLOT resource: a 64-byte filename, then id/version/unknown (uint32 each) and a
    // slot count, followed by that many slot entries. Each entry is a type (uint16) and an X/Y/Z
    // offset (float), then version-dependent extra fields. Layout follows SimPE's SlotItem.
    [Codec(TypeIDs.SLOT)]
    public class SlotCodec : AbstractCodec
    {
        public override AbstractAsset Deserialize(byte[] bytes, ResourceKey tgi, DBPFFile sourceFile)
        {
            var stream = new MemoryStream(bytes);
            var reader = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN);

            reader.Seek(SeekOrigin.Current, 64); // filename
            var id = reader.ReadUInt32();
            var version = reader.ReadUInt32();
            var unknown = reader.ReadUInt32();
            var count = reader.ReadInt32();

            var slots = new List<Slot>(count);
            for (var i = 0; i < count; i++)
            {
                var type = reader.ReadUInt16();
                var offsetX = reader.ReadFloat();
                var offsetY = reader.ReadFloat();
                var offsetZ = reader.ReadFloat();

                for (var j = 0; j < 5; j++)
                    reader.ReadInt32();

                if (version >= 5)
                {
                    reader.ReadFloat();
                    reader.ReadFloat();
                    reader.ReadFloat();
                    reader.ReadInt32();
                }
                if (version >= 6)
                {
                    reader.ReadUInt16();
                    reader.ReadUInt16();
                }
                if (version >= 7) reader.ReadFloat();
                if (version >= 8) reader.ReadInt32();
                if (version >= 9) reader.ReadInt32();
                if (version >= 0x10) reader.ReadFloat();
                if (version >= 0x40)
                {
                    reader.ReadInt32();
                    reader.ReadInt32();
                }

                slots.Add(new Slot
                {
                    Type = type,
                    OffsetX = offsetX,
                    OffsetY = offsetY,
                    OffsetZ = offsetZ,
                });
            }

            stream.Dispose();
            reader.Dispose();
            return new SlotFileAsset(version, slots);
        }
    }
}

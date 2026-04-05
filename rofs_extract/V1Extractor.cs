using System.Buffers.Binary;
using System.Text;

namespace rofs_extract;

/// <summary>ROFS format used in versions 0.10.2 and below.</summary>
internal sealed class V1Extractor : IExtractor
{
    public void Extract(ReadOnlySpan<byte> data, string outFolder)
    {
        int bodyOffset  = (int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(RofsConstants.V1HeaderSizeOffset));
        int fdiBase     = bodyOffset + (int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(RofsConstants.V1FdiOffsetField));

        ProcessDirectory(data, outFolder, bodyOffset, fdiBase, fseIndex: 0, fdiIndex: 0);
        Console.WriteLine("Done!");
    }

    private static void ProcessDirectory(
        ReadOnlySpan<byte> data,
        string outFolder,
        int bodyOffset,
        int fdiBase,
        int fseIndex,
        int fdiIndex)
    {
        int fseOffset       = bodyOffset + fseIndex * RofsConstants.V1FseEntryBytes;
        int entryDataOffset = bodyOffset + (int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(fseOffset));

        ProcessEntries(data, outFolder, bodyOffset, fdiBase, entryDataOffset, fdiIndex, path: "");
    }

    private static void ProcessEntries(
        ReadOnlySpan<byte> data,
        string outFolder,
        int bodyOffset,
        int fdiBase,
        int entryDataOffset,
        int fdiIndex,
        string path)
    {
        int cursor = entryDataOffset;

        while (true)
        {
            byte ctrl = data[cursor++];
            if (ctrl == 0) break;

            bool isDir  = (ctrl & 0x80) != 0;
            int nameLen = ctrl & 0x7F;
            string name = Encoding.UTF8.GetString(data.Slice(cursor, nameLen));

            if (isDir)
            {
                int childId         = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(cursor + nameLen)) & 0xFFF;
                int childFseOffset  = bodyOffset + childId * RofsConstants.V1FseEntryBytes;
                int childFdiStart   = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(childFseOffset + 4));
                int childEntryData  = bodyOffset + (int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(childFseOffset));

                ProcessEntries(data, outFolder, bodyOffset, fdiBase, childEntryData, childFdiStart, path + name + "/");
                cursor += nameLen + 2;
            }
            else
            {
                cursor += nameLen;
                int fdiPos  = fdiBase + fdiIndex++ * RofsConstants.V1FdiEntryBytes;
                uint start  = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(fdiPos));
                uint end    = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(fdiPos + 4));

                FileWriter.Write(outFolder, path + name, data.Slice((int)start, (int)(end - start)));
            }
        }
    }
}

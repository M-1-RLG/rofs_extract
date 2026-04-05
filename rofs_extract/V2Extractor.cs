using System.Buffers.Binary;
using System.Text;

namespace rofs_extract;

/// <summary>ROFS format introduced in version 0.12.0.</summary>
internal sealed class V2Extractor : IExtractor
{
    public void Extract(ReadOnlySpan<byte> data, string outFolder)
    {
        int b = RofsConstants.V2Base;

        int dirOffset       = (int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(b + RofsConstants.V2DirOffsetField));
        int dirSize         = (int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(b + RofsConstants.V2DirSizeField));
        int fileTableOffset = (int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(b + RofsConstants.V2FileTableOffField));
        int fileTableSize   = (int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(b + RofsConstants.V2FileTableSizeField));
        int fileDataOffset  = (int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(b + RofsConstants.V2FileDataOffField));

        var dirPaths = BuildDirectoryMap(data, b + dirOffset, dirSize);
        int count    = ExtractFiles(data, outFolder, b + fileTableOffset, fileTableSize, b + fileDataOffset, dirPaths);

        Console.WriteLine($"Extracted {count} file(s).");
    }

    // -------------------------------------------------------------------------

    private static Dictionary<int, string> BuildDirectoryMap(
        ReadOnlySpan<byte> data,
        int absDirBase,
        int dirSize)
    {
        var map = new Dictionary<int, string>();
        int pos = 0;

        while (pos < dirSize)
        {
            int currentPos = pos;
            int parentOffs = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(absDirBase + pos));
            int nameLen    = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(absDirBase + pos + RofsConstants.V2DirEntryNameLenOff));
            string name    = nameLen > 0
                ? Encoding.Unicode.GetString(data.Slice(absDirBase + pos + RofsConstants.V2DirEntryBaseSize, nameLen))
                : "";

            string fullPath = "";
            if (parentOffs != currentPos && map.TryGetValue(parentOffs, out string? parentPath))
                fullPath = string.IsNullOrEmpty(parentPath) ? name : $"{parentPath}/{name}";

            map[currentPos] = fullPath;
            pos = AlignUp(pos + RofsConstants.V2DirEntryBaseSize + nameLen);
        }

        return map;
    }

    private static int ExtractFiles(
        ReadOnlySpan<byte> data,
        string outFolder,
        int absFileBase,
        int fileTableSize,
        int absFileDataBase,
        Dictionary<int, string> dirPaths)
    {
        int pos   = 0;
        int count = 0;

        while (pos < fileTableSize)
        {
            int    parentOffs = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(absFileBase + pos));
            long   dataOffs   = BinaryPrimitives.ReadInt64LittleEndian(data.Slice(absFileBase + pos + RofsConstants.V2FileDataOffsField));
            long   size       = BinaryPrimitives.ReadInt64LittleEndian(data.Slice(absFileBase + pos + RofsConstants.V2FileSizeField));
            int    nameLen    = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(absFileBase + pos + RofsConstants.V2FileNameLenField));
            string name       = nameLen > 0
                ? Encoding.Unicode.GetString(data.Slice(absFileBase + pos + RofsConstants.V2FileEntryBaseSize, nameLen))
                : "";

            string parentPath = dirPaths.GetValueOrDefault(parentOffs, "");
            string fullPath   = string.IsNullOrEmpty(parentPath) ? name : $"{parentPath}/{name}";

            int finalOffset = absFileDataBase + (int)dataOffs;
            FileWriter.Write(outFolder, fullPath, data.Slice(finalOffset, (int)size));

            pos = AlignUp(pos + RofsConstants.V2FileEntryBaseSize + nameLen);
            count++;
        }

        return count;
    }

    private static int AlignUp(int value) =>
        (value + RofsConstants.Align - 1) & ~(RofsConstants.Align - 1);
}

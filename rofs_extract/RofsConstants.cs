namespace rofs_extract;

internal static class RofsConstants
{
    public const uint Magic = 0x53464F52; // "ROFS" LE

    // V1 layout
    public const int V1HeaderSizeOffset = 0x08;
    public const int V1FdiOffsetField   = 0x0C;
    public const int V1FseEntryBytes    = 8;
    public const int V1FdiEntryBytes    = 8;

    // V2 layout
    public const int V2Base                = 0x400;
    public const int V2DirOffsetField      = 0x0C;
    public const int V2DirSizeField        = 0x10;
    public const int V2FileTableOffField   = 0x1C;
    public const int V2FileTableSizeField  = 0x20;
    public const int V2FileDataOffField    = 0x24;
    public const int V2FileEntryBaseSize   = 0x20;
    public const int V2DirEntryBaseSize    = 0x18;
    public const int V2DirEntryNameLenOff  = 0x14;
    public const int V2FileDataOffsField   = 0x08;
    public const int V2FileSizeField       = 0x10;
    public const int V2FileNameLenField    = 0x1C;
    public const int Align                 = 4;

    // Detection thresholds
    public const uint V1MaxHeaderSize = 0x400;
    public const int  V2MinFileLength = 0x428;
}

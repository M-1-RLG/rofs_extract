using System.Buffers.Binary;

namespace rofs_extract;

/// <summary>
/// Loads a ROFS image, auto-detects its format version, and dispatches
/// extraction to the appropriate <see cref="IExtractor"/> implementation.
/// </summary>
internal sealed class RofsFile
{
    private readonly ReadOnlyMemory<byte> _data;

    private RofsFile(byte[] raw) => _data = raw;

    // -------------------------------------------------------------------------
    // Factory

    public static RofsFile Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: '{path}'");

        byte[] raw = File.ReadAllBytes(path);

        if (raw.Length < 4 || BinaryPrimitives.ReadUInt32LittleEndian(raw) != RofsConstants.Magic)
            throw new InvalidDataException("Not a valid ROFS file (magic mismatch).");

        return new RofsFile(raw);
    }

    // -------------------------------------------------------------------------
    // Detection & extraction

    public void ExtractTo(string outFolder)
    {
        IExtractor extractor = DetectExtractor();
        extractor.Extract(_data.Span, outFolder);
    }

    private IExtractor DetectExtractor()
    {
        ReadOnlySpan<byte> span = _data.Span;
        uint v1HeaderSize = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(RofsConstants.V1HeaderSizeOffset));

        if (v1HeaderSize is > 0 and < RofsConstants.V1MaxHeaderSize)
        {
            Console.WriteLine("Detected format: Legacy (V1 / ≤0.10.2)");
            return new V1Extractor();
        }

        if (_data.Length > RofsConstants.V2MinFileLength)
        {
            Console.WriteLine("Detected format: Modern (V2 / 0.12.0)");
            return new V2Extractor();
        }

        throw new InvalidDataException("Unknown or corrupted ROFS format.");
    }
}

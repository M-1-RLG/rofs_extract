namespace rofs_extract;

internal interface IExtractor
{
    void Extract(ReadOnlySpan<byte> data, string outFolder);
}

internal static class FileWriter
{
    public static void Write(string root, string virtualPath, ReadOnlySpan<byte> content)
    {
        string fullPath = Path.Combine(root, virtualPath.Replace('/', Path.DirectorySeparatorChar));
        string? dir = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        fs.Write(content);
        Console.WriteLine($"  -> {virtualPath}");
    }
}

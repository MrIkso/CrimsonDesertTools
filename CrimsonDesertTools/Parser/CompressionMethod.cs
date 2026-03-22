namespace CrimsonDesertTools.Parser
{
    public enum CompressionMethod : byte
    {
        None = 0,
        Partial = 1, // custom compression
        LZ4 = 2,
        Zlib = 3,
        QuickLZ = 4
    }
}
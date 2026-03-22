namespace CrimsonDesertTools.Parser
{
    public struct FileInfo
    {
        public uint NameOffset;
        public uint Offset;
        public uint CompressSize;
        public uint DecompressSize;
        public ushort PazIndex;
        public ushort Flags;

        public EncryptionMethod Encryption => (EncryptionMethod)(Flags >> 4);
        public CompressionMethod Compression => (CompressionMethod)(Flags & 0x0F);

        public bool IsEncrypted => Encryption != EncryptionMethod.None;
        public bool IsCompressed => Compression != CompressionMethod.None;

        public override string ToString()
        {
            return $"Paz: {NameOffset}, Off: 0x{Offset:X}, CSize: {CompressSize}, DSize: {DecompressSize}, " +
                   $"Enc: {Encryption}, Comp: {Compression}, Archive Index: {PazIndex}";
        }
    }
}
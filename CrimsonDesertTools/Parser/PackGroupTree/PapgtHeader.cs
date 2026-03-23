namespace CrimsonDesertTools.Parser.PackGroupTree
{
    /// <summary>
    /// Represents the Header of a .papgt file (12 bytes).
    /// </summary>
    public struct PapgtHeader
    {
        public uint Unknown;
        public uint FileCrc;    // Checksum of the data following the header
        public byte GroupCount; // Number of .pamt groups
        public ushort Unknown1;
        public byte Pad;        // Padding byte (0x00)
    }

}

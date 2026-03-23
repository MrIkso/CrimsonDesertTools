namespace CrimsonDesertTools.Parser.PackGroupTree
{
    /// <summary>
    /// Contains metadata information for a specific .pamt file.
    /// </summary>
    public struct PackMetaInfo
    {
        public byte IsOptional;
        public PackGroupLanguageType PackGroupLanguageType;
        public byte Zero;
        public uint FolderHash; 
        public uint NameOffset; // Offset within the string block
        public uint PamtCrc;    // Expected checksum of the corresponding 0.pamt file
    }

}

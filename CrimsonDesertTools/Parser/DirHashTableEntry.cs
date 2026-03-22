namespace CrimsonDesertTools.Parser
{
    public struct DirHashTableEntry
    {
        public uint FolderHash;
        public uint NameOffset;
        public uint FileStartIndex;
        public uint FileCount;
    }
}
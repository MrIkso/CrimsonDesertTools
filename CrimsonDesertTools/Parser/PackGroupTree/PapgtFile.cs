namespace CrimsonDesertTools.Parser.PackGroupTree
{
    /// <summary>
    /// Data structure representing the parsed PackGroupTree file.
    /// </summary>
    public class PapgtFile
    {
        public PapgtHeader Header;
        public List<PackMetaInfo> GroupInfos = new List<PackMetaInfo>();
        public List<string> FolderNames = new List<string>();

        /// <summary>
        /// Helper to get a group's folder name by its index.
        /// </summary>
        public string GetGroupName(int index) => index >= 0 && index < FolderNames.Count ? FolderNames[index] : "unknown";
    }

}

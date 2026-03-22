namespace CrimsonDesertTools.Parser
{
    public class PamtFile
    {
        // Checksum of the data following the header
        public uint HeaderCrc;
        public uint Unknown;
        
        public List<PazInfo> PazFiles = new List<PazInfo>();
        public byte[] DirectoryData;
        public byte[] FileNames;
  
        public List<DirHashTableEntry> Folders = new List<DirHashTableEntry>();
       
        public List<FileInfo> Files = new List<FileInfo>();
    }
}
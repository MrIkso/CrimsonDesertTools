namespace CrimsonDesertTools.Parser
{
    public class PamtReader
    {
        public PamtFile Read(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException();
            }

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                PamtFile pamt = new PamtFile();

                // read header
                pamt.HeaderCrc = br.ReadUInt32(); // used for integrity checks, calculated only for data, skip header (12 bytes)
                uint pazCount = br.ReadUInt32();
                pamt.Unknown = br.ReadUInt32();

                // read paz achive info
                for (int i = 0; i < pazCount; i++)
                {
                    pamt.PazFiles.Add(new PazInfo
                    {
                        Index = br.ReadUInt32(),
                        Crc = br.ReadUInt32(), // used for integrity checks, all block size
                        FileSize = br.ReadUInt32()
                    });
                }

                // read direcrory table
                uint dirBlockSize = br.ReadUInt32();
                byte[] dirData = br.ReadBytes((int)dirBlockSize);
                pamt.DirectoryData = dirData;

                // read name tale
                uint fileNameBlockSize = br.ReadUInt32();
                byte[] fileNameData = br.ReadBytes((int)fileNameBlockSize);
                pamt.FileNames = fileNameData;

                // read hash table
                uint hashCount = br.ReadUInt32();
                for (int i = 0; i < hashCount; i++)
                {
                    pamt.Folders.Add(new DirHashTableEntry
                    {
                        FolderHash = br.ReadUInt32(),
                        NameOffset = br.ReadUInt32(),
                        FileStartIndex = br.ReadUInt32(),
                        FileCount = br.ReadUInt32()
                    });
                }

                // read file info
                uint filesCount = br.ReadUInt32();
                for (int i = 0; i < filesCount; i++)
                {
                    pamt.Files.Add(new FileInfo
                    {
                        NameOffset = br.ReadUInt32(),
                        Offset = br.ReadUInt32(),
                        CompressSize = br.ReadUInt32(),
                        DecompressSize = br.ReadUInt32(),
                        PazIndex = br.ReadUInt16(),
                        Flags = br.ReadUInt16()
                    });
                }

                return pamt;
            }
        }
    }
}
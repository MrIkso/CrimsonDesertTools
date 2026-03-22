using CrimsonDesertTools.Utils;
using System.Text;

namespace CrimsonDesertTools.Parser
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

    /// <summary>
    /// Contains metadata information for a specific .pamt file.
    /// </summary>
    public struct PackMetaInfo
    {
        public uint FolderHash; // Hash of the folder name
        public uint NameOffset; // Offset within the string block
        public uint PamtCrc;    // Expected checksum of the corresponding 0.pamt file
    }

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
        public string GetGroupName(int index) => (index >= 0 && index < FolderNames.Count) ? FolderNames[index] : "unknown";
    }

    public class PapgtReader
    {
        /// <summary>
        /// Reads and parses a .papgt file.
        /// </summary>
        /// <param name="filePath">Path to the 0.papgt file.</param>
        /// <param name="verifyChecksum">If true, validates the file integrity using PaChecksum.</param>
        public PapgtFile Read(string filePath, bool verifyChecksum = true)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Could not find .papgt file", filePath);

            byte[] rawData = File.ReadAllBytes(filePath);

            if (verifyChecksum)
            {
                ValidateFileIntegrity(rawData);
            }

            using (var ms = new MemoryStream(rawData))
            using (var br = new BinaryReader(ms))
            {
                var papgt = new PapgtFile();

                // read header
                papgt.Header = new PapgtHeader
                {
                    Unknown = br.ReadUInt32(),
                    FileCrc = br.ReadUInt32(),
                    GroupCount = br.ReadByte(),
                    Unknown1 = br.ReadUInt16(),
                    Pad = br.ReadByte()
                };

                // read PackMetaInfo array
                for (int i = 0; i < papgt.Header.GroupCount; i++)
                {
                    papgt.GroupInfos.Add(new PackMetaInfo
                    {
                        FolderHash = br.ReadUInt32(),
                        NameOffset = br.ReadUInt32(),
                        PamtCrc = br.ReadUInt32()
                    });
                }

                // read String Block Size
                uint stringBlockSize = br.ReadUInt32();

                // read Folder Names
                // Based on the dump, each folder is 4 chars + null terminator = 5 bytes.
                for (int i = 0; i < papgt.Header.GroupCount; i++)
                {
                    byte[] nameBytes = br.ReadBytes(5);
                    // Filter out null characters
                    string name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0');
                    papgt.FolderNames.Add(name);
                }

                return papgt;
            }
        }

        /// <summary>
        /// Validates the internal checksum of the .papgt file.
        /// </summary>
        private void ValidateFileIntegrity(byte[] data)
        {
            if (data.Length < 12)
                throw new Exception("File is too small to be a valid .papgt");

            uint expectedCrc = BitConverter.ToUInt32(data, 4);

            // The payload starts at offset 12
            int payloadSize = data.Length - 12;
            byte[] payload = new byte[payloadSize];
            Buffer.BlockCopy(data, 12, payload, 0, payloadSize);

            uint actualCrc = PaChecksum.Calculate(payload);

            if (actualCrc != expectedCrc)
            {
                throw new Exception($"Integrity check failed! Expected: 0x{expectedCrc:X8}, Got: 0x{actualCrc:X8}. The PackGroupTree file might be corrupted.");
            }
        }
    }

}

using CrimsonDesertTools.Parser;
using CrimsonDesertTools.Utils;
using System.Text;

namespace CrimsonDesertTools.Packer
{
    public class PazArchiveEntry
    {
        public string DirectoryPath { get; set; }
        public string FileName { get; set; }
        public int Offset { get; set; }
        public int CompressSize { get; set; }
        public int DecompressSize { get; set; }
        public ushort PazIndex { get; set; }
        public ushort Flags { get; set; }
    }

    public class PazArchive
    {
        public uint CRC { get; set; }
        public int Size { get; set; }
        public List<PazArchiveEntry> entries { get; set; }
    }

    public class ArchiveGenerator
    {
        private readonly string _inputRootDir;

        public ArchiveGenerator(string rootDir)
        {
            _inputRootDir = rootDir;
        }

        public void PackArchive(string saveDir)
        {
            PazArchive pazArchive = GenerateArchive(saveDir, 0);
            CreatePackMeta(saveDir, pazArchive);
        }

        private ushort CalculateFlags(EncryptionMethod enc, CompressionMethod comp)
        {
            return (ushort)((((byte)enc) << 4) | ((byte)comp));
        }

        private PazArchive GenerateArchive(string saveDir, ushort archiveIndex)
        {
            string pazPath = Path.Combine(saveDir, $"{archiveIndex}.paz");
            var archiveEntries = new List<PazArchiveEntry>();

            if (!Directory.Exists(pazPath))
            {
                Directory.CreateDirectory(saveDir);
            }

            string[] allFiles = Directory.GetFiles(_inputRootDir, "*.*", SearchOption.AllDirectories);

            using (FileStream fs = new FileStream(pazPath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                foreach (string fullFilePath in allFiles)
                {
                    string relativeFile = Path.GetRelativePath(_inputRootDir, fullFilePath);
                    string dirPath = Path.GetDirectoryName(relativeFile)?.Replace("\\", "/") ?? "";
                    string fileName = Path.GetFileName(relativeFile);

                    byte[] originalData = File.ReadAllBytes(fullFilePath);
                    byte[] dataToWrite = originalData;

                    var entry = new PazArchiveEntry
                    {
                        DirectoryPath = dirPath,
                        FileName = fileName,
                        Offset = (int)bw.BaseStream.Position,
                        DecompressSize = originalData.Length,
                        CompressSize = dataToWrite.Length,
                        PazIndex = archiveIndex,
                        Flags = CalculateFlags(EncryptionMethod.None, CompressionMethod.Partial)
                    };

                    bw.Write(dataToWrite);

                    // align data by 16 bytes
                    long currentPos = bw.BaseStream.Position;
                    int padding = (int)((16 - (currentPos % 16)) % 16);
                    if (padding > 0)
                    {
                        bw.Write(new byte[padding]);
                    }
                    archiveEntries.Add(entry);
                }
            }


            byte[] pazArchiveData = File.ReadAllBytes(pazPath);
            uint pazHash = PaChecksum.Calculate(pazArchiveData);
            Console.WriteLine($"Archive {pazPath} created");
            Console.WriteLine($"Archive Checksum: 0x{pazHash:X8}");

            PazArchive pazArchive = new PazArchive();
            pazArchive.CRC = pazHash;
            pazArchive.Size = pazArchiveData.Length;
            pazArchive.entries = archiveEntries;
            return pazArchive;
        }


        // crate pack meta file .pamt from one .paz archive
        private void CreatePackMeta(string saveDir, PazArchive archive)
        {
            string pamtPath = Path.Combine(saveDir, "0.pamt");

            var sortedFiles = archive.entries.OrderBy(e => e.DirectoryPath).ToList();

            using (MemoryStream bodyMs = new MemoryStream())
            using (BinaryWriter bodyBw = new BinaryWriter(bodyMs))
            {
                // write paz info
                bodyBw.Write((uint)archive.entries[0].PazIndex);
                bodyBw.Write(archive.CRC);
                bodyBw.Write((uint)archive.Size);

                // buid dir names trie block
                var dirTrie = new TrieBuilder();
               
                var uniquePaths = sortedFiles.Select(f => f.DirectoryPath).Distinct().OrderBy(p => p);
                foreach (var path in uniquePaths)
                {
                    dirTrie.AddPath(path, true);
                }
                byte[] dirData = dirTrie.GetAsByteArray();
                bodyBw.Write((uint)dirData.Length);
                bodyBw.Write(dirData);

                // write file names block
                var fileTrie = new TrieBuilder();
                var fileNameOffsets = new List<uint>();
                foreach (var file in sortedFiles)
                {
                    fileNameOffsets.Add(fileTrie.AddPath(file.FileName, false));
                }
                byte[] fileData = fileTrie.GetAsByteArray();
                bodyBw.Write((uint)fileData.Length);
                bodyBw.Write(fileData);

                // calculate dir tree
                uint globalFileIdx = 0;
                foreach (var node in dirTrie.RegisteredNodes)
                {
                    var filesInThisDir = sortedFiles.Where(f => f.DirectoryPath == node.FullPath).ToList();
                    node.FileCount = (uint)filesInThisDir.Count;
                    node.FileStartIndex = globalFileIdx;

                    globalFileIdx += node.FileCount;
                }

                // write dir table
                bodyBw.Write((uint)dirTrie.RegisteredNodes.Count);
                foreach (var node in dirTrie.RegisteredNodes)
                {
                    uint folderHash = PaChecksum.Calculate(Encoding.UTF8.GetBytes(node.FullPath));
                    bodyBw.Write(folderHash);
                    bodyBw.Write(node.Offset);
                    bodyBw.Write(node.FileStartIndex);
                    bodyBw.Write(node.FileCount);
                }

                // write fileInfo table
                bodyBw.Write((uint)sortedFiles.Count);
                for (int i = 0; i < sortedFiles.Count; i++)
                {
                    var file = sortedFiles[i];
                    bodyBw.Write(fileNameOffsets[i]);
                    bodyBw.Write(file.Offset);
                    bodyBw.Write(file.CompressSize);
                    bodyBw.Write(file.DecompressSize);
                    bodyBw.Write(file.PazIndex);
                    bodyBw.Write(file.Flags);
                }

                // calculate body hash checksum
                byte[] finalBody = bodyMs.ToArray();
                uint headerCrc = PaChecksum.Calculate(finalBody);

                // write all data and header
                using (FileStream fs = new FileStream(pamtPath, FileMode.Create))
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    bw.Write(headerCrc);
                    bw.Write((uint)1);
                    bw.Write((uint)0); // Seed
                    bw.Write(finalBody);
                }

                Console.WriteLine($"Pack metadata created: {pamtPath}");
            }
        }
    }
}

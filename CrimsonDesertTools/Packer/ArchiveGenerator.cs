using CrimsonDesertTools.Parser;
using CrimsonDesertTools.Parser.PackGroupTree;
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
        public List<PazArchiveEntry> Entries { get; set; }
    }

    public class ArchiveGenerator
    {
        private readonly string _gameRootDir;

        public ArchiveGenerator(string gameRootDir)
        {
            _gameRootDir = gameRootDir;
        }

        public void PackArchive(string unpackResourceRootDir)
        {
            string folderName = "0254";
            string saveDir = Path.Combine(_gameRootDir, folderName);

            Directory.CreateDirectory(saveDir);

            PazArchive pazArchive = GenerateArchive(saveDir, unpackResourceRootDir, 0);
            uint pamtCrc = CreatePackMeta(saveDir, pazArchive);

            string papgtFile = Path.Combine(_gameRootDir, "meta", "0.papgt");
            string backupDir = Path.Combine(_gameRootDir, "backup");
            string backupFile = Path.Combine(backupDir, "0.papgt");

            Directory.CreateDirectory(backupDir);

            if (!File.Exists(backupFile))
            {
                File.Copy(papgtFile, backupFile);
                Console.WriteLine("Vanilla 0.papgt saved to backup folder.");
            }
            else
            {
                string currentBackup = Path.Combine(backupDir, "0.papgt.last");
                File.Copy(papgtFile, currentBackup, true); 
            }

            PatchPapgt(papgtFile, pamtCrc, folderName);
            Console.WriteLine($"Mod archive located in: {saveDir}");
        }

        private ushort CalculateFlags(EncryptionMethod enc, CompressionMethod comp)
        {
            return (ushort)((((byte)enc) << 4) | ((byte)comp));
        }

        private PazArchive GenerateArchive(string saveDir, string resourcesDir, ushort archiveIndex)
        {
            string pazPath = Path.Combine(saveDir, $"{archiveIndex}.paz");
            var archiveEntries = new List<PazArchiveEntry>();

            if (!Directory.Exists(pazPath))
            {
                Directory.CreateDirectory(saveDir);
            }

            string[] allFiles = Directory.GetFiles(resourcesDir, "*.*", SearchOption.AllDirectories);

            using (FileStream fs = new FileStream(pazPath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                foreach (string fullFilePath in allFiles)
                {
                    string relativeFile = Path.GetRelativePath(resourcesDir, fullFilePath);
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
                        Flags = CalculateFlags(EncryptionMethod.None, CompressionMethod.None)
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
            pazArchive.Entries = archiveEntries;
            return pazArchive;
        }


        // create pack meta file .pamt from one .paz archive
        private uint CreatePackMeta(string saveDir, PazArchive archive)
        {
            string pamtPath = Path.Combine(saveDir, "0.pamt");

            var sortedFiles = archive.Entries.OrderBy(e => e.DirectoryPath).ToList();

            using (MemoryStream bodyMs = new MemoryStream())
            using (BinaryWriter bodyBw = new BinaryWriter(bodyMs))
            {
                // write paz info
                bodyBw.Write((uint)archive.Entries[0].PazIndex);
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
                    bw.Write((uint)1); // paz count
                    bw.Write((uint)0); // seed, 0 if data not encrypted
                    bw.Write(finalBody);
                }

                Console.WriteLine($"Pack metadata created: {pamtPath}");
                return headerCrc;
            }
        }


        private void PatchPapgt(string papgtPath, uint modPamtHash, string newFolderName = "0254")
        {
            PapgtReader reader = new PapgtReader();
            PapgtFile papgt = reader.Read(papgtPath);

            if (papgt.Header.GroupCount >= 255)
            {
                throw new Exception("Maximum group count (255) reached. Cannot add more groups.");
            }

            if (papgt.FolderNames.Contains(newFolderName))
            {
                Console.WriteLine($"Group {newFolderName} already exists. Updating its CRC...");
                int idx = papgt.FolderNames.IndexOf(newFolderName);
                var info = papgt.GroupInfos[idx];
                info.PamtCrc = modPamtHash;
                papgt.GroupInfos[idx] = info;
            }
            else
            {
                Console.WriteLine($"Adding new group: {newFolderName}");
                papgt.FolderNames.Add(newFolderName);
                uint newOffset = (uint)(papgt.GroupInfos.Count * 5);

                papgt.GroupInfos.Add(new PackMetaInfo
                {
                    IsOptional = 0,
                    PackGroupLanguageType = PackGroupLanguageType.ALL,
                    Zero = 0,
                    NameOffset = newOffset,
                    PamtCrc = modPamtHash
                });

                var header = papgt.Header;
                header.GroupCount++;
                papgt.Header = header;
            }

            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                // skip header
                bw.Write(new byte[12]);

                // write meta info block
                foreach (var info in papgt.GroupInfos)
                {
                    bw.Write(info.IsOptional);
                    bw.Write((ushort)info.PackGroupLanguageType);
                    bw.Write(info.Zero);
                    bw.Write(info.NameOffset);
                    bw.Write(info.PamtCrc);
                }

                // calculate new  folders sting block size
                uint stringSize = (uint)(papgt.FolderNames.Count * 5);
                bw.Write(stringSize);

                // write folders bloks
                foreach (var name in papgt.FolderNames)
                {
                    byte[] nameBytes = new byte[5];
                    byte[] ascii = Encoding.ASCII.GetBytes(name);
                    Array.Copy(ascii, nameBytes, Math.Min(ascii.Length, 4));
                    bw.Write(nameBytes);
                }

                byte[] fullFile = ms.ToArray();
                int payloadSize = fullFile.Length - 12;
                byte[] payload = new byte[payloadSize];
                Buffer.BlockCopy(fullFile, 12, payload, 0, payloadSize);

                uint newFileCrc = PaChecksum.Calculate(payload);

                // write data
                using (FileStream fs = new FileStream(papgtPath, FileMode.Create))
                using (BinaryWriter finalBw = new BinaryWriter(fs))
                {
                    finalBw.Write(papgt.Header.Unknown);
                    finalBw.Write(newFileCrc);
                    finalBw.Write(papgt.Header.GroupCount);
                    finalBw.Write(papgt.Header.Unknown1);
                    finalBw.Write(papgt.Header.Pad);
                    finalBw.Write(fullFile, 12, fullFile.Length - 12);
                }
            }

            Console.WriteLine($"0.papgt successfully patched!");
        }
    }
}

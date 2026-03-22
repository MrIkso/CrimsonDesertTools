using CrimsonDesertTools.Utils;

namespace CrimsonDesertTools.Parser
{
    public static class Unpacker
    {
        public static void ExtractAll(PamtFile meta, VfsPathResolver resolver, VfsPathResolver dirPathResolver, string rootPazPath, string outputDir)
        {
            Console.WriteLine($"Starting extraction to: {outputDir}");
            int successCount = 0;

            var ranges = meta.Folders.Select(f => new {
                Start = f.FileStartIndex,
                End = f.FileStartIndex + f.FileCount,
                Path = dirPathResolver.GetFullName(f.NameOffset)
            }).ToList();

            for (int i = 0; i < meta.Files.Count; i++)
            {
                var file = meta.Files[i];
                string fileName = resolver.GetFullName(file.NameOffset);

                string folderPath = ranges.FirstOrDefault(r => i >= r.Start && i < r.End)?.Path ?? "";
                string fullPath= Path.Combine(folderPath, fileName).Replace("\\", "/");

                string fullOutputPath = Path.Combine(outputDir, fullPath);

                Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath));

                string pazFile = Path.Combine(rootPazPath, $"{file.PazIndex}.paz");
                if (!File.Exists(pazFile))
                {
                    Console.WriteLine($"File not found: {pazFile}, igrrorting..");
                    continue;
                }

                if (File.Exists(pazFile))
                {
                    try
                    {
                        ExtractFile(meta, pazFile, file, fileName, fullOutputPath);
                        successCount++;
                        if (successCount % 100 == 0)
                        {
                            Console.WriteLine($"Extracted {successCount} files...");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to extract {fileName}: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Missing archive: {file.PazIndex}.paz for file {fileName}");
                }
            }

            Console.WriteLine($"Finished! Total extracted: {successCount}");
        }

        private static void ExtractFile(PamtFile meta, string pazPath, FileInfo info, string relativePath, string outPath )
        {
            using (var fs = new FileStream(pazPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.Seek(info.Offset, SeekOrigin.Begin);
                byte[] data = new byte[info.CompressSize];
                fs.Read(data, 0, (int)info.CompressSize);

            
                byte[] finalData = ProcessEntry(
                    data,
                    relativePath,
                    (int)info.DecompressSize,
                    info.Encryption,
                    info.Compression
                );

                File.WriteAllBytes(outPath, finalData);
            }
        }

        public static byte[] ProcessEntry(byte[] data, string fullPath, int originalSize, EncryptionMethod enc, CompressionMethod comp)
        {
            byte[] result = data;

            if (enc == EncryptionMethod.ChaCha20)
            {
                result = CrimsonCrypto.Decrypt(data, fullPath);
            }

            if (comp == CompressionMethod.LZ4)
            {
                byte[] decoded = new byte[originalSize];
                decoded = CompressionUtils.DecompressLZ4(result, originalSize);
                return decoded;
            }

            if (comp == CompressionMethod.Zlib) {
                byte[] decoded = new byte[originalSize];
                decoded = CompressionUtils.DecompressZlib(result);
                return decoded;
            }

            if (comp == CompressionMethod.Partial)
            {
                return CompressionUtils.DecompressPartial(result, originalSize);
            }

            return result;
        }

    }
}

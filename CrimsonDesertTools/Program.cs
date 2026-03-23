using CrimsonDesertTools.Parser;
using CrimsonDesertTools.Parser.PackGroupTree;
using System.Text;

namespace CrimsonDesertTools
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            if (args.Length < 2)
            {
                PrintUsage();
                return;
            }

            string mode = args[0].ToLower();
            string filePath = args[1];

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[Error] File not found: {filePath}");
                return;
            }

            try
            {
                switch (mode)
                {
                    case "info":
                        ExecuteInfo(filePath);
                        break;

                    case "unpack":
                        string outputDir = args.Length > 2 ? args[2] : null;
                        ExecuteUnpack(filePath, outputDir);
                        break;

                    case "print_groups":
                        ExecutePrintGroups(filePath);
                        break;

                    default:
                        PrintUsage();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Critical Error] {ex.Message}");
#if DEBUG
                Console.WriteLine(ex.StackTrace);
#endif
            }
        }

        #region Commands

        private static void ExecuteInfo(string filePath)
        {
            var (meta, resolver, dirResolver) = LoadPamtContext(filePath);
            string outPath = Path.Combine(Path.GetDirectoryName(filePath) ?? "", "meta_info.txt");

            SaveInfo(meta, resolver, dirResolver, outPath);
        }

        private static void ExecuteUnpack(string filePath, string? outputDir)
        {
            var (meta, resolver, dirResolver) = LoadPamtContext(filePath);
            string pamtDir = Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? "";
            outputDir ??= Path.Combine(pamtDir, "extracted");

            Console.WriteLine($"[*] Total files in meta: {meta.Files.Count}");
            Console.WriteLine($"[*] Output directory: {outputDir}");

            Unpacker.ExtractAll(meta, resolver, dirResolver, pamtDir, outputDir);
        }

        private static void ExecutePrintGroups(string papgtPath)
        {
            var papgtReader = new PapgtReader();
            var rootMap = papgtReader.Read(papgtPath);

            string? metaRootDir = Path.GetDirectoryName(papgtPath);
            string? gameDir = Path.GetDirectoryName(metaRootDir);

            if (gameDir == null)
                throw new Exception("Invalid directory structure for papgt");

            Console.WriteLine($"{"Group",-8} | {"PAMT Status",-15} | {"PAMT CRC (Expected)",-20}");
            Console.WriteLine(new string('-', 55));

            foreach (var info in rootMap.GroupInfos)
            {
                string groupName = rootMap.GetGroupName(rootMap.GroupInfos.IndexOf(info));
                string pamtPath = Path.Combine(gameDir, groupName, "0.pamt");
                string status = File.Exists(pamtPath) ? "OK" : "Missing";

                Console.WriteLine($"{groupName,-8} | {status,-15} | 0x{info.PamtCrc:X8}");

                if (File.Exists(pamtPath))
                {
                    try
                    {
                        var pamt = new PamtReader().Read(pamtPath);
                        bool crcMatch = pamt.HeaderCrc == info.PamtCrc;
                        string crcStatus = crcMatch ? "MATCH" : "MISMATCH!";
                        Console.WriteLine($"         └─ File: {groupName}/0.pamt |Language: {info.PackGroupLanguageType}| Header CRC: 0x{pamt.HeaderCrc:X8} ({crcStatus})");
                    }
                    catch {
                        Console.WriteLine($"         └─ [Error] Failed to read PAMT"); 
                    }
                }
            }
        }

        #endregion

        #region Helpers

        private static (PamtFile meta, VfsPathResolver fileResolver, VfsPathResolver dirResolver) LoadPamtContext(string filePath)
        {
            var reader = new PamtReader();
            var meta = reader.Read(filePath);
            var fileResolver = new VfsPathResolver(meta.FileNames);
            var dirResolver = new VfsPathResolver(meta.DirectoryData);
            return (meta, fileResolver, dirResolver);
        }

        static void SaveInfo(PamtFile meta, VfsPathResolver resolver, VfsPathResolver dirPathResolver, string outPath)
        {
            Console.WriteLine("[*] Collecting metadata information...");

            var folderRanges = meta.Folders.Select(f => new
            {
                Start = f.FileStartIndex,
                End = f.FileStartIndex + f.FileCount,
                Path = dirPathResolver.GetFullName(f.NameOffset)
            }).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("ID|Name|Directory|Archive|Offset|Size_Packed|Size_Unpacked|Encryption|Compression");

            for (int i = 0; i < meta.Files.Count; i++)
            {
                var file = meta.Files[i];
                string folder = folderRanges.FirstOrDefault(r => i >= r.Start && i < r.End)?.Path ?? "root";
                string name = resolver.GetFullName(file.NameOffset);

                sb.AppendLine($"{i}|{name}|{folder}|{file.PazIndex}.paz|0x{file.Offset:X}|{file.CompressSize}|{file.DecompressSize}|{file.Encryption}|{file.Compression}");
            }

            File.WriteAllText(outPath, sb.ToString());
            Console.WriteLine($"[+] Success! Info saved to: {outPath}");
        }

        static void PrintUsage()
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("          CrimsonDesertTools");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine("\nUsage:");
            Console.WriteLine("  CrimsonDesertTools.exe <mode> <file_path> [output_dir]");
            Console.WriteLine("\nModes:");
            Console.WriteLine("  info          - Generates 'meta_info.txt' with all file details.");
            Console.WriteLine("  unpack        - Decrypts and extracts files from .paz archives.");
            Console.WriteLine("  print_groups  - Displays the 0.papgt structure and verifies PAMT hashes.");
            Console.WriteLine("\nExamples:");
            Console.WriteLine("  CrimsonDesertTools.exe info 0005/0.pamt");
            Console.WriteLine("  CrimsonDesertTools.exe unpack 0030/0.pamt ./extracted_data");
            Console.WriteLine("  CrimsonDesertTools.exe print_groups meta/0.papgt");
            Console.WriteLine("\n" + new string('=', 60));
        }

        #endregion
    }
}
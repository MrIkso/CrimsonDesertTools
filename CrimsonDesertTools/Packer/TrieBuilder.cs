using System.Text;

namespace CrimsonDesertTools.Packer
{
    public class TrieNodeInfo
    {
        public string FullPath { get; set; }
        public uint Offset { get; set; }
        public uint FileStartIndex { get; set; }
        public uint FileCount { get; set; }
    }

    public class TrieBuilder
    {
        private readonly MemoryStream _ms = new MemoryStream();
        private readonly BinaryWriter _bw;
        private readonly Dictionary<(uint, string), uint> _nodes = new();
     
        public List<TrieNodeInfo> RegisteredNodes = new();

        public TrieBuilder() => _bw = new BinaryWriter(_ms);

        public uint AddPath(string path, bool isDirectory)
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            uint currentParent = 0xFFFFFFFF;
            string currentFullPath = "";

            for (int i = 0; i < segments.Length; i++)
            {
                string part = segments[i];

                if (i > 0)
                    part = "/" + part;

                currentFullPath += part;

                if (_nodes.TryGetValue((currentParent, part), out uint existingOffset))
                {
                    currentParent = existingOffset;
                }
                else
                {
                    uint newOffset = (uint)_ms.Position;
                    _bw.Write(currentParent);
                    byte[] bytes = Encoding.UTF8.GetBytes(part);
                    _bw.Write((byte)bytes.Length);
                    _bw.Write(bytes);

                    _nodes[(currentParent, part)] = newOffset;

                    if (isDirectory)
                    {
                        RegisteredNodes.Add(new TrieNodeInfo
                        {
                            FullPath = currentFullPath,
                            Offset = newOffset
                        });
                    }

                    currentParent = newOffset;
                }
            }
            return currentParent;
        }

        public byte[] GetAsByteArray() => _ms.ToArray();
    }
}

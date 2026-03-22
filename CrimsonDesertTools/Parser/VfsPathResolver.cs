using System.Text;

namespace CrimsonDesertTools.Parser
{
    public class VfsPathResolver
    {
        private byte[] _nameBlock;

        public VfsPathResolver(byte[] nameBlock)
        {
            _nameBlock = nameBlock;
        }

        // same as sub_140EAB040
        public string GetFullName(uint offset)
        {
            if (offset == uint.MaxValue || offset >= _nameBlock.Length)
                return string.Empty;

            List<string> pathParts = new List<string>();
            uint currentOffset = offset;

            while (currentOffset != uint.MaxValue)
            {
                int pos = (int)currentOffset;

                if (pos + 5 > _nameBlock.Length)
                    break;
                uint parentOffset = BitConverter.ToUInt32(_nameBlock, pos);

                byte len = _nameBlock[pos + 4];

                if (pos + 5 + len > _nameBlock.Length)
                    break;

                string part = Encoding.UTF8.GetString(_nameBlock, pos + 5, len);

                pathParts.Add(part);
                currentOffset = parentOffset;

                if (pathParts.Count > 255)
                    break;
            }

            pathParts.Reverse();
            return string.Join("", pathParts);
        }
    }
}

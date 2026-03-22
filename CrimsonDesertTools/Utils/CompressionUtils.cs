using K4os.Compression.LZ4;
using System.IO.Compression;

namespace CrimsonDesertTools.Utils
{
    public static class CompressionUtils
    {
        public static byte[] DecompressLZ4(byte[] compressed, int decompressedSize)
        {
            byte[] target = new byte[decompressedSize];
            int decoded = LZ4Codec.Decode(
                compressed, 0, compressed.Length,
                target, 0, target.Length);

            if (decoded < 0)
                throw new Exception("LZ4 decompression failed.");

            return target;
        }

        public static byte[] DecompressZlib(byte[] compressed)
        {
            using (var msInput = new MemoryStream(compressed))
            using (var zlibStream = new ZLibStream(msInput, CompressionMode.Decompress))
            using (var msOutput = new MemoryStream())
            {
                zlibStream.CopyTo(msOutput);
                return msOutput.ToArray();
            }
        }

        public static byte[] DecompressPartial(byte[] input, int decompressedSize)
        {
            if (input.Length < 3 || (input[0] != 0x01 && input[0] != 0x02))
                return input;

            byte[] output = new byte[decompressedSize];
            int inPtr = (input[0] == 0x02) ? 9 : 3;
            int outPtr = 0;
            uint control = 1;

            try
            {
                while (outPtr < decompressedSize)
                {
                    if (control == 1)
                    {
                        control = BitConverter.ToUInt32(input, inPtr);
                        inPtr += 4;
                    }

                    if ((control & 1) != 0) // Match
                    {
                        control >>= 1;
                        uint token = BitConverter.ToUInt32(input, inPtr);
                        uint length, distance;

                        if ((token & 3) == 3)
                        {
                            if ((token & 0x7F) == 3)
                            {
                                distance = token >> 15;
                                length = ((token >> 7) & 0xFF) + 3;
                                inPtr += 4;
                            }
                            else
                            {
                                distance = (token >> 7) & 0x1FFFF;
                                length = ((token >> 2) & 0x1F) + 2;
                                inPtr += 3;
                            }
                        }
                        else if ((token & 3) == 2)
                        {
                            distance = (uint)BitConverter.ToUInt16(input, inPtr) >> 6;
                            length = ((token >> 2) & 0xF) + 3;
                            inPtr += 2;
                        }
                        else
                        {
                            distance = (token & 0xFF) >> 2;
                            length = 3;
                            inPtr += 1;
                        }

                        for (int i = 0; i < length; i++)
                        {
                            output[outPtr] = output[outPtr - (int)distance];
                            outPtr++;
                        }
                    }
                    else // Literal
                    {
                        control >>= 1;
                        output[outPtr++] = input[inPtr++];
                    }
                }
            }
            catch { }

            return output;
        }
    }

}

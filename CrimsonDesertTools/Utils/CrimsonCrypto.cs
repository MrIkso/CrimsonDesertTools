using CSChaCha20;
using System.Runtime.CompilerServices;
using System.Text;

namespace CrimsonDesertTools.Utils
{
    // bases on this research, credit lazorr410
    // https://github.com/lazorr410/crimson-desert-unpacker/blob/main/PAZ_DECRYPTION.md
    public static class CrimsonCrypto
    {
        private const uint HASH_INITVAL = 0x000C5EDE;
        private const uint IV_XOR = 0x60616263;

        private static readonly uint[] XOR_DELTAS = {
            0x00000000, 0x0A0A0A0A, 0x0C0C0C0C, 0x06060606,
            0x0E0E0E0E, 0x0A0A0A0A, 0x06060606, 0x02020202
        };

        public static byte[] Decrypt(byte[] ciphertext, string filename)
        {
            var (key, iv) = DeriveKeyIv(filename);

            uint initialCounter = BitConverter.ToUInt32(iv, 0);
            byte[] nonce = new byte[12];
            Buffer.BlockCopy(iv, 4, nonce, 0, 12);

            using (var chacha = new ChaCha20(key, nonce, initialCounter))
            {
                byte[] plaintext = new byte[ciphertext.Length];
                chacha.DecryptBytes(plaintext, ciphertext);
                return plaintext;
            }
        }

        private static (byte[] key, byte[] iv) DeriveKeyIv(string filename)
        {
            string basename = Path.GetFileName(filename).ToLower();
            uint seed = JenkinsHash(Encoding.UTF8.GetBytes(basename), HASH_INITVAL);

            byte[] iv = new byte[16];
            byte[] seedBytes = BitConverter.GetBytes(seed);
            for (int i = 0; i < 4; i++) Buffer.BlockCopy(seedBytes, 0, iv, i * 4, 4);

            byte[] key = new byte[32];
            uint keyBase = seed ^ IV_XOR;
            for (int i = 0; i < 8; i++)
            {
                uint keyPart = keyBase ^ XOR_DELTAS[i];
                Buffer.BlockCopy(BitConverter.GetBytes(keyPart), 0, key, i * 4, 4);
            }

            return (key, iv);
        }

        private static uint JenkinsHash(byte[] data, uint initval)
        {
            uint length = (uint)data.Length;
            uint a, b, c;
            a = b = c = 0xDEADBEEF + length + initval;

            int off = 0;
            int len = (int)length;

            while (len > 12)
            {
                a += BitConverter.ToUInt32(data, off);
                b += BitConverter.ToUInt32(data, off + 4);
                c += BitConverter.ToUInt32(data, off + 8);

                a -= c;
                a ^= Rot(c, 4); c += b;
                b -= a;
                b ^= Rot(a, 6); a += c;
                c -= b;
                c ^= Rot(b, 8); b += a;
                a -= c;
                a ^= Rot(c, 16); c += b;
                b -= a;
                b ^= Rot(a, 19); a += c;
                c -= b;
                c ^= Rot(b, 4); b += a;

                off += 12;
                len -= 12;
            }

            if (len > 0)
            {
                byte[] tail = new byte[12];
                Buffer.BlockCopy(data, off, tail, 0, len);

                if (len >= 12) 
                    c += BitConverter.ToUInt32(tail, 8);
                else if (len >= 9) 
                    c += BitConverter.ToUInt32(tail, 8) & (0xFFFFFFFF >> (8 * (12 - len)));

                if (len >= 8)
                    b += BitConverter.ToUInt32(tail, 4);
                else if (len >= 5) 
                    b += BitConverter.ToUInt32(tail, 4) & (0xFFFFFFFF >> (8 * (8 - len)));

                if (len >= 4)
                    a += BitConverter.ToUInt32(tail, 0);
                else if (len >= 1)
                    a += BitConverter.ToUInt32(tail, 0) & (0xFFFFFFFF >> (8 * (4 - len)));
            }
            else return c;

            c ^= b; 
            c -= Rot(b, 14);
            a ^= c;
            a -= Rot(c, 11);
            b ^= a;
            b -= Rot(a, 25);
            c ^= b;
            c -= Rot(b, 16);
            a ^= c;
            a -= Rot(c, 4);
            b ^= a;
            b -= Rot(a, 14);
            c ^= b;
            c -= Rot(b, 24);

            return c;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Rot(uint v, int k) => (v << k) | (v >> (32 - k));
    }
}
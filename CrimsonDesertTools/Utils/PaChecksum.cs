using System;
using System.Runtime.CompilerServices;

namespace CrimsonDesertTools.Utils
{
    /// <summary>
    /// Pearl Abyss specific implementation of the Bob Jenkins' Lookup3 hash algorithm.
    /// This is used for data integrity verification files.
    /// </summary>
    public static class PaChecksum
    {
        // Pearl Abyss custom initialization constant
        private const uint PA_MAGIC = 0x2145E233;

        /// <summary>
        /// Calculates the custom hash for the provided byte array.
        /// </summary>
        /// <param name="data">The input data to hash.</param>
        /// <returns>A 32-bit unsigned integer representing the hash/checksum.</returns>
        public static uint Calculate(byte[] data)
        {
            int length = data.Length;
            if (length == 0)
                return 0;


            uint a, b, c;
            // Initialize internal state with data length and PA specific magic constant
            a = b = c = (uint)length - PA_MAGIC;

            int offset = 0;
            int remaining = length;

            // Main loop: Process data in 12-byte blocks
            while (remaining > 12)
            {
                a += BitConverter.ToUInt32(data, offset);
                b += BitConverter.ToUInt32(data, offset + 4);
                c += BitConverter.ToUInt32(data, offset + 8);

                // Mixing step: Bitwise rotation and XOR to shuffle the internal state
                a -= c; a ^= RotateLeft(c, 4); c += b;
                b -= a; b ^= RotateLeft(a, 6); a += c;
                c -= b; c ^= RotateLeft(b, 8); b += a;
                a -= c; a ^= RotateLeft(c, 16); c += b;
                b -= a; b ^= RotateLeft(a, 19); a += c;
                c -= b; c ^= RotateLeft(b, 4); b += a;

                offset += 12;
                remaining -= 12;
            }

            // Handling the tail: Process the remaining 1 to 12 bytes
            if (remaining > 0)
            {
                switch (remaining)
                {
                    case 12: c += (uint)data[offset + 11] << 24; goto case 11;
                    case 11: c += (uint)data[offset + 10] << 16; goto case 10;
                    case 10: c += (uint)data[offset + 9] << 8; goto case 9;
                    case 9: c += data[offset + 8]; goto case 8;
                    case 8: b += (uint)data[offset + 7] << 24; goto case 7;
                    case 7: b += (uint)data[offset + 6] << 16; goto case 6;
                    case 6: b += (uint)data[offset + 5] << 8; goto case 5;
                    case 5: b += data[offset + 4]; goto case 4;
                    case 4: a += (uint)data[offset + 3] << 24; goto case 3;
                    case 3: a += (uint)data[offset + 2] << 16; goto case 2;
                    case 2: a += (uint)data[offset + 1] << 8; goto case 1;
                    case 1: a += data[offset]; break;
                }
            }

            // Finalization step
            // Ensures that every bit of the input data affects every bit of the output hash.
            // This block is a modified version of the standard Jenkins' lookup3 final mix.
            uint v82 = (b ^ c) - RotateLeft(b, 14);
            uint v83 = (a ^ v82) - RotateLeft(v82, 11);
            uint v84 = (v83 ^ b) - RotateRight(v83, 7);
            uint v85 = (v84 ^ v82) - RotateLeft(v84, 16);
            uint v86 = RotateLeft(v85, 4);
            uint v87 = (((v83 ^ v85) - v86) ^ v84) - RotateLeft((v83 ^ v85) - v86, 14);

            return (v87 ^ v85) - RotateRight(v87, 8);

        }


        public static uint Calculate(byte[]data, int offset)
        {
            if (data.Length < offset)
                return 0;

            int payloadSize = data.Length - offset;
            byte[] payload = new byte[payloadSize];
            Buffer.BlockCopy(data, offset, payload, 0, payloadSize);

            uint actualCrc = Calculate(payload);
            return actualCrc;
        }

        /// <summary>
        /// Standard Left Circular Shift (Rotate Left).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint RotateLeft(uint x, int k)
        {
            return (x << k) | (x >> (32 - k));
        }

        /// <summary>
        /// Standard Right Circular Shift (Rotate Right).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint RotateRight(uint x, int k)
        {
            return (x >> k) | (x << (32 - k));
        }
    }
}
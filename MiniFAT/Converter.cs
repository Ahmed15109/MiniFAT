using System;
using System.Text;

namespace MiniFAT
{
    public static class Converter
    {
        public static byte[] StringToBytesAsciiFixed(string s, int fixedLen)
        {
            s ??= "";
            var bytes = Encoding.ASCII.GetBytes(s);
            byte[] result = new byte[fixedLen];
            Buffer.BlockCopy(bytes, 0, result, 0, Math.Min(bytes.Length, fixedLen));
            return result;
        }

        public static string BytesToAsciiStringExact(byte[] bytes) => Encoding.ASCII.GetString(bytes);

        public static byte[] IntArrayToBytes(int[] ints)
        {
            if (ints.Length != FsConstants.FAT_ENTRY_COUNT)
                throw new ArgumentException($"FAT int[] length must be {FsConstants.FAT_ENTRY_COUNT}");

            byte[] bytes = new byte[FsConstants.FAT_BYTES];
            for (int i = 0; i < ints.Length; i++)
            {
                byte[] b = BitConverter.GetBytes(ints[i]);
                Buffer.BlockCopy(b, 0, bytes, i * 4, 4);
            }
            return bytes;
        }

        public static int[] BytesToIntArray(byte[] bytes)
        {
            if (bytes.Length != FsConstants.FAT_BYTES)
                throw new ArgumentException($"FAT bytes must be {FsConstants.FAT_BYTES}");

            int[] ints = new int[FsConstants.FAT_ENTRY_COUNT];
            for (int i = 0; i < ints.Length; i++)
                ints[i] = BitConverter.ToInt32(bytes, i * 4);

            return ints;
        }
    }
}

using System;
using System.Linq;

namespace MiniFAT
{
    public sealed class DirectoryEntry
    {
        public const int SIZE = 32;

        public string Name83 { get; set; } = new string(' ', 11);
        public byte Attribute { get; set; }
        public int FirstCluster { get; set; }
        public int FileSize { get; set; }

        public bool IsDirectory => Attribute == FsConstants.ATTR_DIR;
        public bool IsFile => Attribute == FsConstants.ATTR_FILE;

        public byte[] Serialize32()
        {
            byte[] b = new byte[SIZE];

            byte[] nameBytes = Converter.StringToBytesAsciiFixed(Name83, 11);
            Buffer.BlockCopy(nameBytes, 0, b, 0, 11);

            b[11] = Attribute;

            Buffer.BlockCopy(BitConverter.GetBytes(FirstCluster), 0, b, 12, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(FileSize), 0, b, 16, 4);

            return b;
        }

        public static DirectoryEntry? Deserialize32(byte[] bytes, int offset)
        {
            if (bytes.Length - offset < SIZE) return null;

            if (bytes[offset] == 0x00) return null;
            if (bytes[offset] == 0xE5) return null;

            string name83 = Converter.BytesToAsciiStringExact(bytes.Skip(offset).Take(11).ToArray());
            byte attr = bytes[offset + 11];
            int first = BitConverter.ToInt32(bytes, offset + 12);
            int size = BitConverter.ToInt32(bytes, offset + 16);

            return new DirectoryEntry
            {
                Name83 = name83,
                Attribute = attr,
                FirstCluster = first,
                FileSize = size
            };
        }

        public string PrettyName => DirectoryNaming.Parse8Dot3Name(Name83);
    }
}

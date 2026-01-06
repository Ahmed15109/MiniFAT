using System;

namespace MiniFAT
{
    public static class DirectoryNaming
    {
        public static string FormatNameTo8Dot3(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Invalid name.");
            name = name.Trim().ToUpperInvariant();

            string baseName, ext;
            int dot = name.LastIndexOf('.');
            if (dot >= 0)
            {
                baseName = name[..dot];
                ext = name[(dot + 1)..];
            }
            else
            {
                baseName = name;
                ext = "";
            }

            baseName = baseName.Length > 8 ? baseName[..8] : baseName.PadRight(8, ' ');
            ext = ext.Length > 3 ? ext[..3] : ext.PadRight(3, ' ');

            return baseName + ext;
        }

        public static string Parse8Dot3Name(string raw11)
        {
            raw11 ??= "";
            if (raw11.Length < 11) raw11 = raw11.PadRight(11, ' ');
            string baseName = raw11[..8].TrimEnd(' ');
            string ext = raw11[8..11].TrimEnd(' ');
            return ext.Length == 0 ? baseName : $"{baseName}.{ext}";
        }
    }
}

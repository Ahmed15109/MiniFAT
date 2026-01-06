namespace MiniFAT
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string diskPath = args.Length > 0 ? args[0] : "minifat.bin";

            using var disk = new VirtualDisk();
            bool isNew = disk.Initialize(diskPath);

            var super = new SuperblockManager(disk);
            var fat = new FatTableManager(disk);
            var dir = new DirectoryManager(disk, fat);
            var fs = new FileSystem(disk, super, fat, dir);

            fs.Initialize(isNew);

            var shell = new Shell(fs);
            shell.Run();
        }
    }
}

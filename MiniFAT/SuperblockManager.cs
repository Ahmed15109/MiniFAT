namespace MiniFAT
{
    public sealed class SuperblockManager
    {
        private readonly VirtualDisk _disk;
        public SuperblockManager(VirtualDisk disk) => _disk = disk;

        public byte[] ReadSuperblock() => _disk.ReadCluster(FsConstants.SUPERBLOCK_CLUSTER);
        public void WriteSuperblock(byte[] data) => _disk.WriteCluster(FsConstants.SUPERBLOCK_CLUSTER, data);
        public void InitializeSuperblock() => WriteSuperblock(new byte[FsConstants.CLUSTER_SIZE]);
    }
}

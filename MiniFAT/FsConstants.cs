namespace MiniFAT
{
    public static class FsConstants
    {
        public const int CLUSTER_SIZE = 1024;
        public const int CLUSTER_COUNT = 1024;
        public const int DISK_SIZE_BYTES = CLUSTER_SIZE * CLUSTER_COUNT; 

        public const int SUPERBLOCK_CLUSTER = 0;

        public const int FAT_START_CLUSTER = 1;
        public const int FAT_END_CLUSTER = 4; 
        public const int FAT_CLUSTER_COUNT = FAT_END_CLUSTER - FAT_START_CLUSTER + 1;
        public const int FAT_BYTES = FAT_CLUSTER_COUNT * CLUSTER_SIZE;
        public const int FAT_ENTRY_COUNT = CLUSTER_COUNT;

        public const int ROOT_DIR_FIRST_CLUSTER = 5;
        public const int CONTENT_START_CLUSTER = 5;

        public const byte ATTR_FILE = 0x00;
        public const byte ATTR_DIR = 0x10;
    }
}

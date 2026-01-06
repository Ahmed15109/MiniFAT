using System;
using System.Collections.Generic;

namespace MiniFAT
{
    public sealed class FatTableManager
    {
        private readonly VirtualDisk _disk;
        private readonly int[] _fat = new int[FsConstants.FAT_ENTRY_COUNT];

        public FatTableManager(VirtualDisk disk) => _disk = disk;

        public void LoadFatFromDisk()
        {
            byte[] all = new byte[FsConstants.FAT_BYTES];
            int pos = 0;
            for (int c = FsConstants.FAT_START_CLUSTER; c <= FsConstants.FAT_END_CLUSTER; c++)
            {
                byte[] chunk = _disk.ReadCluster(c);
                Buffer.BlockCopy(chunk, 0, all, pos, chunk.Length);
                pos += chunk.Length;
            }

            int[] ints = Converter.BytesToIntArray(all);
            Array.Copy(ints, _fat, _fat.Length);
        }

        public void FlushFatToDisk()
        {
            byte[] all = Converter.IntArrayToBytes(_fat);
            int pos = 0;
            for (int c = FsConstants.FAT_START_CLUSTER; c <= FsConstants.FAT_END_CLUSTER; c++)
            {
                byte[] chunk = new byte[FsConstants.CLUSTER_SIZE];
                Buffer.BlockCopy(all, pos, chunk, 0, chunk.Length);
                _disk.WriteCluster(c, chunk);
                pos += chunk.Length;
            }
        }

        public int Get(int index)
        {
            ValidateIndex(index);
            return _fat[index];
        }

        public void Set(int index, int value)
        {
            ValidateIndex(index);
            _fat[index] = value;
        }

        public List<int> FollowChain(int startCluster)
        {
            if (startCluster <= 0) return new List<int>();
            ValidateIndex(startCluster);

            var chain = new List<int>();
            var visited = new HashSet<int>();
            int cur = startCluster;

            while (true)
            {
                ValidateIndex(cur);
                if (!visited.Add(cur)) throw new InvalidOperationException("Circular FAT chain detected.");
                chain.Add(cur);

                int next = _fat[cur];
                if (next == -1) break;          
                if (next == 0) throw new InvalidOperationException("Broken FAT chain (hit free cluster).");
                cur = next;
            }

            return chain;
        }

        public int AllocateChain(int numClusters)
        {
            if (numClusters <= 0) return 0;

            int allocStart = Math.Max(FsConstants.CONTENT_START_CLUSTER, FsConstants.ROOT_DIR_FIRST_CLUSTER + 1);

            var free = new List<int>(numClusters);
            for (int i = allocStart; i < FsConstants.CLUSTER_COUNT && free.Count < numClusters; i++)
                if (_fat[i] == 0) free.Add(i);

            if (free.Count < numClusters)
                throw new InvalidOperationException("Disk full: insufficient free clusters.");

            for (int i = 0; i < free.Count - 1; i++)
                _fat[free[i]] = free[i + 1];
            _fat[free[^1]] = -1;

            return free[0];
        }

        public void FreeChain(int startCluster)
        {
            if (startCluster <= 0) return;
            var chain = FollowChain(startCluster);
            foreach (var c in chain) _fat[c] = 0;
        }

        private static void ValidateIndex(int i)
        {
            if (i < 0 || i >= FsConstants.FAT_ENTRY_COUNT)
                throw new ArgumentOutOfRangeException(nameof(i));
        }
    }
}

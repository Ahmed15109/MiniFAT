using System;
using System.Collections.Generic;

namespace MiniFAT  
{
    public sealed class DirectoryManager
    {
        private readonly VirtualDisk _disk;  
        private readonly FatTableManager _fat;  
                                               
        public DirectoryManager(VirtualDisk disk, FatTableManager fat) 
                                                                      
        {
            _disk = disk;
            _fat = fat;
        }


        public List<DirectoryEntry> ReadDirectory(int startCluster)
        {
            var entries = new List<DirectoryEntry>();
            if (startCluster <= 0) return entries;

            var chain = _fat.FollowChain(startCluster); 
            
            foreach (var c in chain)
            {
                byte[] cl = _disk.ReadCluster(c);
                for (int off = 0; off < FsConstants.CLUSTER_SIZE; off += DirectoryEntry.SIZE)
                {
                    var e = DirectoryEntry.Deserialize32(cl, off); 
                    if (e != null) entries.Add(e);
                }
            }
            return entries;
        }


        public DirectoryEntry? FindEntry(int dirStartCluster, string name)
        {
            string target = DirectoryNaming.FormatNameTo8Dot3(name);
            foreach (var e in ReadDirectory(dirStartCluster))
                if (string.Equals(e.Name83, target, StringComparison.OrdinalIgnoreCase))
                    return e;
            return null;

        }




        public void AddEntry(int dirStartCluster, DirectoryEntry newEntry)
        {
            var chain = _fat.FollowChain(dirStartCluster);
            if (chain.Count == 0) throw new InvalidOperationException("Directory chain is empty/broken.");

            foreach (var c in chain)
            {
                byte[] cl = _disk.ReadCluster(c);
                for (int off = 0; off < FsConstants.CLUSTER_SIZE; off += DirectoryEntry.SIZE)
                {
                    if (cl[off] == 0x00 || cl[off] == 0xE5)
                    {
                        byte[] eb = newEntry.Serialize32();
                        Buffer.BlockCopy(eb, 0, cl, off, DirectoryEntry.SIZE);
                        _disk.WriteCluster(c, cl);
                        return;
                    }
                }
            }
            int newCluster = _fat.AllocateChain(1);  
            int last = chain[^1];
            _fat.Set(last, newCluster);
            _fat.Set(newCluster, -1);

            byte[] newCl = new byte[FsConstants.CLUSTER_SIZE];
            byte[] eb2 = newEntry.Serialize32();
            Buffer.BlockCopy(eb2, 0, newCl, 0, DirectoryEntry.SIZE);
            _disk.WriteCluster(newCluster, newCl);
        }


        public void UpdateEntryInPlace(int dirStartCluster, DirectoryEntry updated)
        {
            var chain = _fat.FollowChain(dirStartCluster);
            foreach (var c in chain)
            {
                byte[] cl = _disk.ReadCluster(c);
                for (int off = 0; off < FsConstants.CLUSTER_SIZE; off += DirectoryEntry.SIZE)
                {
                    var e = DirectoryEntry.Deserialize32(cl, off);
                    if (e != null && string.Equals(e.Name83, updated.Name83, StringComparison.OrdinalIgnoreCase))
                    {
                        byte[] eb = updated.Serialize32();
                        Buffer.BlockCopy(eb, 0, cl, off, DirectoryEntry.SIZE);
                        _disk.WriteCluster(c, cl);
                        return;
                    }
                }
            }
            throw new InvalidOperationException("Entry to update not found.");
        }


        public DirectoryEntry RemoveEntry(int dirStartCluster, string name)
        {
            string target = DirectoryNaming.FormatNameTo8Dot3(name);
            var chain = _fat.FollowChain(dirStartCluster);
            foreach (var c in chain)
            {
                byte[] cl = _disk.ReadCluster(c);
                for (int off = 0; off < FsConstants.CLUSTER_SIZE; off += DirectoryEntry.SIZE)
                {    
                    var e = DirectoryEntry.Deserialize32(cl, off);
                    if (e != null && string.Equals(e.Name83, target, StringComparison.OrdinalIgnoreCase))
                    {
                        cl[off] = 0x00;  
                        _disk.WriteCluster(c, cl);
                        return e;
                    }
                }
            }  

            throw new InvalidOperationException("Entry not found.");
        }
    }
}

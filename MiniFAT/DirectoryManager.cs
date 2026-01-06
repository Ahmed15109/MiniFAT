using System;
using System.Collections.Generic;

namespace MiniFAT  //  مسؤول عن إدارة محتوى الديركتوري
{
    public sealed class DirectoryManager
    {
        private readonly VirtualDisk _disk;  // يقرا ويكتب الكلاستر 
        private readonly FatTableManager _fat; // عشان يعرف سلسلة الـ clusters بتاعة الديركتوري 
                                               //  لأن الديركتوري ممكن يمتد على أكتر من cluster 
        public DirectoryManager(VirtualDisk disk, FatTableManager fat) 
                                                                      
        {
            _disk = disk;
            _fat = fat;
        }


        public List<DirectoryEntry> ReadDirectory(int startCluster)
        {
            var entries = new List<DirectoryEntry>();
            if (startCluster <= 0) return entries;

            var chain = _fat.FollowChain(startCluster); //باخد startCluster بتاع الديركتوري
            // و بجيب سلسله الكلاستر بتاعته من ال FollowChain 
            foreach (var c in chain)
            {
                byte[] cl = _disk.ReadCluster(c);
                for (int off = 0; off < FsConstants.CLUSTER_SIZE; off += DirectoryEntry.SIZE)
                {
                    var e = DirectoryEntry.Deserialize32(cl, off); // بقسمه كل chunk =32 bytes
                    // وبعدين بحول ال بايتس ل دايركت انتري 
                    if (e != null) entries.Add(e);
                }
            }
            return entries;
        }


        //بسيرش عن فولدر جوا ال ديركتوري 
        public DirectoryEntry? FindEntry(int dirStartCluster, string name)
        {
            string target = DirectoryNaming.FormatNameTo8Dot3(name);
            foreach (var e in ReadDirectory(dirStartCluster))
                if (string.Equals(e.Name83, target, StringComparison.OrdinalIgnoreCase))
                    return e;
            return null;
            // بحول الاسم لصيغه char8.3
            //  عشان اقدر اقارن بينهم لان التخزين ف الديركتوري بنفس الصيغه 
            // FAT بتخزن Upppercase

        }




        // اضافه انتري 
        public void AddEntry(int dirStartCluster, DirectoryEntry newEntry)
        {
            var chain = _fat.FollowChain(dirStartCluster);
            if (chain.Count == 0) throw new InvalidOperationException("Directory chain is empty/broken.");
            // بدور علي مكان فاضي ف الديركتوري لو لقي بمتبها ف نفس الكلاستر 

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
            int newCluster = _fat.AllocateChain(1);  // لو الديركتوري مليان بوسعه عن طريق اني بحجز كلاستر جديد 

            int last = chain[^1];
            // و اربط اخر كلاستر ف ال chain ب ال نيوكلاستر 
            _fat.Set(last, newCluster);
            _fat.Set(newCluster, -1);

            byte[] newCl = new byte[FsConstants.CLUSTER_SIZE];
            byte[] eb2 = newEntry.Serialize32();
            Buffer.BlockCopy(eb2, 0, newCl, 0, DirectoryEntry.SIZE);
            _disk.WriteCluster(newCluster, newCl);
        }


        // التعديل ف انتري 
        public void UpdateEntryInPlace(int dirStartCluster, DirectoryEntry updated)
        {
            var chain = _fat.FollowChain(dirStartCluster);
            foreach (var c in chain)
            {
                // بدور علي نفس الانتري بالاسم و لما بلاقيها بكتب الابديت ف نفس مكانها 
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


        // الحذف 
        public DirectoryEntry RemoveEntry(int dirStartCluster, string name)
        {
            string target = DirectoryNaming.FormatNameTo8Dot3(name);
            var chain = _fat.FollowChain(dirStartCluster);
             //بدور ع الانتري بالاسم 
            foreach (var c in chain)
            {
                byte[] cl = _disk.ReadCluster(c);
                for (int off = 0; off < FsConstants.CLUSTER_SIZE; off += DirectoryEntry.SIZE)
                {    
                    var e = DirectoryEntry.Deserialize32(cl, off);
                    if (e != null && string.Equals(e.Name83, target, StringComparison.OrdinalIgnoreCase))
                    {
                        cl[off] = 0x00;  // لما بلاقيها بمسح الانتري من الديركتوري 
                        _disk.WriteCluster(c, cl);
                        return e;
                    }
                }
            }  // بشيل ال metadata فقط مش بيمسح بيانات الملف 

            throw new InvalidOperationException("Entry not found.");
        }
    }
}

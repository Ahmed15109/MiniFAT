using System;
using System.Collections.Generic;
using System.IO;

namespace MiniFAT
{
    public sealed class FileSystem   // بيجمع كل ال layers هنا 
    {
        private readonly VirtualDisk _disk; 
        private readonly SuperblockManager _super;
        private readonly FatTableManager _fat; 
        private readonly DirectoryManager _dir; 

        public int CurrentDirCluster { get; private set; } = FsConstants.ROOT_DIR_FIRST_CLUSTER;

        public FileSystem(VirtualDisk disk, SuperblockManager super, FatTableManager fat, DirectoryManager dir)
        {
            _disk = disk;       //قراءة/كتابة
            _super = super;    //إدارة كلاستر 0 
            _fat = fat;        // بتتعامل مع الكلاستر تخصيص تعديل 
            _dir = dir;       // اداره الانتري جوا الديركتوري 
        }

        public void Initialize(bool isNewDisk)
        {
            _fat.LoadFatFromDisk();   // بحمل ال fat من الديسك

            if (isNewDisk || IsFatLooksEmpty())
            {
                _super.InitializeSuperblock();  // بصفر السوبرلوك 

                for (int i = 0; i <= FsConstants.FAT_END_CLUSTER; i++)
                    _fat.Set(i, -1); //  بحجز -1 في ال FAt

                _fat.Set(FsConstants.ROOT_DIR_FIRST_CLUSTER, -1);  

                _disk.WriteCluster(FsConstants.ROOT_DIR_FIRST_CLUSTER, new byte[FsConstants.CLUSTER_SIZE]);

                _fat.FlushFatToDisk();
            }
            else
            {
                bool changed = false;

                for (int i = 0; i <= FsConstants.FAT_END_CLUSTER; i++)
                {
                    if (_fat.Get(i) != -1) { _fat.Set(i, -1); changed = true; }
                }

                // بحجز 5 كلاستر ل root directory
                // ال fat == linked list
                if (_fat.Get(FsConstants.ROOT_DIR_FIRST_CLUSTER) != -1)
                {
                    _fat.Set(FsConstants.ROOT_DIR_FIRST_CLUSTER, -1);
                    changed = true;
                }

                if (changed) _fat.FlushFatToDisk();
            }

            CurrentDirCluster = FsConstants.ROOT_DIR_FIRST_CLUSTER;
        }

        private bool IsFatLooksEmpty()
        {
            for (int i = 0; i <= FsConstants.FAT_END_CLUSTER; i++)
                if (_fat.Get(i) != 0) return false;
            return true;
        }

        public DirectoryEntry? GetEntry(string name) => _dir.FindEntry(CurrentDirCluster, name);
        //يجيب metadata 

        public List<int> GetClusterChain(string name)
        {
            var entry = GetEntry(name);
            if (entry == null) throw new FileNotFoundException("Entry not found.");
            if (entry.IsDirectory) throw new InvalidOperationException("FAT chain is for files only.");

            if (entry.FirstCluster == 0 || entry.FileSize == 0)
                return new List<int>();

            return _fat.FollowChain(entry.FirstCluster);
            //يرجع سلسلة تخزين الملف عشان أمر fat في الـ shell)
        }

        public DirectoryEntry[] ListDir() => _dir.ReadDirectory(CurrentDirCluster).ToArray();



        public void CreateFile(string filename)
            // بيتاكد ان الاسم مش موجود 
        {
            if (_dir.FindEntry(CurrentDirCluster, filename) != null)
                throw new InvalidOperationException("File already exists.");
            // و يعمل DirectoryEntry للملف ويضيفه للدايركتوري 
            var entry = new DirectoryEntry
            {
                Name83 = DirectoryNaming.FormatNameTo8Dot3(filename),
                Attribute = FsConstants.ATTR_FILE,
                FirstCluster = 0,
                FileSize = 0
            };

            _dir.AddEntry(CurrentDirCluster, entry);
        }




        public void WriteFile(string filename, byte[] content)
        {
            content ??= Array.Empty<byte>();

            // حجز Clusters في الديسك عن طريق FAT وتكتب الداتا فيهم

            var entry = _dir.FindEntry(CurrentDirCluster, filename);
            if (entry == null) throw new FileNotFoundException("File not found.");
            if (entry.IsDirectory) throw new InvalidOperationException("Cannot write to a directory.");
            //تحدّث الميتاداتا جوه DirectoryEntry
            if (entry.FirstCluster != 0)
                _fat.FreeChain(entry.FirstCluster);

            int needed = content.Length == 0 ? 0 : (int)Math.Ceiling(content.Length / (double)FsConstants.CLUSTER_SIZE);
            int start = needed == 0 ? 0 : _fat.AllocateChain(needed);
            // بنحسب محتاج كام كلاستر 
            //كلاستر =1024 بايت 


            if (needed > 0)
            {
                var chain = _fat.FollowChain(start);
                int pos = 0;
                foreach (var c in chain)
                {
                    byte[] cl = new byte[FsConstants.CLUSTER_SIZE];
                    int take = Math.Min(FsConstants.CLUSTER_SIZE, content.Length - pos);
                    Buffer.BlockCopy(content, pos, cl, 0, take);
                    _disk.WriteCluster(c, cl);
                    pos += take;
                }
            }

            entry.FirstCluster = start;
            entry.FileSize = content.Length;

            _dir.UpdateEntryInPlace(CurrentDirCluster, entry);
            _fat.FlushFatToDisk();
        }

        public byte[] ReadFile(string filename)
        {
            var entry = _dir.FindEntry(CurrentDirCluster, filename);
            if (entry == null) throw new FileNotFoundException("File not found.");
            if (entry.IsDirectory) throw new InvalidOperationException("Cannot read a directory.");

            if (entry.FirstCluster == 0 || entry.FileSize == 0) return Array.Empty<byte>();

            var chain = _fat.FollowChain(entry.FirstCluster);
            byte[] all = new byte[chain.Count * FsConstants.CLUSTER_SIZE];

            int pos = 0;
            foreach (var c in chain)
            {
                byte[] cl = _disk.ReadCluster(c);
                Buffer.BlockCopy(cl, 0, all, pos, cl.Length);
                pos += cl.Length;
            }

            byte[] result = new byte[entry.FileSize];
            Buffer.BlockCopy(all, 0, result, 0, result.Length);
            return result;
        }

        public void DeleteFile(string filename)
        {
            var entry = _dir.FindEntry(CurrentDirCluster, filename);
            if (entry == null) throw new FileNotFoundException("File not found.");
            if (entry.IsDirectory) throw new InvalidOperationException("Use rd for directories.");

            _dir.RemoveEntry(CurrentDirCluster, filename);

            if (entry.FirstCluster != 0) _fat.FreeChain(entry.FirstCluster);
            _fat.FlushFatToDisk();
        }

        public void CreateDirectory(string dirname)
        {
            if (_dir.FindEntry(CurrentDirCluster, dirname) != null)
                throw new InvalidOperationException("Entry already exists.");

            int start = _fat.AllocateChain(1);
            _disk.WriteCluster(start, new byte[FsConstants.CLUSTER_SIZE]);

            var entry = new DirectoryEntry
            {
                Name83 = DirectoryNaming.FormatNameTo8Dot3(dirname),
                Attribute = FsConstants.ATTR_DIR,
                FirstCluster = start,
                FileSize = 0
            };

            _dir.AddEntry(CurrentDirCluster, entry);
            _fat.FlushFatToDisk();
        }

        public void RemoveDirectory(string dirname)
        {
            var entry = _dir.FindEntry(CurrentDirCluster, dirname);
            if (entry == null) throw new DirectoryNotFoundException("Directory not found.");
            if (!entry.IsDirectory) throw new InvalidOperationException("Not a directory.");

            var entries = _dir.ReadDirectory(entry.FirstCluster);
            if (entries.Count > 0) throw new InvalidOperationException("Directory not empty.");

            _dir.RemoveEntry(CurrentDirCluster, dirname);
            if (entry.FirstCluster != 0) _fat.FreeChain(entry.FirstCluster);

            _fat.FlushFatToDisk();
        }

        public void CopyFile(string src, string dst)
        {
            byte[] data = ReadFile(src);
            if (_dir.FindEntry(CurrentDirCluster, dst) != null)
                throw new InvalidOperationException("Destination already exists.");

            CreateFile(dst);
            WriteFile(dst, data);
        }

        public void RenameEntry(string oldName, string newName)
        {
            var entry = _dir.FindEntry(CurrentDirCluster, oldName);
            if (entry == null) throw new FileNotFoundException("Entry not found.");
            if (_dir.FindEntry(CurrentDirCluster, newName) != null)
                throw new InvalidOperationException("New name already exists.");

            _dir.RemoveEntry(CurrentDirCluster, oldName);
            entry.Name83 = DirectoryNaming.FormatNameTo8Dot3(newName);
            _dir.AddEntry(CurrentDirCluster, entry);
        }

        public void ChangeDirectory(string dirname, Stack<int> clusterStack, Stack<string> nameStack)
        {
            if (dirname == "/" || dirname == "\\")
            {
                clusterStack.Clear();
                nameStack.Clear();
                CurrentDirCluster = FsConstants.ROOT_DIR_FIRST_CLUSTER;
                return;
            }

            if (dirname == "..")
            {
                if (clusterStack.Count > 0)
                    CurrentDirCluster = clusterStack.Pop();
                if (nameStack.Count > 0)
                    nameStack.Pop();
                return;
            }

            var entry = _dir.FindEntry(CurrentDirCluster, dirname);
            if (entry == null) throw new DirectoryNotFoundException("Directory not found.");
            if (!entry.IsDirectory) throw new InvalidOperationException("Not a directory.");

            clusterStack.Push(CurrentDirCluster);
            nameStack.Push(entry.PrettyName);
            CurrentDirCluster = entry.FirstCluster;
        }


        public void Format()
        {
            for (int i = 0; i < FsConstants.CLUSTER_COUNT; i++)
                _disk.WriteCluster(i, new byte[FsConstants.CLUSTER_SIZE]);

            for (int i = 0; i <= FsConstants.FAT_END_CLUSTER; i++)
                _fat.Set(i, -1);

            _fat.Set(FsConstants.ROOT_DIR_FIRST_CLUSTER, -1);

            _fat.FlushFatToDisk();

            CurrentDirCluster = FsConstants.ROOT_DIR_FIRST_CLUSTER;
        }

    }
}

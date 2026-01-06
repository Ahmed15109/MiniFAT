using System;
using System.IO;

namespace MiniFAT
{
    public sealed class VirtualDisk : IDisposable
    {
        private FileStream _stream = null!;
        public string DiskPath { get; private set; } = "";

        public bool Initialize(string path)
        {
            DiskPath = path;
            bool isNew = !File.Exists(path);

            if (isNew)
            {
                using (var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    byte[] zeros = new byte[FsConstants.CLUSTER_SIZE];
                    for (int i = 0; i < FsConstants.CLUSTER_COUNT; i++)
                        fs.Write(zeros, 0, zeros.Length);
                    fs.Flush(true);
                }
            }

            _stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            if (_stream.Length != FsConstants.DISK_SIZE_BYTES)
                throw new InvalidOperationException($"Disk size invalid. Expected {FsConstants.DISK_SIZE_BYTES}, got {_stream.Length}.");

            return isNew;
        }

        public byte[] ReadCluster(int clusterIndex)
        {
            ValidateCluster(clusterIndex);
            byte[] data = new byte[FsConstants.CLUSTER_SIZE];
            long offset = (long)clusterIndex * FsConstants.CLUSTER_SIZE;

            _stream.Seek(offset, SeekOrigin.Begin);

            int read = 0;
            while (read < data.Length)
            {
                int r = _stream.Read(data, read, data.Length - read);
                if (r == 0) throw new EndOfStreamException("Unexpected EOF while reading cluster.");
                read += r;
            }
            return data;
        }

        public void WriteCluster(int clusterIndex, byte[] data)
        {
            ValidateCluster(clusterIndex);
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length != FsConstants.CLUSTER_SIZE)
                throw new ArgumentException($"Cluster must be exactly {FsConstants.CLUSTER_SIZE} bytes.");

            long offset = (long)clusterIndex * FsConstants.CLUSTER_SIZE;
            _stream.Seek(offset, SeekOrigin.Begin);
            _stream.Write(data, 0, data.Length);
            _stream.Flush(true); 
        }

        public long GetDiskSize() => _stream.Length;

        public void CloseDisk()
        {
            _stream?.Flush(true);
            _stream?.Dispose();
            _stream = null!;
        }

        public void Dispose() => CloseDisk();

        private static void ValidateCluster(int i)
        {
            if (i < 0 || i >= FsConstants.CLUSTER_COUNT)
                throw new ArgumentOutOfRangeException(nameof(i), $"Cluster must be in [0..{FsConstants.CLUSTER_COUNT - 1}]");
        }
    }
}

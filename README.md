 MiniFAT File System

MiniFAT is a C# console application that simulates a simplified FAT-based file system
using a virtual disk file.

 Features
  - Virtual disk with fixed-size clusters
  - FAT table allocation
  - File and directory management
  - Shell-like command interface
  - Import / Export files from host OS
  - FAT chain and metadata inspection

 Project Structure
  - FsConstants.cs : File system constants
  - VirtualDisk.cs : Low-level disk read/write
  - SuperblockManager.cs : Superblock handling
  - FatTableManager.cs : FAT allocation & chains
  - DirectoryEntry.cs : Directory entry structure
  - DirectoryNaming.cs : 8.3 filename handling
  - DirectoryManager.cs : Directory operations
  - FileSystem.cs : Core file system logic
  - Shell.cs : Command-line shell
  - Program.cs : Application entry point

 How to Run
  1. Open the solution in Visual Studio
  2. Run the project (F5)
  3. Type `help` to see available commands

 Commands
  → See `Command.md` for full command list.

Notes
  - The virtual disk file (`minifat.bin`) is created at runtime and ignored by Git.



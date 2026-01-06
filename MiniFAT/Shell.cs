using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MiniFAT
{
    public sealed class Shell
    {
        private readonly FileSystem _fs;

        private readonly Stack<int> _clusterStack = new Stack<int>();
        private readonly Stack<string> _nameStack = new Stack<string>();

        public Shell(FileSystem fs) => _fs = fs;

        public void Run()
        {
            Console.WriteLine("MiniFAT - type 'help'.");

            while (true)
            {
                Console.Write($"MiniFAT:{GetPath()}> ");
                string? line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] parts = SplitArgs(line);
                string cmd = parts[0].ToLowerInvariant();
                string[] args = parts.Skip(1).ToArray();

                try
                {
                    switch (cmd)
                    {
                        case "exit":
                        case "quit":
                            return;

                        case "help":
                            PrintHelp();
                            break;

                        case "cls":
                        case "clear":
                            Console.Clear();
                            break;

                        case "dir":
                        case "ls":
                            DoList();
                            break;

                        case "touch":
                        case "mkfile":
                            Require(args, 1);
                            _fs.CreateFile(args[0]);
                            Console.WriteLine("OK");
                            break;

                        case "write":
                            Require(args, 2);
                            _fs.WriteFile(args[0], Encoding.UTF8.GetBytes(args[1]));
                            Console.WriteLine("OK");
                            break;

                        case "type":
                        case "cat":
                            Require(args, 1);
                            Console.WriteLine(Encoding.UTF8.GetString(_fs.ReadFile(args[0])));
                            break;

                        case "del":
                        case "rm":
                            Require(args, 1);
                            _fs.DeleteFile(args[0]);
                            Console.WriteLine("OK");
                            break;

                        case "md":
                        case "mkdir":
                            Require(args, 1);
                            _fs.CreateDirectory(args[0]);
                            Console.WriteLine("OK");
                            break;

                        case "rd":
                            Require(args, 1);
                            _fs.RemoveDirectory(args[0]);
                            Console.WriteLine("OK");
                            break;

                        case "cd":
                            Require(args, 1);
                            _fs.ChangeDirectory(args[0], _clusterStack, _nameStack);
                            break;

                        case "copy":
                            Require(args, 2);
                            _fs.CopyFile(args[0], args[1]);
                            Console.WriteLine("OK");
                            break;

                        case "ren":
                            Require(args, 2);
                            _fs.RenameEntry(args[0], args[1]);
                            Console.WriteLine("OK");
                            break;

                        case "import":
                            Require(args, 2);
                            byte[] hostBytes = File.ReadAllBytes(args[0]);
                            var existing = _fs.GetEntry(args[1]);
                            if (existing != null && existing.IsDirectory)
                                throw new InvalidOperationException("Destination name is a directory.");

                            if (existing == null)
                                _fs.CreateFile(args[1]);

                            _fs.WriteFile(args[1], hostBytes);
                            Console.WriteLine("OK");
                            break;

                        case "export":
                            Require(args, 2);
                            byte[] vBytes = _fs.ReadFile(args[0]);
                            File.WriteAllBytes(args[1], vBytes);
                            Console.WriteLine("OK");
                            break;

                        case "stat":
                            Require(args, 1);
                            DoStat(args[0]);
                            break;

                        case "fat":
                            Require(args, 1);
                            DoFat(args[0]);
                            break;

                        case "format":
                            Require(args, 1);
                            if (!args[0].Equals("yes", StringComparison.OrdinalIgnoreCase))
                                throw new InvalidOperationException("Use: format yes");

                            _fs.Format();

                            // go back to root in shell prompt
                            _clusterStack.Clear();
                            _nameStack.Clear();

                            Console.WriteLine("OK (disk formatted doneeeee ).");
                            break;

                        default:
                            Console.WriteLine("Unknown command. Type 'help'.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private void DoList()
        {
            var entries = _fs.ListDir();
            Console.WriteLine("NAME\t\tTYPE\tSIZE\tFIRST");
            foreach (var e in entries)
            {
                string type = e.IsDirectory ? "<DIR>" : "<FILE>";
                Console.WriteLine($"{e.PrettyName}\t{type}\t{e.FileSize}\t{e.FirstCluster}");
            }
        }

        private void DoStat(string name)
        {
            var e = _fs.GetEntry(name);
            if (e == null)
            {
                Console.WriteLine("Not found.");
                return;
            }

            Console.WriteLine($"Name      : {e.PrettyName}");
            Console.WriteLine($"Type      : {(e.IsDirectory ? "Directory" : "File")}");
            Console.WriteLine($"Size      : {e.FileSize} bytes");
            Console.WriteLine($"First     : {e.FirstCluster}");

            if (!e.IsDirectory)
            {
                var chain = (e.FirstCluster == 0 || e.FileSize == 0) ? new List<int>() : _fs.GetClusterChain(name);
                Console.WriteLine($"Clusters  : {chain.Count}");
            }
        }

        private void DoFat(string name)
        {
            var e = _fs.GetEntry(name);
            if (e == null)
            {
                Console.WriteLine("Not found.");
                return;
            }
            if (e.IsDirectory)
            {
                Console.WriteLine("fat is for files only.");
                return;
            }

            var chain = _fs.GetClusterChain(name);
            if (chain.Count == 0)
            {
                Console.WriteLine("(empty) -> EOF");
                return;
            }

            Console.WriteLine(string.Join(" -> ", chain) + " -> EOF");
        }

        private static void PrintHelp()
        {
            Console.WriteLine(@"
Commands:
  help                       Show help
  clear | cls                 Clear screen
  exit | quit                 Exit
  dir | ls                    List current directory
  cd <dir|..|/>               Change directory
  touch | mkfile <name>       Create empty file
  write <name> ""text""        Write UTF-8 text (overwrite)
  type | cat <name>           Show file contents
  del | rm <name>             Delete file
  mkdir | md <name>           Create directory
  rd <name>                   Remove empty directory
  copy <src> <dst>            Copy file
  ren <old> <new>             Rename entry
  import <host> <vname>       Import host file into MiniFAT
  export <vname> <host>       Export MiniFAT file to host

  stat <name>                 Show metadata (size, first cluster, etc.)
  fat <file>                  Show FAT cluster chain

  format yes                  Delete EVERYTHING and re-initialize disk
");
        }

        private string GetPath()
        {
            if (_nameStack.Count == 0) return "/";
            var parts = _nameStack.Reverse().ToArray();
            return "/" + string.Join("/", parts);
        }

        private static void Require(string[] args, int n)
        {
            if (args.Length < n) throw new ArgumentException("Not enough arguments.");
        }

        private static string[] SplitArgs(string input)
        {
            var list = new List<string>();
            var cur = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < input.Length; i++)
            {
                char ch = input[i];
                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (!inQuotes && char.IsWhiteSpace(ch))
                {
                    if (cur.Length > 0)
                    {
                        list.Add(cur.ToString());
                        cur.Clear();
                    }
                }
                else cur.Append(ch);
            }

            if (cur.Length > 0) list.Add(cur.ToString());
            return list.ToArray();
        }
    }
}

using Dokan;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MergeFS
{
    class Root : RootDirectory
    {
        public string Path { get { return path; } }

        string path = "";

        public Root(string Path)
        {
            this.path = Path;
        }

        public bool ContainsDirectory(string filename)
        {
            if (filename == null || filename.Length == 0) filename = "\\";

            string combined = System.IO.Path.Combine(path, stripDriveLetter(filename));
            return Directory.Exists(combined);
        }

        public bool ContainsFile(string filename)
        {
            if (filename == null || filename.Length == 0) filename = "\\";
            return File.Exists(System.IO.Path.Combine(path, stripDriveLetter(filename)));
        }

        public string getRealPath(string virtualFilename)
        {
            if (virtualFilename == null || virtualFilename.Length == 0) virtualFilename = "\\";
            return System.IO.Path.Combine(path, stripDriveLetter(virtualFilename));
        }

        private static string stripDriveLetter(string virtualFilename)
        {
            /*
            if (virtualFilename == null)
                virtualFilename = "\\";

            string output = virtualFilename.Substring(System.IO.Path.GetPathRoot(virtualFilename).Length);
            if (output.Length == 0)
            {
                output = "\\";
            }
             * */
            while (virtualFilename.StartsWith("\\"))
            {
                virtualFilename = virtualFilename.Substring(1);
            }

            return virtualFilename;
        }

        public ulong getFreeSpace()
        {
            ulong freeBytes;
            ulong totalBytes;
            ulong freeBytesAvailable;

            if (GetDiskFreeSpaceEx(System.IO.Path.GetPathRoot(this.path), out freeBytesAvailable, out totalBytes, out freeBytes))
            {
                return freeBytesAvailable;
            }
            else
            {
                return 0;
            }
        }

        public bool hasSpace(ulong bytes)
        {
            return getFreeSpace() > bytes;
        }


        public void mkDirs(string virtualPath)
        {
            if(!Directory.Exists(getRealPath(virtualPath))) 
                Directory.CreateDirectory(getRealPath(virtualPath));
        }

        public List<FileInfo> listFiles(string virtualPath)
        {
            List<FileInfo> output = new List<FileInfo>();

            if (ContainsDirectory(virtualPath))
            {
                foreach(String file in Directory.GetFiles(getRealPath(virtualPath)))
                {
                    output.Add(new FileInfo(file));
                }
            }

            return output;
        }

        public List<DirectoryInfo> listDirectories(string virtualPath)
        {
            List<DirectoryInfo> output = new List<DirectoryInfo>();

            if (ContainsDirectory(virtualPath))
            {
                foreach (String file in Directory.GetDirectories(getRealPath(virtualPath)))
                {
                    output.Add(new DirectoryInfo(file));
                }
            }

            return output;
        }

        public List<FileSystemInfo> getFileSystemInfos(string virtualPath)
        {
            List<FileSystemInfo> output = new List<FileSystemInfo>();

            if (ContainsDirectory(virtualPath))
            {
                DirectoryInfo di = new DirectoryInfo(getRealPath(virtualPath));

                //Console.WriteLine("getting File system info for '"+di.FullName+"'");
                try
                {
                    output.AddRange(di.GetFileSystemInfos());
                }
                catch (ArgumentException e)
                {
                    Console.WriteLine("Caught an argumen exception while getting directory infos for '" + di.FullName + "'. " + e + " " + e.Message);
                    Console.WriteLine("Invalid path chars:" + new string(System.IO.Path.GetInvalidPathChars()));
                    Console.WriteLine("Invalid FN Chars:" + new string(System.IO.Path.GetInvalidFileNameChars()));
                }
            }


            return output;
        }


        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
        out ulong lpFreeBytesAvailable,
        out ulong lpTotalNumberOfBytes,
        out ulong lpTotalNumberOfFreeBytes);




        public ulong getFreeSpaceBytes()
        {
            return getFreeSpace();
        }


        public ulong getTotalSpaceBytes()
        {
            ulong freeBytes;
            ulong totalBytes;
            ulong freeBytesAvailable;

            if (GetDiskFreeSpaceEx(System.IO.Path.GetPathRoot(this.path), out freeBytesAvailable, out totalBytes, out freeBytes))
            {
                return totalBytes;
            }
            else
            {
                return 0;
            }
        }

        public override String ToString()
        {
            return path;
        }



        public void moveFile(string originalVirtualPath, string endVirtualPath)
        {
            //Console.WriteLine("moveFile called '" + originalVirtualPath + "' -> '" + endVirtualPath + "'");

            if (ContainsFile(originalVirtualPath) || ContainsDirectory(originalVirtualPath))
            {
                string from = getRealPath(originalVirtualPath);
                string to = getRealPath(endVirtualPath);
               // Console.WriteLine("Moving file from '" + from + "' to '" + to + "'");

                DirectoryInfo parent = Directory.GetParent(to);
                if (!parent.Exists)
                {
                    Directory.CreateDirectory(parent.FullName);
                }

                if (Directory.Exists(from))
                {
                    Directory.Move(from, to);
                }
                else
                {
                    File.Move(from, to);
                }
            }
        }


        public void deleteFile(string virtualPath)
        {
            if (ContainsFile(virtualPath))
            {
                File.Delete(getRealPath(virtualPath));
            }
        }

        public void deleteDirectory(string virtualPath)
        {
            if (ContainsDirectory(virtualPath))
            {
                Directory.Delete(getRealPath(virtualPath),true);
            }
        }


        public int createFile(string virtualPath, FileMode mode)
        {
            // Console.WriteLine("Creating file in root " + this + " '" + virtualPath + "' (" + mode + ")");
            try
            {
                switch (mode)
                {
                    case FileMode.Append:
                        {
                            FileStream fs = File.Open(getRealPath(virtualPath), mode);
                            fs.Close();
                        }
                        return 0;

                    case FileMode.Create:
                        {
                            FileStream fs = File.Create(getRealPath(virtualPath));
                            fs.Close();
                        }
                        return 0;


                    case FileMode.CreateNew:
                        if (File.Exists(getRealPath(virtualPath)))
                            return DokanNet.ERROR_FILE_EXISTS;
                        else
                        {
                            FileStream fs = File.Create(getRealPath(virtualPath));
                            fs.Close();
                        }

                        return 0;

                    case FileMode.OpenOrCreate:
                        if (!File.Exists(getRealPath(virtualPath)))
                        {
                            FileStream fs = File.Create(getRealPath(virtualPath));
                            fs.Close();
                        }
                        return 0;

                    case FileMode.Truncate:
                        {
                            FileStream fs = File.Create(getRealPath(virtualPath));
                            fs.Close();
                        }
                        return 0;

                    default:
                        return -1;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught an exception while creating '" + virtualPath + "' in " + this + ".  " + e + ": " + e.Message);
                
            }
            return -1;
        }
    }
}

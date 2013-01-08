using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dokan;
using System.IO;

namespace MergeFS
{
    class MergedFileSystem : DokanOperations
    {
       // Logger logger;


        RootCollection mergedDirs;

        public MergedFileSystem(Logger logger, IEnumerable<Root> roots)
        {
          //  this.logger = logger;
            mergedDirs = new RootCollection(roots);
        }

        public int CreateFile(string filename, System.IO.FileAccess access, System.IO.FileShare share, System.IO.FileMode mode, System.IO.FileOptions options, DokanFileInfo info)
        {
            try
            {
                Console.WriteLine("Create File "+filename+" access "+access+" share "+share+" mode "+mode+ " options "+options+" info "+info);
                Root root = mergedDirs.getRootWithBestSpace(0);

                

                if ((mode == FileMode.Create || mode == FileMode.CreateNew || mode == FileMode.OpenOrCreate )&& !root.ContainsDirectory(Path.GetDirectoryName(filename)))
                {

                    root.mkDirs(Path.GetDirectoryName(filename));
                }

               

                if (Directory.Exists(root.getRealPath(filename)))
                {
                    info.IsDirectory = true;
                }
                else if(access != FileAccess.Read)
                {
                    mergedDirs.createFile(filename,mode);
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught an exception: " + e + ": " + e.Message);
                return -1;
            }
        }

        public int OpenDirectory(string filename, DokanFileInfo info)
        {
            Console.WriteLine("Open Directory " + filename + " info " + info);
            try
            {
            
                if (!mergedDirs.ContainsDirectory(filename))
                {
                    mergedDirs.mkDirs(filename);
                }

                info.IsDirectory = true;
                
                return 0;
            }
            catch (Exception e)
            {
                
                return -1;
            }
        }

        public int CreateDirectory(string filename, DokanFileInfo info)
        {
            Console.WriteLine("Create Directory " + filename);
            try
            {
             //   logger.addLog("CreateDirectory '" + filename + "'");
                mergedDirs.mkDirs(filename);
                return 0;
            }
            catch (Exception e)
            {
                //Console.WriteLine("Caught an exception: " + e + ": " + e.Message);
                return -1;
            }
        }

        public int Cleanup(string filename, DokanFileInfo info)
        {
            return 0;
        }

        public int CloseFile(string filename, DokanFileInfo info)
        {
            return 0;
        }

        public int ReadFile(string filename, byte[] buffer, ref uint readBytes, long offset, DokanFileInfo info)
        {
            //Console.WriteLine("Read File " + filename);
            try
            {
                //logger.addLog("ReadFile '" + filename + "'");
                if (!mergedDirs.ContainsFile(filename))
                {
                    //Console.WriteLine("Could not find file " + filename);
                    return DokanNet.ERROR_FILE_NOT_FOUND;
                }

                try
                {
                    FileStream fs = File.OpenRead(mergedDirs.getRealPath(filename));
                    fs.Seek(offset, SeekOrigin.Begin);
                    readBytes = (uint)fs.Read(buffer, 0, buffer.Length);
                    fs.Close();
                    return 0;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Caught an exception while reading file " + e + " " + e.Message);
                    return -1;
                }
            }
            
            catch (Exception e)
            {
                //Console.WriteLine("Caught an exception: " + e + ": " + e.Message);
                return -1;
            }
        }

        public int WriteFile(string filename, byte[] buffer, ref uint writtenBytes, long offset, DokanFileInfo info)
        {
            //Console.WriteLine("Write File: " + filename);
            try
            {
                //logger.addLog("ReadFile '" + filename + "'");
                try
                {
                    FileStream fs = File.OpenWrite(mergedDirs.getRealPath(filename));
                    fs.Seek(offset, SeekOrigin.Begin);
                    fs.Write(buffer, 0, buffer.Length);
                    writtenBytes = (uint)buffer.Length;
                    fs.Close();
                    return 0;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Caught an exception while writing file. " + e + " " + e.Message);
                    return -1;
                }
            }

            catch (Exception e)
            {
                Console.WriteLine("Caught an exception: " + e + ": " + e.Message);
                return -1;
            }
            return -1;
        }

        public int FlushFileBuffers(string filename, DokanFileInfo info)
        {
            return 0;
        }

        public int GetFileInformation(string filename, FileInformation fileinfo, DokanFileInfo info)
        {
            Console.WriteLine("Get File Info");
            try
            {
             //   logger.addLog("GetFileInformation '" + filename + "'");
                string path = mergedDirs.getRealPath(filename);
                if (File.Exists(path))
                {
                    FileInfo f = new FileInfo(path);

                    fileinfo.Attributes = f.Attributes;
                    fileinfo.CreationTime = f.CreationTime;
                    fileinfo.LastAccessTime = f.LastAccessTime;
                    fileinfo.LastWriteTime = f.LastWriteTime;
                    fileinfo.Length = f.Length;
                    return 0;
                }
                else if (Directory.Exists(path))
                {
                    DirectoryInfo f = new DirectoryInfo(path);

                    fileinfo.Attributes = f.Attributes;
                    fileinfo.CreationTime = f.CreationTime;
                    fileinfo.LastAccessTime = f.LastAccessTime;
                    fileinfo.LastWriteTime = f.LastWriteTime;
                    fileinfo.Length = 0;// f.Length;
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught an exception: " + e + ": " + e.Message);
                return -1;
            }
        }

        public int FindFiles(string filename, System.Collections.ArrayList files, DokanFileInfo info)
        {
            try
            {
             //   logger.addLog("FindFiles '" + filename + "'");
            
                if (mergedDirs.ContainsDirectory(filename))
                { 
                    List<FileSystemInfo> entries = mergedDirs.getFileSystemInfos(filename);
                    foreach (FileSystemInfo f in entries)
                    {
                        FileInformation fi = new FileInformation();
                        fi.Attributes = f.Attributes;
                        fi.CreationTime = f.CreationTime;
                        fi.LastAccessTime = f.LastAccessTime;
                        fi.LastWriteTime = f.LastWriteTime;
                        fi.Length = (f is DirectoryInfo) ? 0 : ((FileInfo)f).Length;
                        fi.FileName = f.Name;
                        
                        //fi.Attributes.HasFlag(FileAttributes.ReadOnly);
                        files.Add(fi);
                    }
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught an exception: " + e + ": " + e.Message);
                return -1;
            }
        }

        public int SetFileAttributes(string filename, System.IO.FileAttributes attr, DokanFileInfo info)
        {
            return 0;
        }

        public int SetFileTime(string filename, DateTime ctime, DateTime atime, DateTime mtime, DokanFileInfo info)
        {
            return 0;
        }

        public int DeleteFile(string filename, DokanFileInfo info)
        {
            try
            {
                mergedDirs.deleteFile(filename);
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught an exception " + e + " while deleting file. " + e.Message);
                return -1;
            }
        }

        public int DeleteDirectory(string filename, DokanFileInfo info)
        {
            try
            {
                mergedDirs.deleteDirectory(filename);
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught an exception " + e + " while deleting directory. " + e.Message);
                return -1;
            }
        }

        public int MoveFile(string filename, string newname, bool replace, DokanFileInfo info)
        {
           // Console.WriteLine("Asked to move '" + filename + "' to '" + newname + "'");
            if (!replace && (mergedDirs.ContainsDirectory(newname) || mergedDirs.ContainsFile(newname)))
            {
            //    Console.WriteLine("File already exists.");
                return DokanNet.ERROR_ALREADY_EXISTS;
            }
            else if (replace)
            {
             //   Console.WriteLine("File already exists, but will be replaced");
                if (mergedDirs.ContainsDirectory(newname))
                {
                    mergedDirs.deleteDirectory(newname);
                }
                else if (mergedDirs.ContainsFile(newname))
                {
                    mergedDirs.deleteFile(newname);
                }
            }

           // Console.WriteLine("Calling move on root collection.");
            mergedDirs.moveFile(filename, newname);

            return 0;
        }

        public int SetEndOfFile(string filename, long length, DokanFileInfo info)
        {
            return 0;
        }

        public int SetAllocationSize(string filename, long length, DokanFileInfo info)
        {
            return 0;
        }

        public int LockFile(string filename, long offset, long length, DokanFileInfo info)
        {
            return 0;
        }

        public int UnlockFile(string filename, long offset, long length, DokanFileInfo info)
        {
            return 0;
        }

        public int GetDiskFreeSpace(ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes, DokanFileInfo info)
        {
            try
            {
                freeBytesAvailable = mergedDirs.getFreeSpaceBytes();
                totalBytes = mergedDirs.getTotalSpaceBytes();
                totalFreeBytes = freeBytesAvailable;

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught an exception: " + e + ": " + e.Message);
                return -1;
            }
        }

        public int Unmount(DokanFileInfo info)
        {
            return 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MergeFS
{
    class RootCollection : RootDirectory
    {

        public RootCollection(IEnumerable<Root> roots)
        {
            foreach (Root r in roots)
            {
                addRoot(r);
            }
        }

        private string getRealFile(string virtualfilename)
        {
            foreach (string i in mergedDirs.Keys)
            {
                Root root = mergedDirs[i];

                if (root.ContainsFile(root.getRealPath(virtualfilename)))
                {
                    return root.getRealPath(virtualfilename);
                }
            }

            return "";
        }

        Dictionary<string, Root> mergedDirs = new Dictionary<string, Root>();

        public Root getRootWithBestSpace(ulong fileSizeBytes)
        {
            if (fileSizeBytes == 0)
            {
                return getRootWithMostSpace();
            }
            else
            {
                return getBestFittingRoot(fileSizeBytes);
            }
        }

        public Root getRootWithMostSpace()
        {
            Root bestRoot = null;
            ulong bestFreeSpace = 0;

            foreach (string i in mergedDirs.Keys)
            {
                if (bestRoot == null || mergedDirs[i].getFreeSpace() > bestFreeSpace)
                {
                    bestRoot = mergedDirs[i];
                    bestFreeSpace = mergedDirs[i].getFreeSpace();
                }
            }

            return bestRoot;
        }

        public ulong getFreeSpaceBytes()
        {
            return getRootWithMostSpace().getFreeSpaceBytes();
        }

        public Root getBestFittingRoot(ulong fileSizeBytes)
        {
            Root best = null;
            ulong bestdx = 0;

            foreach (string i in mergedDirs.Keys)
            {
                long dx = (long)mergedDirs[i].getFreeSpace() - (long)fileSizeBytes;
                if (best == null || (dx > 0 && dx < (long)bestdx))
                {
                    best = mergedDirs[i];
                    bestdx = (ulong)((dx < 0) ? -dx : dx);
                }
            }

            return best;
        }

        private void addRoot(Root root)
        {
            this.mergedDirs.Add(root.Path.ToLower(), root);
        }

        private void removeRoot(Root root)
        {
            this.mergedDirs.Remove(root.Path.ToLower());
        }


        public bool ContainsDirectory(string filename)
        {
            foreach(string i in mergedDirs.Keys)
            {
                if(mergedDirs[i].ContainsDirectory(filename))
                {
                    return true;
                }
            }
            return false;
        }

        public bool ContainsFile(string filename)
        {
            foreach (string i in mergedDirs.Keys)
            {
                if (mergedDirs[i].ContainsFile(filename))
                {
                    return true;
                }
            }
            return false;
        }

        public string getRealPath(string virtualFilename)
        {
            foreach (string i in mergedDirs.Keys)
            {
                if (mergedDirs[i].ContainsFile(virtualFilename))
                {
                    return mergedDirs[i].getRealPath(virtualFilename);
                }
            }

            foreach (string i in mergedDirs.Keys)
            {
                if (mergedDirs[i].ContainsDirectory(virtualFilename))
                {
                    return mergedDirs[i].getRealPath(virtualFilename);
                }
            }


            return "";
        }

        public void mkDirs(string virtualPath)
        {
            foreach (string i in mergedDirs.Keys)
            {
                mergedDirs[i].mkDirs(virtualPath);
            }
        }
        

        public List<System.IO.FileInfo> listFiles(string virtualPath)
        {
            List<FileInfo> output = new List<FileInfo>();
            foreach (string i in mergedDirs.Keys)
            {
                output.AddRange(mergedDirs[i].listFiles(virtualPath));
            }

            return output;
        }

        public List<System.IO.DirectoryInfo> listDirectories(string virtualPath)
        {
            List<DirectoryInfo> output = new List<DirectoryInfo>();
            foreach (string i in mergedDirs.Keys)
            {
                output.AddRange(mergedDirs[i].listDirectories(virtualPath));
            }

            return output;
        }


        public List<System.IO.FileSystemInfo> getFileSystemInfos(string virtualPath)
        {
            List<FileSystemInfo> output = new List<FileSystemInfo>();
            HashSet<string> seenDirs = new HashSet<string>();

            foreach(string i in mergedDirs.Keys)
            {
                if (mergedDirs[i].ContainsDirectory(virtualPath) || mergedDirs[i].ContainsFile(virtualPath))
                {
                    IEnumerable<FileSystemInfo> fsis = mergedDirs[i].getFileSystemInfos(virtualPath);

                    foreach (FileSystemInfo fsi in fsis)
                    {
                        if (Directory.Exists(fsi.FullName))
                        {
                            if (!seenDirs.Contains(fsi.Name))
                            {
                                // Don't duplicate directories
                                output.Add(fsi);
                                seenDirs.Add(fsi.Name);
                            }
                        }
                        else
                        {
                            //This is a file
                            output.Add(fsi);
                        }
                    }
                }
            }

            return output;
        }


        public ulong getTotalSpaceBytes()
        {
            ulong total = 0;
            foreach (string i in mergedDirs.Keys)
            {
                total += mergedDirs[i].getTotalSpaceBytes();
            }

            return total;
        }


        public void moveFile(string originalVirtualPath, string endVirtualPath)
        {
            Console.WriteLine("RootCollection moveFile called.  Calling on each root.");
            foreach (string i in mergedDirs.Keys)
            {
                Console.Write("Checking whetner " + mergedDirs[i] + " contains the file: ");
                if (mergedDirs[i].ContainsDirectory(originalVirtualPath) || mergedDirs[i].ContainsFile(originalVirtualPath))
                {
                    Console.Write("Yes\n");
                    Console.WriteLine("File '" + originalVirtualPath + "' is in root " + mergedDirs[i]);
                    mergedDirs[i].moveFile(originalVirtualPath, endVirtualPath);
                }
                Console.Write("No");
            }
        }


        public void deleteFile(string virtualPath)
        {
            foreach (string i in mergedDirs.Keys)
            {
                mergedDirs[i].deleteFile(virtualPath);
            }
        }

        public void deleteDirectory(string virtualPath)
        {
            foreach (string i in mergedDirs.Keys)
            {
                mergedDirs[i].deleteDirectory(virtualPath);
            }
        }

        public Root getRootWithFile(string virtualPath)
        {
            foreach(string i in mergedDirs.Keys)
            {
                if(mergedDirs[i].ContainsFile(virtualPath))
                {
                    return mergedDirs[i];
                }
            }

           return null;
        }

        public int createFile(string virtualPath,FileMode mode)
        {
            if (ContainsFile(virtualPath))
            {
                Root r = getRootWithFile(virtualPath);
                if (r == null)
                {
                    return Dokan.DokanNet.ERROR_FILE_NOT_FOUND;
                }
                else
                {
                    return r.createFile(virtualPath, mode);
                }
            }
            else
            {
                return getRootWithBestSpace(0).createFile(virtualPath, mode);
            }
        }
    }
}

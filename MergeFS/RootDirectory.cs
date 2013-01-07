using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MergeFS
{
    interface RootDirectory
    {
        bool ContainsDirectory(string filename);
        bool ContainsFile(string filename);

        string getRealPath(string virtualFilename);

        void mkDirs(string virtualPath);
        List<FileInfo> listFiles(string virtualPath);
        List<DirectoryInfo> listDirectories(string virtualPath);
        List<FileSystemInfo> getFileSystemInfos(string virtualPath);

        ulong getFreeSpaceBytes();
        ulong getTotalSpaceBytes();

        void moveFile(string originalVirtualPath, string endVirtualPath);

        void deleteFile(string virtualPath);
        void deleteDirectory(string virtualPath);

        int createFile(string virtualPath, FileMode mode);
    }
}

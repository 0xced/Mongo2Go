using System.IO;
using System.Runtime.InteropServices;
using Mono.Unix;

namespace MongoDownloader
{
    internal static class FileSystemInfoExtensions
    {
        public static void SetFileAccessPermissions(this FileSystemInfo fileSystemInfo, int permissions)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (UnixFileSystemInfo.TryGetFileSystemEntry(fileSystemInfo.FullName, out var unixFileInfo))
                {
                    unixFileInfo.FileAccessPermissions = (FileAccessPermissions)permissions;
                }
            }
        }
    }
}
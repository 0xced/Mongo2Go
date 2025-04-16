using System.IO;
using System.Runtime.InteropServices;
using Mono.Unix;

namespace MongoDownloader;

internal static class FileSystemInfoExtensions
{
    public static void SetFileAccessPermissions(this FileSystemInfo fileSystemInfo, int permissions)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var unixFileInfo = UnixFileSystemInfo.GetFileSystemEntry(fileSystemInfo.FullName);
            unixFileInfo.FileAccessPermissions = (FileAccessPermissions)permissions;
        }
    }
}
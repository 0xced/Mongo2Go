#if !NET10_0_OR_GREATER
using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDownloader;

internal static class ZipArchiveEntryExtensions
{
    /// <summary>
    /// A reimplementation of <see cref="ZipFileExtensions.ExtractToFile(System.IO.Compression.ZipArchiveEntry,string,bool)"/>, but async.
    /// Still waiting on <see href="https://github.com/dotnet/runtime/issues/1541">Add async ZipFile APIs</see> which should finally come to .NET 10
    /// thanks to the <see href="https://github.com/dotnet/runtime/pull/114421">Zip async implementation</see> pull request.
    /// </summary>
    public static async Task ExtractToFileAsync(this ZipArchiveEntry source, string destinationFileName, bool overwrite, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destinationFileName);

        FileStreamOptions fileStreamOptions = new()
        {
            Access = FileAccess.Write,
            Mode = overwrite ? FileMode.Create : FileMode.CreateNew,
            Share = FileShare.None,
        };

        const UnixFileMode ownershipPermissions =
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
            UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
            UnixFileMode.OtherRead | UnixFileMode.OtherWrite |  UnixFileMode.OtherExecute;

        // Restore Unix permissions.
        // For security, limit to ownership permissions, and respect umask (through UnixCreateMode).
        // We don't apply UnixFileMode.None because .zip files created on Windows and .zip files created
        // with previous versions of .NET don't include permissions.
        var mode = (UnixFileMode)(source.ExternalAttributes >> 16) & ownershipPermissions;
        if (mode != UnixFileMode.None && !OperatingSystem.IsWindows())
        {
            fileStreamOptions.UnixCreateMode = mode;
        }

        await using (var destinationStream = new FileStream(destinationFileName, fileStreamOptions))
        await using (var sourceStream = source.Open())
        {
            await sourceStream.CopyToAsync(destinationStream, cancellationToken);
        }

        AttemptSetLastWriteTime(destinationFileName, source.LastWriteTime);
    }

    private static void AttemptSetLastWriteTime(string destinationFileName, DateTimeOffset lastWriteTime)
    {
        try
        {
            File.SetLastWriteTime(destinationFileName, lastWriteTime.DateTime);
        }
        catch
        {
            // Some OSes like Android (#35374) might not support setting the last write time, the extraction should not fail because of that
        }
    }
}
#endif
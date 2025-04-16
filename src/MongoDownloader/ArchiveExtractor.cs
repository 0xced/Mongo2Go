using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;

namespace MongoDownloader;

internal class ArchiveExtractor(Options options)
{
    private readonly Options _options = options ?? throw new ArgumentNullException(nameof(options));

    public async Task<UnarchiveResult> DownloadExtractArchiveAsync(Archive archive, DirectoryInfo extractDirectory, IProgress<TransferProgress>? progress, CancellationToken cancellationToken)
    {
        _options.CacheDirectory.Create();
        var fileName = Path.GetFileName(archive.Url.AbsolutePath);
        var archiveExtension = Path.GetExtension(fileName);
        return archiveExtension switch
        {
            ".zip" => await DownloadExtractZipArchiveAsync(archive, extractDirectory, progress, cancellationToken),
            ".tgz" => await DownloadExtractTarGzipArchiveAsync(archive, extractDirectory, progress, cancellationToken),
            _ => throw new NotSupportedException($"Only .zip and .tgz archives are currently supported. \"{fileName}\" can not be extracted.")
        };
    }

    private async Task<UnarchiveResult> DownloadExtractZipArchiveAsync(Archive archive, DirectoryInfo extractDirectory, IProgress<TransferProgress>? progress, CancellationToken cancellationToken)
    {
        using (var httpStreamProgress = new HttpStreamProgress(_options.HttpClient, _options.CacheDirectory, archive.Url, progress))
        {
            using var httpStream = await httpStreamProgress.GetHttpStreamAsync(cancellationToken);
            using var zipFile = new ZipFile(httpStream);

            var binaryRegex = _options.GetBinariesRegex(archive.Product, archive.Target.Platform);
            var binaryFiles = new List<FileInfo>();

            foreach (var entry in zipFile.Cast<ZipEntry>().Where(e => e.IsFile))
            {
                var nameParts = entry.Name.Split('\\', '/').Skip(1).ToList();
                var zipEntryPath = string.Join("/", nameParts);
                var isBinaryFile = binaryRegex.IsMatch(zipEntryPath);
                if (isBinaryFile)
                {
                    var destinationFile = new FileInfo(Path.Combine(extractDirectory.FullName, nameParts.Last()));
                    destinationFile.Directory?.Create();
                    using var destinationStream = destinationFile.OpenWrite();
                    using var inputStream = zipFile.GetInputStream(entry);
                    await inputStream.CopyToAsync(destinationStream);
                    // See https://github.com/dotnet/runtime/blob/v6.0.0/src/libraries/System.IO.Compression.ZipFile/src/System/IO/Compression/ZipFileExtensions.ZipArchiveEntry.Extract.Unix.cs#L18
                    // No need to perform & 0x1FF because it's already done by the `Mono.Unix.UnixFileSystemInfo.FileAccessPermissions` setter
                    destinationFile.SetFileAccessPermissions(entry.ExternalFileAttributes >> 16);
                    binaryFiles.Add(destinationFile);
                }
            }
            return new UnarchiveResult(binaryFiles, httpStreamProgress.BytesSaved);
        }
    }

    private async Task<UnarchiveResult> DownloadExtractTarGzipArchiveAsync(Archive archive, DirectoryInfo extractDirectory, IProgress<TransferProgress>? progress, CancellationToken cancellationToken)
    {
        // See https://github.com/icsharpcode/SharpZipLib/wiki/GZip-and-Tar-Samples#-extract-from-a-tar-with-full-control
        using (var httpStreamProgress = new HttpStreamProgress(_options.HttpClient, _options.CacheDirectory, archive.Url, progress))
        {
            using var httpStream = await httpStreamProgress.GetHttpStreamAsync(cancellationToken);
            using var gzipStream = new GZipInputStream(httpStream);
            using var tarStream = new TarInputStream(gzipStream, Encoding.UTF8);

            var binaryRegex = _options.GetBinariesRegex(archive.Product, archive.Target.Platform);
            var binaryFiles = new List<FileInfo>();

            while (await tarStream.GetNextEntryAsync(cancellationToken) is { } entry)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileName = entry.Name.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
                var parts = fileName.Split(Path.DirectorySeparatorChar);
                var entryFileName = string.Join("/", parts.Skip(1));
                var isBinaryFile = binaryRegex.IsMatch(entryFileName);
                if (isBinaryFile)
                {
                    var destinationFile = new FileInfo(Path.Combine(extractDirectory.FullName, parts.Last()));
                    destinationFile.Directory?.Create();
                    using var destinationStream = destinationFile.OpenWrite();
                    await tarStream.CopyEntryContentsAsync(destinationStream, cancellationToken);
                    destinationFile.SetFileAccessPermissions(entry.TarHeader.Mode);
                    binaryFiles.Add(destinationFile);
                }
            }
            return new UnarchiveResult(binaryFiles, httpStreamProgress.BytesSaved);
        }
    }
}
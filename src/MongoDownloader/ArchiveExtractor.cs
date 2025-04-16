using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

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
            await using var httpStream = await httpStreamProgress.GetHttpStreamAsync(cancellationToken);
            using var zipArchive = new ZipArchive(httpStream);

            var binaryRegex = _options.GetBinariesRegex(archive.Product, archive.Target.Platform);
            var binaryFiles = new List<FileInfo>();

            foreach (var entry in zipArchive.Entries.Where(e => e.Name.Length > 0 && binaryRegex.IsMatch(e.FullName)))
            {
                var destinationFile = new FileInfo(Path.Combine(extractDirectory.FullName, entry.Name));
                destinationFile.Directory?.Create();
                entry.ExtractToFile(destinationFile.FullName, overwrite: true);
                binaryFiles.Add(destinationFile);
            }

            return new UnarchiveResult(binaryFiles, httpStreamProgress.BytesSaved);
        }
    }

    private async Task<UnarchiveResult> DownloadExtractTarGzipArchiveAsync(Archive archive, DirectoryInfo extractDirectory, IProgress<TransferProgress>? progress, CancellationToken cancellationToken)
    {
        // See https://github.com/icsharpcode/SharpZipLib/wiki/GZip-and-Tar-Samples#-extract-from-a-tar-with-full-control
        using (var httpStreamProgress = new HttpStreamProgress(_options.HttpClient, _options.CacheDirectory, archive.Url, progress))
        {
            await using var httpStream = await httpStreamProgress.GetHttpStreamAsync(cancellationToken);
            await using var gzipStream = new GZipInputStream(httpStream);
            await using var tarStream = new TarInputStream(gzipStream, Encoding.UTF8);

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
                    await using var destinationStream = destinationFile.OpenWrite();
                    await tarStream.CopyEntryContentsAsync(destinationStream, cancellationToken);
                    destinationFile.SetFileAccessPermissions(entry.TarHeader.Mode);
                    binaryFiles.Add(destinationFile);
                }
            }
            return new UnarchiveResult(binaryFiles, httpStreamProgress.BytesSaved);
        }
    }
}
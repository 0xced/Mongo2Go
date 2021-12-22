using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Espresso3389.HttpStream;
using HttpProgress;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;

namespace MongoDownloader
{
    internal class ArchiveExtractor
    {
        private const int CachePageSize = 4194304; // 4 MiB

        private readonly Options _options;

        public ArchiveExtractor(Options options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<IReadOnlyCollection<FileInfo>> DownloadExtractZipArchiveAsync(IArchive archive, DirectoryInfo extractDirectory, IProgress<ICopyProgress>? progress, CancellationToken cancellationToken)
        {
            var bytesTransferred = 0L;
            var archiveUrl = archive.Url;
            using var headResponse = await _options.HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, archiveUrl), cancellationToken);
            var contentLength = headResponse.Content.Headers.ContentLength ?? 0;
            var cacheFile = new FileInfo(Path.Combine(_options.CacheDirectory.FullName, archiveUrl.Segments.Last()));
            using var cacheStream = new FileStream(cacheFile.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var stopwatch = Stopwatch.StartNew();
            using var httpStream = new HttpStream(archiveUrl, cacheStream, ownStream: false, CachePageSize, cached: null, _options.HttpClient);
            httpStream.RangeDownloaded += (_, args) =>
            {
                bytesTransferred += args.Length;
                progress?.Report(new CopyProgress(stopwatch.Elapsed, 0, bytesTransferred, contentLength));
            };
            using var zipFile = new ZipFile(httpStream);
            var binaryRegex = _options.GetBinariesRegex(archive.Product, archive.Platform);
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
            progress?.Report(new CopyProgress(stopwatch.Elapsed, 0, bytesTransferred, bytesTransferred));
            return binaryFiles;
        }

        public IReadOnlyCollection<FileInfo> ExtractArchive(IArchive archive, FileInfo archiveFile, DirectoryInfo extractDirectory, CancellationToken cancellationToken)
        {
            return Path.GetExtension(archiveFile.Name) switch
            {
                ".tgz" => ExtractTarGzipArchive(archive, archiveFile, extractDirectory, cancellationToken),
                _ => throw new NotSupportedException($"Only .tgz archives are currently supported. \"{archiveFile.FullName}\" can not be extracted.")
            };
        }

        private IReadOnlyCollection<FileInfo> ExtractTarGzipArchive(IArchive archive, FileInfo archiveFile, DirectoryInfo extractDirectory, CancellationToken cancellationToken)
        {
            // See https://github.com/icsharpcode/SharpZipLib/wiki/GZip-and-Tar-Samples#-extract-from-a-tar-with-full-control
            using var archiveStream = archiveFile.OpenRead();
            using var gzipStream = new GZipInputStream(archiveStream);
            using var tarStream = new TarInputStream(gzipStream, Encoding.UTF8);
            var binaryFiles = new List<FileInfo>();

            var binaryRegex = _options.GetBinariesRegex(archive.Product, archive.Platform);
            TarEntry entry;
            while ((entry = tarStream.GetNextEntry()) != null)
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
                    tarStream.CopyEntryContents(destinationStream);
                    destinationFile.SetFileAccessPermissions(entry.TarHeader.Mode);
                    binaryFiles.Add(destinationFile);
                }
            }

            return binaryFiles;
        }
    }
}
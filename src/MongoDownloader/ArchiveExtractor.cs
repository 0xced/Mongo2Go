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
            var binaryRegex = _options.Binaries[(archive.Product, archive.Platform)];
            var licenseRegex = _options.Licenses[(archive.Product, archive.Platform)];
            var binaryFiles = new List<FileInfo>();
            foreach (var entry in zipFile.Cast<ZipEntry>().Where(e => e.IsFile))
            {
                var nameParts = entry.Name.Split('\\', '/').Skip(1).ToList();
                var zipEntryPath = string.Join("/", nameParts);
                var isBinaryFile = binaryRegex.IsMatch(zipEntryPath);
                var isLicenseFile = licenseRegex.IsMatch(zipEntryPath);
                if (isBinaryFile || isLicenseFile)
                {
                    var destinationPathParts = isLicenseFile ? nameParts.Prepend(ProductDirectoryName(archive.Product)) : nameParts;
                    var destinationFile = new FileInfo(Path.Combine(destinationPathParts.Prepend(extractDirectory.FullName).ToArray()));
                    destinationFile.Directory?.Create();
                    using var destinationStream = destinationFile.OpenWrite();
                    using var inputStream = zipFile.GetInputStream(entry);
                    await inputStream.CopyToAsync(destinationStream);
                    if (isBinaryFile)
                    {
                        binaryFiles.Add(destinationFile);
                    }
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
            // See https://github.com/icsharpcode/SharpZipLib/wiki/GZip-and-Tar-Samples#-simple-full-extract-from-a-tgz-targz
            using var archiveStream = archiveFile.OpenRead();
            using var gzipStream = new GZipInputStream(archiveStream);
            using var tarArchive = TarArchive.CreateInputTarArchive(gzipStream, Encoding.UTF8);
            var extractedFileNames = new List<string>();
            tarArchive.ProgressMessageEvent += (_, entry, _) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                extractedFileNames.Add(entry.Name);
            };
            tarArchive.ExtractContents(extractDirectory.FullName);
            return CleanupExtractedFiles(archive, extractDirectory, extractedFileNames);
        }

        private IReadOnlyCollection<FileInfo> CleanupExtractedFiles(IArchive archive, DirectoryInfo extractDirectory, IEnumerable<string> extractedFileNames)
        {
            var rootDirectoryToDelete = new HashSet<string>();
            var binaryRegex = _options.Binaries[(archive.Product, archive.Platform)];
            var licenseRegex = _options.Licenses[(archive.Product, archive.Platform)];
            var binaryFiles = new List<FileInfo>();
            foreach (var extractedFileName in extractedFileNames.Select(e => e.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar)))
            {
                var extractedFile = new FileInfo(Path.Combine(extractDirectory.FullName, extractedFileName));
                var parts = extractedFileName.Split(Path.DirectorySeparatorChar);
                var entryFileName = string.Join("/", parts.Skip(1));
                rootDirectoryToDelete.Add(parts[0]);
                var isBinaryFile = binaryRegex.IsMatch(entryFileName);
                var isLicenseFile = licenseRegex.IsMatch(entryFileName);
                if (!(isBinaryFile || isLicenseFile))
                {
                    extractedFile.Delete();
                }
                else
                {
                    var destinationPathParts = parts.Skip(1);
                    if (isLicenseFile)
                    {
                        destinationPathParts = destinationPathParts.Prepend(ProductDirectoryName(archive.Product));
                    }
                    var destinationFile = new FileInfo(Path.Combine(destinationPathParts.Prepend(extractDirectory.FullName).ToArray()));
                    destinationFile.Directory?.Create();
                    extractedFile.MoveTo(destinationFile.FullName);
                    if (isBinaryFile)
                    {
                        binaryFiles.Add(destinationFile);
                    }
                }
            }
            var rootArchiveDirectory = new DirectoryInfo(Path.Combine(extractDirectory.FullName, rootDirectoryToDelete.Single()));
            var binDirectory = new DirectoryInfo(Path.Combine(rootArchiveDirectory.FullName, "bin"));
            binDirectory.Delete(recursive: false);
            rootArchiveDirectory.Delete(recursive: false);
            return binaryFiles;
        }

        private static string ProductDirectoryName(Product product)
        {
            return product switch
            {
                Product.CommunityServer => "community-server",
                Product.DatabaseTools => "database-tools",
                _ => throw new ArgumentOutOfRangeException(nameof(product), product, null)
            };
        }
    }
}
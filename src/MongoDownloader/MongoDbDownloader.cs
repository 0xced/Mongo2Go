using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ByteSizeLib;
using HttpProgress;
using NuGet.Versioning;

namespace MongoDownloader
{
    public class MongoDbDownloader : IMongoDbDownloader
    {
        private readonly ArchiveExtractor _extractor;
        private readonly Options _options;

        public MongoDbDownloader(ArchiveExtractor extractor, Options options)
        {
            _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<IArchive> GetArchiveAsync(Product product, Platform platform, Architecture architecture, CancellationToken cancellationToken)
        {
            var version = await GetVersionAsync(product, cancellationToken);
            return GetArchive(product, platform, architecture, version);
        }

        public async Task<IReadOnlyCollection<IArchive>> GetArchivesAsync(Product product, CancellationToken cancellationToken)
        {
            var version = await GetVersionAsync(product, cancellationToken);
            return Enum.GetValues(typeof(Platform)).Cast<Platform>().SelectMany(platform => GetArchives(product, platform, version)).ToList();
        }

        public async Task<ByteSize> ProcessArchiveAsync(IArchive archive, DirectoryInfo extractDirectory, IArchiveProgress progress, CancellationToken cancellationToken)
        {
            IEnumerable<Task<ByteSize>> stripTasks;
            var archiveExtension = Path.GetExtension(archive.Url.AbsolutePath);
            if (archiveExtension == ".zip")
            {
                stripTasks = await _extractor.DownloadExtractZipArchiveAsync(archive, extractDirectory, progress, cancellationToken);
            }
            else
            {
                var archiveFile = await DownloadArchiveAsync(archive, progress, cancellationToken);
                stripTasks = _extractor.ExtractArchive(archive, archiveFile, extractDirectory, cancellationToken);
            }
            progress.Report("Stripping binaries");
            var completedStripTasks = await Task.WhenAll(stripTasks);
            var totalStrippedSize = completedStripTasks.Aggregate(new ByteSize(0), (current, strippedSize) => current + strippedSize);
            progress.ReportCompleted(totalStrippedSize);
            return totalStrippedSize;
        }

        private async Task<FileInfo> DownloadArchiveAsync(IArchive archive, IProgress<ICopyProgress> progress, CancellationToken cancellationToken)
        {
            _options.CacheDirectory.Create();
            var destinationFile = new FileInfo(Path.Combine(_options.CacheDirectory.FullName, archive.Url.Segments.Last()));
            var useCache = bool.TryParse(Environment.GetEnvironmentVariable("MONGO2GO_DOWNLOADER_USE_CACHED_FILE") ?? "", out var useCachedFile) && useCachedFile;
            if (useCache && destinationFile.Exists)
            {
                progress.Report(new CopyProgress(TimeSpan.Zero, 0, 1, 1));
                return destinationFile;
            }
            using var destinationStream = destinationFile.OpenWrite();
            await _options.HttpClient.GetAsync(archive.Url.AbsoluteUri, destinationStream, progress, cancellationToken);
            return destinationFile;
        }

        private async Task<Version> GetVersionAsync(Product product, CancellationToken cancellationToken)
        {
            var url = product switch
            {
                Product.CommunityServer => _options.CommunityServerUrl,
                Product.DatabaseTools => _options.DatabaseToolsUrl,
                _ => throw new ArgumentOutOfRangeException(nameof(product), product, $"The value of argument '{nameof(product)}' ({product}) is invalid for enum type '{nameof(Product)}'.")
            };
            var release = await _options.HttpClient.GetFromJsonAsync<Release>(url, cancellationToken) ?? throw new InvalidOperationException($"Failed to deserialize {nameof(Release)}");
            var semanticVersions = release.Versions.Select(e => new NuGetVersion(e.Number));
            var range = _options.VersionRanges[product];
            var bestMatch = range.FindBestMatch(semanticVersions) ?? throw new InvalidOperationException($"No {product} matching {range} version was found.");
            return release.Versions.Single(e => e.Number == bestMatch.OriginalVersion);
        }

        private IEnumerable<IArchive> GetArchives(Product product, Platform platform, Version version)
        {
            return _options.Architectures[platform].Select(architecture => GetArchive(product, platform, architecture, version));
        }

        private IArchive GetArchive(Product product, Platform platform, Architecture architecture, Version version)
        {
            Func<Download, string> platformName = product switch
            {
                Product.CommunityServer => download => download.Target,
                Product.DatabaseTools => download => download.Name,
                _ => throw new ArgumentOutOfRangeException(nameof(product), product, $"The value of argument '{nameof(product)}' ({product}) is invalid for enum type '{nameof(Product)}'.")
            };

            var platformRegex = _options.PlatformIdentifiers[platform];
            var editionRegex = product == Product.CommunityServer ? _options.Edition : null;

            var architectureRegex = _options.ArchitectureIdentifiers[architecture];
            var matchingDownloads = version.Downloads
                .Where(e => platformRegex.IsMatch(platformName(e)))
                .Where(e => architectureRegex.IsMatch(e.Arch))
                .Where(e => editionRegex?.IsMatch(e.Edition) ?? true)
                .ToList();

            if (matchingDownloads.Count == 0)
            {
                var downloads = version.Downloads.OrderBy(e => e.Target).ThenBy(e => e.Arch);
                var messages = Enumerable.Empty<string>()
                    .Append($"Download not found for {product} {platform}/{architecture}.")
                    .Append($"  Available downloads for version {version}:")
                    .Concat(downloads.Select(e => $"    - {platformName(e)}/{e.Arch} ({e.Edition})"));
                throw new InvalidOperationException(string.Join(Environment.NewLine, messages));
            }

            if (matchingDownloads.Count > 1)
            {
                throw new InvalidOperationException($"Found {matchingDownloads.Count} downloads for {platform}/{architecture} but expected to find only one.");
            }

            return new ArchiveInformation(product, platform, architecture, matchingDownloads[0].Archive.Url, version.Number);
        }
    }
}
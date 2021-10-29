using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ByteSizeLib;
using HttpProgress;

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

        public async Task<(IVersion version, IEnumerable<IArchive> archives)> GetArchivesAsync(Product product, CancellationToken cancellationToken)
        {
            return product switch
            {
                Product.CommunityServer => await GetCommunityServerArchivesAsync(cancellationToken),
                Product.DatabaseTools => await GetDatabaseToolsArchivesAsync(cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(product), product, $"The value of argument '{nameof(product)}' ({product}) is invalid for enum type '{nameof(Product)}'.")
            };
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

        public async Task<(IVersion version, IEnumerable<IArchive> archives)> GetCommunityServerArchivesAsync(CancellationToken cancellationToken)
        {
            var release = await _options.HttpClient.GetFromJsonAsync<Release>(_options.CommunityServerUrl, cancellationToken) ?? throw new InvalidOperationException($"Failed to deserialize {nameof(Release)}");
            var version = release.Versions.FirstOrDefault(e => e.Production) ?? throw new InvalidOperationException("No Community Server production version was found");
            var downloads = Enum.GetValues(typeof(Platform)).Cast<Platform>().SelectMany(platform => GetArchives(platform, Product.CommunityServer, version, _options, _options.Edition));
            return (version, downloads);
        }

        public async Task<(IVersion version, IEnumerable<IArchive> archives)> GetDatabaseToolsArchivesAsync(CancellationToken cancellationToken)
        {
            var release = await _options.HttpClient.GetFromJsonAsync<Release>(_options.DatabaseToolsUrl, cancellationToken) ?? throw new InvalidOperationException($"Failed to deserialize {nameof(Release)}");
            var version = release.Versions.FirstOrDefault() ?? throw new InvalidOperationException("No Database Tools version was found");
            var downloads = Enum.GetValues(typeof(Platform)).Cast<Platform>().SelectMany(platform => GetArchives(platform, Product.DatabaseTools, version, _options));
            return (version, downloads);
        }

        private static IEnumerable<Download> GetArchives(Platform platform, Product product, Version version, Options options, Regex? editionRegex = null)
        {
            var platformRegex = options.PlatformIdentifiers[platform];
            Func<Download, bool> platformPredicate = product switch
            {
                Product.CommunityServer => download => platformRegex.IsMatch(download.Target),
                Product.DatabaseTools => download => platformRegex.IsMatch(download.Name),
                _ => throw new ArgumentOutOfRangeException(nameof(product), product, $"The value of argument '{nameof(product)}' ({product}) is invalid for enum type '{nameof(Product)}'.")
            };

            foreach (var architecture in options.Architectures[platform])
            {
                var architectureRegex = options.ArchitectureIdentifiers[architecture];
                var matchingDownloads = version.Downloads
                    .Where(platformPredicate)
                    .Where(e => architectureRegex.IsMatch(e.Arch))
                    .Where(e => editionRegex?.IsMatch(e.Edition) ?? true)
                    .ToList();

                if (matchingDownloads.Count == 0)
                {
                    var downloads = version.Downloads.OrderBy(e => e.Target).ThenBy(e => e.Arch);
                    var messages = Enumerable.Empty<string>()
                        .Append($"Download not found for {platform}/{architecture}.")
                        .Append($"  Available downloads for {product} {version.Number}:")
                        .Concat(downloads.Select(e => $"    - {e.Target}/{e.Arch} ({e.Edition})"));
                    throw new InvalidOperationException(string.Join(Environment.NewLine, messages));
                }

                if (matchingDownloads.Count > 1)
                {
                    throw new InvalidOperationException($"Found {matchingDownloads.Count} downloads for {platform}/{architecture} but expected to find only one.");
                }

                var download = matchingDownloads[0];
                download.Platform = platform;
                download.Architecture = architecture;
                download.Product = product;

                yield return download;
            }
        }
    }
}
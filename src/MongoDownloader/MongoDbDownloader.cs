using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;

namespace MongoDownloader;

internal class MongoDbDownloader : IMongoDbDownloader
{
    private readonly ArchiveExtractor _extractor;
    private readonly IDownloadOptions _options;

    public MongoDbDownloader(ArchiveExtractor extractor, IDownloadOptions options)
    {
        _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<IArchive> GetArchiveAsync(Product product, Target target, CancellationToken cancellationToken)
    {
        var version = await GetVersionAsync(product, cancellationToken);
        return GetArchive(product, target, version);
    }

    public async Task<IReadOnlyCollection<IArchive>> GetArchivesAsync(Product product, IEnumerable<Target> targets, CancellationToken cancellationToken)
    {
        var version = await GetVersionAsync(product, cancellationToken);
        return targets.Select(target => GetArchive(product, target, version)).ToList();
    }

    public async Task<UnarchiveResult> ProcessArchiveAsync(IArchive archive, DirectoryInfo extractDirectory, IProgress<ITransferProgress>? progress, CancellationToken cancellationToken)
    {
        return await _extractor.DownloadExtractArchiveAsync(archive, extractDirectory, progress, cancellationToken);
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
        var semanticVersions = release.Versions.Where(e => !_options.ProductionReleaseOnly || e.IsProductionRelease).Select(e => new NuGetVersion(e.Number));
        var range = _options.GetVersionRange(product);
        var bestMatch = range.FindBestMatch(semanticVersions) ?? throw new InvalidOperationException($"No {product} version matching {range} was found.");
        return release.Versions.Single(e => e.Number == bestMatch.OriginalVersion);
    }

    private IArchive GetArchive(Product product, Target target, Version version)
    {
        Func<Download, string> getPlatformName = product switch
        {
            Product.CommunityServer => download => download.Target,
            Product.DatabaseTools => download => download.Name,
            _ => throw new ArgumentOutOfRangeException(nameof(product), product, $"The value of argument '{nameof(product)}' ({product}) is invalid for enum type '{nameof(Product)}'.")
        };

        var platformName = _options.GetPlatformName(target.Platform);
        var edition = product == Product.CommunityServer ? _options.GetEdition(target.Platform) : null;

        var architectureRegex = _options.GetArchitecturesRegex(target.Architecture);
        var matchingDownloads = version.Downloads
            .Where(e => platformName == getPlatformName(e))
            .Where(e => architectureRegex.IsMatch(e.Arch))
            .Where(e => edition == null || edition == e.Edition)
            .ToList();

        if (matchingDownloads.Count == 0)
        {
            var downloads = version.Downloads.OrderBy(e => getPlatformName(e)).ThenBy(e => e.Arch);
            var messages = Enumerable.Empty<string>()
                .Append($"Download not found for {product} {target}.")
                .Append($"  Available downloads for version {version.Number}:")
                .Concat(downloads.Select(e => $"    - {getPlatformName(e)}/{e.Arch} ({e.Edition})"));
            throw new InvalidOperationException(string.Join(Environment.NewLine, messages));
        }

        if (matchingDownloads.Count > 1)
        {
            throw new InvalidOperationException($"Found {matchingDownloads.Count} downloads for {product} {target} but expected to find only one.");
        }

        return new ArchiveInformation(product, target, matchingDownloads[0].Archive.Url, version.Number);
    }
}
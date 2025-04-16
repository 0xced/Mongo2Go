using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;

namespace MongoDownloader;

public class MongoDbDownloader(Options options)
{
    private readonly Options _options = options ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Gets an <see cref="Archive"/> for the specified <paramref name="product"/> and <paramref name="target"/>.
    /// </summary>
    /// <param name="product">The MongoDB <see cref="Product"/></param>
    /// <param name="target">The <see cref="Target"/> where the MongoDB product can run.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="Archive"/> that can be passed to <see cref="ProcessArchiveAsync"/>.</returns>
    public async Task<Archive> GetArchiveAsync(Product product, Target target, CancellationToken cancellationToken)
    {
        var version = await GetVersionAsync(product, cancellationToken);
        return GetArchive(product, target, version);
    }

    /// <summary>
    /// Gets a collection <see cref="Archive"/> for the specified <paramref name="product"/> and <paramref name="targets"/>.
    /// </summary>
    /// <param name="product">The MongoDB <see cref="Product"/></param>
    /// <param name="targets">A collection of <see cref="Target"/>s where the MongoDB product can run.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>A collection of <see cref="Archive"/> that can be passed to <see cref="ProcessArchiveAsync"/>.</returns>
    public async Task<IReadOnlyCollection<Archive>> GetArchivesAsync(Product product, IEnumerable<Target> targets, CancellationToken cancellationToken)
    {
        var version = await GetVersionAsync(product, cancellationToken);
        return targets.Select(target => GetArchive(product, target, version)).ToList();
    }

    /// <summary>
    /// Downloads, then extracts the <paramref name="archive"/> into the specified <paramref name="extractDirectory"/>.
    /// </summary>
    /// <param name="archive">The <see cref="Archive"/> to download and extract.</param>
    /// <param name="extractDirectory">The directory where to extract the archive binary files.</param>
    /// <param name="progress">An optional <seealso cref="IProgress{T}"/> that can be used to track the download progress.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>The <see cref="UnarchiveResult"/> object holding the extracted files from the archive and the bytes saved.</returns>
    public async Task<UnarchiveResult> ProcessArchiveAsync(Archive archive, DirectoryInfo extractDirectory, IProgress<TransferProgress>? progress, CancellationToken cancellationToken)
    {
        var extractor = new ArchiveExtractor(_options);
        return await extractor.DownloadExtractArchiveAsync(archive, extractDirectory, progress, cancellationToken);
    }

    private async Task<VersionModel> GetVersionAsync(Product product, CancellationToken cancellationToken)
    {
        var url = product switch
        {
            Product.CommunityServer => _options.CommunityServerUrl,
            Product.DatabaseTools => _options.DatabaseToolsUrl,
            _ => throw new ArgumentOutOfRangeException(nameof(product), product, $"The value of argument '{nameof(product)}' ({product}) is invalid for enum type '{nameof(Product)}'.")
        };
        var release = await _options.HttpClient.GetFromJsonAsync<ReleaseModel>(url, cancellationToken) ?? throw new InvalidOperationException($"Failed to deserialize {nameof(ReleaseModel)}");
        var semanticVersions = release.Versions.Where(e => !_options.ProductionReleaseOnly || e.IsProductionRelease).Select(e => new NuGetVersion(e.Number));
        var range = _options.GetVersionRange(product);
        var bestMatch = range.FindBestMatch(semanticVersions) ?? throw new InvalidOperationException($"No {product} version matching {range} was found.");
        return release.Versions.Single(e => e.Number == bestMatch.OriginalVersion);
    }

    private Archive GetArchive(Product product, Target target, VersionModel version)
    {
        Func<DownloadModel, string> getPlatformName = product switch
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

        var download = matchingDownloads[0];
        var archiveUrl = download.Archive.Url;
        if (archiveUrl == null)
        {
            throw new InvalidOperationException($"The archive URL for {product} {version.Number} ({download.Arch}) is missing.");
        }

        return new Archive
        {
            Product = product,
            Target = target,
            Url = archiveUrl,
            Version = version.Number,
        };
    }
}
using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using NuGet.Versioning;

namespace MongoDownloader;

/// <summary>
/// Options to configure how to download the MongoDB Community Server and Database Tools binaries.
/// </summary>
public class Options
{
    /// <summary>
    /// The <see cref="HttpClient"/> instance used to fetch data over HTTP.
    /// </summary>
    public virtual HttpClient HttpClient { get; init; } = new();

    /// <summary>
    /// The URL of the MongoDB Community Server download information JSON.
    /// </summary>
    public virtual Uri CommunityServerUrl { get; init; } = new("https://s3.amazonaws.com/downloads.mongodb.org/current.json");

    /// <summary>
    /// The URL of the MongoDB Database Tools download information JSON.
    /// </summary>
    public virtual Uri DatabaseToolsUrl { get; init; } = new("https://s3.amazonaws.com/downloads.mongodb.org/tools/db/release.json");

    /// <summary>
    /// The directory to store the downloaded archive files.
    /// </summary>
    public virtual DirectoryInfo CacheDirectory { get; init; } = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.InternetCache), nameof(MongoDownloader)));

    /// <summary>
    /// Whether to consider production releases only.
    /// </summary>
    public bool ProductionReleaseOnly { get; init; } = true;

    /// <summary>
    /// The edition of the archive to download.
    /// </summary>
    /// <remarks>Windows and macOS use <c>base</c> and Linux uses <c>targeted</c> for the community edition.</remarks>
    public virtual string GetEdition(OSPlatform platform)
    {
        return platform == OSPlatform.Linux ? "targeted" : "base";
    }

    /// <summary>
    /// The platform name used to identify platform-specific archives to download.
    /// </summary>
    public virtual string GetPlatformName(OSPlatform platform)
    {
        if (platform == OSPlatform.Linux)
            return "ubuntu2004";

        if (platform == OSPlatform.OSX)
            return "macos";

        if (platform == OSPlatform.Windows)
            return "windows";

        throw new PlatformNotSupportedException();
    }

    /// <summary>
    /// The regular expressions used to identify the specified <paramref name="architecture"/>.
    /// </summary>
    public virtual Regex GetArchitecturesRegex(Architecture architecture)
    {
        return architecture switch
        {
            Architecture.Arm64 => new("arm64|aarch64", RegexOptions.IgnoreCase),
            Architecture.X64 => new("x86_64", RegexOptions.IgnoreCase),
            _ => throw new NotSupportedException($"The architecture {architecture} is not supported.")
        };
    }

    /// <summary>
    /// A regular expression describing how to match MongoDB binaries inside the archives.
    /// </summary>
    /// <param name="product">The <see cref="Product"/> to match binaries for.</param>
    /// <param name="platform">The <see cref="OSPlatform"/> to match binaries for.</param>
    public virtual Regex GetBinariesRegex(Product product, OSPlatform platform)
    {
        var regex = product switch
        {
            Product.CommunityServer => "bin/mongod",
            Product.DatabaseTools => "bin/(mongoexport|mongoimport)",
            _ => throw new ArgumentOutOfRangeException(nameof(product), product, $"The value of argument '{nameof(product)}' ({product}) is invalid for enum type '{nameof(Product)}'.")
        };
        return platform == OSPlatform.Windows ? new Regex(regex + @"\.exe") : new Regex(regex);
    }

    /// <summary>
    /// Gets a NuGet version range to match against all available versions.
    /// The version to download is determined by applying <see cref="VersionRange.FindBestMatch"/> on all the available versions.
    /// <para/>
    /// See https://docs.microsoft.com/en-us/nuget/concepts/package-versioning#version-ranges for version ranges documentation.
    /// The default range represents the latest stable version.
    /// <example>
    /// To get the latest version 3 of the Community Server binaries: <code>VersionRange.Parse("3.*")</code>
    /// </example>
    /// </summary>
    /// <param name="product">The <see cref="Product"/> for which to get the version range.</param>
    public virtual VersionRange GetVersionRange(Product product)
    {
        // Equivalent to VersionRange.AllStableFloating but without the obsolete warning. We don't care about round tripping in an assets file, we just want the latest stable version by default.
        return new VersionRange(minVersion: new NuGetVersion(0, 0, 0), includeMaxVersion: true, floatRange: new FloatRange(NuGetVersionFloatBehavior.Major));
    }
}
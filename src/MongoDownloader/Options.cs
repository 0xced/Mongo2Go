using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using NuGet.Versioning;

namespace MongoDownloader
{
    /// <summary>
    /// Options to configure how to download the MongoDB Community Server and Database Tools binaries.
    /// </summary>
    public class Options
    {
        /// <summary>
        /// The <see cref="HttpClient"/> instance used to fetch data over HTTP.
        /// </summary>
        public HttpClient HttpClient { get; init; } = new();

        /// <summary>
        /// The URL of the MongoDB Community Server download information JSON.
        /// </summary>
        public string CommunityServerUrl { get; init; } = "https://s3.amazonaws.com/downloads.mongodb.org/current.json";

        /// <summary>
        /// The URL of the MongoDB Database Tools download information JSON.
        /// </summary>
        public string DatabaseToolsUrl { get; init; } = "https://s3.amazonaws.com/downloads.mongodb.org/tools/db/release.json";

        /// <summary>
        /// The directory to store the downloaded archive files.
        /// </summary>
        public DirectoryInfo CacheDirectory { get; init; } = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.InternetCache), nameof(MongoDownloader)));

        /// <summary>
        /// The architectures to download for a given platform.
        /// </summary>
        public IReadOnlyDictionary<OSPlatform, IReadOnlyCollection<Architecture>> Architectures { get; init; } = new Dictionary<OSPlatform, IReadOnlyCollection<Architecture>>
        {
            [OSPlatform.Linux] = new[] { Architecture.Arm64, Architecture.X64 },
            [OSPlatform.OSX] = new[] { Architecture.X64 },
            [OSPlatform.Windows] = new[] { Architecture.X64 },
        };

        /// <summary>
        /// The edition of the archive to download.
        /// </summary>
        /// <remarks>macOS and Windows use <c>base</c> and Linux uses <c>targeted</c> for the community edition</remarks>
        public Regex Edition { get; init; } = new(@"base|targeted");

        /// <summary>
        /// The regular expressions used to identify platform-specific archives to download.
        /// </summary>
        public IReadOnlyDictionary<OSPlatform, Regex> PlatformIdentifiers { get; init; } = new Dictionary<OSPlatform, Regex>
        {
            [OSPlatform.Linux] = new(@"ubuntu2004", RegexOptions.IgnoreCase),
            [OSPlatform.OSX] = new(@"macOS", RegexOptions.IgnoreCase),
            [OSPlatform.Windows] = new(@"windows", RegexOptions.IgnoreCase),
        };

        /// <summary>
        /// The regular expressions used to identify architectures to download.
        /// </summary>
        public IReadOnlyDictionary<Architecture, Regex> ArchitectureIdentifiers { get; init; } = new Dictionary<Architecture, Regex>
        {
            [Architecture.Arm64] = new("arm64|aarch64", RegexOptions.IgnoreCase),
            [Architecture.X64] = new("x86_64", RegexOptions.IgnoreCase),
        };

        /// <summary>
        /// A dictionary describing how to match MongoDB binaries inside the zip archives.
        /// <para/>
        /// The key is a tuple with the <see cref="Product"/>/<see cref="OSPlatform"/> and the
        /// value is a regular expressions to match against the zip file name entry.
        /// </summary>
        public IReadOnlyDictionary<(Product, OSPlatform), Regex> Binaries { get; init; } = new Dictionary<(Product, OSPlatform), Regex>
        {
            [(Product.CommunityServer, OSPlatform.Linux)]   = new(@"bin/mongod"),
            [(Product.CommunityServer, OSPlatform.OSX)]     = new(@"bin/mongod"),
            [(Product.CommunityServer, OSPlatform.Windows)] = new(@"bin/mongod\.exe"),
            [(Product.DatabaseTools,   OSPlatform.Linux)]   = new(@"bin/(mongoexport|mongoimport)"),
            [(Product.DatabaseTools,   OSPlatform.OSX)]     = new(@"bin/(mongoexport|mongoimport)"),
            [(Product.DatabaseTools,   OSPlatform.Windows)] = new(@"bin/(mongoexport|mongoimport)\.exe"),
        };

        /// <summary>
        /// A dictionary describing how to match licence files inside the zip archives.
        /// <para/>
        /// The key is a tuple with the <see cref="Product"/>/<see cref="OSPlatform"/> and the
        /// value is a regular expressions to match against the zip file name entry.
        /// </summary>
        public IReadOnlyDictionary<(Product, OSPlatform), Regex> Licenses { get; init; } = new Dictionary<(Product, OSPlatform), Regex>
        {
            // The regular expression matches anything at the zip top level, i.e. does not contain any slash (/) character
            [(Product.CommunityServer, OSPlatform.Linux)]   = new(@"^[^/]+$"),
            [(Product.CommunityServer, OSPlatform.OSX)]     = new(@"^[^/]+$"),
            [(Product.CommunityServer, OSPlatform.Windows)] = new(@"^[^/]+$"),
            [(Product.DatabaseTools,   OSPlatform.Linux)]   = new(@"^[^/]+$"),
            [(Product.DatabaseTools,   OSPlatform.OSX)]     = new(@"^[^/]+$"),
            [(Product.DatabaseTools,   OSPlatform.Windows)] = new(@"^[^/]+$"),
        };

        /// <summary>
        /// A dictionary describing the NuGet version range to match against all available versions.
        /// The version to download is determined by applying <see cref="VersionRange.FindBestMatch"/> on all the available versions.
        /// <para/>
        /// By default, uses the latest stable version.
        /// <para/>
        /// See https://docs.microsoft.com/en-us/nuget/concepts/package-versioning#version-ranges for version ranges documentation.
        /// <example>
        /// To get the latest version 3 of the Community Server binaries:
        /// <code>
        /// [Product.CommunityServer] = VersionRange.Parse("3.*")
        /// </code>
        /// </example>
        /// </summary>
        public IReadOnlyDictionary<Product, VersionRange> VersionRanges { get; init; } = new Dictionary<Product, VersionRange>
        {
            [Product.CommunityServer] = new(VersionRange.AllStable, new FloatRange(NuGetVersionFloatBehavior.Major)),
            [Product.DatabaseTools] = new(VersionRange.AllStable, new FloatRange(NuGetVersionFloatBehavior.Major)),
        };
    }
}
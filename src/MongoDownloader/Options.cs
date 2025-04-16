using System;
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
    public class Options : IDownloadOptions, IExtractOptions
    {
        /// <inheritdoc />
        public virtual HttpClient HttpClient { get; init; } = new();

        /// <inheritdoc />
        public virtual Uri CommunityServerUrl { get; init; } = new("https://s3.amazonaws.com/downloads.mongodb.org/current.json");

        /// <inheritdoc />
        public virtual Uri DatabaseToolsUrl { get; init; } = new("https://s3.amazonaws.com/downloads.mongodb.org/tools/db/release.json");

        /// <inheritdoc />
        public virtual DirectoryInfo CacheDirectory { get; init; } = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.InternetCache), nameof(MongoDownloader)));

        /// <inheritdoc />
        public bool ProductionReleaseOnly { get; init; } = true;

        /// <inheritdoc />
        public virtual string GetEdition(OSPlatform platform)
        {
            return platform == OSPlatform.Linux ? "targeted" : "base";
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public virtual Regex GetArchitecturesRegex(Architecture architecture)
        {
            return architecture switch
            {
                Architecture.Arm64 => new("arm64|aarch64", RegexOptions.IgnoreCase),
                Architecture.X64 => new("x86_64", RegexOptions.IgnoreCase),
                _ => throw new NotSupportedException($"The architecture {architecture} is not supported.")
            };
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public virtual VersionRange GetVersionRange(Product product)
        {
            // Equivalent to VersionRange.AllStableFloating but without the obsolete warning. We don't care about round tripping in an assets file, we just want the latest stable version by default.
            return new VersionRange(minVersion: new NuGetVersion(0, 0, 0), includeMaxVersion: true, floatRange: new FloatRange(NuGetVersionFloatBehavior.Major));
        }
    }
}
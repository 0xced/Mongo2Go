using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using NuGet.Versioning;

namespace MongoDownloader
{
    internal interface IDownloadOptions : IHttpOptions
    {
        /// <summary>
        /// The URL of the MongoDB Community Server download information JSON.
        /// </summary>
        Uri CommunityServerUrl { get; }

        /// <summary>
        /// The URL of the MongoDB Database Tools download information JSON.
        /// </summary>
        Uri DatabaseToolsUrl { get; }

        /// <summary>
        /// Whether to consider production releases only.
        /// </summary>
        bool ProductionReleaseOnly { get; }

        /// <summary>
        /// Gets a NuGet version range to match against all available versions.
        /// The version to download is determined by applying <see cref="VersionRange.FindBestMatch"/> on all the available versions.
        /// <para/>
        /// See https://docs.microsoft.com/en-us/nuget/concepts/package-versioning#version-ranges for version ranges documentation.
        /// <example>
        /// To get the latest version 3 of the Community Server binaries: <code>VersionRange.Parse("3.*")</code>
        /// </example>
        /// </summary>
        /// <param name="product">The <see cref="Product"/> </param>
        VersionRange GetVersionRange(Product product);

        /// <summary>
        /// The edition of the archive to download.
        /// </summary>
        /// <remarks>Windows and macOS use <c>base</c> and Linux uses <c>targeted</c> for the community edition.</remarks>
        string GetEdition(OSPlatform platform);

        /// <summary>
        /// The platform name used to identify platform-specific archives to download.
        /// </summary>
        string GetPlatformName(OSPlatform platform);

        /// <summary>
        /// The regular expressions used to identify the specified <paramref name="architecture"/>.
        /// </summary>
        Regex GetArchitecturesRegex(Architecture architecture);
    }
}
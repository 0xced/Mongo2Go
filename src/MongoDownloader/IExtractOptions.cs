using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace MongoDownloader;

internal interface IExtractOptions : IHttpOptions
{
    /// <summary>
    /// The directory to store the downloaded archive files.
    /// </summary>
    DirectoryInfo CacheDirectory { get; }

    /// <summary>
    /// A regular expression describing how to match MongoDB binaries inside the archives.
    /// </summary>
    /// <param name="product">The <see cref="Product"/> to match binaries for.</param>
    /// <param name="platform">The <see cref="OSPlatform"/> to match binaries for.</param>
    Regex GetBinariesRegex(Product product, OSPlatform platform);
}
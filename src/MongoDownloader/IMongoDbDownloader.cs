using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HttpProgress;

namespace MongoDownloader
{
    /// <summary>
    /// Defines how to retrieve and extract binaries from MongoDB archives.
    /// </summary>
    public interface IMongoDbDownloader
    {
        /// <summary>
        /// Gets an <see cref="IArchive"/> for the specified <paramref name="product"/>, <paramref name="platform"/> and <paramref name="architecture"/>.
        /// </summary>
        /// <param name="product">The MongoDB <see cref="Product"/></param>
        /// <param name="platform">The <see cref="OSPlatform"/> where the MongoDB product can run.</param>
        /// <param name="architecture">The <see cref="Architecture"/> where the MongoDB product can run.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns>An <see cref="IArchive"/> that can be passed to <see cref="ProcessArchiveAsync"/>.</returns>
        Task<IArchive> GetArchiveAsync(Product product, OSPlatform platform, Architecture architecture, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a collection <see cref="IArchive"/> for the specified <paramref name="product"/> that can contain multiple <see cref="OSPlatform"/> and <see cref="Architecture"/> combination.
        /// </summary>
        /// <param name="product">The MongoDB <see cref="Product"/></param>
        /// <param name="platforms">The supported <see cref="OSPlatform"/>.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns>A collection of <see cref="IArchive"/> that can be passed to <see cref="ProcessArchiveAsync"/>.</returns>
        Task<IReadOnlyCollection<IArchive>> GetArchivesAsync(Product product, IEnumerable<OSPlatform> platforms, CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads, then extracts the <paramref name="archive"/> into the specified <paramref name="extractDirectory"/>.
        /// </summary>
        /// <param name="archive">The <see cref="IArchive"/> to download and extract.</param>
        /// <param name="extractDirectory">The directory where to extract the archive binary files.</param>
        /// <param name="progress">An optional <seealso cref="IProgress{T}"/> that can be used to track the download progress.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns>A collection of <seealso cref="FileInfo"/> representing the extracted files from the archive.</returns>
        Task<IReadOnlyCollection<FileInfo>> ProcessArchiveAsync(IArchive archive, DirectoryInfo extractDirectory, IProgress<ICopyProgress>? progress, CancellationToken cancellationToken = default);
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDownloader
{
    /// <summary>
    /// Defines how to retrieve and extract binaries from MongoDB archives.
    /// </summary>
    public interface IMongoDbDownloader
    {
        /// <summary>
        /// Gets an <see cref="IArchive"/> for the specified <paramref name="product"/> and <paramref name="target"/>.
        /// </summary>
        /// <param name="product">The MongoDB <see cref="Product"/></param>
        /// <param name="target">The <see cref="Target"/> where the MongoDB product can run.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns>An <see cref="IArchive"/> that can be passed to <see cref="ProcessArchiveAsync"/>.</returns>
        Task<IArchive> GetArchiveAsync(Product product, Target target, CancellationToken cancellationToken);

        /// <summary>
        /// Gets a collection <see cref="IArchive"/> for the specified <paramref name="product"/> and <paramref name="targets"/>.
        /// </summary>
        /// <param name="product">The MongoDB <see cref="Product"/></param>
        /// <param name="targets">A collection of <see cref="Target"/>s where the MongoDB product can run.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns>A collection of <see cref="IArchive"/> that can be passed to <see cref="ProcessArchiveAsync"/>.</returns>
        Task<IReadOnlyCollection<IArchive>> GetArchivesAsync(Product product, IEnumerable<Target> targets, CancellationToken cancellationToken);

        /// <summary>
        /// Downloads, then extracts the <paramref name="archive"/> into the specified <paramref name="extractDirectory"/>.
        /// </summary>
        /// <param name="archive">The <see cref="IArchive"/> to download and extract.</param>
        /// <param name="extractDirectory">The directory where to extract the archive binary files.</param>
        /// <param name="progress">An optional <seealso cref="IProgress{T}"/> that can be used to track the download progress.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns>The <see cref="UnarchiveResult"/> object holding the extracted files from the archive and the bytes saved.</returns>
        Task<UnarchiveResult> ProcessArchiveAsync(IArchive archive, DirectoryInfo extractDirectory, IProgress<ITransferProgress>? progress, CancellationToken cancellationToken);
    }
}
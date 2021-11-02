using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HttpProgress;

namespace MongoDownloader
{
    public interface IMongoDbDownloader
    {
        Task<IArchive> GetArchiveAsync(Product product, OSPlatform platform, Architecture architecture, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<IArchive>> GetArchivesAsync(Product product, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<FileInfo>> ProcessArchiveAsync(IArchive archive, DirectoryInfo extractDirectory, IProgress<ICopyProgress>? progress, CancellationToken cancellationToken = default);
    }
}
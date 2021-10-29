using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ByteSizeLib;

namespace MongoDownloader
{
    public interface IMongoDbDownloader
    {
        Task<IArchive> GetArchiveAsync(Product product, Platform platform, Architecture architecture, CancellationToken cancellationToken);
        Task<IReadOnlyCollection<IArchive>> GetArchivesAsync(Product product, CancellationToken cancellationToken);
        Task<ByteSize> ProcessArchiveAsync(IArchive archive, DirectoryInfo extractDirectory, IArchiveProgress progress, CancellationToken cancellationToken);
    }
}
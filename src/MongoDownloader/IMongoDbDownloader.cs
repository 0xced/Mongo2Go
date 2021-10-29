using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ByteSizeLib;

namespace MongoDownloader
{
    public interface IMongoDbDownloader
    {
        Task<(IVersion version, IEnumerable<IArchive> archives)> GetArchivesAsync(Product product, CancellationToken cancellationToken);
        Task<ByteSize> ProcessArchiveAsync(IArchive archive, DirectoryInfo extractDirectory, IArchiveProgress progress, CancellationToken cancellationToken);
    }
}
using System.Collections.Generic;
using System.Linq;
using ByteSizeLib;

namespace MongoDownloader.CLI;

internal static class ByteSizeExtensions
{
    public static ByteSize Sum(this IEnumerable<ByteSize> byteSizes)
    {
        return byteSizes.Aggregate(new ByteSize(0), (current, byteSize) => current + byteSize);
    }
}
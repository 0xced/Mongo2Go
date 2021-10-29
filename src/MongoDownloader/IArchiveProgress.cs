using System;
using ByteSizeLib;
using HttpProgress;

namespace MongoDownloader
{
    public interface IArchiveProgress : IProgress<ICopyProgress>
    {
        void Report(string description);
        void ReportCompleted(ByteSize strippedSize);
    }
}
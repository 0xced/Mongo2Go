using System;

namespace MongoDownloader;

public interface ITransferProgress
{
    TimeSpan ElapsedTime { get; }
    long TransferredBytes { get; }
    long TotalBytes { get; }
}
using System;

namespace MongoDownloader;

internal class TransferProgress : ITransferProgress
{
    public TransferProgress(TimeSpan elapsedTime, long transferredBytes, long totalBytes)
    {
        ElapsedTime = elapsedTime;
        TransferredBytes = transferredBytes;
        TotalBytes = totalBytes;
    }

    public TimeSpan ElapsedTime { get; }
    public long TransferredBytes { get; }
    public long TotalBytes { get; }
}
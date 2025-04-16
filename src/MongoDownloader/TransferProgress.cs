using System;

namespace MongoDownloader;

public class TransferProgress
{
    /// <summary>
    /// The elapsed time since the download was initiated.
    /// </summary>
    public required TimeSpan ElapsedTime { get; init; }

    /// <summary>
    /// The current number of bytes transferred.
    /// </summary>
    public required long TransferredBytes { get; init; }

    /// <summary>
    /// The total number of bytes for the download or <see langword="null"/> if unknown (because the HTTP Content-Length header was missing).
    /// </summary>
    public required long? TotalBytes { get; init; }
}
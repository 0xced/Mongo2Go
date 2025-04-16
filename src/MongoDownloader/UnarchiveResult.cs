using System.Collections.Generic;
using System.IO;

namespace MongoDownloader;

/// <summary>
/// Holds information about the result of extracting an archive.
/// </summary>
/// <param name="ExtractedFiles">A collection of <seealso cref="FileInfo"/> representing the extracted files from the archive.</param>
/// <param name="DownloadBytesSaved">The number of bytes that were not downloaded in a zip file.</param>
public record UnarchiveResult(IReadOnlyCollection<FileInfo> ExtractedFiles, long DownloadBytesSaved);
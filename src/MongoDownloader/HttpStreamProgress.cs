using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Espresso3389.HttpStream;

namespace MongoDownloader
{
    internal class HttpStreamProgress : IDisposable
    {
        private const int CachePageSize = 4194304; // 4 MiB

        private readonly HttpClient _httpClient;
        private readonly DirectoryInfo _cacheDirectory;
        private readonly Uri _archiveUrl;
        private readonly IProgress<ITransferProgress>? _progress;
        private readonly Stopwatch _stopwatch;
        private long _bytesTransferred;

        public HttpStreamProgress(HttpClient httpClient, DirectoryInfo cacheDirectory, Uri archiveUrl, IProgress<ITransferProgress>? progress)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _cacheDirectory = cacheDirectory ?? throw new ArgumentNullException(nameof(cacheDirectory));
            _archiveUrl = archiveUrl ?? throw new ArgumentNullException(nameof(archiveUrl));
            _progress = progress;
            _stopwatch = new Stopwatch();
        }

        public async Task<HttpStream> GetHttpStreamAsync(CancellationToken cancellationToken)
        {
            using var headResponse = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, _archiveUrl), cancellationToken);
            var contentLength = headResponse.Content.Headers.ContentLength ?? 0;
            var cacheFile = new FileInfo(Path.Combine(_cacheDirectory.FullName, _archiveUrl.Segments.Last()));
            var cacheStream = new FileStream(cacheFile.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var httpStream = await HttpStream.CreateAsync(_archiveUrl, cacheStream, ownStream: true, CachePageSize, cached: null, _httpClient, cancellationToken);
            httpStream.RangeDownloaded += (_, args) =>
            {
                _bytesTransferred += args.Length;
                _progress?.Report(new TransferProgress(_stopwatch.Elapsed, _bytesTransferred, contentLength));
            };
            _stopwatch.Start();
            return httpStream;
        }

        public void Dispose()
        {
            _progress?.Report(new TransferProgress(_stopwatch.Elapsed, _bytesTransferred, _bytesTransferred));
        }
    }
}
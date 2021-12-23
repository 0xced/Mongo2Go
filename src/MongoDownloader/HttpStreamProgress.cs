using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Espresso3389.HttpStream;
using HttpProgress;

namespace MongoDownloader
{
    internal class HttpStreamProgress : IDisposable
    {
        private const int CachePageSize = 4194304; // 4 MiB

        private readonly HttpClient _httpClient;
        private readonly DirectoryInfo _cacheDirectory;
        private readonly Uri _archiveUrl;
        private readonly IProgress<ICopyProgress>? _progress;
        private readonly Stopwatch _stopwatch;
        private long _bytesTransferred;

        public HttpStreamProgress(HttpClient httpClient, DirectoryInfo cacheDirectory, Uri archiveUrl, IProgress<ICopyProgress>? progress)
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
            var httpStream = new HttpStream(_archiveUrl, cacheStream, ownStream: true, CachePageSize, cached: null, _httpClient);
            httpStream.RangeDownloaded += (_, args) =>
            {
                _bytesTransferred += args.Length;
                _progress?.Report(new CopyProgress(_stopwatch.Elapsed, 0, _bytesTransferred, contentLength));
            };
            _stopwatch.Start();
            return httpStream;
        }

        public void Dispose()
        {
            _progress?.Report(new CopyProgress(_stopwatch.Elapsed, 0, _bytesTransferred, _bytesTransferred));
        }
    }
}
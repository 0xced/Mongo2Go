using System.Net.Http;

namespace MongoDownloader;

internal interface IHttpOptions
{
    /// <summary>
    /// The <see cref="HttpClient"/> instance used to fetch data over HTTP.
    /// </summary>
    HttpClient HttpClient { get; }
}
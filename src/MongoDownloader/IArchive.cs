using System;
using System.Runtime.InteropServices;

namespace MongoDownloader
{
    public interface IArchive
    {
        Product Product { get; }
        Platform Platform { get; }
        Architecture Architecture { get; }
        Uri Url { get; }
        string Version { get; }
    }
}
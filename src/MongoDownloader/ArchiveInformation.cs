using System;
using System.Runtime.InteropServices;

namespace MongoDownloader
{
    internal class ArchiveInformation : IArchive
    {
        public ArchiveInformation(Product product, Platform platform, Architecture architecture, Uri url, string version)
        {
            Product = product;
            Platform = platform;
            Architecture = architecture;
            Url = url;
            Version = version;
        }

        public Product Product { get; }
        public Platform Platform { get; }
        public Architecture Architecture { get; }
        public Uri Url { get; }
        public string Version { get; }

        public override string ToString() => $"{Product} {Version} for {Platform}/{Architecture.ToString().ToLowerInvariant()}";
    }
}
using System;
using System.Runtime.InteropServices;

namespace MongoDownloader
{
    internal class ArchiveInformation : IArchive
    {
        public ArchiveInformation(Product product, OSPlatform platform, Architecture architecture, Uri url, string version)
        {
            Product = product;
            Platform = platform;
            Architecture = architecture;
            Url = url;
            Version = version;
        }

        public Product Product { get; }
        public OSPlatform Platform { get; }
        public Architecture Architecture { get; }
        public Uri Url { get; }
        public string Version { get; }

        public override string ToString() => $"{Product} {Version} for {Platform.Pretty()}/{Architecture.ToString().ToLowerInvariant()}";
    }
}
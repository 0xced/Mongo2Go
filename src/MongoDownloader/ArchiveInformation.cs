using System;

namespace MongoDownloader
{
    internal class ArchiveInformation : IArchive
    {
        public ArchiveInformation(Product product, Target target, Uri url, string version)
        {
            Product = product;
            Target = target;
            Url = url;
            Version = version;
        }

        public Product Product { get; }
        public Target Target { get; }
        public Uri Url { get; }
        public string Version { get; }

        public override string ToString() => $"{Product} {Version} for {Target}";
    }
}
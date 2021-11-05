namespace MongoDownloader
{
    /// <summary>
    /// Factory to create an <see cref="IMongoDbDownloader"/>.
    /// </summary>
    public static class MongoDbDownloaderFactory
    {
        /// <summary>
        /// Create an <see cref="IMongoDbDownloader"/>.
        /// </summary>
        /// <param name="options">The <see cref="Options"/> to configure how to download the MongoDB Community Server and Database Tools binaries.</param>
        /// <returns>An <see cref="IMongoDbDownloader"/> to download the MongoDB Community Server and Database Tools binaries.</returns>
        public static IMongoDbDownloader Create(Options options)
        {
            var archiveExtractor = new ArchiveExtractor(options);
            return new MongoDbDownloader(archiveExtractor, options);
        }
    }
}
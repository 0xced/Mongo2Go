namespace MongoDownloader
{
    /// <summary>
    /// Defines a MongoDB product.
    /// </summary>
    public enum Product
    {
        /// <summary>
        /// The MongoDB Community Server edition, see https://www.mongodb.com/try/download/community
        /// <para/>
        /// This is the product containing the <c>mongod</c> binaries.
        /// </summary>
        CommunityServer,

        /// <summary>
        /// The MongoDB Database Tools, see https://www.mongodb.com/try/download/database-tools
        /// <para/>
        /// This is the product containing the <c>mongoimport</c> and <c>mongoexport</c> binaries.
        /// </summary>
        DatabaseTools,
    }
}
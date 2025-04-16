using System;

namespace MongoDownloader;

/// <summary>
/// Defines a MongoDB .zip or .tgz archive.
/// </summary>
public interface IArchive
{
    /// <summary>
    /// The MongoDB product, either <see cref="MongoDownloader.Product.CommunityServer"/> or <see cref="MongoDownloader.Product.DatabaseTools"/>.
    /// </summary>
    Product Product { get; }

    /// <summary>
    /// The <see cref="Target"/> on which the MongoDB product can run.
    /// </summary>
    Target Target { get; }

    /// <summary>
    /// The URL where to download the archive.
    /// </summary>
    /// <example>https://fastdl.mongodb.org/osx/mongodb-macos-x86_64-4.4.10.tgz</example>
    Uri Url { get; }

    /// <summary>
    /// The version of the MongoDB product.
    /// </summary>
    /// <example>5.0.3</example>
    string Version { get; }
}
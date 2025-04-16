using System;

namespace MongoDownloader;

/// <summary>
/// Defines a MongoDB .zip or .tgz archive.
/// </summary>
public class Archive
{
    /// <summary>
    /// The MongoDB product, either <see cref="MongoDownloader.Product.CommunityServer"/> or <see cref="MongoDownloader.Product.DatabaseTools"/>.
    /// </summary>
    public required Product Product { get; init; }

    /// <summary>
    /// The <see cref="Target"/> on which the MongoDB product can run.
    /// </summary>
    public required Target Target { get; init; }

    /// <summary>
    /// The URL where to download the archive.
    /// </summary>
    /// <example>https://fastdl.mongodb.org/osx/mongodb-macos-x86_64-4.4.10.tgz</example>
    public required Uri Url { get; init; }

    /// <summary>
    /// The version of the MongoDB product.
    /// </summary>
    /// <example>5.0.3</example>
    public required string Version { get; init; }

    /// <summary>
    /// A string representation of the archive.
    /// </summary>
    public override string ToString() => $"{Product} {Version} for {Target}";
}
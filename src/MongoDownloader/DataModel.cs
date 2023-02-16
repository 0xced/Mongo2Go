using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CollectionNeverUpdated.Global

namespace MongoDownloader
{
    /// <summary>
    /// The root object of the JSON describing the available releases.
    /// </summary>
    internal class Release
    {
        [JsonPropertyName("versions")]
        public IReadOnlyCollection<Version> Versions { get; init; } = new List<Version>();
    }

    internal class Version
    {
        /// <summary>
        /// Whether the version is considered a production release.
        /// Only available for the Community Server archives.
        /// </summary>
        [JsonPropertyName("production_release")]
        public bool IsProductionRelease { get; init; } = true;

        [JsonPropertyName("version")]
        public string Number { get; init; } = "";

        [JsonPropertyName("downloads")]
        public IReadOnlyCollection<Download> Downloads { get; init; } = new List<Download>();
    }

    internal class Download
    {
        /// <summary>
        /// Used to identify the platform for the Community Server archives.
        /// </summary>
        [JsonPropertyName("target")]
        public string Target { get; init; } = "";

        /// <summary>
        /// Used to identify the platform for the Database Tools archives.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; init; } = "";

        [JsonPropertyName("arch")]
        public string Arch { get; init; } = "";

        [JsonPropertyName("edition")]
        public string Edition { get; init; } = "";

        [JsonPropertyName("archive")]
        public Archive Archive { get; init; } = new();
    }

    internal class Archive
    {
        [JsonPropertyName("url")]
        public Uri Url { get; init; } = default!;
    }
}
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
        public List<Version> Versions { get; init; } = new();
    }

    internal class Version : IVersion
    {
        [JsonPropertyName("version")]
        public string Number { get; init; } = "";

        [JsonPropertyName("production_release")]
        public bool Production { get; init; } = false;

        [JsonPropertyName("downloads")]
        public List<Download> Downloads { get; init; } = new();
    }

    internal class Download : IArchive
    {
        /// <summary>
        /// Used to identify the platform for the Community Server archives
        /// </summary>
        [JsonPropertyName("target")]
        public string Target { get; init; } = "";

        /// <summary>
        /// Used to identify the platform for the Database Tools archives
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; init; } = "";

        [JsonPropertyName("arch")]
        public string Arch { get; init; } = "";

        [JsonPropertyName("edition")]
        public string Edition { get; init; } = "";

        [JsonPropertyName("archive")]
        public Archive Archive { get; init; } = new();

        public Product Product { get; set; }

        public Platform Platform { get; set; }

        public Architecture Architecture { get; set; }

        public Uri Url => Archive.Url;

        public override string ToString() => $"{Product} for {Platform}/{Architecture.ToString().ToLowerInvariant()}";
    }

    internal class Archive
    {
        [JsonPropertyName("url")]
        public Uri Url { get; init; } = default!;
    }
}
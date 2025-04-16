using System.Runtime.InteropServices;

namespace MongoDownloader;

/// <summary>
/// Represents the combination of an <see cref="OSPlatform"/> and
/// an <see cref="Architecture"/> where an executable can run.
/// </summary>
public readonly struct Target(OSPlatform platform, Architecture architecture)
{
    /// <summary>
    /// The operating system platform.
    /// </summary>
    public OSPlatform Platform { get; } = platform;

    /// <summary>
    /// The operating system architecture.
    /// </summary>
    public Architecture Architecture { get; } = architecture;

    /// <summary>
    /// A string representation of the target.
    /// </summary>
    public override string ToString() => $"{Platform.Pretty()}/{Architecture.ToString().ToLowerInvariant()}";
}
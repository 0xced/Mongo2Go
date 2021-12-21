using System.Runtime.InteropServices;

namespace MongoDownloader
{
    /// <summary>
    /// Represents the combination of an <see cref="OSPlatform"/> and
    /// an <see cref="Architecture"/> where an executable can run.
    /// </summary>
    public struct Target
    {
        public Target(OSPlatform platform, Architecture architecture)
        {
            Platform = platform;
            Architecture = architecture;
        }

        /// <summary>
        /// The operating system platform.
        /// </summary>
        public OSPlatform Platform { get; }

        /// <summary>
        /// The operating system architecture.
        /// </summary>
        public Architecture Architecture { get; }
    }
}
using System.Runtime.InteropServices;

namespace MongoDownloader;

internal static class PrettyPlatform
{
    public static string Pretty(this OSPlatform platform)
    {
        if (platform == OSPlatform.Linux)
        {
            return "Linux";
        }

        if (platform == OSPlatform.OSX)
        {
            return "macOS";
        }

        if (platform == OSPlatform.Windows)
        {
            return "Windows";
        }

        return platform.ToString();
    }
}
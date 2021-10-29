using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ByteSizeLib;
using Spectre.Console;

namespace MongoDownloader.CLI
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            try
            {
                var toolsDirectory = GetToolsDirectory();

                foreach (DirectoryInfo dir in toolsDirectory.EnumerateDirectories())
                {
                    dir.Delete(true); 
                }

                var cancellationTokenSource = new CancellationTokenSource();
                Console.CancelKeyPress += (_, eventArgs) =>
                {
                    // Try to cancel gracefully the first time, then abort the process the second time Ctrl+C is pressed
                    eventArgs.Cancel = !cancellationTokenSource.IsCancellationRequested;
                    cancellationTokenSource.Cancel();
                };
                var options = new Options();
                var performStrip = args.All(e => e != "--no-strip");
                var binaryStripper = performStrip ? await GetBinaryStripperAsync(cancellationTokenSource.Token) : null;
                var archiveExtractor = new ArchiveExtractor(options, binaryStripper);
                var downloader = new MongoDbDownloader(archiveExtractor, options);
                var strippedSize = await AnsiConsole
                    .Progress()
                    .Columns(
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new RemainingTimeColumn(),
                        new DownloadedColumn(),
                        new TaskDescriptionColumn { Alignment = Justify.Left }
                    )
                    .StartAsync(async context => await RunAsync(context, downloader, toolsDirectory, cancellationTokenSource.Token));

                if (performStrip)
                {
                    AnsiConsole.WriteLine($"Saved {strippedSize:#.#} by stripping executables");
                }
                return 0;
            }
            catch (Exception exception)
            {
                if (exception is not OperationCanceledException)
                {
                    AnsiConsole.WriteException(exception, ExceptionFormats.ShortenPaths);
                }
                return 1;
            }
        }

        private static async Task<ByteSize> RunAsync(ProgressContext context, MongoDbDownloader downloader, DirectoryInfo toolsDirectory, CancellationToken cancellationToken)
        {
            const double initialMaxValue = double.Epsilon;
            var globalProgress = context.AddTask("Downloading MongoDB", maxValue: initialMaxValue);

            var (communityServerVersion, communityServerArchives) = await downloader.GetArchivesAsync(Product.CommunityServer, cancellationToken);
            globalProgress.Description = $"Downloading MongoDB Community Server {communityServerVersion.Number}";

            var (databaseToolsVersion, databaseToolsArchives) = await downloader.GetArchivesAsync(Product.DatabaseTools, cancellationToken);
            globalProgress.Description = $"Downloading MongoDB Community Server {communityServerVersion.Number} and Database Tools {databaseToolsVersion.Number}";

            var tasks = new List<Task<ByteSize>>();
            var allArchiveProgresses = new List<ProgressTask>();
            foreach (var archive in communityServerArchives.Concat(databaseToolsArchives))
            {
                var archiveProgress = context.AddTask($"Downloading {archive} from {archive.Url}", maxValue: initialMaxValue);
                var directoryName = $"mongodb-{archive.Platform.ToString().ToLowerInvariant()}-{archive.Architecture.ToString().ToLowerInvariant()}-{communityServerVersion.Number}-database-tools-{databaseToolsVersion.Number}";
                var extractDirectory = new DirectoryInfo(Path.Combine(toolsDirectory.FullName, directoryName));
                allArchiveProgresses.Add(archiveProgress);
                var progress = new ArchiveProgress(archiveProgress, globalProgress, allArchiveProgresses, archive, $"✅ Downloaded and extracted MongoDB Community Server {communityServerVersion.Number} and Database Tools {databaseToolsVersion.Number} into {new Uri(toolsDirectory.FullName).AbsoluteUri}");
                tasks.Add(downloader.ProcessArchiveAsync(archive, extractDirectory, progress, cancellationToken));
            }
            var strippedSizes = await Task.WhenAll(tasks);
            return strippedSizes.Aggregate(new ByteSize(0), (current, strippedSize) => current + strippedSize);
        }

        private static DirectoryInfo GetToolsDirectory()
        {
            for (var directory = new DirectoryInfo("."); directory != null; directory = directory.Parent)
            {
                var toolsDirectory = directory.GetDirectories("tools", SearchOption.TopDirectoryOnly).SingleOrDefault();
                if (toolsDirectory?.Exists ?? false)
                {
                    return toolsDirectory;
                }
            }
            throw new InvalidOperationException("The tools directory was not found");
        }

        private static async Task<BinaryStripper?> GetBinaryStripperAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await BinaryStripper.CreateAsync(cancellationToken);
            }
            catch (FileNotFoundException exception)
            {
                string installCommand;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    installCommand = "brew install llvm";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    installCommand = "scoop install llvm";
                else
                    installCommand = "apt-get install llvm";

                throw new Exception($"{exception.Message} Either install llvm with `{installCommand}` or run MongoDownloader with the --no-strip option to skip binary stripping.", exception);
            }
        }
    }
}

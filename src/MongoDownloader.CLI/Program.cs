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
                var downloader = MongoDbDownloaderFactory.Create(options);
                var strippedSize = await AnsiConsole
                    .Progress()
                    .Columns(
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new RemainingTimeColumn(),
                        new DownloadedColumn(),
                        new TaskDescriptionColumn { Alignment = Justify.Left }
                    )
                    .StartAsync(async context => await RunAsync(context, downloader, binaryStripper, toolsDirectory, cancellationTokenSource.Token));

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

        private static async Task<ByteSize> RunAsync(ProgressContext context, IMongoDbDownloader downloader, BinaryStripper? binaryStripper, DirectoryInfo toolsDirectory, CancellationToken cancellationToken)
        {
            const double initialMaxValue = double.Epsilon;
            var globalProgress = context.AddTask("Downloading MongoDB", maxValue: initialMaxValue);

            var communityServerArchives = await downloader.GetArchivesAsync(Product.CommunityServer, cancellationToken);
            var communityServerVersion = communityServerArchives.FirstOrDefault()?.Version;
            globalProgress.Description = $"Downloading MongoDB Community Server {communityServerVersion}";

            var databaseToolsArchives = await downloader.GetArchivesAsync(Product.DatabaseTools, cancellationToken);
            var databaseToolsVersion = databaseToolsArchives.FirstOrDefault()?.Version;
            globalProgress.Description = $"Downloading MongoDB Community Server {communityServerVersion} and Database Tools {databaseToolsVersion}";

            var tasks = new List<Task<ByteSize>>();
            var allArchiveProgresses = new List<ProgressTask>();
            foreach (var archive in communityServerArchives.Concat(databaseToolsArchives))
            {
                var archiveProgress = context.AddTask($"Downloading {archive} from {archive.Url}", maxValue: initialMaxValue);
                var directoryName = $"mongodb-{archive.Platform.ToString().ToLowerInvariant()}-{archive.Architecture.ToString().ToLowerInvariant()}-{communityServerVersion}-database-tools-{databaseToolsVersion}";
                var extractDirectory = new DirectoryInfo(Path.Combine(toolsDirectory.FullName, directoryName));
                allArchiveProgresses.Add(archiveProgress);
                var progress = new ArchiveProgress(archiveProgress, globalProgress, allArchiveProgresses, archive, $"✅ Downloaded and extracted MongoDB Community Server {communityServerVersion} and Database Tools {databaseToolsVersion} into {new Uri(toolsDirectory.FullName).AbsoluteUri}");
                var processArchiveTask = downloader.ProcessArchiveAsync(archive, extractDirectory, progress, cancellationToken);
                tasks.Add(processArchiveTask.Combine(StripAsync, binaryStripper, progress, cancellationToken));
            }
            var strippedSizes = await Task.WhenAll(tasks);
            return strippedSizes.Sum();
        }

        private static async Task<ByteSize> StripAsync(IReadOnlyCollection<FileInfo> binaryFiles, BinaryStripper? binaryStripper, ArchiveProgress progress, CancellationToken cancellationToken)
        {
            ByteSize strippedSize;
            if (binaryStripper is not null && binaryFiles.Count > 0)
            {
                progress.Report("Stripping binaries");
                var stripTasks = binaryFiles.Select(binaryFile => binaryStripper.StripAsync(binaryFile, cancellationToken));
                var strippedSizes = await Task.WhenAll(stripTasks);
                strippedSize = strippedSizes.Sum();
            }
            else
            {
                strippedSize = new ByteSize(0);
            }
            progress.ReportCompleted(strippedSize);
            return strippedSize;
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

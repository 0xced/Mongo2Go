using System;
using System.Collections.Generic;
using System.Linq;
using ByteSizeLib;
using Spectre.Console;

namespace MongoDownloader.CLI;

public class ArchiveProgress : IProgress<TransferProgress>
{
    private readonly ProgressTask _archiveProgress;
    private readonly ProgressTask _globalProgress;
    private readonly IEnumerable<ProgressTask> _allArchiveProgresses;
    private readonly Archive _archive;
    private readonly string _completedDescription;

    public ArchiveProgress(ProgressTask archiveProgress, ProgressTask globalProgress, IEnumerable<ProgressTask> allArchiveProgresses, Archive archive, string completedDescription)
    {
        _archiveProgress = archiveProgress ?? throw new ArgumentNullException(nameof(archiveProgress));
        _globalProgress = globalProgress ?? throw new ArgumentNullException(nameof(globalProgress));
        _allArchiveProgresses = allArchiveProgresses ?? throw new ArgumentNullException(nameof(allArchiveProgresses));
        _archive = archive ?? throw new ArgumentNullException(nameof(archive));
        _completedDescription = completedDescription ?? throw new ArgumentNullException(nameof(completedDescription));
    }

    public void Report(TransferProgress progress)
    {
        _archiveProgress.Value = progress.TransferredBytes;
        _archiveProgress.MaxValue = progress.TotalBytes;

        string text;
        bool isIndeterminate;
        if (progress.TransferredBytes < progress.TotalBytes)
        {
            var speed = ByteSize.FromBytes(progress.TransferredBytes / progress.ElapsedTime.TotalSeconds);
            text = $"Downloading {_archive} from {_archive.Url} at {speed:0.0}/s";
            isIndeterminate = false;
        }
        else
        {
            text = $"Downloaded {_archive}";
            isIndeterminate = true;
            // Cheat by subtracting 1 so that the progress stays at 99% in indeterminate mode for
            // remaining tasks (stripping) to complete with an indeterminate progress bar
            _archiveProgress.Value = progress.TransferredBytes - 1;
        }
        Report(text, isIndeterminate);

        lock (_globalProgress)
        {
            _globalProgress.Value = _allArchiveProgresses.Sum(e => e.Value);
            _globalProgress.MaxValue = _allArchiveProgresses.Sum(e => e.MaxValue);
        }
    }

    public void Report(string description)
    {
        Report(description, isIndeterminate: true);
    }

    public void ReportCompleted(ByteSize strippedSize)
    {
        _archiveProgress.Value = _archiveProgress.MaxValue;

        lock (_globalProgress)
        {
            if (_allArchiveProgresses.All(e => e.IsFinished))
            {
                _globalProgress.Description = _completedDescription;
                _globalProgress.Value = _globalProgress.MaxValue;
            }
        }

        var saved = strippedSize.Bytes > 0 ? $" (saved {strippedSize:#.#} by stripping)" : "";
        Report($"Extracted {_archive}{saved}", isIndeterminate: false);
    }

    private void Report(string description, bool isIndeterminate)
    {
        _archiveProgress.Description = description;
        _archiveProgress.IsIndeterminate = isIndeterminate;
        lock (_globalProgress)
        {
            _globalProgress.IsIndeterminate = _allArchiveProgresses.All(e => e.IsFinished || e.IsIndeterminate);
        }
    }
}
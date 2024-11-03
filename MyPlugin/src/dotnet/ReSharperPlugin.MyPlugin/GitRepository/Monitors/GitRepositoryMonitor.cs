using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using ReSharperPlugin.MyPlugin.Helpers;

namespace ReSharperPlugin.MyPlugin.GitRepository.Monitors;

[SolutionComponent]
public class GitRepositoryMonitor : IDisposable
{
    private const string GitDirectoryName = ".git";
    private const int DebounceDelayMilliseconds = 500;
    private const string RepositoryChangedSignalName = "GitRepositoryMonitor.RepositoryChanged";

    private readonly FileSystemWatcher _gitWatcher;
    private CancellationTokenSource _debounceCts;

    // Signal to notify about repository changes
    public ISimpleSignal RepositoryChangedSignal { get; }

    [Obsolete("Obsolete")]
    public GitRepositoryMonitor(Lifetime lifetime, ISolution solution)
    {
        RepositoryChangedSignal = new SimpleSignal(lifetime, RepositoryChangedSignalName);

        var repositoryPath = FileOperationsHelper.GetRepositoryRoot(solution.SolutionDirectory.FullPath);
        if (string.IsNullOrEmpty(repositoryPath))
        {
            return;
        }

        _debounceCts = new CancellationTokenSource();

        // Initialize FileSystemWatcher to monitor the .git directory
        _gitWatcher = new FileSystemWatcher(Path.Combine(repositoryPath, GitDirectoryName))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        // Subscribe to FileSystemWatcher events
        _gitWatcher.Changed += OnGitDirectoryChanged;
        _gitWatcher.Created += OnGitDirectoryChanged;
        _gitWatcher.Deleted += OnGitDirectoryChanged;
        _gitWatcher.Renamed += OnGitDirectoryChanged;
    }

    private void OnGitDirectoryChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce multiple events
        _debounceCts.Cancel();
        _debounceCts.Dispose();
        _debounceCts = new CancellationTokenSource();
        var debounceToken = _debounceCts.Token;

        Task.Delay(DebounceDelayMilliseconds, debounceToken).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                RepositoryChangedSignal.Fire(); // Fire the signal to notify listeners
            }
        }, debounceToken);
    }

    public void Dispose()
    {
        _gitWatcher.Dispose();
        _debounceCts?.Dispose();
    }
}
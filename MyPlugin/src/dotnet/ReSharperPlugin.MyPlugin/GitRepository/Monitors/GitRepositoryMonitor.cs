using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;

namespace ReSharperPlugin.MyPlugin.GitRepository.Monitors;

[SolutionComponent]
public class GitRepositoryMonitor : IDisposable
{
    private readonly FileSystemWatcher _gitWatcher;
    private CancellationTokenSource _debounceCts;

    // Signal to notify about repository changes
    public ISimpleSignal RepositoryChangedSignal { get; }

    public GitRepositoryMonitor(Lifetime lifetime, ISolution solution)
    {
        RepositoryChangedSignal = new SimpleSignal(lifetime, "GitRepositoryMonitor.RepositoryChanged");

        var repositoryPath = GetRepositoryRoot(solution.SolutionDirectory.FullPath);
        if (string.IsNullOrEmpty(repositoryPath))
        {
            Console.WriteLine("Solution is not located in a Git repository, monitoring not started.");
            return;
        }

        _debounceCts = new CancellationTokenSource();

        // Initialize FileSystemWatcher to monitor the .git directory
        _gitWatcher = new FileSystemWatcher(Path.Combine(repositoryPath, ".git"))
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

        Console.WriteLine("Started monitoring Git repository for changes...");
    }

    private void OnGitDirectoryChanged(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine($"Detected Git repository change: {e.ChangeType} in {e.FullPath}");

        // Debounce multiple events
        _debounceCts.Cancel();
        _debounceCts.Dispose();
        _debounceCts = new CancellationTokenSource();
        var debounceToken = _debounceCts.Token;

        Task.Delay(500, debounceToken).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                RepositoryChangedSignal.Fire(); // Fire the signal to notify listeners
                Console.WriteLine("Repository change detected, signal fired.");
            }
        }, debounceToken);
    }

    public void Dispose()
    {
        _gitWatcher.Dispose();
        _debounceCts?.Dispose();
    }

    private string GetRepositoryRoot(string solutionPath)
    {
        // Logic to find the repository root if needed
        // Return the root path of the Git repository
        return solutionPath; // Placeholder, implement repository root finding logic
    }
}
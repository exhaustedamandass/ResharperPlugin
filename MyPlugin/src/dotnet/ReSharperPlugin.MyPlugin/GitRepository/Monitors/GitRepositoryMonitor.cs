using System;
using System.IO;
using System.Threading.Tasks;

namespace ReSharperPlugin.MyPlugin.GitRepository.Monitors;

public class GitRepositoryMonitor
{
    private readonly FileSystemWatcher _gitWatcher;
    private readonly Action _onRepositoryChanged;

    public GitRepositoryMonitor(string repositoryPath, Action onRepositoryChanged)
    {
        _onRepositoryChanged = onRepositoryChanged;

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
        // Debounce multiple events triggered by a single Git operation
        Task.Delay(500).ContinueWith(_ =>
        {
            Console.WriteLine($"Detected Git repository change: {e.ChangeType} in {e.FullPath}");
            _onRepositoryChanged?.Invoke(); // Notify that the repository has changed
        });
    }

    public void StopMonitoring()
    {
        _gitWatcher.EnableRaisingEvents = false;
        _gitWatcher.Dispose();
        Console.WriteLine("Stopped monitoring Git repository.");
    }
}
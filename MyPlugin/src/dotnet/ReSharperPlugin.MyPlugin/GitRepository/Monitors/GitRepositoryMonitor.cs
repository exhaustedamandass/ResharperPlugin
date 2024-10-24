using System;
using System.IO;
using System.Threading.Tasks;

namespace ReSharperPlugin.MyPlugin.GitRepository.Monitors;

public class GitRepositoryMonitor
{
    private FileSystemWatcher _gitWatcher;
    private readonly string _repositoryPath;
    private readonly Action _onRepositoryChanged;

    public GitRepositoryMonitor(string repositoryPath, Action onRepositoryChanged)
    {
        _repositoryPath = repositoryPath;
        _onRepositoryChanged = onRepositoryChanged;
        
        InitializeGitWatcher();
    }
    
    private void InitializeGitWatcher()
    {
        _gitWatcher = new FileSystemWatcher();
        _gitWatcher.Path = Path.Combine(_repositoryPath, ".git");
        _gitWatcher.IncludeSubdirectories = true;

        // Monitor for changes that could indicate new commits, branch switches, etc.
        _gitWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;

        _gitWatcher.Changed += OnGitRepositoryChanged;
        _gitWatcher.Created += OnGitRepositoryChanged;
        _gitWatcher.Deleted += OnGitRepositoryChanged;
        _gitWatcher.Renamed += OnGitRepositoryChanged;

        _gitWatcher.EnableRaisingEvents = true;

        Console.WriteLine("Started monitoring the Git repository for changes...");
    }
    
    private void OnGitRepositoryChanged(object sender, FileSystemEventArgs e)
    {
        // Use debounce to avoid multiple triggers in quick succession
        Task.Delay(500).ContinueWith(t =>
        {
            Console.WriteLine($"Detected changes in Git repository: {e.ChangeType} {e.Name}");
            _onRepositoryChanged?.Invoke();
        });
    }
    
    public void StopMonitoring()
    {
        _gitWatcher.EnableRaisingEvents = false;
        _gitWatcher.Dispose();
        Console.WriteLine("Stopped monitoring the Git repository.");
    }
}
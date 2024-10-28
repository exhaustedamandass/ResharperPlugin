using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.ProjectModel;
using LibGit2Sharp;
using ReSharperPlugin.MyPlugin.GitRepository.Monitors;

namespace ReSharperPlugin.MyPlugin.GitRepository.Handlers;

[SolutionComponent]
public class GitRepositoryHandler
{
    private GitRepositoryMonitor _gitMonitor;
    private readonly string _repositoryPath;
    private Dictionary<string, string> _fileCommitMessages;
    
    public bool IsTrackingEnabled { get; private set; }

    public GitRepositoryHandler(ISolution solution)
    {
        _fileCommitMessages = new Dictionary<string, string>();
        
        var solutionPath = solution.SolutionDirectory.FullPath;

        if (string.IsNullOrEmpty(solutionPath))
        {
            Console.WriteLine("Solution path is null or empty, cannot initialize GitRepositoryHandler.");
            IsTrackingEnabled = false;
            return;
        }

        _repositoryPath = GetRepositoryRoot(solutionPath);
        IsTrackingEnabled = !string.IsNullOrEmpty(_repositoryPath);

        if (IsTrackingEnabled)
        {
            Console.WriteLine("Solution is located within a Git repository.");
            StartMonitoring();
        }
        else
        {
            Console.WriteLine("Solution is not located in a Git repository.");
        }
    }

    private void StartMonitoring()
    {
        if (!IsTrackingEnabled) return;
        
        _gitMonitor = new GitRepositoryMonitor(_repositoryPath, OnRepositoryChanged);
    }
    
    private void OnRepositoryChanged()
    {
        // Refresh modified files and commit messages when repository changes
        UpdateModifiedFilesAndMessages();
    }

    private void UpdateModifiedFilesAndMessages()
    {
        _fileCommitMessages.Clear();

        using (var repo = new Repository(_repositoryPath))
        {
            var recentCommits = repo.Commits.Take(10);
            foreach (var commit in recentCommits)
            {
                foreach (var parent in commit.Parents)
                {
                    var changes = repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree);
                    foreach (var change in changes)
                    {
                        if (change.Status == ChangeKind.Modified && !_fileCommitMessages.ContainsKey(change.Path))
                        {
                            _fileCommitMessages[change.Path] = commit.MessageShort;
                        }
                    }
                }
            }
        }
    }

    public bool IsFileModified(string filePath) => _fileCommitMessages.ContainsKey(filePath);

    public string GetCommitMessageForLine(string filePath, int lineNumber)
    {
        return _fileCommitMessages.TryGetValue(filePath, out var commitMessage) ? commitMessage : string.Empty;
    }

    public void StopMonitoring() => _gitMonitor.StopMonitoring();
    
    private string GetRepositoryRoot(string solutionPath)
    {
        var directoryInfo = new DirectoryInfo(solutionPath);
        while (directoryInfo != null && !Directory.Exists(Path.Combine(directoryInfo.FullName, ".git")))
        {
            directoryInfo = directoryInfo.Parent;
        }

        return directoryInfo?.FullName;
    }
}

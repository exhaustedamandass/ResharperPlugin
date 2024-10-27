using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Application.DataContext;
using JetBrains.Application.Settings;
using LibGit2Sharp;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
using ReSharperPlugin.MyPlugin.ElementProblemAnalyzers;
using ReSharperPlugin.MyPlugin.GitRepository.Helpers;
using ReSharperPlugin.MyPlugin.GitRepository.Monitors;
using ReSharperPlugin.MyPlugin.Options;

namespace ReSharperPlugin.MyPlugin.GitRepository.Handlers;

public class GitRepositoryHandler
{
    private string _repositoryPath;
    private GitRepositoryMonitor _gitMonitor;
    private CommitModificationAnalyzer _analyzer;
    private int _nCommits;

    public GitRepositoryHandler(ISettingsStore settingsStore, string solutionPath, IDataContext dataContext,
        CommitModificationAnalyzer analyzer)
    {
        _analyzer = analyzer;
        _repositoryPath = GetRepositoryRoot(solutionPath);

        if (_repositoryPath == null)
        {
            Console.WriteLine("The solution is not located in a Git repository.");
            return;
        }
        
        _gitMonitor = new GitRepositoryMonitor(_repositoryPath, OnGitRepositoryChanged);
        
        var gitPluginSettings = settingsStore.BindToContextTransient(ContextRange.Smart((lt, _) => dataContext));
        var settings = gitPluginSettings.GetKey<MySettingsKey>(SettingsOptimization.DoMeSlowly);
        _nCommits = settings.NCommits; 
    }
    
    private void OnGitRepositoryChanged()
    {
        InvalidateDaemon();
        ProcessRecentCommits();
    }

    // Invalidate daemon or cache
    //TODO: implement
    private void InvalidateDaemon()
    {
        
        Console.WriteLine("Invalidating the daemon...");
        // Logic to invalidate the daemon or cache goes here
    }

    // Re-read recent commits using LibGit2Sharp
    private void ProcessRecentCommits()
    {
        using (var repo = new Repository(_repositoryPath))
        {
            var currentBranch = repo.Head;
            Console.WriteLine($"Current branch: {currentBranch.FriendlyName}");

            var commits = GetRecentCommits(repo);
            foreach (var commit in commits)
            {
                ProcessCommitChanges(commit, repo);
            }
        }
    }

    // Get the recent commits from the repository
    private List<Commit> GetRecentCommits(Repository repo)
    {
        return repo.Commits.Take(_nCommits).ToList();
    }

    // Process the changes of a single commit
    private void ProcessCommitChanges(Commit commit, Repository repo)
    {
        Console.WriteLine($"Processing commit: {commit.Sha}");
        Console.WriteLine($"Message: {commit.MessageShort}");

        foreach (var parent in commit.Parents)
        {
            var changes = repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree);
            ProcessFileChanges(changes, commit.MessageShort);
        }
    }

    // Process the modified files in a commit
    private void ProcessFileChanges(TreeChanges changes, string commitMessage)
    {
        foreach (var change in changes)
        {
            if (change.Status == ChangeKind.Modified)
            {
                Console.WriteLine($"Modified file: {change.Path}");
                HighlightFirstNonWhitespaceCharacters(change.Path, commitMessage);
            }
        }
    }

    // Highlight the first 5 non-whitespace characters (functionality not implemented)
    private void HighlightFirstNonWhitespaceCharacters(string filePath, string commitMessage)
    {
        string fileContent = FileOperationsHelper.ReadFileContents(filePath);
        if (fileContent == null)
        {
            return;
        }

        List<int> positions = FileOperationsHelper.FindFirstNonWhitespacePositions(fileContent, 5);

        foreach (var position in positions)
        {
            // Placeholder for highlighting functionality
            Console.WriteLine($"Highlighting character: {fileContent[position]} at position {position} for commit: {commitMessage}");
        }

        // Placeholder for actual highlighting logic
    }

    // Stop monitoring the repository
    private string GetRepositoryRoot(string solutionPath)
    {
        var directoryInfo = new DirectoryInfo(solutionPath);
        while (directoryInfo != null && !Directory.Exists(Path.Combine(directoryInfo.FullName, ".git")))
        {
            directoryInfo = directoryInfo.Parent;
        }

        return directoryInfo?.FullName;
    }
    
    // Stop monitoring the repository
    public void StopMonitoring()
    {
        _gitMonitor?.StopMonitoring();
    }
}
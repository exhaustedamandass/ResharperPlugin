using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using JetBrains.ProjectModel;
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
            UpdateModifiedFilesAndMessages();
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

        // Get recent commits with file modifications
        var recentCommits = ExecuteGitCommand("log -n 10 --pretty=format:%h --name-only");

        if (!string.IsNullOrEmpty(recentCommits))
        {
            var lines = recentCommits.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            string currentCommitHash = string.Empty;

            foreach (var line in lines)
            {
                if (!line.Contains("/")) // assuming it's a commit hash line
                {
                    currentCommitHash = line;
                }
                else // it's a file path
                {
                    if (!_fileCommitMessages.ContainsKey(line))
                    {
                        var commitMessage = ExecuteGitCommand($"log -1 --pretty=format:%s {currentCommitHash}");
                        _fileCommitMessages[line] = commitMessage;
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

    private string ExecuteGitCommand(string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = _repositoryPath,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return output;
    }
}

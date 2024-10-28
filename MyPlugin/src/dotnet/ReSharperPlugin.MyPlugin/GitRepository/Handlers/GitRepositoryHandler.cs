using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.ProjectModel;
using ReSharperPlugin.MyPlugin.GitRepository.Monitors;

namespace ReSharperPlugin.MyPlugin.GitRepository.Handlers;

public record ModificationRange(int StartLine, int StartChar, int Length, string CommitMessage);

[SolutionComponent]
public class GitRepositoryHandler
{
    private GitRepositoryMonitor _gitMonitor;
    private readonly string _repositoryPath;
    private Dictionary<string, List<ModificationRange>> _fileModificationRanges;

    public bool IsTrackingEnabled { get; private set; }

    public GitRepositoryHandler(ISolution solution)
    {
        _fileModificationRanges = new Dictionary<string, List<ModificationRange>>();

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
    
    public List<ModificationRange> GetModificationRanges(string filePath)
    {
        return _fileModificationRanges.TryGetValue(filePath, out var ranges) ? ranges : new List<ModificationRange>();
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
        _fileModificationRanges.Clear();

        // Retrieve modified files and their details
        var diffOutput = ExecuteGitCommand("diff HEAD~1 --unified=0"); // Shows 1 commits worth of differences

        if (!string.IsNullOrEmpty(diffOutput))
        {
            ParseGitDiffOutput(diffOutput);
        }
    }
    
    private void ParseGitDiffOutput(string diffOutput)
    {
        string currentFile = string.Empty;
        string currentCommitMessage = string.Empty;

        foreach (var line in diffOutput.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith("diff --git"))
            {
                currentFile = ParseFilePath(line);
            }
            else if (line.StartsWith("commit"))
            {
                currentCommitMessage = ParseCommitMessage(line);
            }
            else if (line.StartsWith("@@") && !string.IsNullOrEmpty(currentFile))
            {
                var modificationRange = ParseDiffLineRange(line);
                modificationRange = modificationRange with { CommitMessage = currentCommitMessage };
            
                if (!_fileModificationRanges.ContainsKey(currentFile))
                {
                    _fileModificationRanges[currentFile] = new List<ModificationRange>();
                }
                _fileModificationRanges[currentFile].Add(modificationRange);
            }
        }
    }
    
    private string ParseFilePath(string line)
    {
        // Example line: "diff --git a/path/to/file b/path/to/file"
        var parts = line.Split(' ');
        if (parts.Length >= 3)
        {
            // Assuming the file path is the part after "a/" or "b/"
            var filePath = parts[2].Substring(2); // Remove "b/"
            return filePath;
        }
        return string.Empty;
    }
    
    private string ParseCommitMessage(string line)
    {
        // Example line: "commit abc1234 Some commit message"
        var parts = line.Split(' ');
        if (parts.Length >= 2)
        {
            // Join parts after the commit hash as the commit message
            return string.Join(" ", parts.Skip(2));
        }
        return string.Empty;
    }

    private ModificationRange ParseDiffLineRange(string line)
    {
        // Example line: "@@ -1,5 +1,5 @@"
        var match = Regex.Match(line, @"\+(\d+),(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int startLine) &&
            int.TryParse(match.Groups[2].Value, out int length))
        {
            // Assuming we start at character 0 in the line; adjust as needed based on diff format
            return new ModificationRange(startLine, 0, length, string.Empty);
        }

        return null;
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

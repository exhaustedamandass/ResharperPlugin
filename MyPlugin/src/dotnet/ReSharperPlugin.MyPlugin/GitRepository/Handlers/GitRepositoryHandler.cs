using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using JetBrains.ProjectModel;
using ReSharperPlugin.MyPlugin.GitRepository.Monitors;

namespace ReSharperPlugin.MyPlugin.GitRepository.Handlers;

public record ModificationRange(int StartLine, int StartChar, int Length, string CommitMessage);

[SolutionComponent]
public class GitRepositoryHandler
{
    private GitRepositoryMonitor _gitMonitor;
    private readonly string _repositoryPath;
    private readonly Repository _repository;
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
            _repository = new Repository(_repositoryPath);
            StartMonitoring();
            LoadRecentModifications();
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
        LoadRecentModifications();
    }
    
    private void LoadRecentModifications(int numberOfCommits = 10)
    {
        _fileModificationRanges.Clear();

        // Retrieve the specified number of recent commits in the current branch
        var commits = _repository.Commits.Take(numberOfCommits).ToList();

        foreach (var commit in commits)
        {
            var parent = commit.Parents.FirstOrDefault();
            if (parent != null)
            {
                // Get the diff between this commit and its parent
                var diff = _repository.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree);

                foreach (var change in diff)
                {
                    if (change.Status == ChangeKind.Modified)
                    {
                        var modifiedFilePath = change.Path;
                        var modificationRanges = GetModificationRangesForFile(commit, parent, modifiedFilePath);

                        if (!_fileModificationRanges.ContainsKey(modifiedFilePath))
                        {
                            _fileModificationRanges[modifiedFilePath] = new List<ModificationRange>();
                        }

                        _fileModificationRanges[modifiedFilePath].AddRange(modificationRanges);
                    }
                }
            }
        }
    }

    private List<ModificationRange> GetModificationRangesForFile(Commit commit, Commit parent, string filePath)
    {
        var ranges = new List<ModificationRange>();

        // Get the full patch diff between the commit and its parent
        var patch = _repository.Diff.Compare<Patch>(parent.Tree, commit.Tree);

        // Get the specific file patch for the modified file
        var filePatch = patch[filePath];
        if (filePatch == null) return ranges;

        // Parse the Content property for line-by-line changes (unified diff format)
        var lines = filePatch.Patch.Split('\n');
        int currentNewLineNumber = 0; // Tracks the current line number in the new file

        foreach (var line in lines)
        {
            if (line.StartsWith("@@"))
            {
                // Parse the line numbers from the diff header (e.g., "@@ -oldLineStart,oldLineCount +newLineStart,newLineCount @@")
                var match = System.Text.RegularExpressions.Regex.Match(line, @"\+(\d+)");
                if (match.Success)
                {
                    currentNewLineNumber = int.Parse(match.Groups[1].Value);
                }
            }
            else if (line.StartsWith("+") && !line.StartsWith("+++"))
            {
                // This is an added line; add it to the ranges
                ranges.Add(new ModificationRange(currentNewLineNumber, 0, line.Length - 1, commit.MessageShort)); // Exclude '+' sign
                currentNewLineNumber++;
            }
            else if (line.StartsWith("-") || line.StartsWith(" "))
            {
                // Skip removed or unchanged lines, but adjust line numbers
                if (!line.StartsWith("-"))
                {
                    currentNewLineNumber++;
                }
            }
        }

        return ranges;
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

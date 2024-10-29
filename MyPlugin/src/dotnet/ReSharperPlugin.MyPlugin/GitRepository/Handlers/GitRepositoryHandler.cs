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
            LoadRecentModifications();
        }
        else
        {
            Console.WriteLine("Solution is not located in a Git repository.");
        }
    }
    
    public string GetRelativePath(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath) || string.IsNullOrEmpty(_repositoryPath))
        {
            return fullPath;
        }

        var repoUri = new Uri(_repositoryPath + Path.DirectorySeparatorChar);
        var fileUri = new Uri(fullPath);

        return Uri.UnescapeDataString(repoUri.MakeRelativeUri(fileUri)
            .ToString()
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
    }

    private string NormalizePath(string path)
    {
        return path?.Replace('\\', '/');
    }

    public List<ModificationRange> GetModificationRanges(string filePath)
    {
        var normalizedPath = NormalizePath(filePath);
        return _fileModificationRanges.TryGetValue(normalizedPath, out var ranges) ? ranges : new List<ModificationRange>();
    }

    private void StartMonitoring()
    {
        if (!IsTrackingEnabled) return;

        _gitMonitor = new GitRepositoryMonitor(_repositoryPath, OnRepositoryChanged);
    }

    private void OnRepositoryChanged()
    {
        LoadRecentModifications();
    }

    private void LoadRecentModifications(int numberOfCommits = 1)
    {
        _fileModificationRanges.Clear();

        // Retrieve the specified number of recent commits
        var commitHashes = ExecuteGitCommand($"log -n {numberOfCommits + 1} --pretty=format:%H").Split('\n');

        for (int i = 0; i < commitHashes.Length - 1; i++)
        {
            var currentCommit = commitHashes[i];
            var parentCommit = commitHashes[i + 1];

            var diffOutput = ExecuteGitCommand($"diff {parentCommit} {currentCommit}");
            var commitMessage = ExecuteGitCommand($"show -s --format=%B {currentCommit}");

            ParseDiffOutput(diffOutput, commitMessage);
        }
    }

    private void ParseDiffOutput(string diffOutput, string commitMessage)
    {
        var lines = diffOutput.Split('\n');
        string currentFile = null;
        int currentNewLineNumber = 0;

        foreach (var line in lines)
        {
            if (line.StartsWith("diff --git"))
            {
                var match = Regex.Match(line, @"diff --git a\/(.+?) b\/(.+)");
                if (match.Success)
                {
                    currentFile = match.Groups[2].Value;
                    if (!_fileModificationRanges.ContainsKey(currentFile))
                    {
                        _fileModificationRanges[currentFile] = new List<ModificationRange>();
                    }
                }
            }
            else if (line.StartsWith("@@"))
            {
                var match = Regex.Match(line, @"\+(\d+)");
                if (match.Success)
                {
                    currentNewLineNumber = int.Parse(match.Groups[1].Value);
                }
            }
            else if (line.StartsWith("+") && !line.StartsWith("+++"))
            {
                // Check if the line is empty (ignoring the "+")
                var lineContent = line.Substring(1);
                if (string.IsNullOrWhiteSpace(lineContent))
                {
                    currentNewLineNumber++;
                    continue;
                }

                // Find the index of the first non-whitespace character
                int startingCharacterIndex = lineContent.TakeWhile(char.IsWhiteSpace).Count();

                // Add the new modification range
                _fileModificationRanges[currentFile ?? throw new ArgumentNullException(nameof(currentFile))]
                    .Add(new ModificationRange(currentNewLineNumber, startingCharacterIndex, lineContent.Length - startingCharacterIndex, commitMessage));
                currentNewLineNumber++;
            }
            else if (line.StartsWith(" ") || line.StartsWith("-"))
            {
                if (!line.StartsWith("-"))
                {
                    currentNewLineNumber++;
                }
            }
        }
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using ReSharperPlugin.MyPlugin.GitRepository.Monitors;
using ReSharperPlugin.MyPlugin.Options;

namespace ReSharperPlugin.MyPlugin.GitRepository.Handlers;

public record ModificationRange(int StartLine, int StartChar, int Length, string CommitMessage);

[SolutionComponent]
public class GitRepositoryHandler
{
    private GitRepositoryMonitor _gitMonitor;
    private readonly string _repositoryPath;
    private Dictionary<string, List<ModificationRange>> _fileModificationRanges;
    private IProperty<int> NCommitsProperty { get; set; }

    private bool IsTrackingEnabled { get; set; }

    public GitRepositoryHandler(ISolution solution, Lifetime lifetime, ISettingsStore settingsStore)
    {
        _fileModificationRanges = new Dictionary<string, List<ModificationRange>>();

        var solutionPath = solution.SolutionDirectory.FullPath;
        
        if (string.IsNullOrEmpty(solutionPath))
        {
            Console.WriteLine("Solution path is null or empty, cannot initialize GitRepositoryHandler.");
            IsTrackingEnabled = false;
            return;
        }

        NCommitsProperty = settingsStore.BindToContextLive(lifetime, ContextRange.ApplicationWide)
            .GetValueProperty(lifetime, (MySettingsKey key) => key.NCommits);

        // Listen to changes to keep track of any updates
        NCommitsProperty.Change.Advise(lifetime, args =>
        {
            if (!args.HasNew) return;
            Console.WriteLine($"NCommits setting updated: {args.New}");
            OnRepositoryChanged();
        });

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
        return _fileModificationRanges.TryGetValue(normalizedPath, out var ranges)
            ? ranges
            : [];
    }

    private void StartMonitoring()
    {
        if (!IsTrackingEnabled) return;

        _gitMonitor = new GitRepositoryMonitor(_repositoryPath, OnRepositoryChanged);
    }

    private void OnRepositoryChanged()
    {
        LoadRecentModifications(GetNCommits());
    }

    private int GetNCommits()
    {
        return NCommitsProperty.Value;
    }

    private void LoadRecentModifications(int numberOfCommits = 1)
    {
        _fileModificationRanges.Clear();

        // Retrieve up to the specified number of commits + HEAD
        var commitHashes = ExecuteGitCommand($"log -n {numberOfCommits + 1} --pretty=format:%H").Split('\n');

        // Process each commit except the last one, which acts as a baseline
        for (var i = 0; i < commitHashes.Length - 1; i++)
        {
            var currentCommit = commitHashes[i];
            var parentCommit = commitHashes[i + 1];

            // Retrieve the diff output between the current commit and its parent
            var diffOutput = ExecuteGitCommand($"diff {parentCommit} {currentCommit}");

            // Retrieve the commit message for the current commit
            var commitMessage = ExecuteGitCommand($"show -s --format=%B {currentCommit}");

            // Parse the diff output with the specific commit message
            ParseDiffOutput(diffOutput, commitMessage);
        }
    }

    private void ParseDiffOutput(string diffOutput, string commitMessage)
    {
        var lines = diffOutput.Split('\n');
        string currentFile = null;
        var currentNewLineNumber = 0;
        var insideModificationBlock = false;

        foreach (var line in lines)
        {
            if (line.StartsWith("diff --git"))
            {
                var match = Regex.Match(line, @"diff --git a\/(.+?) b\/(.+)");
                if (match.Success)
                {
                    currentFile = match.Groups[2].Value;
                    currentNewLineNumber = 0;
                    insideModificationBlock = true;

                    if (!_fileModificationRanges.ContainsKey(currentFile))
                    {
                        _fileModificationRanges[currentFile] = new List<ModificationRange>();
                    }
                }
            }
            else if (insideModificationBlock && line.StartsWith("@@"))
            {
                var match = Regex.Match(line, @"\+(\d+)");
                if (match.Success)
                {
                    currentNewLineNumber = int.Parse(match.Groups[1].Value);
                }
            }
            else if (insideModificationBlock && line.StartsWith("+") && !line.StartsWith("+++"))
            {
                var lineContent = line.Substring(1);
                if (string.IsNullOrWhiteSpace(lineContent))
                {
                    currentNewLineNumber++;
                    continue;
                }

                // Find the first 5 non-whitespace characters in the line
                var highlightedCharCount = 0;
                var startHighlightOffset = -1;
                for (var i = 0; i < lineContent.Length && highlightedCharCount < 5; i++)
                {
                    if (!char.IsWhiteSpace(lineContent[i]))
                    {
                        if (highlightedCharCount == 0) startHighlightOffset = i;
                        highlightedCharCount++;
                    }
                }

                // If we found characters to highlight, add the range to the modification list
                if (highlightedCharCount > 0 && startHighlightOffset != -1)
                {
                    _fileModificationRanges[currentFile ?? throw new ArgumentNullException(nameof(currentFile))]
                        .Add(new ModificationRange(currentNewLineNumber, startHighlightOffset, highlightedCharCount,
                            commitMessage));
                }

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

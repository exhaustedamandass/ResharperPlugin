using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using ReSharperPlugin.MyPlugin.DataModels;
using ReSharperPlugin.MyPlugin.GitRepository.Helpers;
using ReSharperPlugin.MyPlugin.GitRepository.Monitors;
using ReSharperPlugin.MyPlugin.Options;

namespace ReSharperPlugin.MyPlugin.GitRepository.Handlers;

[SolutionComponent]
public class GitRepositoryHandler
{
    private readonly string _repositoryPath;
    private readonly Dictionary<string, List<ModificationRange>> _fileModificationRanges;
    private IProperty<int> NCommitsProperty { get; }
    
    public GitRepositoryHandler(ISolution solution, Lifetime lifetime, ISettingsStore settingsStore,
        GitRepositoryMonitor gitRepositoryMonitor)
    {
        _fileModificationRanges = new Dictionary<string, List<ModificationRange>>();

        var solutionPath = solution.SolutionDirectory.FullPath;
        
        if (string.IsNullOrEmpty(solutionPath))
        {
            Console.WriteLine("Solution path is null or empty, cannot initialize GitRepositoryHandler.");
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

        _repositoryPath = FileOperationsHelper.GetRepositoryRoot(solutionPath);

        gitRepositoryMonitor.RepositoryChangedSignal.Advise(lifetime, _ =>
        {
            OnRepositoryChanged(); // React to the repository change signal
        });

        if (!string.IsNullOrEmpty(_repositoryPath))
        {
            //do something in case solution is not in git repository 
        }
    }
    
    public List<ModificationRange> GetModificationRanges(string filePath)
    {
        var normalizedPath = FileOperationsHelper.NormalizePath(filePath);
        return _fileModificationRanges.TryGetValue(normalizedPath, out var ranges)
            ? ranges
            : [];
    }

    public string GetRepositoryPath()
    {
        return _repositoryPath;
    }
    
    private void OnRepositoryChanged()
    {
        LoadRecentModifications(GetNCommits());
    }

    private int GetNCommits()
    {
        return NCommitsProperty.Value;
    }

    private void LoadRecentModifications(int numberOfCommits)
    {
        _fileModificationRanges.Clear();

        // Retrieve up to the specified number of commits + HEAD
        var commitHashes = GitOperationsHelper
            .ExecuteGitCommand($"log -n {numberOfCommits + 1} --pretty=format:%H", _repositoryPath).Split('\n');

        // Process each commit except the last one, which acts as a baseline
        for (var i = 0; i < commitHashes.Length - 1; i++)
        {
            var currentCommit = commitHashes[i];
            var parentCommit = commitHashes[i + 1];

            // Retrieve the diff output between the current commit and its parent
            var diffOutput = GitOperationsHelper
                .ExecuteGitCommand($"diff {parentCommit} {currentCommit}", _repositoryPath);

            // Retrieve the commit message for the current commit
            var commitMessage = GitOperationsHelper
                .ExecuteGitCommand($"show -s --format=%B {currentCommit}", _repositoryPath);

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
            if (IsFileDiffLine(line))
            {
                currentFile = GetCurrentFileName(line);
                currentNewLineNumber = 0;
                insideModificationBlock = true;

                InitializeFileModificationList(currentFile);
            }
            else if (insideModificationBlock && IsLineNumberHeader(line))
            {
                currentNewLineNumber = ParseNewLineNumber(line);
            }
            else if (insideModificationBlock && IsModifiedLine(line))
            {
                ProcessModifiedLine(line, currentFile, ref currentNewLineNumber, commitMessage);
            }
            else if (IsNonModifiedLine(line))
            {
                if (!IsDeletedLine(line))
                {
                    currentNewLineNumber++;
                }
            }
        }
    }

    private static bool IsFileDiffLine(string line) => line.StartsWith("diff --git");

    private static string GetCurrentFileName(string line)
    {
        var match = Regex.Match(line, @"diff --git a\/(.+?) b\/(.+)");
        return match.Success ? match.Groups[2].Value : null;
    }

    private void InitializeFileModificationList(string fileName)
    {
        if (!_fileModificationRanges.ContainsKey(fileName))
        {
            _fileModificationRanges[fileName] = [];
        }
    }

    private static bool IsLineNumberHeader(string line) => line.StartsWith("@@");

    private static int ParseNewLineNumber(string line)
    {
        var match = Regex.Match(line, @"\+(\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    private static bool IsModifiedLine(string line) => line.StartsWith("+") && !line.StartsWith("+++");

    private void ProcessModifiedLine(string line, string currentFile, ref int lineNumber, string commitMessage)
    {
        var lineContent = line[1..];
        if (string.IsNullOrWhiteSpace(lineContent))
        {
            lineNumber++;
            return;
        }

        var highlightRange = GetHighlightRange(lineContent);
        if (highlightRange != null && currentFile != null)
        {
            _fileModificationRanges[currentFile].Add(new ModificationRange(
                lineNumber, highlightRange.Value.StartOffset, highlightRange.Value.Length, commitMessage));
        }

        lineNumber++;
    }

    private static (int StartOffset, int Length)? GetHighlightRange(string lineContent)
    {
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

        if (highlightedCharCount > 0 && startHighlightOffset != -1)
        {
            return (startHighlightOffset, highlightedCharCount);
        }
        
        return null;
    }

    private static bool IsNonModifiedLine(string line) => line.StartsWith(" ") || line.StartsWith("-");

    private static bool IsDeletedLine(string line) => line.StartsWith("-");
}

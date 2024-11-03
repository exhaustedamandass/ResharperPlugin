using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using ReSharperPlugin.MyPlugin.DataModels;
using ReSharperPlugin.MyPlugin.GitRepository.Monitors;
using ReSharperPlugin.MyPlugin.GitRepository.Parsers;
using ReSharperPlugin.MyPlugin.Helpers;
using ReSharperPlugin.MyPlugin.Options;

namespace ReSharperPlugin.MyPlugin.GitRepository.Handlers;

/// <summary>
/// Manages Git repository data, monitors for changes, and retrieves recent code modifications.
/// </summary>
[SolutionComponent]
public class GitRepositoryHandler
{
    private const string LogCommandFormat = "log -n {0} --pretty=format:%H";
    private const string DiffCommandFormat = "diff --word-diff {0} {1}";
    private const string ShowCommitMessageFormat = "show -s --format=%B {0}";

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
            return;
        }
 
        _repositoryPath = FileOperationsHelper.GetRepositoryRoot(solutionPath);
 
        if (string.IsNullOrEmpty(_repositoryPath))
        {
            return;
        }
 
        NCommitsProperty = settingsStore.BindToContextLive(lifetime, ContextRange.ApplicationWide)
            .GetValueProperty(lifetime, (MySettingsKey key) => key.NCommits);
 
        // Listen to changes to keep track of any updates
        NCommitsProperty.Change.Advise(lifetime, args =>
        {
            OnRepositoryChanged();
        });
 
     
        gitRepositoryMonitor.RepositoryChangedSignal.Advise(lifetime, _ =>
        {
            OnRepositoryChanged(); // React to the repository change signal
        });
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
    
    private int GetNCommits()
    {
        return NCommitsProperty.Value;
    }
    
    private void OnRepositoryChanged()
    {
        _ = LoadRecentModifications(GetNCommits());
    }

    /// <summary>
    /// Loads recent modifications for the specified number of commits, parsing each commit's changes.
    /// </summary>
    /// <param name="numberOfCommits">The number of recent commits to analyze.</param>
    private async Task LoadRecentModifications(int numberOfCommits)
    {
        _fileModificationRanges.Clear();

        var commitHashes = (await GitOperationsHelper
            .ExecuteGitCommandAsync(string.Format(LogCommandFormat, numberOfCommits + 1), _repositoryPath)).Split('\n');

        for (var i = 0; i < commitHashes.Length - 1; i++)
        {
            var currentCommit = commitHashes[i];
            var parentCommit = commitHashes[i + 1];

            var diffOutput = await GitOperationsHelper
                .ExecuteGitCommandAsync(string.Format(DiffCommandFormat, parentCommit, currentCommit), _repositoryPath);

            var commitMessage = await GitOperationsHelper
                .ExecuteGitCommandAsync(string.Format(ShowCommitMessageFormat, currentCommit), _repositoryPath);

            // Parse the diff output and get a temporary dictionary of modification ranges
            var commitModificationRanges = DiffParser.ParseDiffOutput(diffOutput, commitMessage);

            // Merge parsed modifications into the main _fileModificationRanges dictionary
            foreach (var file in commitModificationRanges.Keys)
            {
                if (!_fileModificationRanges.ContainsKey(file))
                {
                    _fileModificationRanges[file] = [];
                }
                _fileModificationRanges[file].AddRange(commitModificationRanges[file]);
            }
        }
    }
}

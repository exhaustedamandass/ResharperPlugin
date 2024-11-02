using System;
using System.Collections.Generic;
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

[SolutionComponent]
public class GitRepositoryHandler
{
    private readonly string _repositoryPath;
    private Dictionary<string, List<ModificationRange>> _fileModificationRanges;
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
    
    private int GetNCommits()
    {
        return NCommitsProperty.Value;
    }
    
    private void OnRepositoryChanged()
    {
        LoadRecentModifications(GetNCommits());
    }

    private void LoadRecentModifications(int numberOfCommits)
    {
        _fileModificationRanges.Clear();

        var commitHashes = GitOperationsHelper
            .ExecuteGitCommand($"log -n {numberOfCommits + 1} --pretty=format:%H", _repositoryPath).Split('\n');

        for (var i = 0; i < commitHashes.Length - 1; i++)
        {
            var currentCommit = commitHashes[i];
            var parentCommit = commitHashes[i + 1];

            var diffOutput = GitOperationsHelper
                .ExecuteGitCommand($"diff --word-diff {parentCommit} {currentCommit}", _repositoryPath);

            var commitMessage = GitOperationsHelper
                .ExecuteGitCommand($"show -s --format=%B {currentCommit}", _repositoryPath);

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

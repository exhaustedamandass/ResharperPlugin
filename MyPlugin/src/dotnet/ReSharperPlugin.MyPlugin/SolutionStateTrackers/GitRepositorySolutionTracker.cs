using System;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Tasks;
using JetBrains.ReSharper.Resources.Shell;

namespace ReSharperPlugin.MyPlugin.SolutionStateTrackers;

[ShellComponent]
public class GitRepositorySolutionTracker
{
    public ISignal<ISolution> AfterSolutionOpened { get; }
    public ISignal<ISolution> BeforeSolutionClosed { get; }
    public ISignal<ISolution> OnGitRepositoryChanged { get; }
    
    public ISolution Solution { get; private set; }
    private readonly IProperty<string> _solutionName;

    [Obsolete("Obsolete")]
    public GitRepositorySolutionTracker([NotNull] Lifetime lifetime,
        ISolution solution,
        ISolutionLoadTasksScheduler scheduler)
    {
        AfterSolutionOpened = new Signal<ISolution>(lifetime, "GitRepositorySolutionTracker.AfterSolutionOpened");
        BeforeSolutionClosed = new Signal<ISolution>(lifetime, "GitRepositorySolutionTracker.BeforeSolutionClosed");
        OnGitRepositoryChanged = new Signal<ISolution>(lifetime, "GitRepositorySolutionTracker.OnGitRepositoryChanged");
        
        _solutionName = new Property<string>(lifetime, "GitRepositorySolutionTracker.SolutionName") { Value = "None" };
    }

    public static GitRepositorySolutionTracker Instance => Shell.Instance.GetComponent<GitRepositorySolutionTracker>();

    public void HandleSolutionOpened(ISolution solution)
    {
        Solution = solution;
        _solutionName.Value = solution.SolutionFile?.Name;
        AfterSolutionOpened.Fire(solution);
    }
    
    public void HandleSolutionClosed()
    {
        if (Solution == null)
            return;

        _solutionName.Value = "None";
        BeforeSolutionClosed.Fire(Solution);
        Solution = null;
    }

    public void NotifyRepositoryChanged(ISolution solution)
    {
        OnGitRepositoryChanged.Fire(solution);
    }
}
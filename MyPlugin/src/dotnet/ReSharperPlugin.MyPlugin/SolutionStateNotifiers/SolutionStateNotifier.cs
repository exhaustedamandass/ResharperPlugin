using System;
using JetBrains.Annotations;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Tasks;
using ReSharperPlugin.MyPlugin.SolutionStateTrackers;

namespace ReSharperPlugin.MyPlugin.SolutionStateNotifiers;

[SolutionComponent]
public class SolutionStateNotifier
{
    [Obsolete("Obsolete")]
    public SolutionStateNotifier(
        Lifetime lifetime,
        [NotNull] ISolution solution,
        [NotNull] ISolutionLoadTasksScheduler scheduler,
        [NotNull] GitRepositorySolutionTracker solutionTracker)
    {
        // Null checks to ensure parameters are valid
        if (solution == null)
            throw new ArgumentNullException(nameof(solution));
        if (scheduler == null)
            throw new ArgumentNullException(nameof(scheduler));
        if (solutionTracker == null)
            throw new ArgumentNullException(nameof(solutionTracker));

        // Schedule a task to initialize GitRepositorySolutionTracker after solution load is complete
        scheduler.EnqueueTask(new SolutionLoadTask(typeof(GitRepositorySolutionTracker),
            SolutionLoadTaskKinds.Done, () => solutionTracker.HandleSolutionOpened(solution)));

        // Register a cleanup action to be called when the solution is closed
        lifetime.AddAction(solutionTracker.HandleSolutionClosed);
    }
}
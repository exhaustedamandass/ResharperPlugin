using System;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using ReSharperPlugin.MyPlugin.GitRepository.Monitors;

namespace ReSharperPlugin.MyPlugin.ChangeHandlers;

/// <summary>
/// Handles repository changes by triggering daemon invalidation, which refreshes solution highlights
/// when the repository undergoes any changes.
/// </summary>
[SolutionComponent]
public class RepositoryChangeHandler
{
    private readonly IDaemon _daemon;

    // Constant for the daemon invalidation reason
    private const string InvalidateReason = "";

    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryChangeHandler"/> class.
    /// Subscribes to repository change notifications and invalidates the daemon when a change is detected.
    /// </summary>
    /// <param name="lifetime">Lifetime scope for managing the subscription.</param>
    /// <param name="gitMonitor">Monitors the repository for changes and emits signals.</param>
    /// <param name="daemon">The daemon responsible for solution highlighting.</param>
    public RepositoryChangeHandler(Lifetime lifetime, GitRepositoryMonitor gitMonitor, IDaemon daemon)
    {
        _daemon = daemon;

        // Subscribe to the RepositoryChangedSignal from the Git monitor
        gitMonitor.RepositoryChangedSignal.Advise(lifetime, _ =>
        {
            HandleRepositoryChange();
        });
    }

    /// <summary>
    /// Handles the repository change event by invalidating highlights across the solution.
    /// </summary>
    private void HandleRepositoryChange()
    {
        _daemon.Invalidate(InvalidateReason); // Invalidate the entire solution
    }
}
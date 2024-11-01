using System;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using ReSharperPlugin.MyPlugin.GitRepository.Monitors;

namespace ReSharperPlugin.MyPlugin.ChangeHandlers;

[SolutionComponent]
public class RepositoryChangeHandler
{
    private readonly IDaemon _daemon;

    public RepositoryChangeHandler(Lifetime lifetime, GitRepositoryMonitor gitMonitor, IDaemon daemon)
    {
        _daemon = daemon;

        // Subscribe to the RepositoryChangedSignal from the monitor
        gitMonitor.RepositoryChangedSignal.Advise(lifetime, _ =>
        {
            HandleRepositoryChange();
        });
    }

    private void HandleRepositoryChange()
    {
        InvalidateHighlighting(); // Trigger daemon invalidation
    }

    private void InvalidateHighlighting()
    {
        _daemon.Invalidate(""); // Invalidate the entire solution
        Console.WriteLine("Invalidated daemon for re-analysis after repository change.");
    }
}
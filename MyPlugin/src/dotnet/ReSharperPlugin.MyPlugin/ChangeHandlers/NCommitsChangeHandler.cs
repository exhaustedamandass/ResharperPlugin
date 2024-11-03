using System;
using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using ReSharperPlugin.MyPlugin.Options;

namespace ReSharperPlugin.MyPlugin.ChangeHandlers;

/// <summary>
/// Monitors changes to the NCommits setting and triggers invalidation of the daemon
/// to refresh highlighting when the setting is updated.
/// </summary>
[SolutionComponent]
public class NCommitsChangeHandler
{
    private readonly IDaemon _daemon;

    /// <summary>
    /// Initializes a new instance of the <see cref="NCommitsChangeHandler"/> class.
    /// Binds to the NCommits setting and listens for changes, triggering the daemon invalidation on updates.
    /// </summary>
    /// <param name="lifetime">Lifetime scope for the binding and change listener.</param>
    /// <param name="settingsStore">The settings store containing the NCommits setting.</param>
    /// <param name="daemon">The daemon responsible for solution highlighting.</param>
    public NCommitsChangeHandler(Lifetime lifetime, ISettingsStore settingsStore, IDaemon daemon)
    {
        _daemon = daemon;

        // Bind to the live context of the NCommits setting in the application-wide scope
        var nCommitsProperty = settingsStore.BindToContextLive(lifetime, ContextRange.ApplicationWide)
            .GetValueProperty(lifetime, (MySettingsKey key) => key.NCommits);

        // Listen to changes in the NCommits setting
        nCommitsProperty.Change.Advise(lifetime, args =>
        {
            if (!args.HasNew) return;

            // Trigger invalidation to refresh solution highlighting
            InvalidateHighlighting();
        });
    }

    /// <summary>
    /// Invalidates the daemon for the entire solution, refreshing all highlights.
    /// </summary>
    private void InvalidateHighlighting()
    {
        _daemon.Invalidate("Invalidating the entire solution to refresh highlighting");
    }
}

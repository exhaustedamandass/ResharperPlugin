using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using System;
using JetBrains.Lifetimes;

namespace ReSharperPlugin.MyPlugin.Options.ChangeHandlers;


[SolutionComponent]
public class NCommitsChangeHandler
{
    private readonly IDaemon _daemon;

    public NCommitsChangeHandler(Lifetime lifetime, ISettingsStore settingsStore, IDaemon daemon)
    {
        _daemon = daemon;

        // Bind to the NCommits setting in the settings store
        var nCommitsProperty = settingsStore.BindToContextLive(lifetime, ContextRange.ApplicationWide)
            .GetValueProperty(lifetime, (MySettingsKey key) => key.NCommits);

        // Listen to changes to NCommits and trigger daemon invalidation
        nCommitsProperty.Change.Advise(lifetime, args =>
        {
            if (!args.HasNew) return;

            Console.WriteLine($"NCommits setting updated to: {args.New}");
            InvalidateHighlighting();
        });
    }

    private void InvalidateHighlighting()
    {
        // Invalidate the entire solution to refresh highlighting
        _daemon.Invalidate("Invalidating the entire solution to refresh highlighting");
        Console.WriteLine("Triggered rehighlighting for entire solution due to NCommits change.");
    }
}
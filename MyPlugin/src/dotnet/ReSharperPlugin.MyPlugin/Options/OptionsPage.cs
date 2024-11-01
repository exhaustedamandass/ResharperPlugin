using System;
using JetBrains.Application.Settings.WellKnownRootKeys;
using JetBrains.Application.UI.Options;
using JetBrains.Application.UI.Options.OptionPages;
using JetBrains.Application.UI.Options.OptionsDialog;
using JetBrains.DataFlow;
using JetBrains.IDE.UI.Options;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.Resources;

namespace ReSharperPlugin.MyPlugin.Options;

[OptionsPage(Pid, "R# Git plugin", typeof(FeaturesEnvironmentOptionsThemedIcons.Highlighting), ParentId = ToolsPage.PID)]
public class OptionsPage : BeSimpleOptionsPage
{
    private const string Pid = "ReSharperGitPluginOptions";
    
    [Obsolete("Obsolete")]
    public OptionsPage(Lifetime lifetime, OptionsPageContext optionsPageContext,
        OptionsSettingsSmartContext optionsSettingsSmartContext, bool wrapInScrollablePanel = false) : base(lifetime,
        optionsPageContext, optionsSettingsSmartContext, wrapInScrollablePanel)
    {
        IProperty<int> nCommits = new Property<int>(lifetime, "ReSharperGitPluginOptions::nCommits");
        nCommits.SetValue(optionsSettingsSmartContext.StoreOptionsTransactionContext.GetValue( (MySettingsKey key) => key.NCommits));

        AddIntOption((MySettingsKey key) => key.NCommits, "Number of commits");
    }
}

[JetBrains.Application.Settings.SettingsKey(typeof(EnvironmentSettings), "My settings")]
public class MySettingsKey
{
    [JetBrains.Application.Settings.SettingsEntry(2, "Number of commits")]
    public int NCommits;
}
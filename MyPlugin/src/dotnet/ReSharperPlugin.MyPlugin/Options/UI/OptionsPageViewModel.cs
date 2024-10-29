using System;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.UIAutomation;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;

namespace ReSharperPlugin.MyPlugin.Options.UI;

public class OptionsPageViewModel : AAutomation
{
    private static OptionsPageViewModel _instance;
    public static OptionsPageViewModel Instance => _instance;
    private IProperty<int> NCommitsProperty { get; set; }

    [Obsolete("Obsolete")]
    public OptionsPageViewModel(Lifetime lifetime, ISettingsStore settingsStore)
    {
        _instance = this; // Initialize the singleton instance
        
        NCommitsProperty = new Property<int>(lifetime, "OptionsExampleViewModel.Number");
        var nCommitsValue = settingsStore.BindToContextLive(lifetime, ContextRange.ApplicationWide)
            .GetValueProperty(lifetime, (MySettingsKey key) => key.NCommits);

        nCommitsValue.Change.Advise_HasNew(lifetime, v =>
            {
                NCommitsProperty.Value = v.Property.Value;
            }
        );
    }

    public int GetNCommits()
    {
        return NCommitsProperty.Value;
    }
}
using System;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.UIAutomation;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;

namespace ReSharperPlugin.MyPlugin.Options.UI;

public class OptionsPageViewModel : AAutomation
{
    private IProperty<int> Number { get; set; }

    [Obsolete("Obsolete")]
    public OptionsPageViewModel(Lifetime lifetime, ISettingsStore settingsStore)
    {
        Number = new Property<int>(lifetime, "OptionsExampleViewModel.Number");
        
        var nCommitsValue = settingsStore.BindToContextLive(lifetime, ContextRange.ApplicationWide)
            .GetValueProperty(lifetime, (MySettingsKey key) => key.NCommits);

        nCommitsValue.Change.Advise_HasNew(lifetime, v =>
            {
                Number.Value = v.Property.Value;
            }
        );
    }
}
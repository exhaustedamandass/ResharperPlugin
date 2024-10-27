using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace ReSharperPlugin.MyPlugin.Daemons;

[DaemonStage]
public class CommitModificationDaemonStage : IDaemonStage
{
    public IEnumerable<IDaemonStageProcess> CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
        DaemonProcessKind processKind)
    {
        throw new System.NotImplementedException();
    }
}
using System;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace ReSharperPlugin.MyPlugin.Daemons;

public class CommitModificationDaemonStageProcess : IDaemonStageProcess
{
    public void Execute(Action<DaemonStageResult> committer)
    {
        throw new NotImplementedException();
    }

    public IDaemonProcess DaemonProcess { get; }
}
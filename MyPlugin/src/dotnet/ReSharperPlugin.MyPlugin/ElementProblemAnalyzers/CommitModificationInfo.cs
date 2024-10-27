using System;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon.Specific.Errors.Internal;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace ReSharperPlugin.MyPlugin.ElementProblemAnalyzers;

//TODO: fix this
[StaticSeverityHighlighting(Severity.INFO, typeof(HighlightingGroupIds.GutterMarks))]
public class CommitModificationInfo : IHighlighting
{
    
    public bool IsValid()
    {
        throw new System.NotImplementedException();
    }

    public DocumentRange CalculateRange()
    {
        throw new System.NotImplementedException();
    }

    //TODO: should return the corresponding commit message.
    public string ToolTip => throw new NotImplementedException();
    
    public string ErrorStripeToolTip { get; }
}
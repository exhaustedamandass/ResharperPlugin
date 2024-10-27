using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace ReSharperPlugin.MyPlugin.ElementProblemAnalyzers;

[StaticSeverityHighlighting(Severity.INFO, typeof(HighlightingGroupIds.GutterMarks))]
public class CommitModificationInfo : IHighlighting
{
    private readonly DocumentRange _range;
    private readonly string _commitMessage;

    public CommitModificationInfo(DocumentRange range, string commitMessage)
    {
        _range = range;
        _commitMessage = commitMessage;
        ErrorStripeToolTip = $"Commit: {_commitMessage}";
    }

    public bool IsValid() => _range.IsValid();

    public DocumentRange CalculateRange() => _range;

    public string ToolTip => $"Commit message: {_commitMessage}";

    public string ErrorStripeToolTip { get; }
}
using System.Linq;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.MyPlugin.GitRepository.Handlers;

namespace ReSharperPlugin.MyPlugin.ElementProblemAnalyzers;

[ElementProblemAnalyzer(typeof(ITokenNode), HighlightingTypes = new []{typeof(CommitModificationInfo)})]
public class CommitModificationAnalyzer : ElementProblemAnalyzer<ITokenNode>
{
    private readonly GitRepositoryHandler _gitRepositoryHandler;

    public CommitModificationAnalyzer(GitRepositoryHandler gitRepositoryHandler)
    {
        _gitRepositoryHandler = gitRepositoryHandler;
    }
    
    protected override void Run(ITokenNode element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
        if (!_gitRepositoryHandler.IsTrackingEnabled || !_gitRepositoryHandler.IsFileModified(element.GetSourceFile().GetLocation().FullPath))
            return;

        // Check if the token contains non-whitespace characters
        var text = element.GetText();
        var nonWhitespaceIndex = text.Take(5).Select((ch, index) => (ch, index)).FirstOrDefault(pair => !char.IsWhiteSpace(pair.ch)).index;

        if (nonWhitespaceIndex >= 0)
        {
            var range = new DocumentRange(element.GetDocumentRange().Document, element.GetDocumentRange().TextRange.StartOffset + nonWhitespaceIndex);
            var commitMessage = _gitRepositoryHandler.GetCommitMessageForLine(element.GetSourceFile().GetLocation().FullPath, element.GetTreeStartOffset().Offset);

            consumer.AddHighlighting(new CommitModificationInfo(range, commitMessage));
        }
    }
}
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.dataStructures.TypedIntrinsics;
using ReSharperPlugin.MyPlugin.GitRepository.Handlers;

namespace ReSharperPlugin.MyPlugin.ElementProblemAnalyzers;

[ElementProblemAnalyzer(typeof(IFile), HighlightingTypes = new[] { typeof(CommitModificationInfo) })]
public class CommitModificationAnalyzer : ElementProblemAnalyzer<IFile>
{
    private readonly GitRepositoryHandler _gitRepositoryHandler;

    public CommitModificationAnalyzer(GitRepositoryHandler gitRepositoryHandler)
    {
        _gitRepositoryHandler = gitRepositoryHandler;
    }

    protected override void Run(IFile file, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
        // Get the full file path for the current file
        var filePath = file.GetSourceFile()?.GetLocation().FullPath;
        if (string.IsNullOrEmpty(filePath)) return;

        // Convert the file path to a repository-relative path using the GitRepositoryHandler
        var relativeFilePath = _gitRepositoryHandler.GetRelativePath(filePath);

        // Retrieve all modification ranges for this relative file path
        var modificationRanges = _gitRepositoryHandler.GetModificationRanges(relativeFilePath);

        if (!modificationRanges.Any()) return;

        // Loop through each modification range and highlight it
        foreach (var range in modificationRanges)
        {
            var lineStartOffset = file.GetDocumentRange().Document.GetLineStartOffset((Int32<DocLine>)range.StartLine);
            var modificationStartOffset = lineStartOffset + range.StartChar;
            var modificationEndOffset = modificationStartOffset + range.Length;

            var modificationRange = new DocumentRange(file.GetDocumentRange().Document, new TextRange(modificationStartOffset, modificationEndOffset));

            // Add highlighting for this modified range
            consumer.AddHighlighting(new CommitModificationInfo(modificationRange, range.CommitMessage));
        }
    }
}
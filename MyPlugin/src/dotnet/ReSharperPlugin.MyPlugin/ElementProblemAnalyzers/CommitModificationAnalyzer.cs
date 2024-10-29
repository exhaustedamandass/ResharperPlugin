using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.dataStructures.TypedIntrinsics;
using ReSharperPlugin.MyPlugin.GitRepository.Handlers;
using System;
using System.Linq;

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

        // Sort modification ranges by starting line, then by start character position
        modificationRanges = modificationRanges.OrderBy(range => range.StartLine)
                                               .ThenBy(range => range.StartChar)
                                               .ToList();

        // Get the document's total length to ensure offsets stay in bounds
        var document = file.GetDocumentRange().Document;
        var documentLength = document.GetTextLength();

        // Highlight only the first 5 non-whitespace characters in the first modified line
        foreach (var range in modificationRanges)
        {
            // Calculate the document range for the modified text based on the modification range details
            var lineStartOffset = document.GetLineStartOffset((Int32<DocLine>)(range.StartLine - 1)); 
            var modificationStartOffset = lineStartOffset + range.StartChar;
            var modificationEndOffset = Math.Min(modificationStartOffset + range.Length, documentLength);

            // Extract the modified text from the document and identify first 5 non-whitespace characters
            var modifiedText = document.GetText(new TextRange(modificationStartOffset, modificationEndOffset));

            int highlightedCharCount = 0;
            int startHighlightOffset = -1;
            int endHighlightOffset = modificationStartOffset;

            for (int i = 0; i < modifiedText.Length && highlightedCharCount < 5; i++)
            {
                // Skip whitespace and braces '{', '}', highlighting only meaningful code
                if (!char.IsWhiteSpace(modifiedText[i]) && modifiedText[i] != '{' && modifiedText[i] != '}')
                {
                    if (highlightedCharCount == 0) startHighlightOffset = modificationStartOffset + i;
                    highlightedCharCount++;
                    endHighlightOffset = modificationStartOffset + i + 1;
                }
            }

            // If at least one non-whitespace character is highlighted, create a highlight range
            if (highlightedCharCount > 0 && startHighlightOffset != -1)
            {
                var highlightRange = new DocumentRange(document, new TextRange(startHighlightOffset, endHighlightOffset));

                // Add highlighting for this modified range with the commit message
                consumer.AddHighlighting(new CommitModificationInfo(highlightRange, range.CommitMessage));
            }

            // Stop after highlighting the first modified line
            break;
        }
    }

}
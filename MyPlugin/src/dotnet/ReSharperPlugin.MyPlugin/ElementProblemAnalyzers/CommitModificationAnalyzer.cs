using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.dataStructures.TypedIntrinsics;
using ReSharperPlugin.MyPlugin.GitRepository.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using ReSharperPlugin.MyPlugin.DataModels;
using ReSharperPlugin.MyPlugin.Helpers;

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
        var filePath = FileOperationsHelper.GetFilePath(file);
        if (string.IsNullOrEmpty(filePath)) return;

        var relativeFilePath =
            FileOperationsHelper.GetRelativePath(filePath, _gitRepositoryHandler.GetRepositoryPath());
        var modificationRanges = GetSortedModificationRanges(relativeFilePath);
        if (!modificationRanges.Any()) return;

        HighlightFirstModifiedCharacters(file, modificationRanges, consumer);
    }

    private List<ModificationRange> GetSortedModificationRanges(string relativeFilePath)
    {
        // Retrieve and sort modification ranges by line and character position
        var modificationRanges = _gitRepositoryHandler.GetModificationRanges(relativeFilePath);
        return modificationRanges
            .OrderBy(range => range.StartLine)
            .ThenBy(range => range.StartChar)
            .ToList();
    }

    private static void HighlightFirstModifiedCharacters(IFile file, List<ModificationRange> modificationRanges,
    IHighlightingConsumer consumer)
    {
        var document = file.GetDocumentRange().Document;
        var documentLength = document.GetTextLength();

        var totalHighlightedChars = 0;

        foreach (var range in modificationRanges)
        {
            var (startOffset, endOffset) = GetModificationOffsets(document, range, documentLength);
            var modifiedText = document.GetText(new TextRange(startOffset, endOffset));

            var highlightedCharsCount = 0;
            var highlightStart = -1;

            for (var i = 0; i < modifiedText.Length && totalHighlightedChars < 5; i++)
            {
                if (!char.IsWhiteSpace(modifiedText[i]))
                {
                    if (highlightedCharsCount == 0) // Start of the highlight range
                        highlightStart = startOffset + i;

                    highlightedCharsCount++;
                    totalHighlightedChars++;

                    // Check if we've reached the limit of 5 highlighted characters
                    if (totalHighlightedChars == 5)
                    {
                        var highlightEnd = startOffset + i + 1;
                        consumer.AddHighlighting(new CommitModificationInfo(
                            new DocumentRange(document, new TextRange(highlightStart, highlightEnd)),
                            range.CommitMessage));
                        return;  // Exit as we've highlighted 5 non-whitespace characters
                    }
                }
                else if (highlightedCharsCount > 0) // Found whitespace within the target characters
                {
                    // Highlight up to the current position and reset counters
                    var highlightEnd = startOffset + i;
                    consumer.AddHighlighting(new CommitModificationInfo(
                        new DocumentRange(document, new TextRange(highlightStart, highlightEnd)),
                        range.CommitMessage));

                    highlightedCharsCount = 0;
                    highlightStart = -1;
                }
            }

            // Finalize any remaining highlight in the range if it ends without whitespace
            if (highlightedCharsCount > 0 && totalHighlightedChars < 5)
            {
                var highlightEnd = startOffset + modifiedText.Length;
                consumer.AddHighlighting(new CommitModificationInfo(
                    new DocumentRange(document, new TextRange(highlightStart, highlightEnd)),
                    range.CommitMessage));
            }

            // Stop if 5 characters have been highlighted
            if (totalHighlightedChars >= 5)
                break;
        }
    }

    private static (int startOffset, int endOffset) GetModificationOffsets(IDocument document, ModificationRange range,
        int documentLength)
    {
        // Get the start and end offsets for the specified line in the document
        var lineStartOffset = document.GetLineStartOffset((Int32<DocLine>)(range.StartLine - 1));
        var lineEndOffset = document.GetLineEndOffsetNoLineBreak((Int32<DocLine>)(range.StartLine - 1));

        // Calculate startOffset within the bounds of the line
        var startOffset = Math.Min(lineStartOffset + range.StartChar, lineEndOffset);

        // Calculate endOffset as startOffset + length, but limit it to the lineEndOffset and document length
        var endOffset = Math.Min(startOffset + range.Length, Math.Min(lineEndOffset, documentLength));

        // Ensure startOffset does not exceed endOffset
        startOffset = Math.Min(startOffset, endOffset);

        return (startOffset, endOffset);
    }
}
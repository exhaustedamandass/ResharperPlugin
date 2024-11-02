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

        HighlightFirstModifiedLine(file, modificationRanges, consumer);
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

    private void HighlightFirstModifiedLine(IFile file, List<ModificationRange> modificationRanges,
        IHighlightingConsumer consumer)
    {
        var document = file.GetDocumentRange().Document;
        var documentLength = document.GetTextLength();

        foreach (var range in modificationRanges)
        {
            var (startOffset, endOffset) = GetModificationOffsets(document, range, documentLength);
            var modifiedText = document.GetText(new TextRange(startOffset, endOffset));
            
            var highlightRange = GetHighlightRange(document, modifiedText, startOffset);
            if (highlightRange != null)
            {
                consumer.AddHighlighting(new CommitModificationInfo(highlightRange.Value, range.CommitMessage));
            }

            // Stop after highlighting the first modified line
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
        if (startOffset > endOffset)
        {
            startOffset = endOffset;
        }

        return (startOffset, endOffset);
    }


    private static DocumentRange? GetHighlightRange(IDocument document, string modifiedText, int startOffset)
    {
        var highlightedCharCount = 0;
        var startHighlightOffset = -1;
        var endHighlightOffset = startOffset;

        for (var i = 0; i < modifiedText.Length && highlightedCharCount < 5; i++)
        {
            if (!char.IsWhiteSpace(modifiedText[i]) && modifiedText[i] != '{' && modifiedText[i] != '}')
            {
                if (highlightedCharCount == 0) startHighlightOffset = startOffset + i;
                highlightedCharCount++;
                endHighlightOffset = startOffset + i + 1;
            }
        }

        if (highlightedCharCount > 0 && startHighlightOffset != -1)
        {
            return new DocumentRange(document, new TextRange(startHighlightOffset, endHighlightOffset));
        }
        return null;
    }
}
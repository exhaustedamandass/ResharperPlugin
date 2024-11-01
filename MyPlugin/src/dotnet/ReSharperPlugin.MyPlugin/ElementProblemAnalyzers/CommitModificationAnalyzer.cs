using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.dataStructures.TypedIntrinsics;
using ReSharperPlugin.MyPlugin.GitRepository.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using ReSharperPlugin.MyPlugin.DataModels;
using ReSharperPlugin.MyPlugin.GitRepository.Helpers;

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
        var filePath = GetFilePath(file);
        if (string.IsNullOrEmpty(filePath)) return;

        var relativeFilePath =
            FileOperationsHelper.GetRelativePath(filePath, _gitRepositoryHandler.GetRepositoryPath());
        var modificationRanges = GetSortedModificationRanges(relativeFilePath);
        if (!modificationRanges.Any()) return;

        HighlightFirstModifiedLine(file, modificationRanges, consumer);
    }

    //TODO: reused part, extract into a separate class
    private string GetFilePath(IFile file)
    {
        // Get the full file path for the current file
        return file.GetSourceFile()?.GetLocation().FullPath;
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
        // Calculate the start and end offsets for the modified text range
        var lineStartOffset = document.GetLineStartOffset((Int32<DocLine>)(range.StartLine - 1));
        var startOffset = lineStartOffset + range.StartChar;
        var endOffset = Math.Min(startOffset + range.Length, documentLength);
        return (startOffset, endOffset);
    }

    private DocumentRange? GetHighlightRange(IDocument document, string modifiedText, int startOffset)
    {
        var highlightedCharCount = 0;
        var startHighlightOffset = -1;
        var endHighlightOffset = startOffset;

        for (int i = 0; i < modifiedText.Length && highlightedCharCount < 5; i++)
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
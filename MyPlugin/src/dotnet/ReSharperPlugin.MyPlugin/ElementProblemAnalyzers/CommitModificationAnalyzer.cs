using System;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace ReSharperPlugin.MyPlugin.ElementProblemAnalyzers;

[ElementProblemAnalyzer(typeof(IFile), HighlightingTypes = new []{typeof(ClassModificationInfo)})]
public class CommitModificationAnalyzer : ElementProblemAnalyzer<IFile>
{
    protected override void Run(IFile element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
    // Get the document corresponding to the IFile element
        var document = element.GetSourceFile()?.Document;
        if (document == null)
        {
            throw new NullReferenceException(nameof(document));
        }

        // Iterate over the document text to find the first 5 non-whitespace characters
        var nonWhitespaceCount = 0;
        for (var i = 0; i < document.GetTextLength() && nonWhitespaceCount < 5; i++)
        {
            var currentChar = document.GetText(new TextRange(i, i + 1))[0];
            if (char.IsWhiteSpace(currentChar)) continue;
            // Define the range for highlighting this character
            var range = new DocumentRange(document, new TextRange(i, i + 1));

            // Create an instance of your bespoke highlighting class and add it to the consumer
            consumer.AddHighlighting(new ClassModificationInfo(), range);

            nonWhitespaceCount++;
        }
    }
}
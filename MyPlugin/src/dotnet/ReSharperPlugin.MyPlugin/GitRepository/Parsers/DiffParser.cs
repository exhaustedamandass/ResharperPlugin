using System.Collections.Generic;
using System.Text.RegularExpressions;
using ReSharperPlugin.MyPlugin.DataModels;

namespace ReSharperPlugin.MyPlugin.GitRepository.Parsers;

public static class DiffParser
{
    public static Dictionary<string, List<ModificationRange>> ParseDiffOutput(string diffOutput, string commitMessage)
    {
        var fileModificationRanges = new Dictionary<string, List<ModificationRange>>();
        var lines = diffOutput.Split('\n');
        string currentFile = null;
        var currentNewLineNumber = 0;
        var insideModificationBlock = false;

        foreach (var line in lines)
        {
            if (IsFileDiffLine(line))
            {
                currentFile = GetCurrentFileName(line);
                currentNewLineNumber = 0;
                insideModificationBlock = true;

                if (!string.IsNullOrEmpty(currentFile) && !fileModificationRanges.ContainsKey(currentFile))
                {
                    fileModificationRanges[currentFile] = new List<ModificationRange>();
                }
            }
            else switch (insideModificationBlock)
            {
                case true when IsLineNumberHeader(line):
                    currentNewLineNumber = ParseNewLineNumber(line);
                    break;
                case true when IsModifiedLine(line):
                    ProcessModifiedLine(line, currentFile, ref currentNewLineNumber, commitMessage, fileModificationRanges);
                    break;
                default:
                {
                    if (IsNonModifiedLine(line))
                    {
                        if (!IsDeletedLine(line))
                        {
                            currentNewLineNumber++;
                        }
                    }

                    break;
                }
            }
        }

        return fileModificationRanges;
    }

    private static bool IsFileDiffLine(string line) => line.StartsWith("diff --git");

    private static string GetCurrentFileName(string line)
    {
        var match = Regex.Match(line, @"diff --git a\/(.+?) b\/(.+)");
        return match.Success ? match.Groups[2].Value : null;
    }

    private static bool IsLineNumberHeader(string line) => line.StartsWith("@@");

    private static int ParseNewLineNumber(string line)
    {
        var match = Regex.Match(line, @"\+(\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    private static bool IsModifiedLine(string line) => line.StartsWith("+") && !line.StartsWith("+++");

    private static void ProcessModifiedLine(string line, string currentFile, ref int lineNumber, string commitMessage,
                                     Dictionary<string, List<ModificationRange>> fileModificationRanges)
    {
        var lineContent = line[1..];
        if (string.IsNullOrWhiteSpace(lineContent))
        {
            lineNumber++;
            return;
        }

        var highlightRange = GetHighlightRange(lineContent);
        if (highlightRange != null && currentFile != null)
        {
            fileModificationRanges[currentFile].Add(new ModificationRange(
                lineNumber, highlightRange.Value.StartOffset, highlightRange.Value.Length, commitMessage));
        }

        lineNumber++;
    }

    private static (int StartOffset, int Length)? GetHighlightRange(string lineContent)
    {
        var highlightedCharCount = 0;
        var startHighlightOffset = -1;

        for (var i = 0; i < lineContent.Length && highlightedCharCount < 5; i++)
        {
            if (char.IsWhiteSpace(lineContent[i])) continue;
            if (highlightedCharCount == 0) startHighlightOffset = i;
            highlightedCharCount++;
        }

        return highlightedCharCount > 0 && startHighlightOffset != -1 
            ? (startHighlightOffset, highlightedCharCount) 
            : null;
    }

    private static bool IsNonModifiedLine(string line) => line.StartsWith(" ") || line.StartsWith("-");

    private static bool IsDeletedLine(string line) => line.StartsWith("-");
}
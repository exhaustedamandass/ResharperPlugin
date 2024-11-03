using System.Collections.Generic;
using System.Text.RegularExpressions;
using ReSharperPlugin.MyPlugin.DataModels;

namespace ReSharperPlugin.MyPlugin.GitRepository.Parsers;

public static class DiffParser
{
    private const string FileDiffPrefix = "diff --git";
    private const string LineNumberHeaderPrefix = "@@";
    private const string AddedContentPattern = @"\{\+(.+?)\+\}";
    private const string DeletedContentPattern = @"\[-(.+?)\-\]";
    private const string DiffFileNamePattern = @"diff --git a\/(.+?) b\/(.+)";
    private const string LineNumberPattern = @"@@ -\d+,\d+ \+(\d+),";
    private const int DefaultLineNumber = 0;

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
                insideModificationBlock = true;

                if (!string.IsNullOrEmpty(currentFile) && !fileModificationRanges.ContainsKey(currentFile))
                {
                    fileModificationRanges[currentFile] = [];
                }
            }
            else if (insideModificationBlock && IsLineNumberHeader(line))
            {
                currentNewLineNumber = ParseNewLineNumber(line); // Set the starting line for new additions
            }
            else if (insideModificationBlock)
            {
                if (IsModifiedLine(line))
                {
                    ProcessWordDiffLine(line, currentFile, currentNewLineNumber, commitMessage, fileModificationRanges);
                }
                currentNewLineNumber++; // Increment line number for each line in the diff, regardless of modification
            }
        }

        return fileModificationRanges;
    }

    private static bool IsFileDiffLine(string line) => line.StartsWith(FileDiffPrefix);

    private static string GetCurrentFileName(string line)
    {
        var match = Regex.Match(line, DiffFileNamePattern);
        return match.Success ? match.Groups[2].Value : null;
    }

    private static bool IsLineNumberHeader(string line) => line.StartsWith(LineNumberHeaderPrefix);

    private static int ParseNewLineNumber(string line)
    {
        // Matches lines in the format "@@ -a,b +c,d @@"
        var match = Regex.Match(line, LineNumberPattern);
        return match.Success ? int.Parse(match.Groups[1].Value) : DefaultLineNumber;
    }

    private static bool IsModifiedLine(string line) => line.Contains("{+") || line.Contains("[-");

    private static void ProcessWordDiffLine(string line, string currentFile, int lineNumber, string commitMessage,
                                        Dictionary<string, List<ModificationRange>> fileModificationRanges)
    {
        var addedContentRegex = new Regex(AddedContentPattern);
        var deletedContentRegex = new Regex(DeletedContentPattern);

        var cumulativeOffset = 0; // Tracks the cumulative effect of deleted characters and markers
        var adjustedLine = line;  // A line where deletions are progressively removed for accurate indexing

        // First, process all deletions to calculate the cumulative offset and adjust the line for accurate indexing
        foreach (Match deleteMatch in deletedContentRegex.Matches(line))
        {
            var deletedText = deleteMatch.Value;
            var startIdx = deleteMatch.Index - cumulativeOffset;

            // Update cumulativeOffset by the length of the deleted content and its markers
            cumulativeOffset += deletedText.Length;

            // Remove the deleted text from `adjustedLine` to simulate the final appearance without deletions
            adjustedLine = adjustedLine.Remove(startIdx, deletedText.Length);
        }

        cumulativeOffset = 0; // Reset cumulativeOffset to re-calculate for additions based on the adjusted line

        // Now process additions based on the adjusted line
        foreach (Match addMatch in addedContentRegex.Matches(adjustedLine))
        {
            var startChar = addMatch.Index - cumulativeOffset; // Calculate precise starting char in adjusted line
            var addedText = addMatch.Groups[1].Value;
            var length = addedText.Length;

            // Add modification range with precise start and length
            if (currentFile != null)
            {
                fileModificationRanges[currentFile].Add(new ModificationRange(
                    lineNumber, startChar, length, commitMessage));
            }

            // Update cumulativeOffset by the markers `{+` and `+}` to adjust future matches correctly
            cumulativeOffset += "{+".Length + "+}".Length;
        }
    }
}

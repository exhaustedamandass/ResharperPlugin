using System;
using System.Collections.Generic;
using System.IO;

namespace ReSharperPlugin.MyPlugin.GitRepository.Helpers;

public static class FileOperationsHelper
{
    public static string ReadFileContents(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"File {filePath} not found.");
            return null;
        }
        return File.ReadAllText(filePath);
    }

    // Find the positions of the first 'n' non-whitespace characters in a string
    public static List<int> FindFirstNonWhitespacePositions(string content, int n)
    {
        List<int> positions = new List<int>();
        for (int i = 0; i < content.Length && positions.Count < n; i++)
        {
            if (!char.IsWhiteSpace(content[i]))
            {
                positions.Add(i);
            }
        }
        return positions;
    }
}
using System;
using System.IO;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharperPlugin.MyPlugin.Helpers;

public static class FileOperationsHelper
{
    public static string GetRelativePath(string fullPath, string repositoryPath)
    {
        if (string.IsNullOrEmpty(fullPath) || string.IsNullOrEmpty(repositoryPath))
        {
            return fullPath;
        }

        var repoUri = new Uri(repositoryPath + Path.DirectorySeparatorChar);
        var fileUri = new Uri(fullPath);

        return Uri.UnescapeDataString(repoUri.MakeRelativeUri(fileUri)
            .ToString()
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
    }
    
    public static string GetRepositoryRoot(string solutionPath)
    {
        var directoryInfo = new DirectoryInfo(solutionPath);
        while (directoryInfo != null && !Directory.Exists(Path.Combine(directoryInfo.FullName, ".git")))
        {
            directoryInfo = directoryInfo.Parent;
        }

        return directoryInfo?.FullName;
    }
    
    public static string NormalizePath(string path)
    {
        return path?.Replace('\\', '/');
    }
    
    public static string GetFilePath(IFile file)
    {
        // Get the full file path for the current file
        return file.GetSourceFile()?.GetLocation().FullPath;
    }
}
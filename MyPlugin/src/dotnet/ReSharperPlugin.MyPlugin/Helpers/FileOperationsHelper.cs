using System;
using System.IO;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharperPlugin.MyPlugin.Helpers;

/// <summary>
/// Provides helper methods for file operations, such as retrieving relative paths, repository roots, and normalizing paths.
/// </summary>
public static class FileOperationsHelper
{
    /// <summary>
    /// Gets the relative path of a file within the repository, based on the repository's root path.
    /// If either path is null or empty, returns the full path.
    /// </summary>
    /// <param name="fullPath">The full path of the file.</param>
    /// <param name="repositoryPath">The root path of the repository.</param>
    /// <returns>The relative path from the repository root to the file.</returns>
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

    /// <summary>
    /// Finds the root directory of a Git repository by searching for a .git folder from the solution path upwards.
    /// Returns the repository root path or null if no .git folder is found.
    /// </summary>
    /// <param name="solutionPath">The starting path, typically the solution's directory.</param>
    /// <returns>The repository root path, or null if not found.</returns>
    public static string GetRepositoryRoot(string solutionPath)
    {
        var directoryInfo = new DirectoryInfo(solutionPath);
        while (directoryInfo != null && !Directory.Exists(Path.Combine(directoryInfo.FullName, ".git")))
        {
            directoryInfo = directoryInfo.Parent;
        }

        return directoryInfo?.FullName;
    }

    /// <summary>
    /// Normalizes a file path to use forward slashes instead of backslashes.
    /// Useful for standardizing paths across platforms.
    /// </summary>
    /// <param name="path">The file path to normalize.</param>
    /// <returns>The normalized path with forward slashes.</returns>
    public static string NormalizePath(string path)
    {
        return path?.Replace('\\', '/');
    }

    /// <summary>
    /// Retrieves the full file path for a given file object, or null if the file object is null or has no source file.
    /// </summary>
    /// <param name="file">The file object from which to retrieve the path.</param>
    /// <returns>The full file path, or null if unavailable.</returns>
    public static string GetFilePath(IFile file)
    {
        return file.GetSourceFile()?.GetLocation().FullPath;
    }
}
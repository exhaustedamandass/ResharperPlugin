using System.Diagnostics;
using System.Threading.Tasks;

namespace ReSharperPlugin.MyPlugin.Helpers;

/// <summary>
/// Provides helper methods for executing Git commands within a specified repository.
/// </summary>
public static class GitOperationsHelper
{
    /// <summary>
    /// Executes a specified Git command in the context of the given repository path.
    /// Captures and returns the command's standard output.
    /// </summary>
    /// <param name="arguments">The Git command arguments (e.g., "status", "log").</param>
    /// <param name="repositoryPath">The path to the Git repository where the command should be executed.</param>
    /// <returns>The output from the Git command's standard output.</returns>
    public static async Task<string> ExecuteGitCommandAsync(string arguments, string repositoryPath)
    {
        return await Task.Run(() =>
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = repositoryPath,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output;
        });
    }
}
using System.Diagnostics;

namespace ReSharperPlugin.MyPlugin.GitRepository.Helpers;

public static class GitOperationsHelper
{
    public static string ExecuteGitCommand(string arguments, string repositoryPath)
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
    }
}
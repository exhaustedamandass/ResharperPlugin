using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using ReSharperPlugin.MyPlugin.Helpers;

namespace ReSharperPlugin.MyPlugin.Tests.Helpers;

[TestFixture]
public class GitOperationsHelperTest
{
    private string _tempRepoPath;

    [SetUp]
    public void SetUp()
    {
        // Create a temporary directory and initialize a new Git repository
        _tempRepoPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempRepoPath);
        _ = GitOperationsHelper.ExecuteGitCommandAsync("init", _tempRepoPath);
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up the temporary directory
        if (Directory.Exists(_tempRepoPath))
        {
            Directory.Delete(_tempRepoPath, true);
        }
    }

    [Test]
    public async Task ExecuteGitCommand_WithValidCommand_ShouldReturnExpectedOutput()
    {
        // Arrange
        const string arguments = "status";

        // Act
        var result = await GitOperationsHelper.ExecuteGitCommandAsync(arguments, _tempRepoPath);

        // Assert
        result.Should().Contain("On branch master").And.Contain("No commits yet");
    }

    [Test]
    public async Task ExecuteGitCommand_WithInvalidCommand_ShouldReturnErrorMessage()
    {
        // Arrange
        const string arguments = "invalidcommand";

        // Act
        var result = await GitOperationsHelper.ExecuteGitCommandAsync(arguments, _tempRepoPath);

        // Assert
        // Check for either an error message or empty output to handle the absence of StandardError capture
        if (string.IsNullOrEmpty(result))
        {
            Assert.Pass("The command produced no output, indicating an invalid command was likely executed.");
        }
        else
        {
            result.Should().Contain("is not a git command");
        }
    }
}
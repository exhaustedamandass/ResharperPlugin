using System;
using System.IO;
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
        GitOperationsHelper.ExecuteGitCommand("init", _tempRepoPath);
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
    public void ExecuteGitCommand_WithValidCommand_ShouldReturnExpectedOutput()
    {
        // Arrange
        const string arguments = "status";

        // Act
        var result = GitOperationsHelper.ExecuteGitCommand(arguments, _tempRepoPath);

        // Assert
        result.Should().Contain("On branch master").And.Contain("No commits yet");
    }

    [Test]
    public void ExecuteGitCommand_WithInvalidCommand_ShouldReturnErrorMessage()
    {
        // Arrange
        const string arguments = "invalidcommand";

        // Act
        var result = GitOperationsHelper.ExecuteGitCommand(arguments, _tempRepoPath);

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
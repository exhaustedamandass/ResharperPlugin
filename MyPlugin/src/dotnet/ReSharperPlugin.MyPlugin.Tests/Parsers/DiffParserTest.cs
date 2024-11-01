using FluentAssertions;
using NUnit.Framework;
using ReSharperPlugin.MyPlugin.GitRepository.Parsers;

namespace ReSharperPlugin.MyPlugin.Tests.Parsers;

[TestFixture]
public class DiffParserTests
{
    [Test]
    public void ParseDiffOutput_WithSingleFileModification_ShouldReturnModificationRange()
    {
        // Arrange
        var diffOutput = "diff --git a/file.txt b/file.txt\n" +
                         "@@ -1,5 +1,5 @@\n" +
                         "+Modified line\n";
        var commitMessage = "Commit message";

        // Act
        var result = DiffParser.ParseDiffOutput(diffOutput, commitMessage);

        // Assert
        result.Should().ContainKey("file.txt");
        result["file.txt"].Should().HaveCount(1);
        result["file.txt"][0].CommitMessage.Should().Be(commitMessage);
        result["file.txt"][0].StartLine.Should().Be(1);
        result["file.txt"][0].StartChar.Should().Be(0);
        result["file.txt"][0].Length.Should().Be(5);
    }

    [Test]
    public void ParseDiffOutput_WithMultipleFiles_ShouldReturnModificationRangesForEachFile()
    {
        // Arrange
        var diffOutput = "diff --git a/file1.txt b/file1.txt\n" +
                         "@@ -1,5 +1,5 @@\n" +
                         "+First modified line\n" +
                         "diff --git a/file2.txt b/file2.txt\n" +
                         "@@ -2,5 +2,5 @@\n" +
                         "+Second modified line\n";
        var commitMessage = "Another commit";

        // Act
        var result = DiffParser.ParseDiffOutput(diffOutput, commitMessage);

        // Assert
        result.Should().ContainKeys("file1.txt", "file2.txt");
        result["file1.txt"].Should().HaveCount(1);
        result["file1.txt"][0].CommitMessage.Should().Be(commitMessage);
        result["file1.txt"][0].StartLine.Should().Be(1);
        result["file1.txt"][0].StartChar.Should().Be(0);
        result["file1.txt"][0].Length.Should().Be(5);

        result["file2.txt"].Should().HaveCount(1);
        result["file2.txt"][0].CommitMessage.Should().Be(commitMessage);
        result["file2.txt"][0].StartLine.Should().Be(2);
        result["file2.txt"][0].StartChar.Should().Be(0);
        result["file2.txt"][0].Length.Should().Be(5);
    }

    [Test]
    public void ParseDiffOutput_WithEmptyDiffOutput_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var diffOutput = "";
        var commitMessage = "Empty commit";

        // Act
        var result = DiffParser.ParseDiffOutput(diffOutput, commitMessage);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void ParseDiffOutput_WithNonModifiedLine_ShouldIgnoreLine()
    {
        // Arrange
        var diffOutput = "diff --git a/file.txt b/file.txt\n" +
                         "@@ -1,5 +1,5 @@\n" +
                         " Non-modified line\n";
        var commitMessage = "Commit with non-modified line";

        // Act
        var result = DiffParser.ParseDiffOutput(diffOutput, commitMessage);

        // Assert
        result.Should().ContainKey("file.txt");
        result["file.txt"].Should().BeEmpty();
    }

    [Test]
    public void ParseDiffOutput_WithDeletedLine_ShouldNotIncreaseLineNumber()
    {
        // Arrange
        var diffOutput = "diff --git a/file.txt b/file.txt\n" +
                         "@@ -1,5 +1,5 @@\n" +
                         "-Deleted line\n";
        var commitMessage = "Commit with deleted line";

        // Act
        var result = DiffParser.ParseDiffOutput(diffOutput, commitMessage);

        // Assert
        result.Should().ContainKey("file.txt");
        result["file.txt"].Should().BeEmpty();
    }

    [Test]
    public void ParseDiffOutput_WithWhitespaceOnlyLine_ShouldIgnoreLine()
    {
        // Arrange
        var diffOutput = "diff --git a/file.txt b/file.txt\n" +
                         "@@ -1,5 +1,5 @@\n" +
                         "+    \n";
        var commitMessage = "Commit with whitespace line";

        // Act
        var result = DiffParser.ParseDiffOutput(diffOutput, commitMessage);

        // Assert
        result.Should().ContainKey("file.txt");
        result["file.txt"].Should().BeEmpty();
    }
}

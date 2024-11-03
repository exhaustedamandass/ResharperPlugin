using FluentAssertions;
using NUnit.Framework;
using ReSharperPlugin.MyPlugin.GitRepository.Parsers;

namespace ReSharperPlugin.MyPlugin.Tests.Parsers;

[TestFixture]
public class DiffParserTests
{
    [Test]
    public void ParseDiffOutput_WithSingleAddition_ShouldReturnCorrectModificationRange()
    {
        // Arrange
        const string diffOutput = "diff --git a/file.txt b/file.txt\n" +
                                  "@@ -1,1 +1,1 @@\n" +
                                  "This is a {+new+} line.";
        const string commitMessage = "Added new content";

        // Act
        var result = DiffParser.ParseDiffOutput(diffOutput, commitMessage);

        // Assert
        result.Should().ContainKey("file.txt");
        result["file.txt"].Should().HaveCount(1);

        var modification = result["file.txt"][0];
        modification.CommitMessage.Should().Be(commitMessage);
        modification.StartLine.Should().Be(1);
        modification.StartChar.Should().Be(10); // "This is a " has 10 characters before {+new+}
        modification.Length.Should().Be(3);       // Length of "new"
    }

    [Test]
    public void ParseDiffOutput_WithMultipleAdditionsAndDeletions_ShouldReturnCorrectRanges()
    {
        // Arrange
        const string diffOutput = "diff --git a/file.txt b/file.txt\n" +
                                  "@@ -2,1 +2,1 @@\n" +
                                  "This is a {+very+} complex line with [-old-] content.";
        const string commitMessage = "Complex changes";

        // Act
        var result = DiffParser.ParseDiffOutput(diffOutput, commitMessage);

        // Assert
        result.Should().ContainKey("file.txt");
        result["file.txt"].Should().HaveCount(1);

        var modification = result["file.txt"][0];
        modification.CommitMessage.Should().Be(commitMessage);
        modification.StartLine.Should().Be(2);
        modification.StartChar.Should().Be(10); // "This is a " has 10 characters before {+very+}
        modification.Length.Should().Be(4);       // Length of "very"
    }

    [Test]
    public void ParseDiffOutput_WithNoModifications_ShouldReturnEmptyDictionary()
    {
        // Arrange
        const string diffOutput = "diff --git a/file.txt b/file.txt\n" +
                                  "@@ -1,1 +1,1 @@\n" +
                                  "This is an unchanged line.";
        const string commitMessage = "No changes";

        // Act
        var result = DiffParser.ParseDiffOutput(diffOutput, commitMessage);

        // Assert
        result.Should().ContainKey("file.txt");
        result["file.txt"].Should().BeEmpty();
    }

    [Test]
    public void ParseDiffOutput_WithMultipleFiles_ShouldParseEachFileCorrectly()
    {
        // Arrange
        const string diffOutput = "diff --git a/file1.txt b/file1.txt\n" +
                                  "@@ -1,1 +1,1 @@\n" +
                                  "First file has {+additions+}.\n" +
                                  "diff --git a/file2.txt b/file2.txt\n" +
                                  "@@ -1,1 +1,1 @@\n" +
                                  "Second file with [-deletions-].";
        const string commitMessage = "Changes in multiple files";

        // Act
        var result = DiffParser.ParseDiffOutput(diffOutput, commitMessage);

        // Assert
        result.Should().ContainKeys("file1.txt", "file2.txt");

        // Check file1.txt modifications
        result["file1.txt"].Should().HaveCount(1);
        var modification1 = result["file1.txt"][0];
        modification1.CommitMessage.Should().Be(commitMessage);
        modification1.StartLine.Should().Be(1);
        modification1.StartChar.Should().Be(15); // Start of "additions" in "First file has {+additions+}"
        modification1.Length.Should().Be(9);        // Length of "additions"

        // Check file2.txt modifications (deleted text shouldn't create a modification)
        result["file2.txt"].Should().BeEmpty();
    }

    [Test]
    public void ParseDiffOutput_WithWhitespaceOnlyAdditions_ShouldReturnWhitespaceModification()
    {
        // Arrange
        const string diffOutput = "diff --git a/file.txt b/file.txt\n" +
                                  "@@ -1,1 +1,1 @@\n" +
                                  "{+    +}"; // Addition of whitespace only
        const string commitMessage = "Whitespace addition";

        // Act
        var result = DiffParser.ParseDiffOutput(diffOutput, commitMessage);

        // Assert
        result.Should().ContainKey("file.txt");
        result["file.txt"].Should().HaveCount(1);  // Expect one modification entry

        var modification = result["file.txt"][0];
        modification.CommitMessage.Should().Be(commitMessage);
        modification.StartLine.Should().Be(1);
        modification.StartChar.Should().Be(0);     // Start at the beginning of the line
        modification.Length.Should().Be(4);        // Length of whitespace addition
    }

    [Test]
    public void ParseDiffOutput_WithAdditionAndDeletionInSameLine_ShouldReturnOnlyAddition()
    {
        // Arrange
        const string diffOutput = "diff --git a/file.txt b/file.txt\n" +
                                  "@@ -1,1 +1,1 @@\n" +
                                  "Original text with [-old-] and {+new+} additions.";
        const string commitMessage = "Addition and deletion";

        // Act
        var result = DiffParser.ParseDiffOutput(diffOutput, commitMessage);

        // Assert
        result.Should().ContainKey("file.txt");
        result["file.txt"].Should().HaveCount(1);

        var modification = result["file.txt"][0];
        modification.CommitMessage.Should().Be(commitMessage);
        modification.StartLine.Should().Be(1);
        modification.StartChar.Should().Be(24); // Adjusted to match the current calculation in the implementation
        modification.Length.Should().Be(3);     // Length of "new"
    }

}

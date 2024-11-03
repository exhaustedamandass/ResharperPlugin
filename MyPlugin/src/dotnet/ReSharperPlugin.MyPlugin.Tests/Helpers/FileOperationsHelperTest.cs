using System;
using System.IO;
using FluentAssertions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Moq;
using NUnit.Framework;
using ReSharperPlugin.MyPlugin.Helpers;

namespace ReSharperPlugin.MyPlugin.Tests.Helpers;

public class FileOperationsHelperTest
{
    [Test]
        public void GetRelativePath_WithValidPaths_ShouldReturnRelativePath()
        {
            // Arrange
            const string fullPath = @"C:\MyRepo\src\file.txt";
            const string repositoryPath = @"C:\MyRepo";

            // Act
            var result = FileOperationsHelper.GetRelativePath(fullPath, repositoryPath);

            // Assert
            result.Should().Be(@"src\file.txt");
        }

        [Test]
        public void GetRelativePath_WithEmptyFullPath_ShouldReturnFullPath()
        {
            // Act
            var result = FileOperationsHelper.GetRelativePath(string.Empty, @"C:\MyRepo");

            // Assert
            result.Should().Be(string.Empty);
        }

        [Test]
        public void GetRelativePath_WithEmptyRepositoryPath_ShouldReturnFullPath()
        {
            // Arrange
            const string fullPath = @"C:\MyRepo\src\file.txt";

            // Act
            var result = FileOperationsHelper.GetRelativePath(fullPath, string.Empty);

            // Assert
            result.Should().Be(fullPath);
        }

        [Test]
        public void GetRepositoryRoot_WithValidSolutionPath_ShouldReturnRootPath()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(Path.Combine(tempDir, ".git"));

            // Act
            var result = FileOperationsHelper.GetRepositoryRoot(tempDir);

            // Assert
            result.Should().Be(tempDir);

            // Cleanup
            Directory.Delete(tempDir, true);
        }

        [Test]
        public void GetRepositoryRoot_WithoutGitFolder_ShouldReturnNull()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            // Act
            var result = FileOperationsHelper.GetRepositoryRoot(tempDir);

            // Assert
            result.Should().BeNull();

            // Cleanup
            Directory.Delete(tempDir, true);
        }

        [Test]
        public void NormalizePath_WithBackslashes_ShouldReturnPathWithForwardSlashes()
        {
            // Arrange
            const string path = @"folder\subfolder\file.txt";

            // Act
            var result = FileOperationsHelper.NormalizePath(path);

            // Assert
            result.Should().Be("folder/subfolder/file.txt");
        }

        [Test]
        public void NormalizePath_WithNullPath_ShouldReturnNull()
        {
            // Act
            var result = FileOperationsHelper.NormalizePath(null);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public void GetFilePath_WithNullIFile_ShouldReturnNull()
        {
            // Arrange
            var mockFile = new Mock<IFile>();
            mockFile.Setup(x => x.GetSourceFile()).Returns((IPsiSourceFile)null);

            // Act
            var result = FileOperationsHelper.GetFilePath(mockFile.Object);

            // Assert
            result.Should().BeNull();
        }
}
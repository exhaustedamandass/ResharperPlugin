namespace ReSharperPlugin.MyPlugin.DataModels;

/// <summary>
/// Represents a range of code modifications, including the start line, start character, length of change, and associated commit message.
/// </summary>
public record ModificationRange(int StartLine, int StartChar, int Length, string CommitMessage);

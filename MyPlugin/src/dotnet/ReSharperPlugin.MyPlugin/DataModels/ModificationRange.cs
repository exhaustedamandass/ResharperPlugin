namespace ReSharperPlugin.MyPlugin.DataModels;

public record ModificationRange(int StartLine, int StartChar, int Length, string CommitMessage);

# Resharper Git Plugin

JetBrains test task

Integration with Git in ReSharper

## Project structure
```
ReSharperPlugin.MyPlugin
| Dependencies/
| ChangeHandlers/
|---- NCommitsChangeHandler.cs
|---- RepositoryChangeHandler.cs
| DataModels/
|---- ModificationRange.cs
| ElementProblemAnalyzers/
|---- CommitModificationAnalyzer.cs
|---- CommitModificationInfo.cs
| GitRepository/
|---- Handlers/
|     |---- GitRepositoryHandler.cs
|---- Monitors/
|     |---- GitRepositoryMonitor.cs
|---- Parsers/
|     |---- DiffParser.cs
| Helpers/
|---- FileOperationsHelper.cs
|---- GitOperationsHelper.cs
| Options/
|---- IMyPluginZone.cs

ReSharperPlugin.MyPlugin.Tests
| Dependencies/
| Helpers/
|---- FileOperationsHelperTest.cs
|---- GitOperationsHelperTest.cs
| Parsers/
|---- DiffParserTest.cs
| test/
|---- TestEnvironment.cs
```

## Functionality
Overview of functionality and usage

### Specify the Number of commits to highlight
Go to Extensions -> Resharper -> Options

![optionsNaviage](https://github.com/exhaustedamandass/ResharperPlugin/blob/main/MyPlugin/assets/optionsNavigate.png)

Scroll down to R# Git plugin

![toolsNavigate](https://github.com/exhaustedamandass/ResharperPlugin/blob/main/MyPlugin/assets/toolsNavigate.png)

Specify the number of commits

![numberOfCommits](https://github.com/exhaustedamandass/ResharperPlugin/blob/main/MyPlugin/assets/NumberOfCommits.png)

Click save

![SaveButton](https://github.com/exhaustedamandass/ResharperPlugin/blob/main/MyPlugin/assets/SaveButton.png)

### Highlighting of first 5 non-whitespace characters
After opening the _solution/specifying a new number of commits/performing git operations_ you can see the first 5 modified non-whitespace characters for each changed file in the currently specified number of commits.

![Highlithing](https://github.com/exhaustedamandass/ResharperPlugin/blob/main/MyPlugin/assets/Highlighting.png)

### Check commit message
By hovering over the blue-highlighted code, you can see a ToolTip message displaying the commit message to which the change belongs.  

![toolTipMessage](https://github.com/exhaustedamandass/ResharperPlugin/blob/main/MyPlugin/assets/ToolTipMessage.png)

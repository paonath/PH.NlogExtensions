# PH.NlogExtensions
PH.NlogExtensions is a library designed to extend the functionality of [NLog](https://nlog-project.org/), a popular logging framework for .NET applications. This library provides additional utilities for managing and compressing log files, making it easier to handle and archive logs in various scenarios.
## Features
- **Log File Compression**: Compress log files into ZIP archives for efficient storage and transfer.
- **Integration with NLog**: Seamlessly integrates with NLog to retrieve and process log files.
- **Async and Sync Support**: Provides both asynchronous and synchronous methods for log file operations.
- **Custom Compression Utility**: Utilizes `PH.CompressionUtility` for enhanced compression capabilities.
## Recent Changes
- Replaced the deprecated `Ionic.Zip` library with `System.IO.Compression` and `PH.CompressionUtility`.
- Improved memory management and performance during compression operations.
- Simplified API for compressing log files.
## Installation
To include PH.NlogExtensions in your project, add the following NuGet package:
## Code Examples
**GetCurrentLogFile(string targetFileName)**
```csharp
//assume Logger is NLog.Logger
// bytes is he content of current target named 'full'
var bytes = await Logger.GetCurrentLogFileAsync("full");

```
**CycleOverAllFileTargets()**
```csharp
//assume Logger is NLog.Logger
// dict is a Dictionary<string,byte[]> where Key = TargetName and Value = content
var dict = await Logger.GetAllCurrentLogFiles();
```
**GetWholeLogDirectoryAsZip**
```csharp
var bytes = Logger.GetWholeLogDirectoryAsZip();
System.IO.File.WriteAllBytes($@".\lg{DateTime.Now:yymmddHHmmss}.zip", bytes);
```
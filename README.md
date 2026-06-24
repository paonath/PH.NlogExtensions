# PH.NlogExtensions

`PH.NlogExtensions` is a high-performance, lightweight .NET library that extends the [NLog](https://nlog-project.org/) framework with powerful log management, retrieval, and compression utilities. 

Targeting `netstandard2.0`, it is fully compatible with both legacy .NET Framework applications (e.g., .NET 4.6.1+) and modern cross-platform .NET runtimes (e.g., .NET Core, .NET 5/6/7/8/9).

---

## Features

- **Robust Target Unwrapping**: Supports wrapped targets (`WrapperTargetBase` targets like `AsyncWrapper`, `BufferingWrapper`, etc.) by recursively traversing down to the underlying `FileTarget`.
- **Zip Compression**: Archive active log files or the entire log directory directly into ZIP streams or byte arrays using `System.IO.Compression` and `PH.CompressionUtility`.
- **Asynchronous & Synchronous Support**: Every extension method offers both asynchronous (`*Async`) and synchronous variants.
- **Easy Retrieval**: Access active log data as strings, byte arrays, or streams by specifying the NLog target name.

---

## Installation

Add the library to your project via the dotnet CLI:

```bash
dotnet add package PH.NlogExtensions
```

Or by adding a package reference in your `.csproj`:

```xml
<PackageReference Include="PH.NlogExtensions" Version="x.y.z" />
```

---

## Configuration Example

Define your file targets in `nlog.config` (e.g., an asynchronous wrapper wrapping a file target):

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">

  <targets>
    <!-- An asynchronous wrapper wrapping a File Target -->
    <target xsi:type="AsyncWrapper" name="asyncFile">
      <target xsi:type="File" name="myLogFile" fileName="logs/app-${shortdate}.log" />
    </target>
  </targets>

  <rules>
    <logger name="*" minlevel="Debug" writeTo="asyncFile" />
  </rules>
</nlog>
```

---

## Usage Guide

All methods are exposed as extension methods on the `NLog.Logger` class.

### 1. Compressing & Archiving Log Files

#### Get Current Active Log Files as ZIP Archive
Compress all active log files into a ZIP archive:

```csharp
using PH.NlogExtensions;

// Asynchronous (Byte Array)
byte[] zipBytes = await logger.GetCurrentLogFilesAsZipAsync(cancellationToken);

// Asynchronous (MemoryStream)
using (MemoryStream zipStream = await logger.GetCurrentLogFilesAsZipMemoryStreamAsync(cancellationToken))
{
    // Process stream...
}

// Synchronous (Byte Array)
byte[] zipBytesSync = logger.GetCurrentLogFilesAsZip();

// Synchronous (MemoryStream)
using (MemoryStream zipStreamSync = logger.GetCurrentLogFilesAsZipMemoryStream())
{
    // Process stream...
}
```

#### Get the Entire Log Directory as a ZIP Archive
Useful for archiving all logs in the directory including rotated or historical log files:

```csharp
// Asynchronous (Byte Array)
byte[] zipBytes = await logger.GetWholeLogDirectoryAsZipAsync();

// Asynchronous (MemoryStream)
using (MemoryStream zipStream = await logger.GetWholeLogDirectoryZipAsStreamAsync())
{
    // Process stream...
}

// Synchronous (Byte Array)
byte[] zipBytesSync = logger.GetWholeLogDirectoryAsZip();

// Synchronous (MemoryStream)
using (MemoryStream zipStreamSync = logger.GetWholeLogDirectoryZipAsStream())
{
    // Process stream...
}
```

---

### 2. Reading and Extracting Active Log Contents

#### Read Single Log File by Target Name
Retrieve the contents of a specific target's log file as a string or byte array:

```csharp
// Asynchronous - Read as String
string logText = await logger.ReadCurrentLogFileAsync("myLogFile", cancellationToken);

// Asynchronous - Get raw bytes
byte[] logBytes = await logger.GetCurrentLogFileAsync("myLogFile", cancellationToken);

// Synchronous - Read as String
string logTextSync = logger.ReadCurrentLogFile("myLogFile");

// Synchronous - Get raw bytes
byte[] logBytesSync = logger.GetCurrentLogFile("myLogFile");
```

#### Read All Active Log Files
Fetch all current log files configured in the active NLog configuration:

```csharp
// Asynchronous (Key: File Name, Value: Byte Content)
Dictionary<string, byte[]> allLogs = await logger.GetAllCurrentLogFilesAsync(cancellationToken);

// Asynchronous (Key: FileInfo, Value: Byte Content)
Dictionary<FileInfo, byte[]> allLogsWithInfo = await logger.GetAllCurrentLogFilesWithInfoAsync(cancellationToken);

// Synchronous (Key: File Name, Value: Byte Content)
Dictionary<string, byte[]> allLogsSync = logger.GetAllCurrentLogFiles();

// Synchronous (Key: FileInfo, Value: Byte Content)
Dictionary<FileInfo, byte[]> allLogsWithInfoSync = logger.GetAllCurrentLogFilesWithInfo();
```

---

## License

This project is licensed under the BSD-3-Clause License. See the [LICENSE](LICENSE) file for details.
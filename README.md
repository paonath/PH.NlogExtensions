# PH.NlogExtensions [![NuGet Badge](https://buildstats.info/nuget/PH.NlogExtensions)](https://www.nuget.org/packages/PH.NlogExtensions/)

[Nlog](https://github.com/NLog/NLog) Extensions 


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
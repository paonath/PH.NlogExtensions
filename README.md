# PH.NlogExtensions [![NuGet Badge](https://buildstats.info/nuget/PH.NlogExtensions)](https://www.nuget.org/packages/PH.NlogExtensions/)

[Nlog](https://github.com/NLog/NLog) Extensions 


## Code Examples

**GetCurrentLogFile(string targetFileName)**
```csharp

// bytes are the content of current target named 'full'
var bytes = await Logger.GetCurrentLogFile("full");

```
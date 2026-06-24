# PH.NlogExtensions Rules

Guidelines and instructions for maintaining the `PH.NlogExtensions` repository.

## Frameworks and Architecture

- **Library Target**: The core library targeting must remain `netstandard2.0` to preserve compatibility with both .NET Framework and modern .NET Core.
- **Testing**:
  - `PH.NlogExtensions.Test` targets `net8.0` for cross-platform modern .NET testing.
  - `PH.NlogExtensions.TestFw48` targets .NET Framework 4.8, which is only supported/runnable on Windows environments with .NET Framework installed. When developing on macOS or Linux, run tests using `dotnet test PH.NlogExtensions.Test/PH.NlogExtensions.Test.csproj` specifically.

## NLog Integration Rules

- **Target Unwrapping**: When searching for NLog targets by name, always support wrapped targets. Use the private `GetFileTarget` helper to safely traverse wrapper targets (`WrapperTargetBase` in `NLog.Targets.Wrappers`) down to the underlying `FileTarget`.
- **Cast Safety**: Never cast a target directly to `FileTarget` (e.g. `(FileTarget)target`). Always use safe casting (`as FileTarget`) and handle potential null values defensively.
- **Configuration Availability**: Always check if `LogManager.Configuration` is null before querying targets.
- **Directory Backups**: When zipping the entire log directory, do not require the specific active file target path to exist on disk (`file.Exists`). If the directory (`file.Directory`) exists and is accessible, proceed with zipping the directory contents.

## Testing Guidelines

- When writing new unit tests, add them to `PH.NlogExtensions.Test/LogFileUnitTest.cs` (and if they are compatible with .NET Framework 4.8, mirror them in the `TestFw48` test project).
- Test configurations are located in `nlog.config` inside the test projects. Ensure any new test target definitions are added there.

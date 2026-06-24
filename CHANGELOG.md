# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.0.7] - 2026-06-24

### Added
- Comprehensive XML documentation comments for all synchronous and asynchronous extension methods.
- Built-in NuGet support for the package's `README.md` file.

### Fixed
- Improved cast safety and NLog wrapper target traversal to safely resolve underlying `FileTarget` instances.
- Refactored active log compression methods for better performance and resource usage.
- Simplified packaging configurations inside `PH.NlogExtensions.csproj`.

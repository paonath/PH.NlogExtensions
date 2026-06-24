#region

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NLog;
using NLog.Targets;
using NLog.Targets.Wrappers;
using PH.CompressionUtility;

#endregion

namespace PH.NlogExtensions
{
    /// <summary>
    ///     Log-File Extensions
    /// </summary>
    public static partial class LogfileExtensions
    {
        #region ZIP

        /// <summary>
        /// Asynchronously gets the entire log directory and compresses it into a ZIP archive, returning it as a byte array.
        /// </summary>
        /// <param name="nLogger">The NLog logger instance.</param>
        /// <param name="memberName">The caller member name.</param>
        /// <param name="filePath">The caller file path.</param>
        /// <param name="lineNo">The caller line number.</param>
        /// <returns>A task representing the asynchronous operation, containing the ZIP archive as a byte array.</returns>
        public static async Task<byte[]> GetWholeLogDirectoryAsZipAsync(
            this Logger nLogger, [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNo = 0)
        {
            using (var memory = await nLogger.GetWholeLogDirectoryZipAsStreamAsync(memberName, filePath, lineNo))
            {
                return memory.ToArray();
            }
        }

        /// <summary>
        /// Asynchronously gets the entire log directory and compresses it into a ZIP archive, returning it as a memory stream.
        /// </summary>
        /// <param name="nLogger">The NLog logger instance.</param>
        /// <param name="memberName">The caller member name.</param>
        /// <param name="filePath">The caller file path.</param>
        /// <param name="lineNo">The caller line number.</param>
        /// <returns>A task representing the asynchronous operation, containing the ZIP archive as a <see cref="MemoryStream"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="nLogger"/> is null.</exception>
        public static async Task<MemoryStream> GetWholeLogDirectoryZipAsStreamAsync(this Logger nLogger,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNo = 0)
        {
            if (nLogger is null)
            {
                throw new ArgumentNullException(nameof(nLogger));
            }

            var memory = new MemoryStream();
            var config = LogManager.Configuration;
            if (config != null)
            {
                var d = new Dictionary<string, DirectoryInfo>();
                var processed = new HashSet<FileTarget>();
                foreach (var configurationAllTarget in config.AllTargets)
                {
                    var fileTarget = GetFileTarget(configurationAllTarget);
                    if (fileTarget != null && processed.Add(fileTarget))
                    {
                        var file = GetLogFileByTarget(fileTarget);
                        if (null != file.Directory && file.Directory.Exists)
                        {
                            if (!d.ContainsKey(file.Directory.FullName))
                            {
                                d.Add(file.Directory.FullName, file.Directory);
                            }
                        }
                    }
                }

                var zipStream = await d.Select(x => x.Value).ToZipStreamAsync(CancellationToken.None);
                zipStream.Position = 0;
                await zipStream.CopyToAsync(memory);
                memory.Position = 0;
            }

            return memory;
        }

        /// <summary>
        /// Asynchronously gets the current log files and compresses them into a ZIP archive, returning it as a byte array.
        /// </summary>
        /// <param name="nLogger">The NLog logger instance.</param>
        /// <param name="token">The cancellation token.</param>
        /// <param name="memberName">The caller member name.</param>
        /// <param name="filePath">The caller file path.</param>
        /// <param name="lineNo">The caller line number.</param>
        /// <returns>A task representing the asynchronous operation, containing the ZIP archive as a byte array.</returns>
        public static async Task<byte[]> GetCurrentLogFilesAsZipAsync(this Logger nLogger, CancellationToken token,
                                                                      [CallerMemberName] string memberName = "",
                                                                      [CallerFilePath] string filePath = "",
                                                                      [CallerLineNumber] int lineNo = 0)
        {
            using (var m = await GetCurrentLogFilesAsZipMemoryStreamAsync(nLogger, token, memberName, filePath, lineNo))
            {
                return m.ToArray();
            }
        }


        /// <summary>
        /// Asynchronously gets the current log files and compresses them into a ZIP archive, returning it as a memory stream.
        /// </summary>
        /// <param name="nLogger">The NLog logger instance.</param>
        /// <param name="token">The cancellation token.</param>
        /// <param name="memberName">The caller member name.</param>
        /// <param name="filePath">The caller file path.</param>
        /// <param name="lineNo">The caller line number.</param>
        /// <returns>A task representing the asynchronous operation, containing the ZIP archive as a <see cref="MemoryStream"/>.</returns>
        public static async Task<MemoryStream> GetCurrentLogFilesAsZipMemoryStreamAsync(this Logger nLogger,
                                                                                        CancellationToken token, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "",
                                                                                        [CallerLineNumber] int lineNo = 0)
        {
            var logs   = await GetAllCurrentLogFilesWithInfoAsync(nLogger, token);
            var memory = new MemoryStream();

            var zipStream = await logs.Select(x => x.Key).ToZipStreamAsync(token);
            zipStream.Position = 0;
            await zipStream.CopyToAsync(memory);

            return memory;
        }

        #endregion


        /// <summary>
        /// Asynchronously reads the contents of the current log file for a specific target as a string.
        /// </summary>
        /// <param name="nlogLogger">The NLog logger instance.</param>
        /// <param name="targetFileName">The name of the target file.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing the log file contents as a string, or <c>null</c> if empty.</returns>
        public static async Task<string> ReadCurrentLogFileAsync(this Logger nlogLogger, string targetFileName,
                                                                 CancellationToken token)
        {
            var bytes = await GetCurrentLogFileAsync(nlogLogger, targetFileName, token);
            if (bytes.Length == 0)
            {
                return null;
            }

            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Asynchronously gets the contents of the current log file for a specific target as a byte array.
        /// </summary>
        /// <param name="nlogLogger">The NLog logger instance.</param>
        /// <param name="targetFileName">The name of the target file.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing the log file contents as a byte array.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="nlogLogger"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="targetFileName"/> is null or empty, or the target could not be found, or is not a FileTarget.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the NLog configuration is not initialized.</exception>
        public static async Task<byte[]> GetCurrentLogFileAsync(this Logger nlogLogger, string targetFileName,
                                                                CancellationToken token)
        {
            if (nlogLogger is null)
            {
                throw new ArgumentNullException(nameof(nlogLogger));
            }

            if (string.IsNullOrEmpty(targetFileName) || string.IsNullOrWhiteSpace(targetFileName))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(targetFileName));
            }

            var config = LogManager.Configuration;
            if (config is null)
            {
                throw new InvalidOperationException("NLog configuration is not initialized.");
            }

            var target = config.FindTargetByName(targetFileName);
            if (target is null)
            {
                nlogLogger?.Trace("Not found target with name {TargetFileName}: begin throw new ArgumentException",
                                  targetFileName);
                throw new ArgumentException($"Not found target with name '{targetFileName}'",
                                            nameof(targetFileName));
            }

            var fileTarget = GetFileTarget(target);
            if (fileTarget is null)
            {
                nlogLogger?.Trace("Target with name {TargetFileName} is not a FileTarget or does not wrap a FileTarget: begin throw new ArgumentException",
                                  targetFileName);
                throw new ArgumentException($"Target with name '{targetFileName}' is not a FileTarget or does not wrap a FileTarget",
                                            nameof(targetFileName));
            }

            return await GetCurrentLogFileByFileTargetAsync(nlogLogger, fileTarget, token);
        }

        /// <summary>
        /// Resolves the <see cref="FileInfo"/> for the specified file target.
        /// </summary>
        /// <param name="fileTarget">The file target.</param>
        /// <returns>A <see cref="FileInfo"/> object for the target's log file.</returns>
        private static FileInfo GetLogFileByTargetAsync(FileTarget fileTarget)
        {
            var getInfo  = new LogEventInfo { TimeStamp = DateTime.UtcNow, Level = LogLevel.Off };
            var fileName = fileTarget.FileName.Render(getInfo);

           
            //var info = new FileInfo(path);
            var info = new FileInfo(fileName);
            return info;
        }

        /// <summary>
        /// Asynchronously gets the current log file contents for the specified file target as a byte array.
        /// </summary>
        /// <param name="nlogLogger">The NLog logger instance.</param>
        /// <param name="fileTarget">The file target.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing the log file contents as a byte array.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="nlogLogger"/> or <paramref name="fileTarget"/> is null.</exception>
        public static async Task<byte[]> GetCurrentLogFileByFileTargetAsync(
            this Logger nlogLogger, FileTarget fileTarget,
            CancellationToken token)
        {
            var r = await GetCurrentDataAndFileInfoByFileTargetAsync(nlogLogger, fileTarget, token);
            return r.Data;
        }


        /// <summary>
        /// Gets the current data and file info for a specific file target asynchronous.
        /// </summary>
        /// <param name="nlogLogger">The NLog logger instance.</param>
        /// <param name="fileTarget">The file target to read from.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>A tuple containing the file contents as a byte array and its <see cref="FileInfo"/>.</returns>
        private static async Task<(byte[] Data, FileInfo File)> GetCurrentDataAndFileInfoByFileTargetAsync(
            Logger nlogLogger, FileTarget fileTarget, CancellationToken token)
        {
            if (nlogLogger is null)
            {
                throw new ArgumentNullException(nameof(nlogLogger));
            }

            if (fileTarget is null)
            {
                throw new ArgumentNullException(nameof(fileTarget));
            }


            var file = GetLogFileByTargetAsync(fileTarget);
            if (!file.Exists)
            {
                return (Array.Empty<byte>(), file);
            }

            using (var stream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var m = new MemoryStream())
                {
                    await stream.CopyToAsync(m, 81920, token);
                    m.Position = 0;
                    return (m.ToArray(), file);
                }
            }
        }

        /// <summary>
        /// Asynchronously gets all current log files, mapped by their file names, as byte arrays.
        /// </summary>
        /// <param name="nlogLogger">The NLog logger instance.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing a dictionary of file names to log file contents.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="nlogLogger"/> is null.</exception>
        public static async Task<Dictionary<string, byte[]>> GetAllCurrentLogFilesAsync(
            this Logger nlogLogger, CancellationToken token)
        {
            var d = new Dictionary<string, byte[]>();
            var r = await GetAllCurrentLogFilesWithInfoAsync(nlogLogger, token);
            foreach (var keyValuePair in r)
            {
                d.Add(keyValuePair.Key.Name, keyValuePair.Value);
            }

            return d;
        }


        /// <summary>
        /// Asynchronously gets all current log files, mapped by their <see cref="FileInfo"/>, as byte arrays.
        /// </summary>
        /// <param name="nlogLogger">The NLog logger instance.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing a dictionary of <see cref="FileInfo"/> to log file contents.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="nlogLogger"/> is null.</exception>
        public static async Task<Dictionary<FileInfo, byte[]>> GetAllCurrentLogFilesWithInfoAsync(
            this Logger nlogLogger, CancellationToken token)
        {
            if (nlogLogger is null)
            {
                throw new ArgumentNullException(nameof(nlogLogger));
            }

            var d = new Dictionary<FileInfo, byte[]>();
            var processed = new HashSet<FileTarget>();
            var config = LogManager.Configuration;
            if (config != null)
            {
                foreach (var configurationAllTarget in config.AllTargets)
                {
                    var fileTarget = GetFileTarget(configurationAllTarget);
                    if (fileTarget != null && processed.Add(fileTarget))
                    {
                        var data = await GetCurrentDataAndFileInfoByFileTargetAsync(nlogLogger, fileTarget, token);
                        d.Add(data.File, data.Data);
                    }
                }
            }

            return d;
        }

        /// <summary>
        /// Resolves the underlying <see cref="FileTarget"/> of a target, traversing any wrappers recursively.
        /// </summary>
        /// <param name="target">The NLog target to resolve.</param>
        /// <returns>The resolved <see cref="FileTarget"/>, or <c>null</c> if it does not wrap a file target.</returns>
        private static FileTarget GetFileTarget(Target target)
        {
            while (target is WrapperTargetBase wrapper)
            {
                target = wrapper.WrappedTarget;
            }
            return target as FileTarget;
        }
    }
}
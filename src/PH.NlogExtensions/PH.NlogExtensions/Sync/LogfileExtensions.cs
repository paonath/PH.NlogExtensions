#region

using System;
using System.Collections.Generic;
using System.IO;
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
        /// Gets the current log files and compresses them into a ZIP archive, returning it as a byte array.
        /// </summary>
        /// <param name="nLogger">The NLog logger instance.</param>
        /// <param name="memberName">The caller member name.</param>
        /// <param name="filePath">The caller file path.</param>
        /// <param name="lineNo">The caller line number.</param>
        /// <returns>The ZIP archive as a byte array.</returns>
        public static byte[] GetCurrentLogFilesAsZip(this Logger nLogger, [CallerMemberName] string memberName = "",
                                                     [CallerFilePath] string filePath = "",
                                                     [CallerLineNumber] int lineNo = 0)
        {
            using (var m = nLogger.GetCurrentLogFilesAsZipMemoryStream(memberName, filePath, lineNo))
            {
                return m.ToArray();
            }
        }


        /// <summary>
        /// Gets the current log files and compresses them into a ZIP archive, returning it as a memory stream.
        /// </summary>
        /// <param name="nLogger">The NLog logger instance.</param>
        /// <param name="memberName">The caller member name.</param>
        /// <param name="filePath">The caller file path.</param>
        /// <param name="lineNo">The caller line number.</param>
        /// <returns>The ZIP archive as a <see cref="MemoryStream"/>.</returns>
        public static MemoryStream GetCurrentLogFilesAsZipMemoryStream(this Logger nLogger,
                                                                       [CallerMemberName] string memberName = "",
                                                                       [CallerFilePath] string filePath = "",
                                                                       [CallerLineNumber] int lineNo = 0)
        {
            var logs   = GetAllCurrentLogFilesWithInfo(nLogger);
            var memory = new MemoryStream();

            var files = logs.Select(x => x.Key).ToArray();
            var zipStream =  files.ToZipStreamAsync(CancellationToken.None).GetAwaiter().GetResult();
            zipStream.Position = 0;
            zipStream.CopyTo(memory);
            memory.Position = 0;
            return memory;

            
        }

        /// <summary>
        /// Gets the entire log directory and compresses it into a ZIP archive, returning it as a byte array.
        /// </summary>
        /// <param name="nLogger">The NLog logger instance.</param>
        /// <param name="memberName">The caller member name.</param>
        /// <param name="filePath">The caller file path.</param>
        /// <param name="lineNo">The caller line number.</param>
        /// <returns>The ZIP archive as a byte array.</returns>
        public static byte[] GetWholeLogDirectoryAsZip(this Logger nLogger, [CallerMemberName] string memberName = "",
                                                       [CallerFilePath] string filePath = "",
                                                       [CallerLineNumber] int lineNo = 0)
        {
            using (var memory = nLogger.GetWholeLogDirectoryZipAsStream(memberName, filePath, lineNo))
            {
                return memory.ToArray();
            }
        }
        /// <summary>
        /// Gets the entire log directory and compresses it into a ZIP archive, returning it as a memory stream.
        /// </summary>
        /// <param name="nLogger">The NLog logger instance.</param>
        /// <param name="memberName">The caller member name.</param>
        /// <param name="filePath">The caller file path.</param>
        /// <param name="lineNo">The caller line number.</param>
        /// <returns>The ZIP archive as a <see cref="MemoryStream"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="nLogger"/> is null.</exception>
        public static MemoryStream GetWholeLogDirectoryZipAsStream(this Logger nLogger,
                                                                   [CallerMemberName] string memberName = "",
                                                                   [CallerFilePath] string filePath = "",
                                                                   [CallerLineNumber] int lineNo = 0)
        {
            var memory = new MemoryStream();

            if (nLogger is null)
            {
                throw new ArgumentNullException(nameof(nLogger));
            }

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

                var zip = d.Select(x => x.Value).ToZipStreamAsync(CancellationToken.None).GetAwaiter().GetResult();
                zip.Position = 0;
                zip.CopyTo(memory);
                memory.Position = 0;
            }
            return memory;
        }

        #endregion


        /// <summary>
        /// Reads the contents of the current log file for a specific target as a string.
        /// </summary>
        /// <param name="nlogLogger">The NLog logger instance.</param>
        /// <param name="targetFileName">The name of the target file.</param>
        /// <returns>The log file contents as a string, or <c>null</c> if empty.</returns>
        public static string ReadCurrentLogFile(this Logger nlogLogger, string targetFileName)
        {
            var bytes = GetCurrentLogFile(nlogLogger, targetFileName);
            if (bytes.Length == 0)
            {
                return null;
            }

            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Gets the contents of the current log file for a specific target as a byte array.
        /// </summary>
        /// <param name="nlogLogger">The NLog logger instance.</param>
        /// <param name="targetFileName">The name of the target file.</param>
        /// <returns>The log file contents as a byte array.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="nlogLogger"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="targetFileName"/> is null or empty, or the target could not be found, or is not a FileTarget.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the NLog configuration is not initialized.</exception>
        public static byte[] GetCurrentLogFile(this Logger nlogLogger, string targetFileName)
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

            return GetCurrentLogFileByFileTarget(nlogLogger, fileTarget);
        }

        /// <summary>
        /// Resolves the <see cref="FileInfo"/> for the specified file target.
        /// </summary>
        /// <param name="fileTarget">The file target.</param>
        /// <returns>A <see cref="FileInfo"/> object for the target's log file.</returns>
        private static FileInfo GetLogFileByTarget(FileTarget fileTarget)
        {
            var getInfo  = new LogEventInfo { TimeStamp = DateTime.UtcNow, Level = LogLevel.Off };
            var fileName = fileTarget.FileName.Render(getInfo);

           

            return new FileInfo(fileName);
        }

        /// <summary>
        /// Gets the current log file contents for the specified file target as a byte array.
        /// </summary>
        /// <param name="nlogLogger">The NLog logger instance.</param>
        /// <param name="fileTarget">The file target.</param>
        /// <returns>The log file contents as a byte array.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="nlogLogger"/> or <paramref name="fileTarget"/> is null.</exception>
        public static byte[] GetCurrentLogFileByFileTarget(this Logger nlogLogger, FileTarget fileTarget)
        {
            var r = GetCurrentDataAndFileInfoByFileTarget(nlogLogger, fileTarget);
            return r.Data;
        }


        /// <summary>
        /// Gets the current data and file info for a specific file target.
        /// </summary>
        /// <param name="nlogLogger">The NLog logger instance.</param>
        /// <param name="fileTarget">The file target to read from.</param>
        /// <returns>A tuple containing the file contents as a byte array and its <see cref="FileInfo"/>.</returns>
        private static (byte[] Data, FileInfo File) GetCurrentDataAndFileInfoByFileTarget(
            Logger nlogLogger, FileTarget fileTarget)
        {
            if (nlogLogger is null)
            {
                throw new ArgumentNullException(nameof(nlogLogger));
            }

            if (fileTarget is null)
            {
                throw new ArgumentNullException(nameof(fileTarget));
            }


            var file = GetLogFileByTarget(fileTarget);
            if (!file.Exists)
            {
                return (Array.Empty<byte>(), file);
            }

            using (var stream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var m = new MemoryStream())
                {
                    stream.CopyTo(m);
                    m.Position = 0;
                    return (m.ToArray(), file);
                }
            }
        }

        /// <summary>
        /// Gets all current log files, mapped by their file names, as byte arrays.
        /// </summary>
        /// <param name="nlogLogger">The NLog logger instance.</param>
        /// <returns>A dictionary of file names to log file contents.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="nlogLogger"/> is null.</exception>
        public static Dictionary<string, byte[]> GetAllCurrentLogFiles(this Logger nlogLogger)
        {
            var d = new Dictionary<string, byte[]>();
            var r = GetAllCurrentLogFilesWithInfo(nlogLogger);
            foreach (var keyValuePair in r)
            {
                d.Add(keyValuePair.Key.Name, keyValuePair.Value);
            }

            return d;
        }


        /// <summary>
        /// Gets all current log files, mapped by their <see cref="FileInfo"/>, as byte arrays.
        /// </summary>
        /// <param name="nlogLogger">The NLog logger instance.</param>
        /// <returns>A dictionary of <see cref="FileInfo"/> to log file contents.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="nlogLogger"/> is null.</exception>
        public static Dictionary<FileInfo, byte[]> GetAllCurrentLogFilesWithInfo(this Logger nlogLogger)
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
                        var data = GetCurrentDataAndFileInfoByFileTarget(nlogLogger, fileTarget);
                        d.Add(data.File, data.Data);
                    }
                }
            }

            return d;
        }
    }
}
#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ionic.Zip;
using Ionic.Zlib;
using NLog;
using NLog.Targets;

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
        /// Gets the whole log directory and zip as byte array asynchronous.
        /// </summary>
        /// <param name="nLogger">The n logger.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="lineNo">The line no.</param>
        /// <returns></returns>
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
        /// Gets the whole log directory and zip as stream asynchronous.
        /// </summary>
        /// <param name="nLogger">The nlog logger.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="lineNo">The line no.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">nLogger</exception>
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
            using (var internalMem = new MemoryStream())
            {
                var d = new Dictionary<string, DirectoryInfo>();
                foreach (var configurationAllTarget in LogManager.Configuration.AllTargets)
                {
                    if (configurationAllTarget is FileTarget fileTarget)
                    {
                        var file = GetLogFileByTarget(fileTarget);
                        if (file.Exists && null != file.Directory && file.Directory.Exists)
                        {
                            if (!d.ContainsKey(file.Directory.FullName))
                            {
                                d.Add(file.Directory.FullName, file.Directory);
                            }
                        }
                    }
                }

                using (var zip = new ZipFile())
                {
                    foreach (var dir in d)
                    {
                        zip.AddDirectory(dir.Value.FullName);
                    }

                    zip.CompressionLevel  = CompressionLevel.BestCompression;
                    zip.CompressionMethod = CompressionMethod.BZip2;

                    zip.Comment =
                        $"Logs request from CallerMemberName '{memberName}' at {DateTime.UtcNow:O} UTC - CallerFilePath '{filePath}' - LineNumber {lineNo}";

                    zip.Save(internalMem);
                    internalMem.Position = 0;
                    await internalMem.CopyToAsync(memory);
                    memory.Position = 0;
                }
            }


            return memory;
        }

        /// <summary>
        ///     Get the current log file collection as zip archive
        /// </summary>
        /// <param name="nLogger">The configured logger</param>
        /// <param name="token">The cancellation token</param>
        /// <param name="memberName">CallerMemberName</param>
        /// <param name="filePath">CallerFilePath</param>
        /// <param name="lineNo">CallerLineNumber</param>
        /// <returns>Byte Array with zip</returns>
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
        ///     Get the current log file collection as zip archive
        /// </summary>
        /// <param name="nLogger">The configured logger</param>
        /// <param name="token">The cancellation token</param>
        /// <param name="memberName">CallerMemberName</param>
        /// <param name="filePath">CallerFilePath</param>
        /// <param name="lineNo">CallerLineNumber</param>
        /// <returns>MemoryStream with zip</returns>
        public static async Task<MemoryStream> GetCurrentLogFilesAsZipMemoryStreamAsync(this Logger nLogger,
                                                                                        CancellationToken token, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "",
                                                                                        [CallerLineNumber] int lineNo = 0)
        {
            var logs   = await GetAllCurrentLogFilesWithInfoAsync(nLogger, token);
            var memory = new MemoryStream();

            using (var zip = new ZipFile())
            {
                foreach (var keyValuePair in logs)
                {
                    var eName = keyValuePair.Key.Name;
                    if (!eName.EndsWith(keyValuePair.Key.Extension, StringComparison.InvariantCultureIgnoreCase))
                    {
                        eName = $"{eName}{keyValuePair.Key.Extension}";
                    }

                    zip.AddEntry(eName, keyValuePair.Value);
                }

                zip.CompressionLevel  = CompressionLevel.BestCompression;
                zip.CompressionMethod = CompressionMethod.BZip2;

                zip.Comment =
                    $"Logs request from CallerMemberName '{memberName}' at {DateTime.UtcNow:O} UTC - CallerFilePath '{filePath}' - LineNumber {lineNo}";

                zip.Save(memory);
                memory.Position = 0;
                return memory;
            }
        }

        #endregion


        /// <summary>Reads the current log file.</summary>
        /// <param name="nlogLogger">The nlog logger.</param>
        /// <param name="targetFileName">Name of the target file.</param>
        /// <param name="token">the cancellation token</param>
        /// <returns></returns>
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

        /// <summary>Gets the current log file.</summary>
        /// <param name="nlogLogger">The nlog logger.</param>
        /// <param name="targetFileName">Name of the target file.</param>
        /// <param name="token">the cancellation token</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">nlogLogger</exception>
        /// <exception cref="ArgumentException">
        ///     Value cannot be null or empty. - targetFileName
        ///     or
        ///     Not found target with name '{targetFileName}' - targetFileName
        /// </exception>
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

            var fileTarget = (FileTarget)LogManager.Configuration.FindTargetByName(targetFileName);
            if (null == fileTarget)
            {
                nlogLogger?.Trace("Not found target with name {TargetFileName}: begin throw new ArgumentException",
                                  targetFileName);
                throw new ArgumentException($"Not found target with name '{targetFileName}'",
                                            nameof(targetFileName));
            }

            return await GetCurrentLogFileByFileTargetAsync(nlogLogger, fileTarget, token);
        }

        /// <summary>Gets the log file by target.</summary>
        /// <param name="fileTarget">The file target.</param>
        /// <returns></returns>
        private static FileInfo GetLogFileByTargetAsync(FileTarget fileTarget)
        {
            var getInfo  = new LogEventInfo { TimeStamp = DateTime.UtcNow, Level = LogLevel.Off };
            var fileName = fileTarget.FileName.Render(getInfo);

            //string path = fileName;
            //if (!System.IO.Path.IsPathRooted(fileName))
            //{
            //    var dir = AppContext.BaseDirectory;
            //    path = Path.Combine(dir, fileName);

            //}

            //var info = new FileInfo(path);
            var info = new FileInfo(fileName);
            return info;
        }

        /// <summary>Gets the current log file by file target.</summary>
        /// <param name="nlogLogger">The nlog logger.</param>
        /// <param name="fileTarget">The file target.</param>
        /// <param name="token">the cancellation token</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        ///     nlogLogger
        ///     or
        ///     fileTarget
        /// </exception>
        public static async Task<byte[]> GetCurrentLogFileByFileTargetAsync(
            this Logger nlogLogger, FileTarget fileTarget,
            CancellationToken token)
        {
            var r = await GetCurrentDataAndFileInfoByFileTargetAsync(nlogLogger, fileTarget, token);
            return r.Data;
        }


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

        /// <summary>Gets the current log files.</summary>
        /// <param name="nlogLogger">The nlog logger.</param>
        /// <param name="token">the cancellation token</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">nlogLogger</exception>
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
        ///     Get all current log files
        /// </summary>
        /// <param name="nlogLogger">The nlog logger</param>
        /// <param name="token">The cancellation token</param>
        /// <returns>Dictionary with log files</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<Dictionary<FileInfo, byte[]>> GetAllCurrentLogFilesWithInfoAsync(
            this Logger nlogLogger, CancellationToken token)
        {
            if (nlogLogger is null)
            {
                throw new ArgumentNullException(nameof(nlogLogger));
            }

            var d = new Dictionary<FileInfo, byte[]>();
            foreach (var configurationAllTarget in LogManager.Configuration.AllTargets)
            {
                if (configurationAllTarget is FileTarget fileTarget)
                {
                    var data = await GetCurrentDataAndFileInfoByFileTargetAsync(nlogLogger, fileTarget, token);
                    d.Add(data.File, data.Data);
                }
            }

            return d;
        }
    }
}
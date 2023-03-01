#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
        ///     Get the current log file collection as zip archive
        /// </summary>
        /// <param name="nLogger">The configured logger</param>
        /// <param name="memberName">CallerMemberName</param>
        /// <param name="filePath">CallerFilePath</param>
        /// <param name="lineNo">CallerLineNumber</param>
        /// <returns>Byte Array with zip</returns>
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
        ///     Get the current log file collection as zip archive
        /// </summary>
        /// <param name="nLogger">The configured logger</param>
        /// <param name="memberName">CallerMemberName</param>
        /// <param name="filePath">CallerFilePath</param>
        /// <param name="lineNo">CallerLineNumber</param>
        /// <returns>MemoryStream with zip</returns>
        public static MemoryStream GetCurrentLogFilesAsZipMemoryStream(this Logger nLogger,
                                                                       [CallerMemberName] string memberName = "",
                                                                       [CallerFilePath] string filePath = "",
                                                                       [CallerLineNumber] int lineNo = 0)
        {
            var logs   = GetAllCurrentLogFilesWithInfo(nLogger);
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

        /// <summary>
        /// Gets the whole log directory and zip.
        /// </summary>
        /// <param name="nLogger">The n logger.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="lineNo">The line no.</param>
        /// <returns></returns>
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
        /// Gets the whole log directory and zip as stream.
        /// </summary>
        /// <param name="nLogger">The n logger.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="lineNo">The line no.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">nLogger</exception>
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

                zip.Save(memory);
                memory.Position = 0;
                return memory;
            }
        }

        #endregion


        /// <summary>Reads the current log file.</summary>
        /// <param name="nlogLogger">The nlog logger.</param>
        /// <param name="targetFileName">Name of the target file.</param>
        /// <returns></returns>
        public static string ReadCurrentLogFile(this Logger nlogLogger, string targetFileName)
        {
            var bytes = GetCurrentLogFile(nlogLogger, targetFileName);
            if (bytes.Length == 0)
            {
                return null;
            }

            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        /// <summary>Gets the current log file.</summary>
        /// <param name="nlogLogger">The nlog logger.</param>
        /// <param name="targetFileName">Name of the target file.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">nlogLogger</exception>
        /// <exception cref="ArgumentException">
        ///     Value cannot be null or empty. - targetFileName
        ///     or
        ///     Not found target with name '{targetFileName}' - targetFileName
        /// </exception>
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

            var fileTarget = (FileTarget)LogManager.Configuration.FindTargetByName(targetFileName);
            if (null == fileTarget)
            {
                nlogLogger?.Trace("Not found target with name {TargetFileName}: begin throw new ArgumentException",
                                  targetFileName);
                throw new ArgumentException($"Not found target with name '{targetFileName}'",
                                            nameof(targetFileName));
            }

            return GetCurrentLogFileByFileTarget(nlogLogger, fileTarget);
        }

        /// <summary>Gets the log file by target.</summary>
        /// <param name="fileTarget">The file target.</param>
        /// <returns></returns>
        private static FileInfo GetLogFileByTarget(FileTarget fileTarget)
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

            return new FileInfo(fileName);
        }

        /// <summary>Gets the current log file by file target.</summary>
        /// <param name="nlogLogger">The nlog logger.</param>
        /// <param name="fileTarget">The file target.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        ///     nlogLogger
        ///     or
        ///     fileTarget
        /// </exception>
        public static byte[] GetCurrentLogFileByFileTarget(this Logger nlogLogger, FileTarget fileTarget)
        {
            var r = GetCurrentDataAndFileInfoByFileTarget(nlogLogger, fileTarget);
            return r.Data;
        }


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

        /// <summary>Gets the current log files.</summary>
        /// <param name="nlogLogger">The nlog logger.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">nlogLogger</exception>
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
        ///     Get all current log files
        /// </summary>
        /// <param name="nlogLogger">The nlog logger</param>
        /// <returns>Dictionary with log files</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Dictionary<FileInfo, byte[]> GetAllCurrentLogFilesWithInfo(this Logger nlogLogger)
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
                    var data = GetCurrentDataAndFileInfoByFileTarget(nlogLogger, fileTarget);
                    d.Add(data.File, data.Data);
                }
            }

            return d;
        }
    }
}
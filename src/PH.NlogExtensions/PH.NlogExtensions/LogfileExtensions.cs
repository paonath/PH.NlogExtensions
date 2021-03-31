using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using NLog;
using NLog.Targets;

namespace PH.NlogExtensions
{
    /// <summary>
    /// Log-File Extensions
    /// </summary>
    public static class LogfileExtensions
    {



        /// <summary>Reads the current log file.</summary>
        /// <param name="nlogLogger">The nlog logger.</param>
        /// <param name="targetFileName">Name of the target file.</param>
        /// <returns></returns>
        public static async Task<string> ReadCurrentLogFile(this NLog.Logger nlogLogger, string targetFileName)
        {
            var bytes = await GetCurrentLogFile(nlogLogger, targetFileName);
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
        /// Value cannot be null or empty. - targetFileName
        /// or
        /// Not found target with name '{targetFileName}' - targetFileName
        /// </exception>
        public static async Task<byte[]> GetCurrentLogFile(this NLog.Logger nlogLogger, string targetFileName)
        {
            if (nlogLogger is null)
            {
                throw new ArgumentNullException(nameof(nlogLogger));
            }

            if (string.IsNullOrEmpty(targetFileName) || string.IsNullOrWhiteSpace(targetFileName))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(targetFileName));
            }

            var fileTarget = (FileTarget) LogManager.Configuration.FindTargetByName(targetFileName);
            if (null == fileTarget)
            {
                nlogLogger?.Trace("Not found target with name {TargetFileName}: begin throw new ArgumentException",
                                  targetFileName);
                throw new ArgumentException($"Not found target with name '{targetFileName}'",
                                            nameof(targetFileName));
            }

            return await GetCurrentLogFileByFileTarget(nlogLogger, fileTarget);

        }

        /// <summary>Gets the log file by target.</summary>
        /// <param name="fileTarget">The file target.</param>
        /// <returns></returns>
        private static FileInfo GetLogFileByTarget(FileTarget fileTarget)
        {
            var    getInfo  = new LogEventInfo() {TimeStamp = DateTime.UtcNow, Level = NLog.LogLevel.Off};
            string fileName = fileTarget.FileName.Render(getInfo);

            string path = fileName;
            if (!System.IO.Path.IsPathRooted(fileName))
            {
                var dir = AppContext.BaseDirectory;
                path = Path.Combine(dir, fileName);

            }

            var info = new FileInfo(path);
            return info;
        }

        /// <summary>Gets the current log file by file target.</summary>
        /// <param name="nlogLogger">The nlog logger.</param>
        /// <param name="fileTarget">The file target.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// nlogLogger
        /// or
        /// fileTarget
        /// </exception>
        public static async Task<byte[]> GetCurrentLogFileByFileTarget(this NLog.Logger nlogLogger, FileTarget fileTarget)
        {
            var r = await GetCurrentDataAndFileInfoByFileTarget(nlogLogger, fileTarget);
            return r.Data;
        }

        private static async Task<(byte[] Data, FileInfo File)> GetCurrentDataAndFileInfoByFileTarget(
            NLog.Logger nlogLogger, FileTarget fileTarget)
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

            using (FileStream stream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var m = new MemoryStream())
                {
                    await stream.CopyToAsync(m);
                    m.Position = 0;
                    return (m.ToArray(), file);
                }


            }
        }

        /// <summary>Gets the current log files.</summary>
        /// <param name="nlogLogger">The nlog logger.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">nlogLogger</exception>
        public static async Task<Dictionary<string,byte[]>> GetAllCurrentLogFiles(this NLog.Logger nlogLogger)
        {
            if (nlogLogger is null)
            {
                throw new ArgumentNullException(nameof(nlogLogger));
            }

            var d = new Dictionary<string, byte[]>();
            foreach (var configurationAllTarget in LogManager.Configuration.AllTargets)
            {
                if (configurationAllTarget is FileTarget fileTarget)
                {
                    var bytes = await GetCurrentLogFileByFileTarget(nlogLogger, fileTarget);
                    d.Add(fileTarget.Name, bytes);
                }
            }

            return d;
        }

        ///// <summary>Gets the current log files as zip.</summary>
        ///// <param name="nlogLogger">The nlog logger.</param>
        ///// <returns></returns>
        ///// <exception cref="ArgumentNullException">nlogLogger</exception>
        //public static async Task<byte[]> GetCurrentLogFilesAsZip(this NLog.Logger nlogLogger)
        //{
        //    if (nlogLogger is null)
        //    {
        //        throw new ArgumentNullException(nameof(nlogLogger));
        //    }


        //    using (var m = new MemoryStream())
        //    {
        //        using (ZipFile zip = new ZipFile())
        //        {
        //            foreach (var configurationAllTarget in LogManager.Configuration.AllTargets)
        //            {
        //                if (configurationAllTarget is FileTarget fileTarget)
        //                {
        //                    var bytes = await GetCurrentDataAndFileInfoByFileTarget(nlogLogger, fileTarget);
        //                    zip.AddEntry(bytes.File.Name, bytes.Data);
        //                }
        //            }

        //            zip.Save(m);
        //            m.Position = 0;
        //            return m.ToArray();
        //        }
        //    }
            
        //}
    }
}


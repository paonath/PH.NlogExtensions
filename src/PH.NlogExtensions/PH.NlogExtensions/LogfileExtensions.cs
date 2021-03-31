using System;
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

            var    getInfo  = new LogEventInfo() {TimeStamp = DateTime.UtcNow, Level = NLog.LogLevel.Off};
            string fileName = fileTarget.FileName.Render(getInfo);

            string path = fileName;
            if (!System.IO.Path.IsPathRooted(fileName))
            {
                var dir = AppContext.BaseDirectory;
                path = Path.Combine(dir, fileName);

            }

            if (!System.IO.File.Exists(path))
            {
                return Array.Empty<byte>();
            }


            using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var m = new MemoryStream())
                {
                    await stream.CopyToAsync(m);
                    m.Position = 0;
                    return m.ToArray();
                }


            }
        }

    }
}


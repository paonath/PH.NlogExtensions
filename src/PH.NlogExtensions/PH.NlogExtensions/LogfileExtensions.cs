using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ionic.Zip;
using Ionic.Zlib;
using NLog;
using NLog.Targets;

namespace PH.NlogExtensions
{
    /// <summary>
    /// Log-File Extensions
    /// </summary>
    public static class LogfileExtensions
    {

	    #region ZIP

	    public static async Task<byte[]> GetCurrentLogFilesAsZip(this NLog.Logger nLogger, CancellationToken token)
	    {
		    using (var m = await GetCurrentLogFilesAsZipMemoryStream(nLogger, token))
		    {
			    return m.ToArray();
		    }
		    
	    }



	    public static async Task<MemoryStream> GetCurrentLogFilesAsZipMemoryStream(this NLog.Logger nLogger,
		    CancellationToken token)
	    {
		    var logs   = await GetAllCurrentLogFilesWithInfo(nLogger, token);
		    var memory = new MemoryStream();
		    
			    using (var zip = new Ionic.Zip.ZipFile())
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
        public static async Task<string> ReadCurrentLogFile(this NLog.Logger nlogLogger, string targetFileName,
                                                            CancellationToken token)
        {
            var bytes = await GetCurrentLogFile(nlogLogger, targetFileName, token);
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
        public static async Task<byte[]> GetCurrentLogFile(this NLog.Logger nlogLogger, string targetFileName,
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

            var fileTarget = (FileTarget) LogManager.Configuration.FindTargetByName(targetFileName);
            if (null == fileTarget)
            {
                nlogLogger?.Trace("Not found target with name {TargetFileName}: begin throw new ArgumentException",
                                  targetFileName);
                throw new ArgumentException($"Not found target with name '{targetFileName}'",
                                            nameof(targetFileName));
            }

            return await GetCurrentLogFileByFileTarget(nlogLogger, fileTarget, token);

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
        public static async Task<byte[]> GetCurrentLogFileByFileTarget(this NLog.Logger nlogLogger, FileTarget fileTarget,
                                                                       CancellationToken token)
        {
            var r = await GetCurrentDataAndFileInfoByFileTarget(nlogLogger, fileTarget, token);
            return r.Data;
        }

				

        private static async Task<(byte[] Data, FileInfo File)> GetCurrentDataAndFileInfoByFileTarget(NLog.Logger nlogLogger, FileTarget fileTarget, CancellationToken token)
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
                    await stream.CopyToAsync(m, 81920, token);
                    m.Position = 0;
                    return (m.ToArray(), file);
                }


            }
        }

        /// <summary>Gets the current log files.</summary>
        /// <param name="nlogLogger">The nlog logger.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">nlogLogger</exception>
        public static async Task<Dictionary<string,byte[]>> GetAllCurrentLogFiles(this NLog.Logger nlogLogger, CancellationToken token)
        {
	        var d = new Dictionary<string, byte[]>();
	        var r = await GetAllCurrentLogFilesWithInfo(nlogLogger, token);
          foreach (var keyValuePair in r)
          {
	          d.Add(keyValuePair.Key.Name, keyValuePair.Value);
          }

          return d;
        }

        public static async Task<Dictionary<FileInfo, byte[]>> GetAllCurrentLogFilesWithInfo(this NLog.Logger nlogLogger, CancellationToken token)
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
			        var data = await GetCurrentDataAndFileInfoByFileTarget(nlogLogger, fileTarget, token);
			        d.Add(data.File, data.Data);
		        }
	        }

	        return d;
        }


    }
}


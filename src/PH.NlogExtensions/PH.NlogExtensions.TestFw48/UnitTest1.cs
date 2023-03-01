using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace PH.NlogExtensions.TestFw48
{
    [TestClass]
    public class UnitTest1
    {
        protected NLog.Logger Logger;

        public UnitTest1()
        {
            NLog.LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration("nlog.config");
            Logger                        = NLog.LogManager.GetCurrentClassLogger();
        }


        [TestMethod]
        public void GetWholeLogDirectory()
        {
            Logger.Info("A message");

            var bytes = Logger.GetWholeLogDirectoryAsZip();
            System.IO.File.WriteAllBytes($@".\lg{DateTime.Now:yymmddHHmmss}.zip", bytes);
            Assert.IsNotNull(bytes);
            Assert.IsTrue(bytes.Length > 0);

        }

        [TestMethod]
        public void GetZipFileReturnByteArray()
        {
                Logger.Info("A message");
                Logger.Debug("a Debug");
                Logger.PushScopeNested($"{Guid.NewGuid()}");
                Logger.Warn("a warn message: i need some data in log files");
                Logger.Error("a simple error");
                try
                {
                    int a     = 0;
                    int r     = 0;
                    int error = a / r;
                }
                catch (Exception e)
                {
                    Logger.Fatal(e, "A Fatal with exception: {Message}", e.Message);
                }

                var d = Logger.GetCurrentLogFilesAsZip();
                
                var zipInfo = new FileInfo($@".{Path.DirectorySeparatorChar}test.zip");
                System.IO.File.WriteAllBytes(zipInfo.FullName, d);

                int     count = 0;
                var archive = System.IO.Compression.ZipFile.Open(zipInfo.FullName, ZipArchiveMode.Read);
                //ZipFile z     = ZipFile.Open(zipInfo.FullName);
                {
                    foreach (var zipEntry in archive.Entries)
                    {
                        count++;

                    }
                }

                Guid g = Guid.NewGuid();
               
            
            Assert.AreEqual(2, count);
            }
    }
}

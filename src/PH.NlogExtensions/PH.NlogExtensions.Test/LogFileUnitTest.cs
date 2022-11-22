using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Ionic.Zip;
using NLog;
using NLog.Targets;
using Xunit;

namespace PH.NlogExtensions.Test
{
    public class LogFileUnitTest : UnitTest
    {
	    CancellationToken token = CancellationToken.None;

	    [Fact]
	    public void GetZipFileReturnByteArray()
	    {
		    Logger.Info("A message");
		    Logger.Debug("a Debug");
		    Logger.PushScopeNested($"{Guid.NewGuid()}");
        Logger.Warn("a warn message: i need some data in log files");
        Logger.Error("a simple error");
        try
        {
	        int a = 0;
	        int r = 0;
	        int error = a / r;
        }
        catch (Exception e)
        {
	        Logger.Fatal(e, "A Fatal with exception: {Message}", e.Message);
        }

		    var               d     = Logger.GetCurrentLogFilesAsZip(token).GetAwaiter().GetResult();
		    var               s     = Logger.GetCurrentLogFilesAsZipMemoryStream(token).GetAwaiter().GetResult();


		    int count = 0;
		    using (ZipFile z = ZipFile.Read(s))
		    {
			    foreach (var zipEntry in z.Entries)
			    {
				    count++;
            
			    }
		    }
        
        Guid g = Guid.NewGuid();
        System.IO.File.WriteAllBytes($".\\{g}.zip", d);

        Assert.NotEmpty(d);

        Assert.Equal(2, count);


	    }




	    [Fact]
        public void CycleOverAllFileTargets()
        {
            Logger.Info("A message");
            var d = Logger.GetAllCurrentLogFiles(token).GetAwaiter().GetResult();

           

            Assert.True(d.Keys.Count == 2);
        }

        [Fact]
        public void TestEmptyLogger()
        {

            var file = new FileInfo(@".\logs/log.log");
            file.Delete();



            var bytes      = Logger.GetCurrentLogFile("full", token).GetAwaiter().GetResult();
            var stringText = Logger.ReadCurrentLogFile("full", token).GetAwaiter().GetResult();
            Assert.True(bytes.Length == 0);
            Assert.True(stringText == null);

        }

        [Fact]
        public void GetCurrentLogFileOnNullLoggerWillTrhowException()
        {
            Logger.Info("A message");
            Logger = null;

            Exception nullFound = null;
            try
            {
                var bytes = Logger.GetCurrentLogFile("full", token).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                nullFound = e;
            }

            Assert.NotNull(nullFound);
        }
        
        [Fact]
        public void GetCurrentLogFileOnTargetemptyWillTrhowException()
        {
            Logger.Info("A message");
            Exception notFound = null;
            try
            {
                var bytes = Logger.GetCurrentLogFile("", token).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                notFound = e;
            }

            Assert.NotNull(notFound);
        }
        [Fact]
        public void GetCurrentLogFileOnTargetNotFoundWillTrhowException()
        {
            Logger.Info("A message");
            Exception notFound = null;
            try
            {
                var bytes = Logger.GetCurrentLogFile("A fake appender target", token).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                notFound = e;
            }

            Assert.NotNull(notFound);
        }

        [Fact]
        public void GetCurrentLogFile()
        {

            Logger.Info("A message");

            var bytes      = Logger.GetCurrentLogFile("full", token).GetAwaiter().GetResult();
            var stringText = Logger.ReadCurrentLogFile("full", token).GetAwaiter().GetResult();


            Assert.NotEmpty(bytes);
            Assert.True(!string.IsNullOrEmpty(stringText));
        }


        [Fact]
        public void CycleOverFileTargets()
        {

        }

    }
}
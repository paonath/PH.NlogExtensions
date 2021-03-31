using System;
using System.Collections.Generic;
using System.IO;
using NLog;
using NLog.Targets;
using Xunit;

namespace PH.NlogExtensions.Test
{
    public class LogFileUnitTest : UnitTest
    {

       
        [Fact]
        public void CycleOverAllFileTargets()
        {
            Logger.Info("A message");
            var d = Logger.GetAllCurrentLogFiles().GetAwaiter().GetResult();

           

            Assert.True(d.Keys.Count == 2);
        }

        [Fact]
        public void TestEmptyLogger()
        {

            var file = new FileInfo(@".\logs/log.log");
            file.Delete();



            var bytes      = Logger.GetCurrentLogFile("full").GetAwaiter().GetResult();
            var stringText = Logger.ReadCurrentLogFile("full").GetAwaiter().GetResult();
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
                var bytes = Logger.GetCurrentLogFile("full").GetAwaiter().GetResult();
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
                var bytes = Logger.GetCurrentLogFile("").GetAwaiter().GetResult();
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
                var bytes = Logger.GetCurrentLogFile("A fake appender target").GetAwaiter().GetResult();
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

            var bytes      = Logger.GetCurrentLogFile("full").GetAwaiter().GetResult();
            var stringText = Logger.ReadCurrentLogFile("full").GetAwaiter().GetResult();


            Assert.NotEmpty(bytes);
            Assert.True(!string.IsNullOrEmpty(stringText));
        }

    }
}
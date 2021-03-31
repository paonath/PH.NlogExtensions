using System;

namespace PH.NlogExtensions.Test
{
    public abstract class UnitTest
    {
        protected NLog.Logger Logger;

        protected UnitTest()
        {
            NLog.LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration("nlog.config");
            Logger                        = NLog.LogManager.GetCurrentClassLogger();
        }
    }
}

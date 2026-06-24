using System;

namespace PH.NlogExtensions.Test
{
    /// <summary>
    /// Base class for unit tests initializing the NLog configuration.
    /// </summary>
    public abstract class UnitTest
    {
        /// <summary>
        /// The NLog logger instance for tests.
        /// </summary>
        protected NLog.Logger Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitTest"/> class and loads the NLog configuration.
        /// </summary>
        protected UnitTest()
        {
            NLog.LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration("nlog.config");
            Logger                        = NLog.LogManager.GetCurrentClassLogger();
        }
    }
}

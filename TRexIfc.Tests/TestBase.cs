using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TRex.Log;
using Xbim.Common.Configuration;

namespace TRex.Tests
{
    public abstract class TestBase<T>
    {
        protected readonly Logger TestLogger;
        
        protected TestBase()
        {
            TestLogger = Logger.ByLogFileName($"{typeof(T).FullName}.log");
            
            if (!XbimServices.Current.IsBuilt)
            {
                XbimServices.Current.ConfigureServices(opt =>
                    opt
                        .AddXbimToolkit(conf => conf.AddGeometryServices())
                        .AddLogging(conf => conf.AddSerilog()));
            }
        }
    }
}

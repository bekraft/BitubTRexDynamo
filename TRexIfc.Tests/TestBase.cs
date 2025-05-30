using Xbim.Common.Configuration;

namespace TRex.Tests
{
    public abstract class TestBase<T>
    {
        protected TestBase()
        {
            if (!XbimServices.Current.IsConfigured)
            {
                XbimServices.Current.ConfigureServices(opt => 
                    opt.AddXbimToolkit(conf => 
                        conf.AddGeometryServices()
                    ));
            }
        }
    }
}

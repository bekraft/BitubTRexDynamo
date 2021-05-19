using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using TRex.Internal;

namespace TRex.Tests
{
    public abstract class TestBase<T>
    {
        //protected readonly ILogger logger = GlobalLogging.loggingFactory.CreateLogger<T>();

        protected TestBase()
        { }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using TRex.Store;
using TRex.Log;
using TRex.Export;

namespace TRex.Tests
{
    public class ModelTestBase<T> : TestBase<T>
    {
        protected IfcModel testSimpleSolid;
        protected readonly Logger testLogger;

        protected ModelTestBase() : base()
        {
            testLogger = Logger.ByLogFileName($"{typeof(T).Name}");
        }

        [SetUp]
        public virtual void SetUp()
        {            
            testSimpleSolid = IfcStore.ByIfcModelFile(
                @$"{TestContext.CurrentContext.TestDirectory}\Resources\extruded-solid.ifc", 
                testLogger, 
                IfcTessellationPrefs.ByDefaults());
        }

        protected ComponentScene BuildComponentScene()
        {
            var sceneBuild = ComponentSceneBuild.BySettingsAndModel(SceneBuildSettings.ByContext("Body"), testSimpleSolid);
            return ComponentSceneBuild.RunBuildComponentScene(sceneBuild);
        }

    }
}

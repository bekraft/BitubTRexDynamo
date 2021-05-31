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
        protected readonly Logger testLogger;

        protected Func<Logger, IfcModel> testSimpleSolid = (testLogger) => IfcStore.ByIfcModelFile(
                @$"{TestContext.CurrentContext.TestDirectory}\Resources\extruded-solid.ifc",
                testLogger,
                IfcTessellationPrefs.ByDefaults());
        protected Func<Logger, IfcModel> testSampleHouse = (testLogger) => IfcStore.ByIfcModelFile(
                @$"{TestContext.CurrentContext.TestDirectory}\Resources\IfcSampleHouse.ifc",
                testLogger,
                IfcTessellationPrefs.ByDefaults());        

        protected ModelTestBase() : base()
        {
            testLogger = Logger.ByLogFileName($"{typeof(T).Name}");
        }

        protected ComponentScene BuildComponentSimpleScene()
        {
            var sceneBuild = ComponentSceneBuild.BySettingsAndModel(SceneBuildSettings.ByContext("Body"), testSimpleSolid(testLogger));
            return ComponentSceneBuild.RunBuildComponentScene(sceneBuild);
        }

        protected ComponentScene BuildComponentSampleScene()
        {
            var sceneBuild = ComponentSceneBuild.BySettingsAndModel(SceneBuildSettings.ByContext("Body"), testSampleHouse(testLogger));
            return ComponentSceneBuild.RunBuildComponentScene(sceneBuild);
        }

    }
}

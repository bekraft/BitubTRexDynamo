using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using TRex.Store;
using TRex.Log;
using TRex.Export;

using Bitub.Dto.Spatial;

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

        protected SceneBuildSettings NewBuildSettings(XYZ offset)
        {
            var settings = SceneBuildSettings.ByContext("Body");
            settings.Preferences.UserModelCenter = offset;
            settings.Preferences.Transforming = Bitub.Xbim.Ifc.Export.SceneTransformationStrategy.Quaternion;
            settings.Preferences.Positioning = Bitub.Xbim.Ifc.Export.ScenePositioningStrategy.UserCorrection;
            return settings;
        }

        protected ComponentScene BuildComponentSimpleScene(XYZ offset)
        {
            var settings = NewBuildSettings(offset);            
            var sceneBuild = ComponentSceneBuild.BySettingsAndModel(settings, testSimpleSolid(testLogger));
            return ComponentSceneBuild.RunBuildComponentScene(sceneBuild);
        }

        protected ComponentScene BuildComponentSampleScene(XYZ offset)
        {
            var settings = NewBuildSettings(offset);
            var sceneBuild = ComponentSceneBuild.BySettingsAndModel(settings, testSampleHouse(testLogger));
            return ComponentSceneBuild.RunBuildComponentScene(sceneBuild);
        }

    }
}

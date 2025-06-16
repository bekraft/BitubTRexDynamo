using System;

using NUnit.Framework;

using TRex.Store;
using TRex.Log;
using TRex.Export;

using Bitub.Dto.Spatial;

namespace TRex.Tests
{
    public class ModelTestBase<T> : TestBase<T>
    {
        protected readonly Func<Logger, IfcModel> TestSimpleSolid = (testLogger) => IfcStore.ByIfcModelFile(
                @$"{TestContext.CurrentContext.TestDirectory}\Resources\extruded-solid.ifc",
                testLogger,
                IfcTessellationPrefs.ByDefaults());
        protected readonly Func<Logger, IfcModel> TestSampleHouse = (testLogger) => IfcStore.ByIfcModelFile(
                @$"{TestContext.CurrentContext.TestDirectory}\Resources\IfcSampleHouse.ifc",
                testLogger,
                IfcTessellationPrefs.ByDefaults());        

        protected ModelTestBase() : base()
        {
        }

        protected SceneBuildSettings NewBuildSettings(XYZ offset)
        {
            var settings = SceneBuildSettings.ByContext("Body");
            settings.Preferences.UserModelCenter = offset;
            settings.Preferences.Transforming = Bitub.Xbim.Ifc.Tesselate.SceneTransformationStrategy.Quaternion;
            settings.Preferences.Positioning = Bitub.Xbim.Ifc.Tesselate.ScenePositioningStrategy.UserCorrection;
            return settings;
        }

        protected ComponentScene BuildComponentSimpleSolidScene(XYZ offset)
        {
            var settings = NewBuildSettings(offset);            
            var sceneBuild = ComponentSceneBuild.BySettingsAndModel(settings, TestSimpleSolid(TestLogger));
            return ComponentSceneBuild.RunBuildComponentScene(sceneBuild);
        }

        protected ComponentScene BuildComponentSampleHouseScene(XYZ offset)
        {
            var settings = NewBuildSettings(offset);
            var sceneBuild = ComponentSceneBuild.BySettingsAndModel(settings, TestSampleHouse(TestLogger));
            return ComponentSceneBuild.RunBuildComponentScene(sceneBuild);
        }

    }
}

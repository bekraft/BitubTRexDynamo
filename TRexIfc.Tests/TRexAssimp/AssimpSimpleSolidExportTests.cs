using System;
using System.Linq;

using NUnit.Framework;

using TRex.Export;
using TRex.Geom;
using TRex.Log;

namespace TRex.Tests.TRexAssimp
{
    public class AssimpSimpleSolidExportTests : ModelTestBase<AssimpSimpleSolidExportTests>
    {
        private ComponentScene _testScene;

        [SetUp]
        public void SetUp()
        {
            _testScene = BuildComponentSampleHouseScene(new Bitub.Dto.Spatial.XYZ());
        }

        [Test]
        public void ExportAs3dsTests()
        {
            try
            {
                var format3ds = ComponentScene.exportAsFormats.FirstOrDefault(f => f.ID == "3ds");
                Assert.That(format3ds, Is.Not.Null, "3DS export module exists");

                var exported = ComponentScene.Export(_testScene, UnitScale.defined["m"], CRSTransform.ByRighthandZUp(), format3ds.Extension, null);
                var log = exported.GetActionLog();
                Assert.That(log.Select(l => l.Severity).All(s => LogSeverity.Info.IsAboveOrEqual(s)), Is.True, "No warnings");
                Assert.That(log.Select(l => l.Reason).All(r => r.HasFlag(LogReason.Saved)), Is.True, "Has been saved successfully");
            }
            catch(Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [Test]
        public void ExportAsFbxTests()
        {
            try
            {
                var formatFbx = ComponentScene.exportAsFormats.FirstOrDefault(f => f.ID == "fbx");
                Assert.That(formatFbx, Is.Not.Null, "FBX export module exists");

                var exported = ComponentScene.Export(_testScene, UnitScale.defined["m"], CRSTransform.ByLefthandYUp(), formatFbx.Extension, null);
                var log = exported.GetActionLog();
                Assert.That(log.Select(l => l.Severity).All(s => LogSeverity.Info.IsAboveOrEqual(s)), Is.True, "No warnings");
                Assert.That(log.Select(l => l.Reason).All(r => r.HasFlag(LogReason.Saved)), Is.True, "Has been saved successfully");
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }
    }
}

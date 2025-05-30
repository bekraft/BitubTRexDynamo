using System;
using System.Linq;
using NUnit.Framework;
using TRex.Export;
using TRex.Geom;
using TRex.Log;

namespace TRex.Tests.TRexAssimp
{
    public class SampleExportAssimp : ModelTestBase<SampleExportAssimp>
    {
        private ComponentScene _testScene;
        
        [SetUp]
        public void SetUp()
        {
            _testScene = BuildComponentSampleScene(new Bitub.Dto.Spatial.XYZ(10, 0, 0));
        }

        [Test]
        public void ExportAs3DS()
        {
            try
            {
                var format3DS = ComponentScene.exportAsFormats.FirstOrDefault(f => f.ID == "3ds");
                Assert.That(format3DS, Is.Not.Null, "3DS export module exists");

                var exported = ComponentScene.Export(_testScene, UnitScale.defined["m"], CRSTransform.ByRighthandZUp(), format3DS.Extension, null);
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
        public void ExportAsFBX()
        {
            try
            {
                var format3DS = ComponentScene.exportAsFormats.FirstOrDefault(f => f.ID == "fbx");
                Assert.That(format3DS, Is.Not.Null, "FBX export module exists");

                var exported = ComponentScene.Export(_testScene, UnitScale.defined["m"], CRSTransform.ByRighthandZUp(), format3DS.Extension, null);
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

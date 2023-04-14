using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TRex.Log;
using TRex.Export;
using TRex.Tests;
using TRex.Geom;

using NUnit.Framework;

namespace TRex.Tests.Export
{
    public class SimpleExportAssimp : ModelTestBase<SimpleExportAssimp>
    {
        private ComponentScene testScene;

        public SimpleExportAssimp() : base()
        {
        }

        [SetUp]
        public void SetUp()
        {
            testScene = BuildComponentSampleScene(new Bitub.Dto.Spatial.XYZ());
        }

        [Test]
        public void ExportAs3DS()
        {
            try
            {
                var format3DS = ComponentScene.exportAsFormats.FirstOrDefault(f => f.ID == "3ds");
                Assert.IsNotNull(format3DS, "3DS export module exists");

                var exported = ComponentScene.Export(testScene, UnitScale.defined["m"], CRSTransform.ByRighthandZUp(), format3DS.Extension, null);
                var log = exported.GetActionLog();
                Assert.IsTrue(log.Select(l => l.Severity).All(s => LogSeverity.Info.IsAboveOrEqual(s)), "No warnings");
                Assert.IsTrue(log.Select(l => l.Reason).All(r => r.HasFlag(LogReason.Saved)), "Has been saved successfully");
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
                Assert.IsNotNull(format3DS, "FBX export module exists");

                var exported = ComponentScene.Export(testScene, UnitScale.defined["m"], CRSTransform.ByLefthandYUp(), format3DS.Extension, null);
                var log = exported.GetActionLog();
                Assert.IsTrue(log.Select(l => l.Severity).All(s => LogSeverity.Info.IsAboveOrEqual(s)), "No warnings");
                Assert.IsTrue(log.Select(l => l.Reason).All(r => r.HasFlag(LogReason.Saved)), "Has been saved successfully");
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }
    }
}

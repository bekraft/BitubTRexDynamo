using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Bitub.Dto.Scene;

using TRex.Tests;

using NUnit.Framework;

namespace TRex.Tests.Export
{
    public class ExportAssimp : TestBase<ExportAssimp>
    {
        private ComponentScene testScene;

        public ExportAssimp() : base()
        {
        }

        [SetUp]
        public void SetUp()
        { 
        }

        [Test]
        public void ExportAs3DS()
        {
            try
            {
                var export = new TRexAssimp.TRexAssimpExport();
                var format3DS = export.Formats.FirstOrDefault(f => f.ID == "3ds");
                Assert.IsNotNull(format3DS);

                var result = export.ExportTo(new ComponentScene(), "test.3ds", format3DS);
                Assert.IsTrue(result);
            }
            catch(Exception e)
            {
                Assert.Fail(e.Message);
            }
        }
    }
}

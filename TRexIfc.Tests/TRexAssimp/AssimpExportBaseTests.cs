using NUnit.Framework;

namespace TRex.Tests.TRexAssimp;

public class AssimpExportBaseTests : TestBase<AssimpExportBaseTests>
{
    [Test]
    public void TestAssimpFormats()
    {
        var formats = global::TRexAssimp.TRexAssimpExport.GetDefaultFormats();
        Assert.That(formats, Is.Not.Null);

        foreach (var f in formats)
        {
            Assert.That(f, Is.Not.Null);
            Assert.That(f.Description, Is.Not.WhiteSpace);
            Assert.That(f.Extension, Is.Not.WhiteSpace);
            Assert.That(f.ID, Is.Not.WhiteSpace);
        }
    }
}
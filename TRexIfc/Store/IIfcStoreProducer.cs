using System.Collections.Generic;

using Autodesk.DesignScript.Runtime;

using Log;

namespace Store
{
#pragma warning disable CS1591

    [IsVisibleInDynamoLibrary(false)]
    public interface IIfcStoreProducer : IEnumerable<IfcStore>
    {
        Logger Logger { get; }
    }

#pragma warning restore CS1591
}
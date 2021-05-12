using System.Runtime.CompilerServices;

using Autodesk.DesignScript.Runtime;

[assembly: InternalsVisibleTo("TRexIfcUI"), InternalsVisibleTo("TRexIfc"), InternalsVisibleTo("TRexAssimp")]

namespace Config
{
    [IsVisibleInDynamoLibrary(false)]
    public class GlobalConfig
    {
        public string PropertyNameSeparator { get; set; } = "::";
    }
}

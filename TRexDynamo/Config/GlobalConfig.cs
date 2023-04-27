using System.Runtime.CompilerServices;

using Autodesk.DesignScript.Runtime;

[assembly: InternalsVisibleTo("TRexIfcUI"), InternalsVisibleTo("TRexIfc"), InternalsVisibleTo("TRexAssimp")]

namespace TRex.Config
{
#pragma warning disable CS1591
    
    [IsVisibleInDynamoLibrary(false)]
    public class GlobalConfig
    {
        public string PropertyNameSeparator { get; set; } = "::";
    }
    
#pragma warning restore CS1591
}

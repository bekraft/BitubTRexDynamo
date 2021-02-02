using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.DesignScript.Runtime;

namespace Config
{
    [IsVisibleInDynamoLibrary(false)]
    public class GlobalConfig
    {
        public string PropertyNameSeparator { get; set; } = "::";
    }
}

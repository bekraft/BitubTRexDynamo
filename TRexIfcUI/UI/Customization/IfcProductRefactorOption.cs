using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using TRex.UI.Model;

using Bitub.Ifc.Transform;

namespace TRex.UI.Customization
{
#pragma warning disable CS1591

    public sealed class IfcProductRefactorOption : OptionLabel<ProductRefactorStrategy>
    {
        [JsonConstructor]
        public IfcProductRefactorOption() : base()
        { }

        public IfcProductRefactorOption(string id, string label, ProductRefactorStrategy data) 
            : base(id, label, data)
        { }
    }

#pragma warning restore CS1591
}

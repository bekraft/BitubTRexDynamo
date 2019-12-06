using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace TRexIfc
{
    public class IfcPropertySetTransform : IfcTransform
    {
        #region Internals

        internal IfcPropertySetTransform()
        {
        }

        #endregion

        public static IfcPropertySetTransform NewExclusionPropertyFilter(string[] removePropertySets)
        {
            return new IfcPropertySetTransform();
        }

        public override IfcRepository Transform(IfcRepository source)
        {
            throw new NotImplementedException();
        }
    }
}

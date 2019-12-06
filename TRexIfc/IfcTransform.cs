using System;
using System.Collections.Generic;
using Xbim.Common;
using Xbim.Common.Metadata;

namespace TRexIfc
{
    public abstract class IfcTransform
    {
        public abstract IfcRepository Transform(IfcRepository source);
    }
}

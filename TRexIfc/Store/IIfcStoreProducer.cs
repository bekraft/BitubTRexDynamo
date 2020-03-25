using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Bitub.Ifc;

using Autodesk.DesignScript.Runtime;

using Log;

#pragma warning disable CS1591 

namespace Store
{
    /// <summary>
    /// An IFC store producer pattern interface
    /// </summary>
    [IsVisibleInDynamoLibrary(false)]
    public interface IIfcStoreProducer : IEnumerator<IfcStore>
    {
        Logger Logger { get; }
    }

    [IsVisibleInDynamoLibrary(false)]
    public class IfcStoreSeq : IEnumerable<IfcStore>
    {
        private IIfcStoreProducer _storeProducer;

        public IfcStoreSeq(IIfcStoreProducer storeProducer)
        {
            _storeProducer = storeProducer;
        }

        public IEnumerator<IfcStore> GetEnumerator()
        {
            return _storeProducer;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _storeProducer;
        }
    }
}

#pragma warning restore CS1591 

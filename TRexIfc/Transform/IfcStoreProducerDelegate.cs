using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.DesignScript.Runtime;

using Bitub.Ifc;

using TRexIfc.Logging;

#pragma warning disable CS1591 

namespace TRexIfc.Transform
{
    [IsVisibleInDynamoLibrary(false)]
    public class IfcStoreProducerDelegate : IIfcStoreProducer
    {
        #region Internals

        private readonly IIfcStoreProducer _source;
        private readonly Func<IfcStore, IfcStore> _function;
        private IfcStore _cIfcStore;

        #endregion

        [IsVisibleInDynamoLibrary(false)]
        public IfcStoreProducerDelegate(IIfcStoreProducer source, Func<IfcStore, IfcStore> f)
        {
            if (null == source)
                throw new ArgumentNullException("source");
            
            _source = source;
            _function = f;
        }

        public IfcStore Current { get => _cIfcStore; }

        public Logger Logger { get => _source.Logger; }

        [IsVisibleInDynamoLibrary(false)]
        object IEnumerator.Current { get => Current; }

        [IsVisibleInDynamoLibrary(false)]
        public void Dispose()
        {
            _source.Dispose();
            _cIfcStore?.XbimModel.Dispose();            
        }

        [IsVisibleInDynamoLibrary(false)]
        public bool MoveNext()
        {
            bool hasNext = false;
            do
            {   // Skip over failures
                if (hasNext)
                    _source.Logger?.LogInfo("Skipped IFC model '{0}' due to delegate failures.", _source.Current.FilePathName);
                hasNext = InternallyMoveNext();
            } while (hasNext && null == _cIfcStore);
            return hasNext;
        }

        private bool InternallyMoveNext()
        {
            if (_source?.MoveNext() ?? false)
            {
                if (null != _cIfcStore)
                    _cIfcStore.XbimModel.Dispose();

                _cIfcStore = _function?.Invoke(_source.Current);
                return true;
            }
            return false;
        }

        [IsVisibleInDynamoLibrary(false)]
        public void Reset()
        {
            _source?.Reset();
            _cIfcStore?.XbimModel.Dispose();
            _cIfcStore = null;
        }
    }
}

#pragma warning restore CS1591 

using System;
using System.Collections;
using System.Collections.Generic;

using Autodesk.DesignScript.Runtime;

using Log;

namespace Store
{
#pragma warning disable CS1591

    [IsVisibleInDynamoLibrary(false)]
    public class IfcStoreProducerDelegate : IIfcStoreProducer
    {
        #region Internals

        private readonly IIfcStoreProducer _source;
        private Func<IfcStore, IfcStore> _function;

        public Logger Logger => throw new NotImplementedException();

        internal class IfcStoreDelegateEnumerator : IEnumerator<IfcStore>
        {
            private readonly IEnumerator<IfcStore> _source;
            private readonly Func<IfcStore, IfcStore> _function;
            private readonly Logger _logger;

            internal IfcStoreDelegateEnumerator(IEnumerator<IfcStore> source, Logger logger, Func<IfcStore, IfcStore> f)
            {
                _source = source;
                _function = f;
                _logger = logger;
            }

            public IfcStore Current { get; private set; }

            object IEnumerator.Current { get => Current; }

            public void Dispose()
            {
                Current?.Dispose();
                Current = null;
            }

            public bool MoveNext()
            {
                bool hasNext = false;
                do
                {   // Skip over failures
                    if (hasNext)
                        _logger.LogInfo("Skipped IFC model '{0}' due to delegate failures.", _source.Current.FilePathName);
                    hasNext = InternallyMoveNext();
                } while (hasNext && null == Current);
                return hasNext;
            }

            private bool InternallyMoveNext()
            {
                if (_source?.MoveNext() ?? false)
                {
                    if (null != Current)
                        Current.Dispose();

                    Current = _function?.Invoke(_source.Current);
                    return true;
                }
                return false;
            }

            public void Reset()
            {
                Current?.Dispose();
                Current = null;

                _source.Reset();
            }
        }

        internal IfcStoreProducerDelegate(IIfcStoreProducer source, Func<IfcStore, IfcStore> f)
        {
            if (null == source)
                throw new ArgumentNullException("source");

            _source = source;
            _function = f;
        }

        public IEnumerator<IfcStore> GetEnumerator()
        {
            return new IfcStoreDelegateEnumerator(_source.GetEnumerator(), _source.Logger, _function);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}

#pragma warning restore CS1591 

using System;
using System.Collections.Generic;

using Bitub.Dto;

using Autodesk.DesignScript.Runtime;

using TRex.Internal;

namespace TRex.Store
{
    /// <summary>
    /// A common model cache.
    /// </summary>
    [IsVisibleInDynamoLibrary(false)]
    public sealed class ModelCache
    {
#pragma warning disable CS1591

        #region Internals

        private static ModelCache instance;
        private Dictionary<Type, Dictionary<Qualifier, object>> cache;

        private ModelCache()
        {
            cache = new Dictionary<Type, Dictionary<Qualifier, object>>();
        }        

        #endregion

        public static ModelCache Instance
        {
            get {
                lock (typeof(ModelCache))
                    return instance ?? (instance = new ModelCache());
            }
        }

        private Dictionary<Qualifier, object> GetOrCreateModelCache<TModel>()
        {
            Dictionary<Qualifier, object> modelCache;
            if (!cache.TryGetValue(typeof(TModel), out modelCache))
                cache.Add(typeof(TModel), modelCache = new Dictionary<Qualifier, object>());
            return modelCache;
        }

        public bool TryGetModel<TModel>(Qualifier qualifier, out TModel model)
        {
            lock (this)
            {
                var modelCache = GetOrCreateModelCache<TModel>();
                object cachedModel;
                if (!modelCache.TryGetValue(qualifier, out cachedModel))
                {
                    model = default(TModel);
                    return false;
                }
                else
                {
                    GlobalLogging.log.Information("Reusing existing {1} model qualifier '{0}'.", qualifier.ToLabel("|"), typeof(TModel).Name);
                    model = (TModel)cachedModel;
                    return true;
                }
            }

        }

        public bool TryGetOrCreateModel<TModel>(Qualifier qualifier, Func<Qualifier, TModel> modelProducer, out TModel model)
        {
            lock (this)
            {
                var modelCache = GetOrCreateModelCache<TModel>();
                object cachedModel;
                if (!modelCache.TryGetValue(qualifier, out cachedModel))
                {
                    modelCache.Add(qualifier, cachedModel = modelProducer(qualifier));
                    model = (TModel)cachedModel;

                    GlobalLogging.log.Information("Registered new {1} model qualifier '{0}'.", qualifier.ToLabel("|"), typeof(TModel).Name);                    
                    return false;
                }
                else
                {
                    GlobalLogging.log.Information("Reusing existing {1} model qualifier '{0}'.", qualifier.ToLabel("|"), typeof(TModel).Name);
                    model = (TModel)cachedModel;
                    return true;
                }                
            }
        }

        public void DropModel<TModel>(Qualifier qualifier)
        {
            lock (this)
            {
                GlobalLogging.log.Information("Dropping {1} model '{0}'.", qualifier.ToLabel("|"), typeof(TModel).Name);
                var modelCache = GetOrCreateModelCache<TModel>();
                modelCache.Remove(qualifier);
            }
        }

        public void ClearCompleteCache()
        {
            lock (this)
            {
                GlobalLogging.log.Information("Clearing model cache completely.");
                cache.Clear();
            }
        }

        public void ClearModelCache<TModel>()
        {
            lock (this)
            {
                GlobalLogging.log.Information("Clearing {0} models from cache.", typeof(TModel).Name);
                cache.Remove(typeof(TModel));
            }
        }

#pragma warning restore CS1591
    }
}

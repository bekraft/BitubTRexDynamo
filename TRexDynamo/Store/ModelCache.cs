using System;
using System.Collections.Generic;

using Bitub.Dto;

using Autodesk.DesignScript.Runtime;

using TRex.Internal;

namespace TRex.Store;

/// <summary>
/// A common model cache.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed class ModelCache
{
    #region Internals
    
    private readonly Dictionary<Type, Dictionary<Qualifier, object>> _cache;

    private ModelCache()
    {
        _cache = new Dictionary<Type, Dictionary<Qualifier, object>>();
    }

    static ModelCache()
    {
        Instance = new ModelCache();
    }

    #endregion
    
    public static readonly ModelCache Instance;

    private Dictionary<Qualifier, object> GetOrCreateModelCache<TModel>()
    {
        if (!_cache.TryGetValue(typeof(TModel), out var modelCache))
            _cache.Add(typeof(TModel), modelCache = new Dictionary<Qualifier, object>());
        return modelCache;
    }

    public bool TryGetModel<TModel>(Qualifier qualifier, out TModel? model)
    {
        lock (this)
        {
            var modelCache = GetOrCreateModelCache<TModel>();
            if (!modelCache.TryGetValue(qualifier, out var cachedModel))
            {
                model = default(TModel);
                return false;
            }
            else
            {
                GlobalLogging.Instance.Log.Information("Reusing existing {1} model qualifier '{0}'.", 
                    qualifier.ToLabel("|"), typeof(TModel).Name);
                model = (TModel)cachedModel;
                return true;
            }
        }

    }

    public bool TryGetOrCreateModel<TModel>(Qualifier qualifier, Func<Qualifier, TModel> modelProducer, out TModel? model)
    {
        lock (this)
        {
            var modelCache = GetOrCreateModelCache<TModel>();
            if (!modelCache.TryGetValue(qualifier, out var cachedModel))
            {
                modelCache.Add(qualifier, cachedModel = modelProducer(qualifier));
                model = (TModel)cachedModel;

                GlobalLogging.Instance.Log.Information("Registered new {TypeName} model qualifier '{Qualifier}'.", 
                    typeof(TModel).Name, qualifier.ToLabel("|"));                    
                return false;
            }
            else
            {
                GlobalLogging.Instance.Log.Information("Reusing existing {TypeName} model qualifier '{Qualifier}'.", 
                    typeof(TModel).Name, qualifier.ToLabel("|"));
                model = (TModel)cachedModel;
                return true;
            }                
        }
    }

    public void DropModel<TModel>(Qualifier qualifier)
    {
        lock (this)
        {
            GlobalLogging.Instance.Log.Information("Dropping {TypeName} model '{Qualifier}'.", 
                typeof(TModel).Name, qualifier.ToLabel("|"));
            var modelCache = GetOrCreateModelCache<TModel>();
            modelCache.Remove(qualifier);
        }
    }

    public void ClearCompleteCache()
    {
        lock (this)
        {
            GlobalLogging.Instance.Log.Information("Clearing model cache completely.");
            _cache.Clear();
        }
    }

    public void ClearModelCache<TModel>()
    {
        lock (this)
        {
            GlobalLogging.Instance.Log.Information("Clearing {0} models from cache.", typeof(TModel).Name);
            _cache.Remove(typeof(TModel));
        }
    }
}
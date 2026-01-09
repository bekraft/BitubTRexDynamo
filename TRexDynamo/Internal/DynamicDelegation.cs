using System;
using System.Collections.Concurrent;

using Autodesk.DesignScript.Runtime;

using Bitub.Dto;
using Serilog.Core;

namespace TRex.Internal;

/// <summary>
/// Invisible helper which should be public since Dynamo has to reach it.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public static class DynamicDelegation
{
    [IsVisibleInDynamoLibrary(false)]
    public static Qualifier Put<T1>(Func<T1, object> f1)
    {
        return PutAnonymous(f1);
    }

    [IsVisibleInDynamoLibrary(false)]
    public static Qualifier Put<T1, R>(string[] named, Func<T1, R> f1)
    {
        return Put<T1, R>(named.ToQualifier(), f1);
    }

    [IsVisibleInDynamoLibrary(false)]
    public static Qualifier BuildQualifier(params string[] names)
    {
        return names.ToQualifier();
    }

    [IsVisibleInDynamoLibrary(false)]
    public static Qualifier Put<T1, R>(Qualifier named, Func<T1, R> f1)
    {            
        FunctionCache.AddOrUpdate(named, f1, (q, f) =>
        {
            Log.Warning("Overwriting key '{Key}' with new function reference.", 
                named.ToLabel());
            return f1;
        });
        return named;
    }

    [IsVisibleInDynamoLibrary(false)]
    public static Qualifier Put<T1,T2>(Func<T1, T2, object> f2)
    {
        return PutAnonymous(f2);
    }

    [IsVisibleInDynamoLibrary(false)]
    public static Qualifier Put<T1, T2, T3>(Func<T1, T2, T3, object> f3)
    {
        return PutAnonymous(f3);
    }

    [IsVisibleInDynamoLibrary(false)]
    public static ProgressingTask? CallDynamicTaskConsumer(Qualifier qualifier, ProgressingTask arg1)
    {
        return InternallyCall<ProgressingTask, ProgressingTask>(qualifier, arg1);
    }

    [IsVisibleInDynamoLibrary(false)]
    public static object? Call(Qualifier qualifier, string arg1)
    {
        return InternallyCall<string, object>(qualifier, arg1);
    }

    [IsVisibleInDynamoLibrary(false)]
    public static object? Call(Qualifier key, double arg1)
    {
        return InternallyCall<double, object>(key, arg1);
    }

    [IsVisibleInDynamoLibrary(false)]
    public static object? Call(Qualifier qualifier, object arg1)
    {
        return InternallyCall<object, object>(qualifier, arg1);
    }

    [IsVisibleInDynamoLibrary(false)]
    public static object? Call(Qualifier qualifier, object arg1, object arg2)
    {
        return InternallyCall<object, object, object>(qualifier, arg1, arg2);
    }

    #region Internals

    private static readonly Logger Log = GlobalLogging.Instance.Log;

    private static readonly ConcurrentDictionary<Qualifier, object> FunctionCache = new ConcurrentDictionary<Qualifier, object>();
    
    private static Qualifier PutAnonymous(object f)
    {
        bool hasAdded;
        Qualifier key;
        do
        {
            key = System.Guid.NewGuid().ToQualifier();
            hasAdded = FunctionCache.TryAdd(key, f);
        } while (!hasAdded);
        return key;
    }

    private static T? Get<T>(Qualifier? qualifier) where T : class
    {
        object? f = null;
        switch(qualifier?.GuidOrNameCase ?? Qualifier.GuidOrNameOneofCase.None)
        {
            case Qualifier.GuidOrNameOneofCase.Anonymous:
                if (!FunctionCache.TryRemove(qualifier!, out f))
                    Log.Error("{Type}: Key '{Key}' is not existing.", typeof(DynamicDelegation), qualifier);
                break;
            case Qualifier.GuidOrNameOneofCase.Named:
                if (!FunctionCache.TryGetValue(qualifier!, out f))
                    Log.Error("{Type}: Key '{Key}' is not existing.", typeof(DynamicDelegation), qualifier);
                break;
            default:
                Log.Warning("{Type}: Key '{Key}' is not valid.", typeof(DynamicDelegation), qualifier);
                break;
        }

        return f as T;
    }

    private static R? InternallyCall<T1, R>(Qualifier qualifier, T1 arg1)
    {
        var f = Get<Func<T1, R>>(qualifier);
        if (null != f)
        {
            return f.Invoke(arg1);
        }
        else
        {
            Log.Warning("No function match found for '{Qualifier}'", qualifier);
            return default(R);
        }
    }

    private static R? InternallyCall<T1, T2, R>(Qualifier qualifier, T1 arg1, T2 arg2)
    {
        var f = Get<Func<T1, T2, R>>(qualifier);
        if (null != f)
        {
            return f.Invoke(arg1, arg2);
        }
        else
        {
            Log.Warning("No function match found for '{Qualifier}'", qualifier);
            return default(R);
        }    
    }

    #endregion
}
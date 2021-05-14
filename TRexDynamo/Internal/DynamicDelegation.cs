using Microsoft.Extensions.Logging;

using System;
using System.Collections.Concurrent;

using Autodesk.DesignScript.Runtime;

using Bitub.Dto;

namespace Internal
{
    /// <summary>
    /// Invisible helper which should be public since Dynamo has to reach it.
    /// </summary>
    [IsVisibleInDynamoLibrary(false)]
    public sealed class DynamicDelegation
    {
#pragma warning disable CS1591

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
                Log.LogWarning("{0}: Key '{1}' already present. Using new function reference.", typeof(DynamicDelegation), named.ToLabel());
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
        public static ProgressingTask CallDynamicTaskConsumer(Qualifier qualifier, ProgressingTask arg1)
        {
            return InternallyCall<ProgressingTask, ProgressingTask>(qualifier, arg1);
        }

        [IsVisibleInDynamoLibrary(false)]
        public static object Call(Qualifier qualifier, string arg1)
        {
            return InternallyCall<string, object>(qualifier, arg1);
        }

        [IsVisibleInDynamoLibrary(false)]
        public static object Call(Qualifier key, double arg1)
        {
            return InternallyCall<double, object>(key, arg1);
        }

        [IsVisibleInDynamoLibrary(false)]
        public static object Call(Qualifier qualifier, object arg1)
        {
            return InternallyCall<object, object>(qualifier, arg1);
        }

        [IsVisibleInDynamoLibrary(false)]
        public static object Call(Qualifier qualifier, object arg1, object arg2)
        {
            return InternallyCall<object, object, object>(qualifier, arg1, arg2);
        }

        #region Internals

        private readonly static ILogger Log = GlobalLogging.loggingFactory.CreateLogger<DynamicDelegation>();

        private readonly static ConcurrentDictionary<Qualifier, object> FunctionCache = new ConcurrentDictionary<Qualifier, object>();

        private DynamicDelegation()
        {
        }

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

        private static T Get<T>(Qualifier qualifier)
        {
            object f = null;
            switch(qualifier.GuidOrNameCase)
            {
                case Qualifier.GuidOrNameOneofCase.Anonymous:
                    if (!FunctionCache.TryRemove(qualifier, out f))
                        Log.LogError("{0}: Key '{1}' is not existing.", typeof(DynamicDelegation), qualifier);
                    break;
                case Qualifier.GuidOrNameOneofCase.Named:
                    if (!FunctionCache.TryGetValue(qualifier, out f))
                        Log.LogError("{0}: Key '{1}' is not existing.", typeof(DynamicDelegation), qualifier);
                    break;
            }

            return (T)f;
        }

        private static R InternallyCall<T1, R>(Qualifier qualifier, T1 arg1)
        {
            var f = Get<Func<T1, R>>(qualifier);
            if (null != f)
            {
                return f.Invoke(arg1);
            }
            else
            {
                Log.LogWarning("No function match found for '{0}'", qualifier);
                return default(R);
            }
        }

        private static R InternallyCall<T1, T2, R>(Qualifier qualifier, T1 arg1, T2 arg2)
        {
            var f = Get<Func<T1, T2, R>>(qualifier);
            if (null != f)
            {
                return f.Invoke(arg1, arg2);
            }
            else
            {
                Log.LogWarning("No function match found for '{0}'", qualifier);
                return default(R);
            }    
        }

        #endregion
    }
    
#pragma warning restore CS1591
}

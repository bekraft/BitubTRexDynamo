using System;

using System.Collections.Concurrent;

using Autodesk.DesignScript.Runtime;

using Bitub.Transfer;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace Internal
{
#pragma warning disable CS1591   

    [IsVisibleInDynamoLibrary(false)]
    public sealed class GlobalDelegationService
    {
        /// <summary>
        /// Registers a simple in => out function.
        /// </summary>
        /// <param name="f1">A function having 1 argument</param>
        /// <returns>Function key</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string Put<T1>(Func<T1,object> f1)
        {
            return JsonFormatter.Default.Format(PutAnonymous(f1));
        }

        /// <summary>
        /// Registers a simple in => out function.
        /// </summary>
        /// <param name="named">The reference name</param>
        /// <param name="f1">A function having 1 argument</param>
        /// <returns>Function key</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string Put<T1>(Qualifier named, Func<T1, object> f1)
        {
            FunctionCache.AddOrUpdate(named, f1, (q, f) => 
            {
                Log.LogWarning("{0}: Key '{1}' already present. Using new function reference.", typeof(GlobalDelegationService), named.ToLabel());
                return f1;
            });
            return JsonFormatter.Default.Format(named);
        }

        /// <summary>
        /// Registers a 2-argument function
        /// </summary>
        /// <param name="f2">Function reference</param>
        /// <returns>Function key</returns>   
        [IsVisibleInDynamoLibrary(false)]
        public static string Put<T1,T2>(Func<T1, T2, object> f2)
        {
            return JsonFormatter.Default.Format(PutAnonymous(f2));
        }

        /// <summary>
        /// Registers a 2-argument function
        /// </summary>
        /// <param name="f3">Function reference</param>
        /// <returns>Function key</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string Put<T1, T2, T3>(Func<T1, T2, T3, object> f3)
        {
            return JsonFormatter.Default.Format(PutAnonymous(f3));
        }

        /// <summary>
        /// Registers a 2-argument function
        /// </summary>
        /// <param name="f4">Function reference</param>
        /// <returns>Function key</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string Put<T1, T2, T3, T4>(Func<T1, T2, T3, T4, object> f4)
        {
            return JsonFormatter.Default.Format(PutAnonymous(f4));
        }

        /// <summary>
        /// Calls a non-static delegate 1-argument function.
        /// </summary>
        /// <param name="key">The function key</param>
        /// <param name="arg1">1st argument</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static object Call(string key, string arg1)
        {
            return InternallyCall(key, arg1);
        }

        /// <summary>
        /// Calls a non-static delegate 1-argument function.
        /// </summary>
        /// <param name="key">The function key</param>
        /// <param name="arg1">1st argument</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static object Call(string key, double arg1)
        {
            return InternallyCall(key, arg1);
        }

        /// <summary>
        /// Calls a non-static delegate 1-argument function.
        /// </summary>
        /// <param name="key">The function key</param>
        /// <param name="arg1">1st argument</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static object Call(string key, object arg1)
        {
            return InternallyCall(key, arg1);
        }

        /// <summary>
        /// Calls a non-static delegate 2-argument function.
        /// </summary>
        /// <param name="key">The function key</param>
        /// <param name="arg1">1st argument</param>
        /// <param name="arg2">2nd argument</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static object Call(string key, object arg1, object arg2)
        {
            return InternallyCall(key, arg1, arg2);
        }

        public static object Call(string key, object arg1, object arg2, object arg3)
        {
            return InternallyCall(key, arg1, arg2, arg3);
        }

        public static object Call(string key, object arg1, object arg2, object arg3, object arg4)
        {
            return InternallyCall(key, arg1, arg2, arg3, arg4);
        }

        #region Internals

        private readonly static ILogger Log = GlobalLogging.LoggingFactory.CreateLogger<GlobalDelegationService>();

        private readonly static ConcurrentDictionary<Qualifier, object> FunctionCache = new ConcurrentDictionary<Qualifier, object>();

        private GlobalDelegationService()
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

        private static T Get<T>(string sQualifier)
        {
            var q = JsonParser.Default.Parse<Qualifier>(sQualifier);
            object f = null;
            switch(q.GuidOrNameCase)
            {
                case Qualifier.GuidOrNameOneofCase.Anonymous:
                    if (!FunctionCache.TryRemove(q, out f))
                        Log.LogError("{0}: Key '{1}' is not existing.", typeof(GlobalDelegationService), q);
                    break;
                case Qualifier.GuidOrNameOneofCase.Named:
                    if (!FunctionCache.TryGetValue(q, out f))
                        Log.LogError("{0}: Key '{1}' is not existing.", typeof(GlobalDelegationService), q);
                    break;
            }

            return (T)f;
        }

        private static object InternallyCall<T1>(string sQualifier, T1 arg1)
        {
            return Get<Func<T1, object>>(sQualifier)?.Invoke(arg1);
        }

        private static object InternallyCall<T1,T2>(string sQualifier, T1 arg1, T2 arg2)
        {
            return Get<Func<T1, T2, object>>(sQualifier)?.Invoke(arg1, arg2);
        }

        private static object InternallyCall<T1, T2, T3>(string sQualifier, T1 arg1, T2 arg2, T3 arg3)
        {
            return Get<Func<T1, T2, T3, object>>(sQualifier)?.Invoke(arg1, arg2, arg3);
        }

        private static object InternallyCall<T1, T2, T3, T4>(string sQualifier, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return Get<Func<T1, T2, T3, T4, object>>(sQualifier)?.Invoke(arg1, arg2, arg3, arg4);
        }

        #endregion
    }
    
#pragma warning restore CS1591
}

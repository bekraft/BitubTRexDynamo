using System;

using System.Collections.Concurrent;

using Autodesk.DesignScript.Runtime;

namespace Internal
{
#pragma warning disable CS1591

    [IsVisibleInDynamoLibrary(false)]
    public sealed class GlobalDelegationService
    {
        private readonly static ConcurrentDictionary<string, object> FunctionCache = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// Registers a simple in => out function.
        /// </summary>
        /// <param name="f1">A function having 1 argument</param>
        /// <returns>Function key</returns>
        public static string Put<T1>(Func<T1, object> f1)
        {
            bool hasAdded;
            string key;
            do
            {
                key = Guid.NewGuid().ToString();
                hasAdded = FunctionCache.TryAdd(key, f1);
            } while (!hasAdded);
            return key;
        }

        /// <summary>
        /// Registers a 2-argument function
        /// </summary>
        /// <param name="f2">Function reference</param>
        /// <returns>Function key</returns>       
        public static string Put<T1,T2>(Func<T1, T2, object> f2)
        {
            bool hasAdded;
            string key;
            do
            {
                key = Guid.NewGuid().ToString();
                hasAdded = FunctionCache.TryAdd(key, f2);
            } while (!hasAdded);
            return key;
        }

        /// <summary>
        /// Registers a 2-argument function
        /// </summary>
        /// <param name="f2">Function reference</param>
        /// <returns>Function key</returns>
        public static string Put<T1, T2, T3>(Func<T1, T2, T3, object> f2)
        {
            bool hasAdded;
            string key;
            do
            {
                key = Guid.NewGuid().ToString();
                hasAdded = FunctionCache.TryAdd(key, f2);
            } while (!hasAdded);
            return key;
        }

        /// <summary>
        /// Registers a 2-argument function
        /// </summary>
        /// <param name="f2">Function reference</param>
        /// <returns>Function key</returns>        
        public static string Put<T1, T2, T3, T4>(Func<T1, T2, T3, T4, object> f2)
        {
            bool hasAdded;
            string key;
            do
            {
                key = Guid.NewGuid().ToString();
                hasAdded = FunctionCache.TryAdd(key, f2);
            } while (!hasAdded);
            return key;
        }

        /// <summary>
        /// Calls a non-static delegate 1-argument function.
        /// </summary>
        /// <param name="key">The function key</param>
        /// <param name="arg1">1st argument</param>
        /// <returns></returns>
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

        private GlobalDelegationService()
        {
        }

        internal static object InternallyCall<T1>(string key, T1 arg1)
        {
            object f;
            if (!FunctionCache.TryRemove(key, out f))
                throw new NotImplementedException();

            var r = (f as Func<T1, object>)?.Invoke(arg1);
            return r;
        }

        internal static object InternallyCall<T1,T2>(string key, T1 arg1, T2 arg2)
        {
            object f;
            if (!FunctionCache.TryRemove(key, out f))
                throw new NotImplementedException();

            var r = (f as Func<T1, T2, object>)?.Invoke(arg1, arg2);
            return r;
        }

        internal static object InternallyCall<T1, T2, T3>(string key, T1 arg1, T2 arg2, T3 arg3)
        {
            object f;
            if (!FunctionCache.TryRemove(key, out f))
                throw new NotImplementedException();

            var r = (f as Func<T1, T2, T3, object>)?.Invoke(arg1, arg2, arg3);
            return r;
        }

        internal static object InternallyCall<T1, T2, T3, T4>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            object f;
            if (!FunctionCache.TryRemove(key, out f))
                throw new NotImplementedException();

            var r = (f as Func<T1, T2, T3, T4, object>)?.Invoke(arg1, arg2, arg3, arg4);
            return r;
        }

        #endregion
    }

#pragma warning restore CS1591
}

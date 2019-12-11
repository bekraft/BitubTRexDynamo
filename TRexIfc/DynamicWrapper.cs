using Autodesk.DesignScript.Runtime;
using System;
using System.Collections.Concurrent;

namespace TRexIfc
{
    /// <summary>
    /// A dynamic wrapper utility as workaround for calling non-static
    /// method delegates in Dynamo AST
    /// </summary>
    public class DynamicWrapper
    {
        private static ConcurrentDictionary<string, object> Registry = new ConcurrentDictionary<string, object>(); 

        private DynamicWrapper()
        {
        }

        /// <summary>
        /// Registers a simple in => out function.
        /// </summary>
        /// <param name="f1">A function having 1 argument</param>
        /// <returns>Function key</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string Register<T1>(Func<T1, object> f1)
        {
            bool hasAdded;
            string key;
            do
            {
                key = Guid.NewGuid().ToString();
                hasAdded = Registry.TryAdd(key, f1);
            } while (!hasAdded);
            return key;
        }

        /// <summary>
        /// Registers a 2-argument function
        /// </summary>
        /// <param name="f2">Function reference</param>
        /// <returns>Function key</returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string Register<T1,T2>(Func<T1, T2, object> f2)
        {
            bool hasAdded;
            string key;
            do
            {
                key = Guid.NewGuid().ToString();
                hasAdded = Registry.TryAdd(key, f2);
            } while (!hasAdded);
            return key;
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
            return InternallyCall<string>(key, arg1);
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
            return InternallyCall<double>(key, arg1);
        }

        internal static object InternallyCall<T1>(string key, T1 arg1)
        {
            object f;
            if (!Registry.TryRemove(key, out f))
                throw new NotImplementedException();

            var r = (f as Func<T1, object>)?.Invoke(arg1);            
            return r;
        }

        /// <summary>
        /// Calls a non-static delegate 2-argument function.
        /// </summary>
        /// <param name="key">The function key</param>
        /// <param name="arg1">1st argument</param>
        /// <param name="arg2">2nd argument</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static object Call(string key, string arg1, string arg2)
        {
            return InternallyCall<string, string>(key, arg1, arg2);
        }
        
        internal static object InternallyCall<T1,T2>(string key, T1 arg1, T2 arg2)
        {
            object f;
            if (!Registry.TryRemove(key, out f))
                throw new NotImplementedException();

            var r = (f as Func<T1, T2, object>)?.Invoke(arg1, arg2);
            return r;
        }

    }
}

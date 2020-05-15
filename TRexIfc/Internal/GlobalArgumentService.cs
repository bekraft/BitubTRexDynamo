using Autodesk.DesignScript.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Internal
{
#pragma warning disable CS1591

    [IsVisibleInDynamoLibrary(false)]
    public class GlobalArgumentService
    {
        private static readonly ConcurrentDictionary<string, object[]> ArgumentCache = new ConcurrentDictionary<string, object[]>();

        private GlobalArgumentService()
        {
        }

        public static string PutArguments()
        {
            return Guid.Empty.ToString();
        }

        public static string PutArguments(params object[] args)
        {
            string guid;
            do
            {
                guid = Guid.NewGuid().ToString();
            } while (!ArgumentCache.TryAdd(guid, args));
            return guid;
        }

        public static object[] GetArgs(string guid)
        {
            if (Guid.Empty.ToString().Equals(guid))
                return Array.Empty<object>();

            object[] args = null;
            ArgumentCache.TryRemove(guid, out args);
            return args;
        }

        public static object GetArg(string guid)
        {
            return GetArg<object>(guid);
        }

        public static T GetArg<T>(string guid)
        {
            var args = GetArgs(guid);
            return args.Length > 0 ? (T)args[0] : default(T);
        }

        public static Tuple<T1, T2, T3> GetArgs<T1, T2, T3>(string guid)
        {
            var args = GetArgs(guid);
            if (null != args)
                return new Tuple<T1, T2, T3>((T1)args[0], (T2)args[1], (T3)args[2]);
            return null;
        }
    }

#pragma warning restore CS1591
}

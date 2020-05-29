using Autodesk.DesignScript.Runtime;
using Microsoft.Extensions.Logging;
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
        #region Internals

        private static readonly ILogger Log = GlobalLogging.LoggingFactory.CreateLogger<GlobalArgumentService>();

        private static readonly ConcurrentDictionary<string, object[]> ArgumentCache = new ConcurrentDictionary<string, object[]>();

        private GlobalArgumentService()
        {
        }

        #endregion

        public static object DeserializeEnum(string typeName, string serializedEnum)
        {
            try
            {
                var type = Type.GetType(typeName, true, true);
                return Enum.Parse(type, serializedEnum);
            } 
            catch(Exception e)
            {
                Log.LogError("While deserilising '{0}' of type '{1}': {2}", serializedEnum, typeName, e);
                return null;
            }
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
            if (!ArgumentCache.TryRemove(guid, out args))
                Log.LogWarning("Argument ID '{0}' not found.", guid);
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

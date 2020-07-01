using Autodesk.DesignScript.Runtime;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Internal
{
#pragma warning disable CS1591

    [IsVisibleInDynamoLibrary(false)]
    public class GlobalArgumentService
    {
        #region Internals

        private static readonly ILogger Log = GlobalLogging.LoggingFactory.CreateLogger<GlobalArgumentService>();

        private static readonly ConcurrentDictionary<string, object[]> ArgumentCache = new ConcurrentDictionary<string, object[]>();

        private static readonly IDictionary<string, string> RootNamespaceAssemblyResolver;

        static GlobalArgumentService()
        {
            RootNamespaceAssemblyResolver = new Dictionary<string, string>()
            {
                { "Bitub.Ifc", typeof(Bitub.Ifc.IfcAuthoringMetadata).Assembly.FullName },
                { "Bitub.Transfer", typeof(Bitub.Transfer.CancelableProgress).Assembly.FullName }
            };
        }

        private GlobalArgumentService()
        {
        }

        #endregion

        /// <summary>
        /// Will try to cast a serialized enum as int or string to it's object instance representation.
        /// </summary>
        /// <typeparam name="T">The type of enum</typeparam>
        /// <param name="serializedEnum">The serialized representation</param>
        /// <param name="member">The enum member, if there's a reference</param>
        /// <returns>True, if cast succeeded</returns>
        public static bool TryCastEnum<T>(object serializedEnum, out T member) where T : Enum
        {
            member = default(T);
            bool isCasted = true;
            if (serializedEnum is T a)
                member = a;
            else if (serializedEnum is string s)
                member = (T)Enum.Parse(typeof(T), s);
            else if (serializedEnum is int i)
                member = (T)Enum.ToObject(typeof(T), i);
            else if (serializedEnum is long l)
                member = (T)Enum.ToObject(typeof(T), l);
            else
            {
                if (null != serializedEnum)
                    Log.LogWarning($"Parsing/casting of '{serializedEnum}' (type '{serializedEnum.GetType().Name}') to type '{member.GetType().Name}' failed.");
                isCasted = false;
            }

            return isCasted;
        }

        /// <summary>
        /// Decomposes an object by its properties.
        /// </summary>
        /// <param name="dataArray">The data</param>
        /// <param name="takeCount">The limiting count</param>
        /// <returns>An enumerable of same length of property values</returns>
        public static object[][] Decompose(object[] dataArray, int takeCount = int.MaxValue)
        {
            return dataArray?.Take(takeCount).Select(data =>
            {
                var props = data.GetType().GetProperties();
                if (props.Length > 0)
                    return props
                        .Where(p => p.CanRead)
                        .Select(p =>
                        {
                            var obj = p.GetValue(data);
                            if (obj is Enum e)
                                return e.ToString();
                            else
                                return obj;
                        })
                        .ToArray();
                else
                    return new object[] { data.ToString() };
            }).ToArray();
        }

        /// <summary>
        /// Will try to parse a given enum serialization of a given type.
        /// </summary>
        /// <param name="typeName">The type name</param>
        /// <param name="serializedEnum">The serialized enum</param>
        /// <returns>Enum instance, if succeeded or null</returns>
        public static object TryParseEnum(string typeName, string serializedEnum)
        {
            try
            {
                var assemblyName = RootNamespaceAssemblyResolver.FirstOrDefault(r => typeName.StartsWith(r.Key)).Value;
                var extendedTypeName = null != assemblyName ? $"{typeName}, {assemblyName}" : typeName;

                var type = Type.GetType(extendedTypeName, true, true);
                return Enum.Parse(type, serializedEnum);
            }
            catch (Exception e)
            {
                Log.LogError($"Parsing '{serializedEnum}' to type '{typeName}' failed with exception: {e.Message}");
                return null;
            }
        }

        public static object[] ExludeBySerializationValue(object[] values, string[] selected, bool ignoreCase)
        {
            var comparer = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return values.Where(v => 0 > Array.FindIndex(selected, s => String.Equals(v?.ToString(), s, comparer))).ToArray();
        }

        public static object[] FilterBySerializationValue(object[] values, string[] selected, bool ignoreCase)
        {
            var comparer = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return values.Where(v => -1 < Array.FindIndex(selected, s => String.Equals(v?.ToString(), s, comparer))).ToArray();
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

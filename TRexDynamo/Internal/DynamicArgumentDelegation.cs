using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Autodesk.DesignScript.Runtime;
using Serilog.Core;

namespace TRex.Internal
{
    
#pragma warning disable CS1591

    [IsVisibleInDynamoLibrary(false)]
    public sealed class DynamicArgumentDelegation
    {
        #region Internals

        private static readonly Logger Log = GlobalLogging.Instance.Log;

        private static readonly ConcurrentDictionary<string, object[]> ArgumentCache = new ConcurrentDictionary<string, object[]>();

        private static readonly IDictionary<string, string> RootNamespaceAssemblyResolver;

        static DynamicArgumentDelegation()
        {
            RootNamespaceAssemblyResolver = new Dictionary<string, string>()
            {
                { "Bitub.Xbim.Ifc", typeof(Bitub.Xbim.Ifc.IfcAuthoringMetadata).Assembly.FullName! }, // Example class name
                { "Bitub.Dto", typeof(Bitub.Dto.ILogging).Assembly.FullName! }  // Example class name
            };
        }

        private DynamicArgumentDelegation()
        {
        }

        #endregion

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

        public static object[]? GetArgs(string guid)
        {
            if (Guid.Empty.ToString().Equals(guid))
                return Array.Empty<object>();

            if (!ArgumentCache.TryRemove(guid, out var args))
                Log.Warning("Argument ID '{Guid}' not found.", guid);
            return args;
        }

        public static object? GetArg(string guid)
        {
            return GetArg<object>(guid);
        }

        public static T? GetArg<T>(string guid)
        {
            var args = GetArgs(guid);
            return (args?.Length ?? 0) > 0 ? (T?)args[0] : default(T);
        }

        public static Tuple<T1, T2, T3>? GetArgs<T1, T2, T3>(string guid)
        {
            var args = GetArgs(guid);
            if (null != args)
                return new Tuple<T1, T2, T3>((T1)args[0], (T2)args[1], (T3)args[2]);
            return null;
        }

#pragma warning restore CS1591

        /// <summary>
        /// Will try to cast a serialized enum as int or string to it's object instance representation.
        /// </summary>
        /// <typeparam name="T">The type of enum</typeparam>
        /// <param name="serializedEnum">The serialized representation</param>
        /// <param name="defaultValue">The default value</param>
        /// <returns>The enum or a default</returns>
        public static T? TryCastEnumOrDefault<T>(object serializedEnum, T? defaultValue = default(T)) where T : Enum
        {
            if (!TryCastEnum(serializedEnum, out T? enumMember))
            {
                Log.Warning("Unable to cast serialized enum '{SerializedEnum}' to type '{Type}'. Using default '{Member}'.", 
                    serializedEnum, nameof(T), enumMember);
                enumMember = defaultValue;
            }
            return enumMember;
        }

        /// <summary>
        /// Will try to cast a serialized enum as int or string to it's object instance representation.
        /// </summary>
        /// <typeparam name="T">The type of enum</typeparam>
        /// <param name="serializedEnum">The serialized representation</param>
        /// <param name="member">The enum member, if there's a reference</param>
        /// <returns>True, if cast succeeded</returns>
        public static bool TryCastEnum<T>(object? serializedEnum, out T? member) where T : Enum
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
                    Log.Warning("Parsing/casting of '{SerializedEnum}' (type '{Type}') to type '{Name}' failed.",
                        serializedEnum, serializedEnum.GetType().Name, member?.GetType().Name);
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
        public static object?[][] DecomposeArray(List<object> dataArray, int takeCount = int.MaxValue)
        {
            return Flatten(dataArray).Take(takeCount).Select(DecomposeObject).ToArray();
        }

        // Single decomposition
        private static object?[] DecomposeObject(object? data)
        {
            var props = data?.GetType().GetProperties();
            if (props?.Length > 0)
                return props
                    .Where(p => p.CanRead)
                    .Select(p =>
                    {
                        var obj = p.GetValue(data);
                        if (!p.PropertyType.IsPrimitive)
                            return obj?.ToString();
                        else
                            return obj;
                    })
                    .ToArray();
            else
                return new object?[] { data?.ToString() };
        }

        // Flatten nested data
        private static object[] Flatten(object data)
        {
            if (data is IEnumerable<object> outer)
                return outer.SelectMany(Flatten).ToArray();
            else
                return new[] { data };
        }

        /// <summary>
        /// Decomposes an object by its properties.
        /// </summary>
        /// <param name="data">The data</param>
        /// <param name="takeCount">The limiting count</param>
        /// <returns>An enumerable of same length of property values</returns>
        public static object?[][] Decompose(object data, int takeCount = int.MaxValue)
        {
            return Flatten(data).Take(takeCount).Select(DecomposeObject).ToArray();
        }


        /// <summary>
        /// Will try to parse a given enum serialization of a given type.
        /// </summary>
        /// <param name="typeName">The type name</param>
        /// <param name="serializedEnum">The serialized enum</param>
        /// <returns>Enum instance, if succeeded or null</returns>
        public static object? TryParseEnum(string typeName, string? serializedEnum)
        {
            if (null == serializedEnum)
            {
                return null;
            }
            else
            {
                try
                {
                    var assemblyName = RootNamespaceAssemblyResolver.FirstOrDefault(r => typeName.StartsWith(r.Key)).Value;
                    var extendedTypeName = null != assemblyName ? $"{typeName}, {assemblyName}" : typeName;

                    var type = Type.GetType(extendedTypeName, true, true);
                    return Enum.Parse(type!, serializedEnum);
                }
                catch (Exception e)
                {
                    Log.Error("Parsing '{SerializedEnum}' to type '{TypeName}' failed with exception: {Message}", 
                        serializedEnum, typeName, e.Message);
                    return null;
                }   
            }
        }

        /// <summary>
        /// Exclude values by serialized representation.
        /// </summary>
        /// <param name="values">Values to be filtered</param>
        /// <param name="selected">Selection</param>
        /// <param name="ignoreCase">Whether to ignore case</param>
        /// <returns>A filtered array</returns>
        public static object[] ExludeBySerializationValue(object[]? values, string[]? selected, bool ignoreCase)
        {
            if (null == selected || null == values)
                return values ?? new object[] { };

            var comparer = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return values.Where(v => 0 > Array.FindIndex(selected, s => String.Equals(v?.ToString(), s, comparer))).ToArray();
        }

        /// <summary>
        /// Select values by serialized representation.
        /// </summary>
        /// <param name="values">Values to be filtered</param>
        /// <param name="selected">Selection</param>
        /// <param name="ignoreCase">Whether to ignore case</param>
        /// <returns>A filtered array</returns>
        public static object[] FilterBySerializationValue(object[]? values, string[]? selected, bool ignoreCase)
        {
            if (null == selected || null == values)
                return new object[] { };

            var comparer = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return values.Where(v => -1 < Array.FindIndex(selected, s => String.Equals(v?.ToString(), s, comparer))).ToArray();
        }
    }
}

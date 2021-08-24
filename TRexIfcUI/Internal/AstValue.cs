using System.Collections.Generic;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;

using System;
using System.Linq;
using Autodesk.DesignScript.Runtime;

namespace TRex.Internal
{
#pragma warning disable CS1591

    [IsVisibleInDynamoLibrary(false)]
    [JsonObject(MemberSerialization.OptIn)]
    public class AstReference
    {
        [JsonProperty]
        public string AstId { get; set; }

        public long[] ArrayIndex { get; set; }

        public override bool Equals(object obj)
        {
            return obj is AstReference reference &&
                   AstId == reference.AstId &&
                   Enumerable.SequenceEqual(ArrayIndex, reference.ArrayIndex);
        }

        public override int GetHashCode()
        {
            int hashCode = -2051493648;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AstId);
            hashCode = hashCode * -1521134295 + EqualityComparer<long[]>.Default.GetHashCode(ArrayIndex);
            return hashCode;
        }

        public bool IsTransient { get => string.IsNullOrWhiteSpace(AstId); }

        public AssociativeNode ToAstNode()
        {
            if (ArrayIndex?.Length > 0)
            {
                AssociativeNode valueNode = AstFactory.BuildIdentifier(AstId, AstFactory.BuildIntNode(ArrayIndex[0]));
                if (ArrayIndex.Length > 1)
                {
                    foreach (var i in ArrayIndex.Skip(1))
                        valueNode = AstFactory.BuildIndexExpression(valueNode, AstFactory.BuildIntNode(i));
                }

                return valueNode;
            }
            else
            {
                return AstFactory.BuildIdentifier(AstId);
            }
        }

        public static bool IsEqualTo<T>(IEnumerable<T> arrA, IEnumerable<T> arrB) where T : AstReference
        {
            return arrB.All(r => arrA.Contains(r)) && arrA.All(r => arrB.Contains(r));
        }
    }

    [IsVisibleInDynamoLibrary(false)]
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AstValue<T> : AstReference
    {        
        public T Value { get; private set; }

        public AstValue(AstValue<T> astValue)
        {
            AstId = astValue.AstId;
            ArrayIndex = astValue.ArrayIndex;
            Value = astValue.Value;
        }

        public AstValue(T value) : this(null, value)
        { }

        public AstValue(string astId, T value, params long[] arrayIndex)
        {
            AstId = astId;
            Value = value;
            ArrayIndex = arrayIndex;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is AstValue<T> value)
            {
                if (string.IsNullOrWhiteSpace(value.AstId) || string.IsNullOrWhiteSpace(AstId))
                    return EqualityComparer<T>.Default.Equals(value.Value, Value);
                else
                    return base.Equals(value);
            }
            return false;
        }

        public bool HasValue { get => null != Value; }

        public override string ToString()
        {
            return Value?.ToString() ?? $"({ArrayIndex}) (null)";
        }

        public static IEnumerable<AstValue<T>> Resolve(IEnumerable<AstReference> astRefs, IEnumerable<AstValue<T>> astValues)
        {
            return astValues.Where(v => astRefs.Contains(v));
        }
    }

#pragma warning restore CS1591
}
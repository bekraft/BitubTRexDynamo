using System.Collections.Generic;
using ProtoCore.AST.AssociativeAST;

using Newtonsoft.Json;
using System;
using System.Linq;
using Autodesk.DesignScript.Runtime;

namespace Internal
{
    // Disable comment warning
#pragma warning disable CS1591

    [IsVisibleInDynamoLibrary(false)]
    public class AstReference
    {
        public string AstId { get; set; }
        public int[] ArrayIndex { get; set; }

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
            hashCode = hashCode * -1521134295 + EqualityComparer<int[]>.Default.GetHashCode(ArrayIndex);
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
    }

    [IsVisibleInDynamoLibrary(false)]
    public sealed class AstValue<T> : AstReference
    {        
        [JsonIgnore]
        public T Value { get; private set; }

        public AstValue(AstValue<T> astValue)
        {
            AstId = astValue.AstId;
            ArrayIndex = astValue.ArrayIndex;
            Value = astValue.Value;
        }

        public AstValue(T value) : this(null, value)
        { }

        public AstValue(string astId, T value, params int[] arrayIndex)
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
                if (string.IsNullOrWhiteSpace(value.AstId))
                    return EqualityComparer<T>.Default.Equals(value.Value, Value);
                else
                    return base.Equals(value);
            }
            return false;
        }

        [JsonIgnore]
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
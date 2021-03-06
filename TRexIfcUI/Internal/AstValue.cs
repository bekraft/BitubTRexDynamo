﻿using System.Collections.Generic;
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
        public int? ArrayIndex { get; set; }

        public override bool Equals(object obj)
        {
            return obj is AstReference value &&
                   AstId == value.AstId &&
                   ArrayIndex == value.ArrayIndex;
        }

        public override int GetHashCode()
        {
            int hashCode = -2051493648;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AstId);
            hashCode = hashCode * -1521134295 + ArrayIndex.GetHashCode();
            return hashCode;
        }

        public AssociativeNode ToAstNode() => ArrayIndex.HasValue ?
            AstFactory.BuildIdentifier(AstId, AstFactory.BuildIntNode(ArrayIndex.Value)) : AstFactory.BuildIdentifier(AstId);
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

        public AstValue(string astId, T value, int? arrayIndex = null)
        {
            AstId = astId;
            Value = value;
            ArrayIndex = arrayIndex;
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
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SyntaxTreeIndex
    {
        public readonly struct AliasInfo
        {
            public readonly string AliasName;
            public readonly int AliasArity;

            public readonly AliasTargetKind TargetKind;

            public readonly string? TargetName;
            public readonly int TargetArity;

            private AliasInfo(string aliasName, int aliasArity, AliasTargetKind targetKind, string? targetName = null, int targetArity = 0)
            {
                Debug.Assert(aliasName != null);
                Debug.Assert(aliasArity >= 0);
                Debug.Assert(Enum.IsDefined(typeof(AliasTargetKind), targetKind));
                Debug.Assert(targetName == null || !string.IsNullOrWhiteSpace(targetName));
                Debug.Assert(targetArity >= 0);

                AliasName = aliasName;
                AliasArity = aliasArity;
                TargetKind = targetKind;
                TargetName = targetName;
                TargetArity = targetArity;
            }

            public static AliasInfo CreateName(string aliasName, int aliasArity, string targetName, int targetArity)
                => new AliasInfo(aliasName, aliasArity, AliasTargetKind.Name, targetName: targetName, targetArity: targetArity);

            public static AliasInfo CreateTuple(string aliasName, int aliasArity, int tupleElementCount)
                => CreateName(aliasName, aliasArity, "ValueTuple", tupleElementCount <= 8 ? tupleElementCount : 8);

            public static AliasInfo CreateTypeParameter(string aliasName, int aliasArity, string targetName)
                => new AliasInfo(aliasName, aliasArity, AliasTargetKind.TypeParameter, targetName: targetName);

            public static AliasInfo CreateDynamic(string aliasName, int aliasArity)
                => new AliasInfo(aliasName, aliasArity, AliasTargetKind.Dynamic);

            public static AliasInfo CreateArray(string aliasName, int aliasArity, int rank)
            {
                Debug.Assert(rank >= 1);
                return new AliasInfo(aliasName, aliasArity, AliasTargetKind.Array, targetArity: rank);
            }

            public static AliasInfo CreatePointer(string aliasName, int aliasArity)
                => new AliasInfo(aliasName, aliasArity, AliasTargetKind.Pointer);

            public static AliasInfo CreateFunctionPointer(string aliasName, int aliasArity, int parameterCount)
                => new AliasInfo(aliasName, aliasArity, AliasTargetKind.FunctionPointer, targetArity: parameterCount);
        }

        public enum AliasTargetKind : byte
        {
            Name,
            TypeParameter,
            Dynamic,
            Array,
            Pointer,
            FunctionPointer
        }
    }
}

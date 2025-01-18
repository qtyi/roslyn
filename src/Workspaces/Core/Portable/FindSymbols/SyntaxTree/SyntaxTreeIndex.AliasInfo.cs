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

            public readonly bool IsGlobal;

            private AliasInfo(string aliasName, int aliasArity, AliasTargetKind targetKind, bool isGlobal) : this(aliasName, aliasArity, targetKind, targetName: null, targetArity: 0, isGlobal) { }

            private AliasInfo(string aliasName, int aliasArity, AliasTargetKind targetKind, string? targetName, bool isGlobal) : this(aliasName, aliasArity, targetKind, targetName, targetArity: 0, isGlobal) { }

            private AliasInfo(string aliasName, int aliasArity, AliasTargetKind targetKind, int targetArity, bool isGlobal) : this(aliasName, aliasArity, targetKind, targetName: null, targetArity, isGlobal) { }

            private AliasInfo(string aliasName, int aliasArity, AliasTargetKind targetKind, string? targetName, int targetArity, bool isGlobal)
            {
                Debug.Assert(aliasName != null);
                Debug.Assert(aliasArity >= 0);
                Debug.Assert(Enum.IsDefined(typeof(AliasTargetKind), targetKind));
                Debug.Assert(targetArity >= 0);

                AliasName = aliasName;
                AliasArity = aliasArity;
                TargetKind = targetKind;
                TargetName = targetName;
                TargetArity = targetArity;
                IsGlobal = isGlobal;
            }

            public static AliasInfo CreateName(string aliasName, int aliasArity, string targetName, int targetArity, bool isGlobal)
                => new AliasInfo(aliasName, aliasArity, AliasTargetKind.Name, targetName, targetArity, isGlobal);

            public static AliasInfo CreateTuple(string aliasName, int aliasArity, int tupleElementCount, bool isGlobal)
                => CreateName(aliasName, aliasArity, targetName: "ValueTuple", targetArity: tupleElementCount <= 8 ? tupleElementCount : 8, isGlobal);

            public static AliasInfo CreateTypeParameter(string aliasName, int aliasArity, string targetName, bool isGlobal)
                => new AliasInfo(aliasName, aliasArity, AliasTargetKind.TypeParameter, targetName, isGlobal);

            public static AliasInfo CreateDynamic(string aliasName, int aliasArity, bool isGlobal)
                => new AliasInfo(aliasName, aliasArity, AliasTargetKind.Dynamic, isGlobal);

            public static AliasInfo CreateArray(string aliasName, int aliasArity, int rank, bool isGlobal)
            {
                Debug.Assert(rank >= 1);
                return new AliasInfo(aliasName, aliasArity, AliasTargetKind.Array, targetArity: rank, isGlobal);
            }

            public static AliasInfo CreatePointer(string aliasName, int aliasArity, bool isGlobal)
                => new AliasInfo(aliasName, aliasArity, AliasTargetKind.Pointer, isGlobal);

            public static AliasInfo CreateFunctionPointer(string aliasName, int aliasArity, int parameterCount, bool isGlobal)
                => new AliasInfo(aliasName, aliasArity, AliasTargetKind.FunctionPointer, targetArity: parameterCount, isGlobal);
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

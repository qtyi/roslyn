// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using AliasConstructCheckResult = object;

#pragma warning disable CS0660

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    partial class TypeSymbol
    {
        private AliasConstructAnnotation _lazyAliasConstructAnnotation;

        public bool HasAliasConstructAnnotation
        {
            [MemberNotNullWhen(true, nameof(_lazyAliasConstructAnnotation))]
            get
            {
                return _lazyAliasConstructAnnotation is not null;
            }
        }

        public TypeSymbol OriginalTypeSymbolWithNoAliasConstructAnnotation
        {
            get
            {
                if (_lazyAliasConstructAnnotation is not null)
                {
                    var type = _lazyAliasConstructAnnotation.OriginalTypeSymbol;
                    Debug.Assert(type._lazyAliasConstructAnnotation is null);
                    return type;
                }

                return this;
            }
        }

        public TypeSymbol WithAliasConstructAnnotation(AliasConstructAnnotation annotation)
        {
            var originalType = OriginalTypeSymbolWithNoAliasConstructAnnotation;
            Debug.Assert(originalType.Equals(annotation.OriginalTypeSymbol), "Cannot annotate this type symbol, original type symbols are not same.");
            var newType = (TypeSymbol)originalType.MemberwiseClone();
            newType._lazyAliasConstructAnnotation = annotation;
            return newType;
        }

        public AliasConstructAnnotation GetAliasConstructAnnotation() => _lazyAliasConstructAnnotation;

        public sealed class AliasConstructAnnotation : IEquatable<AliasConstructAnnotation>
        {
            public readonly TypeSymbol OriginalTypeSymbol;
            public readonly AliasSymbolFromSyntax AliasSymbol;
            public readonly ImmutableArray<TypeWithAnnotations> TypeArguments;
            public readonly SeparatedSyntaxList<TypeSyntax> TypeArgumentsSyntax;

            public static readonly AliasConstructCheckResult Unchecked = new AliasConstructCheckResult();
            public static readonly AliasConstructCheckResult NeedsMoreChecks = new AliasConstructCheckResult();
            public static readonly AliasConstructCheckResult Satisfied = new AliasConstructCheckResult();
            public static readonly AliasConstructCheckResult NotSatisfied = new AliasConstructCheckResult();
            [DebuggerDisplay("{GetCheckResultDebuggerDisplay(), nq}")]
            public AliasConstructCheckResult CheckResult = Unchecked;

            public AliasConstructAnnotation(
                TypeSymbol typeSymbol,
                AliasSymbolFromSyntax aliasSymbol,
                ImmutableArray<TypeWithAnnotations> typeArguments,
                SeparatedSyntaxList<TypeSyntax> typeArgumentsSyntax)
            {
                OriginalTypeSymbol = typeSymbol.OriginalTypeSymbolWithNoAliasConstructAnnotation;
                Debug.Assert(!OriginalTypeSymbol.HasAliasConstructAnnotation);
                AliasSymbol = aliasSymbol;
                TypeArguments = typeArguments;
                TypeArgumentsSyntax = typeArgumentsSyntax;
            }

            public override bool Equals(object? obj) => obj is AliasConstructAnnotation other && Equals(other);

            public bool Equals(AliasConstructAnnotation? other)
            {
                if (other is null)
                {
                    return false;
                }

                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                return AliasSymbol.Equals(other.AliasSymbol) &&
                       TypeArguments.Equals(other.TypeArguments) &&
                       TypeArgumentsSyntax.Equals(other.TypeArgumentsSyntax);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(AliasSymbol.GetHashCode(),
                       Hash.Combine(TypeArguments.GetHashCode(),
                                    TypeArgumentsSyntax.GetHashCode()));
            }

            private string GetCheckResultDebuggerDisplay() => GetCheckResultDebuggerDisplay(CheckResult);

            public static string GetCheckResultDebuggerDisplay(AliasConstructCheckResult checkResult)
            {
                if (checkResult == Unchecked)
                {
                    return nameof(Unchecked);
                }
                else if (checkResult == NeedsMoreChecks)
                {
                    return nameof(NeedsMoreChecks);
                }
                else if (checkResult == Satisfied)
                {
                    return nameof(Satisfied);
                }
                else if (checkResult == NotSatisfied)
                {
                    return nameof(NotSatisfied);
                }

                return "?";
            }
        }
    }
}

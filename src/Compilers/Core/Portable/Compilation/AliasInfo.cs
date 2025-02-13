// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public readonly struct AliasInfo : IEquatable<AliasInfo>
    {
        internal static readonly AliasInfo None = new AliasInfo(alias: null);

        /// <summary>
        /// The alias of the expression represented by the syntax node. For expressions that do not
        /// have a alias, null is returned.
        /// </summary>
        public IAliasSymbol? Alias { get; }

        /// <summary>
        /// The namespace or type that <see cref="Alias"/> targets to. If no alias, null is returned.
        /// </summary>
        public INamespaceOrTypeSymbol? Target { get; }

        internal AliasInfo(IAliasSymbol? alias)
        {
            Debug.Assert(alias is null || alias.Arity == 0, "Use AliasInfo(IAliasSymbol, INamespaceOrTypeSymbol) instead.");

            if (alias is not null)
            {
                Alias = alias;
                Target = alias.Target;
            }
        }

        internal AliasInfo(IAliasSymbol alias, INamespaceOrTypeSymbol target)
        {
            Debug.Assert(alias is not null && target is not null);
            Debug.Assert(alias.Arity != 0 || object.Equals(alias.Target, target), "Use AliasInfo(IAliasSymbol?) instead.");

            Alias = alias;
            Target = target;
        }

        public bool Equals(AliasInfo other)
        {
            return object.Equals(this.Alias, other.Alias)
                && object.Equals(this.Target, other.Target);
        }

        public override bool Equals(object? obj)
        {
            return obj is AliasInfo && this.Equals((AliasInfo)obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Alias, this.Target?.GetHashCode() ?? 0);
        }

        internal static AliasInfo From<TSymbol>(SymbolWithAnnotationSymbols<TSymbol> symbol)
            where TSymbol : ISymbolInternal
        {
            Debug.Assert(!symbol.IsDefault);
            IAliasSymbolInternal? aliasSymbol = symbol.Symbol as IAliasSymbolInternal;
            if (aliasSymbol is null)
            {
                return AliasInfo.None;
            }
            else if (aliasSymbol.IsGenericAlias)
            {
                Debug.Assert(symbol.AnnotationSymbols.Length == 1 && symbol.AnnotationSymbols[0] is INamespaceOrTypeSymbolInternal);
                return new AliasInfo((IAliasSymbol)aliasSymbol.GetISymbol(), (INamespaceOrTypeSymbol)symbol.AnnotationSymbols[0].GetISymbol());
            }
            else
            {
                Debug.Assert(symbol.AnnotationSymbols.IsEmpty);
                return new AliasInfo((IAliasSymbol)aliasSymbol.GetISymbol());
            }
        }
    }
}

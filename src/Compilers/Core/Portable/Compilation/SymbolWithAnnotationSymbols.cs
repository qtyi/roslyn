// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal readonly struct SymbolWithAnnotationSymbols<TSymbol> : IEquatable<SymbolWithAnnotationSymbols<TSymbol>>
        where TSymbol : ISymbolInternal
    {
        /// <summary>
        /// The symbol that was referred to by the syntax node.
        /// </summary>
        public TSymbol Symbol { get; }

        /// <summary>
        /// The symbols which annotate <see cref="Symbol"/> to provide additional information, that was referred to by
        /// the syntax node.
        /// Annotation symbol returns not-empty if and only if:
        ///     1. <see cref="Symbol"/> returns a generic alias symbol - returns its bound target type symbol or, in
        ///     error case, namespace symbol.
        /// </summary>
        public ImmutableArray<TSymbol> AnnotationSymbols { get; }

        public bool IsDefault
        {
            [MemberNotNullWhen(false, nameof(Symbol))]
            get
            {
                return Symbol is null || AnnotationSymbols.IsDefault;
            }
        }

        internal SymbolWithAnnotationSymbols(TSymbol symbol, ImmutableArray<TSymbol> annotationSymbols)
        {
            Symbol = symbol;
            AnnotationSymbols = annotationSymbols;
        }

        public static SymbolWithAnnotationSymbols<TSymbol> Create(TSymbol symbol)
        {
            Debug.Assert(symbol is not IAliasSymbol { IsGenericAlias: true }, "Generic alias symbol must be annotated with namespace or type symbol.");
            return new SymbolWithAnnotationSymbols<TSymbol>(symbol, ImmutableArray<TSymbol>.Empty);
        }

        public static SymbolWithAnnotationSymbols<TSymbol> Create<TAliasSymbol, TNamespaceOrTypeSymbol>(TAliasSymbol aliasSymbol, TNamespaceOrTypeSymbol targetSymbol)
            where TAliasSymbol : TSymbol, IAliasSymbolInternal
            where TNamespaceOrTypeSymbol : TSymbol, INamespaceOrTypeSymbolInternal
        {
            Debug.Assert(aliasSymbol.IsGenericAlias, "Use SymbolWithAnnotations`1[TSymbol].Create(TSymbol) instead.");
            return new SymbolWithAnnotationSymbols<TSymbol>(aliasSymbol, ImmutableArray.Create<TSymbol>(targetSymbol));
        }

        public override int GetHashCode()
            => Hash.Combine(Symbol?.GetHashCode() ?? 0, AnnotationSymbols.GetHashCode());

        public override bool Equals(object? obj)
            => obj is SymbolWithAnnotationSymbols<TSymbol> symbol && Equals(symbol);

        public bool Equals(SymbolWithAnnotationSymbols<TSymbol> other)
            => object.Equals(Symbol, other.Symbol) && AnnotationSymbols.Equals(other.AnnotationSymbols);
    }
}

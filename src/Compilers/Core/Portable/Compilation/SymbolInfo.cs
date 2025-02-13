// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis
{
    public readonly struct SymbolInfo : IEquatable<SymbolInfo>
    {
        internal static readonly SymbolInfo None = default;

        /// <summary>
        /// Array of potential candidate symbols if <see cref="Symbol"/> did not bind successfully.  Note: all code in
        /// this type should prefer referencing <see cref="CandidateSymbols"/> instead of this so that they uniformly
        /// only see an non-<see langword="default"/> array.
        /// </summary>
        private readonly ImmutableArray<ISymbol> _candidateSymbols;

        /// <summary>
        /// The symbol that was referred to by the syntax node, if any. Returns null if the given expression did not
        /// bind successfully to a single symbol. If null is returned, it may still be that case that we have one or
        /// more "best guesses" as to what symbol was intended. These best guesses are available via the <see
        /// cref="CandidateSymbols"/> property.
        /// </summary>
        public ISymbol? Symbol { get; }

        /// <summary>
        /// The symbols which annotate <see cref="Symbol"/> to provide additional information, that was referred to by
        /// the syntax node, if any. Returns empty if the given expression did not bind successfully to a single symbol,
        /// or there is no additional information.
        /// Annotation symbol returns not-empty if and only if:
        ///     1. <see cref="Symbol"/> returns a generic alias symbol - returns its bound target type symbol or, in
        ///     error case, namespace symbol.
        /// </summary>
        public ImmutableArray<ISymbol> AnnotationSymbols { get; }

        /// <summary>
        /// If the expression did not successfully resolve to a symbol, but there were one or more symbols that may have
        /// been considered but discarded, this property returns those symbols. The reason that the symbols did not
        /// successfully resolve to a symbol are available in the <see cref="CandidateReason"/> property. For example,
        /// if the symbol was inaccessible, ambiguous, or used in the wrong context.
        /// </summary>
        /// <remarks>Will never return a <see langword="default"/> array.</remarks>
        public ImmutableArray<ISymbol> CandidateSymbols => _candidateSymbols.NullToEmpty();

        ///<summary>
        /// If the expression did not successfully resolve to a symbol, but there were one or more symbols that may have
        /// been considered but discarded, this property describes why those symbol or symbols were not considered
        /// suitable.
        /// </summary>
        public CandidateReason CandidateReason { get; }

        internal SymbolInfo(ISymbol symbol)
            : this(symbol, annotationSymbols: ImmutableArray<ISymbol>.Empty, ImmutableArray<ISymbol>.Empty, CandidateReason.None)
        {
        }

        internal SymbolInfo(ISymbol symbol, CandidateReason reason)
            : this(symbol, annotationSymbols: ImmutableArray<ISymbol>.Empty, ImmutableArray<ISymbol>.Empty, reason)
        {
        }

        private SymbolInfo(ISymbol symbol, ImmutableArray<ISymbol> annotationSymbols, CandidateReason reason)
            : this(symbol, annotationSymbols, ImmutableArray<ISymbol>.Empty, reason)
        {
        }

        internal SymbolInfo(ImmutableArray<ISymbol> candidateSymbols, CandidateReason candidateReason)
            : this(symbol: null, annotationSymbols: ImmutableArray<ISymbol>.Empty, candidateSymbols, candidateReason)
        {
        }

        private SymbolInfo(ISymbol? symbol, ImmutableArray<ISymbol> annotationSymbols, ImmutableArray<ISymbol> candidateSymbols, CandidateReason candidateReason)
        {
            this.Symbol = symbol;
            this.AnnotationSymbols = annotationSymbols;
            _candidateSymbols = candidateSymbols;

#if DEBUG
            const NamespaceKind NamespaceKindNamespaceGroup = 0;
            Debug.Assert(symbol is null || symbol.Kind != SymbolKind.Namespace || ((INamespaceSymbol)symbol).NamespaceKind != NamespaceKindNamespaceGroup);
            foreach (var item in _candidateSymbols)
            {
                Debug.Assert(item.Kind != SymbolKind.Namespace || ((INamespaceSymbol)item).NamespaceKind != NamespaceKindNamespaceGroup);
            }
#endif

            this.CandidateReason = candidateReason;
        }

        internal ImmutableArray<ISymbol> GetAllSymbols()
            => this.Symbol == null ? CandidateSymbols : ImmutableArray.Create(this.Symbol);

        public override bool Equals(object? obj)
            => obj is SymbolInfo info && Equals(info);

        public bool Equals(SymbolInfo other)
            => this.CandidateReason == other.CandidateReason &&
               object.Equals(this.Symbol, other.Symbol) &&
               CandidateSymbols.SequenceEqual(other.CandidateSymbols);

        public override int GetHashCode()
            => Hash.Combine(this.Symbol, Hash.Combine(Hash.CombineValues(this.CandidateSymbols, 4), (int)this.CandidateReason));

        internal bool IsEmpty => this.Symbol == null && this.CandidateSymbols.Length == 0;

        internal static SymbolInfo From<TSymbol>(SymbolWithAnnotationSymbols<TSymbol> symbol, CandidateReason reason = CandidateReason.None)
            where TSymbol : Microsoft.CodeAnalysis.Symbols.ISymbolInternal
        {
            Debug.Assert(!symbol.IsDefault);
            return new SymbolInfo(symbol.Symbol.GetISymbol(), symbol.AnnotationSymbols.SelectAsArray(static s => s.GetISymbol()), reason);
        }

        internal static SymbolInfo From(AliasInfo aliasInfo)
        {
            var alias = aliasInfo.Alias;
            if (alias is null)
            {
                return SymbolInfo.None;
            }
            else if (alias.Arity == 0)
            {
                return new SymbolInfo(alias);
            }
            else
            {
                Debug.Assert(aliasInfo.Target is not null);
                return new SymbolInfo(alias, ImmutableArray.Create<ISymbol>(aliasInfo.Target), CandidateReason.None);
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal readonly struct SymbolWithAdditionalSymbols
    {
        private readonly Symbol _symbol;
        private readonly ImmutableArray<Symbol> _additionalSymbols;

        public Symbol Symbol => _symbol;
        public ImmutableArray<Symbol> AdditionalSymbols => _additionalSymbols;

        public bool IsDefault => _symbol is null;

        private SymbolWithAdditionalSymbols(Symbol symbol, ImmutableArray<Symbol> additionalSymbols)
        {
            _symbol = symbol;
            _additionalSymbols = additionalSymbols;
        }

        public static SymbolWithAdditionalSymbols FromAlias(Symbol symbol, Symbol targetSymbol)
        {
            Debug.Assert(symbol is AliasSymbol && targetSymbol is TypeSymbol);
            return new SymbolWithAdditionalSymbols(symbol, ImmutableArray.Create(targetSymbol));
        }

        public static SymbolWithAdditionalSymbols FromSymbol(Symbol symbol)
        {
            Debug.Assert(symbol is not null);
            return new SymbolWithAdditionalSymbols(symbol, default);
        }

        public static implicit operator SymbolWithAdditionalSymbols(Symbol symbol)
        {
            return FromSymbol(symbol);
        }

        public static implicit operator SymbolWithAdditionalSymbols(Binder.NamespaceOrTypeOrAliasSymbolWithAnnotations symbol)
        {
            if (symbol.IsAlias)
            {
                AliasSymbol aliasSymbol = (AliasSymbol)symbol.Symbol;
                if (aliasSymbol.Arity == 0)
                {
                    return FromSymbol(aliasSymbol);
                }
                else
                {
                    Debug.Assert(symbol.TypeWithAnnotations.HasType);
                    return FromAlias(aliasSymbol, symbol.TypeWithAnnotations.Type);
                }
            }

            return FromSymbol(symbol.Symbol);
        }
    }
}

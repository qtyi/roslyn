// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public sealed class AliasMap
    {
        private readonly ImmutableDictionary<INamespaceSymbol, IAliasSymbol> _namespaceMap;
        private readonly ImmutableHashSet<IAliasSymbol> _typeSet;

        public static readonly AliasMap Empty = new AliasMap();

        private AliasMap()
        {
            _namespaceMap = ImmutableDictionary<INamespaceSymbol, IAliasSymbol>.Empty;
            _typeSet = ImmutableHashSet<IAliasSymbol>.Empty;
        }

        public AliasMap(ImmutableArray<IAliasSymbol> aliasSymbols)
        {
            var dicBuilder = ImmutableDictionary.CreateBuilder<INamespaceSymbol, IAliasSymbol>();
            var setBuilder = ImmutableHashSet.CreateBuilder<IAliasSymbol>();
            foreach (var aliasSymbol in aliasSymbols)
            {
                INamespaceOrTypeSymbol targetSymbol = aliasSymbol.Target;
                if (targetSymbol is INamespaceSymbol namespaceSymbol)
                {
                    if (dicBuilder.ContainsKey(namespaceSymbol))
                        continue;

                    dicBuilder.Add(namespaceSymbol, aliasSymbol);
                }
                else
                {
                    Debug.Assert(targetSymbol is ITypeSymbol);
                    setBuilder.Add(aliasSymbol);
                }
            }
            _namespaceMap = dicBuilder.ToImmutable();
            _typeSet = setBuilder.ToImmutable();
        }

        public bool TryGetAlias(INamespaceOrTypeSymbol symbol, [NotNullWhen(true)] out IAliasSymbol? aliasSymbol, out ImmutableArray<ITypeSymbol> typeArguments, bool mustResolveAllTypeParameters = true, bool skipTargetAliasTypeParameter = true)
        {
            if (symbol is INamespaceSymbol namespaceSymbol)
            {
                typeArguments = default;
                return _namespaceMap.TryGetValue(namespaceSymbol, out aliasSymbol);
            }
            else if (symbol is ITypeSymbol typeSymbol)
            {
                foreach (IAliasSymbol alias in _typeSet)
                {
                    // In some scenarios, like type simplify or minimal symbol display, a generic alias that targets to its type parameter
                    // will cause loop. Think of this:
                    //     using A<T> = T;
                    // Then `Goo` -> `A<Goo>` -> `A<A<Goo>>` -> ... -> `A<A<...A<Goo>...>>`.
                    // We use a parameter to switch on / off the feature to skip these aliases.
                    if (skipTargetAliasTypeParameter && alias.Target is ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Alias })
                    {
                        continue;
                    }

                    var resultKind = NamespaceOrTypeSymbolDeconstructionVisitor.Deconstruct(symbol, alias, out typeArguments);
                    if (resultKind == NamespaceOrTypeSymbolDeconstructionResultKind.Viable ||
                        (resultKind == NamespaceOrTypeSymbolDeconstructionResultKind.Ambiguous && !mustResolveAllTypeParameters))
                    {
                        aliasSymbol = alias;
                        return true;
                    }
                }
            }

            aliasSymbol = null;
            typeArguments = default;
            return false;
        }
    }
}

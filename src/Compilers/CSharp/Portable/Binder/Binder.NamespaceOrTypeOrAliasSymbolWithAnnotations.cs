// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        internal readonly struct NamespaceOrTypeOrAliasSymbolWithAnnotations
        {
            private readonly TypeWithAnnotations _typeWithAnnotations;
            private readonly Symbol _symbol;
            private readonly bool _isNullableEnabled;

            private NamespaceOrTypeOrAliasSymbolWithAnnotations(TypeWithAnnotations typeWithAnnotations)
            {
                Debug.Assert(typeWithAnnotations.HasType);
                _typeWithAnnotations = typeWithAnnotations;
                _symbol = null;
                _isNullableEnabled = false; // Not meaningful for a TypeWithAnnotations, it already baked the fact into its content.
            }

            private NamespaceOrTypeOrAliasSymbolWithAnnotations(NamespaceSymbol symbol)
            {
                _typeWithAnnotations = default;
                _symbol = symbol;
                _isNullableEnabled = false; // Not meaningful for a TypeWithAnnotations, it already baked the fact into its content.
            }

            private NamespaceOrTypeOrAliasSymbolWithAnnotations(AliasSymbol symbol, bool isNullableEnabled)
            {
                Debug.Assert(symbol.Arity == 0 || symbol.Target.IsNamespace, "Use NamespaceOrTypeOrAliasSymbolWithAnnotations..ctor(AliasSymbol, TypeWithAnnotations, bool) instead.");
                _typeWithAnnotations = default;
                _symbol = symbol;
                _isNullableEnabled = isNullableEnabled;
            }

            private NamespaceOrTypeOrAliasSymbolWithAnnotations(AliasSymbol symbol, TypeWithAnnotations typeWithAnnotations, bool isNullableEnabled)
            {
                Debug.Assert(symbol.Arity > 0 && symbol.Target.IsType, "Use NamespaceOrTypeOrAliasSymbolWithAnnotations..ctor(AliasSymbol, bool) instead.");
                Debug.Assert(typeWithAnnotations.HasType);
                _typeWithAnnotations = typeWithAnnotations;
                _symbol = symbol;
                _isNullableEnabled = isNullableEnabled;
            }

            internal TypeWithAnnotations TypeWithAnnotations => _typeWithAnnotations;
            internal Symbol Symbol => _symbol ?? TypeWithAnnotations.Type;
            internal bool IsType => !IsAlias && !_typeWithAnnotations.IsDefault;
            internal bool IsAlias => _symbol?.Kind == SymbolKind.Alias;
            internal NamespaceOrTypeSymbol NamespaceOrTypeSymbol => Symbol as NamespaceOrTypeSymbol ?? (_typeWithAnnotations.HasType ? _typeWithAnnotations.Type : Alias.Target);
            internal AliasSymbol Alias => Symbol as AliasSymbol;
            internal bool IsDefault => !_typeWithAnnotations.HasType && _symbol is null;

            internal bool IsNullableEnabled
            {
                get
                {
                    Debug.Assert(IsAlias); // Not meaningful to use this property otherwise
                    return _isNullableEnabled;
                }
            }

            internal static NamespaceOrTypeOrAliasSymbolWithAnnotations CreateUnannotated(bool isNullableEnabled, Symbol symbol, TypeWithAnnotations aliasTarget = default)
            {
                switch (symbol)
                {
                    case null:
                        return default;

                    case TypeSymbol typeSymbol:
                        return new NamespaceOrTypeOrAliasSymbolWithAnnotations(TypeWithAnnotations.Create(isNullableEnabled, typeSymbol));

                    case NamespaceSymbol namespaceSymbol:
                        return new NamespaceOrTypeOrAliasSymbolWithAnnotations(namespaceSymbol);

                    case AliasSymbol aliasSymbol:
                        return aliasSymbol.Arity > 0 && aliasSymbol.Target.IsType ?
                            new NamespaceOrTypeOrAliasSymbolWithAnnotations(aliasSymbol, aliasTarget, isNullableEnabled) :
                            new NamespaceOrTypeOrAliasSymbolWithAnnotations(aliasSymbol, isNullableEnabled);

                    default:
                        throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
                }
            }

            internal static NamespaceOrTypeOrAliasSymbolWithAnnotations CreateFromAlias(AliasSymbol aliasSymbol, bool isNullableEnabled = false, TypeWithAnnotations aliasTarget = default)
            {
                if (aliasSymbol is null)
                {
                    return default;
                }
                if (aliasSymbol.Arity == 0)
                {
                    return new NamespaceOrTypeOrAliasSymbolWithAnnotations(aliasSymbol, isNullableEnabled);
                }
                else
                {
                    return new NamespaceOrTypeOrAliasSymbolWithAnnotations(aliasSymbol, aliasTarget, isNullableEnabled);
                }
            }

            public static implicit operator NamespaceOrTypeOrAliasSymbolWithAnnotations(TypeWithAnnotations typeWithAnnotations)
            {
                return new NamespaceOrTypeOrAliasSymbolWithAnnotations(typeWithAnnotations);
            }
        }
    }
}

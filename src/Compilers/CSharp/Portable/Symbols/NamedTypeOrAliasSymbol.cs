// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal readonly struct NamedTypeOrAliasSymbol
    {
        public readonly NamedTypeSymbol NamedTypeSymbol;
        public readonly AliasSymbol AliasSymbol;

        public bool IsDefault => !IsNamedType && !IsAlias;
        public bool IsNamedType => NamedTypeSymbol is not null;
        public bool IsAlias => AliasSymbol is not null;

        public ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                if (IsNamedType)
                {
                    return NamedTypeSymbol.TypeParameters;
                }
                else
                {
                    return AliasSymbol.TypeParameters;
                }
            }
        }
        internal TypeSymbol SelfOrTarget => (NamedTypeSymbol ?? AliasSymbol.Target as TypeSymbol)!;

        public NamedTypeOrAliasSymbol(NamedTypeSymbol namedTypeSymbol) => NamedTypeSymbol = namedTypeSymbol;

        public NamedTypeOrAliasSymbol(AliasSymbol aliasSymbol)
        {
            Debug.Assert(aliasSymbol.Target is TypeSymbol);
            AliasSymbol = aliasSymbol;
        }

        public NamedTypeOrAliasSymbol ConstructIfGeneric(ImmutableArray<TypeWithAnnotations> typeArguments)
        {
            if (IsNamedType)
            {
                return NamedTypeSymbol.ConstructIfGeneric(typeArguments);
            }
            else
            {
                return AliasSymbol.ConstructIfGeneric(typeArguments);
            }
        }

        public static implicit operator NamedTypeOrAliasSymbol(NamedTypeSymbol namedTypeSymbol) => new(namedTypeSymbol);
        public static implicit operator NamedTypeOrAliasSymbol(AliasSymbol aliasSymbol) => new(aliasSymbol);
    }
}

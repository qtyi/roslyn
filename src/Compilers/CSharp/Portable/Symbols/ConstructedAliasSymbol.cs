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
    internal sealed class ConstructedAliasSymbol : SubstitutedAliasSymbol
    {
        private readonly ImmutableArray<TypeWithAnnotations> _typeArgumentsWithAnnotations;
        private readonly AliasSymbol _constructedFrom;

        internal ConstructedAliasSymbol(AliasSymbol constructedFrom, ImmutableArray<TypeWithAnnotations> typeArgumentsWithAnnotations)
            : base(newContainer: constructedFrom.ContainingSymbol,
                   map: new TypeMap(constructedFrom.OriginalDefinition.TypeParameters, typeArgumentsWithAnnotations),
                   originalDefinition: constructedFrom.OriginalDefinition)
        {
            _typeArgumentsWithAnnotations = typeArgumentsWithAnnotations;
            _constructedFrom = constructedFrom;

            Debug.Assert(constructedFrom.Arity == typeArgumentsWithAnnotations.Length);
            Debug.Assert(constructedFrom.Arity != 0);
        }

        public override AliasSymbol ConstructedFrom
        {
            get
            {
                return _constructedFrom;
            }
        }

        internal override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics
        {
            get
            {
                return _typeArgumentsWithAnnotations;
            }
        }
    }
}

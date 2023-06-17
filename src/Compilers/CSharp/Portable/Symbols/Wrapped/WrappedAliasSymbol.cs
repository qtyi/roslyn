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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents an alias that is based on another alias.
    /// When inheriting from this class, one shouldn't assume that 
    /// the default behavior it has is appropriate for every case.
    /// That behavior should be carefully reviewed and derived type
    /// should override behavior as appropriate.
    /// </summary>
    internal abstract class WrappedAliasSymbol : AliasSymbol
    {
        /// <summary>
        /// The underlying AliasSymbol.
        /// </summary>
        protected readonly AliasSymbol _underlyingAlias;

        public WrappedAliasSymbol(AliasSymbol underlyingAlias) : base(underlyingAlias.Name, underlyingAlias.ContainingSymbol, underlyingAlias.Locations, underlyingAlias.IsExtern)
        {
            Debug.Assert((object)underlyingAlias != null);
            _underlyingAlias = underlyingAlias;
        }

        public override AliasSymbol OriginalDefinition => _underlyingAlias;

        public override int Arity => _underlyingAlias.Arity;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => _underlyingAlias.TypeParameters;

        public override NamespaceOrTypeSymbol Target => _underlyingAlias.Target;

        internal override bool RequiresCompletion => _underlyingAlias.RequiresCompletion;

        internal override NamespaceOrTypeSymbol GetAliasTarget(ConsList<TypeSymbol> basesBeingResolved)
        {
            return _underlyingAlias.GetAliasTarget(basesBeingResolved);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class AliasSymbol : Symbol, IAliasSymbol
    {
        private readonly Symbols.AliasSymbol _underlying;

        public AliasSymbol(Symbols.AliasSymbol underlying)
        {
            RoslynDebug.Assert(underlying is object);
            _underlying = underlying;
        }

        internal override CSharp.Symbol UnderlyingSymbol => _underlying;

        int IAliasSymbol.Arity
        {
            get
            {
                return _underlying.Arity;
            }
        }

        INamespaceOrTypeSymbol IAliasSymbol.Target
        {
            get
            {
                return _underlying.Target.GetPublicSymbol();
            }
        }

        bool IAliasSymbol.IsGenericAlias => _underlying.Arity != 0;

        bool IAliasSymbol.IsUnboundGenericAlias => throw new System.NotImplementedException();

        ImmutableArray<ITypeParameterSymbol> IAliasSymbol.TypeParameters => throw new System.NotImplementedException();

        ImmutableArray<ITypeSymbol> IAliasSymbol.TypeArguments => throw new System.NotImplementedException();

        ImmutableArray<CodeAnalysis.NullableAnnotation> IAliasSymbol.TypeArgumentNullableAnnotations => throw new System.NotImplementedException();

        INamedTypeSymbol IAliasSymbol.Construct(params ITypeSymbol[] typeArguments)
        {
            throw new System.NotImplementedException();
        }

        INamedTypeSymbol IAliasSymbol.Construct(ImmutableArray<ITypeSymbol> typeArguments, ImmutableArray<CodeAnalysis.NullableAnnotation> typeArgumentNullableAnnotations)
        {
            throw new System.NotImplementedException();
        }

        ImmutableArray<CustomModifier> IAliasSymbol.GetTypeArgumentCustomModifiers(int ordinal)
        {
            throw new System.NotImplementedException();
        }

        #region ISymbol Members

        protected override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitAlias(this);
        }

        protected override TResult? Accept<TResult>(SymbolVisitor<TResult> visitor)
            where TResult : default
        {
            return visitor.VisitAlias(this);
        }

        protected override TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitAlias(this, argument);
        }

        #endregion
    }
}

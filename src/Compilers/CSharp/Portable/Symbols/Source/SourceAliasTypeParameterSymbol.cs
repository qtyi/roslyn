// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Class for alias type parameters.
    /// </summary>
    internal sealed class SourceAliasTypeParameterSymbol : SourceTypeParameterSymbolBase
    {
        private readonly AliasSymbolFromSyntax _owner;

        public SourceAliasTypeParameterSymbol(AliasSymbolFromSyntax owner, string name, int ordinal, Location location, SyntaxReference syntaxRef)
            : base(name, ordinal, ImmutableArray.Create(location), ImmutableArray.Create(syntaxRef))
        {
            _owner = owner;
        }

        public override TypeParameterKind TypeParameterKind
        {
            get
            {
                return TypeParameterKind.Alias;
            }
        }

        public override Symbol ContainingSymbol
        {
            get { return _owner; }
        }

        public override VarianceKind Variance
        {
            get { return VarianceKind.None; }
        }

        public override bool HasConstructorConstraint
        {
            get
            {
                var constraints = this.GetConstraintKinds();
                return (constraints & TypeParameterConstraintKind.Constructor) != 0;
            }
        }

        public override bool HasValueTypeConstraint
        {
            get
            {
                var constraints = this.GetConstraintKinds();
                return (constraints & TypeParameterConstraintKind.AllValueTypeKinds) != 0;
            }
        }

        public override bool IsValueTypeFromConstraintTypes
        {
            get
            {
                Debug.Assert(!HasValueTypeConstraint);
                var constraints = this.GetConstraintKinds();
                return (constraints & TypeParameterConstraintKind.ValueTypeFromConstraintTypes) != 0;
            }
        }

        public override bool HasReferenceTypeConstraint
        {
            get
            {
                var constraints = this.GetConstraintKinds();
                return (constraints & TypeParameterConstraintKind.ReferenceType) != 0;
            }
        }

        public override bool IsReferenceTypeFromConstraintTypes
        {
            get
            {
                var constraints = this.GetConstraintKinds();
                return (constraints & TypeParameterConstraintKind.ReferenceTypeFromConstraintTypes) != 0;
            }
        }

        internal override bool? ReferenceTypeConstraintIsNullable
        {
            get
            {
                return CalculateReferenceTypeConstraintIsNullable(this.GetConstraintKinds());
            }
        }

        public override bool HasNotNullConstraint
        {
            get
            {
                var constraints = this.GetConstraintKinds();
                return (constraints & TypeParameterConstraintKind.NotNull) != 0;
            }
        }

        internal override bool? IsNotNullable
        {
            get
            {
                if ((this.GetConstraintKinds() & TypeParameterConstraintKind.ObliviousNullabilityIfReferenceType) != 0)
                {
                    return null;
                }

                return CalculateIsNotNullable();
            }
        }

        public override bool HasUnmanagedTypeConstraint
        {
            get
            {
                var constraints = this.GetConstraintKinds();
                return (constraints & TypeParameterConstraintKind.Unmanaged) != 0;
            }
        }

        protected override ImmutableArray<TypeParameterSymbol> ContainerTypeParameters
        {
            get { return _owner.TypeParameters; }
        }

        protected override TypeParameterBounds ResolveBounds(ConsList<TypeParameterSymbol> inProgress, BindingDiagnosticBag diagnostics)
        {
            var constraintTypes = _owner.GetTypeParameterConstraintTypes(this.Ordinal);
            if (constraintTypes.IsEmpty && GetConstraintKinds() == TypeParameterConstraintKind.None)
            {
                return null;
            }

            return this.ResolveBounds(this.ContainingAssembly.CorLibrary, inProgress.Prepend(this), constraintTypes, inherited: false, this.DeclaringCompilation, diagnostics);
        }

        private TypeParameterConstraintKind GetConstraintKinds()
        {
            return _owner.GetTypeParameterConstraintKind(this.Ordinal);
        }
    }
}

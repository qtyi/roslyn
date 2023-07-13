// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents an type that is constructed from an alias.
    /// </summary>
    internal sealed class AliasTargetTypeSymbol : TypeSymbol
    {
        private readonly AliasSymbolFromSyntax _constructedFrom;
        private readonly ImmutableArray<TypeWithAnnotations> _typeArgumentsWithAnnotations;
        private readonly TypeMap _map;

        private readonly TypeSymbol _underlyingType;

        internal AliasTargetTypeSymbol(AliasSymbolFromSyntax constructedFrom)
            : this(constructedFrom, ImmutableArray<TypeWithAnnotations>.Empty) { }

        internal AliasTargetTypeSymbol(AliasSymbolFromSyntax constructedFrom, ImmutableArray<TypeWithAnnotations> typeArgumentsWithAnnotations)
        {
            Debug.Assert(constructedFrom.Target is TypeSymbol);
            Debug.Assert(constructedFrom.Arity == typeArgumentsWithAnnotations.Length);

            _constructedFrom = constructedFrom;
            _typeArgumentsWithAnnotations = typeArgumentsWithAnnotations;
            _map = new TypeMap(constructedFrom.OriginalDefinition.TypeParameters, typeArgumentsWithAnnotations);

            if (constructedFrom.Arity == 0)
            {
                _underlyingType = constructedFrom.Target as TypeSymbol;
            }
            else
            {
                _underlyingType = _map.SubstituteType(constructedFrom.Target as TypeSymbol).Type;
            }
        }

        public TypeSymbol UnderlyingType => _underlyingType;

        public int Arity => _constructedFrom.Arity;

        /// <summary>
        /// Returns the map from type parameters to type arguments.
        /// If this is not a generic type instantiation, returns null.
        /// The map targets the original definition of the type.
        /// </summary>
        internal TypeMap TypeSubstitution => _map;

        public new AliasSymbol OriginalDefinition => _constructedFrom;

        internal ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => _typeArgumentsWithAnnotations;

        public AliasSymbolFromSyntax ConstructedFrom => _constructedFrom;

        public override ImmutableArray<Location> Locations => _constructedFrom.Locations;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => _constructedFrom.DeclaringSyntaxReferences;

        public override Accessibility DeclaredAccessibility => _constructedFrom.DeclaredAccessibility;

        internal override string GetDebuggerDisplay()
        {
            return $"{nameof(TypeKindInternal.AliasTargetType)} {{{this.UnderlyingType.GetDebuggerDisplay()}}}";
        }

        #region TypeSymbol members

        protected override TypeSymbol OriginalTypeSymbolDefinition => UnderlyingType.OriginalDefinition;

        public override TypeKind TypeKind => TypeKindInternal.AliasTargetType;

        public override bool IsReferenceType => UnderlyingType.IsReferenceType;

        public override bool IsValueType => UnderlyingType.IsValueType;

        public override bool IsRefLikeType => UnderlyingType.IsRefLikeType;

        public override bool IsReadOnly => UnderlyingType.IsReadOnly;

        public override SymbolKind Kind => UnderlyingType.Kind;

        public override Symbol ContainingSymbol => UnderlyingType.ContainingSymbol;

        public override bool IsStatic => UnderlyingType.IsStatic;

        public override bool IsAbstract => UnderlyingType.IsAbstract;

        public override bool IsSealed => UnderlyingType.IsSealed;

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => UnderlyingType.BaseTypeNoUseSiteDiagnostics;

        internal override bool IsRecord => UnderlyingType.IsRecord;

        internal override bool IsRecordStruct => UnderlyingType.IsRecordStruct;

        internal override ObsoleteAttributeData ObsoleteAttributeData => UnderlyingType.ObsoleteAttributeData;

        public override void Accept(CSharpSymbolVisitor visitor)
        {
            UnderlyingType.Accept(visitor);
        }

        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor)
        {
            return UnderlyingType.Accept(visitor);
        }

        internal override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument a)
        {
            return UnderlyingType.Accept(visitor, a);
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            return UnderlyingType.GetMembers();
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            return UnderlyingType.GetMembers(name);
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return UnderlyingType.GetTypeMembers();
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            return UnderlyingType.GetTypeMembers(name);
        }

        internal override void AddNullableTransforms(ArrayBuilder<byte> transforms)
        {
            UnderlyingType.AddNullableTransforms(transforms);
        }

        internal override bool ApplyNullableTransforms(byte defaultTransformFlag, ImmutableArray<byte> transforms, ref int position, out TypeSymbol result)
        {
            return UnderlyingType.ApplyNullableTransforms(defaultTransformFlag, transforms, ref position, out result);
        }

        internal override ManagedKind GetManagedKind(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            return UnderlyingType.GetManagedKind(ref useSiteInfo);
        }

        internal override bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            return UnderlyingType.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes);
        }

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol> basesBeingResolved = null)
        {
            return UnderlyingType.InterfacesNoUseSiteDiagnostics(basesBeingResolved);
        }

        internal override TypeSymbol MergeEquivalentTypes(TypeSymbol other, VarianceKind variance)
        {
            return UnderlyingType.MergeEquivalentTypes(other, variance);
        }

        internal override TypeSymbol SetNullabilityForReferenceTypes(Func<TypeWithAnnotations, TypeWithAnnotations> transform)
        {
            return UnderlyingType.SetNullabilityForReferenceTypes(transform);
        }

        internal override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls()
        {
            return UnderlyingType.SynthesizedInterfaceMethodImpls();
        }

        protected override ITypeSymbol CreateITypeSymbol(CodeAnalysis.NullableAnnotation nullableAnnotation)
        {
            return UnderlyingType.GetITypeSymbol(nullableAnnotation);
        }

        protected override ISymbol CreateISymbol()
        {
            return UnderlyingType.ISymbol;
        }

        #endregion
    }

    internal static class AliasTargetTypeSymbolExtensions
    {
        /// <summary>
        /// Unwrap a type symbol if it is an AliasTargetTypeSymbol and get its underlying type symbol.
        /// </summary>
        public static TypeSymbol Unwrap(this TypeSymbol type)
        {
            // unwrap recursively.
            while (type.TypeKind == TypeKindInternal.AliasTargetType)
            {
                CheckSymbol(type);

                type = ((AliasTargetTypeSymbol)type).UnderlyingType;
            }

            return type;
        }

        /// <summary>
        /// Unwrap a type symbol if it is an AliasTargetTypeSymbol and get its underlying type symbol.
        /// </summary>
        public static TSymbol Unwrap<TSymbol>(this TypeSymbol type) where TSymbol : TypeSymbol
        {
            type = Unwrap(type);
            Debug.Assert(type is TSymbol);
            return (TSymbol)type;
        }

        /// <summary>
        /// Unwrap a type symbol if it is an AliasTargetTypeSymbol and get its underlying type symbol.
        /// If <paramref name="type"/> is not <typeparamref name="TSymbol"/> then returns <see langword="null"/>.
        /// </summary>
        public static TSymbol? UnwrapAs<TSymbol>(this TypeSymbol type) where TSymbol : TypeSymbol
        {
            type = Unwrap(type);
            return type as TSymbol;
        }

        [Conditional("DEBUG")]
        private static void CheckSymbol(TypeSymbol type)
        {
            if (type.TypeKind == TypeKindInternal.AliasTargetType)
            {
                Debug.Assert(type is AliasTargetTypeSymbol);
            }
        }
    }
}

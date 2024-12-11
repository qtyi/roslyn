// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal enum NamespaceOrTypeSymbolDeconstructionResultKind
    {
        /// <summary>Indicates deconstruction is failed, a namespace symbol or a type symbol does not fit the structure.</summary>
        NotApplicable,
        /// <summary>Indicates a namespace symbol or a type symbol fits the structure, though some of type parameter symbols are not resolved.</summary>
        Ambiguous,
        /// <summary>Indicates deconstruction is success.</summary>
        Viable
    }

    internal class NamespaceOrTypeSymbolDeconstructionVisitor : SymbolVisitor<INamespaceOrTypeSymbol, bool>, IDisposable
    {
        private readonly ImmutableArray<ITypeParameterSymbol> _typeParameterSymbols;
        private readonly PooledDictionary<ITypeParameterSymbol, ITypeSymbol?>? _map;

        private NamespaceOrTypeSymbolDeconstructionVisitor(IAliasSymbol aliasSymbol) : this(aliasSymbol.TypeParameters) { }

        private NamespaceOrTypeSymbolDeconstructionVisitor(ImmutableArray<ITypeParameterSymbol> typeParameterSymbols)
        {
            _typeParameterSymbols = typeParameterSymbols;
            if (!typeParameterSymbols.IsDefaultOrEmpty)
            {
                _map = PooledDictionary<ITypeParameterSymbol, ITypeSymbol?>.GetInstance();
                foreach (var tp in typeParameterSymbols)
                {
                    _map.Add(tp, null);
                }
            }
            else
            {
                _map = null;
            }
        }

        public static NamespaceOrTypeSymbolDeconstructionResultKind Deconstruct(
            INamespaceOrTypeSymbol bound,
            IAliasSymbol aliasSymbol,
            out ImmutableArray<ITypeSymbol> typeArguments)
        {
            using var visitor = new NamespaceOrTypeSymbolDeconstructionVisitor(aliasSymbol);
            return DeconstructCore(visitor, bound, aliasSymbol.Target, out typeArguments);
        }

        public static NamespaceOrTypeSymbolDeconstructionResultKind Deconstruct(
            INamespaceOrTypeSymbol bound,
            INamespaceOrTypeSymbol unbound,
            ImmutableArray<ITypeParameterSymbol> typeParameterSymbols,
            out ImmutableArray<ITypeSymbol> typeArguments)
        {
            using var visitor = new NamespaceOrTypeSymbolDeconstructionVisitor(typeParameterSymbols);
            return DeconstructCore(visitor, bound, unbound, out typeArguments);
        }

        private static NamespaceOrTypeSymbolDeconstructionResultKind DeconstructCore(
            NamespaceOrTypeSymbolDeconstructionVisitor visitor,
            INamespaceOrTypeSymbol bound, INamespaceOrTypeSymbol unbound,
            out ImmutableArray<ITypeSymbol> typeArguments)
        {
            if (visitor.Visit(bound, unbound))
            {
                if (visitor._typeParameterSymbols.IsEmpty)
                {
                    typeArguments = ImmutableArray<ITypeSymbol>.Empty;
                    return NamespaceOrTypeSymbolDeconstructionResultKind.Viable;
                }

                Debug.Assert(visitor._map is not null);

                bool notResolved = false;

                var arguments = ArrayBuilder<ITypeSymbol>.GetInstance(visitor._typeParameterSymbols.Length);
                foreach (var tp in visitor._typeParameterSymbols)
                {
                    var ta = visitor._map[tp];
                    if (ta is null)
                    {
                        notResolved = true;
                        arguments.Add(tp);
                    }
                    else
                    {
                        arguments.Add(ta);
                    }
                }

                typeArguments = arguments.ToImmutableAndFree();
                return notResolved ? NamespaceOrTypeSymbolDeconstructionResultKind.Ambiguous : NamespaceOrTypeSymbolDeconstructionResultKind.Viable;
            }

            typeArguments = default;
            return NamespaceOrTypeSymbolDeconstructionResultKind.NotApplicable;
        }

        protected override bool DefaultResult => throw ExceptionUtilities.Unreachable();

        private static bool CheckNullableAnnotation(ITypeSymbol bound, ITypeSymbol unbound) => CheckNullableAnnotation(bound.NullableAnnotation, unbound.NullableAnnotation);

        private static bool CheckNullableAnnotation(NullableAnnotation boundNullableAnnotation, NullableAnnotation unboundNullableAnnotation)
        {
            if (boundNullableAnnotation == NullableAnnotation.None || unboundNullableAnnotation == NullableAnnotation.None)
            {
                return true;
            }

            return boundNullableAnnotation == unboundNullableAnnotation;
        }

        private bool CheckTypeParameterSymbol(
            ITypeSymbol bound, INamespaceOrTypeSymbol unbound)
        {
            if (unbound is ITypeParameterSymbol typeParameter)
            {
                Debug.Assert(_map is not null);
                if (_map[typeParameter] is ITypeSymbol typeArgument)
                {
                    return bound.Equals(typeArgument);
                }
                else
                {
                    _map[typeParameter] = bound;
                    return true;
                }
            }

            return false;
        }

        public override bool VisitArrayType(IArrayTypeSymbol bound, INamespaceOrTypeSymbol unbound)
        {
            if (CheckTypeParameterSymbol(bound, unbound))
            {
                return true;
            }

            if (unbound is IArrayTypeSymbol arrayType)
            {
                return bound.Rank == arrayType.Rank &&
                    Visit(bound.ElementType, arrayType.ElementType) &&
                    CheckNullableAnnotation(bound, arrayType);
            }

            return false;
        }

        public override bool VisitDynamicType(IDynamicTypeSymbol bound, INamespaceOrTypeSymbol unbound)
        {
            if (CheckTypeParameterSymbol(bound, unbound))
            {
                return true;
            }

            return unbound is IDynamicTypeSymbol dynamicType &&
                CheckNullableAnnotation(bound, dynamicType);
        }

        public override bool VisitFunctionPointerType(IFunctionPointerTypeSymbol bound, INamespaceOrTypeSymbol unbound)
        {
            if (CheckTypeParameterSymbol(bound, unbound))
            {
                return true;
            }

            if (unbound is IFunctionPointerTypeSymbol functionPointerType)
            {
                return VisitFunctionPointerSignature(bound.Signature, functionPointerType.Signature) &&
                    CheckNullableAnnotation(bound, functionPointerType);
            }

            return false;
        }

        private bool VisitFunctionPointerSignature(IMethodSymbol bound, IMethodSymbol unbound)
        {
            if (bound.CallingConvention != unbound.CallingConvention)
            {
                return false;
            }

            return Visit(bound.ReturnType, unbound.ReturnType) &&
                    VisitFunctionPointerParameterList(bound.Parameters, unbound.Parameters);
        }

        private bool VisitFunctionPointerParameterList(ImmutableArray<IParameterSymbol> boundSymbols, ImmutableArray<IParameterSymbol> unboundSymbols)
        {
            if (boundSymbols.Length != unboundSymbols.Length)
            {
                return false;
            }

            for (int i = 0, length = boundSymbols.Length; i < length; i++)
            {
                if (!VisitFunctionPointerParameter(boundSymbols[i], unboundSymbols[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool VisitFunctionPointerParameter(IParameterSymbol bound, IParameterSymbol unbound)
        {
            return bound.RefKind == unbound.RefKind &&
                Visit(bound.Type, unbound.Type);
        }

        public override bool VisitNamedType(INamedTypeSymbol bound, INamespaceOrTypeSymbol unbound)
        {
            if (CheckTypeParameterSymbol(bound, unbound))
            {
                return true;
            }

            if (unbound is INamedTypeSymbol namedType)
            {
                return bound.OriginalDefinition.Equals(unbound.OriginalDefinition) &&
                    (!bound.IsGenericType || VisitList(bound.TypeArguments, namedType.TypeArguments)) &&
                    CheckNullableAnnotation(bound, namedType);
            }

            return false;
        }

        private bool VisitList<TSymbol>(ImmutableArray<TSymbol> boundSymbols, ImmutableArray<TSymbol> unboundSymbols) where TSymbol : INamespaceOrTypeSymbol
        {
            if (boundSymbols.Length != unboundSymbols.Length)
            {
                return false;
            }

            for (int i = 0, length = boundSymbols.Length; i < length; i++)
            {
                if (!Visit(boundSymbols[i], unboundSymbols[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool VisitNamespace(INamespaceSymbol bound, INamespaceOrTypeSymbol unbound)
        {
            return unbound is INamespaceSymbol @namespace && bound.Equals(@namespace);
        }

        public override bool VisitPointerType(IPointerTypeSymbol bound, INamespaceOrTypeSymbol unbound)
        {
            if (CheckTypeParameterSymbol(bound, unbound))
            {
                return true;
            }

            if (unbound is IPointerTypeSymbol pointerType)
            {
                return Visit(bound.PointedAtType, pointerType.PointedAtType) &&
                CheckNullableAnnotation(bound, pointerType);
            }

            return false;
        }

        public override bool VisitTypeParameter(ITypeParameterSymbol bound, INamespaceOrTypeSymbol unbound)
        {
            if (CheckTypeParameterSymbol(bound, unbound))
            {
                return true;
            }

            return unbound is ITypeParameterSymbol typeParameter &&
                CheckNullableAnnotation(bound, typeParameter);
        }

        void IDisposable.Dispose()
        {
            this._map?.Free();
        }
    }
}

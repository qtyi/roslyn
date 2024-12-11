// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Microsoft.CodeAnalysis.FlowAnalysis.ControlFlowGraphBuilder;
using Roslyn.Utilities;
using System.Reflection.Metadata;

namespace Microsoft.Cci
{
    internal static class ITypeReferenceExtensions
    {
        internal static void GetConsolidatedTypeArguments(this ITypeReference typeReference, ArrayBuilder<ITypeReference> consolidatedTypeArguments, EmitContext context)
        {
            INestedTypeReference? nestedTypeReference = typeReference.AsNestedTypeReference;
            nestedTypeReference?.GetContainingType(context).GetConsolidatedTypeArguments(consolidatedTypeArguments, context);

            IGenericTypeInstanceReference? genTypeInstance = typeReference.AsGenericTypeInstanceReference;
            if (genTypeInstance != null)
            {
                consolidatedTypeArguments.AddRange(genTypeInstance.GetGenericArguments(context));
            }
        }

        internal static ITypeReference GetUninstantiatedGenericType(this ITypeReference typeReference, EmitContext context)
        {
            IGenericTypeInstanceReference? genericTypeInstanceReference = typeReference.AsGenericTypeInstanceReference;
            if (genericTypeInstanceReference != null)
            {
                return genericTypeInstanceReference.GetGenericType(context);
            }

            ISpecializedNestedTypeReference? specializedNestedType = typeReference.AsSpecializedNestedTypeReference;
            if (specializedNestedType != null)
            {
                return specializedNestedType.GetUnspecializedVersion(context);
            }

            return typeReference;
        }

        internal static bool IsTypeSpecification(this ITypeReference typeReference)
        {
            INestedTypeReference? nestedTypeReference = typeReference.AsNestedTypeReference;
            if (nestedTypeReference != null)
            {
                return nestedTypeReference.AsSpecializedNestedTypeReference != null ||
                    nestedTypeReference.AsGenericTypeInstanceReference != null;
            }

            return typeReference.AsNamespaceTypeReference == null;
        }

        #region WORKAROUND(sanmuru)
#if false

        internal static bool ContainsTypeParameter(this ITypeReference typeReference, EmitContext context) =>
            typeReference.VisitType(context, (ITypeReference t, EmitContext _, object? _) => t is IGenericParameterReference, null) is not null;

        internal static ITypeReference? VisitType<T>(this ITypeReference typeReference, EmitContext context, Func<ITypeReference, EmitContext, T, bool> predicate, T arg)
        {
            var visitor = new TypeReferenceVisitor<T>(context, predicate, arg);
            visitor.Visit(typeReference);
            return visitor.FindResult;
        }

        internal static ITypeReference? VisitType<T>(this IEnumerable<ITypeReference> typeReferences, EmitContext context, Func<ITypeReference, EmitContext, T, bool> predicate, T arg)
        {
            var visitor = new TypeReferenceVisitor<T>(context, predicate, arg);
            foreach (var typeReference in typeReferences)
            {
                visitor.Visit(typeReference);
                if (visitor.FindResult is ITypeReference result)
                {
                    return result;
                }
            }
            return null;
        }

        private sealed class TypeReferenceVisitor<TArgument> : MetadataVisitor
        {
            private readonly Func<ITypeReference, EmitContext, TArgument, bool> _predicate;
            private readonly TArgument _argument;

            private ITypeReference? _findResult;
            public ITypeReference? FindResult => _findResult;

            public TypeReferenceVisitor(EmitContext context, Func<ITypeReference, EmitContext, TArgument, bool> predicate, TArgument argument)
                : base(context)
            {
                _predicate = predicate;
                _argument = argument;
            }

            public override void Visit(ITypeReference typeReference)
            {
                if (_findResult is not null)
                {
                    return;
                }

                if (_predicate(typeReference, Context, _argument))
                {
                    _findResult = typeReference;
                    return;
                }

                this.DispatchAsReference(typeReference);
            }

            public override void Visit(IGenericTypeInstanceReference genericTypeInstanceReference)
            {
                INestedTypeReference? nestedType = genericTypeInstanceReference.AsNestedTypeReference;

                if (nestedType != null)
                {
                    ITypeReference containingType = nestedType.GetContainingType(Context);

                    if (containingType.AsGenericTypeInstanceReference != null ||
                        containingType.AsSpecializedNestedTypeReference != null)
                    {
                        this.Visit(nestedType.GetContainingType(Context));
                    }
                }

                this.Visit(genericTypeInstanceReference.GetGenericType(Context));
                this.Visit(genericTypeInstanceReference.GetGenericArguments(Context));
            }

            public override void Visit(CommonPEModuleBuilder module) => throw ExceptionUtilities.Unreachable();

            public override void Visit(ITypeDefinition typeDefinition) => throw ExceptionUtilities.Unreachable();
        }

#endif
        #endregion

    }
}

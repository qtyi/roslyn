// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a using alias (Imports alias in Visual Basic).
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IAliasSymbol : ISymbol
    {
        /// <summary>
        /// Gets the <see cref="INamespaceOrTypeSymbol"/> for the
        /// namespace or type referenced by the alias.
        /// </summary>
        INamespaceOrTypeSymbol Target { get; }

        /// <summary>
        /// Returns the arity of this alias, or the number of type parameters it takes.
        /// A non-generic alias has zero arity.
        /// </summary>
        int Arity { get; }

        /// <summary>
        /// True if this alias has type parameters.
        /// </summary>
        bool IsGenericAlias { get; }

        /// <summary>
        /// True if this is a reference to an <em>unbound</em> generic alias. A generic alias is
        /// considered <em>unbound</em> if any of the type argument lists is empty. Note that
        /// the type arguments of an unbound generic alias will be returned as error types
        ///  because they do not really have type arguments.
        /// </summary>
        bool IsUnboundGenericAlias { get; }

        /// <summary>
        /// Returns the type parameters that this alias has. If this is a non-generic alias,
        /// returns an empty ImmutableArray.  
        /// </summary>
        ImmutableArray<ITypeParameterSymbol> TypeParameters { get; }

        /// <summary>
        /// Returns the type arguments that have been substituted for the type parameters. 
        /// If nothing has been substituted for a given type parameter,
        /// then the type parameter itself is considered the type argument.
        /// </summary>
        ImmutableArray<ITypeSymbol> TypeArguments { get; }

        /// <summary>
        /// Returns the top-level nullability of the type arguments that have been substituted
        /// for the type parameters. If nothing has been substituted for a given type parameter,
        /// then <see cref="NullableAnnotation.None"/> is returned for that type argument.
        /// </summary>
        ImmutableArray<NullableAnnotation> TypeArgumentNullableAnnotations { get; }

        /// <summary>
        /// Returns custom modifiers for the type argument that has been substituted for the type parameter. 
        /// The modifiers correspond to the type argument at the same ordinal within the <see cref="TypeArguments"/>
        /// array. Returns an empty array if there are no modifiers.
        /// </summary>
        ImmutableArray<CustomModifier> GetTypeArgumentCustomModifiers(int ordinal);

        /// <summary>
        /// Returns a constructed type given its type arguments.
        /// </summary>
        /// <param name="typeArguments">The immediate type arguments to be replaced for type
        /// parameters in the alias, then replace for those in the alias <see cref="Target"/>.</param>
        INamedTypeSymbol Construct(params ITypeSymbol[] typeArguments);

        /// <summary>
        /// Returns a constructed type given its type arguments and type argument nullable annotations.
        /// </summary>
        INamedTypeSymbol Construct(ImmutableArray<ITypeSymbol> typeArguments, ImmutableArray<NullableAnnotation> typeArgumentNullableAnnotations);

    }
}

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
        /// Returns the type parameters that this alias has. If this is a non-generic alias,
        /// returns an empty ImmutableArray.  
        /// </summary>
        ImmutableArray<ITypeParameterSymbol> TypeParameters { get; }

        /// <summary>
        /// Returns a constructed type given its type arguments.
        /// </summary>
        /// <param name="typeArguments">The immediate type arguments to be replaced for type
        /// parameters in the alias, then replace for those in the alias <see cref="Target"/>.</param>
        ITypeSymbol Construct(params ITypeSymbol[] typeArguments);

        /// <summary>
        /// Returns a constructed type given its type arguments and type argument nullable annotations.
        /// </summary>
        ITypeSymbol Construct(ImmutableArray<ITypeSymbol> typeArguments, ImmutableArray<NullableAnnotation> typeArgumentNullableAnnotations);

    }
}

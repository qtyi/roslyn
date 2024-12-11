// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Symbols;

internal interface IAliasSymbolInternal : ISymbolInternal
{
    /// <summary>
    /// Gets the namespace or type referenced by the alias.
    /// </summary>
    INamespaceOrTypeSymbolInternal Target { get; }

    /// <summary>
    /// Returns the arity of this alias, or the number of type parameters it takes.
    /// A non-generic alias has zero arity.
    /// </summary>
    int Arity { get; }

    /// <summary>
    /// True if this alias has type parameters.
    /// </summary>
    bool IsGenericAlias { get; }
}

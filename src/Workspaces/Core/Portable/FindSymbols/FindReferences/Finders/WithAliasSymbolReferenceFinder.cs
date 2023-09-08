// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal abstract class WithAliasSymbolReferenceFinder<TTargetSymbol> : AbstractReferenceFinder<TTargetSymbol>
        where TTargetSymbol : INamespaceOrTypeSymbol
    {
        protected abstract ValueTask<ImmutableArray<FinderLocation>> FindAllNonLocalAliaseReferencesAsync(
            TTargetSymbol targetSymbol, FindReferencesDocumentState state, CancellationToken cancellationToken);

        protected override async ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(TTargetSymbol targetSymbol, FindReferencesDocumentState state, FindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<FinderLocation>.GetInstance(out var initialReferences);

            initialReferences.AddRange(await FindAllNonLocalAliaseReferencesAsync(targetSymbol, state, cancellationToken).ConfigureAwait(false));

            // This target may end up being locally aliased as well.  If so, now find all the references
            // to the local alias.
            // As a local alias may target to another local alias, we perform finding recursively.  If there
            // is no new location found in the preview pass, we think that all referenced local aliases are
            // found.
            var localAliasReferences = initialReferences.ToImmutable();
            while (!localAliasReferences.IsEmpty)
            {
                localAliasReferences = await FindLocalAliasReferencesAsync(localAliasReferences, state, cancellationToken).ConfigureAwait(false);
                initialReferences.AddRange(localAliasReferences);
            }

            initialReferences.AddRange(await FindReferencesInDocumentInsideGlobalSuppressionsAsync(
                targetSymbol, state, cancellationToken).ConfigureAwait(false));

            return initialReferences.ToImmutable();
        }
    }
}

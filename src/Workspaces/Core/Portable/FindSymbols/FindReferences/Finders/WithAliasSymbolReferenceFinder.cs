// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal abstract class WithAliasSymbolReferenceFinder<TTargetSymbol> : AbstractReferenceFinder<TTargetSymbol>
        where TTargetSymbol : INamespaceOrTypeSymbol
    {
        protected abstract void FindAllNonLocalAliasReferences<TData>(
            TTargetSymbol targetSymbol,
            FindReferencesDocumentState state,
            Action<FinderLocation, TData> processResult,
            TData processResultData,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken);

        protected override void FindReferencesInDocument<TData>(
            TTargetSymbol targetSymbol,
            FindReferencesDocumentState state,
            Action<FinderLocation, TData> processResult,
            TData processResultData,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var initialReferences = ArrayBuilder<FinderLocation>.GetInstance();

            FindAllNonLocalAliasReferences(targetSymbol, state, StandardCallbacks<FinderLocation>.AddToArrayBuilder, initialReferences, options, cancellationToken);

            // The items in initialReferences need to be both reported and used later to calculate additional results.
            foreach (var location in initialReferences)
                processResult(location, processResultData);

            // This target may end up being locally aliased as well.  If so, now find all the references
            // to the local alias.
            // As a local alias may target to another local alias, we perform finding recursively.  If there
            // is no new location found in the preview pass, we think that all referenced local aliases are
            // found.
            while (!initialReferences.IsEmpty)
            {
                var localAliasReferences = ArrayBuilder<FinderLocation>.GetInstance();
                FindLocalAliasReferences(initialReferences, targetSymbol, state, StandardCallbacks<FinderLocation>.AddToArrayBuilder, localAliasReferences, cancellationToken);

                // The items in localAliasReferences need to be both reported and used later to calculate additional results.
                foreach (var location in localAliasReferences)
                    processResult(location, processResultData);

                initialReferences.Free();
                initialReferences = localAliasReferences;
            }

            FindReferencesInDocumentInsideGlobalSuppressions(targetSymbol, state, processResult, processResultData, cancellationToken);

            initialReferences.Free();
        }
    }
}

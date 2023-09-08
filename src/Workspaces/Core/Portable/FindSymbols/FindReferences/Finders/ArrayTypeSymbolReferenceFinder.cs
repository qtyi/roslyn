// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal sealed class ArrayTypeSymbolReferenceFinder : WithAliasSymbolReferenceFinder<IArrayTypeSymbol>
    {
        protected override bool CanFind(IArrayTypeSymbol symbol) => true;

        protected override async Task<ImmutableArray<string>> DetermineGlobalAliasesAsync(IArrayTypeSymbol symbol, Project project, CancellationToken cancellationToken)
        {
            using var result = TemporaryArray<string>.Empty;

            foreach (var document in await project.GetAllRegularAndSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false))
            {
                var index = await SyntaxTreeIndex.GetRequiredIndexAsync(document, cancellationToken).ConfigureAwait(false);
                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                result.AddRange(index.GetGlobalAliasesByArray(symbol.Rank, syntaxFacts));
            }

            return result.ToImmutableAndClear();
        }

        protected override async ValueTask<ImmutableArray<FinderLocation>> FindAllNonLocalAliaseReferencesAsync(IArrayTypeSymbol symbol, FindReferencesDocumentState state, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<FinderLocation>.GetInstance(out var result);

            result.AddRange(await FindArrayTypeReferencesAsync(symbol, symbol.Rank, state, cancellationToken).ConfigureAwait(false));

            foreach (var globalAlias in state.GlobalAliases)
            {
                result.AddRange(await FindReferencesInDocumentUsingIdentifierAsync(symbol, globalAlias, state, cancellationToken).ConfigureAwait(false));
            }

            return result.ToImmutable();
        }

        protected override async Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            IArrayTypeSymbol symbol,
            HashSet<string>? globalAliases,
            Project project,
            IImmutableSet<Document>? documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<Document>.GetInstance(out var result);

            result.AddRange(await FindDocumentsWithPredicateAsync(project, documents, index => index.ContainsArrayCreationExpressionOrArrayType, cancellationToken).ConfigureAwait(false));

            if (globalAliases != null)
            {
                foreach (var globalAlias in globalAliases)
                    result.AddRange(await FindDocumentsAsync(project, documents, cancellationToken, globalAlias).ConfigureAwait(false));
            }

            return result.ToImmutable();
        }

        private static async ValueTask<ImmutableArray<FinderLocation>> FindArrayTypeReferencesAsync(
            IArrayTypeSymbol symbol,
            int rank,
            FindReferencesDocumentState state,
            CancellationToken cancellationToken)
        {
            var nodes = await state.Cache.FindMatchingArrayTypeNodesAsync(state.Document, rank, cancellationToken).ConfigureAwait(false);

            return nodes.SelectAsArray(node => CreateFinderLocation(state, node, CandidateReason.None, cancellationToken));
        }
    }
}

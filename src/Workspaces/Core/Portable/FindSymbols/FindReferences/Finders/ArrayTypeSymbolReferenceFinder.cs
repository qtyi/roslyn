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

        protected override async Task<ImmutableArray<NameWithArity>> DetermineGlobalAliasesAsync(IArrayTypeSymbol symbol, Project project, CancellationToken cancellationToken)
        {
            using var result = TemporaryArray<NameWithArity>.Empty;

            await foreach (var document in project.GetAllRegularAndSourceGeneratedDocumentsAsync(cancellationToken))
            {
                var index = await SyntaxTreeIndex.GetRequiredIndexAsync(document, cancellationToken).ConfigureAwait(false);
                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                result.AddRange(index.GetGlobalAliases(SyntaxTreeIndex.FilterAliasesByArray(symbol.Rank, syntaxFacts)));
            }

            return result.ToImmutableAndClear();
        }

        protected override void FindAllNonLocalAliasReferences<TData>(
            IArrayTypeSymbol symbol,
            FindReferencesDocumentState state,
            Action<FinderLocation, TData> processResult,
            TData processResultData,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            FindArrayTypeReferences(symbol, symbol.Rank, state, processResult, processResultData, cancellationToken);

            foreach (var globalAlias in state.GlobalAliases)
            {
                FindReferencesInDocumentUsingIdentifier(symbol, globalAlias.Name, globalAlias.Arity, state, processResult, processResultData, cancellationToken);
            }
        }

        protected override async Task DetermineDocumentsToSearchAsync<TData>(
            IArrayTypeSymbol symbol,
            HashSet<NameWithArity>? globalAliases,
            Project project,
            IImmutableSet<Document>? documents,
            Action<Document, TData> processResult,
            TData processResultData,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            await FindDocumentsWithPredicateAsync(project, documents, static index => index.ContainsArrayCreationExpressionOrArrayType, processResult, processResultData, cancellationToken).ConfigureAwait(false);

            if (globalAliases != null)
            {
                foreach (var globalAlias in globalAliases)
                    await FindDocumentsAsync(project, documents, processResult, processResultData, cancellationToken, globalAlias.Name).ConfigureAwait(false);
            }
        }

        private static void FindArrayTypeReferences<TData>(
            IArrayTypeSymbol symbol,
            int rank,
            FindReferencesDocumentState state,
            Action<FinderLocation, TData> processResult,
            TData processResultData,
            CancellationToken cancellationToken)
        {
            var nodes = state.Cache.FindMatchingArrayTypeNodes(state.Document, rank, cancellationToken);

            foreach (var node in nodes)
            {
                var location = CreateFinderLocation(state, node, CandidateReason.None, cancellationToken);

                processResult(location, processResultData);
            }
        }
    }
}

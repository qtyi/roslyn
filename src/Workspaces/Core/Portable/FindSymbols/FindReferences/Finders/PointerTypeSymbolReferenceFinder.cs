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
    internal sealed class PointerTypeSymbolReferenceFinder : AbstractPointerTypeSymbolReferenceFinder<IPointerTypeSymbol>
    {
        protected override bool CanFind(IPointerTypeSymbol pointerSymbol) => true;

        protected override async Task<ImmutableArray<NameWithArity>> DetermineGlobalAliasesAsync(IPointerTypeSymbol symbol, Project project, CancellationToken cancellationToken)
        {
            using var result = TemporaryArray<NameWithArity>.Empty;

            await foreach (var document in project.GetAllRegularAndSourceGeneratedDocumentsAsync(cancellationToken))
            {
                var index = await SyntaxTreeIndex.GetRequiredIndexAsync(document, cancellationToken).ConfigureAwait(false);
                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                result.AddRange(index.GetGlobalAliases(SyntaxTreeIndex.FilterAliasesByPointer(syntaxFacts)));
            }

            return result.ToImmutableAndClear();
        }

        protected override void FindPointerTypeReferences<TData>(
            IPointerTypeSymbol symbol,
            FindReferencesDocumentState state,
            Action<FinderLocation, TData> processResult,
            TData processResultData,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var nodes = state.Cache.FindMatchingPointerTypeNodes(state.Document, cancellationToken);

            foreach (var node in nodes)
            {
                var location = CreateFinderLocation(state, node, CandidateReason.None, cancellationToken);

                processResult(location, processResultData);
            }
        }
    }
}

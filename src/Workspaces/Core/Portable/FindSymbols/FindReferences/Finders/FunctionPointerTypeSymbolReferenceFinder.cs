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
    internal sealed class FunctionPointerTypeSymbolReferenceFinder : AbstractPointerTypeSymbolReferenceFinder<IFunctionPointerTypeSymbol>
    {
        protected override bool CanFind(IFunctionPointerTypeSymbol symbol) => true;

        protected override async Task<ImmutableArray<string>> DetermineGlobalAliasesAsync(IFunctionPointerTypeSymbol symbol, Project project, CancellationToken cancellationToken)
        {
            using var result = TemporaryArray<string>.Empty;

            foreach (var document in await project.GetAllRegularAndSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false))
            {
                var index = await SyntaxTreeIndex.GetRequiredIndexAsync(document, cancellationToken).ConfigureAwait(false);
                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                result.AddRange(index.GetGlobalAliasesByFunctionPointer(symbol.Signature.Parameters.Length, syntaxFacts));
            }

            return result.ToImmutableAndClear();
        }

        protected override async ValueTask<ImmutableArray<FinderLocation>> FindPointerTypeReferencesAsync(IFunctionPointerTypeSymbol symbol, FindReferencesDocumentState state, CancellationToken cancellationToken)
        {
            var nodes = await state.Cache.FindMatchingFunctionPointerTypeNodesAsync(state.Document, symbol.Signature.Parameters.Length, cancellationToken).ConfigureAwait(false);

            return nodes.SelectAsArray(node => CreateFinderLocation(state, node, CandidateReason.None, cancellationToken));
        }
    }
}

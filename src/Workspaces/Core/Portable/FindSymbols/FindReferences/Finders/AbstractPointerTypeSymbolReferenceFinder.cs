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
    internal abstract class AbstractPointerTypeSymbolReferenceFinder<TPointerType> : WithAliasSymbolReferenceFinder<TPointerType>
        where TPointerType : ITypeSymbol
    {
        protected override async ValueTask<ImmutableArray<FinderLocation>> FindAllNonLocalAliaseReferencesAsync(TPointerType symbol, FindReferencesDocumentState state, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<FinderLocation>.GetInstance(out var result);

            result.AddRange(await FindPointerTypeReferencesAsync(symbol, state, cancellationToken).ConfigureAwait(false));

            foreach (var globalAlias in state.GlobalAliases)
            {
                result.AddRange(await FindReferencesInDocumentUsingIdentifierAsync(symbol, globalAlias, state, cancellationToken).ConfigureAwait(false));
            }

            return result.ToImmutable();
        }

        protected override async Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            TPointerType symbol,
            HashSet<string>? globalAliases,
            Project project,
            IImmutableSet<Document>? documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<Document>.GetInstance(out var result);

            result.AddRange(await FindDocumentsWithPredicateAsync(project, documents, index => index.ContainsPointerType, cancellationToken).ConfigureAwait(false));

            if (globalAliases != null)
            {
                foreach (var globalAlias in globalAliases)
                    result.AddRange(await FindDocumentsAsync(project, documents, cancellationToken, globalAlias).ConfigureAwait(false));
            }

            return result.ToImmutable();
        }

        protected abstract ValueTask<ImmutableArray<FinderLocation>> FindPointerTypeReferencesAsync(TPointerType symbol, FindReferencesDocumentState state, CancellationToken cancellationToken);
    }
}

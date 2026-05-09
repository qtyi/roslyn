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
        protected override void FindAllNonLocalAliasReferences<TData>(
            TPointerType pointerSymbol,
            FindReferencesDocumentState state,
            Action<FinderLocation, TData> processResult,
            TData processResultData,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            FindPointerTypeReferences(pointerSymbol, state, processResult, processResultData, options, cancellationToken);

            foreach (var globalAlias in state.GlobalAliases)
            {
                FindReferencesInDocumentUsingIdentifier(pointerSymbol, globalAlias.Name, globalAlias.Arity, state, processResult, processResultData, cancellationToken);
            }
        }

        protected override async Task DetermineDocumentsToSearchAsync<TData>(
            TPointerType pointerSymbol,
            HashSet<NameWithArity>? globalAliases,
            Project project,
            IImmutableSet<Document>? documents,
            Action<Document, TData> processResult,
            TData processResultData,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            await FindDocumentsWithPredicateAsync(project, documents, index => index.ContainsPointerType, processResult, processResultData, cancellationToken).ConfigureAwait(false);

            if (globalAliases != null)
            {
                foreach (var globalAlias in globalAliases)
                {
                    await FindDocumentsAsync(project, documents, processResult, processResultData, cancellationToken, globalAlias.Name).ConfigureAwait(false);
                }
            }
        }

        protected abstract void FindPointerTypeReferences<TData>(
            TPointerType pointerSymbol,
            FindReferencesDocumentState state,
            Action<FinderLocation, TData> processResult,
            TData processResultData,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Collections;
using System.Xml.Linq;
using Roslyn.Utilities;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.SQLite.Interop;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal sealed class AliasTypeParameterSymbolReferenceFinder : WithAliasSymbolReferenceFinder<ITypeParameterSymbol>
    {
        protected override bool CanFind(ITypeParameterSymbol symbol)
            => symbol.TypeParameterKind is TypeParameterKind.Alias;

        protected override async Task<ImmutableArray<NameWithArity>> DetermineGlobalAliasesAsync(ITypeParameterSymbol symbol, Project project, CancellationToken cancellationToken)
        {
            using var result = TemporaryArray<NameWithArity>.Empty;

            await foreach (var document in project.GetAllRegularAndSourceGeneratedDocumentsAsync(cancellationToken))
            {
                var index = await SyntaxTreeIndex.GetRequiredIndexAsync(document, cancellationToken).ConfigureAwait(false);
                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                foreach (var name in index.GetGlobalAliases(SyntaxTreeIndex.FilterAliasesByTypeParameter(symbol.Name, syntaxFacts)))
                {
                    if (syntaxFacts.StringComparer.Equals(symbol.ContainingSymbol.Name, name))
                    {
                        result.Add(name);
                    }
                }
            }

            return result.ToImmutableAndClear();
        }

        protected override void FindAllNonLocalAliasReferences<TData>(
            ITypeParameterSymbol symbol,
            FindReferencesDocumentState state,
            Action<FinderLocation, TData> processResult,
            TData processResultData,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            // Alias type parameters are like locals.  They are only in scope in the bounds of the using
            // directive they're declared within.  We improve perf by limiting our search by only looking within the
            // using directive's span. 

            using var _ = ArrayBuilder<FinderLocation>.GetInstance(out var result);

            FindReferencesInDocumentUsingSymbolName(symbol, state, StandardCallbacks<FinderLocation>.AddToArrayBuilder, result, cancellationToken);
            foreach (var location in result)
            {
                // Find references in the document tree that type parameter symbol declared.
                if (state.SyntaxTree != symbol.Locations[0].SourceTree)
                {
                    continue;
                }

                // Find references in the text span of using alias declaration.
                var usingDirective = location.Node.FirstAncestorOrSelf<SyntaxNode>(node => state.SyntaxFacts.IsUsingAliasDirective(node));
                if (usingDirective != null && usingDirective.Span.Contains(symbol.Locations[0].SourceSpan))
                {
                    state.SyntaxFacts.GetPartsOfUsingAliasDirective(usingDirective, out var _, out var identifier, out var typeParameters, out var _);
                    // The using alias declaration must has the proper name and arity.
                    if (state.SyntaxFacts.StringComparer.Equals(identifier.Text, symbol.ContainingSymbol.Name) &&
                        typeParameters.Count == symbol.ContainingSymbol.GetArity())
                    {
                        processResult(location, processResultData);
                    }
                }
            }

            foreach (var globalAlias in state.GlobalAliases)
            {
                FindReferencesInDocumentUsingIdentifier(symbol, globalAlias.Name, globalAlias.Arity, state, processResult, processResultData, cancellationToken);
            }
        }

        protected override async Task DetermineDocumentsToSearchAsync<TData>(
            ITypeParameterSymbol symbol,
            HashSet<NameWithArity>? globalAliases,
            Project project,
            IImmutableSet<Document>? documents,
            Action<Document, TData> processResult,
            TData processResultData,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            // Type parameters are found in documents that have both their name, and the name
            // of its owning alias.  Besides, we have to check in multiple files because of
            // global usings.
            Debug.Assert(symbol.ContainingSymbol.Kind is SymbolKind.Alias);

            await FindDocumentsAsync(project, documents, processResult, processResultData, cancellationToken, symbol.Name, symbol.ContainingSymbol.Name).ConfigureAwait(false);

            if (globalAliases != null)
            {
                foreach (var globalAlias in globalAliases)
                    await FindDocumentsAsync(project, documents, processResult, processResultData, cancellationToken, globalAlias.Name).ConfigureAwait(false);
            }
        }
    }
}

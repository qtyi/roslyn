﻿// Licensed to the .NET Foundation under one or more agreements.
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

        protected override async Task<ImmutableArray<string>> DetermineGlobalAliasesAsync(ITypeParameterSymbol symbol, Project project, CancellationToken cancellationToken)
        {
            using var result = TemporaryArray<string>.Empty;

            foreach (var document in await project.GetAllRegularAndSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false))
            {
                var index = await SyntaxTreeIndex.GetRequiredIndexAsync(document, cancellationToken).ConfigureAwait(false);
                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                foreach (var name in index.GetGlobalAliasesByTypeParameter(symbol.Name, syntaxFacts))
                {
                    if (syntaxFacts.StringComparer.Equals(symbol.ContainingSymbol.Name, name))
                    {
                        result.Add(name);
                    }
                }
            }

            return result.ToImmutableAndClear();
        }

        protected override async ValueTask<ImmutableArray<FinderLocation>> FindAllNonLocalAliaseReferencesAsync(
            ITypeParameterSymbol symbol,
            FindReferencesDocumentState state,
            CancellationToken cancellationToken)
        {
            // Alias type parameters are like locals.  They are only in scope in the bounds of the using
            // directive they're declared within.  We improve perf by limiting our search by only looking within the
            // using directive's span. 

            using var _ = ArrayBuilder<FinderLocation>.GetInstance(out var result);

            foreach (var location in await FindReferencesInDocumentUsingSymbolNameAsync(symbol, state, cancellationToken).ConfigureAwait(false))
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
                        result.Add(location);
                    }
                }
            }

            foreach (var globalAlias in state.GlobalAliases)
            {
                result.AddRange(await FindReferencesInDocumentUsingIdentifierAsync(symbol, globalAlias, state, cancellationToken).ConfigureAwait(false));
            }

            return result.ToImmutable();
        }

        protected override async Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            ITypeParameterSymbol symbol,
            HashSet<string>? globalAliases,
            Project project,
            IImmutableSet<Document>? documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            // Type parameters are found in documents that have both their name, and the name
            // of its owning alias.  Besides, we have to check in multiple files because of
            // global usings.
            Debug.Assert(symbol.ContainingSymbol.Kind is SymbolKind.Alias);

            using var _ = ArrayBuilder<Document>.GetInstance(out var result);

            result.AddRange(await FindDocumentsAsync(project, documents, cancellationToken, symbol.Name, symbol.ContainingSymbol.Name).ConfigureAwait(false));

            if (globalAliases != null)
            {
                foreach (var globalAlias in globalAliases)
                    result.AddRange(await FindDocumentsAsync(project, documents, cancellationToken, globalAlias).ConfigureAwait(false));
            }

            return result.ToImmutable();
        }
    }
}

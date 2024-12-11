// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

/// <summary>
/// Caches information find-references needs associated with each document.  Computed and cached so that multiple calls
/// to find-references in a row can share the same data.
/// </summary>
internal sealed class FindReferenceCache
{
    private static readonly ConditionalWeakTable<Document, AsyncLazy<FindReferenceCache>> s_cache = new();

    public static async ValueTask<FindReferenceCache> GetCacheAsync(Document document, CancellationToken cancellationToken)
    {
        var lazy = s_cache.GetValue(document, static document => AsyncLazy.Create(ComputeCacheAsync, document));
        return await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false);

        static async Task<FindReferenceCache> ComputeCacheAsync(Document document, CancellationToken cancellationToken)
        {
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

            // Find-Refs is not impacted by nullable types at all.  So get a nullable-disabled semantic model to avoid
            // unnecessary costs while binding.
            var model = await document.GetRequiredNullableDisabledSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var nullableEnableSemanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // It's very costly to walk an entire tree.  So if the tree is simple and doesn't contain
            // any unicode escapes in it, then we do simple string matching to find the tokens.
            var index = await SyntaxTreeIndex.GetRequiredIndexAsync(document, cancellationToken).ConfigureAwait(false);

            return new(document, text, model, nullableEnableSemanticModel, root, index);
        }
    }

    public readonly Document Document;
    public readonly SourceText Text;
    public readonly SemanticModel SemanticModel;
    public readonly SyntaxNode Root;
    public readonly ISyntaxFactsService SyntaxFacts;
    public readonly SyntaxTreeIndex SyntaxTreeIndex;

    /// <summary>
    /// Not used by FAR directly.  But we compute and cache this while processing a document so that if we call any
    /// other services that use this semantic model, that they don't end up recreating it.
    /// </summary>
#pragma warning disable IDE0052 // Remove unread private members
    private readonly SemanticModel _nullableEnabledSemanticModel;
#pragma warning restore IDE0052 // Remove unread private members

    private readonly ConcurrentDictionary<SyntaxNode, (SymbolInfo symbolInfo, AliasInfo aliasInfo)> _symbolInfoCache = [];
    private readonly ConcurrentDictionary<NameWithArity, ImmutableArray<SyntaxToken>> _identifierCache;
    private readonly ConcurrentDictionary<int, ImmutableArray<SyntaxNode>> _tupleTypeCache = [];
    private readonly ConcurrentDictionary<int, ImmutableArray<SyntaxNode>> _arrayTypeCache = [];
    private readonly ConcurrentDictionary<int, ImmutableArray<SyntaxNode>> _pointerTypeCache = []; // pointer type for key 0; function pointer type for key (parameter count + 1).

    private ImmutableHashSet<NameWithArity>? _aliasSet;
    private ImmutableArray<SyntaxToken> _constructorInitializerCache;
    private ImmutableArray<SyntaxToken> _newKeywordsCache;

    private FindReferenceCache(
        Document document, SourceText text, SemanticModel semanticModel, SemanticModel nullableEnabledSemanticModel, SyntaxNode root, SyntaxTreeIndex syntaxTreeIndex)
    {
        Document = document;
        Text = text;
        SemanticModel = semanticModel;
        _nullableEnabledSemanticModel = nullableEnabledSemanticModel;
        Root = root;
        SyntaxTreeIndex = syntaxTreeIndex;
        SyntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        _identifierCache = new(comparer: new NameWithArityComparer(semanticModel.Language switch
        {
            LanguageNames.VisualBasic => StringComparer.OrdinalIgnoreCase,
            LanguageNames.CSharp => StringComparer.Ordinal,
            _ => throw ExceptionUtilities.UnexpectedValue(semanticModel.Language)
        }));
    }

    public (SymbolInfo symbolInfo, AliasInfo aliasInfo) GetSymbolInfo(SyntaxNode node, CancellationToken cancellationToken)
    {
        return _symbolInfoCache.GetOrAdd(
            node,
            static (n, arg) =>
            {
                var (semanticModel, cancellationToken) = arg;

                var symbolInfo = semanticModel.GetSymbolInfo(n, cancellationToken);
                var aliasInfo = semanticModel.GetAliasInfo(n, cancellationToken);
                return (symbolInfo, aliasInfo);
            },
            (SemanticModel, cancellationToken));
    }

    public AliasInfo GetAliasInfo(
        ISemanticFactsService semanticFacts, SyntaxToken token, CancellationToken cancellationToken)
    {
        if (_aliasSet == null)
        {
            var set = semanticFacts.GetAliasSet(SemanticModel, cancellationToken);
            Interlocked.CompareExchange(ref _aliasSet, set, null);
        }

        if (_aliasSet.Contains(token.ValueText))
            return SemanticModel.GetAliasInfo(token.GetRequiredParent(), cancellationToken);

        return default; // AliasInfo.None
    }

    public AliasInfo GetAliasInfo(
        ISemanticFactsService semanticFacts, SyntaxNode node, CancellationToken cancellationToken)
    {
        var tokens = node.DescendantTokens();
        if (tokens.IsSingle())
        {
            return GetAliasInfo(semanticFacts, tokens.Single(), cancellationToken);
        }

        return default; // AliasInfo.None
    }
    public ImmutableArray<SyntaxToken> FindMatchingIdentifierTokens(
        string identifier, CancellationToken cancellationToken)
    {
        return FindMatchingIdentifierTokens(identifier, arity: 0, cancellationToken);
    }

    public ImmutableArray<SyntaxToken> FindMatchingIdentifierTokens(
        string identifier, int arity, CancellationToken cancellationToken)
    {
        Debug.Assert(arity >= 0);

        if (identifier == "")
        {
            // Certain symbols don't have a name, so we return without further searching since the text-based index
            // and lookup never terminates if searching for an empty string.
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1655431
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1744118
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1820930
            return [];
        }

        if (_identifierCache.TryGetValue(new NameWithArity(identifier, arity), out var result))
            return result;

        // If this document doesn't even contain this identifier (escaped or non-escaped) we don't have to search it at all.
        if (!this.SyntaxTreeIndex.ProbablyContainsIdentifier(identifier))
            return [];

        // If the identifier was escaped in the file then we'll have to do a more involved search that actually
        // walks the root and checks all identifier tokens.
        //
        // otherwise, we can use the text of the document to quickly find candidates and test those directly.
        if (this.SyntaxTreeIndex.ProbablyContainsEscapedIdentifier(identifier))
        {
            return _identifierCache.GetOrAdd(
                new NameWithArity(identifier, arity),
                nameWithArity => FindMatchingIdentifierTokensFromTree(nameWithArity.Name, nameWithArity.Arity, cancellationToken));
        }

        return _identifierCache.GetOrAdd(
            new NameWithArity(identifier, arity),
            nameWithArity => FindMatchingTokensFromText(
                nameWithArity.Name,
                nameWithArity.Arity,
                static (identifier, arity, token, @this) => @this.IsMatch(identifier, arity, token),
                this, cancellationToken));
    }

    private bool IsMatch(string identifier, int arity, SyntaxToken token)
    {
        if (token.IsMissing || !this.SyntaxFacts.IsIdentifier(token) || !this.SyntaxFacts.TextMatch(token.ValueText, identifier))
        {
            return false;
        }

        if (arity > 0)
        {
            if (this.SyntaxFacts.IsGenericName(token.Parent))
            {
                this.SyntaxFacts.GetPartsOfGenericName(token.Parent, out _, out var typeArguments);
                return typeArguments.Count == arity;
            }
            else if (this.SyntaxFacts.IsUsingAliasDirective(token.Parent))
            {
                this.SyntaxFacts.GetPartsOfUsingAliasDirective(token.Parent, out _, out _, out var typeParameters, out _);
                return typeParameters.Count == arity;
            }

            // If there are other occations that using the wrong arity, we will remove them not here, but later.
        }

        return true;
    }

    private ImmutableArray<SyntaxToken> FindMatchingIdentifierTokensFromTree(
        string identifier, int arity, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<SyntaxToken>.GetInstance(out var result);
        using var obj = SharedPools.Default<Stack<SyntaxNodeOrToken>>().GetPooledObject();

        var stack = obj.Object;
        stack.Push(this.Root);

        while (stack.TryPop(out var current))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (current.AsNode(out var currentNode))
            {
                foreach (var child in currentNode.ChildNodesAndTokens().Reverse())
                    stack.Push(child);
            }
            else if (current.IsToken)
            {
                var token = current.AsToken();
                if (IsMatch(identifier, arity, token))
                    result.Add(token);

                if (token.HasStructuredTrivia)
                {
                    // structured trivia can only be leading trivia
                    foreach (var trivia in token.LeadingTrivia)
                    {
                        if (trivia.HasStructure)
                            stack.Push(trivia.GetStructure()!);
                    }
                }
            }
        }

        return result.ToImmutableAndClear();
    }

    private ImmutableArray<SyntaxToken> FindMatchingTokensFromText<TArgs>(
        string text, int arity, Func<string, int, SyntaxToken, TArgs, bool> isMatch, TArgs args, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<SyntaxToken>.GetInstance(out var result);

        var index = 0;
        while ((index = this.Text.IndexOf(text, index, this.SyntaxFacts.IsCaseSensitive)) >= 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var token = this.Root.FindToken(index, findInsideTrivia: true);
            var span = token.Span;
            if (span.Start == index && span.Length == text.Length && isMatch(text, arity, token, args))
                result.Add(token);

            var nextIndex = index + text.Length;
            nextIndex = Math.Max(nextIndex, token.SpanStart);
            index = nextIndex;
        }

        return result.ToImmutableAndClear();
    }

    public ImmutableArray<SyntaxNode> FindMatchingTupleTypeNodes(
        Document document,
        int tupleElementCount,
        CancellationToken cancellationToken)
    {
        return _tupleTypeCache.GetOrAdd(tupleElementCount, _ =>
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            return Root.DescendantNodes().WhereAsArray((SyntaxNode node, object? _) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!syntaxFacts.IsTupleType(node))
                {
                    return false;
                }

                syntaxFacts.GetPartsOfTupleType<SyntaxNode>(node, out var _, out var elements, out var _);
                if (elements.Count != tupleElementCount)
                {
                    return false;
                }

                return true;
            }, null).ToImmutableArrayOrEmpty();
        });
    }

    public ImmutableArray<SyntaxNode> FindMatchingArrayTypeNodes(
        Document document,
        int rank,
        CancellationToken cancellationToken)
    {
        return _arrayTypeCache.GetOrAdd(rank, _ =>
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            return Root.DescendantNodes().WhereAsArray((SyntaxNode node, object? _) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!syntaxFacts.IsArrayType(node))
                {
                    return false;
                }

                syntaxFacts.GetPartsOfArrayType<SyntaxNode>(node, out var _, out var rankSpecifiers);
                syntaxFacts.GetRankOfArrayRankSpecifier(rankSpecifiers[0], out var arrayRank);
                if (arrayRank != rank)
                {
                    return false;
                }

                return true;
            }, null).ToImmutableArrayOrEmpty();
        });
    }

    public ImmutableArray<SyntaxNode> FindMatchingPointerTypeNodes(
        Document document,
        CancellationToken cancellationToken)
    {
        return _pointerTypeCache.GetOrAdd(0, _ =>
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            return Root.DescendantNodes().WhereAsArray((SyntaxNode node, object? _) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!syntaxFacts.IsPointerType(node))
                {
                    return false;
                }

                return true;
            }, null).ToImmutableArrayOrEmpty();
        });
    }

    public ImmutableArray<SyntaxNode> FindMatchingFunctionPointerTypeNodes(
        Document document,
        int parameterCount,
        CancellationToken cancellationToken)
    {
        return _pointerTypeCache.GetOrAdd(parameterCount + 1, _ =>
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            return Root.DescendantNodes().WhereAsArray((SyntaxNode node, object? _) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!syntaxFacts.IsFunctionPointerType(node))
                {
                    return false;
                }

                syntaxFacts.GetParametersOfFunctionPointerType<SyntaxNode>(node, out var parameters);
                if (parameters.Count != parameterCount + 1)
                {
                    return false;
                }

                return true;
            }, null).ToImmutableArrayOrEmpty();
        });
    }

    public ImmutableArray<SyntaxToken> GetConstructorInitializerTokens(CancellationToken cancellationToken)
    {
        // this one will only get called when we know given document contains constructor initializer.
        // no reason to use text to check whether it exist first.
        if (_constructorInitializerCache.IsDefault)
            ImmutableInterlocked.InterlockedInitialize(ref _constructorInitializerCache, GetConstructorInitializerTokensWorker());

        return _constructorInitializerCache;

        ImmutableArray<SyntaxToken> GetConstructorInitializerTokensWorker()
        {
            var syntaxFacts = this.SyntaxFacts;
            using var _ = ArrayBuilder<SyntaxToken>.GetInstance(out var initializers);
            foreach (var constructor in syntaxFacts.GetConstructors(this.Root, cancellationToken))
            {
                foreach (var token in constructor.DescendantTokens(descendIntoTrivia: false))
                {
                    if (syntaxFacts.IsThisConstructorInitializer(token) || syntaxFacts.IsBaseConstructorInitializer(token))
                        initializers.Add(token);
                }
            }

            return initializers.ToImmutableAndClear();
        }
    }

    public ImmutableArray<SyntaxToken> GetNewKeywordTokens(CancellationToken cancellationToken)
    {
        if (_newKeywordsCache.IsDefault)
            ImmutableInterlocked.InterlockedInitialize(ref _newKeywordsCache, GetNewKeywordTokensWorker());

        return _newKeywordsCache;

        ImmutableArray<SyntaxToken> GetNewKeywordTokensWorker()
        {
            return this.FindMatchingTokensFromText(
                this.SyntaxFacts.GetText(this.SyntaxFacts.SyntaxKinds.NewKeyword),
                arity: 0,
                static (_, _, token, syntaxKinds) => token.RawKind == syntaxKinds.NewKeyword,
                this.SyntaxFacts.SyntaxKinds,
                cancellationToken);
        }
    }
}

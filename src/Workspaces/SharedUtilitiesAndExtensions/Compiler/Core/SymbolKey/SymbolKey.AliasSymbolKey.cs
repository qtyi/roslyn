// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis;

internal partial struct SymbolKey
{
    private sealed class AliasSymbolKey : AbstractSymbolKey<IAliasSymbol>
    {
        public static readonly AliasSymbolKey Instance = new();

        public sealed override void Create(IAliasSymbol symbol, SymbolKeyWriter visitor)
        {
            visitor.WriteString(symbol.Name);
            visitor.WriteInteger(symbol.Arity);

            // Mark that we're writing out an alias.  This way if we hit a alias type parameter
            // in our target, we won't recurse into it, but will instead only write out the type
            // parameter ordinal.  This happens with cases like Goo<T> = T;
            visitor.PushAlias(symbol);

            visitor.WriteSymbolKey(symbol.Target);

            // Done writing this alias.  Remove it from the set of aliases we're writing.
            visitor.PopAlias(symbol);

            visitor.WriteString(symbol.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree.FilePath ?? "");
        }

        protected sealed override SymbolKeyResolution Resolve(
            SymbolKeyReader reader, IAliasSymbol? contextualSymbol, out string? failureReason)
        {
            var name = reader.ReadRequiredString();
            var arity = reader.ReadInteger();

            SymbolKeyResolution targetResolution;
            string? targetFailureReason;
            using (reader.PushAlias(contextualSymbol))
            {
                targetResolution = reader.ReadSymbolKey(contextualSymbol?.Target, out targetFailureReason);
            }

            var filePath = reader.ReadRequiredString();

            if (targetFailureReason != null)
            {
                failureReason = $"({nameof(AliasSymbolKey)} {nameof(targetResolution)} failed -> {targetFailureReason})";
                return default;
            }

            var syntaxTree = reader.GetSyntaxTree(filePath);
            if (syntaxTree != null)
            {
                var target = targetResolution.GetAnySymbol();
                if (target != null)
                {
                    var semanticModel = reader.Compilation.GetSemanticModel(syntaxTree);
                    var result = Resolve(semanticModel, syntaxTree.GetRoot(reader.CancellationToken), name, arity, target, reader.CancellationToken);
                    if (result.HasValue)
                    {
                        failureReason = null;
                        return result.Value;
                    }
                }
            }

            failureReason = $"({nameof(AliasSymbolKey)} '{name}{(arity == 0 ? string.Empty : "`" + arity)}' not found)";
            return default;
        }

        private static SymbolKeyResolution? Resolve(
            SemanticModel semanticModel, SyntaxNode syntaxNode, string name, int arity, ISymbol target,
            CancellationToken cancellationToken)
        {
            // Don't call on the root compilation unit itself.  For top level programs this will be the synthesized
            // '<main>' entrypoint.
            if (syntaxNode is not ICompilationUnitSyntax)
            {
                var symbol = semanticModel.GetDeclaredSymbol(syntaxNode, cancellationToken);
                if (symbol != null)
                {
                    if (symbol is IAliasSymbol aliasSymbol)
                    {
                        if (aliasSymbol.Name == name &&
                            aliasSymbol.Arity == arity &&
                            SymbolEquivalenceComparer.Instance.Equals(aliasSymbol.Target, target))
                        {
                            return new SymbolKeyResolution(aliasSymbol);
                        }
                    }
                    else if (symbol.Kind != SymbolKind.Namespace)
                    {
                        // Don't recurse into anything except namespaces.  We can't find aliases
                        // any deeper than that.
                        return null;
                    }
                }
            }

            foreach (var child in syntaxNode.ChildNodesAndTokens())
            {
                if (child.AsNode(out var childNode))
                {
                    var result = Resolve(semanticModel, childNode, name, arity, target, cancellationToken);
                    if (result.HasValue)
                        return result;
                }
            }

            return null;
        }
    }
}

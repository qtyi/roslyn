// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(GenericAliasTargetCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(ExternAliasCompletionProvider))]
    [Shared]
    internal class GenericAliasTargetCompletionProvider : LSPCompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public GenericAliasTargetCompletionProvider()
        {
        }

        internal override string Language => LanguageNames.CSharp;

        public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
            => CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);

        public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.CommonTriggerCharacters;

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            try
            {
                var document = context.Document;
                var position = context.Position;
                var cancellationToken = context.CancellationToken;

                var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

                if (tree.IsInNonUserCode(position, cancellationToken))
                {
                    return;
                }

                var targetToken = tree
                    .FindTokenOnLeftOfPosition(position, cancellationToken)
                    .GetPreviousTokenIfTouchingWord(position);
                var usingDirectiveSyntax = targetToken.Parent?.FirstAncestorOrSelf<UsingDirectiveSyntax>();
                if (usingDirectiveSyntax == null)
                {
                    return;
                }

                if (targetToken != usingDirectiveSyntax.EqualsToken
                    && !usingDirectiveSyntax.NamespaceOrType.Span.IntersectsWith(position))
                {
                    return;
                }

                if (usingDirectiveSyntax.TypeParameterList is not { Parameters.Count: > 0 })
                {
                    return;
                }

                var syntaxContext = await context.GetSyntaxContextWithExistingSpeculativeModelAsync(document, cancellationToken).ConfigureAwait(false);
                if (!syntaxContext.IsTypeContext)
                {
                    return;
                }
                else if (syntaxContext.IsPossibleTupleContext &&
                    !targetToken.IsPossibleTupleOpenParenOrComma())
                {
                    // (A $$, ...)
                    // or
                    // (A a, B $$, ...)
                    return;
                }

                var typeParameters = usingDirectiveSyntax.TypeParameterList.Parameters
                    .Where(tp => !tp.Identifier.IsMissing)
                    .Select(tp => tp.Identifier.ValueText)
                    .ToSet();
                foreach (var typeParameter in typeParameters)
                {
                    context.AddItem(CommonCompletionItem.Create(
                        typeParameter, displayTextSuffix: "", CompletionItemRules.Default, glyph: Glyph.TypeParameter));
                }
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, ErrorSeverity.General))
            {
                // nop
            }
        }
    }
}

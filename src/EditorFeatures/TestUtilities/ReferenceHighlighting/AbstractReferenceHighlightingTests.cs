// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Highlighting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Xunit;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.UnitTests;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.ReferenceHighlighting
{
    [UseExportProvider]
    public abstract class AbstractReferenceHighlightingTests
    {
        internal async Task TestAsync(string markup)
        {
            foreach (var options in GetOptions())
            {
                await TestAsync(CreateWorkspace(markup, options), ImmutableDictionary<string, object>.Empty);
            }
        }

        protected async Task TestAsync(EditorTestWorkspace workspace, IImmutableDictionary<string, object> testOptions)
        {
            var testDocument = workspace.Documents.Single(doc => !doc.IsSourceGenerated);
            var expectedHighlightSpans = testDocument.SelectedSpans.ToList();
            if (testDocument.SourceCodeKind == SourceCodeKind.Regular)
            {
                if (testDocument.AnnotatedSpans.TryGetValue(nameof(SourceCodeKind.Script), out var scriptOnlySpan))
                {
                    expectedHighlightSpans.RemoveRange(scriptOnlySpan);
                }
            }
            else if (testDocument.SourceCodeKind == SourceCodeKind.Script)
            {
                if (testDocument.AnnotatedSpans.TryGetValue(nameof(SourceCodeKind.Regular), out var regularOnlySpan))
                {
                    expectedHighlightSpans.RemoveRange(regularOnlySpan);
                }
            }
            expectedHighlightSpans.Sort();

            var cursorSpan = testDocument.AnnotatedSpans["Cursor"].Single();
            var textSnapshot = testDocument.GetTextBuffer().CurrentSnapshot;
            var document = workspace.CurrentSolution.GetDocument(testDocument.Id);

            var tree = await document.GetSyntaxTreeAsync();

            // Check that every point within the span (inclusive) produces the expected set of
            // results.
            for (var i = 0; i <= cursorSpan.Length; i++)
            {
                var position = cursorSpan.Start + i;
                var highlightSpans = await GetHighlightSpans(document, position, testOptions);

                CheckSpans(tree, expectedHighlightSpans, highlightSpans);
            }
        }

        protected virtual async Task<IList<TextSpan>> GetHighlightSpans(Document document, int position, IImmutableDictionary<string, object> testOptions)
        {
            var workspace = Assert.IsType<TestWorkspace>(document.Project.Solution.Workspace);
            var service = Assert.IsType<HighlightingService>(workspace.ExportProvider.GetExportedValue<IHighlightingService>());

            var root = await document.GetRequiredSyntaxRootAsync(CancellationToken.None);

            var highlightSpans = new List<TextSpan>();
            service.AddHighlights(root, position, highlightSpans, CancellationToken.None);

            return highlightSpans;
        }

        private static void CheckSpans(SyntaxTree tree, IList<TextSpan> expectedHighlightSpans, IList<TextSpan> highlightSpans)
        {
            Assert.Equal(expectedHighlightSpans.Select(formatSpan), highlightSpans.Select(formatSpan));

            string formatSpan(TextSpan span)
            {
                var lineSpan = tree.GetLineSpan(span).Span;
                var start = lineSpan.Start;
                var end = lineSpan.End;
                var text = tree.GetText().ToString(span);
                return $"({start.Line + 1},{start.Character + 1})-({end.Line + 1},{end.Character + 1}): '{text}'";
            }
        }

        protected abstract EditorTestWorkspace CreateWorkspace(string markup, ParseOptions options);

        protected abstract IEnumerable<ParseOptions> GetOptions();
    }
}

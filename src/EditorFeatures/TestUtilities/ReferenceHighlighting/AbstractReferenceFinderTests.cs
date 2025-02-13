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
using EnvDTE;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Xunit;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.ReferenceHighlighting
{
    [UseExportProvider]
    public abstract class AbstractReferenceFinderTests : AbstractReferenceHighlightingTests
    {
        internal abstract IReferenceFinder GetReferenceFinder();

        internal abstract FindReferencesSearchOptions GetSearchOptions();

        internal async Task TestAsync(string markup,
            IEnumerable<IReferenceFinder> additionalReferenceFinders)
        {
            var testOptions = CreateTestOptions(
                additionalReferenceFinders: additionalReferenceFinders);
            foreach (var options in GetOptions())
            {
                await TestAsync(CreateWorkspace(markup, options), testOptions);
            }
        }

        protected override async Task<IList<TextSpan>> GetHighlightSpans(Document document, int position, IImmutableDictionary<string, object> testOptions)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(CancellationToken.None);
            var solution = document.Project.Solution;
            var symbol = await SymbolFinder.FindSymbolAtPositionAsync(
            semanticModel, position, solution.Services, CancellationToken.None);
            Assert.NotNull(symbol);

            var progress = new StreamingProgressCollector();
            var finders = GetReferenceFinders();
            var engine = new FindReferencesSearchEngine(
                solution,
                ImmutableHashSet.Create(document),
                finders,
                progress,
                GetSearchOptions());

            await engine.FindReferencesAsync(symbol, CancellationToken.None);

            var highlightSpans = new SortedSet<TextSpan>();
            foreach (var referencedSymbol in progress.GetReferencedSymbols())
            {
                foreach (var referenceLocation in referencedSymbol.Locations)
                {
                    highlightSpans.Add(referenceLocation.Location.SourceSpan);
                }
            }

            return highlightSpans.ToList();

            ImmutableArray<IReferenceFinder> GetReferenceFinders()
            {
                using var _ = PooledHashSet<IReferenceFinder>.GetInstance(out var finders);

                finders.Add(GetReferenceFinder());
                if (TryGetTestOption<IEnumerable<IReferenceFinder>>(testOptions, TestOptionKeys.AdditionalReferenceFinders, out var additionalReferenceFinders))
                {
                    finders.UnionWith(additionalReferenceFinders);
                }

                return finders.ToImmutableArray();
            }
        }

        #region TestOptions

        protected static class TestOptionKeys
        {
            public const string AdditionalReferenceFinders = nameof(AdditionalReferenceFinders);
        }

        internal static IImmutableDictionary<string, object> CreateTestOptions(
            IEnumerable<IReferenceFinder>? additionalReferenceFinders = null)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, object>();

            if (additionalReferenceFinders is not null)
            {
                builder.Add(TestOptionKeys.AdditionalReferenceFinders, additionalReferenceFinders.ToImmutableHashSet());
            }

            return builder.ToImmutable();
        }

        internal static bool TryGetTestOption<T>(IImmutableDictionary<string, object> testOptions, string testOptionKey, [NotNullWhen(true)] out T? testOption)
        {
            if (testOptions.TryGetValue(testOptionKey, out var value) && value is T)
            {
                testOption = (T)value;
                return true;
            }

            testOption = default;
            return false;
        }

        #endregion
    }
}

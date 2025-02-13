// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.ReferenceHighlighting;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ReferenceHighlighting
{
    public abstract class AbstractCSharpReferenceFinderTests : AbstractReferenceFinderTests
    {
        protected sealed override EditorTestWorkspace CreateWorkspace(string markup, ParseOptions options)
            => EditorTestWorkspace.CreateCSharp(markup, options);

        protected override IEnumerable<ParseOptions> GetOptions()
        {
            yield return Options.Regular;
            yield return Options.Script;
        }

        internal override FindReferencesSearchOptions GetSearchOptions()
            => FindReferencesSearchOptions.Default;

        internal async Task TestWithGlobalUsingsAsync(string markup, string globalUsings,
            IEnumerable<IReferenceFinder>? additionalReferenceFinders = null)
        {
            var testOptions = CreateTestOptions(
                additionalReferenceFinders: additionalReferenceFinders);
            foreach (var options in GetOptions())
            {
                await TestAsync(EditorTestWorkspace.CreateCSharp(new[] { markup }, new[] { globalUsings }, options), testOptions);
            }
        }
    }
}

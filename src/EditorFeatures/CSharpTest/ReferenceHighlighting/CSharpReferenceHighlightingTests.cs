// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.ReferenceHighlighting;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ReferenceHighlighting
{
    public partial class CSharpReferenceHighlightingTests : AbstractReferenceHighlightingTests
    {
        protected override TestWorkspace CreateWorkspace(string markup, ParseOptions options)
            => TestWorkspace.CreateCSharp(markup, options);

        protected override IEnumerable<ParseOptions> GetOptions()
        {
            yield return Options.Regular;
            yield return Options.Script;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// An array of TextChange created by an <see cref="ISourceGenerator"/>
    /// </summary>
    internal readonly struct ModifiedTexts
    {
        public ImmutableArray<TextChange> TextChanges { get; }

        public string FilePath { get; }

        public ModifiedTexts(string filePath, ImmutableArray<TextChange> textChanges)
        {
            this.FilePath = filePath;
            this.TextChanges = textChanges;
        }
    }
}

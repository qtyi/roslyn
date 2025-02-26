// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class ModifiedTextsCollection
    {
        // global pool, compare file path and ignore case.
        private static readonly ObjectPool<PooledDictionary<string, ArrayBuilder<TextChange>>> s_poolInstance = PooledDictionary<string, ArrayBuilder<TextChange>>.CreatePool(StringComparer.OrdinalIgnoreCase);

        private readonly PooledDictionary<string, ArrayBuilder<TextChange>> _textsModified;

        internal ModifiedTextsCollection()
        {
            _textsModified = s_poolInstance.Allocate();
            Debug.Assert(_textsModified.Count == 0);
        }

        public void Add(string filePath, TextChange textChange)
        {
            var builder = _textsModified.GetOrAdd(filePath, ArrayBuilder<TextChange>.GetInstance);
            AddInternal(builder, textChange);
        }

        public void Add(ModifiedTexts modifiedTexts) => AddRange(modifiedTexts.FilePath, modifiedTexts.TextChanges);

        public void AddRange(string filePath, IEnumerable<TextChange> textChanges)
        {
            var builder = _textsModified.GetOrAdd(filePath, ArrayBuilder<TextChange>.GetInstance);
            foreach (var textChange in textChanges)
            {
                AddInternal(builder, textChange);
            }
        }

        public void AddRange(IEnumerable<ModifiedTexts> modifiedTexts)
        {
            foreach (var modifiedText in modifiedTexts)
            {
                AddRange(modifiedText.FilePath, modifiedText.TextChanges);
            }
        }

        private static void AddInternal(ArrayBuilder<TextChange> builder, TextChange textChange)
        {
            foreach (var exists in builder)
            {
                bool overlaps;
                if (exists.Span.IsEmpty)
                {
                    if (textChange.Span.IsEmpty)
                        overlaps = exists.Span.Start == textChange.Span.Start;
                    else
                        overlaps = textChange.Span.Contains(exists.Span.Start);
                }
                else
                {
                    if (textChange.Span.IsEmpty)
                        overlaps = exists.Span.Contains(textChange.Span.Start);
                    else
                        overlaps = exists.Span.OverlapsWith(textChange.Span);
                }

                if (overlaps)
                {
                    // we don't allow overlapping text changes.
                    throw new InvalidOperationException(string.Format(CodeAnalysisResources.ModifiedTextSpansOverlap, textChange.Span, exists.Span));
                }
            }
            builder.Add(textChange);
        }

        public void CopyTo(ModifiedTextsCollection mtc)
        {
            foreach ((var filePath, var textChanges) in _textsModified)
            {
                var builder = mtc._textsModified.GetOrAdd(filePath, ArrayBuilder<TextChange>.GetInstance);
                // we know the TextChanges are valid, but we do need to check that they
                // don't collide with any we already have
                if (builder.Count == 0)
                {
                    builder.AddRange(textChanges);
                }
                else
                {
                    foreach (var textChange in textChanges)
                    {
                        AddInternal(builder, textChange);
                    }
                }
            }
        }

        internal ImmutableArray<ModifiedTexts> ToImmutableAndFree()
        {
            var result = ToImmutable();
            Free();
            return result;
        }

        internal ImmutableArray<ModifiedTexts> ToImmutable()
        {
            var builder = ArrayBuilder<ModifiedTexts>.GetInstance();
            foreach ((var filePath, var textChanges) in _textsModified)
            {
                builder.Add(new(filePath, textChanges.ToImmutable()));
            }
            return builder.ToImmutableAndFree();
        }

        internal void Free()
        {
            foreach (var builder in _textsModified.Values)
            {
                builder.Free();
            }
            _textsModified.Free();
        }
    }
}

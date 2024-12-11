// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class GenericAliasTargetCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType()
            => typeof(GenericAliasTargetCompletionProvider);

        [Fact]
        public async Task GenericAliase1()
        {
            var markup = @"
using Dic<T> = $$";
            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact]
        public async Task GenericAliase2()
        {
            var markup = @"
using A<T1, T2, TRest> = $$";
            await VerifyItemExistsAsync(markup, "T1");
            await VerifyItemExistsAsync(markup, "T2");
            await VerifyItemExistsAsync(markup, "TRest");
        }

        [Fact]
        public async Task GenericAliaseInTypeArgumentList1()
        {
            var markup = @"
using Dic<T> = System.Collections.Dictionary<$$";
            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact]
        public async Task GenericAliaseInTypeArgumentList2()
        {
            var markup = @"
using Dic<T> = System.Collections.Dictionary<T, $$";
            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact]
        public async Task GenericAliaseInTuple1()
        {
            var markup = @"
using A<T> = ($$";
            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact]
        public async Task GenericAliaseInTuple2()
        {
            var markup = @"
using A<T> = (T, $$";
            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact]
        public async Task GenericAliaseDuplicateTypeParameters()
        {
            var markup = @"
using A<T1, T1> = $$";
            await VerifyItemExistsAsync(markup, "T1");
        }

        [Fact]
        public async Task GenericAliaseNotInTypeContext1()
        {
            var markup = @"
using A<T> = System.Math.$$";
            await VerifyItemIsAbsentAsync(markup, "T");
        }

        [Fact]
        public async Task GenericAliaseNotInTypeContext2()
        {
            var markup = @"
using A<T> = System.Comparison<T>$$";
            await VerifyItemIsAbsentAsync(markup, "T");
        }

        [Fact]
        public async Task GenericAliaseNotInTypeContext3()
        {
            var markup = @"
using A<T> = System.Comparison<T $$";
            await VerifyItemIsAbsentAsync(markup, "T");
        }

        [Fact]
        public async Task GenericAliaseNotInTypeContext4()
        {
            var markup = @"
using A<T1, T2> = System.Comparison<T1, T2 $$";
            await VerifyItemIsAbsentAsync(markup, "T1");
            await VerifyItemIsAbsentAsync(markup, "T2");
        }

        [Fact]
        public async Task GenericAliaseNotInTypeContext5()
        {
            var markup = @"
using A<T> = (T $$";
            await VerifyItemIsAbsentAsync(markup, "T");
        }

        [Fact]
        public async Task GenericAliaseNotInTypeContext6()
        {
            var markup = @"
using A<T1, T2> = (T1 arg1, T2 $$";
            await VerifyItemIsAbsentAsync(markup, "T1");
            await VerifyItemIsAbsentAsync(markup, "T2");
        }
    }
}

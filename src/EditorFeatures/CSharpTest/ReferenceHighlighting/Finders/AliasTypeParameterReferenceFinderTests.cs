// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ReferenceHighlighting
{
    [Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)]
    public class AliasTypeParameterReferenceFinderTests : AbstractCSharpReferenceFinderTests
    {
        internal override IReferenceFinder GetReferenceFinder()
            => ReferenceFinders.DefaultReferenceFinders.OfType<AliasTypeParameterSymbolReferenceFinder>().Single();

        [Fact]
        public async Task TestInName1()
        {
            await TestAsync(
                """
                using X<T> = {|Cursor:[|T|]|}[];
                """);
        }

        [Fact]
        public async Task TestInName2()
        {
            await TestAsync(
                """
                using unsafe X<T> = {|Cursor:[|T|]|}*;
                """);
        }

        [Fact]
        public async Task TestInName3()
        {
            await TestAsync(
                """
                using X<T> = ({|Cursor:[|T|]|}, [|T|]);
                """);
        }

        [Fact]
        public async Task TestInName4()
        {
            await TestAsync(
                """
                using X<T> = System.Collections.Generic.List<{|Cursor:[|T|]|}>;
                """);
        }

        [Fact]
        public async Task TestInDeclaration1()
        {
            await TestAsync(
                """
                using X<{|Cursor:T|}> = [|T|][];
                """);
        }

        [Fact]
        public async Task TestInDeclaration2()
        {
            await TestAsync(
                """
                using unsafe X<{|Cursor:T|}> = [|T|]*;
                """);
        }

        [Fact]
        public async Task TestInDeclaration3()
        {
            await TestAsync(
                """
                using X<{|Cursor:T|}> = ([|T|], [|T|]);
                """);
        }

        [Fact]
        public async Task TestInDeclaration4()
        {
            await TestAsync(
                """
                using X<{|Cursor:T|}> = System.Collections.Generic.List<[|T|]>;
                """);
        }

        [Fact]
        public async Task TestManyTypeParameters()
        {
            await TestAsync(
                """
                using X<T1, {|Cursor:T2|}> = ([|T2|], T1);
                """);
        }

        [Fact]
        public async Task TestDuplicateTypeParametersInName1()
        {
            await TestAsync(
                """
                using X<T1, T1> = ({|Cursor:[|T1|]|}, [|T1|]);
                """);
        }

        [Fact]
        public async Task TestDuplicateTypeParametersInName2()
        {
            await TestAsync(
                """
                using X<T1, T1> = ([|T1|], {|Cursor:[|T1|]|});
                """);
        }

        [Fact]
        public async Task TestDuplicateTypeParametersInDeclaration1()
        {
            await TestAsync(
                """
                using X<{|Cursor:T1|}, T1> = ([|T1|], [|T1|]);
                """);
        }

        [Fact]
        public async Task TestDuplicateTypeParametersInDeclaration2()
        {
            await TestAsync(
                """
                using X<T1, {|Cursor:T1|}> = ([|T1|], [|T1|]);
                """);
        }

        [Fact]
        public async Task TestSameNameInDifferentUsingDirectives()
        {
            await TestAsync(
                """
                using X<T1> = T1;
                using X<T1, T2> = ({|Cursor:[|T1|]|}, T2);
                using Y<T1, T2> = (T1, T2);
                """);
        }

        [Fact]
        public async Task TestUsingItsTypeParameter1()
        {
            await TestAsync(
                """
                using X<T> = {|Cursor:[|T|]|};
                """);
        }

        [Fact]
        public async Task TestUsingItsTypeParameter2()
        {
            await TestAsync(
                """
                using {|Cursor:X|}<T> = [|T|];
                """);
        }

        [Fact]
        public async Task TestUsingOuterAliasWhichUsingItsTypeParameter1()
        {
            await TestAsync(
                """
                using X<{|Cursor:T|}> = [|T|];

                namespace N
                {
                    using Y<T> = [|X|]<T>;
                }
                """);
        }

        [Fact]
        public async Task TestUsingOuterAliasWhichUsingItsTypeParameter2()
        {
            await TestAsync(
                """
                using {|Cursor:X|}<T> = [|T|];
                
                namespace N
                {
                    using Y<T> = [|X|]<T>;
                }
                """);
        }
    }
}

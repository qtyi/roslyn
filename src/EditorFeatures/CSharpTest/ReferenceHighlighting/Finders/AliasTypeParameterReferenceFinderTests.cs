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
        public Task TestInName1()
            => TestAsync(
                """
                using X<T> = {|Cursor:[|T|]|}[];
                """);

        [Fact]
        public Task TestInName2()
            => TestAsync(
                """
                using unsafe X<T> = {|Cursor:[|T|]|}*;
                """);

        [Fact]
        public Task TestInName3()
            => TestAsync(
                """
                using X<T> = ({|Cursor:[|T|]|}, [|T|]);
                """);

        [Fact]
        public Task TestInName4()
            => TestAsync(
                """
                using X<T> = System.Collections.Generic.List<{|Cursor:[|T|]|}>;
                """);

        [Fact]
        public Task TestInDeclaration1()
            => TestAsync(
                """
                using X<{|Cursor:T|}> = [|T|][];
                """);

        [Fact]
        public Task TestInDeclaration2()
            => TestAsync(
                """
                using unsafe X<{|Cursor:T|}> = [|T|]*;
                """);

        [Fact]
        public Task TestInDeclaration3()
            => TestAsync(
                """
                using X<{|Cursor:T|}> = ([|T|], [|T|]);
                """);

        [Fact]
        public Task TestInDeclaration4()
            => TestAsync(
                """
                using X<{|Cursor:T|}> = System.Collections.Generic.List<[|T|]>;
                """);

        [Fact]
        public Task TestManyTypeParameters()
            => TestAsync(
                """
                using X<T1, {|Cursor:T2|}> = ([|T2|], T1);
                """);

        [Fact]
        public Task TestDuplicateTypeParametersInName1()
            => TestAsync(
                """
                using X<T1, T1> = ({|Cursor:[|T1|]|}, [|T1|]);
                """);

        [Fact]
        public Task TestDuplicateTypeParametersInName2()
            => TestAsync(
                """
                using X<T1, T1> = ([|T1|], {|Cursor:[|T1|]|});
                """);

        [Fact]
        public Task TestDuplicateTypeParametersInDeclaration1()
            => TestAsync(
                """
                using X<{|Cursor:T1|}, T1> = ([|T1|], [|T1|]);
                """);

        [Fact]
        public Task TestDuplicateTypeParametersInDeclaration2()
            => TestAsync(
                """
                using X<T1, {|Cursor:T1|}> = ([|T1|], [|T1|]);
                """);

        [Fact]
        public Task TestSameNameInDifferentUsingDirectives()
            => TestAsync(
                """
                using X<T1> = T1;
                using X<T1, T2> = ({|Cursor:[|T1|]|}, T2);
                using Y<T1, T2> = (T1, T2);
                """);

        [Fact]
        public Task TestUsingItsTypeParameter1()
            => TestAsync(
                """
                using X<T> = {|Cursor:[|T|]|};
                """);

        [Fact]
        public Task TestUsingItsTypeParameter2()
            => TestAsync(
                """
                using {|Cursor:X|}<T> = [|T|];
                """);

        [Fact]
        public Task TestUsingOuterAliasWhichUsingItsTypeParameter1()
            => TestAsync(
                """
                using X<{|Cursor:T|}> = [|T|];

                namespace N
                {
                    using Y<T> = [|X|]<T>;
                }
                """);

        [Fact]
        public Task TestUsingOuterAliasWhichUsingItsTypeParameter2()
            => TestAsync(
                """
                using {|Cursor:X|}<T> = [|T|];
                
                namespace N
                {
                    using Y<T> = [|X|]<T>;
                }
                """);
    }
}

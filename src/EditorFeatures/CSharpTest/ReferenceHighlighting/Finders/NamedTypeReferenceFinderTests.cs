// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ReferenceHighlighting
{
    [Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)]
    public partial class NamedTypeReferenceFinderTests : AbstractCSharpReferenceFinderTests
    {
        internal override IReferenceFinder GetReferenceFinder()
            => ReferenceFinders.DefaultReferenceFinders.OfType<NamedTypeSymbolReferenceFinder>().Single();

        [Fact]
        public Task TestNonGeneric()
            => TestAsync(
                """
                class C<T1, T2> {}
                class C<T> : C<C<[|C|]>, C<[|C|]>> {}
                class {|Cursor:C|} : C<[|C|]> {}
                """);

        [Fact]
        public Task TestGeneric()
            => TestAsync(
                """
                class C<T1, T2> {}
                class {|Cursor:C|}<T> : C<[|C|]<C>, [|C|]<C>> {}
                class C : [|C|]<C> {}
                """);

        [Fact]
        public Task TestUsingAlias1()
            => TestAsync(
                """
                using X = [|C|];
                
                class {|Cursor:C|} {}
                """);

        [Fact]
        public Task TestUsingAlias2()
            => TestAsync(
                """
                using X = [|C|];
                
                class {|Cursor:C|} {}

                namespace N
                {
                    using Y = [|X|];
                }
                """);

        [Fact]
        public Task TestUsingAlias3()
            => TestAsync(
                """
                using {|Cursor:X|} = [|C|];
                
                class C {}

                namespace N
                {
                    using Y = [|X|];
                }
                """);

        [Fact]
        public Task TestUsingGenericAlias1()
            => TestAsync(
                """
                using X = C;
                using X<T> = [|C|]<T>;
                
                class {|Cursor:C|}<T> {}
                class C : [|C|]<C> {}
                """);

        [Fact]
        public Task TestUsingGenericAlias2()
            => TestAsync(
                """
                using X = C;
                using X<T> = [|C|]<T>;
                
                class {|Cursor:C|}<T> {}
                class C : [|C|]<C> {}

                namespace N
                {
                    using Y = X;
                    using Y<T> = [|X|]<T>;
                }
                """);

        [Fact]
        public Task TestUsingGenericAlias3()
            => TestAsync(
                """
                using X = C;
                using {|Cursor:X|}<T> = [|C|]<T>;
                
                class C<T> {}
                class C : [|C|]<C> {}

                namespace N
                {
                    using Y = X;
                    using Y<T> = [|X|]<T>;
                }
                """);

        [Fact]
        public Task TestUsingAliasWithGlobalUsings1()
            => TestWithGlobalUsingsAsync(
                markup:
                """
                using Y = [|C|];
                
                class {|Cursor:C|} {}
                
                namespace N
                {
                    using Z1 = {|Regular:[|X|]|};
                    using Z2 = [|Y|];
                }
                """,
                globalUsings:
                """
                global using X = C;
                """);

        [Fact]
        public Task TestUsingGenericAliasWithGlobalUsings2()
            => TestWithGlobalUsingsAsync(
                markup:
                """
                using Y = C;
                using Y<T> = [|C|]<T>;
                
                class {|Cursor:C|}<T> {}
                class C : [|C|]<C> {}
                
                namespace N
                {
                    using Z1 = X;
                    using Z1<T> = {|Regular:[|X|]|}<T>;
                    using Z2 = Y;
                    using Z2<T> = [|Y|]<T>;
                }
                """,
                globalUsings:
                """
                global using X = C;
                global using X<T> = C<T>;
                """);
    }
}

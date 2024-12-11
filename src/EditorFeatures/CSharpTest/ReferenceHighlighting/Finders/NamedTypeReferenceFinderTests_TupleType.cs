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
    partial class NamedTypeReferenceFinderTests
    {
        [Fact]
        public async Task TestValueTupleUsingAlias1()
        {
            await TestAsync(
                """
                using X = System.{|Cursor:[|ValueTuple|]|}<object, object>;

                using Y1 = System.ValueTuple<int>;
                using Y1<T> = System.ValueTuple<T>;

                using Y2 = System.[|ValueTuple|]<int, bool>;
                using Y2<T> = System.[|ValueTuple|]<T, T>;
                using Y2<T1, T2> = System.[|ValueTuple|]<T2, T1>;

                using Y3 = System.ValueTuple<int, bool, string>;
                using Y3<T> = System.ValueTuple<T, T, T>;
                using Y3<T1, T2, T3> = System.ValueTuple<T3, T2, T1>;
                """);
        }

        [Fact]
        public async Task TestValueTupleUsingAlias2()
        {
            await TestAsync(
                """
                using X = System.{|Cursor:[|ValueTuple|]|}<object, object>;

                using Y1 = System.ValueTuple<int>;
                using Y1<T> = System.ValueTuple<T>;

                using Y2 = [|(int, bool)|];
                using Y2<T> = [|(T, T)|];
                using Y2<T1, T2> = [|(T2, T1)|];

                using Y3 = (int, bool, string);
                using Y3<T> = (T, T, T);
                using Y3<T1, T2, T3> = (T3, T2, T1);
                """);
        }

        [Fact]
        public async Task TestTupleUsingAlias1()
        {
            await TestAsync(
                """
                using {|Cursor:X|} = [|(object, object)|];

                using Y1 = System.ValueTuple<int>;
                using Y1<T> = System.ValueTuple<T>;

                using Y2 = System.[|ValueTuple|]<int, bool>;
                using Y2<T> = System.[|ValueTuple|]<T, T>;
                using Y2<T1, T2> = System.[|ValueTuple|]<T2, T1>;

                using Y3 = System.ValueTuple<int, bool, string>;
                using Y3<T> = System.ValueTuple<T, T, T>;
                using Y3<T1, T2, T3> = System.ValueTuple<T3, T2, T1>;
                """);
        }

        [Fact]
        public async Task TestTupleUsingAlias2()
        {
            await TestAsync(
                """
                using {|Cursor:X|} = [|(object, object)|];

                using Y1 = System.ValueTuple<int>;
                using Y1<T> = System.ValueTuple<T>;

                using Y2 = [|(int, bool)|];
                using Y2<T> = [|(T, T)|];
                using Y2<T1, T2> = [|(T2, T1)|];

                using Y3 = (int, bool, string);
                using Y3<T> = (T, T, T);
                using Y3<T1, T2, T3> = (T3, T2, T1);
                """);
        }

        [Fact]
        public async Task TestValueTupleUsingAliasWithGlobalUsings1()
        {
            await TestWithGlobalUsingsAsync(
                markup:
                """
                using {|Cursor:Y|} = [|(object, object)|];

                namespace N
                {
                    using Z1 = [|X|];
                    using Z2 = [|Y|];
                }
                """,
                globalUsings:
                """
                global using X = System.ValueTuple<object, object>;
                """);
        }

        [Fact]
        public async Task TestTupleUsingAliasWithGlobalUsings1()
        {
            await TestWithGlobalUsingsAsync(
                markup:
                """
                using {|Cursor:Y|} = [|(object, object)|];

                namespace N
                {
                    using Z1 = [|X|];
                    using Z2 = [|Y|];
                }
                """,
                globalUsings:
                """
                global using X = (object, object);
                """);
        }
    }
}

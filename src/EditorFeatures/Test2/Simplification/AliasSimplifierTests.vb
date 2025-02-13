' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Simplification
    <Trait(Traits.Feature, Traits.Features.Simplification)>
    Public Class AliasSimplifierTests
        Inherits AbstractSimplificationTests

        <Fact>
        Public Async Function TestDoNotSimplifyGenericAlias() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    using A&lt;T&gt; = T[];
                    namespace Root 
                    {
                        class C 
                        {
                            {|Simplify:A&lt;int&gt;|} arr;
                        }
                    }
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                    using A&lt;T&gt; = T[];
                    namespace Root 
                    {
                        class C 
                        {
                            A&lt;int&gt; arr;
                        }
                    }
                </text>

            Await TestAsync(input, expected)
        End Function

        <Fact>
        Public Async Function TestSimplifyArray01() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    using A&lt;T&gt; = T[];
                    namespace Root 
                    {
                        class C 
                        {
                            {|Simplify:int[]|} arr;
                        }
                    }
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                    using A&lt;T&gt; = T[];
                    namespace Root 
                    {
                        class C 
                        {
                            A&lt;int&gt; arr;
                        }
                    }
                </text>

            Await TestAsync(input, expected)
        End Function

        <Fact>
        Public Async Function TestSimplifyArray02() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    using A&lt;T&gt; = T[,];
                    namespace Root 
                    {
                        class C 
                        {{|SimplifyParent:
                            int[] arr1;
                            int[,] arr2;
                            int[,,] arr3;
                        |}}
                    }
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                    using A&lt;T&gt; = T[,];
                    namespace Root 
                    {
                        class C 
                        {
                            int[] arr1;
                            A&lt;int&gt; arr2;
                            int[,,] arr3;
                        }
                    }
                </text>

            Await TestAsync(input, expected)
        End Function

        <Fact>
        Public Async Function TestSimplifyArray03() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    using A&lt;T&gt; = T[];
                    namespace Root 
                    {
                        class C 
                        {
                            {|Simplify:int[][]|} arr;
                        }
                    }
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                    using A&lt;T&gt; = T[];
                    namespace Root 
                    {
                        class C 
                        {
                            A&lt;A&lt;int&gt;&gt; arr;
                        }
                    }
                </text>

            Await TestAsync(input, expected)
        End Function

        <Fact>
        Public Async Function TestSimplifyPointer01() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    using unsafe P&lt;T&gt; = T* where T : unmanaged;
                    namespace Root 
                    {
                        unsafe class C 
                        {
                            {|Simplify:int*|} p;
                        }
                    }
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                    using unsafe P&lt;T&gt; = T* where T : unmanaged;
                    namespace Root 
                    {
                        unsafe class C 
                        {
                            P&lt;int&gt; p;
                        }
                    }
                </text>

            Await TestAsync(input, expected)
        End Function

        <Fact>
        Public Async Function TestSimplifyPointer02() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    using unsafe P&lt;T&gt; = T* where T : unmanaged;
                    namespace Root 
                    {
                        unsafe class C 
                        {
                            {|Simplify:int**|} p;
                        }
                    }
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                    using unsafe P&lt;T&gt; = T* where T : unmanaged;
                    namespace Root 
                    {
                        unsafe class C 
                        {
                            P&lt;P&lt;int&gt;&gt; p;
                        }
                    }
                </text>

            Await TestAsync(input, expected)
        End Function

        <Fact>
        Public Async Function TestSimplifyFunctionPointer01() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    using unsafe FP&lt;T&gt; = delegate*&lt;T, T, T&gt;;
                    namespace Root 
                    {
                        unsafe class C 
                        {{|Simplify:
                            delegate*&lt;int, int, int&gt; fp1;
                            delegate*&lt;int, int, void&gt; fp2;
                            delegate*&lt;int, string, int&gt; fp3;
                            delegate*&lt;bool, int, int&gt; fp4;
                        |}}
                    }
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                    using unsafe FP&lt;T&gt; = delegate*&lt;T, T, T&gt;;
                    namespace Root 
                    {
                        unsafe class C 
                        {
                            FP&lt;int&gt; fp1;
                            delegate*&lt;int, int, void&gt; fp2;
                            delegate*&lt;int, string, int&gt; fp3;
                            delegate*&lt;bool, int, int&gt; fp4;
                        }
                    }
                </text>

            Await TestAsync(input, expected)
        End Function

        <Fact>
        Public Async Function TestSimplifyTuple01() As Task
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    using T&lt;E1, E2&gt; = (E1, E2);
                    namespace Root 
                    {
                        unsafe class C 
                        {
                            {|Simplify:(byte, bool)|} tuple;
                        }
                    }
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                    using T&lt;E1, E2&gt; = (E1, E2);
                    namespace Root 
                    {
                        unsafe class C 
                        {
                            T&lt;byte,bool&gt; tuple;
                        }
                    }
                </text>

            Await TestAsync(input, expected)
        End Function

    End Class
End Namespace

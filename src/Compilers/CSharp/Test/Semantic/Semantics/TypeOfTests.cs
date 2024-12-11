// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class TypeofTests : CSharpTestBase
    {
        [Fact]
        public void ProducesNoErrorsDuringBinding()
        {
            var source = @"
using A<T> = object;                                        // generic alias targets determined non-generic named type.
using B<T> = int[];                                         // generic alias targets determined array type.
using unsafe C<T> = int*;                                   // generic alias targets determined pointer type.
using unsafe D<T> = delegate*<int, void>;                   // generic alias targets determined function pointer type.
using E<T> = (T, T, T);                                     // generic alias targets tuple type(generic named type) whose type arguments are all alias type parameter.
using F<T> = System.Collections.Generic.Dictionary<T, T>;   // generic alias targets generic named type whose type arguments are all alias type parameter.


static class Program
{
    static unsafe void Main()
    {
        bool[] tasks = new bool[]
        {
            typeof(A<>) == typeof(object),
            typeof(B<>) == typeof(int[]),
            typeof(C<>) == typeof(int*),
            typeof(D<>) == typeof(delegate*<int, void>),
            typeof(E<>) == typeof(System.ValueTuple<,,>),
            typeof(F<>) == typeof(System.Collections.Generic.Dictionary<,>)
        };

        foreach (bool task in tasks)
        {
            if (!task)
            {
                System.Console.WriteLine(""Failed"");
                return;
            }
        }
        System.Console.WriteLine(""Passed"");
    }
}
";
            var expectedOutput = @"Passed";
            var compilation = CreateCompilation(source, options: TestOptions.UnsafeDebugExe);
            CompileAndVerifyCommon(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ProducesErrorsDuringBinding()
        {
            CreateCompilation(@"
using A<T> = T;                                                 // generic alias targets its type parameter.
using B<T> = T[];                                               // generic alias targets array type whose element type is alias type parameter.
using unsafe C<T> = T* where T : unmanaged;                     // generic alias targets pointer type whose point-to type is alias type parameter.
using unsafe D<T> = delegate*<T, T, T>;                         // generic alias targets function pointer type whose return type/parameter types are alias type parameter.
using E<T> = (int, T);                                          // generic alias targets tuple type(generic named type) whose type argument contains both determined types and alias type parameters.
using F<T> = System.Collections.Generic.Dictionary<T, (T, T)>;  // generic alias targets generic named type whose type argument contains both determined types(even though they're tuple types whose type arguments are all alias type parameter) and alias type parameters.

static class Program
{
    static void Main()
    {
        var a = typeof(A<>);
        var b = typeof(B<>);
        var c = typeof(C<>);
        var d = typeof(D<>);
        var e = typeof(E<>);
        var f = typeof(F<>);
    }
}
", options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
                // (13,24): error CS7003: Unexpected use of an unbound generic name
                //         var a = typeof(A<>);
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "A<>").WithLocation(13, 24),
                // (14,24): error CS7003: Unexpected use of an unbound generic name
                //         var b = typeof(B<>);
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "B<>").WithLocation(14, 24),
                // (15,24): error CS7003: Unexpected use of an unbound generic name
                //         var c = typeof(C<>);
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "C<>").WithLocation(15, 24),
                // (16,24): error CS7003: Unexpected use of an unbound generic name
                //         var d = typeof(D<>);
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "D<>").WithLocation(16, 24),
                // (17,24): error CS7003: Unexpected use of an unbound generic name
                //         var e = typeof(E<>);
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "E<>").WithLocation(17, 24),
                // (18,24): error CS7003: Unexpected use of an unbound generic name
                //         var f = typeof(F<>);
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "F<>").WithLocation(18, 24));
        }

        [Fact, WorkItem(1720, "https://github.com/dotnet/roslyn/issues/1720")]
        public void GetSymbolsOnResultOfTypeof()
        {
            var source = @"
class C
{
    public C(int i)
    {
        typeof(C).GetField("" "").SetValue(null, new C(0));
    }
}
";
            var compilation = CreateCompilationWithMscorlib461(source);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var node = (ObjectCreationExpressionSyntax)tree.GetRoot().DescendantNodes().Where(n => n.ToString() == "new C(0)").Last();
            var identifierName = node.Type;

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("C..ctor(System.Int32 i)", symbolInfo.Symbol.ToTestDisplayString());
            var typeInfo = model.GetTypeInfo(node);
            Assert.Equal("C", typeInfo.Type.ToTestDisplayString());

        }
    }
}

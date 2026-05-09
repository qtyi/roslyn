// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.Semantics
{
    public class UsingDirectiveTests : CompilingTestBase
    {
        [Fact]
        public void UsingOuterNonGenericAlias1()
        {
            var source = @"
#pragma warning disable CS8019 // Unnecessary using directive.

using A = System.String;

namespace N
{
    using B = System.Collections.Generic.List<A>;
    using B<T> = System.Collections.Generic.Dictionary<A, T>;
}
";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingOuterNonGenericAlias2()
        {
            var source = @"
#pragma warning disable CS8019 // Unnecessary using directive.

using A = System;

namespace N
{
    using B = System.Collections.Generic.List<A>;
    using B<T> = System.Collections.Generic.Dictionary<A, T>;
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,47): error CS0118: 'A' is a using alias but is used like a type
                //     using B = System.Collections.Generic.List<A>;
                Diagnostic(ErrorCode.ERR_BadSKknown, "A").WithArguments("A", "using alias", "type").WithLocation(8, 47),
                // (9,56): error CS0118: 'A' is a using alias but is used like a type
                //     using B<T> = System.Collections.Generic.Dictionary<A, T>;
                Diagnostic(ErrorCode.ERR_BadSKknown, "A").WithArguments("A", "using alias", "type").WithLocation(9, 56));
        }

        [Fact]
        public void UsingOuterGenericAlias1()
        {
            var source = @"
#pragma warning disable CS8019 // Unnecessary using directive.

using A<TArg, TResult> = System.Func<TArg, TArg, TResult>;

namespace N
{
    using B = A<object, bool>;
    using B<T> = A<T, bool>;
}
";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingOuterGenericAlias2()
        {
            var source = @"
#pragma warning disable CS8019 // Unnecessary using directive.

using A<TArg, TResult> = System;

namespace N
{
    using B = A<object, bool>;
    using B<T> = A<T, bool>;
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,26): error CS7007: A 'using static' or a 'using generic' directive can only be applied to types; 'System' is a namespace not a type. Consider a 'using namespace' directive instead
                // using A<TArg, TResult> = System;
                Diagnostic(ErrorCode.ERR_BadUsingType, "System").WithArguments("System").WithLocation(4, 26),
                // (9,18): error CS7007: A 'using static' or a 'using generic' directive can only be applied to types; 'System' is a namespace not a type. Consider a 'using namespace' directive instead
                //     using B<T> = A<T, bool>;
                Diagnostic(ErrorCode.ERR_BadUsingType, "A<T, bool>").WithArguments("System").WithLocation(9, 18));
        }
    }
}

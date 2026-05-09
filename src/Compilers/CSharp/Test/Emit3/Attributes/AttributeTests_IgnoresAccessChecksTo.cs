// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    using InternalsVisibleFromTests = InternalsVisibleToAndStrongNameTests;

    public class AttributeTests_IgnoresAccessChecksTo : CSharpTestBase
    {
        [Fact]
        public void ExplicitAttribute_FromSource()
        {
            var source = @"class A {}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll.WithFriendAccessibleAssemblyPublicKeys("Other", ImmutableArray<byte>.Empty));
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, symbolValidator: m => AssertIgnoresAccessChecksToAttribute(m.ContainingAssembly, includesAttributeDefinition: true, includesAttributeUse: true, publicDefinition: false));
        }

        private static void AssertNoIgnoresAccessChecksToAttribute(AssemblySymbol assembly)
        {
            AssertIgnoresAccessChecksToAttribute(assembly, includesAttributeDefinition: false, includesAttributeUse: false, publicDefinition: false);
        }

        private static void AssertIgnoresAccessChecksToAttribute(AssemblySymbol assembly)
        {
            AssertIgnoresAccessChecksToAttribute(assembly, includesAttributeDefinition: true, includesAttributeUse: true, publicDefinition: false);
        }

        private static void AssertIgnoresAccessChecksToAttribute(AssemblySymbol assembly, bool includesAttributeDefinition, bool includesAttributeUse, bool publicDefinition)
        {
            const string namespaceName = "System.Runtime.CompilerServices";
            const string typeName = "IgnoresAccessChecksToAttribute";
            const string attributeName = namespaceName + "." + typeName;
            var type = (NamedTypeSymbol)assembly.GlobalNamespace.GetMember(attributeName);
            var attribute = assembly.GetAttributes(namespaceName, typeName).FirstOrDefault();
            if (includesAttributeDefinition)
            {
                Assert.NotNull(type);
            }
            else
            {
                Assert.Null(type);
                if (includesAttributeUse)
                {
                    type = attribute.AttributeClass;
                }
            }
            if (type is object)
            {
                Assert.Equal(publicDefinition ? Accessibility.Public : Accessibility.Internal, type.DeclaredAccessibility);
            }
            if (includesAttributeUse)
            {
                Assert.Equal(type, attribute.AttributeClass);
            }
            else
            {
                Assert.Null(attribute);
            }
        }

    }
}

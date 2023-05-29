// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using static Roslyn.Test.Utilities.SigningTestHelpers;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class InternalsVisibleToAndStrongNameTests
    {
        #region IVF Access Checking

        [Theory]
        [MemberData(nameof(AllProviderParseOptions))]
        public void IVFBasicCompilation(CSharpParseOptions parseOptions)
        {
            string s = @"public class C { internal void Goo() {} }";

            var other = CreateCompilation(s, options: TestOptions.SigningReleaseDll, assemblyName: "GrantsIVFAccess", parseOptions: parseOptions);

            var c = CreateCompilation(
    @"public class A
{
    internal class B
    {
        protected B(C o)
        {
            o.Goo();
        }
    }
}",
                new[] { new CSharpCompilationReference(other) },
                assemblyName: "WantsIVFAccessButCantHave",
                options: TestOptions.SigningReleaseDll,
                parseOptions: parseOptions);

            //compilation should not succeed, but internals should be imported.
            c.VerifyDiagnostics(
                // (7,15): error CS0122: 'C.Goo()' is inaccessible due to its protection level
                //             o.Goo();
                Diagnostic(ErrorCode.ERR_BadAccess, "Goo").WithArguments("C.Goo()").WithLocation(7, 15)
                );

            var c2 = CreateCompilation(
    @"public class A
{
    internal class B
    {
        protected B(C o)
        {
            o.Goo();
        }
    }
}",
                new[] { new CSharpCompilationReference(other) },
                assemblyName: "WantsIVFAccess",
                options: TestOptions.SigningReleaseDll.WithFriendAccessibleAssemblyPublicKeys("GrantsIVFAccess", default));

            Assert.Empty(c2.GetDiagnostics());
        }

        [Theory]
        [MemberData(nameof(AllProviderParseOptions))]
        public void IVFBasicMetadata(CSharpParseOptions parseOptions)
        {
            string s = @"public class C { internal void Goo() {} }";

            var otherStream = CreateCompilation(s, options: TestOptions.SigningReleaseDll, assemblyName: "GrantsIVFAccess", parseOptions: parseOptions).EmitToStream();

            var c = CreateCompilation(
    @"public class A
{
    internal class B
    {
        protected B(C o)
        {
            o.Goo();
        }
    }
}",
            references: new[] { AssemblyMetadata.CreateFromStream(otherStream, leaveOpen: true).GetReference() },
            assemblyName: "WantsIVFAccessButCantHave",
            options: TestOptions.SigningReleaseDll,
            parseOptions: parseOptions);

            //compilation should not succeed, and internals should not be imported.
            c.VerifyDiagnostics(
                // (7,15): error CS1061: 'C' does not contain a definition for 'Goo' and no extension method 'Goo' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
                //             o.Goo();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Goo").WithArguments("C", "Goo").WithLocation(7, 15)
                );

            otherStream.Position = 0;

            var c2 = CreateCompilation(
    @"public class A
{
    internal class B
    {
        protected B(C o)
        {
            o.Goo();
        }
    }
}",
                new[] { MetadataReference.CreateFromStream(otherStream) },
                assemblyName: "WantsIVFAccess",
                options: TestOptions.SigningReleaseDll.WithFriendAccessibleAssemblyPublicKeys("GrantsIVFAccess", default),
                parseOptions: parseOptions);

            Assert.Empty(c2.GetDiagnostics());
        }

        [ConditionalTheory(typeof(WindowsOnly), Reason = ConditionalSkipReason.TestExecutionNeedsWindowsTypes)]
        [MemberData(nameof(AllProviderParseOptions))]
        public void IVFSigned(CSharpParseOptions parseOptions)
        {
            string s = @"public class C { internal void Goo() {} }";

            var other = CreateCompilation(s,
                options: TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_keyPairFile),
                assemblyName: "Paul",
                parseOptions: parseOptions);

            other.VerifyDiagnostics();

            var requestor = CreateCompilation(
    @"public class A
{
    internal class B
    {
        protected B(C o)
        {
            o.Goo();
        }
    }
}",
                new[] { new CSharpCompilationReference(other) },
                TestOptions.SigningReleaseDll.WithCryptoKeyContainer("roslynTestContainer").WithFriendAccessibleAssemblyPublicKeys("Paul", s_publicKey),
                assemblyName: "John",
                parseOptions: parseOptions);

            Assert.Empty(requestor.GetDiagnostics());
        }

        [Theory]
        [MemberData(nameof(AllProviderParseOptions))]
        public void IVFNotBothSigned_CStoCS(CSharpParseOptions parseOptions)
        {
            string s = @"public class C { internal void Goo() {} }";

            var other = CreateCompilation(s, assemblyName: "Paul", options: TestOptions.SigningReleaseDll, parseOptions: parseOptions);
            other.VerifyDiagnostics();

            var requestor = CreateCompilation(
    @"public class A
{
    internal class B
    {
        protected B(C o)
        {
            o.Goo();
        }
    }
}",
                references: new[] { new CSharpCompilationReference(other) },
                assemblyName: "John",
                options: TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_keyPairFile).WithFriendAccessibleAssemblyPublicKeys("Paul", default),
                parseOptions: parseOptions);

            // We allow John to access Paul's internal Goo even though strong-named John should not be referencing weak-named Paul.
            // Paul has, after all, specifically granted access to John.

            // During emit time we should produce an error that says that a strong-named assembly cannot reference
            // a weak-named assembly. But the C# compiler doesn't currently do that. See https://github.com/dotnet/roslyn/issues/26722
            requestor.VerifyDiagnostics();
        }

        [Theory]
        [MemberData(nameof(AllProviderParseOptions))]
        public void IVFNotBothSigned_VBtoCS(CSharpParseOptions parseOptions)
        {
            string s = @"Public Class C
                Friend Sub Goo()
                End Sub
            End Class";

            var other = VisualBasic.VisualBasicCompilation.Create(
                syntaxTrees: new[] { VisualBasic.VisualBasicSyntaxTree.ParseText(s) },
                references: new[] { MscorlibRef_v4_0_30316_17626 },
                assemblyName: "Paul",
                options: new VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithStrongNameProvider(DefaultDesktopStrongNameProvider));
            other.VerifyDiagnostics();

            var requestor = CreateCompilation(
    @"public class A
{
    internal class B
    {
        protected B(C o)
        {
            o.Goo();
        }
    }
}",
                references: new MetadataReference[] { MetadataReference.CreateFromImage(other.EmitToArray()) },
                assemblyName: "John",
                options: TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_keyPairFile).WithFriendAccessibleAssemblyPublicKeys("Paul", default),
                parseOptions: parseOptions);

            // We allow John to access Paul's internal Goo even though strong-named John should not be referencing weak-named Paul.
            // Paul has, after all, specifically granted access to John.

            // During emit time we should produce an error that says that a strong-named assembly cannot reference
            // a weak-named assembly. But the C# compiler doesn't currently do that. See https://github.com/dotnet/roslyn/issues/26722
            requestor.VerifyDiagnostics();
        }

        [ConditionalTheory(typeof(WindowsOnly), Reason = ConditionalSkipReason.TestExecutionNeedsWindowsTypes)]
        [MemberData(nameof(AllProviderParseOptions))]
        public void IVFDeferredSuccess(CSharpParseOptions parseOptions)
        {
            string s = @"internal class CAttribute : System.Attribute { public CAttribute() {} }";

            var other = CreateCompilation(s,
                assemblyName: "Paul",
                options: TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_keyPairFile),
                parseOptions: parseOptions);

            other.VerifyDiagnostics();

            var requestor = CreateCompilation(
    @"
[assembly: C()]  //causes optimistic granting
[assembly: System.Reflection.AssemblyKeyName(""roslynTestContainer"")]
public class A
{
}",
                new[] { new CSharpCompilationReference(other) },
                assemblyName: "John",
                options: TestOptions.SigningReleaseDll.WithFriendAccessibleAssemblyPublicKeys("Paul", s_publicKey),
                parseOptions: parseOptions);

            Assert.True(ByteSequenceComparer.Equals(s_publicKey, other.Assembly.Identity.PublicKey));
            Assert.Empty(requestor.GetDiagnostics());
        }

        [ConditionalTheory(typeof(WindowsOnly), Reason = ConditionalSkipReason.TestExecutionNeedsWindowsTypes)]
        [MemberData(nameof(AllProviderParseOptions))]
        public void IVFDeferredFailSignMismatch(CSharpParseOptions parseOptions)
        {
            string s = @"internal class CAttribute : System.Attribute { public CAttribute() {} }";

            var other = CreateCompilation(s,
                assemblyName: "Paul",
                options: TestOptions.SigningReleaseDll,
                parseOptions: parseOptions); //not signed. cryptoKeyFile: KeyPairFile,

            other.VerifyDiagnostics();

            var requestor = CreateCompilation(
    @"
[assembly: C()]
[assembly: System.Reflection.AssemblyKeyName(""roslynTestContainer"")]
public class A
{
}",
                new[] { new CSharpCompilationReference(other) },
                assemblyName: "John",
                options: TestOptions.SigningReleaseDll.WithFriendAccessibleAssemblyPublicKeys("Paul", s_publicKey));

            Assert.False(ByteSequenceComparer.Equals(s_publicKey, other.Assembly.Identity.PublicKey));
            requestor.VerifyDiagnostics(
                // (2,12): error CS0122: 'CAttribute' is inaccessible due to its protection level
                // [assembly: C()]
                Diagnostic(ErrorCode.ERR_BadAccess, "C").WithArguments("CAttribute").WithLocation(2, 12)
                );
        }

        [ConditionalTheory(typeof(WindowsOnly), Reason = ConditionalSkipReason.TestExecutionNeedsWindowsTypes)]
        [MemberData(nameof(AllProviderParseOptions))]
        public void IVFDeferredFailSignMismatch_AssemblyKeyName(CSharpParseOptions parseOptions)
        {
            string s = @"internal class AssemblyKeyNameAttribute : System.Attribute { public AssemblyKeyNameAttribute() {} }";

            var other = CreateCompilation(s,
                assemblyName: "Paul",
                options: TestOptions.SigningReleaseDll,
                parseOptions: parseOptions); //not signed. cryptoKeyFile: KeyPairFile,

            other.VerifyDiagnostics();

            var requestor = CreateCompilation(
    @"
[assembly: AssemblyKeyName()] //causes optimistic granting
[assembly: System.Reflection.AssemblyKeyName(""roslynTestContainer"")]
public class A
{
}",
                new[] { new CSharpCompilationReference(other) },
                assemblyName: "John",
                options: TestOptions.SigningReleaseDll.WithFriendAccessibleAssemblyPublicKeys("Paul", s_publicKey));

            Assert.False(ByteSequenceComparer.Equals(s_publicKey, other.Assembly.Identity.PublicKey));
            requestor.VerifyDiagnostics(
                // (2,12): error CS0122: 'AssemblyKeyName' is inaccessible due to its protection level
                // [assembly: AssemblyKeyName()] //causes optimistic granting
                Diagnostic(ErrorCode.ERR_BadAccess, "AssemblyKeyName").WithArguments("AssemblyKeyNameAttribute").WithLocation(2, 12),
                // error CS0281: Friend access was granted by 'Paul, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null',
                // but the public key of the output assembly ('John, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2')
                // does not match that specified by the InternalsVisibleFrom attribute in the granting assembly.
                Diagnostic(ErrorCode.ERR_FriendRefNotEqualToThis)
                    .WithArguments("Paul, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "John, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2")
                    .WithLocation(1, 1)
                );
        }

        [ConditionalTheory(typeof(WindowsOnly), Reason = ConditionalSkipReason.TestExecutionNeedsWindowsTypes)]
        [MemberData(nameof(AllProviderParseOptions))]
        public void IVFDeferredFailKeyMismatch(CSharpParseOptions parseOptions)
        {
            string s = @"internal class CAttribute : System.Attribute { public CAttribute() {} }";

            var other = CreateCompilation(s,
                options: TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_keyPairFile),
                assemblyName: "Paul",
                parseOptions: parseOptions);

            other.VerifyDiagnostics();

            var requestor = CreateCompilation(
    @"
[assembly: C()]
[assembly: System.Reflection.AssemblyKeyName(""roslynTestContainer"")]
public class A
{
}",
                new MetadataReference[] { new CSharpCompilationReference(other) },
                assemblyName: "John",
                options: TestOptions.SigningReleaseDll.WithFriendAccessibleAssemblyPublicKeys("Paul", s_publicKey2),
                parseOptions: parseOptions);

            Assert.False(ByteSequenceComparer.Equals(s_publicKey2, other.Assembly.Identity.PublicKey));
            requestor.VerifyDiagnostics(
                // (2,12): error CS0122: 'CAttribute' is inaccessible due to its protection level
                // [assembly: C()]
                Diagnostic(ErrorCode.ERR_BadAccess, "C").WithArguments("CAttribute").WithLocation(2, 12)
                );
        }

        [ConditionalTheory(typeof(WindowsOnly), Reason = ConditionalSkipReason.TestExecutionNeedsWindowsTypes)]
        [MemberData(nameof(AllProviderParseOptions))]
        public void IVFDeferredFailKeyMismatch_AssemblyKeyName(CSharpParseOptions parseOptions)
        {
            string s = @"internal class AssemblyKeyNameAttribute : System.Attribute { public AssemblyKeyNameAttribute() {} }";

            var other = CreateCompilation(s,
                options: TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_keyPairFile),
                assemblyName: "Paul",
                parseOptions: parseOptions);

            other.VerifyDiagnostics();

            var requestor = CreateCompilation(
    @"
[assembly: AssemblyKeyName()]
[assembly: System.Reflection.AssemblyKeyName(""roslynTestContainer"")]
public class A
{
}",
                new MetadataReference[] { new CSharpCompilationReference(other) },
                assemblyName: "John",
                options: TestOptions.SigningReleaseDll.WithFriendAccessibleAssemblyPublicKeys("Paul", s_publicKey2),
                parseOptions: parseOptions);

            Assert.False(ByteSequenceComparer.Equals(s_publicKey2, requestor.Assembly.Identity.PublicKey));
            requestor.VerifyDiagnostics(
                // error CS0281: Friend access was granted by 'Paul, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2',
                // but the public key of the output assembly ('John, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2')
                // does not match that specified by the InternalsVisibleFrom attribute in the granting assembly.
                Diagnostic(ErrorCode.ERR_FriendRefNotEqualToThis)
                    .WithArguments("Paul, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2", "John, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2")
                    .WithLocation(1, 1),
                // (2,12): error CS0122: 'AssemblyKeyNameAttribute' is inaccessible due to its protection level
                // [assembly: AssemblyKeyName()]
                Diagnostic(ErrorCode.ERR_BadAccess, "AssemblyKeyName").WithArguments("AssemblyKeyNameAttribute").WithLocation(2, 12)
                );
        }

        [ConditionalTheory(typeof(WindowsOnly), Reason = ConditionalSkipReason.TestExecutionNeedsWindowsTypes)]
        [MemberData(nameof(AllProviderParseOptions))]
        public void IVFSuccessThroughIAssembly(CSharpParseOptions parseOptions)
        {
            string s = @"internal class CAttribute : System.Attribute { public CAttribute() {} }";

            var other = CreateCompilation(s,
                assemblyName: "Paul",
                options: TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_keyPairFile),
                parseOptions: parseOptions);

            other.VerifyDiagnostics();

            var requestor = CreateCompilation(
    @"
[assembly: C()]  //causes optimistic granting
[assembly: System.Reflection.AssemblyKeyName(""roslynTestContainer"")]
public class A
{
}",
                new MetadataReference[] { new CSharpCompilationReference(other) },
                options: TestOptions.SigningReleaseDll.WithFriendAccessibleAssemblyPublicKeys("Paul", s_publicKey),
                assemblyName: "John",
                parseOptions: parseOptions);

            Assert.True(other.Assembly.GivesAccessTo(requestor.Assembly));
            Assert.Empty(requestor.GetDiagnostics());
        }

        [ConditionalTheory(typeof(WindowsOnly), Reason = ConditionalSkipReason.TestExecutionNeedsWindowsTypes)]
        [MemberData(nameof(AllProviderParseOptions))]
        public void IVFDeferredFailKeyMismatchIAssembly(CSharpParseOptions parseOptions)
        {
            //key is wrong in the first digit. correct key starts with 0
            string s = @"internal class CAttribute : System.Attribute { public CAttribute() {} }";

            var other = CreateCompilation(s,
                options: TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_keyPairFile),
                assemblyName: "Paul",
                parseOptions: parseOptions);

            other.VerifyDiagnostics();

            var requestor = CreateCompilation(
    @"

[assembly: C()]
[assembly: System.Reflection.AssemblyKeyName(""roslynTestContainer"")]
public class A
{
}",
                new MetadataReference[] { new CSharpCompilationReference(other) },
                TestOptions.SigningReleaseDll.WithFriendAccessibleAssemblyPublicKeys("Paul", s_publicKey2),
                assemblyName: "John",
                parseOptions: parseOptions);

            Assert.False(other.Assembly.GivesAccessTo(requestor.Assembly));
            requestor.VerifyDiagnostics(
                // (3,12): error CS0122: 'CAttribute' is inaccessible due to its protection level
                // [assembly: C()]
                Diagnostic(ErrorCode.ERR_BadAccess, "C").WithArguments("CAttribute").WithLocation(3, 12)
                );
        }

        [ConditionalTheory(typeof(WindowsOnly), Reason = ConditionalSkipReason.TestExecutionNeedsWindowsTypes)]
        [MemberData(nameof(AllProviderParseOptions))]
        public void IVFDeferredFailKeyMismatchIAssembly_AssemblyKeyName(CSharpParseOptions parseOptions)
        {
            string s = @"internal class AssemblyKeyNameAttribute : System.Attribute { public AssemblyKeyNameAttribute() {} }";

            var other = CreateCompilation(s,
                options: TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_keyPairFile),
                assemblyName: "Paul",
                parseOptions: parseOptions);

            other.VerifyDiagnostics();

            var requestor = CreateCompilation(
    @"

[assembly: AssemblyKeyName()]
[assembly: System.Reflection.AssemblyKeyName(""roslynTestContainer"")]
public class A
{
}",
                new MetadataReference[] { new CSharpCompilationReference(other) },
                TestOptions.SigningReleaseDll.WithFriendAccessibleAssemblyPublicKeys("Paul", s_publicKey2),
                assemblyName: "John",
                parseOptions: parseOptions);

            Assert.False(other.Assembly.GivesAccessTo(requestor.Assembly));
            requestor.VerifyDiagnostics(
                // error CS0281: Friend access was granted by 'Paul, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2',
                // but the public key of the output assembly ('John, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2')
                // does not match that specified by the InternalsVisibleFrom attribute in the granting assembly.
                Diagnostic(ErrorCode.ERR_FriendRefNotEqualToThis)
                    .WithArguments("Paul, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2", "John, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2")
                    .WithLocation(1, 1),
                // (3,12): error CS0122: 'AssemblyKeyNameAttribute' is inaccessible due to its protection level
                // [assembly: AssemblyKeyName()]
                Diagnostic(ErrorCode.ERR_BadAccess, "AssemblyKeyName").WithArguments("AssemblyKeyNameAttribute").WithLocation(3, 12)
                );
        }

        [WorkItem(820450, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/820450")]
        [Theory]
        [MemberData(nameof(AllProviderParseOptions))]
        public void IVFGivesAccessToUsingDifferentKeys(CSharpParseOptions parseOptions)
        {
            string s = @"namespace ClassLibrary1 { internal class Class1 { } } ";

            var giver = CreateCompilation(s,
                assemblyName: "Paul",
                options: TestOptions.SigningReleaseDll.WithCryptoKeyFile(SigningTestHelpers.KeyPairFile2),
                parseOptions: parseOptions);

            giver.VerifyDiagnostics();

            var requestor = CreateCompilation(
    @"
namespace ClassLibrary2
{
    internal class A
    {
        public void Goo(ClassLibrary1.Class1 a)
        {
        }
    }
}",
                new MetadataReference[] { new CSharpCompilationReference(giver) },
                options: TestOptions.SigningReleaseDll.WithCryptoKeyFile(s_keyPairFile).WithFriendAccessibleAssemblyPublicKeys("Paul", s_publicKey2),
                assemblyName: "John",
                parseOptions: parseOptions);

            Assert.True(giver.Assembly.GivesAccessTo(requestor.Assembly));
            Assert.Empty(requestor.GetDiagnostics());
        }
        #endregion

    }
}

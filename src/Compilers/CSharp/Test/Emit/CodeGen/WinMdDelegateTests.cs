// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class WinMdDelegateTests : CSharpTestBase
    {
        private delegate void VerifyType(bool isWinMd, params string[] expectedMembers);

        /// <summary>
        /// When the output type is .winmdobj, delegate types shouldn't output Begin/End invoke 
        /// members.
        /// </summary>
        [Theory, MemberData(nameof(FileScopedOrBracedNamespace)), WorkItem(1003193, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1003193")]
        public void SimpleDelegateMembersTest(string ob, string cb)
        {
            string libSrc =
$@"namespace Test {ob}
  public delegate void voidDelegate();
{cb}
";
            Func<string[], Action<ModuleSymbol>> getValidator = expectedMembers => m =>
            {
                {
                    var actualMembers =
                        m.GlobalNamespace.GetMember<NamespaceSymbol>("Test").
                        GetMember<NamedTypeSymbol>("voidDelegate").GetMembers().ToArray();

                    AssertEx.SetEqual(actualMembers.Select(s => s.Name), expectedMembers);
                };
            };

            VerifyType verify = (winmd, expected) =>
            {
                var validator = getValidator(expected);

                // We should see the same members from both source and metadata
                var verifier = CompileAndVerify(
                    libSrc,
                    sourceSymbolValidator: validator,
                    symbolValidator: validator,
                    options: winmd ? TestOptions.ReleaseWinMD : TestOptions.ReleaseDll,
                    parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));
                verifier.VerifyDiagnostics();
            };

            // Test winmd
            verify(true,
                WellKnownMemberNames.InstanceConstructorName,
                WellKnownMemberNames.DelegateInvokeName);

            // Test normal
            verify(false,
                WellKnownMemberNames.InstanceConstructorName,
                WellKnownMemberNames.DelegateInvokeName,
                WellKnownMemberNames.DelegateBeginInvokeName,
                WellKnownMemberNames.DelegateEndInvokeName);
        }

        [Fact]
        public void TestAllDelegates()
        {
            var winRtDelegateLibrarySrc =
@"using System;

namespace WinRTDelegateLibrary
{
    public struct S1 { }

    public enum E1
    {
        alpha = 1,
        bravo,
        charlie,
        delta,
    };

    public class C1 { }

    public interface I1 { }

    /// 
    /// These are the interesting types
    /// 

    public delegate void voidvoidDelegate();

    public delegate int intintDelegate(int a);

    public delegate S1 structDelegate(S1 s);

    public delegate E1 enumDelegate(E1 e);

    public delegate C1 classDelegate(C1 c);

    public delegate string stringDelegate(string s);

    public delegate Decimal decimalDelegate(Decimal d);

    public delegate voidvoidDelegate WinRTDelegate(voidvoidDelegate d);

    public delegate int? nullableDelegate(int? a);

    public delegate T genericDelegate<T>(T t);
    public delegate T genericDelegate2<T>(T t) where T : new();
    public delegate T genericDelegate3<T>(T t) where T : class;
    public delegate T genericDelegate4<T>(T t) where T : struct;
    public delegate T genericDelegate5<T>(T t) where T : I1;

    public delegate int[] arrayDelegate(int[] arr);

    public delegate I1 interfaceDelegate(I1 i);

    public delegate dynamic dynamicDelegate(dynamic d);

    public unsafe delegate int* pointerDelegate(int* ip);

    public unsafe delegate S1* pointerDelegate2(S1* op);

    public unsafe delegate E1* pointerDelegate3(E1* ep);
}";
            // We need the 4.5 refs here
            var coreRefs45 = new[] {
                MscorlibRef_v4_0_30316_17626,
                SystemCoreRef_v4_0_30319_17929
            };

            var winRtDelegateLibrary = CreateEmptyCompilation(
                winRtDelegateLibrarySrc,
                references: coreRefs45,
                options: TestOptions.ReleaseWinMD.WithAllowUnsafe(true),
                assemblyName: "WinRTDelegateLibrary").EmitToImageReference();

            var nonWinRtLibrarySrc = winRtDelegateLibrarySrc.Replace("WinRTDelegateLibrary", "NonWinRTDelegateLibrary");

            var nonWinRtDelegateLibrary = CreateEmptyCompilation(
                nonWinRtLibrarySrc,
                references: coreRefs45,
                options: TestOptions.UnsafeReleaseDll,
                assemblyName: "NonWinRTDelegateLibrary").EmitToImageReference();

            var allDelegates =
@"using WinRT = WinRTDelegateLibrary;
using NonWinRT = NonWinRTDelegateLibrary;

class Test
{
    public WinRT.voidvoidDelegate d001;
    public NonWinRT.voidvoidDelegate d101;

    public WinRT.intintDelegate d002;
    public NonWinRT.intintDelegate d102;

    public WinRT.structDelegate d003;
    public NonWinRT.structDelegate d103;

    public WinRT.enumDelegate d004;
    public NonWinRT.enumDelegate d104;

    public WinRT.classDelegate d005;
    public NonWinRT.classDelegate d105;

    public WinRT.stringDelegate d006;
    public NonWinRT.stringDelegate d106;

    public WinRT.decimalDelegate d007;
    public NonWinRT.decimalDelegate d107;

    public WinRT.WinRTDelegate d008;
    public NonWinRT.WinRTDelegate d108;

    public WinRT.nullableDelegate d009;
    public NonWinRT.nullableDelegate d109;

    public WinRT.genericDelegate<float> d010;
    public NonWinRT.genericDelegate<float> d110;

    public WinRT.genericDelegate2<object> d011;
    public NonWinRT.genericDelegate2<object> d111;

    public WinRT.genericDelegate3<WinRT.C1> d012;
    public NonWinRT.genericDelegate3<NonWinRT.C1> d112;

    public WinRT.genericDelegate4<WinRT.S1> d013;
    public NonWinRT.genericDelegate4<NonWinRT.S1> d113;

    public WinRT.genericDelegate5<WinRT.I1> d014;
    public NonWinRT.genericDelegate5<NonWinRT.I1> d114;

    public WinRT.arrayDelegate d015;
    public NonWinRT.arrayDelegate d115;

    public WinRT.interfaceDelegate d016;
    public NonWinRT.interfaceDelegate d116;

    public WinRT.dynamicDelegate d017;
    public NonWinRT.dynamicDelegate d117;

    public WinRT.pointerDelegate d018;
    public NonWinRT.pointerDelegate d118;

    public WinRT.pointerDelegate2 d019;
    public NonWinRT.pointerDelegate2 d119;

    public WinRT.pointerDelegate3 d020;
    public NonWinRT.pointerDelegate3 d120;
}";

            Func<FieldSymbol, bool> isWinRt = (field) =>
            {
                var fieldType = field.Type;

                if ((object)fieldType == null)
                {
                    return false;
                }

                if (!fieldType.IsDelegateType())
                {
                    return false;
                }

                foreach (var member in fieldType.GetMembers())
                {
                    switch (member.Name)
                    {
                        case WellKnownMemberNames.DelegateBeginInvokeName:
                        case WellKnownMemberNames.DelegateEndInvokeName:
                            return false;
                        default:
                            break;
                    }
                }

                return true;
            };

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
                var fields = type.GetMembers();

                foreach (var field in fields)
                {
                    var fieldSymbol = field as FieldSymbol;
                    if ((object)fieldSymbol != null)
                    {
                        if (fieldSymbol.Name.Contains("d1"))
                        {
                            Assert.False(isWinRt(fieldSymbol));
                        }
                        else
                        {
                            Assert.True(isWinRt(fieldSymbol));
                        }
                    }
                }
            };

            var comp = CompileAndVerify(
                allDelegates,
                references: new[] {
                    winRtDelegateLibrary,
                    nonWinRtDelegateLibrary
                },
                symbolValidator: validator);

            comp.VerifyDiagnostics();
        }
    }
}

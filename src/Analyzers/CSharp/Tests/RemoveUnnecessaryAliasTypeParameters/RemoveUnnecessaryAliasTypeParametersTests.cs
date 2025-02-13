// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryAliasTypeParameters;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using static Roslyn.Test.Utilities.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessaryAliasTypeParameters;

[Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryAliasTypeParameters)]
public sealed class RemoveUnnecessaryAliasTypeParametersTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    private readonly ParseOptions CSharpPreview = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpRemoveUnnecessaryAliasTypeParametersDiagnosticAnalyzer(), new CSharpRemoveUnnecessaryAliasTypeParametersCodeFixProvider());

    private Task TestDiagnosticsAsync(string initialMarkup, params DiagnosticDescription[] expectedDiagnostics)
        => TestDiagnosticsAsync(initialMarkup, new TestParameters(parseOptions: CSharpPreview, retainNonFixableDiagnostics: true), expectedDiagnostics);

    [Fact]
    public async Task TestUnused01()
    {
        await TestDiagnosticsAsync(
            """
            using A<[|T|]> = object;
            """,
            Diagnostic(IDEDiagnosticIds.RemoveUnusedAliasTypeParametersDiagnosticId));
    }

    [Fact]
    public async Task TestUnused02()
    {
        await TestDiagnosticsAsync(
            """
            using A<T1, [|T2|]> = T1;
            """,
            Diagnostic(IDEDiagnosticIds.RemoveUnusedAliasTypeParametersDiagnosticId));
    }

    [Fact]
    public async Task TestUnused03()
    {
        await TestDiagnosticsAsync(
            """
            using A<[|T|]> = object where T : struct;
            """,
            Diagnostic(IDEDiagnosticIds.RemoveUnusedAliasTypeParametersDiagnosticId));
    }

    [Fact]
    public async Task TestUnnecessary01()
    {
        await TestDiagnosticsAsync(
            """
            using A<T1, [|T2|]> = T1 where T1 : T2;
            """,
            Diagnostic(IDEDiagnosticIds.RemoveUnnecessaryAliasTypeParametersDiagnosticId));
    }
}

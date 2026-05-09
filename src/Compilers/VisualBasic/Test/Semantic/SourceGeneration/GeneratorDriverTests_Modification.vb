' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities.TestGenerators

Namespace Microsoft.CodeAnalysis.VisualBasic.Semantic.UnitTests.SourceGeneration
    Public Class GeneratorDriverTests_Modification
        Inherits BasicTestBase

        <Theory>
        <InlineData(False)>
        <InlineData(True)>
        Public Sub InsertText(incremental As Boolean)
            Dim source = "Public Class T
End Class"

            Dim generatorSource = "Public MustInherit Class T
End Class"

            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = CreateCompilation(BasicTestSource.Parse(source, path:="test.cs", options:=parseOptions), options:=TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Assert.Single(compilation.SyntaxTrees)
            Dim oldTree = compilation.SyntaxTrees.First()

            ' `Class` -> `MustInherit Class`
            Dim insertTexts = New(SyntaxTree, Integer, String)() {(oldTree, 7, "MustInherit ")}
            Dim testGenerator As ModifyTextGenerator = If(incremental, New IncrementalModifyTextGenerator(insertTexts:=insertTexts),
                                                                       New ModifyTextGenerator(insertTexts:=insertTexts))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(testGenerator), parseOptions:=parseOptions)
            Dim outputCompilation As Compilation = Nothing
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, outputCompilation, diagnostics)
            outputCompilation.VerifyDiagnostics()

            Assert.Single(outputCompilation.SyntaxTrees)
            Dim newTree = outputCompilation.SyntaxTrees.First()

            Assert.NotEqual(compilation, outputCompilation)
            Assert.NotEqual(oldTree, newTree)
            Assert.Equal(oldTree.FilePath, newTree.FilePath)
            Assert.Equal(generatorSource, newTree.ToString())
        End Sub

        <Theory>
        <InlineData(False)>
        <InlineData(True)>
        Public Sub ReplaceText(incremental As Boolean)
            Dim source = "Class T
End Class"

            Dim generatorSource = "Structure T
End Structure"

            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = CreateCompilation(BasicTestSource.Parse(source, path:="test.cs", options:=parseOptions), options:=TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Assert.Single(compilation.SyntaxTrees)
            Dim oldTree = compilation.SyntaxTrees.First()

            ' `Class` -> `Structure`
            Dim replaceTexts = New(SyntaxTree, TextSpan, String)() {(oldTree, New TextSpan(0, 5), "Structure"), (oldTree, New TextSpan(13, 5), "Structure")}
            Dim testGenerator As ModifyTextGenerator = If(incremental, New IncrementalModifyTextGenerator(replaceTexts:=replaceTexts),
                                                                       New ModifyTextGenerator(replaceTexts:=replaceTexts))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(testGenerator), parseOptions:=parseOptions)
            Dim outputCompilation As Compilation = Nothing
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, outputCompilation, diagnostics)
            outputCompilation.VerifyDiagnostics()

            Assert.Single(outputCompilation.SyntaxTrees)
            Dim newTree = outputCompilation.SyntaxTrees.First()

            Assert.NotEqual(compilation, outputCompilation)
            Assert.NotEqual(oldTree, newTree)
            Assert.Equal(oldTree.FilePath, newTree.FilePath)
            Assert.Equal(generatorSource, newTree.ToString())
        End Sub

        <Theory>
        <InlineData(False)>
        <InlineData(True)>
        Public Sub RemoveText(incremental As Boolean)
            Dim source = "Public Partial Class T
End Class"

            Dim generatorSource = "Public Class T
End Class"

            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = CreateCompilation(BasicTestSource.Parse(source, path:="test.cs", options:=parseOptions), options:=TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Assert.Single(compilation.SyntaxTrees)
            Dim oldTree = compilation.SyntaxTrees.First()

            ' `Partial Class` -> `Class`
            Dim removeTexts = New(SyntaxTree, TextSpan)() {(oldTree, New TextSpan(7, 8))}
            Dim testGenerator As ModifyTextGenerator = If(incremental, New IncrementalModifyTextGenerator(removeTexts:=removeTexts),
                                                                       New ModifyTextGenerator(removeTexts:=removeTexts))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(testGenerator), parseOptions:=parseOptions)
            Dim outputCompilation As Compilation = Nothing
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, outputCompilation, diagnostics)
            outputCompilation.VerifyDiagnostics()

            Assert.Single(outputCompilation.SyntaxTrees)
            Dim newTree = outputCompilation.SyntaxTrees.First()

            Assert.NotEqual(compilation, outputCompilation)
            Assert.NotEqual(oldTree, newTree)
            Assert.Equal(oldTree.FilePath, newTree.FilePath)
            Assert.Equal(generatorSource, newTree.ToString())
        End Sub

        <Theory>
        <InlineData(False)>
        <InlineData(True)>
        Public Sub ReplaceSource(incremental As Boolean)
            Dim source = "Public Class A
End Class"

            Dim newSource = "Public Class B
End Class"

            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = CreateCompilation(BasicTestSource.Parse(source, path:="test.cs", options:=parseOptions), options:=TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Assert.Single(compilation.SyntaxTrees)
            Dim oldTree = compilation.SyntaxTrees.First()

            Dim newTree = Parse(newSource, fileName:="test2.cs", options:=parseOptions)
            Assert.NotEqual(oldTree, newTree)
            Assert.NotEqual(oldTree.FilePath, newTree.FilePath)

            ' `... Class A ...` -> `... Class B ...`
            Dim replaceSources = New(SyntaxTree, SyntaxTree)() {(oldTree, newTree)}
            Dim testGenerator As ModifyTextGenerator = If(incremental, New IncrementalModifyTextGenerator(replaceSources:=replaceSources),
                                                                       New ModifyTextGenerator(replaceSources:=replaceSources))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(testGenerator), parseOptions:=parseOptions)
            Dim outputCompilation As Compilation = Nothing
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, outputCompilation, diagnostics)
            outputCompilation.VerifyDiagnostics()

            Assert.Single(outputCompilation.SyntaxTrees)

            Assert.NotEqual(compilation, outputCompilation)
            Assert.Equal(newTree.ToString(), outputCompilation.SyntaxTrees.First().ToString())
        End Sub

        <Theory>
        <InlineData(False)>
        <InlineData(True)>
        Public Sub RemoveSource(incremental As Boolean)
            Dim parseOptions = TestOptions.Regular

            Dim classTree = Parse("Public Class T
End Class", "test1.cs", options:=parseOptions)
            Dim structTree = Parse("Public Structure T
End Structure", "test2.cs", options:=parseOptions)

            Dim compilation As Compilation = CreateCompilation(New SyntaxTree() {classTree, structTree}, options:=TestOptions.DebugDll)
            AssertTheseDiagnostics(compilation, <errors>
BC30179: class 'T' and structure 'T' conflict in namespace '&lt;Default&gt;'.
Public Class T
             ~
BC30179: structure 'T' and class 'T' conflict in namespace '&lt;Default&gt;'.
Public Structure T
                 ~
</errors>)

            Assert.Equal(New SyntaxTree() {classTree, structTree}, compilation.SyntaxTrees)

            ' remove `... Class ...`
            Dim removeSources = New SyntaxTree() {classTree}
            Dim testGenerator As ModifyTextGenerator = If(incremental, New IncrementalModifyTextGenerator(removeSources:=removeSources),
                                                                       New ModifyTextGenerator(removeSources:=removeSources))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(testGenerator), parseOptions:=parseOptions)
            Dim outputCompilation As Compilation = Nothing
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, outputCompilation, diagnostics)
            outputCompilation.VerifyDiagnostics()

            Assert.Single(outputCompilation.SyntaxTrees)

            Assert.NotEqual(Compilation, outputCompilation)
            Assert.Equal(structTree, outputCompilation.SyntaxTrees.First())
        End Sub
    End Class
End Namespace

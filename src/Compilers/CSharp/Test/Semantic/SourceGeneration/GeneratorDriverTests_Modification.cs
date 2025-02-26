// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.SourceGeneration
{
    public sealed class GeneratorDriverTests_Modification : CSharpTestBase
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void InsertText(bool incremental)
        {
            var source = @"public struct T { }";

            var generatorSource = @"public readonly struct T { }";

            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, sourceFileName: "test.cs", options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);
            var oldTree = compilation.SyntaxTrees.First();

            // `struct` -> `readonly struct`
            var insertTexts = new[] { (7, "readonly ") };
            ModifyTextGenerator testGenerator = incremental ? new IncrementalModifyTextGenerator(insertTexts: insertTexts)
                                                            : new ModifyTextGenerator(insertTexts: insertTexts);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

            Assert.Single(outputCompilation.SyntaxTrees);
            var newTree = outputCompilation.SyntaxTrees.First();

            Assert.NotEqual(compilation, outputCompilation);
            Assert.NotEqual(oldTree, newTree);
            Assert.Equal(oldTree.FilePath, newTree.FilePath);
            Assert.Equal(generatorSource, newTree.GetText().ToString());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ReplaceText(bool incremental)
        {
            var source = @"class T { }";

            var generatorSource = @"struct T { }";

            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, sourceFileName: "test.cs", options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);
            var oldTree = compilation.SyntaxTrees.First();

            // `class` -> `struct`
            var replaceTexts = new[] { (new TextSpan(0, 5), "struct") };
            ModifyTextGenerator testGenerator = incremental ? new IncrementalModifyTextGenerator(replaceTexts: replaceTexts)
                                                            : new ModifyTextGenerator(replaceTexts: replaceTexts);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

            Assert.Single(outputCompilation.SyntaxTrees);
            var newTree = outputCompilation.SyntaxTrees.First();

            Assert.NotEqual(compilation, outputCompilation);
            Assert.NotEqual(oldTree, newTree);
            Assert.Equal(oldTree.FilePath, newTree.FilePath);
            Assert.Equal(generatorSource, newTree.GetText().ToString());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RemoveText(bool incremental)
        {
            var source = @"public partial class T { }";

            var generatorSource = @"public class T { }";

            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, sourceFileName: "test.cs", options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);
            var oldTree = compilation.SyntaxTrees.First();

            // `partial class` -> `class`
            var removeTexts = new[] { new TextSpan(7, 8) };
            ModifyTextGenerator testGenerator = incremental ? new IncrementalModifyTextGenerator(removeTexts: removeTexts)
                                                            : new ModifyTextGenerator(removeTexts: removeTexts);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

            Assert.Single(outputCompilation.SyntaxTrees);
            var newTree = outputCompilation.SyntaxTrees.First();

            Assert.NotEqual(compilation, outputCompilation);
            Assert.NotEqual(oldTree, newTree);
            Assert.Equal(oldTree.FilePath, newTree.FilePath);
            Assert.Equal(generatorSource, newTree.GetText().ToString());
        }
    }
}

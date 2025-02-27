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
            var insertTexts = new[] { (oldTree, 7, "readonly ") };
            ModifyTextGenerator testGenerator = incremental ? new IncrementalModifyTextGenerator(insertTexts: insertTexts)
                                                            : new ModifyTextGenerator(insertTexts: insertTexts);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
            outputCompilation.VerifyDiagnostics();

            Assert.Single(outputCompilation.SyntaxTrees);
            var newTree = outputCompilation.SyntaxTrees.First();

            Assert.NotEqual(compilation, outputCompilation);
            Assert.NotEqual(oldTree, newTree);
            Assert.Equal(oldTree.FilePath, newTree.FilePath);
            Assert.Equal(generatorSource, newTree.ToString());
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
            var replaceTexts = new[] { (oldTree, new TextSpan(0, 5), "struct") };
            ModifyTextGenerator testGenerator = incremental ? new IncrementalModifyTextGenerator(replaceTexts: replaceTexts)
                                                            : new ModifyTextGenerator(replaceTexts: replaceTexts);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
            outputCompilation.VerifyDiagnostics();

            Assert.Single(outputCompilation.SyntaxTrees);
            var newTree = outputCompilation.SyntaxTrees.First();

            Assert.NotEqual(compilation, outputCompilation);
            Assert.NotEqual(oldTree, newTree);
            Assert.Equal(oldTree.FilePath, newTree.FilePath);
            Assert.Equal(generatorSource, newTree.ToString());
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
            var removeTexts = new[] { (oldTree, new TextSpan(7, 8)) };
            ModifyTextGenerator testGenerator = incremental ? new IncrementalModifyTextGenerator(removeTexts: removeTexts)
                                                            : new ModifyTextGenerator(removeTexts: removeTexts);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
            outputCompilation.VerifyDiagnostics();

            Assert.Single(outputCompilation.SyntaxTrees);
            var newTree = outputCompilation.SyntaxTrees.First();

            Assert.NotEqual(compilation, outputCompilation);
            Assert.NotEqual(oldTree, newTree);
            Assert.Equal(oldTree.FilePath, newTree.FilePath);
            Assert.Equal(generatorSource, newTree.ToString());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ReplaceSource(bool incremental)
        {
            var source = @"public class A { }";

            var newSource = @"public class B { }";

            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, sourceFileName: "test.cs", options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);
            var oldTree = compilation.SyntaxTrees.First();

            var newTree = Parse(newSource, filename: "test2.cs", options: parseOptions);
            Assert.NotEqual(oldTree, newTree);
            Assert.NotEqual(oldTree.FilePath, newTree.FilePath);

            // `... class A ...` -> `... class B ...`
            var replaceSources = new[] { (oldTree, newTree) };
            ModifyTextGenerator testGenerator = incremental ? new IncrementalModifyTextGenerator(replaceSources: replaceSources)
                                                            : new ModifyTextGenerator(replaceSources: replaceSources);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
            outputCompilation.VerifyDiagnostics();

            Assert.Single(outputCompilation.SyntaxTrees);

            Assert.NotEqual(compilation, outputCompilation);
            Assert.Equal(newTree.ToString(), outputCompilation.SyntaxTrees.First().ToString());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RemoveSource(bool incremental)
        {
            var parseOptions = TestOptions.Regular;

            var classTree = Parse(@"public class T { }", "test1.cs", options: parseOptions);
            var structTree = Parse(@"public struct T { }", "test2.cs", options: parseOptions);

            Compilation compilation = CreateCompilation([classTree, structTree], options: TestOptions.DebugDllThrowing);
            compilation.VerifyDiagnostics(
                // test2.cs(1,15): error CS0101: The namespace '<global namespace>' already contains a definition for 'T'
                // public struct T { }
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "T").WithArguments("T", "<global namespace>").WithLocation(1, 15));

            Assert.Equal([classTree, structTree], compilation.SyntaxTrees);

            // remove `... class ...`
            var removeSources = new[] { classTree };
            ModifyTextGenerator testGenerator = incremental ? new IncrementalModifyTextGenerator(removeSources: removeSources)
                                                            : new ModifyTextGenerator(removeSources: removeSources);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
            outputCompilation.VerifyDiagnostics();

            Assert.Single(outputCompilation.SyntaxTrees);

            Assert.NotEqual(compilation, outputCompilation);
            Assert.Equal(structTree, outputCompilation.SyntaxTrees.First());
        }
    }
}

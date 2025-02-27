// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Test.Utilities.TestGenerators
{
    internal class SingleFileTestGenerator : ISourceGenerator
    {
        private readonly List<(string content, string hintName)> _sources = new();

        public SingleFileTestGenerator()
        {
        }

        public SingleFileTestGenerator(string content, string? hintName = null)
        {
            AddSource(content, hintName);
        }

        public void AddSource(string content, string? hintName = null)
        {
            hintName ??= "generatedFile" + (_sources.Any() ? (_sources.Count + 1).ToString() : "");
            _sources.Add((content, hintName));
        }

        public void Execute(GeneratorExecutionContext context)
        {
            foreach (var (content, hintName) in _sources)
                context.AddSource(hintName, SourceText.From(content, Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }

    /// <summary>
    /// A generator that produces diagnostics against existng source trees, rather than generating new content.
    /// </summary>
    internal class DiagnosticProducingGenerator : ISourceGenerator
    {
        public static readonly DiagnosticDescriptor Descriptor =
            new DiagnosticDescriptor(nameof(DiagnosticProducingGenerator), "Diagnostic Title", "Diagnostic Format", "Test", DiagnosticSeverity.Error, isEnabledByDefault: true);

        private readonly Func<GeneratorExecutionContext, Location> _produceLocation;

        public DiagnosticProducingGenerator(Func<GeneratorExecutionContext, Location> produceLocation)
        {
            _produceLocation = produceLocation;
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptor, _produceLocation(context)));
        }
    }

    internal class SingleFileTestGenerator2 : SingleFileTestGenerator
    {
        public SingleFileTestGenerator2(string content, string hintName = "generatedFile") : base(content, hintName)
        {
        }
    }

#pragma warning disable RS0062 // Do not implicitly capture primary constructor paramters
    internal class CallbackGenerator(
        Action<GeneratorInitializationContext> onInit,
        Action<GeneratorExecutionContext> onExecute,
        Func<ImmutableArray<(string hintName, SourceText? sourceText)>> computeSourceTexts)
        : ISourceGenerator
    {
        public CallbackGenerator(Action<GeneratorInitializationContext> onInit, Action<GeneratorExecutionContext> onExecute, string? source = "")
            : this(onInit, onExecute, () => ("source", source))
        {
        }

        public CallbackGenerator(Action<GeneratorInitializationContext> onInit, Action<GeneratorExecutionContext> onExecute, Func<(string hintName, string? source)> computeSource)
            : this(onInit, onExecute, () =>
            {
                var (hint, source) = computeSource();
                return ImmutableArray.Create((hint, string.IsNullOrWhiteSpace(source)
                    ? null
                    : SourceText.From(source, Encoding.UTF8)));
            })
        {
        }

        public CallbackGenerator(Func<(string hintName, string? source)> computeSource)
            : this(onInit: static _ => { }, onExecute: static _ => { }, () =>
            {
                var (hint, source) = computeSource();
                return ImmutableArray.Create((hint, string.IsNullOrWhiteSpace(source)
                    ? null
                    : SourceText.From(source, Encoding.UTF8)));
            })
        {
        }

        public void Initialize(GeneratorInitializationContext context)
            => onInit(context);

        public void Execute(GeneratorExecutionContext context)
        {
            onExecute(context);

            foreach (var (hintName, sourceText) in computeSourceTexts())
            {
                if (sourceText != null)
                    context.AddSource(hintName, sourceText);
            }
        }
    }
#pragma warning restore RS0062 // Do not implicitly capture primary constructor paramters

    internal class CallbackGenerator2 : CallbackGenerator
    {
        public CallbackGenerator2(Action<GeneratorInitializationContext> onInit, Action<GeneratorExecutionContext> onExecute, string? source = "") : base(onInit, onExecute, source)
        {
        }
    }

    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText _content;

        public InMemoryAdditionalText(string path, string content)
        {
            Path = path;
            _content = SourceText.From(content, Encoding.UTF8);
        }

        public override string Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default) => _content;

        internal class BinaryText : InMemoryAdditionalText
        {
            public BinaryText(string path) : base(path, string.Empty) { }

            public override SourceText GetText(CancellationToken cancellationToken = default) => throw new InvalidDataException("Binary content not supported");
        }

        internal string GetDebuggerDisplay()
        {
            return $"'{Path}': '{_content.Lines[0]}'";
        }
    }

    internal sealed class PipelineCallbackGenerator : IIncrementalGenerator
    {
        private readonly Action<IncrementalGeneratorInitializationContext> _registerPipelineCallback;

        public PipelineCallbackGenerator(Action<IncrementalGeneratorInitializationContext> registerPipelineCallback)
        {
            _registerPipelineCallback = registerPipelineCallback;
        }

        public void Initialize(IncrementalGeneratorInitializationContext context) => _registerPipelineCallback(context);
    }

    internal sealed class PipelineCallbackGenerator2 : IIncrementalGenerator
    {
        private readonly Action<IncrementalGeneratorInitializationContext> _registerPipelineCallback;

        public PipelineCallbackGenerator2(Action<IncrementalGeneratorInitializationContext> registerPipelineCallback)
        {
            _registerPipelineCallback = registerPipelineCallback;
        }

        public void Initialize(IncrementalGeneratorInitializationContext context) => _registerPipelineCallback(context);
    }

    internal sealed class IncrementalAndSourceCallbackGenerator : CallbackGenerator, IIncrementalGenerator
    {
        private readonly Action<IncrementalGeneratorInitializationContext> _onInit;

        public IncrementalAndSourceCallbackGenerator(Action<GeneratorInitializationContext> onInit, Action<GeneratorExecutionContext> onExecute, Action<IncrementalGeneratorInitializationContext> onIncrementalInit)
            : base(onInit, onExecute)
        {
            _onInit = onIncrementalInit;
        }

        public void Initialize(IncrementalGeneratorInitializationContext context) => _onInit(context);
    }

    internal class ModifyTextGenerator(
        IEnumerable<(SyntaxTree tree, int position, string newText)>? insertTexts = null,
        IEnumerable<(SyntaxTree tree, TextSpan span, string newText)>? replaceTexts = null,
        IEnumerable<(SyntaxTree tree, TextSpan)>? removeTexts = null,
        IEnumerable<(SyntaxTree oldTree, SyntaxTree newTree)>? replaceSources = null,
        IEnumerable<SyntaxTree>? removeSources = null)
        : ISourceGenerator
    {
        private readonly IEnumerable<(SyntaxTree tree, int position, string newText)> _insertTexts = insertTexts ?? [];
        private readonly IEnumerable<(SyntaxTree tree, TextSpan span, string newText)> _replaceTexts = replaceTexts ?? [];
        private readonly IEnumerable<(SyntaxTree tree, TextSpan)> _removeTexts = removeTexts ?? [];
        private readonly IEnumerable<(SyntaxTree oldTree, SyntaxTree newTree)> _replaceSources = replaceSources ?? [];
        private readonly IEnumerable<SyntaxTree> _removeSources = removeSources ?? [];

        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context)
        {
            foreach (var tree in context.Compilation.SyntaxTrees)
            {
                Modify(tree, context.InsertText, context.ReplaceText, context.RemoveText, context.ReplaceSource, context.RemoveSource);
            }
        }

        protected void Modify(
            SyntaxTree tree,
            Action<SyntaxTree, int, string> insertTextAction,
            Action<SyntaxTree, TextSpan, string> replaceTextAction,
            Action<SyntaxTree, TextSpan> removeTextAction,
            Action<SyntaxTree, SyntaxTree> replaceSourceAction,
            Action<SyntaxTree> removeSourceAction)
        {
            foreach (var (t, position, newText) in _insertTexts)
            {
                if (ReferenceEquals(tree, t))
                {
                    insertTextAction(tree, position, newText);
                }
            }

            foreach (var (t, span, newText) in _replaceTexts)
            {
                if (ReferenceEquals(tree, t))
                {
                    replaceTextAction(tree, span, newText);
                }
            }

            foreach (var (t, span) in _removeTexts)
            {
                if (ReferenceEquals(tree, t))
                {
                    removeTextAction(tree, span);
                }
            }

            foreach (var (t, newTree) in _replaceSources)
            {
                if (ReferenceEquals(tree, t))
                {
                    replaceSourceAction(tree, newTree);
                }
            }

            foreach (var t in _removeSources)
            {
                if (ReferenceEquals(tree, t))
                {
                    removeSourceAction(tree);
                }
            }
        }
    }

    internal sealed class IncrementalModifyTextGenerator(
        IEnumerable<(SyntaxTree tree, int position, string newText)>? insertTexts = null,
        IEnumerable<(SyntaxTree tree, TextSpan span, string newText)>? replaceTexts = null,
        IEnumerable<(SyntaxTree tree, TextSpan)>? removeTexts = null,
        IEnumerable<(SyntaxTree oldTree, SyntaxTree newTree)>? replaceSources = null,
        IEnumerable<SyntaxTree>? removeSources = null)
        : ModifyTextGenerator(insertTexts, replaceTexts, removeTexts, replaceSources, removeSources), IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
            => context.RegisterSourceOutput(
                source: context.SyntaxProvider.CreateSyntaxProvider(
                    predicate: static (node, cancellationToken) => node == node.SyntaxTree.GetRoot(cancellationToken),
                    transform: static (context, _) => context.Node.SyntaxTree),
                action: (context, tree) => Modify(tree, context.InsertText, context.ReplaceText, context.RemoveText, context.ReplaceSource, context.RemoveSource));
    }
}

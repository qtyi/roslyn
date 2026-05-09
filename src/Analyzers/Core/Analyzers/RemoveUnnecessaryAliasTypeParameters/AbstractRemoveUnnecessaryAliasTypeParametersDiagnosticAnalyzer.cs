// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.CodeQuality;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryAliasTypeParameters;

using UsedNameDictionary = Dictionary<NameWithArity, HashSet<IAliasSymbol>>;
using FixInfoDictionary = Dictionary<IAliasSymbol, ImmutableDictionary<ITypeParameterSymbol, (ImmutableHashSet<SyntaxNode> declNodes, ImmutableHashSet<SyntaxNode> refNodes)>>;
using FixInfo = ImmutableDictionary<ITypeParameterSymbol, (ImmutableHashSet<SyntaxNode> declNodes, ImmutableHashSet<SyntaxNode> refNodes)>;

internal abstract class AbstractRemoveUnnecessaryAliasTypeParametersDiagnosticAnalyzer<
    TAliasDeclarationSyntax>
    : AbstractCodeQualityDiagnosticAnalyzer
    where TAliasDeclarationSyntax : SyntaxNode
{
    // IDE0901: "Remove unused alias type parameters" (Type parameter is declared but never referenced)
    private static readonly DiagnosticDescriptor s_removeUnusedAliasTypeParametersRule = CreateDescriptor(
        IDEDiagnosticIds.RemoveUnusedAliasTypeParametersDiagnosticId,
        EnforceOnBuildValues.RemoveUnusedAliasTypeParameters,
        new LocalizableResourceString(nameof(AnalyzersResources.Remove_unused_alias_type_parameters), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Alias_type_parameter_0_is_unused), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        hasAnyCodeStyleOption: false, isUnnecessary: true);

    // IDE0902: "Remove unnecessary alias type parameters" (Type parameter is declared but only referenced out of alias target)
    private static readonly DiagnosticDescriptor s_removeUnnecessaryAliasTypeParametersRule = CreateDescriptor(
        IDEDiagnosticIds.RemoveUnnecessaryAliasTypeParametersDiagnosticId,
        EnforceOnBuildValues.RemoveUnnecessaryAliasTypeParameters,
        new LocalizableResourceString(nameof(AnalyzersResources.Remove_unnecessary_alias_type_parameters), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Alias_type_parameter_0_is_unnecessary), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        hasAnyCodeStyleOption: false, isUnnecessary: true);

    protected AbstractRemoveUnnecessaryAliasTypeParametersDiagnosticAnalyzer()
        : base([s_removeUnusedAliasTypeParametersRule, s_removeUnnecessaryAliasTypeParametersRule],
               GeneratedCodeAnalysisFlags.None) // We want to analyze alias declaration in generated code, but not report unused type parameters in generated code.
    { }

    // We need to ask semantic questions about global alias declarations.
    // We don't need to analyze the whole document for edits within a method body
    public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected sealed override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(compilationStartAnalysisContext =>
        {
            var nameComparer = compilationStartAnalysisContext.Compilation.IsCaseSensitive
                                   ? NameWithArityComparer.Default
                                   : NameWithArityComparer.IgnoreCase;
            var worker = new RemoveWorker(this, nameComparer);

            compilationStartAnalysisContext.RegisterSemanticModelAction(worker.CollectFixInformation);
            compilationStartAnalysisContext.RegisterCompilationEndAction(worker.CheckAndReportDiagnostics);
        });
    }

    protected abstract bool IsInAliasTarget(SyntaxNode node, TAliasDeclarationSyntax aliasDeclaration);

    protected abstract bool IsPartOfTypeParameterDeclaration(SyntaxNode node);

    protected abstract bool IsGlobalAliasDeclaration(TAliasDeclarationSyntax aliasDeclaration, IAliasSymbol aliasSymbol);

    protected abstract bool IsTopLevelAliasDeclaration(TAliasDeclarationSyntax aliasDeclaration, IAliasSymbol aliasSymbol);

    private class RemoveWorker
    {
        private readonly AbstractRemoveUnnecessaryAliasTypeParametersDiagnosticAnalyzer<TAliasDeclarationSyntax> _analyzer;
        private readonly NameWithArityComparer _nameComparer;

        private readonly UsedNameDictionary _globalUsedNames;
        private readonly Dictionary<SyntaxNode, UsedNameDictionary> _topLevelUsedNames;
        private readonly Dictionary<SyntaxNode, UsedNameDictionary> _scopeUsedNames;
        private readonly FixInfoDictionary _globalFixInfos;
        private readonly Dictionary<SyntaxNode, FixInfoDictionary> _topLevelFixInfos;
        private readonly Dictionary<SyntaxNode, FixInfoDictionary> _scopeFixInfos;

        public RemoveWorker(
            AbstractRemoveUnnecessaryAliasTypeParametersDiagnosticAnalyzer<TAliasDeclarationSyntax> analyzer,
            NameWithArityComparer nameComparer)
        {
            _analyzer = analyzer;
            _nameComparer = nameComparer;

            _globalUsedNames = new(_nameComparer);
            _topLevelUsedNames = [];
            _scopeUsedNames = [];
            _globalFixInfos = [];
            _topLevelFixInfos = [];
            _scopeFixInfos = [];
        }

        private void AddUsedName(
            TAliasDeclarationSyntax declaration,
            IAliasSymbol aliasSymbol)
        {
            var nameWithArity = new NameWithArity(aliasSymbol.Name, aliasSymbol.Arity);
            if (_analyzer.IsGlobalAliasDeclaration(declaration, aliasSymbol))
            {
                _globalUsedNames!.GetOrAdd(nameWithArity, static () => [])
                                 .Add(aliasSymbol);
            }
            else if (_analyzer.IsTopLevelAliasDeclaration(declaration, aliasSymbol))
            {
                _topLevelUsedNames!.GetOrAdd(declaration.Parent!, static (_, nameComparer) => new(nameComparer), _nameComparer)
                                   .GetOrAdd(nameWithArity, static () => [])
                                   .Add(aliasSymbol);
            }
            else
            {
                _scopeUsedNames!.GetOrAdd(declaration.Parent!, static (_, nameComparer) => new(nameComparer), _nameComparer)
                               .GetOrAdd(nameWithArity, static () => [])
                               .Add(aliasSymbol);
            }
        }

        private void AddFixInfo(
            TAliasDeclarationSyntax declaration,
            IAliasSymbol aliasSymbol,
            ImmutableDictionary<ITypeParameterSymbol, (ImmutableHashSet<SyntaxNode>, ImmutableHashSet<SyntaxNode> refNodes)> fixInfo)
        {
            if (_analyzer.IsGlobalAliasDeclaration(declaration, aliasSymbol))
            {
                _globalFixInfos!.Add(aliasSymbol, fixInfo);
            }
            else if (_analyzer.IsTopLevelAliasDeclaration(declaration, aliasSymbol))
            {
                _topLevelFixInfos!.GetOrAdd(declaration.Parent!, static () => [])
                                  .Add(aliasSymbol, fixInfo);
            }
            else
            {
                _scopeFixInfos!.GetOrAdd(declaration.Parent!, static () => [])
                               .Add(aliasSymbol, fixInfo);
            }
        }

        public void CollectFixInformation(SemanticModelAnalysisContext context)
        {
            var root = context.GetAnalysisRoot(findInTrivia: false);
            foreach (var declaration in root.DescendantNodesAndSelf().OfType<TAliasDeclarationSyntax>())
            {
                var aliasSymbol = context.SemanticModel.GetDeclaredSymbol(declaration, context.CancellationToken) as IAliasSymbol;
                if (aliasSymbol is null)
                    continue;

                // In generated codes, we only collect alias information but not try to fix them.
                if (context.IsGeneratedCode)
                {
                    AddUsedName(declaration, aliasSymbol);
                    continue;
                }

                var fixInfo = LookUpUnnecessaryTypeParametersFixInfo(context, declaration, aliasSymbol);
                // If no need to fix.
                if (fixInfo.IsEmpty)
                {
                    AddUsedName(declaration, aliasSymbol);
                    continue;
                }

                AddFixInfo(declaration, aliasSymbol, fixInfo);
            }
        }

        private FixInfo LookUpUnnecessaryTypeParametersFixInfo(SemanticModelAnalysisContext context, TAliasDeclarationSyntax declaration, IAliasSymbol aliasSymbol)
        {
            if (!aliasSymbol.IsGenericAlias)
            {
                return ImmutableDictionary<ITypeParameterSymbol, (ImmutableHashSet<SyntaxNode>, ImmutableHashSet<SyntaxNode>)>.Empty;
            }

            ImmutableDictionary<ITypeParameterSymbol, (ImmutableHashSet<SyntaxNode>, ImmutableHashSet<SyntaxNode>)>.Builder? dicBuilder = null;
            foreach (var typeParameterSymbol in aliasSymbol.TypeParameters)
            {
                var usedInAliasTarget = false;
                ImmutableHashSet<SyntaxNode>.Builder? declBuilder = null;
                ImmutableHashSet<SyntaxNode>.Builder? refBuilder = null;
                foreach (var node in declaration.DescendantNodes())
                {
                    var symbol =
                        context.SemanticModel.GetDeclaredSymbol(node, context.CancellationToken) as ITypeParameterSymbol ??
                        context.SemanticModel.GetSymbolInfo(node, context.CancellationToken).Symbol as ITypeParameterSymbol;
                    if (typeParameterSymbol.Equals(symbol))
                    {
                        if (_analyzer.IsInAliasTarget(node, declaration))
                        {
                            usedInAliasTarget = true;
                            break;
                        }

                        if (_analyzer.IsPartOfTypeParameterDeclaration(node))
                        {
                            declBuilder ??= ImmutableHashSet.CreateBuilder<SyntaxNode>();
                            declBuilder.Add(node);
                        }
                        else
                        {
                            refBuilder ??= ImmutableHashSet.CreateBuilder<SyntaxNode>();
                            refBuilder.Add(node);
                        }
                    }
                }

                if (usedInAliasTarget)
                {
                    continue;
                }

                dicBuilder ??= ImmutableDictionary.CreateBuilder<ITypeParameterSymbol, (ImmutableHashSet<SyntaxNode>, ImmutableHashSet<SyntaxNode>)>();
                dicBuilder.Add(
                    typeParameterSymbol,
                    (declBuilder is null ? [] : declBuilder.ToImmutableHashSet(),
                     refBuilder is null ? [] : refBuilder.ToImmutableHashSet()));
            }

            return dicBuilder.ToImmutableDictionaryOrEmpty();
        }

        public void CheckAndReportDiagnostics(CompilationAnalysisContext context)
        {
            // First check top-level alias declarations.
            foreach ((var topLevelScope, var topLevelFixInfos) in _topLevelFixInfos)
            {
                UsedNameDictionary[] usedNameDictionaries = _topLevelUsedNames.TryGetValue(topLevelScope, out var topLevelUsedNames)
                    ? [_globalUsedNames, topLevelUsedNames]
                    : [_globalUsedNames];
                FixInfoDictionary[] fixInfoDictionaries = [_globalFixInfos, topLevelFixInfos];

                foreach (var fixInfo in GetAliasWithoutDuplicateNameWithArityInScope(usedNameDictionaries, fixInfoDictionaries))
                {
                    ReportFixInfoDiagnostics(context, fixInfo);
                }
            }

            // Then check alias declaration in each scope.
            foreach ((var scope, var scopeFixInfos) in _scopeFixInfos)
            {
                UsedNameDictionary[] usedNameDictionaries = _scopeUsedNames.TryGetValue(scope, out var scopeUsedNames)
                    ? [scopeUsedNames]
                    : [];
                FixInfoDictionary[] fixInfoDictionaries = [scopeFixInfos];

                foreach (var fixInfo in GetAliasWithoutDuplicateNameWithArityInScope(usedNameDictionaries, fixInfoDictionaries))
                {
                    ReportFixInfoDiagnostics(context, fixInfo);
                }
            }
        }

        private IEnumerable<FixInfo> GetAliasWithoutDuplicateNameWithArityInScope(UsedNameDictionary[] usedNameDictionaries, params FixInfoDictionary[] fixInfoDictionaries)
        {
            var fixedInfos = new FixInfoDictionary();
            var fixedNames = new UsedNameDictionary(_nameComparer);
            foreach ((var aliasSymbol, var fixInfo) in fixInfoDictionaries.SelectMany(static item => item))
            {
                Debug.Assert(!fixedInfos.ContainsKey(aliasSymbol));
                fixedInfos.Add(aliasSymbol, fixInfo);
                fixedNames.GetOrAdd(new(aliasSymbol.Name, aliasSymbol.Arity - fixInfo.Count), static () => [])
                         .Add(aliasSymbol);
            }

            foreach ((var nameWithArity, var aliasSymbols) in fixedNames)
            {
                if (aliasSymbols.Count > 1)
                    continue;

                // Name is used.
                if (usedNameDictionaries.Any(dic => dic.ContainsKey(nameWithArity)))
                    continue;

                var aliasSymbol = aliasSymbols.First();
                Debug.Assert(fixedInfos.ContainsKey(aliasSymbol));
                yield return fixedInfos[aliasSymbol];
            }
        }

        private static void ReportFixInfoDiagnostics(CompilationAnalysisContext context, FixInfo fixInfo)
        {
            foreach ((var tp, var nodes) in fixInfo)
            {
                var rule = nodes.refNodes.Count == 0 ? s_removeUnusedAliasTypeParametersRule : s_removeUnnecessaryAliasTypeParametersRule;

                context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
                    descriptor: rule,
                    location: tp.Locations.First(),
                    NotificationOption2.ForSeverity(rule.DefaultSeverity),
                    context.Options,
                    additionalLocations: [],
                    additionalUnnecessaryLocations: GetAdditionalUnnecessaryLocations(tp, fixInfo)));
            }
        }

        private static ImmutableArray<Location> GetAdditionalUnnecessaryLocations(ITypeParameterSymbol typeParameter, FixInfo fixInfo)
        {
            var (declNodes, refNodes) = fixInfo[typeParameter];
            var locations = ImmutableHashSet.CreateBuilder<Location>();
            foreach (var node in declNodes)
            {
                locations.Add(node.GetLocation());
            }
            foreach (var node in refNodes)
            {
                locations.Add(node.GetLocation());
            }

            locations.Remove(typeParameter.Locations.First());

            return locations.ToImmutableArray();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal sealed partial class SyntaxTreeIndex
{
    public bool ProbablyContainsIdentifier(string identifier) => _identifierInfo.ProbablyContainsIdentifier(identifier);
    public bool ProbablyContainsEscapedIdentifier(string identifier) => _identifierInfo.ProbablyContainsEscapedIdentifier(identifier);

    public bool ContainsPredefinedType(PredefinedType type) => _contextInfo.ContainsPredefinedType(type);
    public bool ContainsPredefinedOperator(PredefinedOperator op) => _contextInfo.ContainsPredefinedOperator(op);

    public bool ProbablyContainsStringValue(string value) => _literalInfo.ProbablyContainsStringValue(value);
    public bool ProbablyContainsInt64Value(long value) => _literalInfo.ProbablyContainsInt64Value(value);

    public bool ContainsAwait => _contextInfo.ContainsAwait;
    public bool ContainsBaseConstructorInitializer => _contextInfo.ContainsBaseConstructorInitializer;
    public bool ContainsConversion => _contextInfo.ContainsConversion;
    public bool ContainsDeconstruction => _contextInfo.ContainsDeconstruction;
    public bool ContainsExplicitOrImplicitElementAccessExpression => _contextInfo.ContainsExplicitOrImplicitElementAccessExpression;
    public bool ContainsForEachStatement => _contextInfo.ContainsForEachStatement;
    public bool ContainsGlobalKeyword => _contextInfo.ContainsGlobalKeyword;
    public bool ContainsGlobalSuppressMessageAttribute => _contextInfo.ContainsGlobalSuppressMessageAttribute;
    public bool ContainsImplicitObjectCreation => _contextInfo.ContainsImplicitObjectCreation;
    public bool ContainsIndexerMemberCref => _contextInfo.ContainsIndexerMemberCref;
    public bool ContainsLockStatement => _contextInfo.ContainsLockStatement;
    public bool ContainsQueryExpression => _contextInfo.ContainsQueryExpression;
    public bool ContainsThisConstructorInitializer => _contextInfo.ContainsThisConstructorInitializer;
    public bool ContainsTupleExpressionOrTupleType => _contextInfo.ContainsTupleExpressionOrTupleType;
    public bool ContainsUsingStatement => _contextInfo.ContainsUsingStatement;
    public bool ContainsCollectionInitializer => _contextInfo.ContainsCollectionInitializer;
    public bool ContainsAttribute => _contextInfo.ContainsAttribute;
    public bool ContainsArrayCreationExpressionOrArrayType => _contextInfo.ContainsArrayCreationExpressionOrArrayType;
    public bool ContainsPointerType => _contextInfo.ContainsPointerType;

    /// <summary>
    /// Gets the set of global aliases that point to something fit a certain condition.
    /// </summary>
    public ImmutableArray<NameWithArity> GetGlobalAliases(Func<AliasInfo, bool> predicate)
         => GetAliasesWorker(predicate, isGlobal: true);

    /// <summary>
    /// Gets the set of local aliases that point to something fit a certain condition.
    /// </summary>
    public ImmutableArray<NameWithArity> GetAliases(Func<AliasInfo, bool> predicate)
         => GetAliasesWorker(predicate, isGlobal: false);

    private ImmutableArray<NameWithArity> GetAliasesWorker(Func<AliasInfo, bool> predicate, bool isGlobal)
    {
        if (_aliasInfo == null)
            return [];

        using var _ = ArrayBuilder<NameWithArity>.GetInstance(out var result);

        foreach (var info in _aliasInfo)
        {
            if (info.IsGlobal == isGlobal && predicate(info))
            {
                result.Add(new NameWithArity(info.AliasName, info.AliasArity));
            }
        }

        return result.ToImmutable();
    }

    public static Func<AliasInfo, bool> FilterAliasesByName(string name, int arity, ISyntaxFacts syntaxFacts)
    {
        Debug.Assert(arity >= 0);

        return info => info.TargetKind == AliasTargetKind.Name &&
            syntaxFacts.StringComparer.Equals(info.TargetName, name) &&
            info.TargetArity == arity;
    }

    public static Func<AliasInfo, bool> FilterAliasesByTypeParameter(string name, ISyntaxFacts syntaxFacts)
    {
        return info => info.TargetKind == AliasTargetKind.TypeParameter &&
            syntaxFacts.StringComparer.Equals(info.TargetName, name);
    }

    public static Func<AliasInfo, bool> FilterAliasesByDynamic(ISyntaxFacts syntaxFacts)
    {
        return info => info.TargetKind == AliasTargetKind.Dynamic;
    }

    public static Func<AliasInfo, bool> FilterAliasesByArray(int rank, ISyntaxFacts syntaxFacts)
    {
        Debug.Assert(rank >= 1);

        return info => info.TargetKind == AliasTargetKind.Array &&
            info.TargetArity == rank;
    }

    public static Func<AliasInfo, bool> FilterAliasesByPointer(ISyntaxFacts syntaxFacts)
    {
        return info => info.TargetKind == AliasTargetKind.Pointer;
    }

    public static Func<AliasInfo, bool> FilterAliasesByFunctionPointer(int parameterCount, ISyntaxFacts syntaxFacts)
    {
        return info => info.TargetKind == AliasTargetKind.FunctionPointer &&
            info.TargetArity == parameterCount;
    }

    public bool TryGetInterceptsLocation(InterceptsLocationData data, out TextSpan span)
    {
        if (_interceptsLocationInfo == null)
        {
            span = default;
            return false;
        }

        return _interceptsLocationInfo.TryGetValue(data, out span);
    }
}

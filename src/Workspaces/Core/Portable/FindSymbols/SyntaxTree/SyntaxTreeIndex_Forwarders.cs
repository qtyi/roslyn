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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
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
        public bool ContainsArrayCreationExpressionOrArrayType => _contextInfo.ContainsArrayCreationExpressionOrArrayType;
        public bool ContainsPointerType => _contextInfo.ContainsPointerType;

        /// <summary>
        /// Gets the set of global aliases that point to something with the provided name and arity.
        /// For example of there is <c>global alias X = A.B.C&lt;int&gt;</c>, then looking up with
        /// <c>name="C"</c> and arity=1 will return <c>X</c>.
        /// </summary>
        public ImmutableArray<string> GetGlobalAliasesByName(string name, int arity, ISyntaxFacts syntaxFacts)
        {
            if (_globalAliasInfo == null)
                return ImmutableArray<string>.Empty;

            using var _ = ArrayBuilder<string>.GetInstance(out var result);

            foreach (var info in _globalAliasInfo)
            {
                if (info.TargetKind == AliasTargetKind.Name &&
                    syntaxFacts.StringComparer.Equals(info.TargetName, name) && info.TargetArity == arity)
                {
                    result.Add(info.AliasName);
                }
            }

            return result.ToImmutable();
        }

        public ImmutableArray<string> GetGlobalAliasesByTypeParameter(string name, ISyntaxFacts syntaxFacts)
        {
            if (_globalAliasInfo == null)
                return ImmutableArray<string>.Empty;

            using var _ = ArrayBuilder<string>.GetInstance(out var result);

            foreach (var info in _globalAliasInfo)
            {
                if (info.TargetKind == AliasTargetKind.TypeParameter && syntaxFacts.StringComparer.Equals(info.TargetName, name))
                {
                    result.Add(info.AliasName);
                }
            }

            return result.ToImmutable();
        }

        public ImmutableArray<string> GetGlobalAliasesByDynamic(ISyntaxFacts syntaxFacts)
        {
            if (_globalAliasInfo == null)
                return ImmutableArray<string>.Empty;

            using var _ = ArrayBuilder<string>.GetInstance(out var result);

            foreach (var info in _globalAliasInfo)
            {
                if (info.TargetKind == AliasTargetKind.Dynamic)
                {
                    result.Add(info.AliasName);
                }
            }

            return result.ToImmutable();
        }

        public ImmutableArray<string> GetGlobalAliasesByArray(int rank, ISyntaxFacts syntaxFacts)
        {
            Debug.Assert(rank >= 1);

            if (_globalAliasInfo == null)
                return ImmutableArray<string>.Empty;

            using var _ = ArrayBuilder<string>.GetInstance(out var result);

            foreach (var info in _globalAliasInfo)
            {
                if (info.TargetKind == AliasTargetKind.Array && info.TargetArity == rank)
                {
                    result.Add(info.AliasName);
                }
            }

            return result.ToImmutable();
        }

        public ImmutableArray<string> GetGlobalAliasesByPointer(ISyntaxFacts syntaxFacts)
        {
            if (_globalAliasInfo == null)
                return ImmutableArray<string>.Empty;

            using var _ = ArrayBuilder<string>.GetInstance(out var result);

            foreach (var info in _globalAliasInfo)
            {
                if (info.TargetKind == AliasTargetKind.Pointer)
                {
                    result.Add(info.AliasName);
                }
            }

            return result.ToImmutable();
        }

        public ImmutableArray<string> GetGlobalAliasesByFunctionPointer(int parameterCount, ISyntaxFacts syntaxFacts)
        {
            Debug.Assert(parameterCount >= 0);

            if (_globalAliasInfo == null)
                return ImmutableArray<string>.Empty;

            using var _ = ArrayBuilder<string>.GetInstance(out var result);

            foreach (var info in _globalAliasInfo)
            {
                if (info.TargetKind == AliasTargetKind.FunctionPointer && info.TargetArity == parameterCount)
                {
                    result.Add(info.AliasName);
                }
            }

            return result.ToImmutable();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification;

internal partial class CSharpTypeReducer : AbstractCSharpReducer
{
    private static readonly ObjectPool<IReductionRewriter> s_pool = new(
        () => new Rewriter(s_pool));

    public CSharpTypeReducer() : base(s_pool)
    {
    }

    protected override bool IsApplicable(CSharpSimplifierOptions options)
        => true;

    private static readonly Func<TypeSyntax, SemanticModel, CSharpSimplifierOptions, CancellationToken, ExpressionSyntax> s_simplifyType = SimplifyType;

    private static ExpressionSyntax SimplifyType(TypeSyntax node, SemanticModel semanticModel, CSharpSimplifierOptions options, CancellationToken cancellationToken)
    {
        ExpressionSyntax replacementNode;

        var expressionSyntax = (ExpressionSyntax)node;
        if (!ExpressionSimplifier.Instance.TrySimplify(expressionSyntax, semanticModel, options, out var expressionReplacement, out _, cancellationToken))
        {
            return node;
        }

        replacementNode = expressionReplacement;

        return node.CopyAnnotationsTo(replacementNode).WithAdditionalAnnotations(Formatter.Annotation).WithoutAnnotations(Simplifier.Annotation);
    }
}

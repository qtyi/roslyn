// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification;

internal partial class CSharpTypeReducer
{
    private class Rewriter : AbstractReductionRewriter
    {
        public Rewriter(ObjectPool<IReductionRewriter> pool)
            : base(pool)
        {
        }

        public override SyntaxNode Visit(SyntaxNode node)
        {
            var savedAlwaysSimplify = alwaysSimplify;
            alwaysSimplify = true;

            var result = base.Visit(node);

            alwaysSimplify = savedAlwaysSimplify;

            return result;
        }

        public override SyntaxNode VisitArrayType(ArrayTypeSyntax node)
        {
            return SimplifyNode(
                node,
                newNode: base.VisitArrayType(node),
                simplifier: s_simplifyType);
        }

        public override SyntaxNode VisitFunctionPointerType(FunctionPointerTypeSyntax node)
        {
            return SimplifyNode(
                node,
                newNode: base.VisitFunctionPointerType(node),
                simplifier: s_simplifyType);
        }

        public override SyntaxNode VisitNullableType(NullableTypeSyntax node)
        {
            return SimplifyNode(
                node,
                newNode: base.VisitNullableType(node),
                simplifier: s_simplifyType);
        }

        public override SyntaxNode VisitPointerType(PointerTypeSyntax node)
        {
            return SimplifyNode(
                node,
                newNode: base.VisitPointerType(node),
                simplifier: s_simplifyType);
        }

        public override SyntaxNode VisitTupleType(TupleTypeSyntax node)
        {
            return SimplifyNode(
                node,
                newNode: base.VisitTupleType(node),
                simplifier: s_simplifyType);
        }
    }
}

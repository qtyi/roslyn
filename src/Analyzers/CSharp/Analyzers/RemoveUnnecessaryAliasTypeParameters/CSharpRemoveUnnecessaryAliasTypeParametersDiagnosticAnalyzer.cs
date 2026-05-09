// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.RemoveUnnecessaryAliasTypeParameters;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryAliasTypeParameters;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpRemoveUnnecessaryAliasTypeParametersDiagnosticAnalyzer
    : AbstractRemoveUnnecessaryAliasTypeParametersDiagnosticAnalyzer<
        UsingDirectiveSyntax>
{
    protected override bool IsInAliasTarget(SyntaxNode node, UsingDirectiveSyntax aliasDeclaration)
    {
        return aliasDeclaration.NamespaceOrType.Span.Contains(node.Span);
    }

    protected override bool IsPartOfTypeParameterDeclaration(SyntaxNode node)
    {
        if (node is TypeParameterSyntax)
        {
            return true;
        }

        if (node is IdentifierNameSyntax identifierName &&
            identifierName.Parent is TypeParameterConstraintClauseSyntax)
        {
            return true;
        }

        return false;
    }

    protected override bool IsGlobalAliasDeclaration(UsingDirectiveSyntax declaration, IAliasSymbol aliasSymbol)
    {
        var globalKeyword = declaration.GlobalKeyword;
        return globalKeyword != default;
    }

    protected override bool IsTopLevelAliasDeclaration(UsingDirectiveSyntax declaration, IAliasSymbol aliasSymbol)
    {
        return aliasSymbol.ContainingNamespace.IsGlobalNamespace;
    }
}

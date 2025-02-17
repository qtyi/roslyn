﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InitializeParameter;

namespace Microsoft.CodeAnalysis.CSharp.InitializeParameter;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.AddParameterCheck), Shared]
[ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.ChangeSignature)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpAddParameterCheckCodeRefactoringProvider() :
    AbstractAddParameterCheckCodeRefactoringProvider<
        BaseTypeDeclarationSyntax,
        ParameterSyntax,
        StatementSyntax,
        ExpressionSyntax,
        BinaryExpressionSyntax,
        CSharpSimplifierOptions>
{
    protected override bool IsFunctionDeclaration(SyntaxNode node)
        => InitializeParameterHelpers.IsFunctionDeclaration(node);

    protected override SyntaxNode GetBody(SyntaxNode functionDeclaration)
        => InitializeParameterHelpers.GetBody(functionDeclaration);

    protected override bool IsImplicitConversion(Compilation compilation, ITypeSymbol source, ITypeSymbol destination)
        => InitializeParameterHelpers.IsImplicitConversion(compilation, source, destination);

    protected override bool CanOffer(SyntaxNode body)
    {
        if (InitializeParameterHelpers.IsExpressionBody(body))
        {
            return InitializeParameterHelpers.TryConvertExpressionBodyToStatement(body,
                semicolonToken: SemicolonToken,
                createReturnStatementForExpression: false,
                statement: out var _);
        }

        return true;
    }

    protected override bool PrefersThrowExpression(CSharpSimplifierOptions options)
        => options.PreferThrowExpression.Value;

    protected override string EscapeResourceString(string input)
        => input.Replace("\\", "\\\\").Replace("\"", "\\\"");

    protected override StatementSyntax CreateParameterCheckIfStatement(ExpressionSyntax condition, StatementSyntax ifTrueStatement, CSharpSimplifierOptions options)
    {
        var withBlock = options.PreferBraces.Value == CodeAnalysis.CodeStyle.PreferBracesPreference.Always;
        var singleLine = options.AllowEmbeddedStatementsOnSameLine.Value;
        var closeParenToken = CloseParenToken;
        if (withBlock)
        {
            ifTrueStatement = Block(ifTrueStatement);
        }
        else if (singleLine)
        {
            // Any elastic trivia between the closing parenthesis of if and the statement must be removed
            // to convince the formatter to keep everything on a single line.
            // Note: ifTrueStatement and closeParenToken are generated, so there is no need to deal with any existing trivia.
            closeParenToken = closeParenToken.WithTrailingTrivia(Space);
            ifTrueStatement = ifTrueStatement.WithoutLeadingTrivia();
        }

        return IfStatement(
            attributeLists: default,
            ifKeyword: IfKeyword,
            openParenToken: OpenParenToken,
            condition: condition,
            closeParenToken: closeParenToken,
            statement: ifTrueStatement,
            @else: null);
    }
}

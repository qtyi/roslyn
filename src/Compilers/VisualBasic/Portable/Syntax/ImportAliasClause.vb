' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Public Class SyntaxFactory
        ''' <summary>
        ''' Represents an alias identifier followed by an "=" token in an Imports clause.
        ''' </summary>
        ''' <param name="identifier">
        ''' The identifier being introduced.
        ''' </param>
        ''' <param name="equalsToken">
        ''' The "=" token.
        ''' </param>
        Public Shared Function ImportAliasClause(identifier As SyntaxToken, equalsToken As SyntaxToken) As ImportAliasClauseSyntax
            Return ImportAliasClause(identifier, typeParameterList:=Nothing, equalsToken)
        End Function
    End Class
End Namespace

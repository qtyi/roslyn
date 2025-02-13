' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Namespace Syntax
        Partial Public Class SimpleImportsClauseSyntax
            ''' <inheritdoc cref="NamespaceOrType"/>
            Public ReadOnly Property Name As NameSyntax
                Get
                    Return TryCast(NamespaceOrType, NameSyntax)
                End Get
            End Property
        End Class
    End Namespace

    Partial Public Class SyntaxFactory
        ''' <summary>
        ''' Represents the clause of an Imports statement that imports all members of a
        ''' type or namespace or aliases a type or namespace.
        ''' </summary>
        ''' <param name="alias">
        ''' An optional alias for the namespace or type being imported.
        ''' </param>
        ''' <param name="name">
        ''' The namespace or type being imported.
        ''' </param>
        Public Shared Function SimpleImportsClause([alias] As ImportAliasClauseSyntax, name As NameSyntax) As SimpleImportsClauseSyntax
            Return SimpleImportsClause([alias], namespaceOrType:=name)
        End Function

        ''' <summary>
        ''' Represents the clause of an Imports statement that imports all members of a
        ''' type or namespace or aliases a type or namespace.
        ''' </summary>
        ''' <param name="name">
        ''' The namespace or type being imported.
        ''' </param>
        Public Shared Function SimpleImportsClause(name As NameSyntax) As SimpleImportsClauseSyntax
            Return SimpleImportsClause(namespaceOrType:=name)
        End Function
    End Class
End Namespace

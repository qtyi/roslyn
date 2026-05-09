' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Structure NamedTypeOrAliasSymbol
        Public ReadOnly NamedTypeSymbol As NamedTypeSymbol
        Public ReadOnly AliasSymbol As AliasSymbol

        Private Sub New(namedTypeSymbol As NamedTypeSymbol)
            Me.NamedTypeSymbol = namedTypeSymbol
        End Sub

        Private Sub New(aliasSymbol As AliasSymbol)
            Me.AliasSymbol = aliasSymbol
        End Sub

        Public ReadOnly Property IsDefault As Boolean
            Get
                Return Not IsNamedType AndAlso Not IsAlias
            End Get
        End Property

        Public ReadOnly Property IsNamedType As Boolean
            Get
                Return NamedTypeSymbol IsNot Nothing
            End Get
        End Property

        Public ReadOnly Property IsAlias As Boolean
            Get
                Return AliasSymbol IsNot Nothing
            End Get
        End Property

        Public ReadOnly Property IsGeneric As Boolean
            Get
                Debug.Assert(Not IsDefault, "No named type or alias symbol")
                If IsNamedType Then
                    Return NamedTypeSymbol.IsGenericType
                Else
                    Return AliasSymbol.IsGenericAlias
                End If
            End Get
        End Property

        Public ReadOnly Property CanConstruct As Boolean
            Get
                Debug.Assert(Not IsDefault, "No named type or alias symbol")
                If IsNamedType Then
                    Return NamedTypeSymbol.CanConstruct
                Else
                    Return AliasSymbol.Target.IsType
                End If
            End Get
        End Property

        Public ReadOnly Property Arity As Integer
            Get
                Debug.Assert(Not IsDefault, "No named type or alias symbol")
                If IsNamedType Then
                    Return NamedTypeSymbol.Arity
                Else
                    Return AliasSymbol.Arity
                End If
            End Get
        End Property

        Public ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Debug.Assert(Not IsDefault, "No named type or alias symbol")
                If IsNamedType Then
                    Return NamedTypeSymbol.TypeParameters
                Else
                    Return AliasSymbol.TypeParameters
                End If
            End Get
        End Property

        Public Function Construct(typeArguments As ImmutableArray(Of TypeSymbol)) As TypeSymbol
            Debug.Assert(CanConstruct, "Named type or alias symbol cannot construct")
            If IsNamedType Then
                Return NamedTypeSymbol.Construct(typeArguments)
            Else
                Return AliasSymbol.Construct(typeArguments)
            End If
        End Function

        Public Shared Widening Operator CType(namedTypeSymbol As NamedTypeSymbol) As NamedTypeOrAliasSymbol
            Return New NamedTypeOrAliasSymbol(namedTypeSymbol)
        End Operator

        Public Shared Widening Operator CType(aliasSymbol As AliasSymbol) As NamedTypeOrAliasSymbol
            Return New NamedTypeOrAliasSymbol(aliasSymbol)
        End Operator
    End Structure
End Namespace

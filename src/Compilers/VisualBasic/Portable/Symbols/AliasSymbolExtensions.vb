' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Module AliasSymbolExtensions

        ''' <summary>
        ''' Given a generic alias, create a type symbol representing its unbound target.
        ''' </summary>
        <Extension()>
        Public Function AsUnboundGenericType(this As AliasSymbol) As TypeSymbol
            Dim type As TypeSymbol = DirectCast(this.Target, TypeSymbol)

            If Not this.IsGenericAlias Then
                Return type
            End If

            Dim containsAliasTypeParameter = type.VisitType(Function(t As TypeSymbol, a As AliasSymbol) t.TypeKind = TypeKind.TypeParameter, this) IsNot Nothing
            If Not containsAliasTypeParameter Then
                Return type
            End If

            Dim namedType = DirectCast(type, NamedTypeSymbol)
            If namedType.IsTupleType Then
                Return TupleTypeSymbol.TransformToTupleIfCompatible(namedType.TupleUnderlyingType.AsUnboundGenericType())
            Else
                Return namedType.AsUnboundGenericType()
            End If
        End Function

    End Module
End Namespace

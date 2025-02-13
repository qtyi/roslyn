' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend NotInheritable Class SourceAliasSymbol
        Inherits AliasSymbol

        Private ReadOnly _binder As Binder
        Private ReadOnly _importClause As SyntaxReference

        Friend Sub New(binder As Binder,
                       importClause As SimpleImportsClauseSyntax,
                       Optional isDuplicate As Boolean = False)
            MyBase.New(binder.Compilation, binder.ContainingNamespaceOrType)

            Debug.Assert(importClause.Alias IsNot Nothing)
            Debug.Assert(importClause.Alias.Identifier <> Nothing)

            Me._binder = binder
            MyBase._aliasName = importClause.Alias.Identifier.ValueText
            MyBase._aliasArity = If(importClause.Alias.TypeParameterList Is Nothing, 0, importClause.Alias.TypeParameterList.Parameters.Count)
            If isDuplicate Then
                ' A dulicate alias symbol in source use the location of SimpleImportsClauseSyntax as its declaration location.
                MyBase._aliasLocations = ImmutableArray.Create(importClause.GetLocation())
            Else
                MyBase._aliasLocations = ImmutableArray.Create(If(binder.BindingLocation = BindingLocation.ProjectImportsDeclaration, NoLocation.Singleton, importClause.Alias.Identifier.GetLocation()))
            End If
            Me._importClause = importClause.GetReference()
        End Sub

        Public Overrides ReadOnly Property Target As NamespaceOrTypeSymbol
            Get
                Return GetAliasTarget()
            End Get
        End Property

        Public Overrides ReadOnly Property TargetDiagnostics As BindingDiagnosticBag
            Get
                GetAliasTarget()
                Return Me._aliasTargetDiagnostics
            End Get
        End Property

        Private Function GetAliasTarget() As NamespaceOrTypeSymbol
            If MyBase._aliasTargetDiagnostics Is Nothing Then
                Interlocked.CompareExchange(MyBase._aliasTargetDiagnostics, BindingDiagnosticBag.GetInstance(), Nothing)
            End If

            If MyBase._aliasTarget Is Nothing Then
                Dim syntax = DirectCast(Me._importClause.GetVisualBasicSyntax(), SimpleImportsClauseSyntax)
                Dim targetBinder = BinderBuilder.CreateBinderForAliasImportsClause(Me, Me._binder)
                Dim symbol = targetBinder.BindNamespaceOrTypeSyntax(syntax.NamespaceOrType, MyBase._aliasTargetDiagnostics)

                If (symbol.Kind = SymbolKind.Namespace AndAlso Me.IsGenericAlias) Then
                    ' Generic alias cannot target to namespace symbol
                    Binder.ReportDiagnostic(MyBase._aliasTargetDiagnostics,
                                            syntax,
                                            ERRID.ERR_InvalidTypeForAliasesImport3,
                                            symbol,
                                            symbol.Name)
                End If

                If symbol.Kind <> SymbolKind.Namespace Then
                    Dim type = TryCast(symbol, TypeSymbol)

                    If type Is Nothing OrElse type.IsDelegateType Then
                        Binder.ReportDiagnostic(MyBase._aliasTargetDiagnostics,
                                                syntax,
                                                If(Me.IsGenericAlias, ERRID.ERR_InvalidTypeForAliasesImport3, ERRID.ERR_InvalidTypeForAliasesImport2),
                                                symbol,
                                                symbol.Name)
                    End If
                End If

                If symbol.Kind <> SymbolKind.ErrorType Then
                    Dim useSiteInfo As UseSiteInfo(Of AssemblySymbol) = symbol.GetUseSiteInfo()

                    If ShouldReportUseSiteErrorForAlias(useSiteInfo.DiagnosticInfo) Then
                        Binder.ReportUseSite(MyBase._aliasTargetDiagnostics, syntax, useSiteInfo)
                    Else
                        MyBase._aliasTargetDiagnostics.AddDependencies(useSiteInfo)
                    End If
                Else
                    MyBase._aliasTargetDiagnostics.DependenciesBag.Clear()
                End If

                ' We resolve the alias symbol even when the target is erroneous, 
                ' so that we can bind to the alias and avoid cascading errors.
                ' As a result the further consumers of the aliases have to account for the error case.
                Interlocked.CompareExchange(MyBase._aliasTarget, symbol, Nothing)
            End If

            Return MyBase._aliasTarget
        End Function

        ''' <summary>
        ''' Checks use site error and returns True in case it should be reported for the alias. 
        ''' In current implementation checks for errors #36924 and #36925
        ''' </summary>
        Private Shared Function ShouldReportUseSiteErrorForAlias(useSiteErrorInfo As DiagnosticInfo) As Boolean
            Return useSiteErrorInfo IsNot Nothing AndAlso
                       useSiteErrorInfo.Code <> ERRID.ERR_CannotUseGenericTypeAcrossAssemblyBoundaries AndAlso
                       useSiteErrorInfo.Code <> ERRID.ERR_CannotUseGenericBaseTypeAcrossAssemblyBoundaries
        End Function

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray.Create(Me._importClause)
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                If MyBase._aliasTypeParameters.IsDefault Then
                    ImmutableInterlocked.InterlockedInitialize(MyBase._aliasTypeParameters, MakeTypeParameters())
                End If

                Return MyBase._aliasTypeParameters
            End Get
        End Property

        Private Function MakeTypeParameters() As ImmutableArray(Of TypeParameterSymbol)
            Dim n = Me.Arity
            If n = 0 Then
                Return ImmutableArray(Of TypeParameterSymbol).Empty
            End If

            Dim typeParameters(0 To n - 1) As TypeParameterSymbol

            For i = 0 To n - 1
                Dim tree = _importClause.SyntaxTree
                Dim syntaxNode = DirectCast(_importClause.GetVisualBasicSyntax(), SimpleImportsClauseSyntax)
                Dim typeParamListSyntax = syntaxNode.Alias.TypeParameterList.Parameters
                Debug.Assert(typeParamListSyntax.Count = n)
                Dim typeParamSyntax = typeParamListSyntax(i)
                Dim typeParamName = typeParamSyntax.Identifier.ValueText
                Debug.Assert(typeParamName IsNot Nothing)

                typeParameters(i) = New SourceTypeParameterOnAliasSymbol(Me, i, typeParamName, tree.GetReference(typeParamSyntax))
            Next

            Return typeParameters.AsImmutableOrNull()
        End Function

        ''' <summary>
        ''' Bind the constraint declarations for the given type parameter.
        ''' </summary>
        ''' <remarks>
        ''' The caller is expected to handle constraint checking and any caching of results.
        ''' </remarks>
        Friend Function BindTypeParameterConstraints(syntax As TypeParameterSyntax,
                                                     diagnostics As BindingDiagnosticBag) As ImmutableArray(Of TypeParameterConstraint)
            Dim tpBinder = BinderBuilder.CreateBinderForAliasImportsClause(Me, Me._binder)

            ' Handle type parameter variance.
            If syntax.VarianceKeyword.Kind <> SyntaxKind.None Then
                Binder.ReportDiagnostic(diagnostics, syntax.VarianceKeyword, ERRID.ERR_VarianceDisallowedHere)
            End If

            ' Wrap constraints binder in a location-specific binder to
            ' avoid checking constraints when binding type names.
            tpBinder = New LocationSpecificBinder(BindingLocation.GenericConstraintsClause, Me, tpBinder)
            Return tpBinder.BindTypeParameterConstraintClause(Me, syntax.TypeParameterConstraintClause, diagnostics)
        End Function
    End Class

End Namespace

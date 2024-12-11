' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Data for Binder.BindImportClause that exposes dictionaries of
    ''' the members and aliases that have been bound during the
    ''' execution of BindImportClause. It is the responsibility of derived
    ''' classes to update the dictionaries in AddMember and AddAlias.
    ''' </summary>
    Friend MustInherit Class ImportData
        Protected Sub New(members As HashSet(Of NamespaceOrTypeSymbol),
                          aliases As Dictionary(Of NameWithArity, AliasAndImportsClausePosition),
                          xmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition))
            Me.Members = members
            Me.Aliases = aliases
            Me.XmlNamespaces = xmlNamespaces
        End Sub

        Public ReadOnly Members As HashSet(Of NamespaceOrTypeSymbol)
        Public ReadOnly Aliases As Dictionary(Of NameWithArity, AliasAndImportsClausePosition)
        Public ReadOnly XmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition)

        Public MustOverride Sub AddMember(syntaxRef As SyntaxReference, member As NamespaceOrTypeSymbol, importsClausePosition As Integer, dependencies As IReadOnlyCollection(Of AssemblySymbol), isProjectImportsDeclaration As Boolean)
        Public MustOverride Sub AddAlias(syntaxRef As SyntaxReference, name As String, arity As Integer, [alias] As AliasSymbol, importsClausePosition As Integer, dependencies As IReadOnlyCollection(Of AssemblySymbol))
    End Class

    Partial Friend Class Binder
        ' Bind one import clause, add it to the correct collection of imports.
        ' Warnings and errors are emitted to the diagnostic bag, including detecting duplicates.
        ' Note that this binder should have been already been set up to only bind to things in the global namespace.
        Public Sub BindImportClause(importClauseSyntax As ImportsClauseSyntax,
                                          data As ImportData,
                                          diagBag As DiagnosticBag)
            ImportsBinder.BindImportClause(importClauseSyntax, Me, data, diagBag)
        End Sub

        ''' <summary>
        ''' The Imports binder handles binding of Imports statements in files, and also the project-level imports.
        ''' </summary>
        Private Class ImportsBinder
            ' Bind one import clause, add it to the correct collection of imports.
            ' Warnings and errors are emitted to the diagnostic bag, including detecting duplicates.
            ' Note that the binder should have been already been set up to only bind to things in the global namespace.
            Public Shared Sub BindImportClause(importClauseSyntax As ImportsClauseSyntax,
                                                     binder As Binder,
                                                     data As ImportData,
                                                     diagBag As DiagnosticBag)
                Select Case importClauseSyntax.Kind
                    Case SyntaxKind.SimpleImportsClause

                        Dim simpleImportsClause = DirectCast(importClauseSyntax, SimpleImportsClauseSyntax)

                        If simpleImportsClause.Alias Is Nothing Then
                            BindMembersImportsClause(simpleImportsClause, binder, data, diagBag)
                        Else
                            BindAliasImportsClause(simpleImportsClause, binder, data, diagBag)
                        End If

                    Case SyntaxKind.XmlNamespaceImportsClause
                        BindXmlNamespaceImportsClause(DirectCast(importClauseSyntax, XmlNamespaceImportsClauseSyntax), binder, data, diagBag)
                    Case Else
                End Select

            End Sub

            ' Bind an alias imports clause. If it is OK, and also unique, add it to the dictionary.
            Private Shared Sub BindAliasImportsClause(aliasImportSyntax As SimpleImportsClauseSyntax,
                                                      binder As Binder,
                                                      data As ImportData,
                                                      diagnostics As DiagnosticBag)
                Dim aliasIdentifier = aliasImportSyntax.Alias.Identifier
                Dim aliasTypeParameterList = aliasImportSyntax.Alias.TypeParameterList
                Dim aliasText = aliasIdentifier.ValueText
                Dim aliasArity = If(aliasTypeParameterList Is Nothing, 0, aliasTypeParameterList.Parameters.Count)
                ' Parser checks for type characters on alias text, so don't need to check again here.

                ' Check for duplicate symbol.
                If data.Aliases.ContainsKey(New NameWithArity(aliasText, aliasArity)) Then
                    Binder.ReportDiagnostic(diagnostics, aliasIdentifier, ERRID.ERR_DuplicateNamedImportAlias1, aliasText)
                Else
                    ' Make sure that the Import's alias doesn't have the same name as a type or a namespace in the global namespace
                    Dim conflictsWith = binder.Compilation.GlobalNamespace.GetMembers(aliasText).WhereAsArray(Function(s) s.GetArity() = aliasArity)

                    If Not conflictsWith.IsEmpty Then
                        ' TODO: Note that symbol's name in this error message is supposed to include Class/Namespace word at the beginning.
                        '       Might need to use special format for that parameter in the error message. 
                        Binder.ReportDiagnostic(diagnostics,
                                                    aliasImportSyntax,
                                                    ERRID.ERR_ImportAliasConflictsWithType2,
                                                    aliasText,
                                                    conflictsWith(0))
                    Else
                        Dim aliasSymbol = New SourceAliasSymbol(binder, aliasImportSyntax)
                        Dim aliasTargetDiagBag = aliasSymbol.TargetDiagnostics

                        data.AddAlias(binder.GetSyntaxReference(aliasImportSyntax), aliasText, aliasArity, aliasSymbol, aliasImportSyntax.SpanStart, DirectCast(aliasTargetDiagBag.DependenciesBag, IReadOnlyCollection(Of AssemblySymbol)))

                        diagnostics.AddRange(aliasTargetDiagBag.DiagnosticBag)
                    End If
                End If
            End Sub

            ' Bind a members imports clause. If it is OK, and also unique, add it to the members imports set.
            Private Shared Sub BindMembersImportsClause(membersImportsSyntax As SimpleImportsClauseSyntax,
                                                        binder As Binder,
                                                        data As ImportData,
                                                        diagnostics As DiagnosticBag)
                Dim diagBag = BindingDiagnosticBag.GetInstance()

                Debug.Assert(membersImportsSyntax.Alias Is Nothing)
                Dim importsName = membersImportsSyntax.NamespaceOrType
                Dim importedSymbol As NamespaceOrTypeSymbol = binder.BindNamespaceOrTypeSyntax(importsName, diagBag)

                If importedSymbol.Kind <> SymbolKind.Namespace Then
                    Dim type = TryCast(importedSymbol, TypeSymbol)

                    ' Non-aliased interface imports are disallowed
                    If type Is Nothing OrElse type.IsDelegateType OrElse type.IsInterfaceType Then
                        Binder.ReportDiagnostic(diagBag,
                                                membersImportsSyntax,
                                                ERRID.ERR_NonNamespaceOrClassOnImport2,
                                                importedSymbol,
                                                importedSymbol.Name)
                    End If
                End If

                If importedSymbol.Kind <> SymbolKind.ErrorType Then
                    ' Check for duplicate symbol.
                    If data.Members.Contains(importedSymbol) Then
                        ' NOTE: Dev10 doesn't report this error for project level imports. We still 
                        '       generate the error but filter it out when bind project level imports
                        Binder.ReportDiagnostic(diagBag, importsName, ERRID.ERR_DuplicateImport1, importedSymbol)
                    Else
                        Dim importedSymbolIsValid As Boolean = True

                        ' Disallow importing different instantiations of the same generic type.
                        If importedSymbol.Kind = SymbolKind.NamedType Then
                            Dim namedType = DirectCast(importedSymbol, NamedTypeSymbol)

                            If namedType.IsGenericType Then
                                namedType = namedType.OriginalDefinition

                                For Each contender In data.Members
                                    If contender.OriginalDefinition Is namedType Then
                                        importedSymbolIsValid = False
                                        Binder.ReportDiagnostic(diagBag, importsName, ERRID.ERR_DuplicateRawGenericTypeImport1, namedType)
                                        Exit For
                                    End If
                                Next
                            End If
                        End If

                        If importedSymbolIsValid Then
                            data.AddMember(binder.GetSyntaxReference(importsName), importedSymbol, membersImportsSyntax.SpanStart, DirectCast(diagBag.DependenciesBag, IReadOnlyCollection(Of AssemblySymbol)), binder.BindingLocation = BindingLocation.ProjectImportsDeclaration)
                        End If
                    End If
                End If

                diagnostics.AddRange(diagBag.DiagnosticBag)
                diagBag.Free()
            End Sub

            Private Shared Sub BindXmlNamespaceImportsClause(syntax As XmlNamespaceImportsClauseSyntax,
                                                             binder As Binder,
                                                             data As ImportData,
                                                             diagnostics As DiagnosticBag)
#If DEBUG Then
                Dim diagBag = BindingDiagnosticBag.GetInstance()
#Else
                Dim diagBag = BindingDiagnosticBag.GetInstance(withDiagnostics:=True, withDependencies:=False)
#End If

                Dim prefix As String = Nothing
                Dim namespaceName As String = Nothing
                Dim [namespace] As BoundExpression = Nothing
                Dim hasErrors As Boolean = False
                If binder.TryGetXmlnsAttribute(syntax.XmlNamespace, prefix, namespaceName, [namespace], hasErrors, fromImport:=True, diagnostics:=diagBag) AndAlso
                    Not hasErrors Then
                    Debug.Assert([namespace] Is Nothing) ' Binding should have been skipped.

                    If data.XmlNamespaces.ContainsKey(prefix) Then
                        ' "XML namespace prefix '{0}' is already declared."
                        Binder.ReportDiagnostic(diagBag, syntax, ERRID.ERR_DuplicatePrefix, prefix)
                    Else
                        ' Do not expose any locations for project level xml namespaces.  This matches the effective
                        ' logic we have for aliases, which are given NoLocation.Singleton (which never translates to a
                        ' DeclaringSyntaxReference).
                        Dim reference = If(binder.BindingLocation = BindingLocation.ProjectImportsDeclaration,
                            Nothing,
                            binder.GetSyntaxReference(syntax))
                        data.XmlNamespaces.Add(prefix, New XmlNamespaceAndImportsClausePosition(namespaceName, syntax.SpanStart, reference))
                    End If
                End If
#If DEBUG Then
                Debug.Assert(diagBag.DependenciesBag.Count = 0)
#End If
                diagnostics.AddRange(diagBag.DiagnosticBag)
                diagBag.Free()
            End Sub
        End Class
    End Class
End Namespace

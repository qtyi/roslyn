' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Option Strict On
Option Infer On
Option Explicit On
Option Compare Binary

Namespace Global.System.Runtime.CompilerServices
    <Global.Microsoft.VisualBasic.Embedded()>
    <Global.System.AttributeUsage(Global.System.AttributeTargets.Assembly, AllowMultiple:=True, Inherited:=False)>
    <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)>
    <Global.System.Runtime.CompilerServices.CompilerGenerated()>
    Friend NotInheritable Class IgnoresAccessChecksToAttribute
        Inherits Global.System.Attribute
        Public ReadOnly AssemblyName As String
        Public Sub New(AssemblyName As String)
            Me.AssemblyName = AssemblyName
        End Sub
    End Class
End Namespace

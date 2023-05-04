// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A context for binding type parameter symbols of type alias.
    /// </summary>
    internal sealed class AliasTypeParameterBuilder
    {
        private readonly SyntaxReference _syntaxRef;
        private readonly AliasSymbolFromSyntax _owner;
        private readonly Location _location;

        internal AliasTypeParameterBuilder(SyntaxReference syntaxRef, AliasSymbolFromSyntax owner, Location location)
        {
            _syntaxRef = syntaxRef;
            Debug.Assert(syntaxRef.GetSyntax().IsKind(SyntaxKind.TypeParameter));
            _owner = owner;
            _location = location;
        }

        internal TypeParameterSymbol MakeSymbol(int ordinal, BindingDiagnosticBag diagnostics)
        {
            var syntaxNode = (TypeParameterSyntax)_syntaxRef.GetSyntax();
            var result = new SourceAliasTypeParameterSymbol(
                _owner,
                syntaxNode.Identifier.ValueText,
                ordinal,
                _location,
                _syntaxRef);

            // SPEC: A type parameter [of a type] cannot have the same name as the type itself.
            if (result.Name == result.ContainingSymbol.Name)
            {
                diagnostics.Add(ErrorCode.ERR_TypeVariableSameAsParent, result.GetFirstLocation(), result.Name);
            }

            return result;
        }
    }
}

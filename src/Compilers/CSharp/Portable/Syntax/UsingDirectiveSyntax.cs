// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class UsingDirectiveSyntax
    {
        /// <summary>
        /// Returns the name this <see cref="UsingDirectiveSyntax"/> points at, or <see langword="null"/> if it does not
        /// point at a name.  A normal <c>using X.Y.Z;</c> or <c>using static X.Y.Z;</c> will always point at a name and
        /// will always return a value for this.  However, a using-alias (e.g. <c>using x = ...;</c>) may or may not
        /// point at a name and may return <see langword="null"/> here.  An example of when that may happen is the type
        /// on the right side of the <c>=</c> is not a name.  For example <c>using x = (X.Y.Z, A.B.C);</c>.  Here, as
        /// the type is a tuple-type there is no name to return.
        /// </summary>
        public NameSyntax? Name => this.NamespaceOrType as NameSyntax;

        /// <summary>
        /// Returns a <see cref="NameEqualsSyntax"/> that describe the alias (if exist) of this <see cref="UsingDirectiveSyntax"/>.
        /// This property is now only used for compatibility for Roslyn extension references, like System.Text.Json.SourceGenerator,
        /// to test alias absence. To get alias name and equals token, use <see cref="Identifier"/> and <see cref="EqualsToken"/> instead.
        /// </summary>
        [System.Obsolete("This property is only used for compatibility to test alias absence. To get alias name, use Identifier instead.", error: true)]
        public NameEqualsSyntax? Alias
        {
            get
            {
                if (((InternalSyntax.UsingDirectiveSyntax)this.CsGreen).Identifier is null)
                {
                    return null;
                }

                return SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(this.Identifier), this.EqualsToken);
            }
        }

        public UsingDirectiveSyntax Update(SyntaxToken usingKeyword, SyntaxToken staticKeyword, SyntaxToken identifier, TypeParameterListSyntax typeParameters, SyntaxToken equalsToken, NameSyntax name, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, SyntaxToken semicolonToken)
            => this.Update(this.GlobalKeyword, usingKeyword, staticKeyword, this.UnsafeKeyword, identifier, typeParameters, equalsToken, namespaceOrType: name, constraintClauses, semicolonToken);

        public UsingDirectiveSyntax Update(SyntaxToken globalKeyword, SyntaxToken usingKeyword, SyntaxToken staticKeyword, SyntaxToken identifier, TypeParameterListSyntax typeParameters, SyntaxToken equalsToken, NameSyntax name, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, SyntaxToken semicolonToken)
            => this.Update(globalKeyword, usingKeyword, staticKeyword, this.UnsafeKeyword, identifier, typeParameters, equalsToken, namespaceOrType: name, constraintClauses, semicolonToken);

        public UsingDirectiveSyntax WithName(NameSyntax name)
            => WithNamespaceOrType(name);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
    }
}

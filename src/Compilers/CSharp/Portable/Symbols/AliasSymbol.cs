// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Symbol representing a using alias appearing in a compilation unit or within a namespace
    /// declaration. Generally speaking, these symbols do not appear in the set of symbols reachable
    /// from the unnamed namespace declaration.  In other words, when a using alias is used in a
    /// program, it acts as a transparent alias, and the symbol to which it is an alias is used in
    /// the symbol table.  For example, in the source code
    /// <pre>
    /// namespace NS
    /// {
    ///     using o = System.Object;
    ///     partial class C : o {}
    ///     partial class C : object {}
    ///     partial class C : System.Object {}
    /// }
    /// </pre>
    /// all three declarations for class C are equivalent and result in the same symbol table object
    /// for C. However, these using alias symbols do appear in the results of certain SemanticModel
    /// APIs. Specifically, for the base clause of the first of C's class declarations, the
    /// following APIs may produce a result that contains an AliasSymbol:
    /// <pre>
    ///     SemanticInfo SemanticModel.GetSemanticInfo(ExpressionSyntax expression);
    ///     SemanticInfo SemanticModel.BindExpression(CSharpSyntaxNode location, ExpressionSyntax expression);
    ///     SemanticInfo SemanticModel.BindType(CSharpSyntaxNode location, ExpressionSyntax type);
    ///     SemanticInfo SemanticModel.BindNamespaceOrType(CSharpSyntaxNode location, ExpressionSyntax type);
    /// </pre>
    /// Also, the following are affected if container==null (and, for the latter, when arity==null
    /// or arity==0):
    /// <pre>
    ///     IList&lt;string&gt; SemanticModel.LookupNames(CSharpSyntaxNode location, NamespaceOrTypeSymbol container = null, LookupOptions options = LookupOptions.Default, List&lt;string> result = null);
    ///     IList&lt;Symbol&gt; SemanticModel.LookupSymbols(CSharpSyntaxNode location, NamespaceOrTypeSymbol container = null, string name = null, int? arity = null, LookupOptions options = LookupOptions.Default, List&lt;Symbol> results = null);
    /// </pre>
    /// </summary>
    internal abstract class AliasSymbol : Symbol
    {
        private readonly ImmutableArray<Location> _locations;  // NOTE: can be empty for the "global" alias.
        private readonly string _aliasName;
        private readonly bool _isExtern;
        private readonly Symbol _containingSymbol;

        protected AliasSymbol(string aliasName, Symbol containingSymbol, ImmutableArray<Location> locations, bool isExtern)
        {
            Debug.Assert(locations.Length == 1 || (locations.IsEmpty && aliasName == "global")); // It looks like equality implementation depends on this condition.
            Debug.Assert(containingSymbol is not null);

            _locations = locations;
            _aliasName = aliasName;
            _isExtern = isExtern;
            _containingSymbol = containingSymbol;
        }

        // For the purposes of SemanticModel, it is convenient to have an AliasSymbol for the "global" namespace that "global::" binds
        // to. This alias symbol is returned only when binding "global::" (special case code).
        internal static AliasSymbol CreateGlobalNamespaceAlias(NamespaceSymbol globalNamespace)
        {
            return new AliasSymbolFromResolvedTarget(globalNamespace, "global", globalNamespace, ImmutableArray<Location>.Empty, isExtern: false);
        }

        internal static AliasSymbol CreateCustomDebugInfoAlias(NamespaceOrTypeSymbol targetSymbol, SyntaxToken aliasToken, Symbol containingSymbol, bool isExtern)
        {
            return new AliasSymbolFromResolvedTarget(targetSymbol, aliasToken.ValueText, containingSymbol, ImmutableArray.Create(aliasToken.GetLocation()), isExtern);
        }

        internal AliasSymbol ToNewSubmission(CSharpCompilation compilation)
        {
            // We can pass basesBeingResolved: null because base type cycles can't cross
            // submission boundaries - there's no way to depend on a subsequent submission.
            var previousTarget = Target;
            if (previousTarget.Kind != SymbolKind.Namespace)
            {
                return this;
            }

            var expandedGlobalNamespace = compilation.GlobalNamespace;
            var expandedNamespace = Imports.ExpandPreviousSubmissionNamespace((NamespaceSymbol)previousTarget, expandedGlobalNamespace);
            return new AliasSymbolFromResolvedTarget(expandedNamespace, Name, ContainingSymbol, _locations, _isExtern);
        }

        public sealed override string Name
        {
            get
            {
                return _aliasName;
            }
        }

        public sealed override SymbolKind Kind
        {
            get
            {
                return SymbolKind.Alias;
            }
        }

        /// <summary>
        /// Returns the arity of this alias, or the number of type parameters it takes.
        /// A non-generic alias has zero arity.
        /// </summary>
        public abstract int Arity
        {
            get;
        }

        /// <summary>
        /// Returns the type parameters that this alias has. If this is a non-generic alias,
        /// returns an empty ImmutableArray.  
        /// </summary>
        public abstract ImmutableArray<TypeParameterSymbol> TypeParameters { get; }

        /// <summary>
        /// Gets the <see cref="NamespaceOrTypeSymbol"/> for the
        /// namespace or type referenced by the alias.
        /// </summary>
        public abstract NamespaceOrTypeSymbol Target
        {
            get;
        }

        public sealed override ImmutableArray<Location> Locations
        {
            get
            {
                return _locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return _isExtern
                    ? GetDeclaringSyntaxReferenceHelper<ExternAliasDirectiveSyntax>(_locations)
                    : GetDeclaringSyntaxReferenceHelper<UsingDirectiveSyntax>(_locations);
            }
        }

        public sealed override bool IsExtern
        {
            get
            {
                return _isExtern;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return false;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return false;
            }
        }
        public override bool IsOverride
        {
            get
            {
                return false;
            }
        }

        public override bool IsVirtual
        {
            get
            {
                return false;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns data decoded from Obsolete attribute or null if there is no Obsolete attribute.
        /// This property returns ObsoleteAttributeData.Uninitialized if attribute arguments haven't been decoded yet.
        /// </summary>
        internal sealed override ObsoleteAttributeData? ObsoleteAttributeData
        {
            get { return null; }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return Accessibility.NotApplicable;
            }
        }

        /// <summary>
        /// Using aliases in C# are always contained within a namespace declaration, or at the top
        /// level within a compilation unit, within the implicit unnamed namespace declaration.  We
        /// return that as the "containing" symbol, even though the alias isn't a member of the
        /// namespace as such.
        /// </summary>
        public override Symbol ContainingSymbol
        {
            get
            {
                return _containingSymbol;
            }
        }

        public new virtual AliasSymbol OriginalDefinition
        {
            get { return this; }
        }

        protected sealed override Symbol OriginalSymbolDefinition => this.OriginalDefinition;

        /// <summary>
        /// Returns the map from type parameters to type arguments.
        /// If this is not a generic alias instantiation, returns null.
        /// The map targets the original definition of the type.
        /// </summary>
        internal virtual TypeMap? TypeSubstitution
        {
            get { return null; }
        }

        internal override TResult Accept<TArg, TResult>(CSharpSymbolVisitor<TArg, TResult> visitor, TArg a)
        {
            return visitor.VisitAlias(this, a);
        }

        public override void Accept(CSharpSymbolVisitor visitor)
        {
            visitor.VisitAlias(this);
        }

        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor)
        {
            return visitor.VisitAlias(this);
        }

        // basesBeingResolved is only used to break circular references.
        internal abstract NamespaceOrTypeSymbol GetAliasTarget(ConsList<TypeSymbol>? basesBeingResolved);

        internal void CheckConstraints(BindingDiagnosticBag diagnostics)
        {
            var target = this.Target as TypeSymbol;
            if ((object?)target != null && Locations.Length > 0)
            {
                var corLibrary = this.ContainingAssembly.CorLibrary;
                var conversions = new TypeConversions(corLibrary);
                target.CheckAllConstraints(DeclaringCompilation, conversions, Locations[0], diagnostics);
            }
        }

        public override bool Equals(Symbol? obj, TypeCompareKind compareKind)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            AliasSymbol? other = obj as AliasSymbol;

            return (object?)other != null &&
                Equals(this.Locations.FirstOrDefault(), other.Locations.FirstOrDefault()) &&
                Equals(this.ContainingSymbol, other.ContainingSymbol, compareKind);
        }

        public override int GetHashCode()
            => this.Locations.FirstOrDefault()?.GetHashCode() ?? Name.GetHashCode();

        internal abstract override bool RequiresCompletion
        {
            get;
        }

        #region Construct

        /// <summary>
        /// Returns a constructed alias given a list of type arguments.
        /// </summary>
        /// <param name="typeArguments">The immediate type arguments to be replaced for type
        /// parameters in the alias target.</param>
        public AliasTargetTypeSymbol Construct(params TypeSymbol[] typeArguments)
        {
            // https://github.com/dotnet/roslyn/issues/30064: We should fix the callers to pass TypeWithAnnotations[] instead of TypeSymbol[].
            return ConstructWithoutModifiers(typeArguments.AsImmutableOrNull(), false);
        }

        /// <summary>
        /// Returns a constructed alias given a list of type arguments.
        /// </summary>
        /// <param name="typeArguments">The immediate type arguments to be replaced for type
        /// parameters in the alias target.</param>
        public AliasTargetTypeSymbol Construct(ImmutableArray<TypeSymbol> typeArguments)
        {
            // https://github.com/dotnet/roslyn/issues/30064: We should fix the callers to pass ImmutableArray<TypeWithAnnotations> instead of ImmutableArray<TypeSymbol>.
            return ConstructWithoutModifiers(typeArguments, false);
        }

        /// <summary>
        /// Returns a constructed alias given a list of type arguments.
        /// </summary>
        /// <param name="typeArguments"></param>
        public AliasTargetTypeSymbol Construct(IEnumerable<TypeSymbol> typeArguments)
        {
            // https://github.com/dotnet/roslyn/issues/30064: We should fix the callers to pass IEnumerable<TypeWithAnnotations> instead of IEnumerable<TypeSymbol>.
            return ConstructWithoutModifiers(typeArguments.AsImmutableOrNull(), false);
        }

        private AliasTargetTypeSymbol ConstructWithoutModifiers(ImmutableArray<TypeSymbol> typeArguments, bool unbound)
        {
            ImmutableArray<TypeWithAnnotations> modifiedArguments;

            if (typeArguments.IsDefault)
            {
                modifiedArguments = default(ImmutableArray<TypeWithAnnotations>);
            }
            else
            {
                modifiedArguments = typeArguments.SelectAsArray(t => TypeWithAnnotations.Create(t));
            }

            return Construct(modifiedArguments, unbound);
        }

        internal AliasTargetTypeSymbol Construct(ImmutableArray<TypeWithAnnotations> typeArguments)
        {
            return Construct(typeArguments, unbound: false);
        }

        internal AliasTargetTypeSymbol Construct(ImmutableArray<TypeWithAnnotations> typeArguments, bool unbound)
        {
            if (this.Arity == 0)
            {
                throw new InvalidOperationException(CSharpResources.CannotCreateConstructedFromNongeneric);
            }

            if (typeArguments.IsDefault)
            {
                throw new ArgumentNullException(nameof(typeArguments));
            }

            if (typeArguments.Any(NamedTypeSymbol.TypeWithAnnotationsIsNullFunction))
            {
                throw new ArgumentException(CSharpResources.TypeArgumentCannotBeNull, nameof(typeArguments));
            }

            if (typeArguments.Length != this.Arity)
            {
                throw new ArgumentException(CSharpResources.WrongNumberOfTypeArguments, nameof(typeArguments));
            }

            Debug.Assert(!unbound || typeArguments.All(NamedTypeSymbol.TypeWithAnnotationsIsErrorType));

            return this.ConstructCore(typeArguments, unbound);
        }

        protected abstract AliasTargetTypeSymbol ConstructCore(ImmutableArray<TypeWithAnnotations> typeArguments, bool unbound);

        #endregion

        protected override ISymbol CreateISymbol()
        {
            return new PublicModel.AliasSymbol(this);
        }
    }

    internal sealed class AliasSymbolFromResolvedTarget : AliasSymbol
    {
        private readonly NamespaceOrTypeSymbol _aliasTarget;

        internal AliasSymbolFromResolvedTarget(NamespaceOrTypeSymbol target, string aliasName, Symbol containingSymbol, ImmutableArray<Location> locations, bool isExtern)
            : base(aliasName, containingSymbol, locations, isExtern)
        {
            _aliasTarget = target;
        }

        public override int Arity
        {
            get
            {
                return 0;
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                return ImmutableArray<TypeParameterSymbol>.Empty;
            }
        }

        /// <summary>
        /// Gets the <see cref="NamespaceOrTypeSymbol"/> for the
        /// namespace or type referenced by the alias.
        /// </summary>
        public override NamespaceOrTypeSymbol Target
        {
            get
            {
                return _aliasTarget;
            }
        }

        internal override NamespaceOrTypeSymbol GetAliasTarget(ConsList<TypeSymbol>? basesBeingResolved)
        {
            return _aliasTarget;
        }

        protected override AliasTargetTypeSymbol ConstructCore(ImmutableArray<TypeWithAnnotations> typeArguments, bool unbound)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override bool RequiresCompletion
        {
            get { return false; }
        }
    }
}

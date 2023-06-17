// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class AliasSymbolFromSyntax : AliasSymbol
    {
        private readonly SyntaxReference _directive;
        private SymbolCompletionState _state;
        private readonly int _aliasArity;
        private ImmutableArray<TypeParameterSymbol> _lazyTypeParameters;
        private NamespaceOrTypeSymbol? _aliasTarget;

        /// <summary>
        /// A collection of type parameter constraint types, populated when
        /// constraint types for the first type parameter are requested.
        /// </summary>
        private ImmutableArray<ImmutableArray<TypeWithAnnotations>> _lazyTypeParameterConstraintTypes;

        /// <summary>
        /// A collection of type parameter constraint kinds, populated when
        /// constraint kinds for the first type parameter are requested.
        /// </summary>
        private ImmutableArray<TypeParameterConstraintKind> _lazyTypeParameterConstraintKinds;

        // lazy binding
        private BindingDiagnosticBag? _aliasTargetDiagnostics;

        internal AliasSymbolFromSyntax(SourceNamespaceSymbol containingSymbol, UsingDirectiveSyntax syntax)
            : base(syntax.Identifier.ValueText, containingSymbol, ImmutableArray.Create(syntax.Identifier.GetLocation()), isExtern: false)
        {
            Debug.Assert(syntax.Identifier != default);

            _aliasArity = syntax.TypeParameterList != null ? syntax.TypeParameterList.Parameters.Count : 0;
            _directive = syntax.GetReference();
        }

        internal AliasSymbolFromSyntax(SourceNamespaceSymbol containingSymbol, ExternAliasDirectiveSyntax syntax)
            : base(syntax.Identifier.ValueText, containingSymbol, ImmutableArray.Create(syntax.Identifier.GetLocation()), isExtern: true)
        {
            _aliasArity = 0;
            _directive = syntax.GetReference();
        }

        public override int Arity
        {
            get
            {
                return _aliasArity;
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                if (_lazyTypeParameters.IsDefault)
                {
                    var diagnostics = BindingDiagnosticBag.GetInstance();
                    if (ImmutableInterlocked.InterlockedInitialize(ref _lazyTypeParameters, MakeTypeParameters(diagnostics)))
                    {
                        AddDeclarationDiagnostics(diagnostics);
                    }

                    diagnostics.Free();
                }

                return _lazyTypeParameters;
            }
        }

        private ImmutableArray<TypeParameterSymbol> MakeTypeParameters(BindingDiagnosticBag diagnostics)
        {
            if (_aliasArity == 0)
            {
                return ImmutableArray<TypeParameterSymbol>.Empty;
            }

            var typeParameterNames = new string[_aliasArity];

            var decl = (UsingDirectiveSyntax)_directive.GetSyntax();
            var syntaxTree = decl.SyntaxTree;
            var tpl = decl.TypeParameterList!;

            MessageID.IDS_FeatureGenerics.CheckFeatureAvailability(diagnostics, tpl.LessThanToken);

            var parameterBuilders = new List<AliasTypeParameterBuilder>();
            int i = 0;
            foreach (var tp in tpl.Parameters)
            {
                if (tp.VarianceKeyword.Kind() != SyntaxKind.None)
                {
                    // cannot use in / out in alias.
                    diagnostics.Add(ErrorCode.ERR_IllegalVarianceSyntax, tp.VarianceKeyword.GetLocation());
                }

                var name = typeParameterNames[i] = tp.Identifier.ValueText;
                var location = new SourceLocation(tp.Identifier);

                SourceMemberContainerTypeSymbol.ReportReservedTypeName(tp.Identifier.Text, this.DeclaringCompilation, diagnostics.DiagnosticBag, location);

                for (int j = 0; j < i; j++)
                {
                    if (name == typeParameterNames[j])
                    {
                        diagnostics.Add(ErrorCode.ERR_DuplicateTypeParameter, location, name);
                        break;
                    }
                }

                parameterBuilders.Add(new AliasTypeParameterBuilder(syntaxTree.GetReference(tp), this, location));
                i++;
            }

            var parameters = parameterBuilders.Select((builder, i) => builder.MakeSymbol(i, diagnostics));
            return parameters.AsImmutable();
        }

        /// <summary>
        /// Returns the constraint types for the given type parameter.
        /// </summary>
        internal ImmutableArray<TypeWithAnnotations> GetTypeParameterConstraintTypes(int ordinal)
        {
            var constraintTypes = GetTypeParameterConstraintTypes();
            return (constraintTypes.Length > 0) ? constraintTypes[ordinal] : ImmutableArray<TypeWithAnnotations>.Empty;
        }

        private ImmutableArray<ImmutableArray<TypeWithAnnotations>> GetTypeParameterConstraintTypes()
        {
            var constraintTypes = _lazyTypeParameterConstraintTypes;
            if (constraintTypes.IsDefault)
            {
                GetTypeParameterConstraintKinds();

                var diagnostics = BindingDiagnosticBag.GetInstance();
                if (ImmutableInterlocked.InterlockedInitialize(ref _lazyTypeParameterConstraintTypes, MakeTypeParameterConstraintTypes(diagnostics)))
                {
                    this.AddDeclarationDiagnostics(diagnostics);
                }
                diagnostics.Free();
                constraintTypes = _lazyTypeParameterConstraintTypes;
            }

            return constraintTypes;
        }

        private ImmutableArray<ImmutableArray<TypeWithAnnotations>> MakeTypeParameterConstraintTypes(BindingDiagnosticBag diagnostics)
        {
            var results = MakeTypeParameterConstraintClauses(diagnostics);

            return results.SelectAsArray(clause => clause.ConstraintTypes);
        }

        /// <summary>
        /// Returns the constraint kind for the given type parameter.
        /// </summary>
        internal TypeParameterConstraintKind GetTypeParameterConstraintKind(int ordinal)
        {
            var constraintKinds = GetTypeParameterConstraintKinds();
            return (constraintKinds.Length > 0) ? constraintKinds[ordinal] : TypeParameterConstraintKind.None;
        }

        private ImmutableArray<TypeParameterConstraintKind> GetTypeParameterConstraintKinds()
        {
            var constraintKinds = _lazyTypeParameterConstraintKinds;
            if (constraintKinds.IsDefault)
            {
                ImmutableInterlocked.InterlockedInitialize(ref _lazyTypeParameterConstraintKinds, MakeTypeParameterConstraintKinds());
                constraintKinds = _lazyTypeParameterConstraintKinds;
            }

            return constraintKinds;
        }

        private ImmutableArray<TypeParameterConstraintKind> MakeTypeParameterConstraintKinds()
        {
            var results = MakeTypeParameterConstraintClauses();

            return results.SelectAsArray(clause => clause.Constraints);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private ImmutableArray<TypeParameterConstraintClause> MakeTypeParameterConstraintClauses(BindingDiagnosticBag? diagnostics = null)
        {
            diagnostics ??= BindingDiagnosticBag.GetInstance();
            var typeParameters = this.TypeParameters;
            var results = ImmutableArray<TypeParameterConstraintClause>.Empty;

            if (_aliasArity > 0)
            {
                var typeParameterList = ((UsingDirectiveSyntax)_directive.GetSyntax()).TypeParameterList!;
                var binderFactory = this.DeclaringCompilation.GetBinderFactory(_directive.SyntaxTree);
                Binder binder;
                ImmutableArray<TypeParameterConstraintClause> constraints;

                binder = binderFactory.GetBinder(typeParameterList.Parameters[0]);
                constraints = binder.GetDefaultTypeParameterConstraintClauses(typeParameterList);

                Debug.Assert(constraints.Length == _aliasArity);

                constraints = ConstraintsHelper.AdjustConstraintKindsBasedOnConstraintTypes(typeParameters, constraints);

                if (constraints.Any(clause => clause.Constraints != TypeParameterConstraintKind.None))
                {
                    results = constraints;
                }
            }

            return results;
        }

        internal sealed override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics
        {
            get
            {
                return GetTypeParametersAsTypeArguments();
            }
        }

        internal ImmutableArray<TypeWithAnnotations> GetTypeParametersAsTypeArguments()
        {
            return TypeMap.TypeParametersAsTypeSymbolsWithAnnotations(this.TypeParameters);
        }

        /// <summary>
        /// Gets the <see cref="NamespaceOrTypeSymbol"/> for the
        /// namespace or type referenced by the alias.
        /// </summary>
        public override NamespaceOrTypeSymbol Target
        {
            get
            {
                return GetAliasTarget(basesBeingResolved: null);
            }
        }

        // basesBeingResolved is only used to break circular references.
        internal override NamespaceOrTypeSymbol GetAliasTarget(ConsList<TypeSymbol>? basesBeingResolved)
        {
            if (!_state.HasComplete(CompletionPart.AliasTarget))
            {
                // the target is not yet bound. If it is an ordinary alias, bind the target
                // symbol. If it is an extern alias then find the target in the list of metadata references.
                var newDiagnostics = BindingDiagnosticBag.GetInstance();

                NamespaceOrTypeSymbol symbol = this.IsExtern
                    ? ResolveExternAliasTarget(newDiagnostics)
                    : ResolveAliasTarget((UsingDirectiveSyntax)_directive.GetSyntax(), newDiagnostics, basesBeingResolved);

                if (Interlocked.CompareExchange(ref _aliasTarget, symbol, null) is null)
                {
                    // Note: It's important that we don't call newDiagnosticsToReadOnlyAndFree here. That call
                    // can force the prompt evaluation of lazy initialized diagnostics.  That in turn can 
                    // call back into GetAliasTarget on the same thread resulting in a dead lock scenario.
                    bool won = Interlocked.Exchange(ref _aliasTargetDiagnostics, newDiagnostics) == null;
                    Debug.Assert(won, "Only one thread can win the alias target CompareExchange");

                    _state.NotePartComplete(CompletionPart.AliasTarget);
                    // we do not clear this.aliasTargetName, as another thread might be about to use it for ResolveAliasTarget(...)
                }
                else
                {
                    newDiagnostics.Free();
                    // Wait for diagnostics to have been reported if another thread resolves the alias
                    _state.SpinWaitComplete(CompletionPart.AliasTarget, default(CancellationToken));
                }
            }

            return _aliasTarget!;
        }

        internal BindingDiagnosticBag AliasTargetDiagnostics
        {
            get
            {
                GetAliasTarget(null);
                RoslynDebug.Assert(_aliasTargetDiagnostics != null);
                return _aliasTargetDiagnostics;
            }
        }

        private NamespaceSymbol ResolveExternAliasTarget(BindingDiagnosticBag diagnostics)
        {
            NamespaceSymbol? target;
            if (!ContainingSymbol.DeclaringCompilation.GetExternAliasTarget(Name, out target))
            {
                diagnostics.Add(ErrorCode.ERR_BadExternAlias, GetFirstLocation(), Name);
            }

            RoslynDebug.Assert(target is object);
            RoslynDebug.Assert(target.IsGlobalNamespace);

            return target;
        }

        private NamespaceOrTypeSymbol ResolveAliasTarget(
            UsingDirectiveSyntax usingDirective,
            BindingDiagnosticBag diagnostics,
            ConsList<TypeSymbol>? basesBeingResolved)
        {
            if (usingDirective.UnsafeKeyword != default)
            {
                MessageID.IDS_FeatureUsingTypeAlias.CheckFeatureAvailability(diagnostics, usingDirective.UnsafeKeyword);
            }
            else if (usingDirective.NamespaceOrType is not NameSyntax)
            {
                MessageID.IDS_FeatureUsingTypeAlias.CheckFeatureAvailability(diagnostics, usingDirective.NamespaceOrType);
            }

            if (usingDirective.TypeParameterList is not null)
            {
                MessageID.IDS_FeatureUsingGenericAlias.CheckFeatureAvailability(diagnostics, usingDirective.TypeParameterList);
            }

            var syntax = usingDirective.NamespaceOrType;
            var flags = BinderFlags.SuppressConstraintChecks | BinderFlags.SuppressObsoleteChecks;
            if (usingDirective.UnsafeKeyword != default)
            {
                this.CheckUnsafeModifier(DeclarationModifiers.Unsafe, usingDirective.UnsafeKeyword.GetLocation(), diagnostics);
                flags |= BinderFlags.UnsafeRegion;
            }
            else
            {
                // Prior to C#12, allow the alias to be an unsafe region.  This allows us to maintain compat with prior
                // versions of the compiler that allowed `using X = List<int*[]>` to be written.  In 12.0 and onwards
                // though, we require the code to explicitly contain the `unsafe` keyword.
                if (!DeclaringCompilation.IsFeatureEnabled(MessageID.IDS_FeatureUsingTypeAlias))
                    flags |= BinderFlags.UnsafeRegion;
            }

            var declarationBinder = new WithAliasTypeParametersBinder(this,
                ContainingSymbol.DeclaringCompilation
                    .GetBinderFactory(syntax.SyntaxTree)
                    .GetBinder(syntax))
                .WithAdditionalFlags(flags);

            var annotatedNamespaceOrType = declarationBinder.BindNamespaceOrTypeSymbol(syntax, diagnostics, basesBeingResolved);

            // `using X = RefType?;` is not legal.
            if (usingDirective.NamespaceOrType is NullableTypeSyntax nullableType &&
                annotatedNamespaceOrType.TypeWithAnnotations.NullableAnnotation == NullableAnnotation.Annotated &&
                annotatedNamespaceOrType.TypeWithAnnotations.Type?.IsReferenceType is true)
            {
                diagnostics.Add(ErrorCode.ERR_BadNullableReferenceTypeInUsingAlias, nullableType.QuestionToken.GetLocation());
            }

            var namespaceOrType = annotatedNamespaceOrType.NamespaceOrTypeSymbol;
            if (namespaceOrType is TypeSymbol { IsNativeIntegerWrapperType: true } &&
                (usingDirective.NamespaceOrType.IsNint || usingDirective.NamespaceOrType.IsNuint))
            {
                // using X = nint;
                MessageID.IDS_FeatureUsingTypeAlias.CheckFeatureAvailability(diagnostics, usingDirective.NamespaceOrType);
            }

            return namespaceOrType;
        }

        internal override bool RequiresCompletion
        {
            get { return true; }
        }

        public override AliasSymbol ConstructedFrom => this;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class SubstitutedAliasSymbol : WrappedAliasSymbol
    {
        private readonly TypeMap _inputMap;

        // The container of a substituted alias symbol is typically a named type or a namespace.
        // However, in some error-recovery scenarios it might be some other container. For example,
        // consider "int Goo = 123; Goo<string> x = null;" What is the type of x? We construct an alias symbol of arity one target to an error
        // type symbol associated with local variable symbol Goo; when we construct
        // that alias symbol with <string>, the resulting substituted alias symbol has
        // the same containing symbol as the local: it is contained in the method.
        private readonly Symbol _newContainer;

        private NamespaceOrTypeSymbol _lazyTarget;

        protected SubstitutedAliasSymbol(Symbol newContainer, TypeMap map, AliasSymbol originalDefinition) : base(originalDefinition)
        {
            Debug.Assert(originalDefinition.IsDefinition);
            _newContainer = newContainer;
            _inputMap = map;
        }

        public sealed override Symbol ContainingSymbol
        {
            get { return _newContainer; }
        }

        internal override TypeMap TypeSubstitution => _inputMap;

        public sealed override NamespaceOrTypeSymbol Target
        {
            get
            {
                return GetAliasTarget(basesBeingResolved: null);
            }
        }

        internal override NamespaceOrTypeSymbol GetAliasTarget(ConsList<TypeSymbol> basesBeingResolved)
        {
            var target = _lazyTarget;
            if ((object)target == null)
            {
                var underlyingTarget = base.GetAliasTarget(basesBeingResolved);
                if (underlyingTarget.IsNamespace)
                {
                    Interlocked.Exchange(ref _lazyTarget, underlyingTarget);
                }
                else
                {
                    Interlocked.Exchange(ref _lazyTarget, _inputMap.SubstituteType(underlyingTarget as TypeSymbol).Type);
                }
                target = _lazyTarget;
            }

            return target;
        }
    }
}

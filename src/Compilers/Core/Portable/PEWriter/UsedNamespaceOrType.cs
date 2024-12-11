// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    /// <summary>
    /// Represents a single using directive (Imports clause).
    /// </summary>
    internal readonly struct UsedNamespaceOrType : IEquatable<UsedNamespaceOrType>
    {
        public readonly NameWithArity? AliasOpt;
        public readonly IAssemblyReference? TargetAssemblyOpt;
        public readonly INamespace? TargetNamespaceOpt;
        public readonly ITypeReference? TargetTypeOpt;
        public readonly string? TargetXmlNamespaceOpt;

        private UsedNamespaceOrType(
            NameWithArity? alias = null,
            IAssemblyReference? targetAssembly = null,
            INamespace? targetNamespace = null,
            ITypeReference? targetType = null,
            string? targetXmlNamespace = null)
        {
            AliasOpt = alias;
            TargetAssemblyOpt = targetAssembly;
            TargetNamespaceOpt = targetNamespace;
            TargetTypeOpt = targetType;
            TargetXmlNamespaceOpt = targetXmlNamespace;
        }

        internal static UsedNamespaceOrType CreateType(ITypeReference type, NameWithArity? aliasOpt = null)
        {
            RoslynDebug.Assert(type != null);
            return new UsedNamespaceOrType(alias: aliasOpt, targetType: type);
        }

        internal static UsedNamespaceOrType CreateNamespace(INamespace @namespace, IAssemblyReference? assemblyOpt = null, NameWithArity? aliasOpt = null)
        {
            RoslynDebug.Assert(@namespace != null);
            return new UsedNamespaceOrType(alias: aliasOpt, targetAssembly: assemblyOpt, targetNamespace: @namespace);
        }

        internal static UsedNamespaceOrType CreateExternAlias(string alias)
        {
            RoslynDebug.Assert(alias != null);
            return new UsedNamespaceOrType(alias: alias);
        }

        internal static UsedNamespaceOrType CreateXmlNamespace(string prefix, string xmlNamespace)
        {
            RoslynDebug.Assert(xmlNamespace != null);
            RoslynDebug.Assert(prefix != null);
            return new UsedNamespaceOrType(alias: prefix, targetXmlNamespace: xmlNamespace);
        }

        public override bool Equals(object? obj)
        {
            return obj is UsedNamespaceOrType other && Equals(other);
        }

        public bool Equals(UsedNamespaceOrType other)
        {
            return
                (AliasOpt.HasValue
                    ? other.AliasOpt.HasValue && NameWithArityComparer.Default.Equals(AliasOpt.Value, other.AliasOpt.Value)
                    : !other.AliasOpt.HasValue)
                && object.Equals(TargetAssemblyOpt, other.TargetAssemblyOpt)
                && Equals(TargetNamespaceOpt, other.TargetNamespaceOpt)
                && Equals(TargetTypeOpt, other.TargetTypeOpt)
                && TargetXmlNamespaceOpt == other.TargetXmlNamespaceOpt;
        }

        public override int GetHashCode()
        {
            int hashCode = Hash.Combine((object?)TargetAssemblyOpt,
                           Hash.Combine(GetHashCode(TargetNamespaceOpt),
                           Hash.Combine(GetHashCode(TargetTypeOpt),
                           Hash.Combine(TargetXmlNamespaceOpt, 0))));
            return AliasOpt.HasValue ? Hash.Combine(NameWithArityComparer.Default.GetHashCode(AliasOpt.Value), hashCode) : hashCode;
        }

        private static bool Equals(ITypeReference? x, ITypeReference? y)
        {
            if (x == y)
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            var xSymbol = x.GetInternalSymbol();
            var ySymbol = y.GetInternalSymbol();

            if (xSymbol is object && ySymbol is object)
            {
                return xSymbol.Equals(ySymbol);
            }
            else if (xSymbol is object || ySymbol is object)
            {
                return false;
            }

            return x.Equals(y);
        }

        private static int GetHashCode(ITypeReference? obj)
        {
            var objSymbol = obj?.GetInternalSymbol();

            if (objSymbol is object)
            {
                return objSymbol.GetHashCode();
            }

            return obj?.GetHashCode() ?? 0;
        }

        private static bool Equals(INamespace? x, INamespace? y)
        {
            if (x == y)
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            var xSymbol = x.GetInternalSymbol();
            var ySymbol = y.GetInternalSymbol();

            if (xSymbol is object && ySymbol is object)
            {
                return xSymbol.Equals(ySymbol);
            }
            else if (xSymbol is object || ySymbol is object)
            {
                return false;
            }

            return x.Equals(y);
        }

        private static int GetHashCode(INamespace? obj)
        {
            var objSymbol = obj?.GetInternalSymbol();

            if (objSymbol is object)
            {
                return objSymbol.GetHashCode();
            }

            return obj?.GetHashCode() ?? 0;
        }
    }
}

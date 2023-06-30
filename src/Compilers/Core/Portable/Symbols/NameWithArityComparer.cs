// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public class NameWithArityComparer : IComparer<NameWithArity>, IEqualityComparer<NameWithArity>
    {
        private readonly StringComparer _nameComparer;

        public static NameWithArityComparer Default { get; } = new(StringComparer.Ordinal);
        public static NameWithArityComparer IgnoreCase { get; } = new(StringComparer.OrdinalIgnoreCase);

        public NameWithArityComparer(StringComparer nameComparer)
        {
            if (nameComparer is null)
            {
                throw new ArgumentNullException(nameof(nameComparer));
            }

            _nameComparer = nameComparer;
        }

        public int Compare(NameWithArity x, NameWithArity y)
        {
            int result = _nameComparer.Compare(x.Name, y.Name);
            if (result != 0)
            {
                return result;
            }

            return x.Arity.CompareTo(y.Arity);
        }

        public bool Equals(NameWithArity x, NameWithArity y)
        {
            if (!_nameComparer.Equals(x.Name, y.Name))
            {
                return false;
            }

            return x.Arity == y.Arity;
        }

        public int GetHashCode(NameWithArity obj)
        {
            return Hash.Combine(_nameComparer.GetHashCode(obj.Name), obj.Arity);
        }
    }
}

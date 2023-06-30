// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public readonly struct NameWithArity : IComparable<NameWithArity>
    {
        public readonly string Name;
        public readonly int Arity;

        public bool IsDefault => this.Name is null;
        public bool HasArity => this.Arity > 0;

        public NameWithArity(string name, int arity)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (arity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arity), arity, "arity must not less than zero");
            }

            this.Name = name;
            this.Arity = arity;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Name.GetHashCode(), this.Arity.GetHashCode());
        }

        public override string ToString()
        {
            return this.GetDebuggerDisplay();
        }

        private string GetDebuggerDisplay()
        {
            if (this.Arity == 0)
            {
                return this.Name;
            }

            return $"{this.Name}`{this.Arity}";
        }

        int IComparable<NameWithArity>.CompareTo(NameWithArity other)
        {
            int result = this.Name.CompareTo(other.Name);
            if (result != 0)
            {
                return result;
            }

            return this.Arity.CompareTo(other.Arity);
        }

        public static implicit operator NameWithArity(string? name)
        {
            if (name is null)
            {
                return default;
            }

            return new NameWithArity(name, 0);
        }
    }
}

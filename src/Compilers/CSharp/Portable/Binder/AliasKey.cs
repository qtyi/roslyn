// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal readonly struct AliasKey : IComparable<AliasKey>
    {
        public readonly string Name;
        public readonly int Arity;

        public AliasKey(string name, int arity)
        {
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

        int IComparable<AliasKey>.CompareTo(AliasKey other)
        {
            int result = this.Name.CompareTo(other.Name);
            if (result != 0)
            {
                return result;
            }

            return this.Arity.CompareTo(other.Arity);
        }

        public static implicit operator AliasKey(string name)
        {
            return new AliasKey(name, 0);
        }
    }
}

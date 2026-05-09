// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Describes a command line assembly identity specification.
    /// </summary>
    [DebuggerDisplay("{Name,nq}")]
    public readonly struct CommandLineAssemblyIdentity : IEquatable<CommandLineAssemblyIdentity>
    {
        public CommandLineAssemblyIdentity(string name, ImmutableArray<byte> publicKey)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));
            Name = name;
            PublicKey = publicKey;
        }

        /// <summary>
        /// Display name of an assembly.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Public key of an assembly.
        /// </summary>
        public ImmutableArray<byte> PublicKey { get; }

        public override bool Equals(object? obj)
        {
            return obj is CommandLineAssemblyIdentity && base.Equals((CommandLineAssemblyIdentity)obj);
        }

        public bool Equals(CommandLineAssemblyIdentity other)
        {
            return Name == other.Name
                && PublicKey.Equals(other.PublicKey);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Name, PublicKey.GetHashCode());
        }
    }
}

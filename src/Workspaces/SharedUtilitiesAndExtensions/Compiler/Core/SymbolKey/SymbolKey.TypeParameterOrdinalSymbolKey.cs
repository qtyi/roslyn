// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal partial struct SymbolKey
{
    private static class TypeParameterOrdinalSymbolKey
    {
        public static void Create(ITypeParameterSymbol symbol, int index, SymbolKeyWriter visitor)
        {
            Contract.ThrowIfFalse(symbol.TypeParameterKind is TypeParameterKind.Method or TypeParameterKind.Alias);
            visitor.WriteInteger((int)symbol.TypeParameterKind);
            visitor.WriteInteger(index);
            visitor.WriteInteger(symbol.Ordinal);
        }

        public static SymbolKeyResolution Resolve(SymbolKeyReader reader, out string? failureReason)
        {
            var kind = (TypeParameterKind)reader.ReadInteger();
            var index = reader.ReadInteger();
            var ordinal = reader.ReadInteger();
            ITypeParameterSymbol? typeParameter;
            if (kind == TypeParameterKind.Method)
            {
                var method = reader.ResolveMethod(index);
                typeParameter = method?.TypeParameters[ordinal];
            }
            else
            {
                var alias = reader.ResolveAlias(index);
                typeParameter = alias?.TypeParameters[ordinal];
            }

            if (typeParameter == null)
            {
                failureReason = $"({nameof(TypeParameterOrdinalSymbolKey)} failed)";
                return default;
            }

            failureReason = null;
            return new SymbolKeyResolution(typeParameter);
        }
    }
}

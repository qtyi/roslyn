// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal sealed partial class SyntaxTreeIndex : IObjectWritable
    {
        public static Task<SyntaxTreeIndex?> LoadAsync(
            IChecksummedPersistentStorageService storageService, DocumentKey documentKey, Checksum? checksum, StringTable stringTable, CancellationToken cancellationToken)
        {
            return LoadAsync(storageService, documentKey, checksum, stringTable, ReadIndex, cancellationToken);
        }

        public override void WriteTo(ObjectWriter writer)
        {
            _literalInfo.WriteTo(writer);
            _identifierInfo.WriteTo(writer);
            _contextInfo.WriteTo(writer);

            if (_globalAliasInfo == null)
            {
                writer.WriteInt32(0);
            }
            else
            {
                writer.WriteInt32(_globalAliasInfo.Count);
                foreach (var info in _globalAliasInfo)
                {
                    writer.WriteString(info.AliasName);
                    writer.WriteInt32(info.AliasArity);
                    writer.WriteByte((byte)info.TargetKind);
                    switch (info.TargetKind)
                    {
                        case AliasTargetKind.Name:
                            writer.WriteString(info.TargetName);
                            writer.WriteInt32(info.TargetArity);
                            break;

                        case AliasTargetKind.TypeParameter:
                            writer.WriteString(info.TargetName);
                            break;

                        case AliasTargetKind.Dynamic:
                            break;

                        case AliasTargetKind.Array:
                            writer.WriteInt32(info.TargetArity);
                            break;

                        case AliasTargetKind.Pointer:
                            break;

                        case AliasTargetKind.FunctionPointer:
                            writer.WriteInt32(info.TargetArity);
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(info.TargetKind);
                    }
                }
            }
        }

        private static SyntaxTreeIndex? ReadIndex(
            StringTable stringTable, ObjectReader reader, Checksum? checksum)
        {
            var literalInfo = LiteralInfo.TryReadFrom(reader);
            var identifierInfo = IdentifierInfo.TryReadFrom(reader);
            var contextInfo = ContextInfo.TryReadFrom(reader);

            if (literalInfo == null || identifierInfo == null || contextInfo == null)
                return null;

            var globalAliasInfoCount = reader.ReadInt32();
            HashSet<AliasInfo>? globalAliasInfo = null;

            if (globalAliasInfoCount > 0)
            {
                globalAliasInfo = new HashSet<AliasInfo>();

                for (var i = 0; i < globalAliasInfoCount; i++)
                {
                    var aliasName = reader.ReadString();
                    var aliasArity = reader.ReadInt32();
                    var targetKind = (AliasTargetKind)reader.ReadByte();

                    AliasInfo info;
                    switch (targetKind)
                    {
                        case AliasTargetKind.Name:
                            info = AliasInfo.CreateName(aliasName, aliasArity,
                                reader.ReadString(),
                                reader.ReadInt32());
                            break;

                        case AliasTargetKind.TypeParameter:
                            info = AliasInfo.CreateTypeParameter(aliasName, aliasArity,
                                reader.ReadString());
                            break;

                        case AliasTargetKind.Dynamic:
                            info = AliasInfo.CreateDynamic(aliasName, aliasArity);
                            break;

                        case AliasTargetKind.Array:
                            info = AliasInfo.CreateArray(aliasName, aliasArity,
                                reader.ReadInt32());
                            break;

                        case AliasTargetKind.Pointer:
                            info = AliasInfo.CreatePointer(aliasName, aliasArity);
                            break;

                        case AliasTargetKind.FunctionPointer:
                            info = AliasInfo.CreateFunctionPointer(aliasName, aliasArity,
                                reader.ReadInt32());
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(targetKind);
                    }

                    globalAliasInfo.Add(info);
                }
            }

            return new SyntaxTreeIndex(
                checksum,
                literalInfo.Value,
                identifierInfo.Value,
                contextInfo.Value,
                globalAliasInfo);
        }
    }
}

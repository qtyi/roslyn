// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;
using AliasConstructAnnotation = Microsoft.CodeAnalysis.CSharp.Symbols.TypeSymbol.AliasConstructAnnotation;
using AliasConstructCheckResult = object;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static partial class ConstraintsHelper
    {
        /// <summary>
        /// Check, if type is target of generic alias, the alias constraints.
        /// </summary>
        private static AliasConstructCheckResult CheckAliasConstructAnnotations(TypeSymbol type, in CheckConstraintsArgs args)
        {
            var result = AliasConstructAnnotation.Satisfied;
            // Check alias construct annotations if any.
            var annotation = type.GetAliasConstructAnnotation();
            if (annotation is not null)
            {
                result = annotation.CheckResult;
                if (result == AliasConstructAnnotation.Unchecked)
                {
                    // Set check result to unchecked to prevent loop.
                    if (Interlocked.CompareExchange(ref annotation.CheckResult, AliasConstructAnnotation.NeedsMoreChecks, AliasConstructAnnotation.Unchecked) == AliasConstructAnnotation.Unchecked)
                    {
                        // Check alias constraints with a new BindingDiagnosticBag.
                        var bag = BindingDiagnosticBag.GetInstance();
                        result = annotation.AliasSymbol.CheckConstraints(annotation.TypeArguments, new CheckConstraintsArgs(args.CurrentCompilation, args.Conversions, args.Location, bag), annotation.TypeArgumentsSyntax)
                            ? AliasConstructAnnotation.Satisfied
                            : AliasConstructAnnotation.NotSatisfied;

                        // Set check result.
                        if (Interlocked.CompareExchange(ref annotation.CheckResult, result, AliasConstructAnnotation.NeedsMoreChecks) == AliasConstructAnnotation.NeedsMoreChecks)
                        {
                            // We won.
                            // Report diagnostics.
                            args.Diagnostics.AddRange(bag);
                        }
                        else
                        {
                            // Another thread won.
                            // Do not report diagnostics since they are already reported by another thread.
                        }

                        bag.Free();
                    }
                    else
                    {
                        // Another thread won.
                    }
                }

                result = annotation.CheckResult;
                Debug.Assert(result != AliasConstructAnnotation.Unchecked);
            }

            return result;
        }

    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    partial class GenericConstraintsTests
    {
        [Fact]
        public void AliasConstraint_None()
        {
            CreateCompilation(@"
using X<T> = T;

public struct S {}
public class C {}

#nullable enable

public class Test
{
    X<int> x1;      // primitive value type
    X<string> x2;   // primitive reference type
    X<S> x3;        // value type
    X<C> x4;        // reference type
    X<S?> x5;       // nullable value type
    X<C?> x6;       // nullable reference type
}").VerifyDiagnostics(
                // (11,12): warning CS0169: The field 'Test.x1' is assigned but its value is never used
                //     X<int> x1;      // primitive value type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x1").WithArguments("Test.x1").WithLocation(11, 12),
                // (12,15): warning CS8618: Non-nullable field 'x2' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     X<string> x2;   // primitive reference type
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "x2").WithArguments("field", "x2").WithLocation(12, 15),
                // (12,15): warning CS0169: The field 'Test.x2' is assigned but its value is never used
                //     X<string> x2;   // primitive reference type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x2").WithArguments("Test.x2").WithLocation(12, 15),
                // (13,10): warning CS0169: The field 'Test.x3' is assigned but its value is never used
                //     X<S> x3;        // value type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x3").WithArguments("Test.x3").WithLocation(13, 10),
                // (14,10): warning CS8618: Non-nullable field 'x4' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     X<C> x4;        // reference type
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "x4").WithArguments("field", "x4").WithLocation(14, 10),
                // (14,10): warning CS0169: The field 'Test.x4' is assigned but its value is never used
                //     X<C> x4;        // reference type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x4").WithArguments("Test.x4").WithLocation(14, 10),
                // (15,11): warning CS0169: The field 'Test.x5' is assigned but its value is never used
                //     X<S?> x5;       // nullable value type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x5").WithArguments("Test.x5").WithLocation(15, 11),
                // (16,11): warning CS8618: Non-nullable field 'x6' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     X<C?> x6;       // nullable reference type
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "x6").WithArguments("field", "x6").WithLocation(16, 11),
                // (16,11): warning CS0169: The field 'Test.x6' is assigned but its value is never used
                //     X<C?> x6;       // nullable reference type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x6").WithArguments("Test.x6").WithLocation(16, 11));
        }

        [Fact]
        public void AliasConstraint_Pointer1()
        {
            CreateCompilation(@"
using unsafe X<T> = T*;

public struct S {}
public class C {}

#nullable enable

public unsafe class Test
{
    X<int> x1;      // primitive value type
    X<string> x2;   // primitive reference type
    X<S> x3;        // value type
    X<C> x4;        // reference type
    X<S?> x5;       // nullable value type
    X<C?> x6;       // nullable reference type
}", options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                    // (2,14): warning CS8500: This takes the address of, gets the size of, or declares a pointer to a managed type ('T')
                    // using unsafe X<T> = T*;
                    Diagnostic(ErrorCode.WRN_ManagedAddr, "X").WithArguments("T").WithLocation(2, 14),
                    // (11,12): warning CS0169: The field 'Test.x1' is never used
                    //     X<int> x1;      // primitive value type
                    Diagnostic(ErrorCode.WRN_UnreferencedField, "x1").WithArguments("Test.x1").WithLocation(11, 12),
                    // (12,15): warning CS8500: This takes the address of, gets the size of, or declares a pointer to a managed type ('string')
                    //     X<string> x2;   // primitive reference type
                    Diagnostic(ErrorCode.WRN_ManagedAddr, "x2").WithArguments("string").WithLocation(12, 15),
                    // (12,15): warning CS0169: The field 'Test.x2' is never used
                    //     X<string> x2;   // primitive reference type
                    Diagnostic(ErrorCode.WRN_UnreferencedField, "x2").WithArguments("Test.x2").WithLocation(12, 15),
                    // (13,10): warning CS0169: The field 'Test.x3' is never used
                    //     X<S> x3;        // value type
                    Diagnostic(ErrorCode.WRN_UnreferencedField, "x3").WithArguments("Test.x3").WithLocation(13, 10),
                    // (14,10): warning CS8500: This takes the address of, gets the size of, or declares a pointer to a managed type ('C')
                    //     X<C> x4;        // reference type
                    Diagnostic(ErrorCode.WRN_ManagedAddr, "x4").WithArguments("C").WithLocation(14, 10),
                    // (14,10): warning CS0169: The field 'Test.x4' is never used
                    //     X<C> x4;        // reference type
                    Diagnostic(ErrorCode.WRN_UnreferencedField, "x4").WithArguments("Test.x4").WithLocation(14, 10),
                    // (15,11): warning CS0169: The field 'Test.x5' is never used
                    //     X<S?> x5;       // nullable value type
                    Diagnostic(ErrorCode.WRN_UnreferencedField, "x5").WithArguments("Test.x5").WithLocation(15, 11),
                    // (16,11): warning CS8500: This takes the address of, gets the size of, or declares a pointer to a managed type ('C')
                    //     X<C?> x6;       // nullable reference type
                    Diagnostic(ErrorCode.WRN_ManagedAddr, "x6").WithArguments("C").WithLocation(16, 11),
                    // (16,11): warning CS0169: The field 'Test.x6' is never used
                    //     X<C?> x6;       // nullable reference type
                    Diagnostic(ErrorCode.WRN_UnreferencedField, "x6").WithArguments("Test.x6").WithLocation(16, 11));
        }

        [Fact]
        public void AliasConstraint_Pointer2()
        {
            CreateCompilation(@"
using unsafe X<T> = T* where T : unmanaged;

public struct S {}
public class C {}

#nullable enable

public unsafe class Test
{
    X<int> x1;      // primitive value type
    X<string> x2;   // primitive reference type
    X<S> x3;        // value type
    X<C> x4;        // reference type
    X<S?> x5;       // nullable value type
    X<C?> x6;       // nullable reference type
}", options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (11,12): warning CS0169: The field 'Test.x1' is never used
                //     X<int> x1;      // primitive value type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x1").WithArguments("Test.x1").WithLocation(11, 12),
                // (12,7): error CS8377: The type 'string' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type, method or alias 'X<T>'
                //     X<string> x2;   // primitive reference type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "string").WithArguments("X<T>", "T", "string").WithLocation(12, 7),
                // (12,15): warning CS8500: This takes the address of, gets the size of, or declares a pointer to a managed type ('string')
                //     X<string> x2;   // primitive reference type
                Diagnostic(ErrorCode.WRN_ManagedAddr, "x2").WithArguments("string").WithLocation(12, 15),
                // (12,15): warning CS0169: The field 'Test.x2' is never used
                //     X<string> x2;   // primitive reference type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x2").WithArguments("Test.x2").WithLocation(12, 15),
                // (13,10): warning CS0169: The field 'Test.x3' is never used
                //     X<S> x3;        // value type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x3").WithArguments("Test.x3").WithLocation(13, 10),
                // (14,7): error CS8377: The type 'C' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type, method or alias 'X<T>'
                //     X<C> x4;        // reference type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "C").WithArguments("X<T>", "T", "C").WithLocation(14, 7),
                // (14,10): warning CS8500: This takes the address of, gets the size of, or declares a pointer to a managed type ('C')
                //     X<C> x4;        // reference type
                Diagnostic(ErrorCode.WRN_ManagedAddr, "x4").WithArguments("C").WithLocation(14, 10),
                // (14,10): warning CS0169: The field 'Test.x4' is never used
                //     X<C> x4;        // reference type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x4").WithArguments("Test.x4").WithLocation(14, 10),
                // (15,7): error CS8377: The type 'S?' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type, method or alias 'X<T>'
                //     X<S?> x5;       // nullable value type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "S?").WithArguments("X<T>", "T", "S?").WithLocation(15, 7),
                // (15,11): warning CS0169: The field 'Test.x5' is never used
                //     X<S?> x5;       // nullable value type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x5").WithArguments("Test.x5").WithLocation(15, 11),
                // (16,7): error CS8377: The type 'C' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type, method or alias 'X<T>'
                //     X<C?> x6;       // nullable reference type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "C?").WithArguments("X<T>", "T", "C").WithLocation(16, 7),
                // (16,11): warning CS8500: This takes the address of, gets the size of, or declares a pointer to a managed type ('C')
                //     X<C?> x6;       // nullable reference type
                Diagnostic(ErrorCode.WRN_ManagedAddr, "x6").WithArguments("C").WithLocation(16, 11),
                // (16,11): warning CS0169: The field 'Test.x6' is never used
                //     X<C?> x6;       // nullable reference type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x6").WithArguments("Test.x6").WithLocation(16, 11));
        }

        [Fact]
        public void AliasConstraint_Pointer3()
        {
            CreateCompilation(@"
using unsafe X<T> = T* where T : class;

#nullable enable

public struct S {}
public class C {}

public unsafe class Test
{
    X<int> x1;      // primitive value type
    X<string> x2;   // primitive reference type
    X<S> x3;        // value type
    X<C> x4;        // reference type
    X<S?> x5;       // nullable value type
    X<C?> x6;       // nullable reference type
}", options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (2,14): warning CS8500: This takes the address of, gets the size of, or declares a pointer to a managed type ('T')
                // using unsafe X<T> where T : class = T*;
                Diagnostic(ErrorCode.WRN_ManagedAddr, "X").WithArguments("T").WithLocation(2, 14),
                // (11,7): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type, method or alias 'X<T>'
                //     X<int> x1;      // primitive value type
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("X<T>", "T", "int").WithLocation(11, 7),
                // (11,12): warning CS0169: The field 'Test.x1' is never used
                //     X<int> x1;      // primitive value type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x1").WithArguments("Test.x1").WithLocation(11, 12),
                // (12,15): warning CS8500: This takes the address of, gets the size of, or declares a pointer to a managed type ('string')
                //     X<string> x2;   // primitive reference type
                Diagnostic(ErrorCode.WRN_ManagedAddr, "x2").WithArguments("string").WithLocation(12, 15),
                // (12,15): warning CS0169: The field 'Test.x2' is never used
                //     X<string> x2;   // primitive reference type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x2").WithArguments("Test.x2").WithLocation(12, 15),
                // (13,7): error CS0452: The type 'S' must be a reference type in order to use it as parameter 'T' in the generic type, method or alias 'X<T>'
                //     X<S> x3;        // value type
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "S").WithArguments("X<T>", "T", "S").WithLocation(13, 7),
                // (13,10): warning CS0169: The field 'Test.x3' is never used
                //     X<S> x3;        // value type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x3").WithArguments("Test.x3").WithLocation(13, 10),
                // (14,10): warning CS8500: This takes the address of, gets the size of, or declares a pointer to a managed type ('C')
                //     X<C> x4;        // reference type
                Diagnostic(ErrorCode.WRN_ManagedAddr, "x4").WithArguments("C").WithLocation(14, 10),
                // (14,10): warning CS0169: The field 'Test.x4' is never used
                //     X<C> x4;        // reference type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x4").WithArguments("Test.x4").WithLocation(14, 10),
                // (15,7): error CS0452: The type 'S?' must be a reference type in order to use it as parameter 'T' in the generic type, method or alias 'X<T>'
                //     X<S?> x5;       // nullable value type
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "S?").WithArguments("X<T>", "T", "S?").WithLocation(15, 7),
                // (15,11): warning CS0169: The field 'Test.x5' is never used
                //     X<S?> x5;       // nullable value type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x5").WithArguments("Test.x5").WithLocation(15, 11),
                // (16,11): warning CS8500: This takes the address of, gets the size of, or declares a pointer to a managed type ('C')
                //     X<C?> x6;       // nullable reference type
                Diagnostic(ErrorCode.WRN_ManagedAddr, "x6").WithArguments("C").WithLocation(16, 11),
                // (16,11): warning CS0169: The field 'Test.x6' is never used
                //     X<C?> x6;       // nullable reference type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x6").WithArguments("Test.x6").WithLocation(16, 11));
        }

        [Fact]
        public void AliasConstraint_Nullable1()
        {
            CreateCompilation(@"
#nullable enable

using X<T> = T?;

public struct S {}
public class C {}

public class Test
{
    X<int> x1;      // primitive value type
    X<string> x2;   // primitive reference type
    X<S> x3;        // value type
    X<C> x4;        // reference type
    X<S?> x5;       // nullable value type
    X<C?> x6;       // nullable reference type
}").VerifyDiagnostics(
                // (4,14): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'System.Nullable<T>'
                // using X<T> = T?;
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "T?").WithArguments("System.Nullable<T>", "T", "T").WithLocation(4, 14),
                // (11,12): warning CS0169: The field 'Test.x1' is assigned but its value is never used
                //     X<int> x1;      // primitive value type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x1").WithArguments("Test.x1").WithLocation(11, 12),
                // (12,15): warning CS8618: Non-nullable field 'x2' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     X<string> x2;   // primitive reference type
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "x2").WithArguments("field", "x2").WithLocation(12, 15),
                // (12,15): warning CS0169: The field 'Test.x2' is assigned but its value is never used
                //     X<string> x2;   // primitive reference type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x2").WithArguments("Test.x2").WithLocation(12, 15),
                // (13,10): warning CS0169: The field 'Test.x3' is assigned but its value is never used
                //     X<S> x3;        // value type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x3").WithArguments("Test.x3").WithLocation(13, 10),
                // (14,10): warning CS8618: Non-nullable field 'x4' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     X<C> x4;        // reference type
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "x4").WithArguments("field", "x4").WithLocation(14, 10),
                // (14,10): warning CS0169: The field 'Test.x4' is assigned but its value is never used
                //     X<C> x4;        // reference type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x4").WithArguments("Test.x4").WithLocation(14, 10),
                // (15,11): warning CS0169: The field 'Test.x5' is assigned but its value is never used
                //     X<S?> x5;       // nullable value type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x5").WithArguments("Test.x5").WithLocation(15, 11),
                // (16,11): warning CS8618: Non-nullable field 'x6' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
                //     X<C?> x6;       // nullable reference type
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "x6").WithArguments("field", "x6").WithLocation(16, 11),
                // (16,11): warning CS0169: The field 'Test.x6' is assigned but its value is never used
                //     X<C?> x6;       // nullable reference type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x6").WithArguments("Test.x6").WithLocation(16, 11));
        }

        [Fact]
        public void AliasConstraint_Nullable2()
        {
            CreateCompilation(@"
#nullable enable

using X<T> = T? where T : class?;

public struct S {}
public class C {}

public class Test
{
    X<int> x1;      // primitive value type
    X<string> x2;   // primitive reference type
    X<S> x3;        // value type
    X<C> x4;        // reference type
    X<S?> x5;       // nullable value type
    X<C?> x6;       // nullable reference type
}").VerifyDiagnostics(
                // (4,15): error CS9132: Using alias cannot be a nullable reference type.
                // using X<T> = T? where T : class?;
                Diagnostic(ErrorCode.ERR_BadNullableReferenceTypeInUsingAlias, "?").WithLocation(4, 15),
                // (4,14): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type, method or alias 'Nullable<T>'
                // using X<T> = T? where T : class?;
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "T?").WithArguments("System.Nullable<T>", "T", "T").WithLocation(4, 14),
                // (13,7): error CS0452: The type 'S' must be a reference type in order to use it as parameter 'T' in the generic type, method or alias 'X<T>'
                //     X<S> x3;        // value type
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "S").WithArguments("X<T>", "T", "S").WithLocation(13, 7),
                // (15,7): error CS0452: The type 'S?' must be a reference type in order to use it as parameter 'T' in the generic type, method or alias 'X<T>'
                //     X<S?> x5;       // nullable value type
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "S?").WithArguments("X<T>", "T", "S?").WithLocation(15, 7),
                // (11,7): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type, method or alias 'X<T>'
                //     X<int> x1;      // primitive value type
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("X<T>", "T", "int").WithLocation(11, 7),
                // (12,15): warning CS8618: Non-nullable field 'x2' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
                //     X<string> x2;   // primitive reference type
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "x2").WithArguments("field", "x2").WithLocation(12, 15),
                // (14,10): warning CS8618: Non-nullable field 'x4' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
                //     X<C> x4;        // reference type
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "x4").WithArguments("field", "x4").WithLocation(14, 10),
                // (16,11): warning CS8618: Non-nullable field 'x6' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
                //     X<C?> x6;       // nullable reference type
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "x6").WithArguments("field", "x6").WithLocation(16, 11),
                // (15,11): warning CS0169: The field 'Test.x5' is never used
                //     X<S?> x5;       // nullable value type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x5").WithArguments("Test.x5").WithLocation(15, 11),
                // (12,15): warning CS0169: The field 'Test.x2' is never used
                //     X<string> x2;   // primitive reference type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x2").WithArguments("Test.x2").WithLocation(12, 15),
                // (11,12): warning CS0169: The field 'Test.x1' is never used
                //     X<int> x1;      // primitive value type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x1").WithArguments("Test.x1").WithLocation(11, 12),
                // (14,10): warning CS0169: The field 'Test.x4' is never used
                //     X<C> x4;        // reference type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x4").WithArguments("Test.x4").WithLocation(14, 10),
                // (16,11): warning CS0169: The field 'Test.x6' is never used
                //     X<C?> x6;       // nullable reference type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x6").WithArguments("Test.x6").WithLocation(16, 11),
                // (13,10): warning CS0169: The field 'Test.x3' is never used
                //     X<S> x3;        // value type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x3").WithArguments("Test.x3").WithLocation(13, 10));
        }

        [Fact]
        public void AliasConstraint_Nullable3()
        {
            CreateCompilation(@"
#nullable enable

using X<T> = T? where T : struct;

public struct S {}
public class C {}

public class Test
{
    X<int> x1;      // primitive value type
    X<string> x2;   // primitive reference type
    X<S> x3;        // value type
    X<C> x4;        // reference type
    X<S?> x5;       // nullable value type
    X<C?> x6;       // nullable reference type
}").VerifyDiagnostics(
                // (11,12): warning CS0169: The field 'Test.x1' is never used
                //     X<int> x1;      // primitive value type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x1").WithArguments("Test.x1").WithLocation(11, 12),
                // (12,7): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type, method or alias 'X<T>'
                //     X<string> x2;   // primitive reference type
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "string").WithArguments("X<T>", "T", "string").WithLocation(12, 7),
                // (12,15): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type, method or alias 'Nullable<T>'
                //     X<string> x2;   // primitive reference type
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "x2").WithArguments("System.Nullable<T>", "T", "string").WithLocation(12, 15),
                // (12,15): warning CS0169: The field 'Test.x2' is never used
                //     X<string> x2;   // primitive reference type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x2").WithArguments("Test.x2").WithLocation(12, 15),
                // (13,10): warning CS0169: The field 'Test.x3' is never used
                //     X<S> x3;        // value type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x3").WithArguments("Test.x3").WithLocation(13, 10),
                // (14,7): error CS0453: The type 'C' must be a non-nullable value type in order to use it as parameter 'T' in the generic type, method or alias 'X<T>'
                //     X<C> x4;        // reference type
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "C").WithArguments("X<T>", "T", "C").WithLocation(14, 7),
                // (14,10): error CS0453: The type 'C' must be a non-nullable value type in order to use it as parameter 'T' in the generic type, method or alias 'Nullable<T>'
                //     X<C> x4;        // reference type
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "x4").WithArguments("System.Nullable<T>", "T", "C").WithLocation(14, 10),
                // (14,10): warning CS0169: The field 'Test.x4' is never used
                //     X<C> x4;        // reference type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x4").WithArguments("Test.x4").WithLocation(14, 10),
                // (15,7): error CS0453: The type 'S?' must be a non-nullable value type in order to use it as parameter 'T' in the generic type, method or alias 'X<T>'
                //     X<S?> x5;       // nullable value type
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "S?").WithArguments("X<T>", "T", "S?").WithLocation(15, 7),
                // (15,11): error CS0453: The type 'S?' must be a non-nullable value type in order to use it as parameter 'T' in the generic type, method or alias 'Nullable<T>'
                //     X<S?> x5;       // nullable value type
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "x5").WithArguments("System.Nullable<T>", "T", "S?").WithLocation(15, 11),
                // (15,11): warning CS0169: The field 'Test.x5' is never used
                //     X<S?> x5;       // nullable value type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x5").WithArguments("Test.x5").WithLocation(15, 11),
                // (16,7): error CS0453: The type 'C' must be a non-nullable value type in order to use it as parameter 'T' in the generic type, method or alias 'X<T>'
                //     X<C?> x6;       // nullable reference type
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "C?").WithArguments("X<T>", "T", "C").WithLocation(16, 7),
                // (16,11): error CS0453: The type 'C' must be a non-nullable value type in order to use it as parameter 'T' in the generic type, method or alias 'Nullable<T>'
                //     X<C?> x6;       // nullable reference type
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "x6").WithArguments("System.Nullable<T>", "T", "C").WithLocation(16, 11),
                // (16,11): warning CS0169: The field 'Test.x6' is never used
                //     X<C?> x6;       // nullable reference type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x6").WithArguments("Test.x6").WithLocation(16, 11));
        }

        [Fact]
        public void AliasConstraint_Nullable4()
        {
            CreateCompilation(@"
#nullable enable

using X<T> = T? where T : unmanaged;

public struct U {}
public struct S
{
    string? s;
}
public class C {}

public class Test
{
    X<int> x1;      // primitive unmanaged type
    X<string> x2;   // primitive reference type
    X<U> x3;        // unmanaged type
    X<S> x4;        // value type
    X<C> x5;        // reference type
    X<U?> x6;       // nullable unmanaged type
    X<S?> x7;       // nullable value type
    X<C?> x8;       // nullable reference type
}").VerifyDiagnostics(
                // (9,13): warning CS0169: The field 'S.s' is never used
                //     string? s;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "s").WithArguments("S.s").WithLocation(9, 13),
                // (15,12): warning CS0169: The field 'Test.x1' is never used
                //     X<int> x1;      // primitive unmanaged type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x1").WithArguments("Test.x1").WithLocation(15, 12),
                // (16,7): error CS8377: The type 'string' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type, method or alias 'X<T>'
                //     X<string> x2;   // primitive reference type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "string").WithArguments("X<T>", "T", "string").WithLocation(16, 7),
                // (16,15): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type, method or alias 'Nullable<T>'
                //     X<string> x2;   // primitive reference type
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "x2").WithArguments("System.Nullable<T>", "T", "string").WithLocation(16, 15),
                // (16,15): warning CS0169: The field 'Test.x2' is never used
                //     X<string> x2;   // primitive reference type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x2").WithArguments("Test.x2").WithLocation(16, 15),
                // (17,10): warning CS0169: The field 'Test.x3' is never used
                //     X<U> x3;        // unmanaged type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x3").WithArguments("Test.x3").WithLocation(17, 10),
                // (18,7): error CS8377: The type 'S' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type, method or alias 'X<T>'
                //     X<S> x4;        // value type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "S").WithArguments("X<T>", "T", "S").WithLocation(18, 7),
                // (18,10): warning CS0169: The field 'Test.x4' is never used
                //     X<S> x4;        // value type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x4").WithArguments("Test.x4").WithLocation(18, 10),
                // (19,7): error CS8377: The type 'C' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type, method or alias 'X<T>'
                //     X<C> x5;        // reference type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "C").WithArguments("X<T>", "T", "C").WithLocation(19, 7),
                // (19,10): error CS0453: The type 'C' must be a non-nullable value type in order to use it as parameter 'T' in the generic type, method or alias 'Nullable<T>'
                //     X<C> x5;        // reference type
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "x5").WithArguments("System.Nullable<T>", "T", "C").WithLocation(19, 10),
                // (19,10): warning CS0169: The field 'Test.x5' is never used
                //     X<C> x5;        // reference type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x5").WithArguments("Test.x5").WithLocation(19, 10),
                // (20,7): error CS8377: The type 'U?' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type, method or alias 'X<T>'
                //     X<U?> x6;       // nullable unmanaged type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "U?").WithArguments("X<T>", "T", "U?").WithLocation(20, 7),
                // (20,11): error CS0453: The type 'U?' must be a non-nullable value type in order to use it as parameter 'T' in the generic type, method or alias 'Nullable<T>'
                //     X<U?> x6;       // nullable unmanaged type
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "x6").WithArguments("System.Nullable<T>", "T", "U?").WithLocation(20, 11),
                // (20,11): warning CS0169: The field 'Test.x6' is never used
                //     X<U?> x6;       // nullable unmanaged type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x6").WithArguments("Test.x6").WithLocation(20, 11),
                // (21,7): error CS8377: The type 'S?' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type, method or alias 'X<T>'
                //     X<S?> x7;       // nullable value type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "S?").WithArguments("X<T>", "T", "S?").WithLocation(21, 7),
                // (21,11): error CS0453: The type 'S?' must be a non-nullable value type in order to use it as parameter 'T' in the generic type, method or alias 'Nullable<T>'
                //     X<S?> x7;       // nullable value type
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "x7").WithArguments("System.Nullable<T>", "T", "S?").WithLocation(21, 11),
                // (21,11): warning CS0169: The field 'Test.x7' is never used
                //     X<S?> x7;       // nullable value type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x7").WithArguments("Test.x7").WithLocation(21, 11),
                // (22,7): error CS8377: The type 'C' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type, method or alias 'X<T>'
                //     X<C?> x8;       // nullable reference type
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "C?").WithArguments("X<T>", "T", "C").WithLocation(22, 7),
                // (22,11): error CS0453: The type 'C' must be a non-nullable value type in order to use it as parameter 'T' in the generic type, method or alias 'Nullable<T>'
                //     X<C?> x8;       // nullable reference type
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "x8").WithArguments("System.Nullable<T>", "T", "C").WithLocation(22, 11),
                // (22,11): warning CS0169: The field 'Test.x8' is never used
                //     X<C?> x8;       // nullable reference type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x8").WithArguments("Test.x8").WithLocation(22, 11));
        }

        [Fact]
        public void AliasConstraint_Nullable5()
        {
            CreateCompilation(@"
#nullable enable

using X<T> = T? where T : notnull;

public struct S {}
public class C {}

public class Test
{
    X<int> x1;      // primitive value type
    X<string> x2;   // primitive reference type
    X<S> x3;        // value type
    X<C> x4;        // reference type
    X<S?> x5;       // nullable value type
    X<C?> x6;       // nullable reference type
}").VerifyDiagnostics(
                // (4,14): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type, method or alias 'Nullable<T>'
                // using X<T> = T? where T : notnull;
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "T?").WithArguments("System.Nullable<T>", "T", "T").WithLocation(4, 14),
                // (11,12): warning CS0169: The field 'Test.x1' is never used
                //     X<int> x1;      // primitive value type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x1").WithArguments("Test.x1").WithLocation(11, 12),
                // (12,15): warning CS8618: Non-nullable field 'x2' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
                //     X<string> x2;   // primitive reference type
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "x2").WithArguments("field", "x2").WithLocation(12, 15),
                // (12,15): warning CS0169: The field 'Test.x2' is never used
                //     X<string> x2;   // primitive reference type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x2").WithArguments("Test.x2").WithLocation(12, 15),
                // (13,10): warning CS0169: The field 'Test.x3' is never used
                //     X<S> x3;        // value type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x3").WithArguments("Test.x3").WithLocation(13, 10),
                // (14,10): warning CS8618: Non-nullable field 'x4' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
                //     X<C> x4;        // reference type
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "x4").WithArguments("field", "x4").WithLocation(14, 10),
                // (14,10): warning CS0169: The field 'Test.x4' is never used
                //     X<C> x4;        // reference type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x4").WithArguments("Test.x4").WithLocation(14, 10),
                // (15,7): warning CS8714: The type 'S?' cannot be used as type parameter 'T' in the generic type, method or alias 'X<T>'. Nullability of type argument 'S?' doesn't match 'notnull' constraint.
                //     X<S?> x5;       // nullable value type
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "S?").WithArguments("X<T>", "T", "S?").WithLocation(15, 7),
                // (15,11): warning CS0169: The field 'Test.x5' is never used
                //     X<S?> x5;       // nullable value type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x5").WithArguments("Test.x5").WithLocation(15, 11),
                // (16,7): warning CS8714: The type 'C?' cannot be used as type parameter 'T' in the generic type, method or alias 'X<T>'. Nullability of type argument 'C?' doesn't match 'notnull' constraint.
                //     X<C?> x6;       // nullable reference type
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint, "C?").WithArguments("X<T>", "T", "C?").WithLocation(16, 7),
                // (16,11): warning CS8618: Non-nullable field 'x6' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
                //     X<C?> x6;       // nullable reference type
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "x6").WithArguments("field", "x6").WithLocation(16, 11),
                // (16,11): warning CS0169: The field 'Test.x6' is never used
                //     X<C?> x6;       // nullable reference type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x6").WithArguments("Test.x6").WithLocation(16, 11));
        }

        [Fact]
        public void AliasConstraint_Enum()
        {
            CreateCompilation(@"
using EnumDictionary<TKey, TValue> = System.Collections.Generic.Dictionary<TKey, TValue> where TKey : System.Enum;

public struct S {}
public class C {}
public enum E {}
public interface I {}
public delegate void D();

public class Test
{
    EnumDictionary<S, S> sDic;                      // struct type
    EnumDictionary<C, C> cDic;                      // class type
    EnumDictionary<E, E> eDic;                      // enum type
    EnumDictionary<I, I> iDic;                      // interface type
    EnumDictionary<D, D> dDic;                      // delegate type
    EnumDictionary<System.Enum, System.Enum> dic;   // base type
}").VerifyDiagnostics(
                // (12,20): error CS0315: The type 'S' cannot be used as type parameter 'TKey' in the generic type, method or alias 'EnumDictionary<TKey, TValue>'. There is no boxing conversion from 'S' to 'System.Enum'.
                //     EnumDictionary<S, S> sDic;                      // struct type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "S").WithArguments("EnumDictionary<TKey, TValue>", "System.Enum", "TKey", "S").WithLocation(12, 20),
                // (12,26): warning CS0169: The field 'Test.sDic' is never used
                //     EnumDictionary<S, S> sDic;                      // struct type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "sDic").WithArguments("Test.sDic").WithLocation(12, 26),
                // (13,20): error CS0311: The type 'C' cannot be used as type parameter 'TKey' in the generic type, method or alias 'EnumDictionary<TKey, TValue>'. There is no implicit reference conversion from 'C' to 'System.Enum'.
                //     EnumDictionary<C, C> cDic;                      // class type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "C").WithArguments("EnumDictionary<TKey, TValue>", "System.Enum", "TKey", "C").WithLocation(13, 20),
                // (13,26): warning CS0169: The field 'Test.cDic' is never used
                //     EnumDictionary<C, C> cDic;                      // class type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "cDic").WithArguments("Test.cDic").WithLocation(13, 26),
                // (14,26): warning CS0169: The field 'Test.eDic' is never used
                //     EnumDictionary<E, E> eDic;                      // enum type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "eDic").WithArguments("Test.eDic").WithLocation(14, 26),
                // (15,20): error CS0311: The type 'I' cannot be used as type parameter 'TKey' in the generic type, method or alias 'EnumDictionary<TKey, TValue>'. There is no implicit reference conversion from 'I' to 'System.Enum'.
                //     EnumDictionary<I, I> iDic;                      // interface type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "I").WithArguments("EnumDictionary<TKey, TValue>", "System.Enum", "TKey", "I").WithLocation(15, 20),
                // (15,26): warning CS0169: The field 'Test.iDic' is never used
                //     EnumDictionary<I, I> iDic;                      // interface type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "iDic").WithArguments("Test.iDic").WithLocation(15, 26),
                // (16,20): error CS0311: The type 'D' cannot be used as type parameter 'TKey' in the generic type, method or alias 'EnumDictionary<TKey, TValue>'. There is no implicit reference conversion from 'D' to 'System.Enum'.
                //     EnumDictionary<D, D> dDic;                      // delegate type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "D").WithArguments("EnumDictionary<TKey, TValue>", "System.Enum", "TKey", "D").WithLocation(16, 20),
                // (16,26): warning CS0169: The field 'Test.dDic' is never used
                //     EnumDictionary<D, D> dDic;                      // delegate type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "dDic").WithArguments("Test.dDic").WithLocation(16, 26),
                // (17,46): warning CS0169: The field 'Test.dic' is never used
                //     EnumDictionary<System.Enum, System.Enum> dic;   // base type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "dic").WithArguments("Test.dic").WithLocation(17, 46));
        }

        [Fact]
        public void AliasConstraint_Interface()
        {
            CreateCompilation(@"
using DeepInterface<TInterface> = I<I<I<I<I<TInterface>>>>> where TInterface : I;

public struct S : I {}
public class C {}
public enum E {}
public interface I {}
public interface I<T> : I where T : I {}
public delegate void D();

public class Test
{
    DeepInterface<S> sInter;        // struct type
    DeepInterface<C> cInter;        // class type
    DeepInterface<E> eInter;        // enum type
    DeepInterface<I> iInter;        // interface type
    DeepInterface<I<I>> inter;      // generic interface type
    DeepInterface<D> dInter;        // delegate type
}").VerifyDiagnostics(
                // (13,22): warning CS0169: The field 'Test.sInter' is never used
                //     DeepInterface<S> sInter;        // struct type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "sInter").WithArguments("Test.sInter").WithLocation(13, 22),
                // (14,19): error CS0311: The type 'C' cannot be used as type parameter 'TInterface' in the generic type, method or alias 'DeepInterface<TInterface>'. There is no implicit reference conversion from 'C' to 'I'.
                //     DeepInterface<C> cInter;        // class type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "C").WithArguments("DeepInterface<TInterface>", "I", "TInterface", "C").WithLocation(14, 19),
                // (14,22): error CS0311: The type 'C' cannot be used as type parameter 'T' in the generic type, method or alias 'I<T>'. There is no implicit reference conversion from 'C' to 'I'.
                //     DeepInterface<C> cInter;        // class type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "cInter").WithArguments("I<T>", "I", "T", "C").WithLocation(14, 22),
                // (14,22): warning CS0169: The field 'Test.cInter' is never used
                //     DeepInterface<C> cInter;        // class type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "cInter").WithArguments("Test.cInter").WithLocation(14, 22),
                // (15,19): error CS0315: The type 'E' cannot be used as type parameter 'TInterface' in the generic type, method or alias 'DeepInterface<TInterface>'. There is no boxing conversion from 'E' to 'I'.
                //     DeepInterface<E> eInter;        // enum type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "E").WithArguments("DeepInterface<TInterface>", "I", "TInterface", "E").WithLocation(15, 19),
                // (15,22): error CS0315: The type 'E' cannot be used as type parameter 'T' in the generic type, method or alias 'I<T>'. There is no boxing conversion from 'E' to 'I'.
                //     DeepInterface<E> eInter;        // enum type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "eInter").WithArguments("I<T>", "I", "T", "E").WithLocation(15, 22),
                // (15,22): warning CS0169: The field 'Test.eInter' is never used
                //     DeepInterface<E> eInter;        // enum type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "eInter").WithArguments("Test.eInter").WithLocation(15, 22),
                // (16,22): warning CS0169: The field 'Test.iInter' is never used
                //     DeepInterface<I> iInter;        // interface type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "iInter").WithArguments("Test.iInter").WithLocation(16, 22),
                // (17,25): warning CS0169: The field 'Test.inter' is never used
                //     DeepInterface<I<I>> inter;      // generic interface type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "inter").WithArguments("Test.inter").WithLocation(17, 25),
                // (18,19): error CS0311: The type 'D' cannot be used as type parameter 'TInterface' in the generic type, method or alias 'DeepInterface<TInterface>'. There is no implicit reference conversion from 'D' to 'I'.
                //     DeepInterface<D> dInter;        // delegate type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "D").WithArguments("DeepInterface<TInterface>", "I", "TInterface", "D").WithLocation(18, 19),
                // (18,22): error CS0311: The type 'D' cannot be used as type parameter 'T' in the generic type, method or alias 'I<T>'. There is no implicit reference conversion from 'D' to 'I'.
                //     DeepInterface<D> dInter;        // delegate type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "dInter").WithArguments("I<T>", "I", "T", "D").WithLocation(18, 22),
                // (18,22): warning CS0169: The field 'Test.dInter' is never used
                //     DeepInterface<D> dInter;        // delegate type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "dInter").WithArguments("Test.dInter").WithLocation(18, 22));
        }

        [Fact]
        public void AliasConstraint_Delegate()
        {
            CreateCompilation(@"
using ActionWithCallback<TCallback> = System.Action<TCallback> where TCallback : System.Delegate;

public struct S {}
public class C {}
public enum E {}
public interface I {}
public delegate void D();

public class Test
{
    ActionWithCallback<S> sAct;                 // struct type
    ActionWithCallback<C> cAct;                 // class type
    ActionWithCallback<E> eAct;                 // enum type
    ActionWithCallback<I> iAct;                 // interface type
    ActionWithCallback<D> dAct;                 // delegate type
    ActionWithCallback<System.Delegate> act;    // base type
}").VerifyDiagnostics(
                // (12,24): error CS0315: The type 'S' cannot be used as type parameter 'TCallback' in the generic type, method or alias 'ActionWithCallback<TCallback>'. There is no boxing conversion from 'S' to 'System.Delegate'.
                //     ActionWithCallback<S> sAct;                 // struct type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "S").WithArguments("ActionWithCallback<TCallback>", "System.Delegate", "TCallback", "S").WithLocation(12, 24),
                // (12,27): warning CS0169: The field 'Test.sAct' is never used
                //     ActionWithCallback<S> sAct;                 // struct type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "sAct").WithArguments("Test.sAct").WithLocation(12, 27),
                // (13,24): error CS0311: The type 'C' cannot be used as type parameter 'TCallback' in the generic type, method or alias 'ActionWithCallback<TCallback>'. There is no implicit reference conversion from 'C' to 'System.Delegate'.
                //     ActionWithCallback<C> cAct;                 // class type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "C").WithArguments("ActionWithCallback<TCallback>", "System.Delegate", "TCallback", "C").WithLocation(13, 24),
                // (13,27): warning CS0169: The field 'Test.cAct' is never used
                //     ActionWithCallback<C> cAct;                 // class type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "cAct").WithArguments("Test.cAct").WithLocation(13, 27),
                // (14,24): error CS0315: The type 'E' cannot be used as type parameter 'TCallback' in the generic type, method or alias 'ActionWithCallback<TCallback>'. There is no boxing conversion from 'E' to 'System.Delegate'.
                //     ActionWithCallback<E> eAct;                 // enum type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "E").WithArguments("ActionWithCallback<TCallback>", "System.Delegate", "TCallback", "E").WithLocation(14, 24),
                // (14,27): warning CS0169: The field 'Test.eAct' is never used
                //     ActionWithCallback<E> eAct;                 // enum type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "eAct").WithArguments("Test.eAct").WithLocation(14, 27),
                // (15,24): error CS0311: The type 'I' cannot be used as type parameter 'TCallback' in the generic type, method or alias 'ActionWithCallback<TCallback>'. There is no implicit reference conversion from 'I' to 'System.Delegate'.
                //     ActionWithCallback<I> iAct;                 // interface type
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "I").WithArguments("ActionWithCallback<TCallback>", "System.Delegate", "TCallback", "I").WithLocation(15, 24),
                // (15,27): warning CS0169: The field 'Test.iAct' is never used
                //     ActionWithCallback<I> iAct;                 // interface type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "iAct").WithArguments("Test.iAct").WithLocation(15, 27),
                // (16,27): warning CS0169: The field 'Test.dAct' is never used
                //     ActionWithCallback<D> dAct;                 // delegate type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "dAct").WithArguments("Test.dAct").WithLocation(16, 27),
                // (17,41): warning CS0169: The field 'Test.act' is never used
                //     ActionWithCallback<System.Delegate> act;    // base type
                Diagnostic(ErrorCode.WRN_UnreferencedField, "act").WithArguments("Test.act").WithLocation(17, 41));
        }
    }
}

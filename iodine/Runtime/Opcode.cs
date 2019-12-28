/**
  * Copyright (c) 2015, phath0m All rights reserved.

  * Redistribution and use in source and binary forms, with or without modification,
  * are permitted provided that the following conditions are met:
  * 
  *  * Redistributions of source code must retain the above copyright notice, this list
  *    of conditions and the following disclaimer.
  * 
  *  * Redistributions in binary form must reproduce the above copyright notice, this
  *    list of conditions and the following disclaimer in the documentation and/or
  *    other materials provided with the distribution.

  * Neither the name of the copyright holder nor the names of its contributors may be
  * used to endorse or promote products derived from this software without specific
  * prior written permission.
  * 
  * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY
  * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
  * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT
  * SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
  * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
  * TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR
  * BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
  * CONTRACT ,STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
  * ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH
  * DAMAGE.
**/

namespace Iodine.Runtime
{
    /// <summary>
    /// Operation codes used by the Virtual Machine
    /// </summary>
    public enum Opcode
    {
        Nop = 0,
        UnaryOp = 2,
        Pop = 3,
        Dup = 4,
        Dup3 = 5,
        LoadConst = 6,
        LoadNull = 7,
        LoadSelf = 8,
        LoadTrue = 9,
        LoadFalse = 0x0A,
        LoadLocal = 0x0B,
        StoreLocal = 0x0C,
        LoadGlobal = 0x0D,
        StoreGlobal = 0x0E,
        LoadAttributeOrNull = 0x0F,
        LoadAttribute = 0x10,
        StoreAttribute = 0x11,
        LoadIndex = 0x12,
        StoreIndex = 0x13,
        Invoke = 0x14,
        InvokeSuper = 0x15,
        InvokeVar = 0x16,
        Return = 0x17,
        Yield = 0x18,
        JumpIfTrue = 0x19,
        JumpIfFalse = 0x1A,
        Jump = 0x1B,
        BuildHash = 0x1C,
        BuildList = 0x1D,
        BuildTuple = 0x1E,
        BuildClosure = 0x1F,
        GetIter = 0x20,
        IterGetNext = 0x21,
        IterMoveNext = 0x22,
        IterReset = 0x23,
        Raise = 0x24,
        PushExceptionHandler = 0x25,
        PopExceptionHandler = 0x26,
        LoadException = 0x27,
        BeginExcept = 0x28,
        InstanceOf = 0x29,
        DynamicCast = 0x2A,
        Import = 0x2B,
        ImportFrom = 0x2C,
        ImportAll = 0x2D,
        SwitchLookup = 0x2E,
        NullCoalesce = 0x2F,
        BeginWith = 0x30,
        EndWith = 0x31,
        Slice = 0x32,
        BuildGenExpr = 0x33,
        CastLocal = 0x34,
        IncludeMixin = 0x35,
        ApplyMixin = 0x36,
        BuildFunction = 0x37,
        BuildClass = 0x39,
        BuildEnum = 0x3A,
        BuildContract = 0x3B,
        BuildMixin = 0x3C,
        BuildTrait = 0x3D,
        MatchPattern = 0x3E,
        Unwrap = 0x3F,
        Unpack = 0x40,
        TestUnwrap = 0x41,
        Add = 0x42,
        Sub = 0x43,
        Div = 0x44,
        Mul = 0x45,
        Mod = 0x46,
        And = 0x47,
        Or = 0x48,
        Xor = 0x49,
        GreaterThan = 0x4A,
        LessThan = 0x4B,
        GreaterThanOrEqu = 0x4C,
        LessThanOrEqu = 0x4D,
        HalfRange = 0x4E,
        ClosedRange = 0x4F,
        LeftShift = 0x50,
        RightShift = 0x51,
        BoolAnd = 0x52,
        BoolOr = 0x53,
        Equals = 0x54,
        NotEquals = 0x55,
        RangeCheck = 0x56,
        BuildRegex = 0x57
    }
}


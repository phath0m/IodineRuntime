﻿/**
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

using System;
using System.Numerics;
using System.Globalization;
using Iodine.Compiler;

namespace Iodine.Runtime
{
    public sealed class IodineBigInt : IodineObject
    {
        public static readonly IodineTypeDefinition TypeDefinition = new BigIntTypeDef ();

        class BigIntTypeDef : IodineTypeDefinition
        {
            public BigIntTypeDef ()
                : base ("BigInt")
            {
                SetDocumentation (
                    "An arbitrary size integer"
                );
            }

            public override IodineObject Invoke (VirtualMachine vm, IodineObject[] args)
            {
                if (args.Length <= 0) {
                    vm.RaiseException (new IodineArgumentException (1));
                }

                if (args [0] is IodineFloat) {
                    var fp = args [0] as IodineFloat;
                    return new IodineBigInt ((long)fp.Value);
                }

                if (args [0] is IodineInteger) {
                    var integer = args [0] as IodineInteger;
                    return new IodineBigInt (new BigInteger (integer.Value));
                }

                BigInteger value;

                if (!BigInteger.TryParse (args [0].ToString (), out value)) {
                    vm.RaiseException (new IodineTypeCastException ("Int"));
                    return null;
                } else {
                    return new IodineBigInt (value);
                }
            }
        }

        public readonly BigInteger Value;

        public IodineBigInt (BigInteger val)
            : base (TypeDefinition)
        {
            Value = val;
        }

        public override bool Equals (IodineObject obj)
        {
            BigInteger intVal;

            if (ConvertToBigInt (obj, out intVal)) {
                return intVal == Value;
            }

            return false;
        }

        #region Operator implementations

        public override IodineObject Add (VirtualMachine vm, IodineObject right)
        {
            BigInteger intVal;
            if (!ConvertToBigInt (right, out intVal)) {
                vm.RaiseException (new IodineTypeException ("Right hand side must be of type Int!"));
                return null;
            }
            return new IodineBigInt (Value + intVal);
        }

        public override IodineObject Sub (VirtualMachine vm, IodineObject right)
        {
            BigInteger intVal;
            if (!ConvertToBigInt (right, out intVal)) {
                vm.RaiseException (new IodineTypeException ("Right hand side must be of type Int!"));
                return null;
            }
            return new IodineBigInt (Value - intVal);
        }

        public override IodineObject Mul (VirtualMachine vm, IodineObject right)
        {
            BigInteger intVal;
            if (!ConvertToBigInt (right, out intVal)) {
                vm.RaiseException (new IodineTypeException ("Right hand side must be of type Int!"));
                return null;
            }
            return new IodineBigInt (Value * intVal);
        }

        public override IodineObject Div (VirtualMachine vm, IodineObject right)
        {
            BigInteger intVal;
            if (!ConvertToBigInt (right, out intVal)) {
                vm.RaiseException (new IodineTypeException ("Right hand side must be of type Int!"));
                return null;
            }
            return new IodineBigInt (Value / intVal);
        }

        public override IodineObject Mod (VirtualMachine vm, IodineObject right)
        {
            BigInteger intVal;
            if (!ConvertToBigInt (right, out intVal)) {
                vm.RaiseException (new IodineTypeException ("Right hand side must be of type Int!"));
                return null;
            }
            return new IodineBigInt (Value % intVal);
        }

        public override IodineObject Pow (VirtualMachine vm, IodineObject right)
        {
            BigInteger intVal;
            if (!ConvertToBigInt (right, out intVal)) {
                vm.RaiseException (new IodineTypeException ("Right hand side must be of type Int!"));
                return null;
            }
            return new IodineBigInt (BigInteger.Pow (Value, (int)intVal));
        }

        public override IodineObject And (VirtualMachine vm, IodineObject right)
        {
            BigInteger intVal;
            if (!ConvertToBigInt (right, out intVal)) {
                vm.RaiseException (new IodineTypeException ("Right hand side must be of type Int!"));
                return null;
            }
            return new IodineBigInt (Value & intVal);
        }

        public override IodineObject Or (VirtualMachine vm, IodineObject right)
        {
            BigInteger intVal;
            if (!ConvertToBigInt (right, out intVal)) {
                vm.RaiseException (new IodineTypeException ("Right hand side must be of type Int!"));
                return null;
            }
            return new IodineBigInt (Value | intVal);
        }

        public override IodineObject Xor (VirtualMachine vm, IodineObject right)
        {
            BigInteger intVal;
            if (!ConvertToBigInt (right, out intVal)) {
                vm.RaiseException (new IodineTypeException ("Right hand side must be of type Int!"));
                return null;
            }
            return new IodineBigInt (Value ^ intVal);
        }
            
        public override IodineObject Equals (VirtualMachine vm, IodineObject right)
        {
            BigInteger intVal;
            if (!ConvertToBigInt (right, out intVal)) {
                vm.RaiseException (new IodineTypeException ("Right hand side must be of type Int!"));
                return null;
            }
            return IodineBool.Create (Value == intVal);
        }

        public override IodineObject NotEquals (VirtualMachine vm, IodineObject right)
        {
            BigInteger intVal;
            if (!ConvertToBigInt (right, out intVal)) {
                vm.RaiseException (new IodineTypeException ("Right hand side must be of type Int!"));
                return null;
            }
            return IodineBool.Create (Value != intVal);
        }

        public override IodineObject GreaterThan (VirtualMachine vm, IodineObject right)
        {
            BigInteger intVal;
            if (!ConvertToBigInt (right, out intVal)) {
                vm.RaiseException (new IodineTypeException ("Right hand side must be of type Int!"));
                return null;
            }
            return IodineBool.Create (Value > intVal);
        }

        public override IodineObject GreaterThanOrEqual (VirtualMachine vm, IodineObject right)
        {
            BigInteger intVal;
            if (!ConvertToBigInt (right, out intVal)) {
                vm.RaiseException (new IodineTypeException ("Right hand side must be of type Int!"));
            }
            return IodineBool.Create (Value >= intVal);
        }

        public override IodineObject LessThan (VirtualMachine vm, IodineObject right)
        {
            BigInteger intVal;
            if (!ConvertToBigInt (right, out intVal)) {
                vm.RaiseException (new IodineTypeException ("Right hand side must be of type Int!"));
                return null;
            }
            return IodineBool.Create (Value < intVal);
        }

        public override IodineObject LessThanOrEqual (VirtualMachine vm, IodineObject right)
        {
            BigInteger intVal;
            if (!ConvertToBigInt (right, out intVal)) {
                vm.RaiseException (new IodineTypeException ("Right hand side must be of type Int!"));
                return null;
            }
            return IodineBool.Create (Value <= intVal);
        }

        public override IodineObject LeftShift (VirtualMachine vm, IodineObject right)
        {
            BigInteger intVal;
            if (!ConvertToBigInt (right, out intVal)) {
                vm.RaiseException (new IodineTypeException ("Right hand side must be of type Int!"));
                return null;
            }
            return new IodineBigInt (Value * BigInteger.Pow (2, (int)(uint)intVal));
        }

        public override IodineObject RightShift (VirtualMachine vm, IodineObject right)
        {
            BigInteger intVal;
            if (!ConvertToBigInt (right, out intVal)) {
                vm.RaiseException (new IodineTypeException ("Right hand side must be of type Int!"));
                return null;
            }
            return new IodineBigInt (Value / BigInteger.Pow (2, (int)(uint)intVal));
        }

        #endregion

        public override IodineObject PerformUnaryOperation (VirtualMachine vm, UnaryOperation op)
        {
            switch (op) {
            case UnaryOperation.Not:
                return new IodineBigInt (~Value);
            case UnaryOperation.Negate:
                return new IodineBigInt (-Value);
            }
            return null;
        }

        private static bool ConvertToBigInt (IodineObject obj, out BigInteger result)
        {
            if (obj is IodineBigInt) {
                result = ((IodineBigInt)obj).Value;
                return true;
            }

            if (obj is IodineInteger) {
                result = new BigInteger (((IodineInteger)obj).Value);
                return true;
            }

            result = BigInteger.Zero;
            return false;
        }

        public override bool IsTrue ()
        {
            return Value > 0;
        }

        public override string ToString ()
        {
            return Value.ToString ();
        }

        public override int GetHashCode ()
        {
            return Value.GetHashCode ();
        }
    }
}


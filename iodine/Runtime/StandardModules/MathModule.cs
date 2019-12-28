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

using System;

namespace Iodine.Runtime
{
    [BuiltinDocString (
        "Provides miscellaneous mathematical functions."
    )]
    [IodineBuiltinModule ("math")]
    public class MathModule : IodineModule
    {
        public MathModule ()
            : base ("math")
        {
            SetAttribute ("PI", new IodineFloat (Math.PI));
            SetAttribute ("E", new IodineFloat (Math.E));
            SetAttribute ("pow", new BuiltinMethodCallback (Pow, this));
            SetAttribute ("sin", new BuiltinMethodCallback (Sin, this));
            SetAttribute ("cos", new BuiltinMethodCallback (Cos, this));
            SetAttribute ("tan", new BuiltinMethodCallback (Tan, this));
            SetAttribute ("asin", new BuiltinMethodCallback (ASin, this));
            SetAttribute ("acos", new BuiltinMethodCallback (ACos, this));
            SetAttribute ("atan", new BuiltinMethodCallback (ATan, this));
            SetAttribute ("abs", new BuiltinMethodCallback (Abs, this));
            SetAttribute ("sqrt", new BuiltinMethodCallback (Sqrt, this));
            SetAttribute ("floor", new BuiltinMethodCallback (Floor, this));
            SetAttribute ("ceiling", new BuiltinMethodCallback (Ceiling, this));
            SetAttribute ("log", new BuiltinMethodCallback (Log, this));
        }

        [BuiltinDocString (
            "Returns the specified number raised to the specified power.",
            "@param number The number.",
            "@param power The power."
        )]
        IodineObject Pow (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 1) {
                vm.RaiseException (new IodineArgumentException (2));
                return null;
            }

            double a1 = 0;
            double a2 = 0;

            if (!ConvertToDouble (args [0], out a1)) {
                vm.RaiseException (new IodineTypeException ("Float"));
                return null;
            }

            if (!ConvertToDouble (args [1], out a2)) {
                vm.RaiseException (new IodineTypeException ("Float"));
                return null;
            }

            return new IodineFloat (Math.Pow (a1, a2));
        }

        [BuiltinDocString (
            "Returns the sine of the specified number.",
            "@param number The number."
        )]
        IodineObject Sin (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            double input = 0;

            if (!ConvertToDouble (args [0], out input)) {
                vm.RaiseException (new IodineTypeException ("Float"));
                return null;
            }

            return new IodineFloat (Math.Sin (input));
        }

        [BuiltinDocString (
            "Returns the cosine of the specified number.",
            "@param number The number."
        )]
        IodineObject Cos (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            double input = 0;

            if (!ConvertToDouble (args [0], out input)) {
                vm.RaiseException (new IodineTypeException ("Float"));
                return null;
            }
            return new IodineFloat (Math.Cos (input));
        }

        [BuiltinDocString (
            "Returns the tangent of the specified number.",
            "@param number The number."
        )]
        IodineObject Tan (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            double input = 0;

            if (!ConvertToDouble (args [0], out input)) {
                vm.RaiseException (new IodineTypeException ("Float"));
                return null;
            }

            return new IodineFloat (Math.Tan (input));
        }

        [BuiltinDocString (
            "Returns the arc sine of the specified number.",
            "@param number The number."
        )]
        IodineObject ASin (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            double input = 0;

            if (!ConvertToDouble (args [0], out input)) {
                vm.RaiseException (new IodineTypeException ("Float"));
                return null;
            }

            return new IodineFloat (Math.Asin (input));
        }

        [BuiltinDocString (
            "Returns the arc cosine of the specified number.",
            "@param number The number."
        )]
        IodineObject ACos (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            double input = 0;

            if (!ConvertToDouble (args [0], out input)) {
                vm.RaiseException (new IodineTypeException ("Float"));
                return null;
            }

            return new IodineFloat (Math.Acos (input));
        }

        [BuiltinDocString (
            "Returns the arc tangent of the specified number.",
            "@param number The number."
        )]
        IodineObject ATan (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            double input = 0;

            if (!ConvertToDouble (args [0], out input)) {
                vm.RaiseException (new IodineTypeException ("Float"));
                return null;
            }

            return new IodineFloat (Math.Atan (input));
        }

        [BuiltinDocString (
            "Returns the absolute value of the specified number.",
            "@param number The number."
        )]
        IodineObject Abs (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            double input = 0;

            if (!ConvertToDouble (args [0], out input)) {
                vm.RaiseException (new IodineTypeException ("Float"));
                return null;
            }

            return new IodineFloat (Math.Abs (input));
        }

        [BuiltinDocString (
            "Returns the square root of the specified number.",
            "@param number The number."
        )]
        IodineObject Sqrt (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            double input = 0;

            if (!ConvertToDouble (args [0], out input)) {
                vm.RaiseException (new IodineTypeException ("Float"));
                return null;
            }

            return new IodineFloat (Math.Sqrt (input));
        }

        [BuiltinDocString (
            "Returns the specified number, rounded down to the closest integer.",
            "@param number The number."
        )]
        IodineObject Floor (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            double input = 0;

            if (!ConvertToDouble (args [0], out input)) {
                vm.RaiseException (new IodineTypeException ("Float"));
                return null;
            }

            return new IodineFloat (Math.Floor (input));
        }

        [BuiltinDocString (
            "Returns the specified numner, rounded up to the closest integer."
        )]
        IodineObject Ceiling (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            double input = 0;

            if (!ConvertToDouble (args [0], out input)) {
                vm.RaiseException (new IodineTypeException ("Float"));
                return null;
            }

            return new IodineFloat (Math.Ceiling (input));
        }

        [BuiltinDocString (
            "Returns the base 10 logarithm of the specified number.",
            "@param number The number."
        )]
        IodineObject Log (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            double value = 0;
            double numericBase = 10;

            if (!ConvertToDouble (args [0], out value)) {
                vm.RaiseException (new IodineTypeException ("Float"));
                return null;
            }

            if (args.Length > 1 && !ConvertToDouble (args [1], out numericBase)) {
                vm.RaiseException (new IodineTypeException ("Float"));
                return null;
            }

            return new IodineFloat (Math.Log (value, numericBase));
        }

        static bool ConvertToDouble (IodineObject obj, out double value)
        {
            if (obj is IodineInteger) {
                value = (double)((IodineInteger)obj).Value;
                return true;
            } else if (obj is IodineFloat) {
                value = ((IodineFloat)obj).Value;
                return true;
            }
            value = 0;
            return false;
        }
    }
}


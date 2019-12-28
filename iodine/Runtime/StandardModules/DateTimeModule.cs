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
        "Provides methods for retrieving the current date and time"
    )]
    [IodineBuiltinModule ("datetime")]
    public class DateTimeModule : IodineModule
    {
        [BuiltinDocString (
            "Provides information about the current date and time"
        )]
        public class IodineTimeStamp : IodineObject
        {
            public readonly static IodineTypeDefinition TimeStampTypeDef = new IodineTypeDefinition ("TimeStamp");

            public DateTime Value { private set; get; }

            public IodineTimeStamp (DateTime val)
                : base (TimeStampTypeDef)
            {
                Value = val;
                var unixEsposh = (long)(val.Subtract (new DateTime (1970, 1, 1))).TotalSeconds;
                SetAttribute ("millisecond", new IodineInteger (val.Millisecond));
                SetAttribute ("second", new IodineInteger (val.Second));
                SetAttribute ("minute", new IodineInteger (val.Minute));
                SetAttribute ("hour", new IodineInteger (val.Hour));
                SetAttribute ("day", new IodineInteger (val.Day));
                SetAttribute ("month", new IodineInteger (val.Month));
                SetAttribute ("year", new IodineInteger (val.Year));
                SetAttribute ("epoch", new IodineInteger (unixEsposh));
                SetAttribute ("utc", new BuiltinMethodCallback (ToUtc, null));
            }

            IodineObject ToUtc (VirtualMachine vm, IodineObject self, IodineObject [] args)
            {
                return new IodineTimeStamp (Value.ToUniversalTime ());
            }

            public override IodineObject GreaterThan (VirtualMachine vm, IodineObject right)
            {
                var op = right as IodineTimeStamp;
                if (op == null) {
                    vm.RaiseException (new IodineTypeException (
                        "Right hand value expected to be of type TimeStamp"));
                    return null;
                }
                return IodineBool.Create (Value.CompareTo (op.Value) > 0);
            }

            public override IodineObject LessThan (VirtualMachine vm, IodineObject right)
            {
                var op = right as IodineTimeStamp;
                if (op == null) {
                    vm.RaiseException (new IodineTypeException (
                        "Right hand value expected to be of type TimeStamp"));
                    return null;
                }
                return IodineBool.Create (Value.CompareTo (op.Value) < 0);
            }

            public override IodineObject GreaterThanOrEqual (VirtualMachine vm, IodineObject right)
            {
                var op = right as IodineTimeStamp;

                if (op == null) {
                    vm.RaiseException (new IodineTypeException (
                        "Right hand value expected to be of type TimeStamp"));
                    return null;
                }

                return IodineBool.Create (Value.CompareTo (op.Value) >= 0);
            }

            public override IodineObject LessThanOrEqual (VirtualMachine vm, IodineObject right)
            {
                var op = right as IodineTimeStamp;
                if (op == null) {
                    vm.RaiseException (new IodineTypeException (
                        "Right hand value expected to be of type TimeStamp"));
                    return null;
                }
                return IodineBool.Create (Value.CompareTo (op.Value) <= 0);
            }

            public override IodineObject Equals (VirtualMachine vm, IodineObject right)
            {
                var op = right as IodineTimeStamp;
                if (op == null) {
                    vm.RaiseException (new IodineTypeException (
                        "Right hand value expected to be of type TimeStamp"));
                    return null;
                }
                return IodineBool.Create (Value.CompareTo (op.Value) == 0);
            }
        }

        public DateTimeModule ()
            : base ("datetime")
        {
            SetAttribute ("now", new BuiltinMethodCallback (Now, this));
        }

        [BuiltinDocString (
            "Returns the current time"
        )]
        static IodineObject Now (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            return new IodineTimeStamp (DateTime.Now);
        }
    }

}


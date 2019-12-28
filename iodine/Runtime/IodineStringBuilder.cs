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
using System.Text;

namespace Iodine.Runtime
{
    public class IodineStringBuilder : IodineObject
    {
        public readonly static IodineTypeDefinition TypeDefinition = new StringBuilderTypeDef ();

        sealed class StringBuilderTypeDef : IodineTypeDefinition
        {
            public StringBuilderTypeDef ()
                : base ("StringBuffer")
            {
                BindAttributes (this);

                SetDocumentation (
                    "A mutable string of UTF-16 characters"
                );
            }

            public override IodineObject BindAttributes (IodineObject newStrBuf)
            {
                newStrBuf.SetAttribute ("clear", new BuiltinMethodCallback (Clear, newStrBuf));
                newStrBuf.SetAttribute ("append", new BuiltinMethodCallback (Append, newStrBuf));
                newStrBuf.SetAttribute ("prepend", new BuiltinMethodCallback (Prepend, newStrBuf));
                return newStrBuf;

            }

            public override IodineObject Invoke (VirtualMachine vm, IodineObject[] args)
            {
                return new IodineStringBuilder ();
            }

            [BuiltinDocString (
                "Appends each argument to the end of the string buffer.",
                "@param *args Each item to append to the end of the buffer."
            )]
            private IodineObject Append (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineStringBuilder;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                foreach (IodineObject obj in args) {
                    thisObj.Buffer.Append (obj.ToString (vm));
                }
                return null;
            }

            [BuiltinDocString (
                "Prepends text to the beginning of the string buffer.",
                "@param item The item to append."
            )]
            private IodineObject Prepend (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineStringBuilder;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                foreach (IodineObject obj in args) {
                    thisObj.Buffer.Insert (0, obj.ToString (vm));
                }
                return null;
            }

            [BuiltinDocString (
                "Clears the string buffer."
            )]
            private IodineObject Clear (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineStringBuilder; 

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                thisObj.Buffer.Clear ();
                return null;
            }
        }

        public readonly StringBuilder Buffer = new StringBuilder ();

        public IodineStringBuilder ()
            : base (TypeDefinition)
        {
        }

        public override bool Equals (IodineObject obj)
        {
            var strVal = obj as IodineStringBuilder;

            if (strVal != null) {
                return strVal.ToString () == ToString ();
            }

            return false;
        }

        public override string ToString ()
        {
            return Buffer.ToString ();
        }

        public override IodineObject Len (VirtualMachine vm)
        {
            return new IodineInteger (Buffer.Length);
        }

        public override IodineObject ToString (VirtualMachine vm)
        {
            return new IodineString (Buffer.ToString ());
        }

    }
}


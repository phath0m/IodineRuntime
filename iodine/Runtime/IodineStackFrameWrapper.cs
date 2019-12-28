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
    /// <summary>
    /// Iodine stack frame wrapper.
    /// </summary>
    public class IodineStackFrameWrapper : IodineObject
    {
        public readonly static IodineTypeDefinition TypeDefinition = new IodineTypeDefinition ("StackFrame"); 

        public readonly StackFrame StackFrame;

        public IodineStackFrameWrapper (StackFrame frame)
            : base (TypeDefinition)
        {
            StackFrame = frame;
            SetAttribute ("parent", new InternalIodineProperty (GetParentFrame, null));
            SetAttribute ("printtrace", new BuiltinMethodCallback (PrintTrace, this));
            //SetAttribute ("name", frame.Method.Name);
        }

        private IodineObject GetParentFrame (VirtualMachine vm)
        {
            StackFrame frame = StackFrame.Parent;

            if (frame == null) {
                return IodineNull.Instance;
            }

            return new IodineStackFrameWrapper (frame);
        }

        private IodineObject PrintTrace (VirtualMachine vm, IodineObject self, IodineObject[] args)
        {
            StackFrame top = StackFrame;
            Console.WriteLine ("Stack trace:");
            Console.WriteLine ("------------");
            while (top != null) {
                Console.WriteLine (" at {0} (Module: {1}, Line: {2})", top.Method.Name, top.Module.Name,
                    top.Location != null ?
                    top.Location.Line + 1 : 
                    0
                );

                top = top.Parent;
            }
            return null;
        }
    }
}


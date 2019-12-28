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
    /// IodineClosure wraps a method around an existing method's stack frame. This 
    /// enables the child method to access the parents methods local variables
    /// </summary>
    public class IodineClosure : IodineObject
    {
        public static readonly IodineTypeDefinition TypeDefinition = new IodineTypeDefinition ("Closure");

        public readonly IodineMethod Target;

        StackFrame frame;

        public IodineClosure (StackFrame frame, IodineMethod target)
            : base (TypeDefinition)
        {
            this.frame = frame;
            Target = target;
        }

        public override bool IsCallable ()
        {
            return true;
        }

        public override IodineObject Invoke (VirtualMachine vm, IodineObject [] arguments)
        {
            var newFrame = frame.Duplicate (vm.Top);
            var initialValue = vm.InvokeMethod (Target, newFrame, frame.Self, arguments);

            if (newFrame.Yielded) {
                return new IodineGenerator (newFrame, Target, arguments, initialValue);
            }
            return initialValue;
        }

        public override string ToString ()
        {
            return string.Format ("<Closure>");
        }
    }
}


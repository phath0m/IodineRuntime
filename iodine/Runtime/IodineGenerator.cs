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
    /// A generator function which can be iterated
    /// </summary>
    public class IodineGenerator : IodineObject
    {
        static readonly IodineTypeDefinition TypeDefinition = new IodineTypeDefinition ("Generator");

        /*
         * Initial access is set after the first attempt to iterate over this generator
         */
        bool initialAccess = false;

        /*
         * The method that created this generator
         */
        IodineMethod baseMethod;

        /*
         * self pointer if baseMethod is an instance method
         */
        IodineObject self;

        /*
         * Initial value created by the first yield statement, we return this on the
         * first iteration
         */
        IodineObject value;

        /*
         * Arguments originally passed to the method
         */
        IodineObject [] arguments;

        /*
         * The stack frame we are operating from
         */
        StackFrame stackFrame;

        public IodineGenerator (StackFrame frame,
            IodineMethod baseMethod,
            IodineObject[] args, 
            IodineObject initialValue)
            : base (TypeDefinition)
        {
            arguments = args;
            value = initialValue;
            stackFrame = frame;
            this.baseMethod = baseMethod;

            SetAttribute ("__iter__", new BuiltinMethodCallback ((VirtualMachine vm, IodineObject self, IodineObject [] arguments) => {
                return GetIterator (vm);
            }, this));
        }

        public IodineGenerator (StackFrame frame,
            IodineBoundMethod baseMethod,
            IodineObject[] args,
            IodineObject initialValue)
            : base (TypeDefinition)
        {
            arguments = args;
            value = initialValue;
            self = baseMethod.Self;
            stackFrame = frame;
            this.baseMethod = baseMethod.Method;

            SetAttribute ("__iter__", new BuiltinMethodCallback ((VirtualMachine vm, IodineObject self, IodineObject[] arguments) => {
                return GetIterator (vm);
            }, this));
        }

        public override IodineObject GetIterator (VirtualMachine vm)
        {
            return this;
        }

        public override bool IterMoveNext (VirtualMachine vm)
        {
            if (stackFrame.AbortExecution) {
                return false;
            }

            if (initialAccess) {
                value = vm.InvokeMethod (baseMethod, stackFrame, self, arguments);

                return stackFrame.Yielded;
            } else {
                initialAccess = true;
                return true;
            }
        }

        public override IodineObject IterGetCurrent (VirtualMachine vm)
        {
            stackFrame.Yielded = false;
            return value;
        }

        public override void IterReset (VirtualMachine vm)
        {
        }
    }
}


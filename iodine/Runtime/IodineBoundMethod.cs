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
    /// An IodineBound method represents an IodineMethod that has been "bound" to an
    /// object. When invoked, this will provide the "bound" method with the self 
    /// reference needed to access any instance types
    /// </summary>
    public class IodineBoundMethod : IodineObject
    {
        private static readonly IodineTypeDefinition InstanceTypeDef = new IodineTypeDefinition ("BoundMethod");

        /// <summary>
        /// The method that has been "bound" to a new a self reference
        /// </summary>
        public readonly IodineMethod Method;

        /// <summary>
        /// The self reference which will be provided to the "bound" method
        /// </summary>
        public IodineObject Self {
            private set;
            get;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Iodine.Runtime.IodineBoundMethod"/> class.
        /// </summary>
        /// <param name="self">Self.</param>
        /// <param name="method">Method.</param>
        public IodineBoundMethod (IodineObject self, IodineMethod method)
            : base (InstanceTypeDef)
        {
            Method = method;
            SetAttribute ("__name__", method.Attributes ["__name__"]);
            SetAttribute ("__doc__", method.Attributes ["__doc__"]);
            SetAttribute ("__invoke__", method.Attributes ["__invoke__"]);
            Self = self;
        }

        /// <summary>
        /// Rebinds this method with a new self reference
        /// </summary>
        /// <param name="newSelf">New self reference</param>
        public void Bind (IodineObject newSelf)
        {
            Self = newSelf;
        }

        public override bool IsCallable ()
        {
            return true;
        }

        /// <summary>
        /// Invoke the specified vm and arguments.
        /// </summary>
        /// <param name="vm">Vm.</param>
        /// <param name="arguments">Arguments.</param>
        public override IodineObject Invoke (VirtualMachine vm, IodineObject[] arguments)
        {
            var frame = new StackFrame (
                vm,
                Method.Module, 
                Method,
                vm.Top == null ? new IodineObject[]{} : vm.Top.Arguments,
                vm.Top,
                Self
            );

            var initialValue = vm.InvokeMethod (Method, frame, Self, arguments);

            if (frame.Yielded) {
                return new IodineGenerator (frame, this, arguments, initialValue);
            }
            return initialValue;
        }

        public override string ToString ()
        {
            return string.Format ("<Bound {0}>", Method.Name);
        }
    }
}


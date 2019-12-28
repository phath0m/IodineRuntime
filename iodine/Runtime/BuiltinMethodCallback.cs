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
using System.Threading;
using Iodine.Compiler;

namespace Iodine.Runtime
{
    public delegate IodineObject IodineMethodDelegate (VirtualMachine vm,
        IodineObject self,
        IodineObject[] arguments
    );

    /// <summary>
    /// Represents a C# method that can be called in Iodine
    /// </summary>
    public class BuiltinMethodCallback : IodineObject
    {
        static readonly IodineTypeDefinition InternalMethodTypeDef = new IodineTypeDefinition ("Builtin");

        public IodineObject Self {
            get;
            internal set;
        }

        public IodineMethodDelegate Callback {
            private set;
            get;
        }

        public BuiltinMethodCallback (IodineMethodDelegate callback, IodineObject self, bool setInvokeAttribute = true)
            : base (InternalMethodTypeDef)
        {
            Self = self;
            Callback = callback;
            var attributes = callback.GetInvocationList() [0].Method.GetCustomAttributes (false);

            foreach (object attr in attributes) {
                if (attr is BuiltinDocString) {
                    BuiltinDocString docstr = attr as BuiltinDocString;
                    SetAttribute ("__doc__", new InternalIodineProperty (vm => {
                        return new IodineString (docstr.DocumentationString);
                    }, null));
                }
            }

            // This is needed to prevent a stackoverflow
            if (setInvokeAttribute) {
                
                // Set the invoke attribute so traits can match __invoke__
                SetAttribute ("__invoke__", new BuiltinMethodCallback (Invoke, this, false));
            }
        }

        public override bool IsCallable ()
        {
            return true;
        }

        IodineObject Invoke (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            return Invoke (vm, args);
        }

        public override IodineObject Invoke (VirtualMachine vm, IodineObject[] arguments)
        {
            try {
                return Callback.Invoke (vm, Self, arguments);
            } catch (RuntimeException ex) {
                throw ex;
            } catch (SyntaxException ex) {
                throw ex;
            } catch (UnhandledIodineExceptionException e) {
                throw e;
            } catch (ThreadAbortException) {
            } catch (Exception ex) {
                vm.RaiseException (new IodineInternalErrorException (ex));
            }
            return null;
            
        }
    }
}


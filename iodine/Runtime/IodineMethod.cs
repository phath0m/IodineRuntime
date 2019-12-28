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
using System.Linq;
using System.Collections.Generic;

namespace Iodine.Runtime
{
    [Flags]
    public enum MethodFlags
    {
        AcceptsVarArgs = 0x01,
        AcceptsKwargs = 0x02,
        HasDefaultParameters = 0x04,
        HasTypedParameters = 0x08
    }

    /// <summary>
    /// Abstract class representing an IodineMethod containing Iodine bytecode. This 
    /// is the only class that is directly invokable by the virtual machine
    /// </summary>
    public class IodineMethod : IodineObject
    {
        static readonly IodineTypeDefinition MethodTypeDef = new IodineTypeDefinition ("Method");

        public readonly CodeObject Bytecode;

        string name;

        /// <summary>
        /// The name of the method
        /// </summary>
        /// <value>The name.</value>
        public string Name {
            get {
                return name;
            }
            protected set {
                name = value;
                SetAttribute ("__name__", new IodineString (value));
            }
        }

        /// <summary>
        /// How many parameters the method can receive
        /// </summary>
        /// <value>The parameter count.</value>
        public readonly int ParameterCount;

        /// <summary>
        /// Does this method accept variable arguments?
        /// </summary>
        /// <value><c>true</c> if variadic; otherwise, <c>false</c>.</value>
        public readonly bool Variadic;

        /// <summary>
        /// Does this method accept keyword arguments?
        /// </summary>
        /// <value><c>true</c> if accepts keyword arguments; otherwise, <c>false</c>.</value>
        public readonly bool AcceptsKeywordArgs;

        /// <summary>
        /// Does this method have default values
        /// </summary>
        public readonly bool HasDefaultValues;

        public readonly int DefaultValuesStartIndex;

        /// <summary>
        /// Does this method have a chance of yielding to the caller?
        /// </summary>
        /// <value><c>true</c> if generator; otherwise, <c>false</c>.</value>
        public readonly bool Generator;

        /// <summary>
        /// Module in which this method was defined in
        /// </summary>
        /// <value>The module.</value>
        public readonly IodineModule Module;


        /// <summary>
        /// Maps each parameter to a local variable index, used by the virtual machine
        /// </summary>
        public readonly List<IodineParameter> Parameters = new List<IodineParameter> ();
        public readonly IodineObject[] DefaultValues;

        public readonly string VarargsParameter;
        public readonly string KwargsParameter;

        public IodineMethod (
            IodineModule module,
            IodineString name,
            CodeObject bytecode,
            IodineTuple parameters,
            MethodFlags flags,
            IodineObject[] defaultValues,
            int defaultStart = 0
        )
            : base (MethodTypeDef)
        {
            Module = module;
            Bytecode = bytecode;
            ParameterCount = Parameters.Count;
            Variadic = (flags & MethodFlags.AcceptsVarArgs) != 0;
            AcceptsKeywordArgs = (flags & MethodFlags.AcceptsKwargs) != 0;
            HasDefaultValues = (flags & MethodFlags.HasDefaultParameters) != 0;
            DefaultValuesStartIndex = defaultStart;
            DefaultValues = defaultValues;
            SetParameters (Parameters, parameters);

            Name = name.ToString ();

            SetAttribute ("__doc__", IodineString.Empty);
            SetAttribute ("__invoke__", new BuiltinMethodCallback (invoke, this));

            if (AcceptsKeywordArgs) {

                var lastParameter = Parameters.Last () as IodineNamedParameter;

                KwargsParameter = lastParameter.Name;

                if (Variadic) {
                    var secondToLastParameter = Parameters [Parameters.Count - 2] as
                                                          IodineNamedParameter;

                    VarargsParameter = secondToLastParameter.Name;
                }
            } else if (Variadic) {
                var lastParameter = Parameters.Last () as IodineNamedParameter;
                VarargsParameter = lastParameter.Name;
            }
        }


        void SetParameters (List<IodineParameter> paramList, IodineTuple tuple)
        {
            foreach (IodineObject obj in tuple.Objects) {
                var strObj = obj as IodineName;

                if (strObj != null) {
                    paramList.Add (new IodineNamedParameter (strObj.Value));
                    continue;
                }

                var tupleObj = obj as IodineTuple;

                if (tupleObj != null) {
                    var deconstructionList = new List<IodineParameter> ();

                    SetParameters (deconstructionList, tupleObj);

                    paramList.Add (new IodineTupleParameter (deconstructionList));
                }

            }
        }

        /// <summary>
        /// A small wrapper around IodineObject.Invoke
        /// </summary>
        /// <param name="vm">Vm.</param>
        /// <param name="self">Self.</param>
        /// <param name="args">Arguments.</param>
        IodineObject invoke (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            return Invoke (vm, args);
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
            /*
             * If this method happens to be a generator method (Which just means it has
             * a yield statement in it), we will attempt to invoke it in the VM and check
             * if the method yielded or not. If the method did yield, we must return a 
             * generator so the caller can iterate over any other items which this method
             * may yield. If the method did not yield, we just return the original value
             * returned
             */

            var previousArguments = vm.Top?.Arguments ?? null;

            var frame = new StackFrame (vm, Module, this, previousArguments, vm.Top, null);


            var initialValue = vm.InvokeMethod (this, frame, null, arguments);

            if (frame.Yielded) {
                return new IodineGenerator (frame, this, arguments, initialValue);
            }
            return initialValue;
        }

        public override string ToString ()
        {
            return string.Format ("<Function {0}>", name);
        }
    }
}

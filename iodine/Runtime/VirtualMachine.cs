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
using System.Threading;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Iodine.Util;
using Iodine.Compiler;

namespace Iodine.Runtime
{
    // Callback for debugger
    public delegate bool TraceCallback (TraceType type,
        VirtualMachine vm,
        StackFrame frame,
        SourceLocation location
    );

    public enum TraceType
    {
        Line,
        Exception,
        Function
    }

    /// <summary>
    /// Represents an instance of an Iodine virtual machine. Each Iodine thread gets its own
    /// instance of this class
    /// </summary>
    public sealed class VirtualMachine
    {
        public static readonly Dictionary<string, IodineModule> ModuleCache = new Dictionary<string, IodineModule> ();

        public readonly IodineContext Context;

        internal Instruction CurrentInstruction {
            get {
                return instruction;
            }
        }

        int frameCount = 0;
        int stackSize = 0;

        TraceCallback traceCallback = null;
        IodineObject lastException = null;

        Instruction instruction;
        LinkedStack<StackFrame> frames = new LinkedStack<StackFrame> ();
        ManualResetEvent pauseVirtualMachine = new ManualResetEvent (true);

        public StackFrame Top;

        public VirtualMachine (IodineContext context)
        {
            Context = context;
            context.ResolveModule += (name) => {
                if (BuiltInModules.Modules.ContainsKey (name)) {
                    return BuiltInModules.Modules [name];
                }
                return null;
            };
        }

        /// <summary>
        /// Returns a string representing the current stack tracee
        /// </summary>
        /// <returns>The stack trace.</returns>
        public string GetStackTrace ()
        {
            var accum = new StringBuilder ();

            StackFrame top = Top;

            while (top != null) {
                accum.AppendFormat (" at {0} (Module: {1}, Line: {2})\n",
                    top.Method != null ? top.Method.Name : "",
                    top.Module.Name,
                    top.Location.Line + 1
                );

                top = top.Parent;
            }

            return accum.ToString ();
        }

        /// <summary>
        /// Resumes execution
        /// </summary>
        public void ContinueExecution ()
        {
            pauseVirtualMachine.Set ();
        }

        /// <summary>
        /// Executes an Iodine method
        /// </summary>
        /// <returns>Value evaluated on return (null if void).</returns>
        /// <param name="method">Method.</param>
        /// <param name="self">self pointer.</param>
        /// <param name="arguments">Arguments.</param>
        public IodineObject InvokeMethod (IodineMethod method, IodineObject self, IodineObject [] arguments)
        {
            int requiredArgs = method.AcceptsKeywordArgs ? method.ParameterCount - 1 : method.ParameterCount;
            if ((method.Variadic && arguments.Length + 1 < requiredArgs) ||
                (!method.Variadic && arguments.Length < requiredArgs)) {
                RaiseException (new IodineArgumentException (method.ParameterCount));
                return null;
            }

            NewFrame (method, arguments, self);

            return Invoke (method, arguments);
        }

        /// <summary>
        /// Executes an Iodine method using a preallocated stack frame. this is used for 
        /// closures 
        /// </summary>
        /// <returns>The method.</returns>
        /// <param name="method">Method.</param>
        /// <param name="frame">Frame.</param>
        /// <param name="self">Self.</param>
        /// <param name="arguments">Arguments.</param>
        public IodineObject InvokeMethod (IodineMethod method,
            StackFrame frame,
            IodineObject self,
            IodineObject [] arguments)
        {
            int requiredArgs = method.AcceptsKeywordArgs ? method.ParameterCount - 1
                : method.ParameterCount;

            requiredArgs -= method.DefaultValues.Length;

            if ((method.Variadic && arguments.Length + 1 < requiredArgs) ||
                (!method.Variadic && arguments.Length < requiredArgs)) {
                RaiseException (new IodineArgumentException (method.ParameterCount));
                return null;
            }

            NewFrame (frame);

            return Invoke (method, arguments);
        }

        /*
         * Internal implementation of Invoke
         */
        IodineObject Invoke (IodineMethod method, IodineObject [] arguments)
        {
            if (method.Bytecode.Instructions.Length > 0) {
                instruction = method.Bytecode.Instructions [0];
            }
            int insCount = method.Bytecode.Instructions.Length;
            int prevStackSize = stackSize;
            int i = 0;

            /*
             * Store function arguments into their respective local variable slots
             */
            foreach (IodineParameter param in method.Parameters) {

                var namedParam = param as IodineNamedParameter;

                if (namedParam != null) {
                    StoreNamedParameter (method, arguments, namedParam, i);
                }

                var tupleParam = param as IodineTupleParameter;

                if (tupleParam != null) {

                    var tuple = arguments [i] as IodineTuple;

                    if (tuple == null) {
                        RaiseException (new IodineException ("Tuple"));
                        return null;
                    }

                    DecomposeTupleParameter (tuple, tupleParam);
                }

                i++;
            }

            StackFrame top = Top;

            top.Module = method.Module;

            if (traceCallback != null) {
                Trace (TraceType.Function, top, instruction.Location);
            }

            var retVal = EvalCode (method.Bytecode);

            if (top.Yielded) {
                top.Pop ();
            }

            /*
             * Calls __exit__ on any object used in a with statement
             */
            while (!top.Yielded && top.DisposableObjects.Count > 0) {
                top.DisposableObjects.Pop ().Exit (this);
            }

            stackSize = prevStackSize;

            if (top.AbortExecution) {
                /*
                 * If AbortExecution was set, something went wrong and we most likely just
                 * raised an exception. We'll return right here and let what ever catches 
                 * the exception clean up the stack
                 */
                return retVal;
            }

            EndFrame ();

            return retVal;
        }

        void StoreNamedParameter (IodineMethod method,
                                  IodineObject [] arguments,
                                  IodineNamedParameter param,
                                  int paramIndex)
        {
            if (param.Name == method.VarargsParameter) {
                // Variable list arguments
                IodineObject [] tupleItems = new IodineObject [arguments.Length - paramIndex];
                Array.Copy (arguments, paramIndex, tupleItems, 0, arguments.Length - paramIndex);
                Top.StoreLocalExplicit (param.Name, new IodineTuple (tupleItems));

            } else if (param.Name == method.KwargsParameter) {
                /*
                 * At the moment, keyword arguments are passed to the function as an IodineHashMap,
                 */
                if (paramIndex < arguments.Length && arguments [paramIndex] is IodineDictionary) {
                    Top.StoreLocalExplicit (param.Name, arguments [paramIndex]);
                } else {
                    Top.StoreLocalExplicit (param.Name, new IodineDictionary ());
                }
            } else {
                if (arguments.Length <= paramIndex && method.HasDefaultValues) {
                    Top.StoreLocalExplicit (param.Name, method.DefaultValues [paramIndex - method.DefaultValuesStartIndex]);
                } else {
                    Top.StoreLocalExplicit (param.Name, arguments [paramIndex++]);
                }
            }
        }

        void DecomposeTupleParameter (IodineTuple tuple, IodineTupleParameter param)
        {
            int index = 0;

            foreach (IodineParameter subparam in param.ElementNames) {
                var namedParam = subparam as IodineNamedParameter;

                if (namedParam != null) {
                    Top.StoreLocalExplicit (namedParam.Name, tuple.Objects [index]);
                }

                var tupleParam = subparam as IodineTupleParameter;

                if (tupleParam != null) {
                    var tupleObj = tuple.Objects [index] as IodineTuple;

                    DecomposeTupleParameter (tupleObj, tupleParam);
                }

                index++;
            }
        }

        /// <summary>
        /// Evaluates an Iodine code object
        /// </summary>
        /// <returns>The code.</returns>
        /// <param name="bytecode">Bytecode.</param>
        public IodineObject EvalCode (CodeObject bytecode)
        {
            int insCount = bytecode.Instructions.Length;

            int pc = Top.InstructionPointer;

            StackFrame top = Top;
            IodineObject selfReference = null;

            top.SetLocationAccessor (() => {
                return instruction.Location;
            });

            top.SetInstructionPointerAccessor (
                () => { return pc; },
                (newIp) => { pc = newIp; }
            );

            while (pc < insCount && !top.AbortExecution && !top.Yielded) {
                instruction = bytecode.Instructions [pc++];


                switch (instruction.OperationCode) {
                case Opcode.Pop: {
                        top.Pop ();
                        break;
                    }
                case Opcode.Dup: {
                        var val = top.Pop ();
                        top.Push (val);
                        top.Push (val);
                        break;
                    }
                case Opcode.LoadConst: {
                        top.Push (instruction.ArgumentObject);
                        break;
                    }
                case Opcode.LoadNull: {
                        top.Push (IodineNull.Instance);
                        break;
                    }
                case Opcode.LoadSelf: {
                        top.Push (Top.Self);

                        if (Top.Self == null) {
                            RaiseException (new IodineFunctionInvocationException ());
                        }

                        break;
                    }
                case Opcode.LoadTrue: {
                        top.Push (IodineBool.True);
                        break;
                    }
                case Opcode.LoadException: {
                        top.Push (lastException);
                        break;
                    }
                case Opcode.LoadFalse: {
                        top.Push (IodineBool.False);
                        break;
                    }
                case Opcode.StoreLocal: {
                        Top.StoreLocal (instruction.ArgumentString, top.Pop ());
                        break;
                    }
                case Opcode.LoadLocal: {
                        top.Push (Top.LoadLocal (instruction.ArgumentString));
                        break;
                    }
                case Opcode.StoreGlobal: {
                        Top.Module.SetAttribute (this, instruction.ArgumentString, top.Pop ());
                        break;
                    }
                case Opcode.LoadGlobal: {
                        if (instruction.ArgumentString == "_") {
                            top.Push (Top.Module);
                        } else if (Top.Module.Attributes.ContainsKey (instruction.ArgumentString)) {
                            top.Push (Top.Module.GetAttribute (this, instruction.ArgumentString));
                        } else {
                            RaiseException (new IodineAttributeNotFoundException (instruction.ArgumentString));
                        }
                        break;
                    }
                case Opcode.StoreAttribute: {
                        var target = top.Pop ();
                        var value = top.Pop ();

                        string attribute = instruction.ArgumentString;

                        if (target.Attributes.ContainsKey (attribute) &&
                            target.Attributes [attribute] is IIodineProperty) {
                            var property = (IIodineProperty)target.Attributes [attribute];
                            property.Set (this, value);
                            break;
                        }
                        target.SetAttribute (this, attribute, value);
                        break;
                    }
                case Opcode.LoadAttribute: {
                        var target = top.Pop ();
                        string attribute = instruction.ArgumentString;
                        if (target.Attributes.ContainsKey (attribute) &&
                            target.Attributes [attribute] is IIodineProperty) {
                            var property = (IIodineProperty)target.Attributes [attribute];
                            top.Push (property.Get (this));
                            selfReference = target;
                            break;
                        }
                        top.Push (target.GetAttribute (this, attribute));
                        selfReference = target;
                        break;
                    }
                case Opcode.LoadAttributeOrNull: {
                        var target = top.Pop ();
                        string attribute = instruction.ArgumentString;

                        if (target.Attributes.ContainsKey (attribute)) {
                            top.Push (target.GetAttribute (this, attribute));
                        } else {
                            top.Push (IodineNull.Instance);
                        }
                        selfReference = top.Stack.LastObject;
                        break;
                    }
                case Opcode.StoreIndex: {
                        var index = top.Pop ();
                        var target = top.Pop ();
                        var value = top.Pop ();
                        target.SetIndex (this, index, value);
                        break;
                    }
                case Opcode.LoadIndex: {
                        var index = top.Pop ();
                        var target = top.Pop ();
                        top.Push (target.GetIndex (this, index));
                        break;
                    }
                case Opcode.CastLocal: {
                        var type = top.Pop () as IodineTypeDefinition;

                        var o = Top.LoadLocal (instruction.ArgumentString);

                        if (type == null) {
                            RaiseException (new IodineTypeException ("TypeDef"));
                            break;
                        }
                        if (o.InstanceOf (type)) {
                            top.Push (o);
                        } else {
                            RaiseException (new IodineTypeException (type.Name));
                        }
                        break;
                    }
                case Opcode.Equals: {
                        top.Push (top.Pop ().Equals (this, top.Pop ()));
                        break;
                    }
                case Opcode.NotEquals: {
                        top.Push (top.Pop ().NotEquals (this, top.Pop ()));
                        break;
                    }
                case Opcode.BoolAnd: {
                        var left = top.Pop ();
                        var right = top.Pop ();

                        top.Push (left.LogicalAnd (this, right));

                        break;
                    }
                case Opcode.BoolOr: {
                        var left = top.Pop ();
                        var right = top.Pop ();

                        top.Push (left.LogicalOr (this, right));

                        break;
                    }
                case Opcode.Add: {
                        top.Push (top.Pop ().Add (this, top.Pop ()));
                        break;
                    }
                case Opcode.Sub: {
                        top.Push (top.Pop ().Sub (this, top.Pop ()));
                        break;
                    }
                case Opcode.Mul: {
                        top.Push (top.Pop ().Mul (this, top.Pop ()));
                        break;
                    }
                case Opcode.Div: {
                        top.Push (top.Pop ().Div (this, top.Pop ()));
                        break;
                    }
                case Opcode.Mod: {
                        top.Push (top.Pop ().Mod (this, top.Pop ()));
                        break;
                    }
                case Opcode.Xor: {
                        top.Push (top.Pop ().Xor (this, top.Pop ()));
                        break;
                    }
                case Opcode.And: {
                        top.Push (top.Pop ().And (this, top.Pop ()));
                        break;
                    }
                case Opcode.Or: {
                        top.Push (top.Pop ().Or (this, top.Pop ()));
                        break;
                    }
                case Opcode.LeftShift: {
                        top.Push (top.Pop ().LeftShift (this, top.Pop ()));
                        break;
                    }
                case Opcode.RightShift: {
                        top.Push (top.Pop ().RightShift (this, top.Pop ()));
                        break;
                    }
                case Opcode.GreaterThan: {
                        top.Push (top.Pop ().GreaterThan (this, top.Pop ()));
                        break;
                    }
                case Opcode.GreaterThanOrEqu: {
                        top.Push (top.Pop ().GreaterThanOrEqual (this, top.Pop ()));
                        break;
                    }
                case Opcode.LessThan: {
                        top.Push (top.Pop ().LessThan (this, top.Pop ()));
                        break;
                    }
                case Opcode.LessThanOrEqu: {
                        top.Push (top.Pop ().LessThanOrEqual (this, top.Pop ()));
                        break;
                    }
                case Opcode.HalfRange: {
                        top.Push (top.Pop ().HalfRange (this, top.Pop ()));
                        break;
                    }
                case Opcode.ClosedRange: {
                        top.Push (top.Pop ().ClosedRange (this, top.Pop ()));
                        break;
                    }
                case Opcode.UnaryOp: {
                        top.Push (top.Pop ().PerformUnaryOperation (this,
                            (UnaryOperation)instruction.Argument));
                        break;
                    }
                case Opcode.Invoke: {
                        var target = top.Pop ();
                        var arguments = new IodineObject [instruction.Argument];
                        for (int i = 1; i <= instruction.Argument; i++) {
                            arguments [instruction.Argument - i] = top.Pop ();
                        }
                        top.Push (target.Invoke (this, arguments));
                        break;
                    }
                case Opcode.InvokeVar: {
                        var target = top.Pop ();
                        var arguments = new List<IodineObject> ();
                        var tuple = top.Pop () as IodineTuple;
                        if (tuple == null) {
                            RaiseException (new IodineTypeException ("Tuple"));
                            break;
                        }
                        for (int i = 0; i < instruction.Argument; i++) {
                            arguments.Add (top.Pop ());
                        }
                        arguments.AddRange (tuple.Objects);
                        top.Push (target.Invoke (this, arguments.ToArray ()));
                        break;
                    }
                case Opcode.InvokeSuper: {
                        var target = top.Pop () as IodineTypeDefinition;
                        var arguments = new IodineObject [instruction.Argument];

                        for (int i = 1; i <= instruction.Argument; i++) {
                            arguments [instruction.Argument - i] = top.Pop ();
                        }

                        target.Inherit (this, Top.Self, arguments);
                        break;
                    }
                case Opcode.Return: {
                        pc = int.MaxValue;
                        break;
                    }
                case Opcode.Yield: {
                        Top.Yielded = true;
                        break;
                    }
                case Opcode.JumpIfTrue: {
                        if (top.Pop ().IsTrue ()) {
                            pc = instruction.Argument;
                        }
                        break;
                    }
                case Opcode.JumpIfFalse: {
                        if (!top.Pop ().IsTrue ()) {
                            pc = instruction.Argument;
                        }
                        break;
                    }
                case Opcode.Jump: {
                        pc = instruction.Argument;
                        break;
                    }
                case Opcode.BuildClass: {
                        var name = top.Pop () as IodineName;
                        var doc = top.Pop () as IodineString;
                        var constructor = top.Pop () as IodineMethod;
                        //CodeObject initializer = Pop as CodeObject;
                        var baseClass = top.Pop () as IodineTypeDefinition;
                        var interfaces = top.Pop () as IodineTuple;
                        var clazz = new IodineClass (name.ToString (), new CodeObject (), constructor);

                        if (baseClass != null) {
                            clazz.BaseClass = baseClass;
                            baseClass.BindAttributes (clazz);
                        }

                        for (int i = 0; i < instruction.Argument; i++) {
                            var val = top.Pop ();
                            var key = top.Pop ();

                            clazz.Attributes [val.ToString ()] = key;
                        }

                        foreach (IodineObject obj in interfaces.Objects) {
                            var contract = obj as IodineContract;
                            if (!contract.InstanceOf (clazz)) {
                                //RaiseException (new IodineTypeException (contract.Name));
                                break;
                            }
                        }

                        clazz.SetAttribute ("__doc__", doc);

                        top.Push (clazz);
                        break;
                    }
                case Opcode.BuildMixin: {
                        var name = top.Pop () as IodineName;

                        var mixin = new IodineMixin (name.ToString ());

                        for (int i = 0; i < instruction.Argument; i++) {
                            var val = top.Pop ();
                            var key = top.Pop ();

                            mixin.Attributes [val.ToString ()] = key;
                        }

                        top.Push (mixin);
                        break;
                    }
                case Opcode.BuildEnum: {
                        var name = top.Pop () as IodineName;
                        var ienum = new IodineEnum (name.ToString ());

                        for (int i = 0; i < instruction.Argument; i++) {
                            var val = top.Pop () as IodineInteger;
                            var key = top.Pop () as IodineName;
                            ienum.AddItem (key.ToString (), (int)val.Value);
                        }

                        top.Push (ienum);
                        break;
                    }
                case Opcode.BuildContract: {
                        var name = top.Pop () as IodineName;

                        var contract = new IodineContract (name.ToString ());
                        for (int i = 0; i < instruction.Argument; i++) {
                            var val = top.Pop () as IodineMethod;
                            contract.AddMethod (val);
                        }

                        top.Push (contract);
                        break;
                    }
                case Opcode.BuildTrait: {
                        var name = top.Pop () as IodineName;
                        var trait = new IodineTrait (name.ToString ());

                        for (int i = 0; i < instruction.Argument; i++) {
                            var val = top.Pop () as IodineMethod;
                            trait.AddMethod (val);
                        }

                        top.Push (trait);
                        break;
                    }
                case Opcode.BuildHash: {
                        var hash = new IodineDictionary ();

                        for (int i = 0; i < instruction.Argument; i++) {
                            var val = top.Pop ();
                            var key = top.Pop ();
                            hash.Set (key, val);
                        }
                        top.Push (hash);
                        break;
                    }
                case Opcode.BuildList: {
                        var items = new IodineObject [instruction.Argument];

                        for (int i = 1; i <= instruction.Argument; i++) {
                            items [instruction.Argument - i] = top.Pop ();
                        }

                        top.Push (new IodineList (items));
                        break;
                    }
                case Opcode.BuildTuple: {
                        var items = new IodineObject [instruction.Argument];
                        for (int i = 1; i <= instruction.Argument; i++) {
                            items [instruction.Argument - i] = top.Pop ();
                        }
                        top.Push (new IodineTuple (items));
                        break;
                    }
                case Opcode.BuildClosure: {
                        var obj = top.Pop ();
                        var method = obj as IodineMethod;
                        top.Push (new IodineClosure (Top, method));
                        break;
                    }

                case Opcode.BuildGenExpr: {
                        var method = top.Pop () as CodeObject;
                        top.Push (new IodineGeneratorExpr (Top, method));
                        break;
                    }
                case Opcode.BuildRegex: {
                        var str = top.Pop () as IodineString;
                        top.Push (new RegexModule.IodineRegex (str.Value));
                        break;
                    }
                case Opcode.Slice: {
                        var target = top.Pop ();

                        var arguments = new IodineInteger [3];

                        for (int i = 0; i < 3; i++) {
                            var obj = top.Pop ();
                            arguments [i] = obj as IodineInteger;

                            if (obj != IodineNull.Instance && arguments [i] == null) {
                                RaiseException (new IodineTypeException ("Int"));
                                break;
                            }
                        }

                        var slice = new IodineSlice (arguments [0], arguments [1], arguments [2]);

                        top.Push (target.Slice (this, slice));

                        break;
                    }
                case Opcode.RangeCheck: {
                        var range = top.Pop () as IodineRange;
                        var matchee = top.Pop ();


                        long longVal;


                        if (!MarshalUtil.MarshalAsInt64 (matchee, out longVal) ||
                            range == null) {
                            top.Stack.Push (IodineBool.False);
                            break;
                        }

                        top.Stack.Push (IodineBool.Create (
                            range.LowerBound <= longVal &&
                            range.UpperBound >= longVal
                        ));

                        break;
                    }
                case Opcode.MatchPattern: {
                        var collection = top.Pop ().GetIterator (this);

                        var items = new IodineObject [instruction.Argument];
                        for (int i = 1; i <= instruction.Argument; i++) {
                            items [instruction.Argument - i] = top.Pop ();
                        }


                        int index = 0;

                        collection.IterReset (this);

                        while (collection.IterMoveNext (this) && index < items.Length) {

                            var o = collection.IterGetCurrent (this);

                            if (items [index] is IodineTypeDefinition) {
                                if (!o.InstanceOf (items [index] as IodineTypeDefinition)) {
                                    top.Push (IodineBool.False);
                                    break;
                                }
                            } else if (items [index] is IodineRange) {
                                var range = items [index] as IodineRange;

                                long longValue;

                                if (MarshalUtil.MarshalAsInt64 (o, out longValue)) {
                                    if (longValue > range.UpperBound ||
                                        longValue < range.LowerBound) {

                                        top.Push (IodineBool.False);
                                        break;
                                    }
                                } else {
                                    top.Push (IodineBool.False);
                                    break;
                                }

                            } else {
                                if (!o.Equals (items [index])) {
                                    top.Push (IodineBool.False);
                                    break;
                                }
                            }

                            index++;
                        }

                        top.Push (IodineBool.Create (index == items.Length));

                        break;
                    }
                case Opcode.Unwrap: {
                        var container = top.Pop ();

                        var value = container.Unwrap (this);

                        if (instruction.Argument > 0) {
                            var len = value.Len (this) as IodineInteger;

                            if (len == null || len.Value != instruction.Argument) {
                                top.Push (IodineBool.False);
                                break;
                            }
                        }

                        top.Push (value);
                        top.Push (IodineBool.True);

                        break;
                    }
                case Opcode.Unpack: {
                        var tuple = top.Pop () as IodineTuple;

                        if (tuple == null) {
                            RaiseException (new IodineTypeException ("Tuple"));
                            break;
                        }

                        if (tuple.Objects.Length != instruction.Argument) {
                            RaiseException (new IodineUnpackException (instruction.Argument));
                            break;
                        }
                        for (int i = tuple.Objects.Length - 1; i >= 0; i--) {
                            top.Push (tuple.Objects [i]);
                        }
                        break;
                    }
                case Opcode.GetIter: {
                        top.Push (top.Pop ().GetIterator (this));
                        break;
                    }
                case Opcode.IterGetNext: {
                        top.Push (top.Pop ().IterGetCurrent (this));
                        break;
                    }
                case Opcode.IterMoveNext: {
                        top.Push (IodineBool.Create (top.Pop ().IterMoveNext (this)));
                        break;
                    }
                case Opcode.IterReset: {
                        top.Pop ().IterReset (this);
                        break;
                    }
                case Opcode.PushExceptionHandler: {
                        Top.ExceptionHandlers.Push (new IodineExceptionHandler (frameCount, instruction.Argument));
                        break;
                    }
                case Opcode.PopExceptionHandler: {
                        Top.ExceptionHandlers.Pop ();
                        break;
                    }
                case Opcode.InstanceOf: {
                        var o = top.Pop ();
                        var type = top.Pop () as IodineTypeDefinition;
                        if (type == null) {
                            RaiseException (new IodineTypeException ("TypeDef"));
                            break;
                        }
                        top.Push (IodineBool.Create (o.InstanceOf (type)));
                        break;
                    }
                case Opcode.DynamicCast: {
                        var o = top.Pop ();
                        var type = top.Pop () as IodineTypeDefinition;
                        if (type == null) {
                            RaiseException (new IodineTypeException ("TypeDef"));
                            break;
                        }
                        if (o.InstanceOf (type)) {
                            top.Push (o);
                        } else {
                            top.Push (IodineNull.Instance);
                        }
                        break;
                    }
                case Opcode.NullCoalesce: {
                        var o1 = top.Pop ();
                        var o2 = top.Pop ();
                        if (o1 is IodineNull) {
                            top.Push (o2);
                        } else {
                            top.Push (o1);
                        }
                        break;
                    }
                case Opcode.BeginExcept: {
                        bool rethrow = true;

                        for (int i = 1; i <= instruction.Argument; i++) {
                            var type = top.Pop () as IodineTypeDefinition;

                            if (type == null) {
                                RaiseException (new IodineTypeException ("TypeDef"));
                                break;
                            }

                            if (lastException.InstanceOf (type)) {
                                rethrow = false;
                                break;
                            }
                        }

                        if (rethrow) {
                            RaiseException (lastException);
                        }
                        break;
                    }
                case Opcode.Raise: {
                        var e = top.Pop ();
                        if (e.InstanceOf (IodineException.TypeDefinition)) {
                            RaiseException (e);
                        } else {
                            RaiseException (new IodineTypeException ("Exception"));
                        }
                        break;
                    }
                case Opcode.SwitchLookup: {
                        var lookup = new Dictionary<int, IodineObject> ();
                        var needle = top.Pop ().GetHashCode ();

                        for (int i = 0; i < instruction.Argument; i++) {
                            var value = top.Pop ();
                            var key = top.Pop ();
                            lookup [key.GetHashCode ()] = value;
                        }
                        if (lookup.ContainsKey (needle)) {
                            lookup [needle].Invoke (this, new IodineObject [] { });
                            top.Push (IodineBool.True);
                        } else {
                            top.Push (IodineBool.False);
                        }
                        break;
                    }
                case Opcode.BeginWith: {
                        var obj = top.Pop ();
                        obj.Enter (this);
                        Top.DisposableObjects.Push (obj);
                        break;
                    }
                case Opcode.EndWith: {
                        Top.DisposableObjects.Pop ().Exit (this);
                        break;
                    }
                case Opcode.IncludeMixin: {
                        var obj = top.Pop ();
                        var type = top.Pop ();

                        foreach (var attr in obj.Attributes) {
                            type.SetAttribute (attr.Key, attr.Value);
                        }
                        break;
                    }
                case Opcode.ApplyMixin: {
                        var type = top.Pop ();
                        var mixin = instruction.ArgumentObject as IodineMixin;

                        foreach (var attr in mixin.Attributes) {
                            type.SetAttribute (attr.Key, attr.Value);
                        }
                        break;
                    }
                case Opcode.BuildFunction: {
                        var flags = (MethodFlags)instruction.Argument;

                        var name = top.Pop () as IodineString;
                        var doc = top.Pop () as IodineString;
                        var codeObj = top.Pop () as CodeObject;
                        var parameters = top.Pop () as IodineTuple;

                        var defaultValues = new IodineObject [] { };

                        int defaultValuesStart = 0;

                        if (flags.HasFlag (MethodFlags.HasDefaultParameters)) {
                            var defaultValuesTuple = top.Pop () as IodineTuple;
                            var startInt = top.Pop () as IodineInteger;
                            defaultValues = defaultValuesTuple.Objects;
                            defaultValuesStart = (int)startInt.Value;
                        }

                        var method = new IodineMethod (
                            Top.Module,
                            name,
                            codeObj,
                            parameters,
                            flags,
                            defaultValues,
                            defaultValuesStart
                        );

                        method.SetAttribute ("__doc__", doc);

                        top.Push (method);

                        break;
                    }
                }
            }
            return top.Stack.LastObject ?? IodineNull.Instance;
        }

        /// <summary>
        /// Raises a generic Iodine exception
        /// </summary>
        /// <param name="message">Format.</param>
        /// <param name="args">Arguments.</param>
        public void RaiseException (string message, params object [] args)
        {
            RaiseException (new IodineException (message, args));
        }

        /// <summary>
        /// Raises an exception, throwing 'ex' as an IodineException object
        /// </summary>
        /// <param name="ex">Exception to raise.</param>
        public void RaiseException (IodineObject ex)
        {
            if (traceCallback != null) {
                traceCallback (TraceType.Exception, this, Top, Top.Location);
            }

            var handler = PopCurrentExceptionHandler ();

            if (handler == null) { // No exception handler
               /*
                * The program has gone haywire and we ARE going to crash, however
                * we must attempt to properly dispose any objects created inside 
                * Iodine's with statement
                */
                StackFrame top = Top;

                while (top != null) {
                    while (top.DisposableObjects.Count > 0) {
                        var obj = top.DisposableObjects.Pop ();

                        try {
                            obj.Exit (this); // Call __exit__
                        } catch (UnhandledIodineExceptionException) {
                            // Ignore this, we will throw one when we're done anyway
                        }
                    }
                    top = top.Parent;
                }

                throw new UnhandledIodineExceptionException (Top, ex);
            }

            ex.SetAttribute ("stacktrace", new IodineString (GetStackTrace ()));

            UnwindStack (frameCount - handler.Frame);

            lastException = ex;

            Top.InstructionPointer = handler.InstructionPointer;
        }

        /// <summary>
        /// Sets the trace callback function (For debugging).
        /// </summary>
        /// <param name="callback">Callback.</param>
        public void SetTrace (TraceCallback callback)
        {
            traceCallback = callback;
        }

        void Trace (TraceType type, StackFrame frame, SourceLocation location)
        {
            pauseVirtualMachine.WaitOne ();

            if (traceCallback != null && traceCallback (type, this, frame, location)) {
                pauseVirtualMachine.Reset ();
            }
        }

        /// <summary>
        /// Unwinds the stack n frames
        /// </summary>
        /// <param name="numFrames">Frames.</param>
        void UnwindStack (int numFrames)
        {
            for (int i = 0; i < numFrames; i++) {
                var frame = frames.Pop ();
                frame.AbortExecution = true;
            }

            frameCount -= numFrames;

            Top = frames.Peek ();
        }

        IodineExceptionHandler PopCurrentExceptionHandler ()
        {
            StackFrame current = Top;
            while (current != null) {
                if (current.ExceptionHandlers.Count > 0) {
                    return current.ExceptionHandlers.Pop ();
                }
                current = current.Parent;
            }
            return null;
        }

#if DOTNET_45
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
#endif
        public void NewFrame (StackFrame frame)
        {
            frameCount++;
            stackSize++;
            Top = frame;
            frames.Push (frame);
        }

#if DOTNET_45
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
#endif
        void NewFrame (IodineMethod method, IodineObject [] args, IodineObject self)
        {
            frameCount++;
            stackSize++;
            Top = new StackFrame (this, method.Module, method, args, Top, self);
            frames.Push (Top);
        }

#if DOTNET_45
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
#endif
        public StackFrame EndFrame ()
        {
            frameCount--;
            stackSize--;
            var ret = frames.Pop ();
            if (frames.Count != 0) {
                Top = frames.Peek ();
            } else {
                Top = null;
            }
            return ret;
        }

        public IodineModule LoadModule (string name, bool useCache = true)
        {
            var module = Context.LoadModule (name, useCache);
            if (module == null) {
                throw new ModuleNotFoundException (name, Context.SearchPath);
            }
            return module;
        }

        /// <summary>
        /// Sets a global variable 
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="val">Value.</param>
        public void SetGlobal (string name, IodineObject val)
        {
            Top.Module.SetAttribute (name, val);
        }
    }
}



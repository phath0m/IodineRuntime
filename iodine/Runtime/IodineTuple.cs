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
    public class IodineTuple : IodineObject
    {
        public static readonly IodineTypeDefinition TypeDefinition = new TupleTypeDef ();

        sealed class TupleTypeDef : IodineTypeDefinition
        {
            public TupleTypeDef ()
                : base ("Tuple")
            {
                BindAttributes (this);

                SetDocumentation ("An immutable collection of objects");

            }

            public override IodineObject BindAttributes (IodineObject obj)
            {
                IodineIterableMixin.ApplyMixin (obj);
                return base.BindAttributes (obj);
            }

            public override IodineObject Invoke (VirtualMachine vm, IodineObject[] args)
            {
                if (args.Length >= 1) {
                    var inputList = args [0] as IodineList;
                    return new IodineTuple (inputList.Objects.ToArray ());
                }
                return null;
            }
        }

        class TupleIterable : IodineObject
        {
            static IodineTypeDefinition TypeDefinition = new IodineTypeDefinition ("TupleIterator");

            int iterIndex = 0;
            IodineObject [] objects;

            public TupleIterable (IodineObject[] objects)
                : base (TypeDefinition)
            {
                this.objects = objects;
            }

            public override IodineObject IterGetCurrent (VirtualMachine vm)
            {
                return objects [iterIndex - 1];
            }

            public override bool IterMoveNext (VirtualMachine vm)
            {
                if (iterIndex >= objects.Length) {
                    return false;
                }
                iterIndex++;
                return true;
            }

            public override void IterReset (VirtualMachine vm)
            {
                iterIndex = 0;
            }
        }

        public IodineObject[] Objects { private set; get; }

        public IodineTuple (IodineObject[] items)
            : base (TypeDefinition)
        {
            Objects = items;

            // HACK: Add __iter__ attribute to match Iterable trait
            SetAttribute ("__iter__", new BuiltinMethodCallback ((VirtualMachine vm, IodineObject self, IodineObject [] args) => {
                return GetIterator (vm);
            }, this));
        }

        public override bool Equals (IodineObject obj)
        {
            var tupleVal = obj as IodineTuple;

            if (tupleVal != null && tupleVal.Objects.Length == Objects.Length) {
                for (int i = 0; i < Objects.Length; i++) {
                    if (!Objects [i].Equals (tupleVal.Objects [i])) {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public override IodineObject Equals (VirtualMachine vm, IodineObject left)
        {
            return IodineBool.Create (Equals (left));
        }

        public override IodineObject Len (VirtualMachine vm)
        {
            return new IodineInteger (Objects.Length);
        }

        public override IodineObject Slice (VirtualMachine vm, IodineSlice slice)
        {
            return Subtuple (
                slice.Start,
                slice.Stop,
                slice.Stride,
                slice.DefaultStart,
                slice.DefaultStop
            );
        }

        IodineTuple Subtuple (int start, int end, int stride, bool defaultStart, bool defaultEnd)
        {
            int actualStart = start >= 0 ? start : Objects.Length - (start + 2);
            int actualEnd = end >= 0 ? end : Objects.Length - (end + 2);

            var accum = new List<IodineObject> ();

            if (stride >= 0) {

                if (defaultStart) {
                    actualStart = 0;
                }

                if (defaultEnd) {
                    actualEnd = Objects.Length;
                }

                for (int i = actualStart; i < actualEnd; i += stride) {
                    accum.Add (Objects [i]);
                }
            } else {

                if (defaultStart) {
                    actualStart = Objects.Length - 1;
                }

                if (defaultEnd) {
                    actualEnd = 0;
                }

                for (int i = actualStart; i >= actualEnd; i += stride) {
                    accum.Add (Objects [i]);
                }
            }

            return new IodineTuple (accum.ToArray ());
        }

        public override IodineObject GetIndex (VirtualMachine vm, IodineObject key)
        {
            var index = key as IodineInteger;
            if (index.Value < Objects.Length) {
                return Objects [(int)index.Value];
            }
            vm.RaiseException (new IodineIndexException ());
            return null;
        }

        public override IodineObject GetIterator (VirtualMachine vm)
        {
            return new TupleIterable (Objects);
        }

        public override IodineObject Represent (VirtualMachine vm)
        {
            var repr = String.Join (", ", Objects.Select (p => p.Represent (vm).ToString ()));
            return new IodineString (String.Format ("({0})", repr));
        }

        public override bool IsTrue ()
        {
            return Objects.Length > 0;
        }

        public override int GetHashCode ()
        {
            int accum = 17;
            unchecked {
                foreach (IodineObject obj in Objects) {
                    if (obj != null) {
                        accum += 529 * obj.GetHashCode ();
                    }
                }
            }
            return accum;
        }
    }
}


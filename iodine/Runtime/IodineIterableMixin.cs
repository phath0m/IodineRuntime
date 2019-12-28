// /**
//   * Copyright (c) 2015, phath0m All rights reserved.
//
//   * Redistribution and use in source and binary forms, with or without modification,
//   * are permitted provided that the following conditions are met:
//   * 
//   *  * Redistributions of source code must retain the above copyright notice, this list
//   *    of conditions and the following disclaimer.
//   * 
//   *  * Redistributions in binary form must reproduce the above copyright notice, this
//   *    list of conditions and the following disclaimer in the documentation and/or
//   *    other materials provided with the distribution.
//
//   * Neither the name of the copyright holder nor the names of its contributors may be
//   * used to endorse or promote products derived from this software without specific
//   * prior written permission.
//   * 
//   * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY
//   * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
//   * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT
//   * SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
//   * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
//   * TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR
//   * BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
//   * CONTRACT ,STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
//   * ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH
//   * DAMAGE.
// /**
using System;

namespace Iodine.Runtime
{
    public class IodineIterableMixin : IodineMixin
    {
        public IodineIterableMixin ()
            : base ("IterableMixin")
        {
        }

        public override void Inherit (VirtualMachine vm, IodineObject self, IodineObject [] arguments)
        {
            ApplyMixin (self);

            base.Inherit (vm, self, arguments);
        }

        public override IodineObject BindAttributes (IodineObject obj)
        {
            ApplyMixin (obj);

            return base.BindAttributes (obj);
        }

        public static void ApplyMixin (IodineObject obj)
        {
            obj.SetAttribute ("each", new BuiltinMethodCallback (Each, obj));
            obj.SetAttribute ("filter", new BuiltinMethodCallback (Filter, obj));
            obj.SetAttribute ("first", new BuiltinMethodCallback (First, obj));
            obj.SetAttribute ("map", new BuiltinMethodCallback (Map, obj));
            obj.SetAttribute ("last", new BuiltinMethodCallback (Last, obj));
            obj.SetAttribute ("reduce", new BuiltinMethodCallback (Reduce, obj));
        }

        [BuiltinDocString (
            "Iterates through each element in the collection.",
            "@param func The function to call for each element in the collection"
        )]
        static IodineObject Each (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {

            if (args.Length == 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            var iterator = self.GetIterator (vm);

            iterator.IterReset (vm);

            while (iterator.IterMoveNext (vm)) {
                var obj = iterator.IterGetCurrent (vm);

                args [0].Invoke (vm, new IodineObject [] { obj });
            }

            return null;
        }

        [BuiltinDocString (
            "Iterates over the specified iterable, passing the result of each iteration to the specified ",
            "callable. If the callable returns true, the result is appended to a list that is returned ",
            "to the caller.",
            "@param callable The callable to be used for filtering."
        )]
        static IodineObject Filter (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length == 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            var list = new IodineList (new IodineObject [] { });
            var collection = self.GetIterator (vm);

            IodineObject func = args [0];

            collection.IterReset (vm);

            while (collection.IterMoveNext (vm)) {
                var o = collection.IterGetCurrent (vm);
                if (func.Invoke (vm, new IodineObject [] { o }).IsTrue ()) {
                    list.Add (o);
                }
            }
            return list;
        }

        [BuiltinDocString (
            "Returns the first item in this collection.",
            "@param value The default value to use if this collection is empty"
        )]
        static IodineObject First (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {

            IodineObject defaultObject = IodineNull.Instance;

            if (args.Length > 0) {
                defaultObject = args [0];
            }

            var iterator = self.GetIterator (vm);

            iterator.IterReset (vm);

            if (iterator.IterMoveNext (vm)) {
                return iterator.IterGetCurrent (vm);
            }

            return defaultObject;
        }

        [BuiltinDocString (
            "Iterates over the specified iterable, passing the result of each iteration to the specified ",
            "callable. The result of the specified callable is added to a list that is returned to the caller.",
            "@param callable The callable to be used for mapping."
        )]
        static IodineObject Map (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {

            if (args.Length == 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            var list = new IodineList (new IodineObject [] { });
            var collection = self.GetIterator (vm);
            var func = args [0];

            collection.IterReset (vm);
            while (collection.IterMoveNext (vm)) {
                var o = collection.IterGetCurrent (vm);
                list.Add (func.Invoke (vm, new IodineObject [] { o }));
            }
            return list;
        }

        [BuiltinDocString (
            "Returns the last item in this collection.",
            "@param value The default value to use if this collection is empty"
        )]
        static IodineObject Last (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {

            IodineObject returnObject = IodineNull.Instance;

            if (args.Length > 0) {
                returnObject = args [0];
            }

            var iterator = self.GetIterator (vm);

            iterator.IterReset (vm);

            while (iterator.IterMoveNext (vm)) {
                returnObject = iterator.IterGetCurrent (vm);
            }

            return returnObject;
        }

        [BuiltinDocString (
            "Reduces all members of the specified iterable by applying the specified callable to each item ",
            "left to right. The callable passed to reduce receives two arguments, the first one being the ",
            "result of the last call to it and the second one being the current item from the iterable.",
            "@param callable The callable to be used for reduced.",
            "@param default The default item."
        )]
        static IodineObject Reduce (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length == 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            var collection = self.GetIterator (vm);

            IodineObject result = args.Length > 1 ? args [0] : null;
            IodineObject func = args.Length > 1 ? args [1] : args [0];

            collection.IterReset (vm);

            while (collection.IterMoveNext (vm)) {
                var o = collection.IterGetCurrent (vm);

                if (result == null) {
                    result = o;
                }

                result = func.Invoke (vm, new IodineObject [] { result, o });
            }
            return result;
        }


    }
}

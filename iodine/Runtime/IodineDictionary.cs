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

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Iodine.Util;

namespace Iodine.Runtime
{
    public class IodineDictionary : IodineObject
    {
        public static readonly IodineTypeDefinition TypeDefinition = new MapTypeDef ();

        sealed class MapTypeDef : IodineTypeDefinition
        {
            public MapTypeDef ()
                : base ("Dict")
            {
                BindAttributes (this);

                SetDocumentation (
                    "A dictionary containing a list of unique keys and an associated value",
                    "@optional values An iterable collection of tuples to initialize the dictionary with"
                );
            }


            public override IodineObject BindAttributes (IodineObject obj)
            {
                obj.SetAttribute ("contains", new BuiltinMethodCallback (Contains, obj));
                obj.SetAttribute ("getSize", new BuiltinMethodCallback (GetSize, obj));
                obj.SetAttribute ("clear", new BuiltinMethodCallback (Clear, obj));
                obj.SetAttribute ("set", new BuiltinMethodCallback (Set, obj));
                obj.SetAttribute ("get", new BuiltinMethodCallback (Get, obj));
                obj.SetAttribute ("remove", new BuiltinMethodCallback (Remove, obj));
                return obj;
            }

            public override IodineObject Invoke (VirtualMachine vm, IodineObject[] args)
            {
                if (args.Length >= 1) {
                    var inputList = args [0] as IodineList;
                    var ret = new IodineDictionary ();
                    if (inputList != null) {
                        foreach (IodineObject item in inputList.Objects) {
                            IodineTuple kv = item as IodineTuple;
                            if (kv != null) {
                                ret.Set (kv.Objects [0], kv.Objects [1]);
                            }
                        }
                    } 
                    return ret;
                }
                return new IodineDictionary ();
            }

            [BuiltinDocString (
                "Tests to see if the dictionary contains a key, returning true if it does.",
                "@param key The key to test if this dictionary contains."
            )]
            IodineObject Contains (VirtualMachine vm, IodineObject self, IodineObject [] args)
            {
                var thisObj = self as IodineDictionary;
                if (args.Length <= 0) {
                    vm.RaiseException (new IodineArgumentException (1));
                    return null;
                }
                return IodineBool.Create (thisObj.dict.ContainsKey (args [0]));
            }

            IodineObject GetSize (VirtualMachine vm, IodineObject self, IodineObject [] arguments)
            {
                var thisObj = self as IodineDictionary;
                return new IodineInteger (thisObj.dict.Count);
            }

            [BuiltinDocString (
                "Clears the dictionary, removing all items."
            )]
            IodineObject Clear (VirtualMachine vm, IodineObject self, IodineObject [] arguments)
            {
                var thisObj = self as IodineDictionary;
                thisObj.dict.Clear ();
                return null;
            }

            [BuiltinDocString (
                "Sets a key to a specified value, if the key does not exist, it will be created.",
                "@param key The key of the specified value",
                "@param value The value associated with [key]"
            )]
            IodineObject Set (VirtualMachine vm, IodineObject self, IodineObject [] arguments)
            {
                var thisObj = self as IodineDictionary;
                if (arguments.Length >= 2) {
                    IodineObject key = arguments [0];
                    IodineObject val = arguments [1];
                    thisObj.dict [key] = val;
                    return null;
                }
                vm.RaiseException (new IodineArgumentException (2));
                return null;
            }

            [BuiltinDocString (
                "Returns the value specified by [key], raising a KeyNotFound exception if the given key does not exist.",
                "@param key The key whose value will be returned."
            )]
            IodineObject Get (VirtualMachine vm, IodineObject self, IodineObject [] arguments)
            {
                var thisObj = self as IodineDictionary;
                if (arguments.Length <= 0) {
                    vm.RaiseException (new IodineArgumentException (1));
                    return null;
                } else if (arguments.Length == 1) {
                    IodineObject key = arguments [0];
                    if (thisObj.dict.ContainsKey (key)) {
                        return thisObj.dict [key] as IodineObject;
                    }
                    vm.RaiseException (new IodineKeyNotFound ());
                    return null;
                } else {
                    IodineObject key = arguments [0];
                    if (thisObj.dict.ContainsKey (key)) {
                        return thisObj.dict [key] as IodineObject;
                    }
                    return arguments [1];
                }
            }

            [BuiltinDocString (
                "Removes a specified entry from the dictionary, raising a KeyNotFound exception if the given key does not exist.",
                "@param key The key which is to be removed."
            )]
            IodineObject Remove (VirtualMachine vm, IodineObject self, IodineObject [] arguments)
            {
                var thisObj = self as IodineDictionary;
                if (arguments.Length >= 1) {
                    IodineObject key = arguments [0];
                    if (!thisObj.dict.ContainsKey (key)) {
                        vm.RaiseException (new IodineKeyNotFound ());
                        return null;
                    }
                    thisObj.dict.Remove (key);
                    return null;
                }
                vm.RaiseException (new IodineArgumentException (2));
                return null;
            }
        }

        class DictIterator : IodineObject
        {
            static IodineTypeDefinition TypeDefinition = new IodineTypeDefinition ("DictIterator");

            IDictionaryEnumerator enumerator;

            public DictIterator (ObjectDictionary dict)
                : base (TypeDefinition)
            {
                enumerator = dict.GetEnumerator ();
            }

            public override IodineObject IterGetCurrent (VirtualMachine vm)
            {
                return new IodineTuple (new IodineObject [] {
                    enumerator.Key as IodineObject,
                    enumerator.Value as IodineObject
                });
            }

            public override bool IterMoveNext (VirtualMachine vm)
            {
                return enumerator.MoveNext ();
            }

            public override void IterReset (VirtualMachine vm)
            {
                enumerator.Reset ();
            }
        }

        ObjectDictionary dict;

        public IEnumerable<IodineObject> Keys {
            get {
                return dict.Keys.Cast<IodineObject> ();
            }
        }

        public IodineDictionary ()
            : this (new ObjectDictionary ())
        {
        }

        public IodineDictionary (ObjectDictionary dict)
            : base (TypeDefinition)
        {
            this.dict = dict;
            SetAttribute ("__iter__", new BuiltinMethodCallback ((VirtualMachine vm, IodineObject self, IodineObject [] args) => {
                return GetIterator (vm);
            }, this));
        }

        public IodineDictionary (AttributeDictionary dict)
            : this ()
        {
            foreach (KeyValuePair<string, IodineObject> kv in dict) {
                this.dict [new IodineString (kv.Key)] = kv.Value;
            }
        }

        public override IodineObject Len (VirtualMachine vm)
        {
            return new IodineInteger (dict.Count);
        }

        public override IodineObject GetIndex (VirtualMachine vm, IodineObject key)
        {
            if (!dict.ContainsKey (key)) {
                vm.RaiseException (new IodineKeyNotFound ());
                return null;
            }
            return dict [key] as IodineObject;
        }

        public override void SetIndex (VirtualMachine vm, IodineObject key, IodineObject value)
        {
            dict [key] = value;
        }

        public override bool Equals (IodineObject obj)
        {
            var map = obj as IodineDictionary;

            if (map != null) {
                if (map.dict.Count != this.dict.Count) {
                    return false;
                }

                foreach (IodineObject key in map.Keys) {
                    if (!map.ContainsKey (key)) {
                        return false;
                    }

                    var dictKey = map.Get (key) as IodineObject;
                    if (!dictKey.Equals ((IodineObject)dict [key])) {
                        return false;
                    }
                }
                return true;
            }

            return false;
        }

        public override IodineObject Equals (VirtualMachine vm, IodineObject right)
        {
            var hash = right as IodineDictionary;
            if (hash == null) {
                vm.RaiseException (new IodineTypeException ("HashMap"));
                return null;
            }
            return IodineBool.Create (Equals (hash));
        }

        public override int GetHashCode ()
        {
            return dict.GetHashCode ();
        }

        public override IodineObject GetIterator (VirtualMachine vm)
        {
            return new DictIterator (dict);
        }

        /// <summary>
        /// Set the specified key and valuw.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="val">Value.</param>
        public void Set (IodineObject key, IodineObject val)
        {
            dict [key] = val;
        }

        /// <summary>
        /// Get the specified key.
        /// </summary>
        /// <param name="key">Key.</param>
        public IodineObject Get (IodineObject key)
        {
            return dict [key] as IodineObject;
        }

        /// <summary>
        /// Determines whether or not this dictionary contains the specific key
        /// </summary>
        /// <returns><c>true</c>, if key was containsed, <c>false</c> otherwise.</returns>
        /// <param name="key">Key.</param>
        public bool ContainsKey (IodineObject key)
        {
            return dict.ContainsKey (key);
        }
    }
}
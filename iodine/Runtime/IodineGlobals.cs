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

using System.Collections.Generic;

namespace Iodine.Runtime
{
    public class IodineGlobals : IodineObject, IIodineProperty
    {
        public static readonly IodineTypeDefinition TypeDefinition = new IodineTypeDefinition ("Globals");

        class GlobalsObject : IodineObject
        {
            public GlobalsObject ()
                : base (TypeDefinition)
            {
            }

            public override void SetAttribute (VirtualMachine vm, string name, IodineObject value)
            {
                vm.Context.Globals [name] = value;
            }

            public override IodineObject GetAttribute (VirtualMachine vm, string name)
            {
                return vm.Context.Globals [name];
            }

        }

        public readonly static IodineObject Instance = new IodineGlobals ();

        protected IodineGlobals ()
            : base (TypeDefinition)
        {
            
        }

        public IodineObject Set (VirtualMachine vm, IodineObject obj)
        {
            var dict = obj as IodineDictionary;
            if (dict != null) {
                vm.Context.Globals.Clear ();

                foreach (IodineObject key in dict.Keys) {
                    vm.Context.Globals [key.ToString ()] = dict.Get (key);
                }
            }
            return null;
        }

        public IodineObject Get (VirtualMachine vm)
        {
            var ret = new IodineDictionary ();
            foreach (KeyValuePair<string, IodineObject> kv in vm.Context.Globals) {
                ret.Set (new IodineString (kv.Key), kv.Value);
            }
            return ret;
        }
    }
}


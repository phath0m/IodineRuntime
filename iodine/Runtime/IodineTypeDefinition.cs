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
using System.Reflection;
using System.Collections.Generic;

namespace Iodine.Runtime
{
    /// <summary>
    /// Base class for all custom Iodine types, the equivalent of C#'s Type class or
    /// Java's Class class. 
    /// </summary>
    public class IodineTypeDefinition : IodineObject
    {
        public class TypeDefinitionTypeDefinition : IodineTypeDefinition
        {
            public TypeDefinitionTypeDefinition ()
                : base ("TypeDef")
            {
            }

            public override IodineObject Invoke (VirtualMachine vm, IodineObject[] args)
            {
                if (args.Length == 0) {
                    vm.RaiseException (new IodineArgumentException (1));
                    return null;
                }

                var name = args [0] as IodineString;

                if (name == null) {
                    vm.RaiseException (new IodineTypeException ("Str"));
                    return null;
                }

                IodineTypeDefinition baseType = IodineObject.ObjectTypeDef;

                if (args.Length > 1) {
                    baseType = args [1] as IodineTypeDefinition;

                    if (baseType == null) {
                        vm.RaiseException (new IodineTypeException ("TypeDef"));
                        return null;
                    }
                }

                var clazz = new IodineTypeDefinition (name.Value);

                clazz.Base = baseType;

                if (args.Length > 2) {
                    var map = args [2] as IodineDictionary;

                    foreach (IodineObject key in map.Keys) {
                        clazz.SetAttribute (key.ToString (), map.Get (key));
                    }
                }

                return clazz;
            }
        }

        public static IodineTypeDefinition TypeDefinition = new TypeDefinitionTypeDefinition ();

        public readonly string Name;

        public IodineTypeDefinition (string name)
            : base (TypeDefinition)
        {
            Name = name;

            Attributes ["__name__"] = new InternalIodineProperty (vm => new IodineString (name), null);
        }

        public void SetDocumentation (params string[] args)
        {
            Attributes ["__doc__"] = new InternalIodineProperty (vm => {
                return new IodineString (string.Join ("\n", args));
            }, null);
        }

        public override bool IsCallable ()
        {
            return true;
        }

        public override IodineObject Invoke (VirtualMachine vm, IodineObject[] arguments)
        {
            return new IodineObject (this);
        }

        public override string ToString ()
        {
            return Name;
        }

        public virtual void Inherit (VirtualMachine vm, IodineObject self, IodineObject[] arguments)
        {
            var obj = Invoke (vm, arguments);
            foreach (string attr in Attributes.Keys) {
                if (!self.HasAttribute (attr)) {
                    self.SetAttribute (attr, Attributes [attr]);
                }
                obj.SetAttribute (attr, Attributes [attr]);
            }
            self.SetAttribute ("__super__", obj);
            self.Base = obj;
        }

        public virtual IodineObject BindAttributes (IodineObject obj)
        {
            foreach (KeyValuePair<string, IodineObject> kv in Attributes) {
                if (!obj.HasAttribute (kv.Key)) {
                    obj.SetAttribute (kv.Key, kv.Value);
                }
            }
            return obj;
        }
    }
}


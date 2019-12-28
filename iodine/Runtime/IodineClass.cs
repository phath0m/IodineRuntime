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
    /// <summary>
    /// Metaclass for userdefined Iodine classes
    /// </summary>
    public class IodineClass : IodineTypeDefinition
    {
        bool initializerInvoked = false;

        public CodeObject Initializer { internal set; get; }

        public IodineMethod Constructor { private set; get; }

        public IodineTypeDefinition BaseClass {
            set {
                Attributes ["__base__"] = value;
            }
            get {
                if (!Attributes.ContainsKey ("__base__")) {
                    return null;
                }
                return Attributes ["__base__"] as IodineTypeDefinition;
            }
        }

        public IodineClass (string name, CodeObject initializer, IodineMethod constructor)
            : base (name)
        {
            Constructor = constructor;
            Initializer = initializer;
            SetAttribute ("__doc__", IodineString.Empty);
        }

        public override IodineObject GetAttribute (VirtualMachine vm, string name)
        {
            if (!initializerInvoked) {
                initializerInvoked = true;
            }
            return base.GetAttribute (vm, name);
        }

        public override void SetAttribute (VirtualMachine vm, string name, IodineObject value)
        {
            if (!initializerInvoked) {
                initializerInvoked = true;
            }
            base.SetAttribute (vm, name, value);
        }

        public override bool IsCallable ()
        {
            return true;
        }

        public override IodineObject Invoke (VirtualMachine vm, IodineObject [] arguments)
        {
            if (!initializerInvoked) {
                initializerInvoked = true;
                //Initializer.Invoke (vm, new IodineObject[] { });
            }
            var obj = new IodineObject (this);

            BindAttributes (obj);

            if (BaseClass != null) {
                //BaseClass.Inherit (vm, obj, new IodineObject[] { });
            }
            vm.InvokeMethod (Constructor, obj, arguments);
            return obj;
        }

        public override void Inherit (VirtualMachine vm, IodineObject self, IodineObject [] arguments)
        {
            var obj = Invoke (vm, arguments);

            foreach (KeyValuePair<string, IodineObject> kv in Attributes) {
                if (!self.HasAttribute (kv.Key))
                    self.SetAttribute (kv.Key, kv.Value);
                if (!obj.HasAttribute (kv.Key)) {
                    obj.SetAttribute (kv.Key, kv.Value);
                }
            }
            Dictionary<string, IodineObject> childAttributes = obj.Attributes;

            foreach (KeyValuePair<string, IodineObject> kv in childAttributes) {
                if (kv.Value is IodineBoundMethod) {
                    IodineBoundMethod wrapper = (IodineBoundMethod)kv.Value;
                    wrapper.Bind (self);
                }
            }
            self.SetAttribute ("__super__", obj);
            self.Base = obj;
        }

        public override IodineObject Represent (VirtualMachine vm)
        {
            return new IodineString (string.Format ("<Class {0}>", Name));
        }

        public override string ToString ()
        {
            return Name;
        }
    }
}


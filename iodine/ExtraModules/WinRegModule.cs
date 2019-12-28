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

#if COMPILE_EXTRAS

using System;
using Microsoft.Win32;
using Iodine.Runtime;

namespace Iodine.Modules.Extras
{
    [IodineBuiltinModule ("winreg")]
    internal class WinRegModule : IodineModule
    {
        class IodineRegistryKeyHandle : IodineObject
        {
            private static new readonly RegistryKeyHandleTypeDef TypeDef = new RegistryKeyHandleTypeDef ();

            class RegistryKeyHandleTypeDef : IodineTypeDefinition
            {
                public RegistryKeyHandleTypeDef ()
                    : base ("RegistryKeyHandle")
                {

                }
            }

            public RegistryKey Key { private set; get; }

            public IodineRegistryKeyHandle (RegistryKey original)
                : base (TypeDef)
            {
                Key = original;
                SetAttribute ("setValue", new BuiltinMethodCallback (setValue, this));
                SetAttribute ("getValue", new BuiltinMethodCallback (getValue, this));
                SetAttribute ("deleteValue", new BuiltinMethodCallback (deleteValue, this));
            }

            private IodineObject setValue (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                string name = args [0].ToString ();
                IodineObject ioval = args [1];
                object val = ConvertIodineObjectToObject (ioval);
                Key.SetValue (name, val);
                return null;
            }

            private IodineObject getValue (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                string name = args [0].ToString ();
                object val = Key.GetValue (name);
                return ConvertPrimativeToIodineObject (val);
            }

            private IodineObject deleteValue (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                string name = args [0].ToString ();
                Key.DeleteValue (name);
                return null;
            }
        }

        class IodineRegistryHive : IodineObject
        {
            private static new readonly RegistryHiveTypeDef TypeDef = new RegistryHiveTypeDef ();

            class RegistryHiveTypeDef : IodineTypeDefinition
            {
                public RegistryHiveTypeDef ()
                    : base ("RegistryHive")
                {

                }
            }

            public RegistryHive Hive { private set; get; }

            public IodineRegistryHive (RegistryHive original)
                : base (TypeDef)
            {
                this.Hive = original;
            }
        }

        public WinRegModule ()
            : base ("winreg")
        {
            SetAttribute ("HKEY_CLASSES_ROOT", new IodineRegistryHive (RegistryHive.ClassesRoot));
            SetAttribute ("HKEY_CURRENT_CONFIG", new IodineRegistryHive (RegistryHive.CurrentConfig));
            SetAttribute ("HKEY_CURRENT_USER", new IodineRegistryHive (RegistryHive.CurrentUser));
            SetAttribute ("HKEY_LOCAL_MACHINE", new IodineRegistryHive (RegistryHive.LocalMachine));
            SetAttribute ("HKEY_USERS", new IodineRegistryHive (RegistryHive.Users));
            SetAttribute ("ClassesRoot", new IodineRegistryKeyHandle (Registry.ClassesRoot));
            SetAttribute ("CurrentConfig", new IodineRegistryKeyHandle (Registry.CurrentConfig));
            SetAttribute ("CurrentUser", new IodineRegistryKeyHandle (Registry.CurrentUser));
            SetAttribute ("LocalMachine", new IodineRegistryKeyHandle (Registry.LocalMachine));
            SetAttribute ("setValue", new BuiltinMethodCallback (setValue, null));
            SetAttribute ("getValue", new BuiltinMethodCallback (getValue, null));
        }

        private IodineObject setValue (VirtualMachine vm, IodineObject self, IodineObject[] args)
        {
            string keyName = args [0].ToString ();
            string name = args [1].ToString ();
            IodineObject ioval = args [2];
            object val = ConvertIodineObjectToObject (ioval);
            Registry.SetValue (keyName, name, val);
            return null;
        }

        private IodineObject getValue (VirtualMachine vm, IodineObject self, IodineObject[] args)
        {
            string keyName = args [0].ToString ();
            string name = args [1].ToString ();
            object val = Registry.GetValue (keyName, name, null);
            return ConvertPrimativeToIodineObject (val);
        }


        private static object ConvertIodineObjectToObject (IodineObject obj)
        {
            if (obj is IodineInteger) {
                return (Int32)(((IodineInteger)obj).Value);
            }

            if (obj is IodineString) {
                return (((IodineString)obj).Value);
            }

            return null;
        }

        private static IodineObject ConvertPrimativeToIodineObject (object obj)
        {
            if (obj is Int32) {
                return new IodineInteger ((int)obj);
            }

            if (obj is Int64) {
                return new IodineInteger ((long)obj);
            }

            if (obj is String) {
                return new IodineString ((string)obj);
            }
            return null;
        }
    }
}

#endif
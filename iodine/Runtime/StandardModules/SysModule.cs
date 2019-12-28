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

namespace Iodine.Runtime
{
    [IodineBuiltinModule ("sys")]
    public class SysModule : IodineModule
    {
        public SysModule ()
            : base ("sys")
        {
            int major = Assembly.GetExecutingAssembly ().GetName ().Version.Major;
            int minor = Assembly.GetExecutingAssembly ().GetName ().Version.Minor;
            int patch = Assembly.GetExecutingAssembly ().GetName ().Version.Build;
            SetAttribute ("executable", new IodineString (Assembly.GetExecutingAssembly ().Location));
            // TODO: Make path accessible
            //SetAttribute ("path", new IodineList (IodineModule.SearchPaths));
            SetAttribute ("exit", new BuiltinMethodCallback (Exit, this));
            SetAttribute ("getframe", new BuiltinMethodCallback (GetFrame, this));
            SetAttribute ("_warn", new BuiltinMethodCallback (Warn, this));
            SetAttribute ("_getwarnmask", new BuiltinMethodCallback (GetWarnMask, this));
            SetAttribute ("_setwarnmask", new BuiltinMethodCallback (SetWarnMask, this));
            SetAttribute ("path", new InternalIodineProperty (GetPath, null));

            SetAttribute ("VERSION_MAJOR", new IodineInteger (major));
            SetAttribute ("VERSION_MINOR", new IodineInteger (minor));
            SetAttribute ("VERSION_PATCH", new IodineInteger (patch));
            SetAttribute ("VERSION_STR", new IodineString (String.Format ("v{0}.{1}.{2}", major, minor, patch)));
        }

        IodineObject GetPath (VirtualMachine vm)
        {
            return new IodineTuple (vm.Context.SearchPath.Select (p => new IodineString (p)).ToArray ());
        }

        [BuiltinDocString (
            "Forcefully terminates the current process (Including the Iodine host).",
            "@param code The exit code."
        )]
        IodineObject Exit (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            var exitCode = args [0] as IodineInteger;

            if (exitCode == null) {
                vm.RaiseException (new IodineTypeException ("Int"));
                return null;
            }

            Environment.Exit ((int)exitCode.Value);
            return null;
        }

        [BuiltinDocString (
            "Returns the nth stack frame.",
            "@param n Stack frame index, relative to the current frame."
        )]
        IodineObject GetFrame (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length == 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            var index = args [0] as IodineInteger;

            if (index == null) {
                vm.RaiseException (new IodineTypeException ("Int"));
                return null;
            }

            StackFrame top = vm.Top;
            int i = 0;
            while (top != null && i != index.Value) {
                top = top.Parent;
                i++;
            }

            if (top == null) {
                return IodineNull.Instance;
            }

            return new IodineStackFrameWrapper (top);
        }

        /**
         * Iodine Function: _warn (type, msg)
         * Description: Internal low level function for issuing warnings
         * See modules/warnings.id
         */
        IodineObject Warn (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 1) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            var warnType = args [0] as IodineInteger;
            var warnMsg = args [1] as IodineString;

            if (warnType == null) {
                vm.RaiseException (new IodineTypeException ("Int"));
                return null;
            }

            if (warnMsg == null) {
                vm.RaiseException (new IodineTypeException ("Str"));
                return null;
            }

            vm.Context.Warn ((WarningType)warnType.Value, warnMsg.ToString ());

            return null;
        }

        /**
         * Iodine Function: _getWarnMask ()
         * Description: Internal low level function for obtaining the current warning mask
         * See modules/warnings.id
         */
        IodineObject GetWarnMask (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            return new IodineInteger ((long)vm.Context.WarningFilter);
        }

        /**
         * Iodine Function: _setWarnMask (val)
         * Description: Internal low level function for settings the current warning mask
         * See modules/warnings.id
         */
        IodineObject SetWarnMask (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length == 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            var value = args [0] as IodineInteger;

            if (value == null) {
                vm.RaiseException (new IodineTypeException ("Int"));
                return null;
            }

            vm.Context.WarningFilter = (WarningType)value.Value;
            return null;
        }
    }
}


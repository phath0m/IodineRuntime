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
using System.IO;
using System.Diagnostics;

namespace Iodine.Runtime
{
    [BuiltinDocString (
        "Provides a portable way for interacting with and creating processes"
    )]
    [IodineBuiltinModule ("psutils")]
    public class PsUtilsModule : IodineModule
    {
        /// <summary>
        /// IodineProc, a process on the host operating system
        /// </summary>
        class IodineProc : IodineObject
        {
            // Note: Having this much nesting really bothers me, but, I can't
            // really think of a better way to do this. Anonymous classes would
            // be a cool thing in C# for singletons
            sealed class ProcTypeDef : IodineTypeDefinition
            {
                public ProcTypeDef ()
                    : base ("Process")
                {
                    SetDocumentation ("An active process",
                                      "Note: This class cannot be instantiated directly");
                    BindAttributes (this);
                }

                public override IodineObject BindAttributes (IodineObject obj)
                {
                    obj.SetAttribute ("kill", new BuiltinMethodCallback (Kill, obj));
                    return obj;
                }

                [BuiltinDocString ("Attempts to kill the associated process.")]
                static IodineObject Kill (
                    VirtualMachine vm,
                    IodineObject self,
                    IodineObject [] args)
                {
                    var thisObj = self as IodineProc;

                    if (thisObj == null) {
                        vm.RaiseException (new IodineFunctionInvocationException ());
                        return null;
                    }

                    thisObj.Value.Kill ();

                    return null;
                }
            }

            public static new readonly IodineTypeDefinition TypeDef = new ProcTypeDef ();

            public readonly Process Value;

            public IodineProc (Process proc)
                : base (TypeDef)
            {
                Value = proc;
                SetAttribute ("id", new IodineInteger (proc.Id));
                SetAttribute ("name", new IodineString (proc.ProcessName));
            }
        }

        /// <summary>
        /// Subprocess, a process spawned by iodine
        /// </summary>
        class IodineSubprocess : IodineObject
        {
            sealed class SubProcessTypeDef : IodineTypeDefinition
            {
                public SubProcessTypeDef ()
                    : base ("Subprocess")
                {
                    SetDocumentation ("A subprocess spawned from ```psutils.popen```",
                                      "**Note**: This class cannot be instantiated directly");
                    BindAttributes (this);
                }

                public override IodineObject BindAttributes (IodineObject obj)
                {
                    obj.SetAttribute ("write", new BuiltinMethodCallback (Write, obj));
                    obj.SetAttribute ("writeln", new BuiltinMethodCallback (Writeln, obj));
                    obj.SetAttribute ("read", new BuiltinMethodCallback (Read, obj));
                    obj.SetAttribute ("readln", new BuiltinMethodCallback (Readln, obj));
                    obj.SetAttribute ("kill", new BuiltinMethodCallback (Kill, obj));
                    obj.SetAttribute ("empty", new BuiltinMethodCallback (Empty, obj));
                    obj.SetAttribute ("alive", new BuiltinMethodCallback (Alive, obj));
                    return base.BindAttributes (obj);
                }

                [BuiltinDocString (
                    "Writes each string passed in *args to the process's standard input stream",
                    "@param *args Arguments to be written to this process's standard input"
                )]
                IodineObject Write (VirtualMachine vm, IodineObject self, IodineObject [] args)
                {
                    var thisObj = self as IodineSubprocess;

                    if (thisObj == null) {
                        vm.RaiseException (new IodineFunctionInvocationException ());
                        return null;
                    }

                    foreach (IodineObject obj in args) {
                        var str = obj as IodineString;

                        if (str == null) {
                            vm.RaiseException (new IodineTypeException ("Str"));
                            return null;
                        }

                        thisObj.StdinWriteString (vm, str.Value);
                    }
                    return null;
                }

                [BuiltinDocString (
                    "Writes each string passed in *args to the process's standard input stream and appends a new line",
                    "@param *args Arguments to be written to this process's standard input"
                )]
                IodineObject Writeln (VirtualMachine vm, IodineObject self, IodineObject [] args)
                {
                    var thisObj = self as IodineSubprocess;

                    if (thisObj == null) {
                        vm.RaiseException (new IodineFunctionInvocationException ());
                        return null;
                    }

                    foreach (IodineObject obj in args) {
                        var str = obj as IodineString;

                        if (str == null) {
                            vm.RaiseException (new IodineTypeException ("Str"));
                            return null;
                        }

                        thisObj.StdinWriteString (vm, str.Value + "\n");

                    }
                    return null;
                }

                [BuiltinDocString (
                    "Reads a single line from the process's standard output stream."
                )]
                IodineObject Readln (VirtualMachine vm, IodineObject self, IodineObject [] args)
                {
                    var thisObj = self as IodineSubprocess;

                    if (thisObj == null) {
                        vm.RaiseException (new IodineFunctionInvocationException ());
                        return null;
                    }

                    return new IodineString (thisObj.Value.StandardOutput.ReadLine ());
                }

                [BuiltinDocString (
                    "Reads all text written to the process's standard output stream."
                )]
                IodineObject Read (VirtualMachine vm, IodineObject self, IodineObject [] args)
                {
                    var thisObj = self as IodineSubprocess;

                    if (thisObj == null) {
                        vm.RaiseException (new IodineFunctionInvocationException ());
                        return null;
                    }

                    var stderrOutput = thisObj.Value.StandardError.ReadToEnd ();
                    var stdoutOutput = thisObj.Value.StandardOutput.ReadToEnd ();

                    return new IodineString (stderrOutput + stdoutOutput);
                }

                [BuiltinDocString ("Attempts to kill the associated process.")]
                static IodineObject Kill (
                    VirtualMachine vm,
                    IodineObject self,
                    IodineObject [] args)
                {
                    var thisObj = self as IodineSubprocess;

                    if (thisObj == null) {
                        vm.RaiseException (new IodineFunctionInvocationException ());
                        return null;
                    }

                    thisObj.Value.Kill ();

                    return null;
                }

                [BuiltinDocString ("Returns true if the process is alive.")]
                IodineObject Alive (VirtualMachine vm, IodineObject self, IodineObject [] args)
                {
                    var thisObj = self as IodineSubprocess;

                    if (thisObj == null) {
                        vm.RaiseException (new IodineFunctionInvocationException ());
                        return null;
                    }

                    return IodineBool.Create (thisObj.Value.HasExited);
                }

                [BuiltinDocString ("Returns true if there is no more data to be read from stdout.")]
                IodineObject Empty (VirtualMachine vm, IodineObject self, IodineObject [] args)
                {
                    var thisObj = self as IodineSubprocess;

                    if (thisObj == null) {
                        vm.RaiseException (new IodineFunctionInvocationException ());
                        return null;
                    }

                    return IodineBool.Create (thisObj.Value.StandardOutput.Peek () < 0);
                }
            }

            public static new IodineTypeDefinition TypeDef = new SubProcessTypeDef ();

            public readonly Process Value;

            bool canRead;
            bool canWrite;

            public IodineSubprocess (Process proc, bool read, bool write)
                : base (TypeDef)
            {
                canRead = read;
                canWrite = write;
                Value = proc;
                SetAttribute ("id", new IodineInteger (proc.Id));
                SetAttribute ("name", new IodineString (proc.ProcessName));
            }

            public override void Exit (VirtualMachine vm)
            {
                if (canRead) {
                    Value.StandardOutput.Close ();
                    Value.StandardError.Close ();
                }

                if (canWrite) {
                    Value.StandardInput.Close ();
                }
            }

            void StdinWriteString (VirtualMachine vm, string str)
            {
                Value.StandardInput.Write (str);
            }
        }

        public PsUtilsModule ()
            : base ("psutils")
        {

            SetAttribute ("spawn", new BuiltinMethodCallback (Spawn, this));
            SetAttribute ("popen", new BuiltinMethodCallback (Popen, this));
            SetAttribute ("proctable", new InternalIodineProperty (GetProcList, null));
            SetAttribute ("Subprocess", IodineSubprocess.TypeDef);
            SetAttribute ("Process", IodineProc.TypeDef);
        }

        [BuiltinDocString ("Returns a list of processes running on the machine.")]
        IodineObject GetProcList (VirtualMachine vm)
        {
            var list = new IodineList (new IodineObject [] { });
            foreach (Process proc in Process.GetProcesses ()) {
                try {
                    list.Add (new IodineProc (proc));
                } catch {
                    // Why are we doing this? Well on some platforms this loop
                    // is very slow and some processes could have exited, causing
                    // an exception to be thrown.
                    continue;
                }
            }
            return list;
        }

        [BuiltinDocString (
            "Spawns a new process.",
            "@param executable The executable to run",
            "@param [args] Command line arguments",
            "@param [wait] Should we wait to exit"
        )]
        IodineObject Spawn (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
            }

            var str = args [0] as IodineString;

            string cmdArgs = "";
            bool wait = true;

            if (str == null) {
                vm.RaiseException (new IodineTypeException ("Str"));
                return null;
            }

            if (args.Length >= 2) {
                var cmdArgsObj = args [1] as IodineString;
                if (cmdArgsObj == null) {
                    vm.RaiseException (new IodineTypeException ("Str"));
                    return null;
                }
                cmdArgs = cmdArgsObj.Value;
            }

            if (args.Length >= 3) {
                var waitObj = args [2] as IodineBool;
                if (waitObj == null) {
                    vm.RaiseException (new IodineTypeException ("Bool"));
                    return null;
                }
                wait = waitObj.Value;
            }

            var info = new ProcessStartInfo (str.Value, cmdArgs);

            info.UseShellExecute = false;

            return new IodineProc (Process.Start (info));
        }

        [BuiltinDocString (
            "Opens up a new process, returning a Proc object.",
            "@param commmand Command to run.",
            "@param mode Mode to open up the process in ('r' or 'w')."
        )]
        IodineObject Popen (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length < 2) {
                vm.RaiseException (new IodineArgumentException (2));
                return null;
            }
            var command = args [0] as IodineString;
            var mode = args [1] as IodineString;

            if (command == null || mode == null) {
                vm.RaiseException (new IodineTypeException ("Str"));
                return null;
            }

            bool read = false;
            bool write = false;

            foreach (char c in mode.Value) {
                switch (c) {
                case 'r':
                    read = true;
                    break;
                case 'w':
                    write = true;
                    break;
                }

            }

            bool isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT
                             || Environment.OSVersion.Platform == PlatformID.Win32S
                             || Environment.OSVersion.Platform == PlatformID.Win32Windows
                             || Environment.OSVersion.Platform == PlatformID.WinCE
                             || Environment.OSVersion.Platform == PlatformID.Xbox;

            if (isWindows) {
                return new IodineSubprocess (Popen_Win32 (command.Value, read, write), read, write);
            } else {
                var proc = Popen_Unix (command.Value, read, write);
                proc.Start ();
                return new IodineSubprocess (proc, read, write);
            }

        }

        public static Process Popen_Win32 (string command, bool read, bool write)
        {
            var systemPath = Environment.GetFolderPath (Environment.SpecialFolder.System);
            var args = String.Format ("/K \"{0}\"", command);
            var info = new ProcessStartInfo (Path.Combine (systemPath, "cmd.exe"), args);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = read;
            info.RedirectStandardError = read;
            info.RedirectStandardInput = write;
            var proc = new Process ();
            proc.StartInfo = info;
            return proc;
        }

        public static  Process Popen_Unix (string command, bool read, bool write)
        {
            var args = String.Format ("-c \"{0}\"", command);
            var info = new ProcessStartInfo ("/bin/sh", args);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = read;
            info.RedirectStandardError = read;
            info.RedirectStandardInput = write;
            var proc = new Process ();
            proc.StartInfo = info;
            return proc;
        }
    }
}


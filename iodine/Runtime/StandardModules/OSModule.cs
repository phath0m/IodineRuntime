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
using System.Collections.Generic;

namespace Iodine.Runtime
{
    [BuiltinDocString (
        "Provides a portable way for interacting with the host operating system"
    )]
    [IodineBuiltinModule ("os")]
    public class OSModule : IodineModule
    {
        
        public OSModule ()
            : base ("os")
        {
            SetAttribute ("USER_DIR", new IodineString (
                Environment.GetFolderPath (Environment.SpecialFolder.UserProfile))
            );
            SetAttribute ("ENV_SEP", new IodineString (Path.PathSeparator.ToString ()));
            SetAttribute ("SEEK_SET", new IodineInteger (IodineStream.SEEK_SET));
            SetAttribute ("SEEK_CUR", new IodineInteger (IodineStream.SEEK_CUR));
            SetAttribute ("SEEK_END", new IodineInteger (IodineStream.SEEK_END));

            SetAttribute ("call", new BuiltinMethodCallback (Call, this));
            SetAttribute ("putenv", new BuiltinMethodCallback (SetEnv, this));
            SetAttribute ("getenv", new BuiltinMethodCallback (GetEnv, this));
            SetAttribute ("getcwd", new BuiltinMethodCallback (GetCwd, this));
            SetAttribute ("setcwd", new BuiltinMethodCallback (SetCwd, this));
            SetAttribute ("getlogin", new BuiltinMethodCallback (GetUsername, this));
            SetAttribute ("system", new BuiltinMethodCallback (System, this));
            SetAttribute ("unlink", new BuiltinMethodCallback (Unlink, this));
            SetAttribute ("mkdir", new BuiltinMethodCallback (Mkdir, this));
            SetAttribute ("rmdir", new BuiltinMethodCallback (Rmdir, this));
            SetAttribute ("rmtree", new BuiltinMethodCallback (Rmtree, this));
            SetAttribute ("list", new BuiltinMethodCallback (List, this));
        }


        [BuiltinDocString (
            "Executes program, waiting for it to exit and returning its exit code.",
            "@param executable The executable to run.",
            "@optional args Command line arguments.",
            "@optional useShell Should the OS shell be used to invoke the executable."
        )]
        IodineObject Call (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length < 1) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            var program = args [0] as IodineString;

            string arguments = "";

            bool useShell = false;

            if (program == null) {
                vm.RaiseException (new IodineTypeException ("Str"));
                return null;
            }

            if (args.Length > 1) {

                var argObj = args [1] as IodineString;

                if (argObj == null) {
                    vm.RaiseException (new IodineTypeException ("Str"));
                    return null;
                }

                arguments = argObj.Value;
            }

            if (args.Length > 2) {
                var useShellObj = args [1] as IodineBool;

                if (useShellObj == null) {
                    vm.RaiseException (new IodineTypeException ("Bool"));
                    return null;
                }

                useShell = useShellObj.Value;
            }

            var info = new ProcessStartInfo ();
            info.FileName = program.Value;
            info.Arguments = arguments;
            info.UseShellExecute = useShell;

            var proc = Process.Start (info);

            proc.WaitForExit ();

            return new IodineInteger (proc.ExitCode);

        }


        [BuiltinDocString ("Returns the login name of the current user.")]
        IodineObject GetUsername (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            return new IodineString (Environment.UserName);
        }

        [BuiltinDocString ("Returns the current working directory.")]
        IodineObject GetCwd (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            return new IodineString (Environment.CurrentDirectory);
        }

        [BuiltinDocString (
            "Sets the current working directory.",
            "@param cwd The new current working directory."
        )]
        IodineObject SetCwd (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            var cwd = args [0] as IodineString;

            if (cwd == null) {
                vm.RaiseException (new IodineTypeException ("Str"));
                return null;
            }

            Environment.CurrentDirectory = args [0].ToString ();
            return null;
        }

        [BuiltinDocString (
            "Returns the value of an environmental variable.",
            "@param env The name of the environmental variable."
        )]
        IodineObject GetEnv (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            var str = args [0] as IodineString;

            if (str == null) {
                vm.RaiseException (new IodineTypeException ("Str"));
                return null;
            }

            if (Environment.GetEnvironmentVariable (str.Value) != null) {
                return new IodineString (Environment.GetEnvironmentVariable (str.Value));
            }

            return null;
        }

        [BuiltinDocString (
            "Sets an environmental variable to a specified value",
            "@param env The name of the environmental variable.",
            "@param value The value to set the environmental variable."
        )]
        IodineObject SetEnv (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length < 2) {
                vm.RaiseException (new IodineArgumentException (2));
            }

            var str = args [0] as IodineString;

            if (str == null) {
                vm.RaiseException (new IodineTypeException ("Str"));
                return null;
            }

            Environment.SetEnvironmentVariable (str.Value, args [1].ToString (), EnvironmentVariableTarget.User);
            return null;
        }


        [BuiltinDocString (
            "Executes a command using the default shell.",
            "@param commmand Command to run."
        )]
        IodineObject System (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length < 1) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }


            var command = args [0] as IodineString;

            if (command == null) {
                vm.RaiseException (new IodineTypeException ("Str"));
                return null;
            }

            bool isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT
                             || Environment.OSVersion.Platform == PlatformID.Win32S
                             || Environment.OSVersion.Platform == PlatformID.Win32Windows
                             || Environment.OSVersion.Platform == PlatformID.WinCE
                             || Environment.OSVersion.Platform == PlatformID.Xbox;


            Process proc = null;

            if (isWindows) {
                proc = PsUtilsModule.Popen_Win32 (command.Value, false, false);
            } else {
                proc = PsUtilsModule.Popen_Unix (command.Value, false, false);
            }

            proc.Start ();

            proc.WaitForExit ();

            return new IodineInteger (proc.ExitCode);
        }

        [BuiltinDocString (
            "Removes a file from the filesystem.",
            "@param path The file to delete."
        )]
        IodineObject Unlink (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            var fileString = args [0] as IodineString;

            if (fileString == null) {
                vm.RaiseException (new IodineTypeException ("Str"));
                return null;
            }

            if (File.Exists (fileString.Value)) {
                File.Delete (fileString.Value);
            } else {
                vm.RaiseException (new IodineIOException ("File not found!"));
                return null;
            }
            return null;
        }

        [BuiltinDocString (
            "Creates a new directory.",
            "@param path The directory to create."
        )]
        IodineObject Mkdir (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            if (!(args [0] is IodineString)) {
                vm.RaiseException (new IodineTypeException ("Str"));
                return null;
            }
            Directory.CreateDirectory (args [0].ToString ());
            return null;
        }

        [BuiltinDocString (
            "Removes an empty directory.",
            "@param path The directory to remove."
        )]
        IodineObject Rmdir (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            var path = args [0] as IodineString;

            if (path == null) {
                vm.RaiseException (new IodineTypeException ("Str"));
                return null;
            }

            if (!Directory.Exists (path.Value)) {
                vm.RaiseException (new IodineIOException ("Directory '" + args [0].ToString () +
                "' does not exist!"
                ));
                return null;
            }

            Directory.Delete (path.Value);

            return null;
        }

        [BuiltinDocString (
            "Removes an directory, deleting all subfiles.",
            "@param path The directory to remove."
        )]
        IodineObject Rmtree (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            var path = args [0] as IodineString;

            if (path == null) {
                vm.RaiseException (new IodineTypeException ("Str"));
                return null;
            }

            if (!Directory.Exists (path.Value)) {
                vm.RaiseException (new IodineIOException ("Directory '" + args [0].ToString () +
                "' does not exist!"
                ));
                return null;
            }

            RemoveRecursive (path.Value);

            return null;
        }

        /*
         * Recurisively remove a directory
         */
        static bool RemoveRecursive (string target)
        {
            var dir = new DirectoryInfo (target);
            var dirs = dir.GetDirectories ();

            if (!dir.Exists) {
                return false;
            }

            var files = dir.GetFiles ();
            foreach (FileInfo file in files) {
                var temppath = Path.Combine (target, file.Name);
                File.Delete (temppath);
            }

            foreach (DirectoryInfo subdir in dirs) {
                var temppath = Path.Combine (target, subdir.Name);
                RemoveRecursive (temppath);
            }
            Directory.Delete (target);
            return true;
        }

        [BuiltinDocString (
            "Returns a list of all subfiles in a directory.",
            "@param path The directory to list."
        )]
        IodineObject List (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length < 1) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            var dir = args [0] as IodineString;

            if (dir == null) {
                vm.RaiseException (new IodineTypeException ("Str"));
                return null;
            }

            if (!Directory.Exists (dir.Value)) {
                vm.RaiseException (new IodineIOException ("Directory does not exist"));
                return null;
            }

            var items = new List<string> ();

            items.AddRange (Directory.GetFiles (dir.Value));
            items.AddRange (Directory.GetDirectories (dir.Value));

            var retList = new IodineList (new IodineObject [] { });

            items.ForEach (p => retList.Add (new IodineString (p)));

            return retList;
        }
    }
}


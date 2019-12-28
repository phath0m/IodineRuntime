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

using System.IO;
using System.Collections.Generic;

namespace Iodine.Runtime
{
    [BuiltinDocString (
        "Provides miscellaneous functions for interacting with the host operating system's filesystem."
    )]
    [IodineBuiltinModule ("fsutils")]
    public class FSUtilsModule : IodineModule
    {
        public FSUtilsModule ()
            : base ("fsutils")
        {
            SetAttribute ("copy", new BuiltinMethodCallback (Copy, this));
            SetAttribute ("copytree", new BuiltinMethodCallback (Copytree, this));
            SetAttribute ("exists", new BuiltinMethodCallback (Exists, this));
            SetAttribute ("isdir", new BuiltinMethodCallback (IsDir, this));
            SetAttribute ("isfile", new BuiltinMethodCallback (IsFile, this));
            SetAttribute ("read", new BuiltinMethodCallback (Read, this));
            SetAttribute ("readbytes", new BuiltinMethodCallback (ReadBytes, this));
            SetAttribute ("readlines", new BuiltinMethodCallback (ReadLines, this));

            SetAttribute ("ctime", new BuiltinMethodCallback (GetCreationTime, this));
            SetAttribute ("atime", new BuiltinMethodCallback (GetModifiedTime, this));
        }

        [BuiltinDocString (
            "Copies a file.",
            "@param src The source file.",
            "@param dest The destination file."
        )]
        IodineObject Copy (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length < 2) {
                vm.RaiseException (new IodineArgumentException (2));
                return null;
            }

            var src = args [0] as IodineString;
            var dest = args [1] as IodineString;

            if (dest == null || src == null) {
                vm.RaiseException (new IodineTypeException ("Str"));
                return null;
            }

            if (File.Exists (src.Value)) {
                File.Copy (src.Value, dest.Value, true);
            } else if (Directory.Exists (src.Value)) {
                CopyDir (src.Value, dest.Value, false);
            } else {
                vm.RaiseException (new IodineIOException ("File does not exist"));
            }
            return null;
        }

        [BuiltinDocString (
            "Copies a directory.",
            "@param src The source directory.",
            "@param dest The destination directory."
        )]
        IodineObject Copytree (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length < 2) {
                vm.RaiseException (new IodineArgumentException (2));
                return null;
            }

            var src = args [0] as IodineString;
            var dest = args [1] as IodineString;

            if (dest == null || src == null) {
                vm.RaiseException (new IodineTypeException ("Str"));
                return null;
            }

            CopyDir (src.Value, dest.Value, true);

            return null;
        }

        [BuiltinDocString (
            "Returns true if a file or directory exist.",
            "@param path The file name."
        )]
        IodineObject Exists (VirtualMachine vm, IodineObject self, IodineObject [] args)
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

            return IodineBool.Create (Directory.Exists (path.Value) || File.Exists (path.Value));
        }

        [BuiltinDocString (
            "Returns true if a path string is a directory.",
            "@param path The path to test."
        )]
        IodineObject IsDir (VirtualMachine vm, IodineObject self, IodineObject [] args)
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

            return IodineBool.Create (Directory.Exists (path.Value));
        }

        [BuiltinDocString (
            "Returns true if a path string is a file.",
            "@param path The path to test."
        )]
        IodineObject IsFile (VirtualMachine vm, IodineObject self, IodineObject [] args)
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

            return IodineBool.Create (File.Exists (path.Value));
        }

        [BuiltinDocString (
            "Reads all text from a file, returning a string.",
            "@param path The file to read."
        )]
        IodineObject Read (VirtualMachine vm, IodineObject self, IodineObject [] args)
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

            if (!File.Exists (path.Value)) {
                vm.RaiseException (new IodineIOException ("File does not exist"));
                return null;
            }

            return new IodineString (File.ReadAllText (path.Value));
        }

        [BuiltinDocString (
            "Reads all bytes from a file, returning a byte string.",
            "@param path The file to read."
        )]
        IodineObject ReadBytes (VirtualMachine vm, IodineObject self, IodineObject [] args)
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

            if (!File.Exists (path.Value)) {
                vm.RaiseException (new IodineIOException ("File does not exist"));
                return null;
            }

            return new IodineBytes (File.ReadAllBytes (path.Value));
        }

        [BuiltinDocString (
            "Reads all lines from a file, returning a new list.",
            "@param path The file to read."
        )]
        IodineObject ReadLines (VirtualMachine vm, IodineObject self, IodineObject [] args)
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

            if (!File.Exists (path.Value)) {
                vm.RaiseException (new IodineIOException ("File does not exist"));
                return null;
            }

            var lines = new List<IodineObject> ();

            foreach (string line in File.ReadAllLines (path.Value)) {
                lines.Add (new IodineString (line));
            }

            return new IodineList (lines);
        }

        [BuiltinDocString (
            "Returns the time this file was last accessed.",
            "@param file The file in question."
        )]
        IodineObject GetModifiedTime (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }
            if (!(args [0] is IodineString)) {
                vm.RaiseException (new IodineTypeException ("Str"));
                return null;

            }
            if (!File.Exists (args [0].ToString ())) {
                vm.RaiseException (new IodineIOException ("File '" + args [0].ToString () +
                "' does not exist!"));
                return null;
            }
            return new DateTimeModule.IodineTimeStamp (File.GetLastAccessTime (args [0].ToString ()));
        }

        [BuiltinDocString (
            "Returns the time this file was created.",
            "@param file The file in question."
        )]
        IodineObject GetCreationTime (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            if (!(args [0] is IodineString)) {
                vm.RaiseException (new IodineTypeException ("Str"));
                return null;
            }
            if (!File.Exists (args [0].ToString ())) {
                vm.RaiseException (new IodineIOException ("File '" + args [0].ToString () +
                "' does not exist!"));
                return null;
            }
            return new DateTimeModule.IodineTimeStamp (File.GetCreationTime (args [0].ToString ()));
        }

        static bool CopyDir (string src, string dest, bool recurse)
        {
            var dir = new DirectoryInfo (src);
            var dirs = dir.GetDirectories ();

            if (!dir.Exists) {
                return false;
            }

            if (!Directory.Exists (dest)) {
                Directory.CreateDirectory (dest);
            }

            var files = dir.GetFiles ();
            foreach (FileInfo file in files) {
                var temppath = Path.Combine (dest, file.Name);
                file.CopyTo (temppath, false);
            }

            if (recurse) {
                foreach (DirectoryInfo subdir in dirs) {
                    var temppath = Path.Combine (dest, subdir.Name);
                    CopyDir (subdir.FullName, temppath, recurse);
                }
            }
            return true;
        }
    }
}


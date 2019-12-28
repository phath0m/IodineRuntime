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
using System.Text;
using System.Collections.Generic;

namespace Iodine.Runtime
{
    // TODO: Implement binary mode
    public class IodineStream : IodineObject
    {
        public const int SEEK_SET = 0;
        public const int SEEK_CUR = 1;
        public const int SEEK_END = 2;

        public static readonly IodineTypeDefinition TypeDefinition = new IodineFileTypeDef ();

        sealed class IodineFileTypeDef : IodineTypeDefinition
        {
            public IodineFileTypeDef () 
                : base ("File")
            {
                BindAttributes (this);

                SetDocumentation (
                    "An object supporting read or write operations (Typically a file)"
                );
            }

            public override IodineObject BindAttributes (IodineObject newFile)
            {
                newFile.SetAttribute ("write", new BuiltinMethodCallback (Write, newFile));
                newFile.SetAttribute ("writeln", new BuiltinMethodCallback (Writeln, newFile));
                newFile.SetAttribute ("read", new BuiltinMethodCallback (Read, newFile));
                newFile.SetAttribute ("readln", new BuiltinMethodCallback (Readln, newFile));
                newFile.SetAttribute ("close", new BuiltinMethodCallback (Close, newFile));
                newFile.SetAttribute ("flush", new BuiltinMethodCallback (Flush, newFile));
                newFile.SetAttribute ("readall", new BuiltinMethodCallback (ReadAll, newFile));
                return newFile;
            }

            [BuiltinDocString (
                "Writes an object to the underlying stream.",
                "@param obj The object to be written."
            )]
            private IodineObject Write (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineStream;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }


                if (thisObj.Closed) { 
                    vm.RaiseException (new IodineIOException ("Stream has been closed!"));
                    return null;
                }

                if (!thisObj.CanWrite) {
                    vm.RaiseException (new IodineIOException ("Can not write to stream!"));
                    return null;
                }

                foreach (IodineObject obj in args) {
                    thisObj.Write (obj);
                }
                return null;
            }

            [BuiltinDocString (
                "Writes an object to the stream, appending a new line character to the end of the file.",
                "@param obj The object to be written."
            )]
            private IodineObject Writeln (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineStream;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                if (thisObj.Closed) { 
                    vm.RaiseException (new IodineIOException ("Stream has been closed!"));
                    return null;
                }

                if (!thisObj.CanWrite) {
                    vm.RaiseException (new IodineIOException ("Can not write to stream!"));
                    return null;
                }

                foreach (IodineObject obj in args) {
                    if (!thisObj.Write (obj)) {
                        vm.RaiseException (new IodineNotSupportedException (
                            "The requested type is not supported"
                        ));
                        return null;
                    }
                    foreach (byte b in Environment.NewLine) {
                        thisObj.File.WriteByte (b);
                    }
                }
                return null;
            }

            [BuiltinDocString (
                "Reports the current position of the underlying stream"
            )]
            private IodineObject Tell (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineStream;
                if (thisObj.Closed) { 
                    vm.RaiseException ("Stream has been closed!");
                    return null;
                }
                return new IodineInteger (thisObj.File.Position);
            }

            [BuiltinDocString (
                "Sets the position of the stream to a specified value, relative to [whence]",
                "@param whence The origin to use, possible values are SEEK_SET, SEEK_CUR, and SEEK_END"
            )]
            private IodineObject Seek (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineStream;
                if (args.Length == 0) {
                    vm.RaiseException (new IodineArgumentException (1));
                    return null;
                }

                if (thisObj.Closed) {
                    vm.RaiseException (new IodineException ("The underlying stream has been closed!"));
                    return null;
                }

                if (!thisObj.File.CanSeek) {
                    vm.RaiseException (new IodineIOException ("The stream does not support seek"));
                    return null;
                }

                var offsetObj = args [0] as IodineInteger;
                int whence = SEEK_SET;
                long offset = offsetObj.Value;

                if (offsetObj == null) {
                    vm.RaiseException (new IodineTypeException ("Int"));
                    return null;
                }

                if (args.Length > 1) {
                    var whenceObj = args [1] as IodineInteger;

                    if (whenceObj == null) {
                        vm.RaiseException (new IodineTypeException ("Int"));
                        return null;
                    }

                    whence = (int)whenceObj.Value;
                }

                switch (whence) {
                case SEEK_SET:
                    thisObj.File.Position = offset;
                    break;
                case SEEK_CUR:
                    thisObj.File.Seek (offset, SeekOrigin.Current);
                    break;
                case SEEK_END:
                    thisObj.File.Seek (offset, SeekOrigin.End);
                    break;
                default:
                    vm.RaiseException (new IodineNotSupportedException ());
                    return null;
                }
                return null;
            }

            [BuiltinDocString (
                "Closes the stream."
            )]
            private IodineObject Close (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineStream;
                if (thisObj.Closed) { 
                    vm.RaiseException ("Stream has been closed!");
                    return null;
                }
                thisObj.File.Close ();
                return null;
            }

            [BuiltinDocString (
                "Flushes the underlying stream."
            )]
            private IodineObject Flush (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineStream;
                if (thisObj.Closed) { 
                    vm.RaiseException ("Stream has been closed!");
                    return null;
                }
                thisObj.File.Flush ();
                return null;
            }

            [BuiltinDocString (
                "Reads all text."
            )]
            private IodineObject ReadAll (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineStream;

                if (thisObj.Closed) { 
                    vm.RaiseException ("Stream has been closed!");
                    return null;
                }

                var bytes = new List<byte> ();
                int ch = 0;
                while ((ch = thisObj.File.ReadByte ()) != -1) {
                    bytes.Add ((byte)ch);
                }

                if (thisObj.BinaryMode) {
                    return new IodineBytes (bytes.ToArray ());
                }
                return new IodineString (Encoding.UTF8.GetString (bytes.ToArray ()));
            }

            [BuiltinDocString (
                "Reads a single line from the underlying stream."
            )]
            private IodineObject Readln (VirtualMachine vm, IodineObject self, IodineObject[] argss)
            {
                var thisObj = self as IodineStream;
                if (thisObj.Closed) { 
                    vm.RaiseException ("Stream has been closed!");
                    return null;
                }

                if (!thisObj.CanRead) {
                    vm.RaiseException ("Stream is not open for reading!");
                    return null;
                }

                return thisObj.ReadLine ();
            }

            [BuiltinDocString (
                "Reads [n] bytes from the underlying stream.",
                "@param n How many bytes to read"
            )]
            private IodineObject Read (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineStream;
                if (thisObj.Closed) { 
                    vm.RaiseException ("Stream has been closed!");
                    return null;
                }

                if (!thisObj.CanRead) {
                    vm.RaiseException ("Stream is not open for reading!");
                    return null;
                }

                if (args.Length > 0) {
                    var intv = args [0] as IodineInteger;

                    if (intv == null) {
                        vm.RaiseException (new IodineTypeException ("Int"));
                        return null;
                    }

                    byte[] buf = new byte[(int)intv.Value];
                    thisObj.File.Read (buf, 0, buf.Length);
                    return new IodineString (Encoding.UTF8.GetString (buf));
                } else {
                    vm.RaiseException (new IodineArgumentException (1));
                    return null;
                }
            }

        }

        public bool Closed { set; get; }

        public Stream File { private set; get; }

        public bool CanRead { private set; get; }

        public bool CanWrite { private set; get; }

        public bool BinaryMode { private set; get; }

        public IodineStream (Stream file, bool canWrite, bool canRead, bool binary = false)
            : base (TypeDefinition)
        {
            File = file;
            CanRead = canRead;
            CanWrite = canWrite;
        }

        public override IodineObject Len (VirtualMachine vm)
        {
            return new IodineInteger (File.Length);
        }

        public override void Exit (VirtualMachine vm)
        {
            if (!Closed) {
                File.Close ();
                File.Dispose ();
            }
        }

        public bool Write (IodineObject obj)
        {
            if (obj is IodineString) {
                Write (obj.ToString ());
            } else if (obj is IodineBytes) {
                var arr = obj as IodineBytes;
                File.Write (arr.Value, 0, arr.Value.Length);
            } else if (obj is IodineInteger) {
                var intVal = obj as IodineInteger;
                Write ((byte)intVal.Value);
            } else {
                return false;
            }

            return true;
        }

        private IodineObject Read (int n)
        {
            byte[] buf = new byte[n];
            File.Read (buf, 0, buf.Length);

            if (BinaryMode) {
                return new IodineBytes (buf);
            }
            return new IodineString (Encoding.UTF8.GetString (buf));
        }


        public IodineObject ReadLine ()
        {
            var bytes = new List<byte> ();
            int ch = 0;
            while ((ch = File.ReadByte ()) != '\n' && ch != -1) {
                bytes.Add ((byte)ch);
            }

            if (BinaryMode) {
                return new IodineBytes (bytes.ToArray ());
            }
            return new IodineString (Encoding.UTF8.GetString (bytes.ToArray ()));
        }

        private void Write (string str)
        {
            foreach (char c in str) {
                File.WriteByte ((byte)c);
            }
        }

        public void Write (byte b)
        {
            File.WriteByte (b);
        }
    }
}


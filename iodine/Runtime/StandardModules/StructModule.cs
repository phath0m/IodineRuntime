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
    [BuiltinDocString (
        "Provides functions for converting iodine types to C structs."
    )]
    [IodineBuiltinModule ("struct")]
    public class StructModule : IodineModule
    {
        public StructModule ()
            : base ("struct")
        {
            SetAttribute ("pack", new BuiltinMethodCallback (Pack, this));
            SetAttribute ("unpack", new BuiltinMethodCallback (Unpack, this));
            SetAttribute ("getsize", new BuiltinMethodCallback (GetSize, this));
        }

        [BuiltinDocString (
            "Packs a struct from a supplied format specifier and tuple.",
            "@param format The format describing how [tuple] is to be packed into the struct.",
            "@param tuple A tuple containing all the items which are to be packed into the struct."
        )]
        private IodineObject Pack (VirtualMachine vm, IodineObject self, IodineObject[] args)
        {
            if (args.Length < 2) {
                vm.RaiseException (new IodineArgumentException (2));
                return null;
            }

            var format = args [0] as IodineString;
            var tuple = args [1] as IodineTuple;

            if (format == null) {
                vm.RaiseException (new IodineTypeException ("Str"));
                return null;
            }

            if (tuple == null) {
                vm.RaiseException (new IodineTypeException ("Tuple"));
                return null;
            }
            int nextObj = 0;
            using (MemoryStream ms = new MemoryStream ())
            using (BinaryWriter bw = new BinaryWriter (ms)) {
                int i = 0;
                while (i < format.Value.Length) {
                    int arg = 1;
                    if (i < format.Value.Length && char.IsDigit (format.Value [i])) {
                        var accum = new StringBuilder ();
                        do {
                            accum.Append (format.Value [i++]);
                        } while (i < format.Value.Length && char.IsDigit (format.Value [i]));
                        arg = Int32.Parse (accum.ToString ());
                    }
                    if (i < format.Value.Length) {
                        char specifier = format.Value [i++];

                        if (specifier == 'x') {
                            for (int j = 0; j < arg; j++) {
                                bw.Write ((byte)0);
                            }
                        } else {
                            if (nextObj > tuple.Objects.Length) {
                                vm.RaiseException (new IodineException ("Invalid format"));
                                return null;
                            }
                            IodineObject obj = tuple.Objects [nextObj++];
                            if (!PackObj (vm, bw, specifier, arg, obj)) {
                                vm.RaiseException (new IodineException ("Invalid format"));
                                return null;
                            }
                        }
                    }
                }
                return new IodineBytes (ms.ToArray ());
            }
        }

        [BuiltinDocString (
            "Unpacks a struct from a supplied format specifier, returning a tuple.",
            "@param format The format describing how [tuple] is to be packed into the struct.",
            "@param tuple A tuple containing all the items which are to be packed into the struct."
        )]
        private IodineObject Unpack (VirtualMachine vm, IodineObject self, IodineObject[] args)
        {
            if (args.Length < 2) {
                vm.RaiseException (new IodineArgumentException (2));
                return null;
            }
            var format = args [0] as IodineString;
            var str = args [1] as IodineBytes;

            if (format == null) {
                vm.RaiseException (new IodineTypeException ("Str"));
                return null;
            }

            if (str == null) {
                vm.RaiseException (new IodineTypeException ("Tuple"));
                return null;
            }

            var items = new List<IodineObject> ();

            using (MemoryStream ms = new MemoryStream (str.Value))
            using (BinaryReader br = new BinaryReader (ms)) {
                int i = 0;
                while (i < format.Value.Length) {
                    int arg = 1;
                    if (i < format.Value.Length && char.IsDigit (format.Value [i])) {
                        var accum = new StringBuilder ();
                        do {
                            accum.Append (format.Value [i++]);
                        } while (i < format.Value.Length && char.IsDigit (format.Value [i]));
                        arg = Int32.Parse (accum.ToString ());
                    }
                    if (i < format.Value.Length) {
                        char specifier = format.Value [i++];
                        if (i < format.Value.Length && char.IsDigit (format.Value [i])) {
                            var accum = new StringBuilder ();
                            do {
                                accum.Append (format.Value [i++]);
                            } while (i < format.Value.Length && char.IsDigit (format.Value [i]));
                            arg = Int32.Parse (accum.ToString ());
                        }
                        if (specifier == 'x') {
                            for (int j = 0; j < arg; j++) {
                                br.ReadByte ();
                            }
                        } else {
                            items.Add (UnpackObj (vm, br, specifier, arg));
                        }
                    }
                }
                return new IodineTuple (items.ToArray ());
            }
        }

        [BuiltinDocString (
            "Returns the length of a struct format specifier. ",
            "@param format The format specifier."
        )]
        private IodineObject GetSize (VirtualMachine vm, IodineObject self, IodineObject[] args)
        {
            if (args.Length < 1) {
                vm.RaiseException (new IodineArgumentException (2));
                return null;
            }
            var format = args [0] as IodineString;
            int ret = 0;
            int i = 0;
            while (i < format.Value.Length) {
                int arg = 1;
                if (i < format.Value.Length && char.IsDigit (format.Value [i])) {
                    StringBuilder accum = new StringBuilder ();
                    do {
                        accum.Append (format.Value [i++]);
                    } while (i < format.Value.Length && char.IsDigit (format.Value [i]));
                    arg = Int32.Parse (accum.ToString ());
                }
                if (i < format.Value.Length) {
                    char specifier = format.Value [i++];
                    if (i < format.Value.Length && char.IsDigit (format.Value [i])) {
                        var accum = new StringBuilder ();
                        do {
                            accum.Append (format.Value [i++]);
                        } while (i < format.Value.Length && char.IsDigit (format.Value [i]));
                        arg = Int32.Parse (accum.ToString ());
                    }
                    if (specifier == 'x') {
                        ret += arg;
                    } else {
                        ret += GetSize (specifier, arg);
                    }
                }
            }
            return new IodineInteger (ret);
        }


        private bool PackObj (VirtualMachine vm, BinaryWriter bw, char type, int arg, IodineObject obj)
        {
            switch (type) {
            case '?':
                {
                    var val = obj as IodineBool;
                    if (val == null) {
                        vm.RaiseException (new IodineTypeException ("Bool"));
                        return false;
                    } else {
                        bw.Write (val.Value);
                    }
                    break;
                }
            case 'b':
                {
                    var val = obj as IodineInteger;
                    if (val == null) {
                        vm.RaiseException (new IodineTypeException ("Int"));
                        return false;
                    } else {
                        bw.Write ((SByte)val.Value);
                    }
                    break;
                }
            case 'B':
                {
                    var val = obj as IodineInteger;
                    if (val == null) {
                        vm.RaiseException (new IodineTypeException ("Int"));
                        return false;
                    } else {
                        bw.Write ((Byte)val.Value);
                    }
                    break;
                }
            case 'h':
                {
                    var val = obj as IodineInteger;
                    if (val == null) {
                        vm.RaiseException (new IodineTypeException ("Int"));
                        return false;
                    } else {
                        bw.Write ((Int16)val.Value);
                    }
                    break;
                }
            case 'H':
                {
                    var val = obj as IodineInteger;
                    if (val == null) {
                        vm.RaiseException (new IodineTypeException ("Int"));
                        return false;
                    } else {
                        bw.Write ((UInt16)val.Value);
                    }
                    break;
                }
            case 'l':
            case 'i':
                {
                    var val = obj as IodineInteger;
                    if (val == null) {
                        vm.RaiseException (new IodineTypeException ("Int"));
                        return false;
                    } else {
                        bw.Write ((Int32)val.Value);
                    }
                    break;
                }
            case 'I':
            case 'L':
                {
                    var val = obj as IodineInteger;
                    if (val == null) {
                        vm.RaiseException (new IodineTypeException ("Int"));
                        return false;
                    } else {
                        bw.Write ((UInt32)val.Value);
                    }
                    break;
                }
            case 'q':
                {
                    var val = obj as IodineInteger;
                    if (val == null) {
                        vm.RaiseException (new IodineTypeException ("Int"));
                        return false;
                    } else {
                        bw.Write (val.Value);
                    }
                    break;
                }
            case 'Q':
                {
                    var val = obj as IodineInteger;
                    if (val == null) {
                        vm.RaiseException (new IodineTypeException ("Int"));
                        return false;
                    } else {
                        bw.Write ((UInt64)val.Value);
                    }
                    break;
                }
            case 'p':
            case 's':
                {
                    var val = obj as IodineString;
                    if (val == null) {
                        vm.RaiseException (new IodineTypeException ("Str"));
                        return false;
                    }

                    var bytes = Encoding.ASCII.GetBytes (val.ToString ());
                    for (int i = 0; i < arg; i++) {
                        if (i < bytes.Length) {
                            bw.Write (bytes [i]);
                        } else {
                            bw.Write ((byte)0);
                        }
                    }

                    break;
                }
            }
            return true;
        }

        private IodineObject UnpackObj (VirtualMachine vm, BinaryReader br, char type, int arg)
        {
            switch (type) {
            case '?':
                {
                    return IodineBool.Create (br.ReadBoolean ());
                }
            case 'b':
                {
                    return new IodineInteger (br.ReadSByte ());
                }
            case 'B':
                {
                    return new IodineInteger (br.ReadByte ());
                }
            case 'h':
                {
                    return new IodineInteger (br.ReadInt16 ());
                }
            case 'H':
                {
                    return new IodineInteger (br.ReadUInt16 ());
                }
            case 'l':
            case 'i':
                {
                    return new IodineInteger (br.ReadInt32 ());
                }
            case 'I':
            case 'L':
                {
                    return new IodineInteger (br.ReadUInt32 ());
                }
            case 'q':
                {
                    return new IodineInteger (br.ReadInt64 ());
                }
            case 'Q':
                {
                    return new IodineInteger ((long)br.ReadUInt64 ());
                }
            case 'p':
            case 's':
                {
                    return new IodineString (Encoding.ASCII.GetString (br.ReadBytes (arg)));
                }
            }
            return null;
        }

        private int GetSize (char type, int arg)
        {
            switch (type) {
            case '?':
            case 'b':
            case 'B':
                return 1;
            case 'h':
            case 'H':
                return 2;
            case 'l':
            case 'i':
                return 4;
            case 'I':
            case 'L':
                return 4;
            case 'q':
            case 'Q':
                return 8;
            case 'p':
            case 's':
                return arg;
            }
            return 0;
        }
    }
}


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
using Iodine.Compiler;

namespace Iodine.Runtime
{
    public class IodineString : IodineObject
    {
        public static readonly IodineTypeDefinition TypeDefinition = new StringTypeDef ();
        public static readonly IodineString Empty = new IodineString (string.Empty);

        sealed class StringTypeDef : IodineTypeDefinition
        {
            public StringTypeDef ()
                : base ("Str")
            {
                BindAttributes (this);

                SetDocumentation (
                    "An immutable string of UTF-16 characters"
                );
            }

            public override IodineObject Invoke (VirtualMachine vm, IodineObject[] arguments)
            {
                if (arguments.Length <= 0) {
                    vm.RaiseException (new IodineArgumentException (1));
                }
                return new IodineString (arguments [0].ToString ());
            }
                
            public override IodineObject BindAttributes (IodineObject obj)
            {
                IodineIterableMixin.ApplyMixin (obj);

                obj.SetAttribute ("lower", new BuiltinMethodCallback (Lower, obj));
                obj.SetAttribute ("upper", new BuiltinMethodCallback (Upper, obj));
                obj.SetAttribute ("substr", new BuiltinMethodCallback (Substring, obj));
                obj.SetAttribute ("index", new BuiltinMethodCallback (IndexOf, obj));
                obj.SetAttribute ("rindex", new BuiltinMethodCallback (RightIndex, obj));
                obj.SetAttribute ("find", new BuiltinMethodCallback (Find, obj));
                obj.SetAttribute ("rfind", new BuiltinMethodCallback (RightFind, obj));
                obj.SetAttribute ("contains", new BuiltinMethodCallback (Contains, obj));
                obj.SetAttribute ("replace", new BuiltinMethodCallback (Replace, obj));
                obj.SetAttribute ("startswith", new BuiltinMethodCallback (StartsWith, obj));
                obj.SetAttribute ("endswith", new BuiltinMethodCallback (EndsWith, obj));
                obj.SetAttribute ("split", new BuiltinMethodCallback (Split, obj));
                obj.SetAttribute ("join", new BuiltinMethodCallback (Join, obj));
                obj.SetAttribute ("trim", new BuiltinMethodCallback (Trim, obj));
                obj.SetAttribute ("format", new BuiltinMethodCallback (Format, obj));
                obj.SetAttribute ("isalpha", new BuiltinMethodCallback (IsLetter, obj));
                obj.SetAttribute ("isdigit", new BuiltinMethodCallback (IsDigit, obj));
                obj.SetAttribute ("isalnum", new BuiltinMethodCallback (IsLetterOrDigit, obj));
                obj.SetAttribute ("iswhitespace", new BuiltinMethodCallback (IsWhiteSpace, obj));
                obj.SetAttribute ("issymbol", new BuiltinMethodCallback (IsSymbol, obj));
                obj.SetAttribute ("ljust", new BuiltinMethodCallback (PadRight, obj));
                obj.SetAttribute ("rjust", new BuiltinMethodCallback (PadLeft, obj));

                base.BindAttributes (obj);

                return obj;
            }

            [BuiltinDocString (
                "Returns the uppercase representation of this string"
            )]
            private IodineObject Upper (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineString;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                return new IodineString (thisObj.Value.ToUpper ());
            }

            [BuiltinDocString (
                "Returns the lowercase representation of this string"
            )]
            private IodineObject Lower (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineString;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                return new IodineString (thisObj.Value.ToLower ());
            }
                
            [BuiltinDocString (
                "Returns a substring contained within this string.",
                "@param start The starting index.",
                "@optional end The ending index (Default is the length of the string)",
                "@returns The substring between start and end"
            )]
            private IodineObject Substring (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineString;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                if (args.Length < 1) {
                    vm.RaiseException (new IodineArgumentException (1));
                    return null;
                }
                int start = 0;
                int len = 0;
                var startObj = args [0] as IodineInteger;

                if (startObj == null) {
                    vm.RaiseException (new IodineTypeException ("Int"));
                    return null;
                }
                start = (int)startObj.Value;

                if (args.Length == 1) {
                    len = thisObj.Value.Length;
                } else {
                    var endObj = args [1] as IodineInteger;
                    if (endObj == null) {
                        vm.RaiseException (new IodineTypeException ("Int"));
                        return null;
                    }
                    len = (int)endObj.Value;
                }

                if (start < thisObj.Value.Length && len <= thisObj.Value.Length) {
                    return new IodineString (thisObj.Value.Substring (start, len - start));
                }
                vm.RaiseException (new IodineIndexException ());
                return null;
            }

            [BuiltinDocString (
                "Returns the index of the first occurance of a string within this string. Raises KeyNotFound exception " +
                "if the specified substring does not exist.",
                "@param substring The string to find."
            )]
            private IodineObject IndexOf (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineString;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                if (args.Length < 1) {
                    vm.RaiseException (new IodineArgumentException (1));
                    return null;
                }

                var ch = args [0] as IodineString;

                if (ch == null) {
                    vm.RaiseException (new IodineTypeException ("Str"));
                    return null;
                }

                var val = ch.ToString ();

                if (!thisObj.Value.Contains (val)) {
                    vm.RaiseException (new IodineKeyNotFound ());
                    return null;
                }

                return new IodineInteger (thisObj.Value.IndexOf (val));
            }
                
            [BuiltinDocString (
                "Returns the index of the last occurance of a string within this string. Raises KeyNotFound exception " +
                "if the specified substring does not exist.",
                "@param substring The string to find."
            )]
            private IodineObject RightIndex (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineString;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                if (args.Length < 1) {
                    vm.RaiseException (new IodineArgumentException (1));
                    return null;
                }

                var ch = args [0] as IodineString;

                if (ch == null) {
                    vm.RaiseException (new IodineTypeException ("Str"));
                    return null;
                }

                var val = ch.ToString ();

                if (!thisObj.Value.Contains (val)) {
                    vm.RaiseException (new IodineKeyNotFound ());
                    return null;
                }
                return new IodineInteger (thisObj.Value.LastIndexOf (val));
            }

            [BuiltinDocString (
                "Returns the index of the first occurance of a string within this string. Returns -1 " +
                "if the specified substring does not exist.",
                "@param substring The string to find."
            )]
            private IodineObject Find (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineString;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                if (args.Length < 1) {
                    vm.RaiseException (new IodineArgumentException (1));
                    return null;
                }

                var ch = args [0] as IodineString;

                if (ch == null) {
                    vm.RaiseException (new IodineTypeException ("Str"));
                    return null;
                }

                var val = ch.ToString ();

                if (!thisObj.Value.Contains (val)) {
                    return new IodineInteger (-1);
                }

                return new IodineInteger (thisObj.Value.IndexOf (val));
            }

            [BuiltinDocString (
                "Returns the index of the last occurance of a string within this string. Returns -1 " +
                "if the specified substring does not exist.",
                "@param substring The string to find."
            )]
            private IodineObject RightFind (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineString;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                if (args.Length < 1) {
                    vm.RaiseException (new IodineArgumentException (1));
                    return null;
                }

                var ch = args [0] as IodineString;

                if (ch == null) {
                    vm.RaiseException (new IodineTypeException ("Str"));
                    return null;
                }
                var val = ch.ToString ();

                if (!thisObj.Value.Contains (val)) {
                    return new IodineInteger (-1);
                }
                return new IodineInteger (thisObj.Value.LastIndexOf (val));
            }

            [BuiltinDocString (
                "Returns true if the string contains the specified value. ",
                "@param value The value that this string must contain to return true."
            )]
            private IodineObject Contains (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineString;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                if (args.Length < 1) {
                    vm.RaiseException (new IodineArgumentException (1));
                    return null;
                }
                return IodineBool.Create (thisObj.Value.Contains (args [0].ToString ()));
            }

            [BuiltinDocString (
                "Returns true if the string starts with the specified value.",
                "@param value The value that this string must start with to return true."
            )]
            private IodineObject StartsWith (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineString;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                if (args.Length < 1) {
                    vm.RaiseException (new IodineArgumentException (1));
                    return null;
                }
                return IodineBool.Create (thisObj.Value.StartsWith (args [0].ToString ()));
            }
                
            [BuiltinDocString (
                "Returns true if the string ends with the specified value.",
                "@param value The value that this string must end with to return true."
            )]
            private IodineObject EndsWith (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineString;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                if (args.Length < 1) {
                    vm.RaiseException (new IodineArgumentException (1));
                    return null;
                }
                return IodineBool.Create (thisObj.Value.EndsWith (args [0].ToString ()));
            }

            [BuiltinDocString (
                "Returns a new string where call occurances of [str1] have been replaced with [str2].",
                "@param str1 The value that will be replaced.",
                "@param str2 The value to replace [str1] with."
            )]
            private IodineObject Replace (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineString;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                if (args.Length < 2) {
                    vm.RaiseException (new IodineArgumentException (2));
                    return null;
                }
                var arg1 = args [0] as IodineString;
                var arg2 = args [1] as IodineString;

                if (arg1 == null || arg2 == null) {
                    vm.RaiseException (new IodineTypeException ("Str"));
                    return null;
                }
                return new IodineString (thisObj.Value.Replace (arg1.Value, arg2.Value));
            }

            [BuiltinDocString (
                "Returns a list containing every substring between [seperator].",
                "@param seperator The seperator to split this string by."
            )]
            private IodineObject Split (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineString;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                if (args.Length < 1) {
                    vm.RaiseException (new IodineArgumentException (1));
                    return null;
                }

                var ch = args [0] as IodineString;
                char val;
                if (ch == null) {
                    vm.RaiseException (new IodineTypeException ("Str"));
                    return null;

                }
                val = ch.Value [0];

                var list = new IodineList (new IodineObject[]{ });
                foreach (string str in thisObj.Value.Split (val)) {
                    list.Add (new IodineString (str));
                }
                return list;
            }

            [BuiltinDocString (
                "Returns a string where all leading whitespace characters have been removed."
            )]
            private IodineObject Trim (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineString;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                return new IodineString (thisObj.Value.Trim ());
            }

            [BuiltinDocString (
                "Joins all arguments together, returning a string where this string has been placed between all supplied arguments",
                "@param *args Arguments to join together using this string as a seperator."
            )]
            private IodineObject Join (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineString;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                var accum = new StringBuilder ();
                var collection = args [0].GetIterator (vm);
                collection.IterReset (vm);
                string last = "";
                string sep = "";
                while (collection.IterMoveNext (vm)) {
                    IodineObject o = collection.IterGetCurrent (vm);
                    accum.AppendFormat ("{0}{1}", last, sep);
                    last = o.ToString (vm).ToString ();
                    sep = thisObj.Value;
                }
                accum.Append (last);
                return new IodineString (accum.ToString ());
            }

            private IodineObject Format (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineString;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                string format = thisObj.Value;
                var formatter = new IodineFormatter ();
                return new IodineString (formatter.Format (vm, format, args));
            }

            [BuiltinDocString (
                "Returns true if all characters in this string are letters."
            )]
            private IodineObject IsLetter (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineString;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                bool result = thisObj.Value.Length == 0 ? false : true;
                for (int i = 0; i < thisObj.Value.Length; i++) {
                    if (!char.IsLetter (thisObj.Value [i])) {
                        return IodineBool.False;
                    }
                }
                return IodineBool.Create (result);
            }

            [BuiltinDocString (
                "Returns true if all characters in this string are digits."
            )]
            private IodineObject IsDigit (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineString;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                bool result = thisObj.Value.Length == 0 ? false : true;
                for (int i = 0; i < thisObj.Value.Length; i++) {
                    if (!char.IsDigit (thisObj.Value [i])) {
                        return IodineBool.False;
                    }
                }
                return IodineBool.Create (result);
            }
                
            [BuiltinDocString (
                "Returns true if all characters in this string are letters or digits."
            )]
            private IodineObject IsLetterOrDigit (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineString;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                bool result = thisObj.Value.Length == 0 ? false : true;
                for (int i = 0; i < thisObj.Value.Length; i++) {
                    if (!char.IsLetterOrDigit (thisObj.Value [i])) {
                        return IodineBool.False;
                    }
                }
                return IodineBool.Create (result);
            }
                
            [BuiltinDocString (
                "Returns true if all characters in this string are white space characters."
            )]
            private IodineObject IsWhiteSpace (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineString;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                bool result = thisObj.Value.Length == 0 ? false : true;
                for (int i = 0; i < thisObj.Value.Length; i++) {
                    if (!char.IsWhiteSpace (thisObj.Value [i])) {
                        return IodineBool.False;
                    }
                }
                return IodineBool.Create (result);
            }

            [BuiltinDocString (
                "Returns true if all characters in this string are symbols."
            )]
            private IodineObject IsSymbol (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineString;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                bool result = thisObj.Value.Length == 0 ? false : true;
                for (int i = 0; i < thisObj.Value.Length; i++) {
                    if (!char.IsSymbol (thisObj.Value [i])) {
                        return IodineBool.False;
                    }
                }
                return IodineBool.Create (result);
            }

            [BuiltinDocString (
                "Returns a string that has been justified by [n] characters to right.", 
                "@param n How much to justify this string.",
                "@optional c The string to use as padding (Default is whitespace)."
            )]
            private IodineObject PadRight (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineString;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                char ch = ' ';

                if (args.Length == 0) {
                    vm.RaiseException (new IodineArgumentException (1));
                    return null;
                }

                var width = args [0] as IodineInteger;

                if (width == null) {
                    vm.RaiseException (new IodineTypeException ("Int"));
                    return null;
                }

                if (args.Length > 1) {
                    var chStr = args [0] as IodineString;

                    if (chStr == null) {
                        vm.RaiseException (new IodineTypeException ("Str"));
                        return null;
                    }

                    ch = chStr.Value [0];
                }

                return new IodineString (thisObj.Value.PadRight ((int)width.Value, ch));
            }

            [BuiltinDocString (
                "Returns a string that has been justified by [n] characters to left.", 
                "@param n How much to justify this string.",
                "@optional c The string to use as padding (Default is whitespace)."
            )]
            private IodineObject PadLeft (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                var thisObj = self as IodineString;

                if (thisObj == null) {
                    vm.RaiseException (new IodineFunctionInvocationException ());
                    return null;
                }

                char ch = ' ';

                if (args.Length == 0) {
                    vm.RaiseException (new IodineArgumentException (1));
                    return null;
                }

                var width = args [0] as IodineInteger;

                if (width == null) {
                    vm.RaiseException (new IodineTypeException ("Int"));
                    return null;
                }

                if (args.Length > 1) {
                    var chStr = args [0] as IodineString;

                    if (chStr == null) {
                        vm.RaiseException (new IodineTypeException ("Str"));
                        return null;
                    }

                    ch = chStr.Value [0];
                }

                return new IodineString (thisObj.Value.PadLeft ((int)width.Value, ch));
            }
        }

        class StringIterator : IodineObject
        {
            static IodineTypeDefinition TypeDefinition = new IodineTypeDefinition ("StrIterator");

            private string value;

            private int iterIndex = 0;

            public StringIterator (string value)
                : base (TypeDefinition)
            {
                this.value = value;
            }

            public override IodineObject IterGetCurrent (VirtualMachine vm)
            {
                return new IodineString (value [iterIndex - 1].ToString ());
            }

            public override bool IterMoveNext (VirtualMachine vm)
            {
                if (iterIndex >= value.Length) {
                    return false;
                }
                iterIndex++;
                return true;
            }

            public override void IterReset (VirtualMachine vm)
            {
                iterIndex = 0;
            }
        }


        public string Value { private set; get; }

        private IodineString (string value, bool supressTypeBinding)
        {
            Value = value;
        }

        public IodineString (string val)
            : base (TypeDefinition)
        {
            Value = val ?? "";

            // HACK: Add __iter__ attribute to match Iterable trait
            SetAttribute ("__iter__", new BuiltinMethodCallback ((VirtualMachine vm, IodineObject self, IodineObject [] args) => {
                return GetIterator (vm);
            }, this));
        }

        public override bool Equals (IodineObject obj)
        {
            var strVal = obj as IodineString;

            if (strVal != null) {
                return strVal.Value == Value;
            }

            return false;
        }

        public override IodineObject Len (VirtualMachine vm)
        {
            return new IodineInteger (Value.Length);
        }

        public override IodineObject Slice (VirtualMachine vm, IodineSlice slice)
        {
            return new IodineString (Substring (
                slice.Start,
                slice.Stop,
                slice.Stride,
                slice.DefaultStart,
                slice.DefaultStop)
            );
        }

        private string Substring (int start, int end, int stride, bool defaultStart, bool defaultEnd)
        {
            int actualStart = start >= 0 ? start : Value.Length - (start + 2);
            int actualEnd = end >= 0 ? end : Value.Length - (end + 2);

            var accum = new StringBuilder ();

            if (stride >= 0) {

                if (defaultStart) {
                    actualStart = 0;
                }

                if (defaultEnd) {
                    actualEnd = Value.Length;
                }

                for (int i = actualStart; i < actualEnd; i += stride) {
                    accum.Append (Value [i]);
                }
            } else {

                if (defaultStart) {
                    actualStart = Value.Length - 1;
                }

                if (defaultEnd) {
                    actualEnd = 0;
                }

                for (int i = actualStart; i >= actualEnd; i += stride) {
                    accum.Append (Value [i]);
                }
            }

            return accum.ToString ();
        }


        public override IodineObject Compare (VirtualMachine vm, IodineObject obj)
        {
            return new IodineInteger (Value.CompareTo (obj.ToString ()));
        }

        public override IodineObject Add (VirtualMachine vm, IodineObject right)
        {
            var str = right as IodineString;
            if (str == null) {
                vm.RaiseException ("Right hand value must be of type Str!");
                return null;
            }
            return new IodineString (Value + str.Value);
        }

        public override bool Equals (object obj)
        {
            return Equals (obj as IodineObject);
        }

        public override IodineObject Equals (VirtualMachine vm, IodineObject right)
        {
            var str = right as IodineString;
            if (str == null) {
                return base.Equals (vm, right);
            }
            return IodineBool.Create (str.Value == Value);
        }

        public override IodineObject NotEquals (VirtualMachine vm, IodineObject right)
        {
            var str = right as IodineString;
            if (str == null) {
                return base.NotEquals (vm, right);
            }
            return IodineBool.Create (str.Value != Value);
        }

        public override string ToString ()
        {
            return Value;
        }

        public override int GetHashCode ()
        {
            if (Value == null) {
                return 0;
            }
            return Value.GetHashCode ();
        }

        public override IodineObject GetIndex (VirtualMachine vm, IodineObject key)
        {
            var index = key as IodineInteger;
            if (index == null) {
                vm.RaiseException (new IodineTypeException ("Int"));
                return null;
            }
            if (index.Value >= Value.Length) {
                vm.RaiseException (new IodineIndexException ());
                return null;
            }
            return new IodineString (Value [(int)index.Value].ToString ());
        }

        public override IodineObject GetIterator (VirtualMachine vm)
        {
            return new StringIterator (Value);
        }
      
        public override IodineObject Represent (VirtualMachine vm)
        {
            return new IodineString (String.Format ("\"{0}\"", Value));
        }
            
        public override bool IsTrue ()
        {
            return Value.Length > 0;
        }

        internal static IodineString CreateTypelessString (string str)
        {
            return new IodineString (str, false);
        }
    }
}


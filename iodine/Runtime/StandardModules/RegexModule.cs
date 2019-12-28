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

using System.Text.RegularExpressions;

namespace Iodine.Runtime
{
    [IodineBuiltinModule ("regex")]
    public class RegexModule : IodineModule
    {
        public class IodineRegex : IodineObject
        {
            public static readonly IodineTypeDefinition TypeDefinition = new RegexTypeDefinition ();


            public readonly Regex Value;

            sealed class RegexTypeDefinition : IodineTypeDefinition
            {
                public RegexTypeDefinition ()
                    : base ("Regex")
                {
                    BindAttributes (this);
                }

                public override IodineObject BindAttributes (IodineObject obj)
                {
                    SetAttribute ("find", new BuiltinMethodCallback (Match, obj));
                    SetAttribute ("ismatch", new BuiltinMethodCallback (IsMatch, obj));
                    SetAttribute ("replace", new BuiltinMethodCallback (Replace, obj));

                    return base.BindAttributes (obj);
                }


                [BuiltinDocString (
                    "Finds the first occurance of the regular expression within the supplied string.",
                    "@param str The string to search"
                )]
                IodineObject Match (VirtualMachine vm, IodineObject self, IodineObject [] args)
                {
                    if (args.Length < 1) {
                        vm.RaiseException (new IodineArgumentException (1));
                        return null;
                    }

                    var regexObj = self as IodineRegex;

                    if (regexObj == null) {
                        vm.RaiseException (new IodineFunctionInvocationException ());
                        return null;
                    }

                    var expr = args [0] as IodineString;

                    if (expr == null) {
                        vm.RaiseException (new IodineTypeException ("Str"));
                        return null;
                    }

                    return new IodineMatch (regexObj.Value.Match (expr.ToString ()));
                }

                [BuiltinDocString (
                    "Returns true if the specified string matches the regular expression",
                    "@param str The string to test"
                )]
                IodineObject IsMatch (VirtualMachine vm, IodineObject self, IodineObject [] args)
                {
                    if (args.Length <= 0) {
                        vm.RaiseException (new IodineArgumentException (1));
                        return null;
                    }

                    var regexObj = self as IodineRegex;

                    if (regexObj == null) {
                        vm.RaiseException (new IodineFunctionInvocationException ());
                        return null;
                    }

                    var expr = args [0] as IodineString;

                    if (expr == null) {
                        vm.RaiseException (new IodineTypeException ("Str"));
                        return null;
                    }

                    return IodineBool.Create (regexObj.Value.IsMatch (expr.ToString ()));
                }


                [BuiltinDocString (
                    "Returns a string, where all instances of the specified regular expression",
                    "have been replaced with the specified value",
                    "@param pattern The pattern",
                    "@param value The value to replace all occurances of the supplied pattern"
                )]
                IodineObject Replace (VirtualMachine vm, IodineObject self, IodineObject [] args)
                {
                    if (args.Length <= 1) {
                        vm.RaiseException (new IodineArgumentException (1));
                        return null;
                    }

                    var regexObj = self as IodineRegex;

                    if (regexObj == null) {
                        vm.RaiseException (new IodineFunctionInvocationException ());
                        return null;
                    }


                    var input = args [0] as IodineString;
                    var val = args [1] as IodineString;

                    if (input == null || val == null) {
                        vm.RaiseException (new IodineTypeException ("Str"));
                        return null;
                    }

                    var str = regexObj.Value.Replace (args [0].ToString (), args [1].ToString ());
                    return new IodineString (str);
                }
            }

            public IodineRegex (string pattern)
                : base (TypeDefinition)
            {
                Value = new Regex (pattern);
            }
        }


        class IodineMatch : IodineObject
        {
            public static readonly IodineTypeDefinition MatchTypeDef = new MatchTypeDefinition ();

            sealed class MatchTypeDefinition : IodineTypeDefinition
            {
                public MatchTypeDefinition ()
                    : base ("Match")
                {
                    BindAttributes (this);
                }

                public override IodineObject BindAttributes (IodineObject obj)
                {
                    SetAttribute ("nextmatch", new BuiltinMethodCallback (GetNextMatch, obj));
                    SetAttribute ("captures", new BuiltinMethodCallback (Captures, obj));

                    return base.BindAttributes (obj);
                }

                static IodineObject Captures (VirtualMachine vm, IodineObject self, IodineObject [] args)
                {
                    var match = self as IodineMatch;

                    if (match == null) {
                        vm.RaiseException (new IodineFunctionInvocationException ());
                        return null;
                    }

                    var strObjects = new IodineString [match.Value.Captures.Count];

                    for (int i = 0; i < match.Value.Captures.Count; i++) {
                        strObjects [i] = new IodineString (match.Value.Captures [i].Value);
                    }

                    return new IodineTuple (strObjects);
                }

                [BuiltinDocString (
                    "Returns the next match"
                )]
                static IodineObject GetNextMatch (VirtualMachine vm, IodineObject self, IodineObject [] args)
                {
                    var match = self as IodineMatch;

                    if (match == null) {
                        vm.RaiseException (new IodineFunctionInvocationException ());
                        return null;
                    }

                    return new IodineMatch (match.Value.NextMatch ());
                }
            }

            class MatchIterator : IodineObject
            {
                Match currentMatch;

                readonly Match firstMatch;

                public MatchIterator (Match match)
                    : base (new IodineTypeDefinition ("MatchIterator"))
                {
                    firstMatch = match;
                    currentMatch = match;
                }


                public override IodineObject IterGetCurrent (VirtualMachine vm)
                {
                    return new IodineMatch (currentMatch);
                }

                public override bool IterMoveNext (VirtualMachine vm)
                {
                    currentMatch = currentMatch.NextMatch ();

                    return currentMatch.Success;
                }

                public override void IterReset (VirtualMachine vm)
                {
                    currentMatch = firstMatch;
                }
            }

            public readonly Match Value;


            public IodineMatch (Match val)
                : base (MatchTypeDef)
            {
                Value = val;
                SetAttribute ("value", new IodineString (val.Value));
                SetAttribute ("success", IodineBool.Create (val.Success));
            }

            public override IodineObject GetIterator (VirtualMachine vm)
            {
                return new MatchIterator (Value);
            }

            public override IodineObject GetIndex (VirtualMachine vm, IodineObject key)
            {
                var intObj = key as IodineInteger;

                if (intObj != null) {
                    var intVal = (int)intObj.Value;

                    if (intVal < Value.Groups.Count) {
                        return new IodineString (Value.Groups [intVal].Value);
                    }
                }

                var strObj = key as IodineString;

                if (strObj != null) {

                    var strVal = strObj.ToString ();

                    return new IodineString (Value.Groups [strVal].Value);
                }
                vm.RaiseException (new IodineIndexException ());

                return null;
            }
        }

        public RegexModule ()
            : base ("regex")
        {
            SetAttribute ("compile", new BuiltinMethodCallback (Compile, this));
            SetAttribute ("find", new BuiltinMethodCallback (Match, this));
            SetAttribute ("ismatch", new BuiltinMethodCallback (IsMatch, this));
        }


        [BuiltinDocString (
            "Compiles the specified string, returning a Regex object",
            "@param pattern The regular expression to compile"
        )]
        IodineObject Compile (VirtualMachine vm, IodineObject self, IodineObject[] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            var expr = args [0] as IodineString;

            if (expr == null) {
                vm.RaiseException (new IodineTypeException ("Str"));
                return null;
            }

            return new IodineRegex (expr.ToString ());
        }

        [BuiltinDocString (
            "Finds the first occurance of the specified regular expression within the supplied string ",
            "@param str The string to search",
            "@param pattern The regular expression"
        )]
        IodineObject Match (VirtualMachine vm, IodineObject self, IodineObject[] args)
        {
            if (args.Length <= 1) {
                vm.RaiseException (new IodineArgumentException (2));
                return null;
            }

            var data = args [0] as IodineString;
            var pattern = args [1] as IodineString;

            if (pattern == null || data == null) {
                vm.RaiseException (new IodineTypeException ("Str"));
                return null;
            }

            return new IodineMatch (Regex.Match (data.ToString (), pattern.ToString ()));
        }

        [BuiltinDocString (
            "Returns true if the specified string matches the specified pattern",
            "@param str The string to test",
            "@param pattern The regular expression to match str with"
        )]
        IodineObject IsMatch (VirtualMachine vm, IodineObject self, IodineObject[] args)
        {
            if (args.Length <= 1) {
                vm.RaiseException (new IodineArgumentException (2));
                return null;
            }
            var data = args [0] as IodineString;
            var pattern = args [1] as IodineString;

            if (pattern == null || data == null) {
                vm.RaiseException (new IodineTypeException ("Str"));
                return null;
            }

            return IodineBool.Create (Regex.IsMatch (data.ToString (), pattern.ToString ()));
        }

    }
}


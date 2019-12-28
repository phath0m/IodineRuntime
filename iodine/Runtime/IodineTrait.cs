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
using System.Collections.Generic;

namespace Iodine.Runtime
{
    /// <summary>
    /// Represents an IodineTrait. An IodineTrait describes which members a class may
    /// have, although classes do not have to actually implement a trait. In short, 
    /// traits are what I like to call "non-censual interfaces"
    /// </summary>
    public class IodineTrait : IodineTypeDefinition
    {
        public IList<IodineMethod> RequiredMethods { private set; get; }

        public IodineTrait (string name)
            : base (name)
        {
            RequiredMethods = new List<IodineMethod> ();
        }

        public void AddMethod (IodineMethod method)
        {
            RequiredMethods.Add (method);
        }

        /// <summary>
        /// Determines whether an object has this trait
        /// </summary>
        /// <returns><c>true</c> if obj has trait; otherwise, <c>false</c>.</returns>
        /// <param name="obj">Object.</param>
        public bool HasTrait (IodineObject obj)
        {
            foreach (IodineMethod method in RequiredMethods) {
                if (obj.HasAttribute (method.Name)) {
                    var attr = obj.GetAttribute (method.Name);

                    var objMethod = attr as IodineMethod;

                    if (objMethod == null) {
                        // HACK: Make builtin methods work
                        if (attr is BuiltinMethodCallback) {
                            continue;
                        }
                        if (attr is IodineBoundMethod) {
                            objMethod = ((IodineBoundMethod)attr).Method;
                        } else {
                            return false;
                        }
                    }

                    bool match = method.AcceptsKeywordArgs == objMethod.AcceptsKeywordArgs
                                 && method.Variadic == objMethod.Variadic
                                 && method.ParameterCount == objMethod.ParameterCount;

                    if (!match) {
                        return false;
                    }
                } else {
                    return false;
                }
            }

            return true;
        }

        public override string ToString ()
        {
            return string.Format ("<Trait {0}>", Name);
        }
    }
}


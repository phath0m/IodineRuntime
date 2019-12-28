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
using Iodine.Compiler;

namespace Iodine.Runtime
{
    /// <summary>
    /// The structure of an Iodine bytecode instruction
    /// </summary>
    public struct Instruction
    {
        /// <summary>
        /// Source location where this instruction was emitted from, used for printing
        /// useful stack traces and debugging
        /// </summary>
        public readonly SourceLocation Location;

        public readonly Opcode OperationCode;

        public readonly int Argument;
        public readonly IodineObject ArgumentObject;
        public readonly string ArgumentString;

        public Instruction (SourceLocation location, Opcode opcode)
            : this ()
        {
            OperationCode = opcode;
            Argument = 0;
            Location = location;
        }

        public Instruction (SourceLocation location, Opcode opcode, int arg)
            : this ()
        {
            OperationCode = opcode;
            Argument = arg;
            Location = location;
        }

        public Instruction (SourceLocation location, Opcode opcode, IodineObject obj)
            : this ()
        {
            OperationCode = opcode;
            Argument = 0;
            Location = location;
            ArgumentObject = obj;

            if (obj is IodineName) {
                ArgumentString = ((IodineName)obj).Value;
            }
        }

        public Instruction (SourceLocation location, Opcode opcode, int arg, IodineObject obj)
            : this ()
        {
            OperationCode = opcode;
            Argument = arg;
            Location = location;
            ArgumentObject = obj;

            if (obj is IodineName) {
                ArgumentString = ((IodineName)obj).Value;
            }
        }
    }
}


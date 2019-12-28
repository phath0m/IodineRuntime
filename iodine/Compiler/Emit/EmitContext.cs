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
using Iodine.Runtime;

namespace Iodine.Compiler
{
    public sealed class EmitContext
    {
        public readonly SymbolTable SymbolTable;

        public CodeBuilder CurrentMethod {
            private set;
            get;
        }

        public ModuleBuilder CurrentModule {
            private set;
            get;
        }

        public bool ShouldOptimize {
            set;
            get;
        }

        public readonly Stack<Label> BreakLabels = new Stack<Label> ();
        public readonly Stack<Label> ContinueLabels = new Stack<Label> ();
        public readonly IodineObject PatternTemporary;

        public readonly bool IsPatternExpression = false;
        public readonly bool IsInClass = false;
        public readonly bool IsInClassBody = false;

        public EmitContext (SymbolTable symbolTable,
            ModuleBuilder module,
            CodeBuilder method,
            bool isInClass = false,
            bool isInClassBody = false,
            bool isPatternExpression = false,
            IodineObject patternTempory = null)
        {
            SymbolTable = symbolTable;
            CurrentMethod = method;
            CurrentModule = module;
            IsInClass = isInClass;
            IsInClassBody = isInClassBody;
            IsPatternExpression = isPatternExpression;
            PatternTemporary = patternTempory;
        }

        public void SetCurrentMethod (CodeBuilder method)
        {
            CurrentMethod = method;
        }

        public void SetCurrentModule (ModuleBuilder builder)
        {
            CurrentModule = builder;
        }
    }
}


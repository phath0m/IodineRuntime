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

namespace Iodine.Compiler
{
    public enum Errors
    {
        InternalError = 0x00,
        IllegalSyntax = 0x01,
        IllegalPatternExpression = 0x02,
        UnexpectedCharacter = 0x03,
        UnexpectedToken = 0x04,
        UnrecognizedEscapeSequence = 0x05,
        UnterminatedStringLiteral = 0x06,
        StatementNotAllowedOutsideFunction = 0x07,
        VariableAlreadyDefined = 0x08,
        ArgumentAfterKeywordArgs = 0x09,
        ArgumentAfterVariadicArgs = 0x0A,
        SuperCalledAfter = 0x0B,
        PipeIntoNonFunction = 0x0C,
        ExpectedIdentifier = 0x0D,
        IllegalInterfaceDeclaration = 0x0E,
        UnexpectedEndOfFile = 0x0F,
        IntegerOverBounds = 0x10,
        RecordCantHaveVargs = 0x11,
        RecordCantHaveKwargs = 0x12,
        RecordCantHaveSelf = 0x13,
        MatchDoesNotAccountForAllConditions = 0x14,
        EmptyDecompositionList = 0x15
    }

    public sealed class Error
    {

        static readonly string[] errorStringLookup = new string[] {
            "Internal error",
            "Illegal syntax",
            "Illegal pattern expression",
            "Unexpected character '{0}'",
            "Unexpected '{0}'",
            "Unrecognized escape sequence",
            "Unterminated string literal",
            "Statement not allowed outside function body",
            "Variable '{0}' was already defined",
            "Parameter after keyworld argument list",
            "Parameter after variable argument list",
            "super () must be called first",
            "Expression must be piped into function",
            "Illegal identifier",
            "Interface declaration may only contain function prototypes",
            "Unexpected end of file",
            "Integer value exceeds the maximum bounds of a 64 bit integer",
            "Record cannot have variable length arguments!",
            "Record cannot have keyword aguments!",
            "self keyword invalid in record field list!",
            "match expression does not account for all possible values",
            "You must specify at least one capture within a tuple decomposition list"
        };

        public readonly string Text;

        public readonly Errors ErrorID;

        public readonly Token Token;

        public readonly SourceLocation Location;


        public bool HasToken {
            get {
                return Token != null;
            }
        }

        public Error (Errors error,
                      SourceLocation location,
                      Token offendingToken,
                      params object [] args)
        {
            Token = offendingToken;
            Text = string.Format (errorStringLookup [(int)error], args);
            Location = location;
            ErrorID = error;
        }

        public Error (Errors error, SourceLocation location, params object[] args)
        {
            Text = string.Format (errorStringLookup [(int)error], args);
            Location = location;
            ErrorID = error;
        }
    }
}


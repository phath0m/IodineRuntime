// /**
//   * Copyright (c) 2015, phath0m All rights reserved.
//
//   * Redistribution and use in source and binary forms, with or without modification,
//   * are permitted provided that the following conditions are met:
//   * 
//   *  * Redistributions of source code must retain the above copyright notice, this list
//   *    of conditions and the following disclaimer.
//   * 
//   *  * Redistributions in binary form must reproduce the above copyright notice, this
//   *    list of conditions and the following disclaimer in the documentation and/or
//   *    other materials provided with the distribution.
//
//   * Neither the name of the copyright holder nor the names of its contributors may be
//   * used to endorse or promote products derived from this software without specific
//   * prior written permission.
//   * 
//   * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY
//   * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
//   * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT
//   * SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
//   * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
//   * TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR
//   * BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
//   * CONTRACT ,STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
//   * ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH
//   * DAMAGE.
// /**

using System.Text;

namespace Iodine.Compiler
{
    public class SourceReader
    {
        int position;
        int sourceLength;


        int line;
        int column;

        string file;
        string source;

        public SourceLocation Location => new SourceLocation (line, column, file);

        public SourceReader (
            string source,
            string file
        )
        {
            this.source = source;
            this.file = file;

            sourceLength = source.Length;
        }
           
        public bool See (int i = 0)
        {
            return i + position < sourceLength;
        }

        public void Skip (int amount = 1)
        {
            for (int i = 0; i < amount; i++) {
                if (See (i) && Peek () == '\n') {
                    line++;
                    column = 0;
                } else if (See (i)) {
                    column++;
                }
                position++;
            }
        }

        public void SkipWhitespace ()
        {
            while (See () && char.IsWhiteSpace (Peek ())) {
                Skip ();
            }
        }

        public void SkipLine ()
        {
            while (See () && Peek () != '\n') {
                Skip ();
            }

            Skip ();
        }

        public char Peek (int amount = 0)
        {
            if (See (amount)) {
                return source [position + amount];
            }
            return '\0';
        }

        public string Peeks (int chars)
        {
            var accum = new StringBuilder ();

            for (int i = 0; i < chars && See (i); i++) {
                accum.Append (Peek (i));    
            }

            return accum.ToString ();
        }

        public char Read ()
        {
            Skip ();
            return Peek (-1);
        }

        public string Reads (int chars)
        {
            var ret = Peeks (chars);
            Skip (chars);
            return ret;
        }
    }
}


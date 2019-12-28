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

namespace Iodine.Runtime
{
    public class IodineRange : IodineObject
    {
        static IodineTypeDefinition TypeDefinition = new IodineTypeDefinition ("Range");

        class RangeIterator : IodineObject
        {
            static IodineTypeDefinition TypeDefinition = new IodineTypeDefinition ("RangeIterator");

            long iterIndex = 0;

            readonly long min;
            readonly long end;
            readonly long step;

            public RangeIterator (long min, long max, long step)
                : base (TypeDefinition)
            {
                this.end = max;
                this.step = step;
                this.min = min;
            }

            public override IodineObject IterGetCurrent (VirtualMachine vm)
            {
                return new IodineInteger (iterIndex - 1);
            }

            public override bool IterMoveNext (VirtualMachine vm)
            {
                if (iterIndex >= this.end) {
                    return false;
                }
                iterIndex += this.step;
                return true;
            }

            public override void IterReset (VirtualMachine vm)
            {
                this.iterIndex = min;
            }
        }

        public readonly long LowerBound;
        public readonly long UpperBound;
        readonly long step;

        public IodineRange (long min, long max, long step)
            : base (TypeDefinition)
        {
            UpperBound = max;

            this.step = step;
            this.LowerBound = min;


            IodineIterableMixin.ApplyMixin (this);

            // HACK: Add __iter__ attribute to match Iterable trait
            SetAttribute ("__iter__", new BuiltinMethodCallback ((VirtualMachine vm, IodineObject self, IodineObject [] args) => {
                return GetIterator (vm);
            }, this));
        }

        public override IodineObject GetIterator (VirtualMachine vm)
        {
            return new RangeIterator (LowerBound, UpperBound, step);
        }

        public override string ToString ()
        {
            if (step == 1) {
                return string.Format ("{0} .. {1}", LowerBound, UpperBound);
            }
            return string.Format ("range ({0}, {1}, {2})", LowerBound, UpperBound, step);
        }
    }
}


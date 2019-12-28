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

using System.Security.Cryptography;

namespace Iodine.Runtime
{
    [IodineBuiltinModule ("__hashfunctions__")]
    public class HashModule : IodineModule
    {
        public HashModule ()
            : base ("__hashfunctions__")
        {
            SetAttribute ("sha1", new BuiltinMethodCallback (Sha1, this));
            SetAttribute ("sha256", new BuiltinMethodCallback (Sha256, this));
            SetAttribute ("sha512", new BuiltinMethodCallback (Sha512, this));
            SetAttribute ("md5", new BuiltinMethodCallback (Md5, this));
        }

        /**
         * Iodine Function: sha256 (data)
         * Description: Returns the SHA256 digest of data
         */
        IodineObject Sha256 (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            byte [] hash = null;

            var shaAlgol = new SHA256Managed ();

            hash = PreformHash (vm, args [0], shaAlgol);

            if (hash != null) {
                return new IodineBytes (hash);
            }

            return null;
        }

        /**
         * Iodine Function: sha1 (data)
         * Description: Returns the SHA1 digest of data
         */
        IodineObject Sha1 (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            byte [] hash = null;

            var shaAlgol = new SHA1Managed ();

            hash = PreformHash (vm, args [0], shaAlgol);

            if (hash != null) {
                return new IodineBytes (hash);
            }

            return null;

        }

        /**
         * Iodine Function: sha512 (data)
         * Description: Returns the SHA512 digest of data
         */
        IodineObject Sha512 (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            byte [] hash = null;

            var shaAlgol = new SHA512Managed ();

            hash = PreformHash (vm, args [0], shaAlgol);

            if (hash != null) {
                return new IodineBytes (hash);
            }

            return null;
        }

        /**
         * Iodine Function: md5 (data)
         * Description: Returns the MD5 digest of data
         */
        IodineObject Md5 (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }

            byte [] hash = null;

            var md5Algol = MD5.Create ();

            hash = PreformHash (vm, args [0], md5Algol);

            if (hash != null) {
                return new IodineBytes (hash);
            }

            return null;
        }

        static byte [] GetBytes (IodineObject obj)
        {
            if (obj is IodineString) {
                return System.Text.Encoding.UTF8.GetBytes (obj.ToString ());
            }

            if (obj is IodineBytes) {
                return ((IodineBytes)obj).Value;
            }
            return null;
        }

        static byte [] PreformHash (VirtualMachine vm, IodineObject obj, HashAlgorithm algol)
        {
            if (obj is IodineString || obj is IodineBytes) {
                var data = GetBytes (obj);

                return algol.ComputeHash (data);
            }

            var stream = obj as IodineStream;

            if (obj == null) {
                vm.RaiseException (new IodineTypeException ("Str"));
                return null;
            }

            return algol.ComputeHash (stream.File);
        }
    }
}


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

#if COMPILE_EXTRAS

using System;
using System.Net;
using System.Net.Sockets;
using Iodine.Runtime;

namespace Iodine.Modules.Extras
{
    [IodineBuiltinModule ("net/dns")]
    internal class DnsModule : IodineModule
    {
        public class IodineHostEntry : IodineObject
        {
            private static readonly IodineTypeDefinition HostEntryTypeDef = new IodineTypeDefinition ("HostEntry");

            public IPHostEntry Entry { private set; get; }

            public IodineHostEntry (IPHostEntry host)
                : base (HostEntryTypeDef)
            {
                this.Entry = host;
                IodineObject[] addresses = new IodineObject[Entry.AddressList.Length];
                int i = 0;
                foreach (IPAddress ip in Entry.AddressList) {
                    addresses [i++] = new IodineString (ip.ToString ());
                }
                SetAttribute ("addresses", new IodineTuple (addresses));
            }

        }

        public DnsModule ()
            : base ("dns")
        {
            SetAttribute ("lookup", new BuiltinMethodCallback (GetHostEntry, this));
        }

        private IodineObject GetHostEntry (VirtualMachine vm, IodineObject self, IodineObject[] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return null;
            }
            IodineString domain = args [0] as IodineString;

            if (domain == null) {
                vm.RaiseException (new IodineTypeException ("Str"));
                return null;
            }
            try {
                return new IodineHostEntry (Dns.GetHostEntry (domain.Value));
            } catch (Exception ex) {
                vm.RaiseException (new IodineException (ex.Message));
                return null;
            }
        }

    }
}


#endif
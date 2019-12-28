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
using System.Text;
using System.Collections.Specialized;
using System.Security;
using System.Security.Cryptography;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Iodine.Runtime;

namespace Iodine.Modules.Extras
{
    [IodineBuiltinModule ("net/webclient")]
    internal class WebClientModule : IodineModule
    {
        public class CookieAwareWebClient : WebClient
        {
            public CookieContainer CookieContainer { get; set; } = new CookieContainer ();

            protected override WebRequest GetWebRequest (Uri uri)
            {
                WebRequest request = base.GetWebRequest (uri);
                if (request is HttpWebRequest)
                {
                    (request as HttpWebRequest).CookieContainer = CookieContainer;
                }
                return request;
            }
        }

        public class IodineWebClient : IodineObject
        {
            private static IodineTypeDefinition WebClientTypeDef = new IodineTypeDefinition ("WebClient");

            private WebClient client;

            public IodineWebClient ()
                : base (WebClientTypeDef)
            {
                SetAttribute ("downloadstr", new BuiltinMethodCallback (DownloadString, this));
                SetAttribute ("downloadraw", new BuiltinMethodCallback (DownloadRaw, this));
                SetAttribute ("downloadfile", new BuiltinMethodCallback (DownloadFile, this));
                SetAttribute ("uploadfile", new BuiltinMethodCallback (UploadFile, this));
                SetAttribute ("uploadstr", new BuiltinMethodCallback (UploadString, this));
                SetAttribute ("uploadvalues", new BuiltinMethodCallback (UploadValues, this));
                WebProxy proxy = new WebProxy ();
                client = new CookieAwareWebClient ();
                client.Proxy = proxy;
            }

            private IodineObject DownloadString (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                ServicePointManager.ServerCertificateValidationCallback += (o, certificate, chain, errors) => true;
                IodineString uri = args [0] as IodineString;
                string data;
                try {
                    data = this.client.DownloadString (uri.ToString ());
                } catch (Exception e) {
                    vm.RaiseException (e.Message);
                    return null;
                }
                return new IodineString (data);
            }

            private IodineObject DownloadRaw (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                ServicePointManager.ServerCertificateValidationCallback += (o, certificate, chain, errors) => true;
                IodineString uri = args [0] as IodineString;
                byte[] data;
                try {
                    data = client.DownloadData (uri.ToString ());
                } catch (Exception e) {
                    vm.RaiseException (e.Message);
                    return null;
                }
                return new IodineBytes (data);
            }

            private IodineObject DownloadFile (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                ServicePointManager.ServerCertificateValidationCallback += (o, certificate, chain, errors) => true;
                IodineString uri = args [0] as IodineString;
                IodineString file = args [1] as IodineString;

                try {
                    client.DownloadFile (uri.ToString (), file.ToString ());
                } catch (Exception e) {
                    vm.RaiseException (e.Message);
                }
                return null;
            }

            private IodineObject UploadFile (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                ServicePointManager.ServerCertificateValidationCallback += (o, certificate, chain, errors) => true;
                IodineString uri = args [0] as IodineString;
                IodineString file = args [1] as IodineString;
                byte[] result = client.UploadFile (uri.ToString (), file.ToString ());
                return new IodineBytes (result);
            }

            private IodineObject UploadString (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                ServicePointManager.ServerCertificateValidationCallback += (o, certificate, chain, errors) => true;
                IodineString uri = args [0] as IodineString;
                IodineString str = args [1] as IodineString;
                try {
                    string result = client.UploadString (uri.ToString (), "POST", str.ToString ());
                    return new IodineString (result);
                } catch (Exception ex) {
                    Console.WriteLine (ex.Message);
                    Console.WriteLine (ex.InnerException);
                }
                return new IodineString ("");
            }

            private IodineObject UploadValues (VirtualMachine vm, IodineObject self, IodineObject[] args)
            {
                ServicePointManager.ServerCertificateValidationCallback += (o, certificate, chain, errors) => true;
                IodineString uri = args [0] as IodineString;
                IodineDictionary dict = args [1] as IodineDictionary;

                NameValueCollection nv = new NameValueCollection ();

                foreach (IodineObject key in dict.Keys) {
                    nv [key.ToString ()] = dict.Get (key).ToString ();
                }

                byte[] result = client.UploadValues (uri.ToString (), nv);
                return new IodineBytes (result);
            }
        }

        public WebClientModule () : base ("webclient")
        {
            SetAttribute ("WebClient", new BuiltinMethodCallback (webclient, this));
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true; 
        }

        private IodineObject webclient (VirtualMachine vm, IodineObject self, IodineObject[] args)
        {
            return new IodineWebClient ();
        }


    }
}

#endif
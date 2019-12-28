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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Iodine.Compiler;
using Iodine.Compiler.Ast;
using Iodine.Runtime;

namespace Iodine.Interop
{
    /// <summary>
    /// Represents an abstracted Iodine instance allowing for partial .NET interoperability
    /// </summary>
    public sealed class IodineEngine
    {
        /// <summary>
        /// The Iodine context
        /// </summary>
        public readonly IodineContext Context;

        private TypeRegistry typeRegistry = new TypeRegistry ();
        private Dictionary<string, IodineModule> modules = new Dictionary<string, IodineModule> ();


        private IodineModule module;
        // Last module produced by DoString or DoFile

        /// <summary>
        /// Gets or sets the <see cref="Iodine.Interop.IodineEngine"/>'s global dictionary.
        /// </summary>
        /// <param name="name">Name.</param>
        public dynamic this [string name] {
            get {
                return GetMember (name);
            }
            set {
                SetMember (name, value);
            }
        }

        public IodineEngine ()
            : this (IodineContext.Create ())
        {
        }

        public IodineEngine (IodineContext context)
        {
            Context = context;
            Context.ResolveModule += ResolveModule;
        }

        /// <summary>
        /// Registers a class in the global namespace, allowing it to be
        /// instantiated in Iodine 
        /// </summary>
        /// <param name="name">Name of the class.</param>
        /// <typeparam name="T">The class.</typeparam>
        public void RegisterClass<T> (string name)
            where T : class
        {
            Type type = typeof(T);
            ClassWrapper wrapper = ClassWrapper.CreateFromType (typeRegistry, type, name);
            typeRegistry.AddTypeMapping (type, wrapper, null);
            Context.Globals [name] = wrapper;
        }

        /// <summary>
        /// Registers a struct in the global namespace, allowing it to be
        /// instantiated in Iodine 
        /// </summary>
        /// <param name="name">Name of the class.</param>
        /// <typeparam name="T">The class.</typeparam>
        public void RegisterStruct<T> (string name)
            where T : struct
        {
            Type type = typeof(T);
            ClassWrapper wrapper = ClassWrapper.CreateFromType (typeRegistry, type, name);
            typeRegistry.AddTypeMapping (type, wrapper, null);
            Context.Globals [name] = wrapper;
        }

        /// <summary>
        /// Registers a C# class in Iodine
        /// </summary>
        /// <param name="type">Type.</param>
        /// <param name="name">Name.</param>
        public void RegisterClass (Type type, string name)
        {
            ClassWrapper wrapper = ClassWrapper.CreateFromType (typeRegistry, type, name);
            typeRegistry.AddTypeMapping (type, wrapper, null);
            Context.Globals [name] = wrapper;
        }

        /// <summary>
        /// Registers an assembly, allowing all classes in this assembly to be
        /// used from Iodine.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        public void RegisterAssembly (Assembly assembly)
        {
            var classes = assembly.GetExportedTypes ().Where (p => p.IsClass || p.IsValueType || p.IsInterface);
            foreach (Type type in classes) {
                if (type.Namespace != "") {
                    string moduleName = type.Namespace.Contains (".") ? 
                        type.Namespace.Substring (type.Namespace.LastIndexOf (".") + 1) :
                        type.Namespace;
                    IodineModule module = null;
                    if (!modules.ContainsKey (type.Namespace)) {
                        #warning This needs fixed
                        //module = new IodineModule (moduleName);
                        modules [type.Namespace] = module;
                    } else {
                        module = modules [type.Namespace];
                    }
                    ClassWrapper wrapper = ClassWrapper.CreateFromType (typeRegistry, type,
                                               type.Name);
                    module.SetAttribute (type.Name, wrapper);
                    typeRegistry.AddTypeMapping (type, wrapper, null);
                    
                }
            }
        }

        /// <summary>
        /// Executes a string of Iodine source code
        /// </summary>
        /// <returns>The last object evaluated during the execution of the source.</returns>
        /// <param name="source">A string containing valid Iodine code..</param>
        public dynamic DoString (string source)
        {
            SourceUnit line = SourceUnit.CreateFromSource (source);
            module = line.Compile (Context);
            Context.Invoke (module, new IodineObject[] { });
            return null;
        }

        /// <summary>
        /// Executes and loads an Iodine source file
        /// </summary>
        /// <returns>Last object evaluated during the execution of the file</returns>
        /// <param name="file">File path.</param>
        public dynamic DoFile (string file)
        {
            SourceUnit line = SourceUnit.CreateFromFile (file);
            module = line.Compile (Context);
            Context.Invoke (module, new IodineObject[] { });
            return null;
        }

        /// <summary>
        /// Calls an Iodine function in the current module
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="args">Arguments.</param>
        public dynamic Call (string name, params object[] args)
        {
            IodineObject[] arguments = new IodineObject[args.Length];
            for (int i = 0; i < args.Length; i++) {
                arguments [i] = typeRegistry.ConvertToIodineObject (args [i]);
            }
            IodineObject ret = Context.Invoke (module.GetAttribute (name), arguments);
            return IodineDynamicObject.Create (ret, Context.VirtualMachine, typeRegistry);
        }

        public dynamic Call (IodineObject obj, params object[] args)
        {
            IodineObject[] arguments = new IodineObject[args.Length];
            for (int i = 0; i < args.Length; i++) {
                arguments [i] = typeRegistry.ConvertToIodineObject (args [i]);
            }
            IodineObject ret = Context.Invoke (obj, arguments);
            return IodineDynamicObject.Create (ret, Context.VirtualMachine, typeRegistry);
        }

        public T Call<T> (string name, params object[] args)
        {
            IodineObject[] arguments = new IodineObject[args.Length];
            for (int i = 0; i < args.Length; i++) {
                arguments [i] = typeRegistry.ConvertToIodineObject (args [i]);
            }
            IodineObject ret = Context.Invoke (module.GetAttribute (name), arguments);
            return (T)typeRegistry.ConvertToNativeObject (ret, typeof(T));
        }

        /// <summary>
        /// Returns an item in this Iodine instance's global dictionary
        /// </summary>
        /// <param name="name">Name.</param>
        public dynamic Get (string name)
        {
            IodineObject ret = module.GetAttribute (name);
            return IodineDynamicObject.Create (ret, Context.VirtualMachine, typeRegistry);
        }

        /// <summary>
        /// Returns an item in this Iodine instance's global dictionary as type T
        /// </summary>
        /// <param name="name">Name.</param>
        /// <typeparam name="T">.NET Type</typeparam>
        public T Get<T> (string name)
        {
            IodineObject ret = module.GetAttribute (name);
            return (T)typeRegistry.ConvertToNativeObject (ret, typeof(T));
        }

        private IodineModule ResolveModule (string path)
        {
            /*
             * Resolve any imported .NET modules
             */
            string moduleName = path.Replace ("\\", ".").Replace ("/", ".");
            if (modules.ContainsKey (moduleName)) {
                return modules [moduleName];
            }
            return null;
        }

        private dynamic GetMember (string name)
        {
            IodineObject obj = null;
            if (Context.Globals.ContainsKey (name)) {
                obj = Context.Globals [name];
            }
            return IodineDynamicObject.Create (obj, Context.VirtualMachine, typeRegistry);
        }

        private void SetMember (string name, dynamic value)
        {
            IodineObject obj = typeRegistry.ConvertToIodineObject ((object)value);
            Context.Globals [name] = obj;
        }
    }
}


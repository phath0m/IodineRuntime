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
using Iodine.Util;
using Iodine.Runtime;
using Iodine.Interop;

namespace Iodine.Compiler
{
    public delegate IodineModule ModuleResolveHandler (string name);

    /// <summary>
    /// Global state for an Iodine interpreter instance
    /// </summary>
    public sealed class IodineContext
    {
        public readonly ErrorSink ErrorLog;
        public readonly VirtualMachine VirtualMachine;

        public readonly IodineConfiguration Configuration;
        // Virtual machine configuration
        public readonly TypeRegistry TypeRegistry = new TypeRegistry ();
        // Type registry for .NET interops

        /*
         * Where we can search for modules
         */
        public readonly List<string> SearchPath = new List<string> ();

        // Globals
        public readonly AttributeDictionary Globals = new AttributeDictionary ();

        /// <summary>
        /// Local variables created from a read-evaluate-print-loop
        /// </summary>
        public readonly AttributeDictionary InteractiveLocals = new AttributeDictionary ();

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Iodine.Compiler.IodineContext"/> can use the 
        /// built in Iodine standard library.
        /// </summary>
        /// <value><c>true</c> if allow builtins; otherwise, <c>false</c>.</value>
        public bool AllowBuiltins { 
            set;
            get;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Iodine.Compiler.IodineContext"/> should optimize bytecode 
        /// after compilation.
        /// </summary>
        /// <value><c>true</c> if should optimize; otherwise, <c>false</c>.</value>
        public bool ShouldOptimize {
            set;
            get;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Iodine.Compiler.IodineContext"/> should cache 
        /// bytecode from evaluated iodine files
        /// </summary>
        /// <value><c>true</c> if should cache; otherwise, <c>false</c>.</value>
        public bool ShouldCache {
            set;
            get;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Iodine.Compiler.IodineContext"/> is being ran with
        /// a debugger.
        /// </summary>
        /// <value><c>true</c> if debug; otherwise, <c>false</c>.</value>
        public bool Debug {
            set;
            get;
        }

        /// <summary>
        /// Gets or sets the warning filter.
        /// </summary>
        /// <value>The warning filter.</value>
        public WarningType WarningFilter {
            get;
            set;
        }

        Dictionary<string, IodineModule> moduleCache = new Dictionary<string, IodineModule> ();

        ModuleResolveHandler _resolveModule;

        /// <summary>
        /// Occurs before a module is resolved
        /// </summary>
        public event ModuleResolveHandler ResolveModule {
            add {
                _resolveModule += value;
            }
            remove {
                _resolveModule -= value;
            }
        }


        public IodineContext ()
            : this (new IodineConfiguration ())
        {
            var exeDir = Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location);

            var iodinePath = Environment.GetEnvironmentVariable ("IODINE_PATH");

            SearchPath.Add (Environment.CurrentDirectory);
            SearchPath.Add (Path.Combine (exeDir, "modules"));
            SearchPath.Add (Path.Combine (exeDir, "extensions"));

            if (iodinePath != null) {
                SearchPath.AddRange (iodinePath.Split (':'));
            } 

            // Defaults
            WarningFilter = WarningType.DeprecationWarning | WarningType.SyntaxWarning;
            ShouldOptimize = true;
            AllowBuiltins = true;
            ShouldCache = true;
        }

        public IodineContext (IodineConfiguration config)
        {
            Configuration = config;
            ErrorLog = new ErrorSink ();
            VirtualMachine = new VirtualMachine (this);

            var modules = BuiltInModules.Modules.Values.Where (p => p.ExistsInGlobalNamespace);
            foreach (IodineModule module in modules) {
                foreach (KeyValuePair<string, IodineObject> value in module.Attributes) {
                    Globals [value.Key] = value.Value;
                }
            }
        }

        /// <summary>
        /// Displays a warning 
        /// </summary>
        /// <param name="type">Type.</param>
        /// <param name="message">Message.</param>
        public void Warn (WarningType type, string message)
        {
            WarningType filter = type & WarningFilter;

            if (filter != WarningType.None) {

                var filePath = "";
                var lineNumber = 0;

                if (VirtualMachine.Top != null) {
                    filePath = VirtualMachine.Top.Location.File;
                    lineNumber = VirtualMachine.Top.Location.Line;
                }

                var forecolor = Console.ForegroundColor;

                Console.ForegroundColor = ConsoleColor.Yellow;

                Console.Error.Write ("WARNING ");

                Console.ForegroundColor = forecolor;

                Console.Error.WriteLine ("**: {0}:{1} {2}", filePath, lineNumber, message);
            }
        }

        /// <summary>
        /// Invokes an IodineObject (Calling its __invoke__ method) under this
        /// context 
        /// </summary>
        /// <param name="obj">The object to invoke.</param>
        /// <param name="args">Arguments.</param>
        public IodineObject Invoke (IodineObject obj, IodineObject[] args)
        {
            return obj.Invoke (VirtualMachine, args);
        }

        /// <summary>
        /// Loads an Iodine module.
        /// </summary>
        /// <returns>A compiled Iodine module.</returns>
        /// <param name="name">The module's name.</param>
        /// <param name="useCached">Whether or not this should load modules that have been cached.</param>
        public IodineModule LoadModule (string name, bool useCached = true)
        {
            if (moduleCache.ContainsKey (name) && useCached) {
                return moduleCache [name];
            }

            if (_resolveModule != null) {
                foreach (Delegate del in _resolveModule.GetInvocationList ()) {
                    var handler = del as ModuleResolveHandler;
                    var result = handler (name);
                    if (result != null) {
                        return result;
                    }
                }
            }

            var module = LoadIodineModule (name);

            if (module == null) {
                module = LoadExtensionModule (name);
            }

            if (module != null) {
                moduleCache [name] = module;
            }

            return module;
        }

        /// <summary>
        /// Exposes an iodine module by name to this context's module resolver
        /// </summary>
        /// <param name="moduleName">Module name.</param>
        /// <param name="module">Module.</param>
        public void ExposeModule (string moduleName, IodineModule module)
        {
            ResolveModule += (name) => {
                if (name == moduleName) {
                    return module;
                }
                return null;
            };
        }

        /// <summary>
        /// Creates a new Iodine context
        /// </summary>
        public static IodineContext Create ()
        {
            return new IodineContext ();
        }

        IodineModule LoadIodineModule(string name)
        {
            var modulePath = FindModuleSource (name);

            if (modulePath != null) {

                if (moduleCache.ContainsKey (modulePath)) {
                    return moduleCache [modulePath];
                }


                var source = SourceUnit.CreateFromFile (modulePath);

                var module = source.Compile (this);

                moduleCache [modulePath] = module;

                return module;
            }

            return null;
        }

        IodineModule LoadExtensionModule(string name)
        {
            var extPath = FindExtension (name);

            if (extPath != null) {
                return LoadLibrary (name, extPath);
            }

            return null;
        }

        static IodineModule LoadLibrary (string module, string dll)
        {
            var extension = Assembly.Load (AssemblyName.GetAssemblyName (dll));

            foreach (Type type in extension.GetTypes ()) {
                if (type.IsDefined (typeof(IodineBuiltinModule), false)) {
                    var attr = (IodineBuiltinModule)type.GetCustomAttributes (
                        typeof(IodineBuiltinModule), false).First ();
                    if (attr.Name == module) {
                        return (IodineModule)type.GetConstructor (new Type[] { }).Invoke (new object[] { });
                    }
                }
            }
            return null;
        }

        string FindModuleSource (string moduleName)
        {
            return FileFile (moduleName, ".id");
        }

        string FindExtension (string extensionName)
        {
            return FileFile (extensionName, ".dll");
        }

        string FileFile (string moduleName, string fileExtension)
        {
            /*
             * Iodine module search algorithm
             * 
             * First attempt to search in the current working directory for the module,
             * 
             * If that fails, try to search in each parent directory
             * 
             * If that fails... We'll try searching in each path contained in IODINE_PATH
             * 
             * If that fails, return null
             */
            if (VirtualMachine.Top == null ||
                VirtualMachine.Top.Module.Location == null) {

                return FindInSearchPath (moduleName, fileExtension);
            }

            var moduleDir = Path.GetDirectoryName (VirtualMachine.Top.Module.Location);

            var file = FindInDirectory (moduleDir, moduleName, fileExtension);

            if (file != null) {
                return file;
            }

            var pathCharCount = moduleDir.Count (
                p => p == Path.PathSeparator ? true : false
            );

            string cd = moduleDir;

            for (int i = 0; i < pathCharCount; i++) {
                cd = cd.Substring (0, cd.LastIndexOf (Path.PathSeparator));

                file = FindInDirectory (cd, moduleName, fileExtension);

                if (file != null) {
                    return file;
                }
            }

            return FindInSearchPath (moduleName, fileExtension);
        }

        string FindInDirectory (
            string directory,
            string moduleName,
            string fileExtension)
        {

            string originalPath = VirtualMachine?.Top?.Module?.Location;

            var relativeFile = Path.Combine (directory, moduleName + fileExtension);
            var depFile = Path.Combine (directory, ".deps", moduleName + fileExtension);

            if (originalPath != null && originalPath == relativeFile) {
                return null;
            }

            if (File.Exists (relativeFile)) {
                return Path.Combine (directory, moduleName + fileExtension);
            }

            if (File.Exists (depFile)) {
                return Path.Combine (directory, ".deps", moduleName + fileExtension);
            }

            if (Directory.Exists (Path.Combine (directory, moduleName))) {
                if (File.Exists (Path.Combine (
                    directory,
                    moduleName,
                    "__init__.id"))) {
                    return Path.Combine (directory, moduleName, "__init__.id");
                }
            }

            if (Directory.Exists (Path.Combine (directory, moduleName))) {
                if (File.Exists (Path.Combine (
                    directory,
                    ".deps",
                    moduleName,
                    "__init__.id"))) {
                    return Path.Combine (directory, moduleName, ".deps", "__init__.id");
                }
            }

            return null;
        }

        string FindInSearchPath (string moduleName, string fileExtension)
        {
            string originalPath = VirtualMachine?.Top?.Module?.Location;

            foreach (string path in SearchPath) {
                var file = FindInDirectory (path, moduleName, fileExtension);

                if (file != null) {
                    return file;
                }
            }


            if (originalPath != null &&
                Path.GetFullPath (originalPath) == Path.GetFullPath (moduleName)) {
                return null;
            }

            if (File.Exists (moduleName)) {
                return moduleName;
            }

            // Module not found!
            return null;
        }
    }
}


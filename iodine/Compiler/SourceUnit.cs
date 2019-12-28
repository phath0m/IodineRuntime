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
using System.Collections.Generic;
using Iodine.Runtime;

namespace Iodine.Compiler
{
    /// <summary>
    /// Represents a unit of Iodine code (Typically a file or a string of code)
    /// </summary>
    public sealed class SourceUnit
    {
        public readonly string Text;
        public readonly string Path;

        public bool HasPath {
            get {
                return Path != null;
            }
        }

        SourceUnit (string source, string path = null)
        {
            Text = source;
            Path = path;
        }

        public static SourceUnit CreateFromFile (string path)
        {
            return new SourceUnit (
                File.ReadAllText (path),
                System.IO.Path.GetFullPath (path)
            );
        }

        public static SourceUnit CreateFromSource (string source)
        {
            return new SourceUnit (source);
        }

        public SourceReader GetReader ()
        {
            return new SourceReader (Text, Path);
        }

        public IodineModule Compile (IodineContext context)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            context.ErrorLog.Clear ();

            string moduleName = Path == null ? "__anonymous__"
                : System.IO.Path.GetFileNameWithoutExtension (Path);

            if (HasPath) {
                var wd = System.IO.Path.GetDirectoryName (Path);
                var depPath = System.IO.Path.Combine (wd, ".deps");

                if (!context.SearchPath.Contains (wd)) {
                    context.SearchPath.Add (wd);
                }

                if (!context.SearchPath.Contains (depPath)) {
                    context.SearchPath.Add (depPath);
                }

                IodineModule cachedModule = null;

                if (LoadCachedModule (ref cachedModule)) {
                    return cachedModule;
                }
            }

            var parser = Parser.CreateParser (context, this);

            var root = parser.Parse ();

            var compiler = IodineCompiler.CreateCompiler (context, root);

            var module = compiler.Compile (moduleName, Path);

            if (Path == null) {
                foreach (KeyValuePair<string, IodineObject> kv in module.Attributes) {
                    context.InteractiveLocals [kv.Key] = kv.Value;
                }
                module.Attributes = context.InteractiveLocals;
            } else if (context.ShouldCache) {
                CacheModule (module);
            }

            return module;
        }

        bool LoadCachedModule (ref IodineModule module)
        {
            var cacheDir = System.IO.Path.Combine (
                System.IO.Path.GetDirectoryName (Path),
                ".iodine_cache"
            );

            if (!Directory.Exists (cacheDir)) {
                return false;
            }

            var filePath = System.IO.Path.Combine (
                cacheDir,
                System.IO.Path.GetFileNameWithoutExtension (Path) + ".bytecode"
            );

            if (!File.Exists (filePath)) {
                return false;
            }

            using (FileStream fs = new FileStream (filePath, FileMode.Open)) {
                var testFile = new BytecodeFile (fs, Path);
                return testFile.TryReadModule (ref module);
            }
        }

        void CacheModule (IodineModule module)
        {
            try {
                var cacheDir = System.IO.Path.Combine (
                    System.IO.Path.GetDirectoryName (Path),
                    ".iodine_cache"
                );

                if (!Directory.Exists (cacheDir)) {
                    Directory.CreateDirectory (cacheDir);
                }

                var filePath = System.IO.Path.Combine (
                    cacheDir,
                    System.IO.Path.GetFileNameWithoutExtension (Path) + ".bytecode"
                );

                using (FileStream fs = new FileStream (filePath, FileMode.OpenOrCreate)) {
                    var testFile = new BytecodeFile (fs, Path);
                    testFile.WriteModule (module as ModuleBuilder);
                }
            } catch (UnauthorizedAccessException) {

            }
        }
    }
}


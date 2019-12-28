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
using System.Net;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;
using Iodine.Compiler;
using Iodine.Runtime;
using Iodine.Runtime.Debug;

namespace Iodine
{
    public class IodineEntry
    {
        private static IodineContext context;

        public static void Main (string[] args)
        {
            context = IodineContext.Create ();

            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) => {
                if (e.ExceptionObject is UnhandledIodineExceptionException) {
                    HandleIodineException (e.ExceptionObject as UnhandledIodineExceptionException);
                }
            };

            IodineOptions options = IodineOptions.Parse (args);

            context.ShouldCache = !options.SupressAutoCache;
            context.ShouldOptimize = !options.SupressOptimizer;

            ExecuteOptions (options);
        }

        private static void HandleIodineException (UnhandledIodineExceptionException ex)
        {
            Console.Error.WriteLine (
                "An unhandled {0} has occured!",
                ex.OriginalException.TypeDef.Name
            );

            Console.Error.WriteLine (
                "\tMessage: {0}",
                ex.OriginalException.GetAttribute ("message").ToString ()
            );

            Console.WriteLine ();
            ex.PrintStack ();
            Console.Error.WriteLine ();
            Panic ("Program terminated.");
        }

        private static bool WaitForDebugger (
            TraceType type,
            VirtualMachine vm,
            StackFrame frame,
            SourceLocation location)
        {
            Console.WriteLine ("Waiting for debugger...");
            return true;
        }

        private static void ExecuteOptions (IodineOptions options)
        {
            if (options.DebugFlag) {
                RunDebugServer ();
            }

            if (options.WarningFlag) {
                context.WarningFilter = WarningType.SyntaxWarning;
            }

            if (options.SupressWarningFlag) {
                context.WarningFilter = WarningType.None;
            }

            switch (options.InterpreterAction) {
            case InterpreterAction.Check:
                CheckIfFileExists (options.FileName);
                CheckSourceUnit (options, SourceUnit.CreateFromFile (options.FileName));
                break;
            case InterpreterAction.ShowVersion:
                DisplayInfo ();
                break;
            case InterpreterAction.ShowHelp:
                DisplayUsage ();
                break;
            case InterpreterAction.EvaluateFile:
                CheckIfFileExists (options.FileName);
                EvalSourceUnit (options, SourceUnit.CreateFromFile (options.FileName));
                break;
            case InterpreterAction.EvaluateArgument:
                EvalSourceUnit (options, SourceUnit.CreateFromSource (options.FileName));
                break;
            case InterpreterAction.Repl:
                LaunchRepl (options);
                break;
            }
        }

        private static void CheckIfFileExists (string file)
        {
            if (!File.Exists (file)) {
                Panic ("Could not find file '{0}'", file);
            }
        }

        private static void LaunchRepl (IodineOptions options, IodineModule module = null)
        {
            string interpreterDir = Path.GetDirectoryName (
                Assembly.GetExecutingAssembly ().Location
            );

            if (module != null) {
                foreach (KeyValuePair<string, IodineObject> kv in module.Attributes) {
                    context.Globals [kv.Key] = kv.Value;
                }
            }

            string iosh = Path.Combine (interpreterDir, "tools", "iosh.id");

            if (File.Exists (iosh) && !options.FallBackFlag) {
                EvalSourceUnit (options, SourceUnit.CreateFromFile (iosh));
            } else {
                ReplShell shell = new ReplShell (context);
                shell.Run ();
            }
        }

        private static void EvalSourceUnit (IodineOptions options, SourceUnit unit)
        {
            try {
                IodineModule module = unit.Compile (context);

                if (context.Debug) {
                    context.VirtualMachine.SetTrace (WaitForDebugger);
                }

                do {
                    context.Invoke (module, new IodineObject[] { });

                    if (module.HasAttribute ("main")) {
                        context.Invoke (module.GetAttribute ("main"), new IodineObject[] {
                            options.IodineArguments
                        });
                    }
                } while (options.LoopFlag);

                if (options.ReplFlag) {
                    LaunchRepl (options, module);
                }

            } catch (UnhandledIodineExceptionException ex) {
                HandleIodineException (ex);
            } catch (SyntaxException ex) {
                DisplayErrors (ex.ErrorLog);
                Panic ("Compilation failed with {0} errors!", ex.ErrorLog.ErrorCount);
            } catch (ModuleNotFoundException ex) {
                Console.Error.WriteLine (ex.ToString ());
                Panic ("Program terminated.");
            } catch (Exception e) {
                Console.Error.WriteLine ("Fatal exception has occured!");
                Console.Error.WriteLine (e.Message);
                Console.Error.WriteLine ("Stack trace: \n{0}", e.StackTrace);
                Console.Error.WriteLine (
                    "\nIodine stack trace \n{0}",
                    context.VirtualMachine.GetStackTrace ()
                );
                Panic ("Program terminated.");
            }

        }

        private static void CheckSourceUnit (IodineOptions options, SourceUnit unit)
        {
            try {
                context.ShouldCache = false;
                unit.Compile (context);
            } catch (SyntaxException ex) {
                DisplayErrors (ex.ErrorLog);
            }
        }

        private static void RunDebugServer ()
        {
            DebugServer server = new DebugServer (context.VirtualMachine);
            Thread debugThread = new Thread (() => {
                server.Start (new IPEndPoint (IPAddress.Loopback, 6569));
            });
            debugThread.Start ();
            Console.WriteLine ("Debug server listening on 127.0.0.1:6569");
        }

        private static IodineConfiguration LoadConfiguration ()
        {
            if (IsUnix ()) {
                if (File.Exists ("/etc/iodine.conf")) {
                    return IodineConfiguration.Load ("/etc/iodine.conf");
                }
            }
            string exePath = Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location);
            string commonAppData = Environment.GetFolderPath (
                Environment.SpecialFolder.CommonApplicationData
            );
            string appData = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);

            if (File.Exists (Path.Combine (exePath, "iodine.conf"))) {
                return IodineConfiguration.Load (Path.Combine (exePath, "iodine.conf"));
            }

            if (File.Exists (Path.Combine (commonAppData, "iodine.conf"))) {
                return IodineConfiguration.Load (Path.Combine (commonAppData, "iodine.conf"));
            }

            if (File.Exists (Path.Combine (appData, "iodine.conf"))) {
                return IodineConfiguration.Load (Path.Combine (appData, "iodine.conf"));
            }

            return new IodineConfiguration (); // If we can't find a configuration file, load the default
        }

        private static void DisplayInfo ()
        {
            int major = Assembly.GetExecutingAssembly ().GetName ().Version.Major;
            int minor = Assembly.GetExecutingAssembly ().GetName ().Version.Minor;
            int patch = Assembly.GetExecutingAssembly ().GetName ().Version.Build;
            Console.WriteLine ("Iodine v{0}.{1}.{2}-alpha", major, minor, patch);
            Environment.Exit (0);
        }

        private static void DisplayErrors (ErrorSink errorLog)
        {
            Dictionary<string, string[]> lineDict = new Dictionary<string, string[]> ();

            foreach (Error err in errorLog) {
                SourceLocation loc = err.Location;

                if (!lineDict.ContainsKey (err.Location.File)) {
                    lineDict [err.Location.File] = File.ReadAllLines (err.Location.File);
                }

                string[] lines = lineDict [err.Location.File];

                Console.Error.WriteLine ("{0} ({1}:{2}) error ID{3:d4}: {4}",
                    Path.GetFileName (loc.File),
                    loc.Line,
                    loc.Column,
                    (int)err.ErrorID,
                    err.Text
                );

                string source = lines [loc.Line];

                Console.Error.WriteLine ("    {0}", source);
                Console.Error.WriteLine ("    {0}", "^".PadLeft (loc.Column));
            }
        }

        private static void DisplayUsage ()
        {
            Console.WriteLine ("usage: iodine [option] ... [file] [arg] ...");
            Console.WriteLine ("\n");
            Console.WriteLine ("-c             Check syntax only");
            Console.WriteLine ("-d             Run a debug server");
            Console.WriteLine ("-e             Evaluate a string of iodine code");
            Console.WriteLine ("-f             Use builtin fallback REPL shell instead of iosh");
            Console.WriteLine ("-h             Display this message");
            Console.WriteLine ("-l             Assume 'while (true) { ...}' loop around the program");
            Console.WriteLine ("-r             Launch an interactive REPL shell after the supplied program is ran");
            Console.WriteLine ("-v             Display the version of this interpreter");
            Console.WriteLine ("-w             Enable all warnings");
            Console.WriteLine ("-x             Disable all warnings");
            Console.WriteLine ("--no-cache     Do not cache compiled code");
            Console.WriteLine ("--no-optimize  Disable bytecode optimizations");
            Environment.Exit (0);
        }

        private static void Panic (string format, params object[] args)
        {
            Console.Error.WriteLine (format, args);
            Environment.Exit (-1);
        }

        public static bool IsUnix ()
        {
            int p = (int)Environment.OSVersion.Platform;
            return (p == 4) || (p == 6) || (p == 128);
        }
    }
}

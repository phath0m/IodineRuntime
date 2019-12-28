using System;
using System.IO;
using System.Collections.Generic;
using Iodine.Runtime; // IodineList

namespace Iodine
{

    public enum InterpreterAction
    {
        Repl,
        Check,
        EvaluateFile,
        EvaluateArgument,
        ShowHelp,
        ShowVersion

    }

    public class IodineOptions
    {
        public string FileName { get; set; }


        public bool DebugFlag {
            protected set;
            get;
        }

        public bool LoopFlag {
            protected set;
            get;
        }

        public bool WarningFlag {
            protected set;
            get;
        }

        public bool SupressWarningFlag {
            protected set;
            get;
        }

        public bool SupressAutoCache {
            protected set;
            get;
        }

        public bool SupressOptimizer {
            protected set;
            get;
        }

        public bool FallBackFlag {
            protected set;
            get;
        }

        public bool ReplFlag {
            protected set;
            get;
        }

        public InterpreterAction InterpreterAction {
            protected set;
            get;
        }

        public IodineList IodineArguments { private set; get; }

        private IodineOptions () {
            InterpreterAction = InterpreterAction.Repl;
        }

        public static IodineOptions Parse (string[] args)
        {
            IodineOptions ret = new IodineOptions ();
            int i;
            bool sentinel = true;
            for (i = 0; i < args.Length && sentinel; i++) {
                switch (args [i]) {
                case "-d":
                case "--debug":
                    ret.DebugFlag = true;
                    break;
                case "-l":
                case "--loop":
                    ret.LoopFlag = true;
                    break;
                case "-w":
                    ret.WarningFlag = true;
                    break;
                case "-x":
                    ret.SupressWarningFlag = true;
                    break;
                case "-f":
                case "--fallback-repl":
                    ret.FallBackFlag = true;
                    break;
                case "-r":
                case "--repl":
                    ret.ReplFlag = true;
                    break;
                case "-c":
                case "--check":
                    ret.InterpreterAction = InterpreterAction.Check;
                    break;
                case "-v":
                case "--version":
                    ret.InterpreterAction = InterpreterAction.ShowVersion;
                    break;
                case "-h":
                case "--help":
                    ret.InterpreterAction = InterpreterAction.ShowHelp;
                    break;
                case "-e":
                case "--eval":
                    ret.InterpreterAction = InterpreterAction.EvaluateArgument;
                    break;
                case "--no-cache":
                    ret.SupressAutoCache = true;
                    break;
                case "--no-optimize":
                    ret.SupressOptimizer = true;
                    break;
                default:
                    if (args [i].StartsWith ("-")) {
                        Console.Error.WriteLine ("Unknown option '{0}'", args [i]);
                    } else {
                        ret.FileName = args [i];

                        if (ret.InterpreterAction == InterpreterAction.Repl) {
                            ret.InterpreterAction = InterpreterAction.EvaluateFile;
                        }
                    }
                    sentinel = false;
                    break;
                }
            }

            IodineObject[] arguments = new IodineObject [
                args.Length - i > 0 ? args.Length - i :
                0
            ];

            int start = i;

            for (; i < args.Length; i++) {
                arguments [i - start] = new IodineString (args [i]);
            }

            ret.IodineArguments = new IodineList (arguments);
            return ret;
        }
    }
}


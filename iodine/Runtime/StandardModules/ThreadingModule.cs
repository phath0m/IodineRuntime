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

using System.Threading;

namespace Iodine.Runtime
{
    [IodineBuiltinModule ("threading")]
    public class ThreadingModule : IodineModule
    {
        class IodineThread : IodineObject
        {
            sealed class ThreadTypeDefinition : IodineTypeDefinition
            {
                public ThreadTypeDefinition ()
                    : base ("Thread")
                {
                    BindAttributes (this);

                    SetDocumentation (
                        "Creates and controls a thread.",
                        "@param func The function to invoke when this thread is created."
                    );
                }

                public override IodineObject BindAttributes (IodineObject obj)
                {
                    obj.SetAttribute ("start", new BuiltinMethodCallback (Start, obj));
                    obj.SetAttribute ("abort", new BuiltinMethodCallback (Abort, obj));
                    obj.SetAttribute ("alive", new BuiltinMethodCallback (Alive, obj));
                    obj.SetAttribute ("join", new BuiltinMethodCallback (Join, obj));
                    return obj;
                }

                public override IodineObject Invoke (VirtualMachine vm, IodineObject [] args)
                {
                    if (args.Length <= 0) {
                        vm.RaiseException (new IodineArgumentException (1));
                        return null;
                    }

                    var func = args [0];

                    var newVm = new VirtualMachine (vm.Context);


                    var threadStart = new ManualResetEvent (false);

                    var t = new Thread (() => {
                        try {
                            threadStart.Set ();
                            func.Invoke (newVm, new IodineObject [] { });
                        } catch (UnhandledIodineExceptionException ex) {
                            vm.RaiseException (ex.OriginalException);
                        }
                    });
                    return new IodineThread (threadStart, t);
                }

                [BuiltinDocString (
                    "Starts the thread."
                )]
                static IodineObject Start (VirtualMachine vm, IodineObject self, IodineObject [] args)
                {
                    var thread = self as IodineThread;

                    if (thread == null) {
                        vm.RaiseException (new IodineTypeException (TypeDefinition.Name));
                        return null;
                    }


                    thread.Start ();

                    return null;
                }

                [BuiltinDocString (
                    "Terminates the thread."
                )]
                static IodineObject Abort (VirtualMachine vm, IodineObject self, IodineObject [] args)
                {
                    var thread = self as IodineThread;

                    if (thread == null) {
                        vm.RaiseException (new IodineTypeException (TypeDefinition.Name));
                        return null;
                    }

                    thread.Value.Abort ();

                    return null;
                }

                [BuiltinDocString (
                    "Returns true if this thread is alive, false if it is not."
                )]
                static IodineObject Alive (VirtualMachine vm, IodineObject self, IodineObject [] args)
                {
                    var thread = self as IodineThread;

                    if (thread == null) {
                        vm.RaiseException (new IodineTypeException (TypeDefinition.Name));
                        return null;
                    }

                    return IodineBool.Create (thread.Value.IsAlive);
                }

                [BuiltinDocString (
                    "Joins this thread with the calling thread"
                )]
                static IodineObject Join (VirtualMachine vm, IodineObject self, IodineObject [] args)
                {
                    var thread = self as IodineThread;

                    if (thread == null) {
                        vm.RaiseException (new IodineTypeException (TypeDefinition.Name));
                        return null;
                    }


                    thread.Value.Join ();

                    return null;
                }
            }

            public static readonly IodineTypeDefinition TypeDefinition = new ThreadTypeDefinition ();

            public readonly Thread Value;

            readonly ManualResetEvent startEvent;

            public IodineThread (ManualResetEvent startEvent, Thread t)
                : base (TypeDefinition)
            {
                Value = t;
                this.startEvent = startEvent;
            }

            public void Start ()
            {
                Value.Start ();

                startEvent.WaitOne ();
            }
        }


        class IodineConditionVariable : IodineObject
        {
            public static readonly IodineTypeDefinition TypeDefinition = new ConditionVariableTypeDefinition ();

            sealed class ConditionVariableTypeDefinition : IodineTypeDefinition
            {
                public ConditionVariableTypeDefinition ()
                    : base ("ConditionVariable")
                {
                    BindAttributes (this);
                }

                public override IodineObject BindAttributes (IodineObject obj)
                {
                    obj.SetAttribute ("wait", new BuiltinMethodCallback (Wait, obj));
                    obj.SetAttribute ("signal", new BuiltinMethodCallback (Signal, obj));
                    return obj;
                }

                public override IodineObject Invoke (VirtualMachine vm, IodineObject [] args)
                {
                    return new IodineConditionVariable ();
                }

                [BuiltinDocString (
                    "Waits the condition variable to be signaled and releases the ",
                    "supplied mutex.",
                    "@param mutex The mutex"
                )]
                static IodineObject Wait (VirtualMachine vm, IodineObject self, IodineObject [] args)
                {
                    var cv = self as IodineConditionVariable;

                    if (cv == null) {
                        vm.RaiseException (new IodineFunctionInvocationException ());
                        return null;
                    }

                    if (args.Length == 0) {
                        vm.RaiseException (new IodineArgumentException (1));
                        return null;
                    }

                    var mutex = args [0] as IodineMutex;

                    if (mutex == null) {
                        vm.RaiseException (new IodineTypeException ("Mutex"));
                        return null;
                    }

                    mutex.Release ();

                    cv.Wait ();

                    mutex.Acquire ();

                    return null;
                }

                [BuiltinDocString (
                    "Signals the first thread waiting for this condition variable."
                )]
                static IodineObject Signal (VirtualMachine vm, IodineObject self, IodineObject [] args)
                {
                    var cv = self as IodineConditionVariable;

                    if (cv == null) {
                        vm.RaiseException (new IodineFunctionInvocationException ());
                        return null;
                    }

                    cv.Signal ();

                    return null;
                }

            }

            readonly ManualResetEvent resetEvent = new ManualResetEvent (false);

            public IodineConditionVariable ()
                : base (TypeDefinition)
            {
            }

            public void Wait ()
            {
                resetEvent.WaitOne ();
            }

            public void Signal ()
            {
                resetEvent.Set ();
            }
        }

        class IodineMutex : IodineObject
        {
            public static readonly IodineTypeDefinition TypeDefinition = new MutexTypeDefinition ();

            sealed class MutexTypeDefinition : IodineTypeDefinition
            {
                public MutexTypeDefinition ()
                    : base ("Mutex")
                {
                    BindAttributes (this);
                    SetDocumentation (
                        "A simple mutual exclusion lock."
                    );
                }

                public override IodineObject BindAttributes (IodineObject obj)
                {
                    obj.SetAttribute ("acquire", new BuiltinMethodCallback (Acquire, obj));
                    obj.SetAttribute ("release", new BuiltinMethodCallback (Release, obj));
                    obj.SetAttribute ("synchronize", new BuiltinMethodCallback (Synchronize, obj));
                    return obj;
                }

                public override IodineObject Invoke (VirtualMachine vm, IodineObject [] args)
                {
                    return new IodineMutex ();
                }

                [BuiltinDocString (
                    "Enters the critical section, blocking all threads until release the lock is released."
                )]
                static IodineObject Acquire (VirtualMachine vm, IodineObject self, IodineObject [] args)
                {
                    var spinlock = self as IodineMutex;

                    if (spinlock == null) {
                        vm.RaiseException (new IodineFunctionInvocationException ());
                        return null;
                    }

                    spinlock.Acquire ();
                    return null;
                }

                [BuiltinDocString (
                    "Releases the mutex, allowing any threads blocked by this lock to continue."
                )]
                static IodineObject Release (VirtualMachine vm, IodineObject self, IodineObject [] args)
                {
                    var spinlock = self as IodineMutex;

                    if (spinlock == null) {
                        vm.RaiseException (new IodineFunctionInvocationException ());
                        return null;
                    }

                    spinlock.Release ();
                    return null;
                }
                [BuiltinDocString (
                    "Acquires a lock, then executes the supplied argument before releasing the lock.",
                    "@param callable The function to synchronize"
                )]
                static IodineObject Synchronize (VirtualMachine vm, IodineObject self, IodineObject [] args)
                {
                    var mutex = self as IodineMutex;

                    if (mutex == null) {
                        vm.RaiseException (new IodineFunctionInvocationException ());
                        return null;
                    }

                    if (args.Length == 0) {
                        vm.RaiseException (new IodineArgumentException (1));
                        return null;
                    }

                    var func = args [0];

                    mutex.Acquire ();

                    func.Invoke (vm, new IodineObject [] { });

                    mutex.Release ();

                    return null;
                }
            }

            readonly Mutex mutex;

            public IodineMutex ()
                : base (TypeDefinition)
            {
                mutex = new Mutex ();
            }

            public void Acquire ()
            {
                mutex.WaitOne ();
            }

            public void Release ()
            {
                mutex.ReleaseMutex ();
            }

        }



        public ThreadingModule ()
            : base ("threading")
        {
            SetAttribute ("Thread", IodineThread.TypeDefinition);
            SetAttribute ("Mutex", IodineMutex.TypeDefinition);
            SetAttribute ("ConditionVariable", IodineConditionVariable.TypeDefinition);
            SetAttribute ("sleep", new BuiltinMethodCallback (Sleep, this));
        }

        [BuiltinDocString (
            "Suspends the current thread for t milliseconds.",
            "@param t How many milliseconds to suspend the thread for"
        )]
        IodineObject Sleep (VirtualMachine vm, IodineObject self, IodineObject [] args)
        {
            if (args.Length <= 0) {
                vm.RaiseException (new IodineArgumentException (1));
            }
            var time = args [0] as IodineInteger;

            Thread.Sleep ((int)time.Value);

            return null;
        }
    }
}


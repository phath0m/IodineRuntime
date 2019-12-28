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

using System.IO;
using System.Collections.Generic;
using Iodine.Runtime;
using Iodine.Compiler.Ast;

namespace Iodine.Compiler
{
    /// <summary>
    /// Responsible for compiling an Iodine abstract syntax tree into iodine bytecode. 
    /// </summary>
    public class IodineCompiler : AstVisitor
    {
        static List<IBytecodeOptimization> Optimizations = new List<IBytecodeOptimization> ();

        static IodineCompiler ()
        {
            Optimizations.Add (new ControlFlowOptimization ());
            Optimizations.Add (new InstructionOptimization ());
        }

        Stack<EmitContext> emitContexts = new Stack<EmitContext> ();

        public EmitContext Context {
            get {
                return emitContexts.Peek ();
            }
        }

        SymbolTable symbolTable;
        CompilationUnit root;

        int _nextTemporary = 2048;

        IodineCompiler (SymbolTable symbolTable, CompilationUnit root)
        {
            this.symbolTable = symbolTable;
            this.root = root;
        }

        public static IodineCompiler CreateCompiler (IodineContext context, CompilationUnit root)
        {
            var analyser = new SemanticAnalyser (context.ErrorLog);
            var table = analyser.Analyse (root);

            if (context.ErrorLog.ErrorCount > 0) {
                throw new SyntaxException (context.ErrorLog);
            }

            return new IodineCompiler (table, root);
        }

        public IodineModule Compile (string moduleName, string filePath)
        {
            var moduleBuilder = new ModuleBuilder (moduleName, filePath);
            var context = new EmitContext (symbolTable, moduleBuilder, moduleBuilder.Initializer);

            context.SetCurrentModule (moduleBuilder);

            emitContexts.Push (context);

            root.Visit (this);

            moduleBuilder.Initializer.FinalizeMethod ();

            DestroyContext ();

            return moduleBuilder;
        }

        void OptimizeObject (CodeBuilder code)
        {
            foreach (IBytecodeOptimization opt in Optimizations) {
                opt.PerformOptimization (code);
            }
        }

        void CreateContext (bool isInClassBody = false)
        {
            emitContexts.Push (new EmitContext (Context.SymbolTable,
                Context.CurrentModule,
                Context.CurrentMethod,
                Context.IsInClass,
                isInClassBody
            ));
        }

        void CreateContext (CodeBuilder methodBuilder)
        {
            emitContexts.Push (new EmitContext (Context.SymbolTable,
                Context.CurrentModule,
                methodBuilder,
                Context.IsInClass
            ));
        }

        void CreatePatternContext (IodineObject temporary)
        {
            emitContexts.Push (new EmitContext (Context.SymbolTable,
                Context.CurrentModule,
                Context.CurrentMethod,
                Context.IsInClass,
                false,
                true,
                temporary
            ));
        }

        void DestroyContext ()
        {
            emitContexts.Pop ();
        }

        IodineObject CreateTemporary ()
        {
            return new IodineName ("$tmp" + (_nextTemporary++).ToString ());
        }

        IodineObject CreateName (string name)
        {
            return new IodineName (name);
        }

        public override void Accept (CompilationUnit ast)
        {
            ast.VisitChildren (this);
        }

        #region Declarations

        void CompileClass (ClassDeclaration classDecl)
        {
            Context.SymbolTable.AddSymbol (classDecl.Name);

            CreateContext (true);

            foreach (AstNode member in classDecl.Members) {
                member.Visit (this);
            }

            DestroyContext ();

            foreach (AstNode contract in classDecl.Interfaces) {
                contract.Visit (this);
            }


            Context.CurrentMethod.EmitInstruction (
                Opcode.BuildTuple,
                classDecl.Interfaces.Count
            );

            if (classDecl.BaseClass != null) {
                classDecl.BaseClass.Visit (this);
            } else {
                Context.CurrentMethod.EmitInstruction (Opcode.LoadNull);
            }

            CompileMethod (classDecl.Constructor);

            Context.CurrentMethod.EmitInstruction (
                Opcode.LoadConst,
                new IodineString (classDecl.Documentation)
            );

            Context.CurrentMethod.EmitInstruction (
                Opcode.LoadConst,
                CreateName (classDecl.Name)
            );

            Context.CurrentMethod.EmitInstruction (
                Opcode.BuildClass,
                classDecl.Members.Count
            );

        }

        void CompileContract (ContractDeclaration contractDecl)
        {
            Context.SymbolTable.AddSymbol (contractDecl.Name);

            foreach (AstNode member in contractDecl.Members) {
                if (member is FunctionDeclaration) {
                    var funcDecl = member as FunctionDeclaration;
                    CompileMethod (funcDecl);
                }
            }

            Context.CurrentMethod.EmitInstruction (
                Opcode.LoadConst,
                CreateName (contractDecl.Name)
            );

            Context.CurrentMethod.EmitInstruction (
                Opcode.BuildContract,
                contractDecl.Members.Count
            );

        }

        void CompileTrait (TraitDeclaration traitDecl)
        {
            Context.SymbolTable.AddSymbol (traitDecl.Name);

            foreach (AstNode member in traitDecl.Members) {
                if (member is FunctionDeclaration) {
                    var funcDecl = member as FunctionDeclaration;
                    CompileMethod (funcDecl);
                }
            }

            Context.CurrentMethod.EmitInstruction (
                Opcode.LoadConst,
                CreateName (traitDecl.Name)
            );

            Context.CurrentMethod.EmitInstruction (
                Opcode.BuildTrait,
                traitDecl.Members.Count
            );

        }

        void CompileMixin (MixinDeclaration mixinDecl)
        {
            Context.SymbolTable.AddSymbol (mixinDecl.Name);

            /*
             * <mixin members>
             */

            foreach (AstNode member in mixinDecl.Members) {
                var funcDecl = member as FunctionDeclaration;

                if (funcDecl != null) {
                    
                    CompileMethod (funcDecl);

                    /*
                     * LOAD_CONST   funcDecl.name
                     */

                    Context.CurrentMethod.EmitInstruction (
                        Opcode.LoadConst,
                        CreateName (funcDecl.Name)
                    );
                }
            }

            /*
             * LOAD_CONST       mixinDecl.Documentation
             * LOAD_CONST       mixinDecl.Name
             * BUILD_CLASS      mixinDecl.Members.Count
             */

            Context.CurrentMethod.EmitInstruction (
                Opcode.LoadConst,
                new IodineString (mixinDecl.Documentation)
            );

            Context.CurrentMethod.EmitInstruction (
                Opcode.LoadConst,
                CreateName (mixinDecl.Name)
            );

            Context.CurrentMethod.EmitInstruction (
                Opcode.BuildClass,
                mixinDecl.Members.Count
            );

        }

        void CompileEnum (EnumDeclaration enumDecl)
        {
            Context.SymbolTable.AddSymbol (enumDecl.Name);

            foreach (var key in enumDecl.Items) {

                /*
                 * LOAD_CONST   key.Key
                 * LOAD_CONST   key.Value
                 */

                Context.CurrentMethod.EmitInstruction (
                    Opcode.LoadConst,
                    CreateName (key.Key)
                );

                Context.CurrentMethod.EmitInstruction (
                    Opcode.LoadConst,
                    new IodineInteger (key.Value)
                );

            }

            /*
             * LOAD_CONST       enumDecl.Name
             * BUILD_ENUM       enumDecl.Items.Count
             */

            Context.CurrentMethod.EmitInstruction (
                Opcode.LoadConst,
                CreateName (enumDecl.Name)
            );

            Context.CurrentMethod.EmitInstruction (
                Opcode.BuildEnum,
                enumDecl.Items.Count
            );
        }


        void CompileMethodParameters (FunctionParameter parameter)
        {
            var namedParam = parameter as NamedParameter;

            if (namedParam != null) {
                Context.SymbolTable.AddSymbol (namedParam.Name);

                if (namedParam.HasType) {
                    namedParam.Type.Visit (this);

                    Context.CurrentMethod.EmitInstruction (
                        Opcode.CastLocal,
                        CreateName (namedParam.Name)
                    );
                }
            }

            var tupleParam = parameter as DecompositionParameter;

            if (tupleParam != null) {
                foreach (FunctionParameter subparam in tupleParam.CaptureNames) {
                    CompileMethodParameters (subparam);
                }
            }
        }

        void LoadParameterNames (FunctionParameter parameter)
        {
            var namedParam = parameter as NamedParameter;

            if (namedParam != null) {
                Context.CurrentMethod.EmitInstruction (
                    Opcode.LoadConst,
                    new IodineName (namedParam.Name)
                );
            }

            var tupleParam = parameter as DecompositionParameter;

            if (tupleParam != null) {

                foreach (FunctionParameter param in tupleParam.CaptureNames) {
                    LoadParameterNames (param);
                }

                Context.CurrentMethod.EmitInstruction (
                    Opcode.BuildTuple,
                    tupleParam.CaptureNames.Count
                );
            }
        }

        void CompileMethod (Function funcDecl)
        {
            Context.SymbolTable.AddSymbol (funcDecl.Name);

            Context.SymbolTable.EnterScope ();

            var bytecode = new CodeBuilder ();

            CreateContext (bytecode);

            foreach (FunctionParameter param in funcDecl.Parameters) {
                CompileMethodParameters (param);
            }

            funcDecl.VisitChildren (this);

            DestroyContext ();

            bytecode.FinalizeMethod ();

            OptimizeObject (bytecode);

            var flags = new MethodFlags ();

            if (funcDecl.AcceptsKeywordArgs) {
                flags |= MethodFlags.AcceptsKwargs;
            }

            if (funcDecl.Variadic) {
                flags |= MethodFlags.AcceptsVarArgs;
            }

            if (funcDecl.HasDefaultValues) {
                flags |= MethodFlags.HasDefaultParameters;

                var startingIndex = funcDecl.Parameters.FindIndex (
                    p => p is NamedParameter && ((NamedParameter)p).HasDefaultValue
                );

                int defaultParamCount = 0;

                Context.CurrentMethod.EmitInstruction (
                    Opcode.LoadConst,
                    new IodineInteger (startingIndex)
                );

                foreach (FunctionParameter param in funcDecl.Parameters) {

                    var namedParameter = param as NamedParameter; 

                    if (namedParameter != null && namedParameter.HasDefaultValue) {
                        namedParameter.DefaultValue.Visit (this);
                        defaultParamCount++;
                    }
                }

                Context.CurrentMethod.EmitInstruction (
                    Opcode.BuildTuple,
                    defaultParamCount
                );
            }

            foreach (FunctionParameter param in funcDecl.Parameters) {
                LoadParameterNames (param);
            }

            /*
             * BUILD_TUPLE      funcDecl.Parameters.Count
             * LOAD_CONST       bytecode
             * LOAD_CONST       funcDecl.Documentation
             * LOAD_CONST       funcDecl.Name
             * BUILD_FUNCTION   flags
             */

            Context.CurrentMethod.EmitInstruction (
                Opcode.BuildTuple,
                funcDecl.Parameters.Count
            );

            Context.CurrentMethod.EmitInstruction (
                Opcode.LoadConst,
                bytecode
            );

            Context.CurrentMethod.EmitInstruction (
                Opcode.LoadConst,
                new IodineString (funcDecl.Documentation)
            );

            Context.CurrentMethod.EmitInstruction (
                Opcode.LoadConst,
                new IodineString (funcDecl.Name)
            );

            Context.CurrentMethod.EmitInstruction (
                Opcode.BuildFunction,
                (int)flags
            );

            symbolTable.ExitScope ();
        }

        public override void Accept (ClassDeclaration classDecl)
        {
            CompileClass (classDecl);

            if (Context.IsInClassBody) {
                Context.CurrentMethod.EmitInstruction (
                    Opcode.LoadConst,
                    CreateName (classDecl.Name)
                );
                return;
            }

            if (symbolTable.IsInGlobalScope) {
                Context.CurrentMethod.EmitInstruction (
                    Opcode.StoreGlobal,
                    CreateName (classDecl.Name)
                );
                return;
            } 

            Context.CurrentMethod.EmitInstruction (
                Opcode.StoreLocal,
                CreateName (classDecl.Name)
            );

        }

        public override void Accept (ContractDeclaration interfaceDecl)
        {
            CompileContract (interfaceDecl);

            if (Context.IsInClassBody) {
                
                Context.CurrentMethod.EmitInstruction (
                    Opcode.LoadConst,
                    CreateName (interfaceDecl.Name)
                );

                return;
            }

            if (symbolTable.IsInGlobalScope) {
                Context.CurrentMethod.EmitInstruction (
                    Opcode.StoreGlobal,
                    CreateName (interfaceDecl.Name)
                );
                return;
            }

            Context.CurrentMethod.EmitInstruction (
                Opcode.StoreLocal,
                CreateName (interfaceDecl.Name)
            );

        }

        public override void Accept (TraitDeclaration traitDecl)
        {
            CompileTrait (traitDecl);

            if (Context.IsInClassBody) {
                Context.CurrentMethod.EmitInstruction (
                    Opcode.LoadConst, CreateName (traitDecl.Name)
                );
            } else if (symbolTable.IsInGlobalScope) {
                Context.CurrentMethod.EmitInstruction (
                    Opcode.StoreGlobal,
                    CreateName (traitDecl.Name)
                );
            } else {
                Context.CurrentMethod.EmitInstruction (
                    Opcode.StoreLocal,
                    CreateName (traitDecl.Name)
                );
            }
        }

        public override void Accept (MixinDeclaration traitDecl)
        {
            CompileMixin (traitDecl);

            if (Context.IsInClassBody) {
                Context.CurrentMethod.EmitInstruction (
                    Opcode.LoadConst,
                    CreateName (traitDecl.Name)
                );
            } else if (symbolTable.IsInGlobalScope) {
                Context.CurrentMethod.EmitInstruction (
                    Opcode.StoreGlobal,
                    CreateName (traitDecl.Name)
                );
            } else {
                Context.CurrentMethod.EmitInstruction (
                    Opcode.StoreLocal,
                    CreateName (traitDecl.Name)
                );
            }
        }

        public override void Accept (EnumDeclaration enumDecl)
        {
            CompileEnum (enumDecl);

            if (Context.IsInClassBody) {
                Context.CurrentMethod.EmitInstruction (Opcode.LoadConst, CreateName (enumDecl.Name));
            } else if (symbolTable.IsInGlobalScope) {
                Context.CurrentMethod.EmitInstruction (Opcode.StoreGlobal, CreateName (enumDecl.Name));
            } else {
                Context.CurrentMethod.EmitInstruction (Opcode.StoreLocal, CreateName (enumDecl.Name));
            }
        }

        public override void Accept (FunctionDeclaration funcDecl)
        {
            CompileMethod (funcDecl);

            if (Context.IsInClassBody) {
                Context.CurrentMethod.EmitInstruction (Opcode.LoadConst, CreateName (funcDecl.Name));
            } else if (symbolTable.IsInGlobalScope) {
                Context.CurrentMethod.EmitInstruction (Opcode.StoreGlobal, CreateName (funcDecl.Name));
            } else {
                Context.CurrentMethod.EmitInstruction (funcDecl.Location, Opcode.BuildClosure);
                Context.CurrentMethod.EmitInstruction (Opcode.StoreLocal, CreateName (funcDecl.Name));
            }
        }

        public override void Accept (DecoratedFunction funcDecl)
        {
            CompileMethod (funcDecl.Function);

            if (!(Context.IsInClassBody || symbolTable.IsInGlobalScope)) {
                Context.CurrentMethod.EmitInstruction (Opcode.BuildClosure);
            }
            funcDecl.Decorator.Visit (this);
            Context.CurrentMethod.EmitInstruction (Opcode.Invoke, 1);

            if (Context.IsInClassBody) {
                Context.CurrentMethod.EmitInstruction (Opcode.LoadConst, CreateName (funcDecl.Function.Name));
            } else if (symbolTable.IsInGlobalScope) {
                Context.CurrentMethod.EmitInstruction (Opcode.StoreGlobal, CreateName (funcDecl.Function.Name));
            } else {
                Context.CurrentMethod.EmitInstruction (Opcode.StoreLocal, CreateName (funcDecl.Function.Name));
            }
        }

        public override void Accept (CodeBlock scope)
        {
            Context.SymbolTable.EnterScope ();
            scope.VisitChildren (this);
            Context.SymbolTable.ExitScope ();
        }

        public override void Accept (StatementList stmtList)
        {
            stmtList.VisitChildren (this);
        }

        #endregion

        #region Statements

        public override void Accept (UseStatement useStmt)
        {
            string import = !useStmt.Relative ? useStmt.Module : Path.Combine (
                Path.GetDirectoryName (useStmt.Location.File),
                useStmt.Module);

            /*
             * Implementation detail: The use statement in all reality is simply an 
             * alias for the function require (); Here we translate the use statement
             * into a call to the require function
             */

            if (useStmt.Wildcard) {

                /*
                 * LOAD_CONST   import
                 * BUILD_TUPLE  0 (Note: This is an empty tuple)
                 * LOAD_GLOBAL  require
                 * INVOKE       2
                 * POP
                 */

                Context.CurrentModule.Initializer.EmitInstruction (
                    useStmt.Location,
                    Opcode.LoadConst,
                    new IodineString (import)
                );

                Context.CurrentModule.Initializer.EmitInstruction (
                    useStmt.Location, 
                    Opcode.BuildTuple, 
                    0
                );
                Context.CurrentModule.Initializer.EmitInstruction (
                    useStmt.Location,
                    Opcode.LoadGlobal,
                    new IodineName ("require")
                );

                Context.CurrentModule.Initializer.EmitInstruction (
                    useStmt.Location, 
                    Opcode.Invoke,
                    2
                );

                Context.CurrentModule.Initializer.EmitInstruction (
                    useStmt.Location,
                    Opcode.Pop
                );
                return;

            } 


            Context.CurrentModule.Initializer.EmitInstruction (
                useStmt.Location,
                Opcode.LoadConst,
                new IodineString (import)
            );

            if (useStmt.Imports.Count > 0) {

                for (int i = 0; i < useStmt.Imports.Count; i++) {

                    Context.CurrentMethod.EmitInstruction (
                        useStmt.Location,
                        Opcode.LoadConst,
                        new IodineString (useStmt.Imports [i])
                    );
                }

                Context.CurrentMethod.EmitInstruction (
                    useStmt.Location,
                    Opcode.BuildTuple,
                    useStmt.Imports.Count
                );
            }

            Context.CurrentMethod.EmitInstruction (
                useStmt.Location,
                Opcode.LoadGlobal,
                CreateName ("require")
            );

            Context.CurrentMethod.EmitInstruction (
                useStmt.Location,
                Opcode.Invoke,
                useStmt.Imports.Count == 0 ? 1 : 2
            );

            Context.CurrentMethod.EmitInstruction (useStmt.Location, Opcode.Pop);


        }
        public override void Accept (ExtendStatement exten)
        {
            exten.Class.Visit (this);

            foreach (AstNode member in exten.Members) {
                var funcDecl = member as FunctionDeclaration;

                if (funcDecl != null) {
                    CompileMethod (funcDecl);
                    Context.CurrentMethod.EmitInstruction (
                        Opcode.LoadConst,
                        CreateName (funcDecl.Name)
                    );
                }
            }


            /*
             * LOAD_CONST       __anonymous__
             * BUILD_MIXEN
             * INCLUDE_MIXEN
             */

            Context.CurrentMethod.EmitInstruction (
                Opcode.LoadConst,
                CreateName ("__anonymous__")
            );

            Context.CurrentMethod.EmitInstruction (
                Opcode.BuildMixin,
                exten.Members.Count
            );

            Context.CurrentMethod.EmitInstruction (
                exten.Location,
                Opcode.IncludeMixin
            );

            foreach (AstNode node in exten.Mixins) {
                node.Visit (this);
                Context.CurrentMethod.EmitInstruction (
                    exten.Location,
                    Opcode.IncludeMixin
                );
            }
        }

        public override void Accept (Statement stmt)
        {
            stmt.VisitChildren (this);
        }

        public override void Accept (TryExceptStatement tryCatch)
        {
            var exceptLabel = Context.CurrentMethod.CreateLabel ();
            var endLabel = Context.CurrentMethod.CreateLabel ();

            /*
             * PUSH_EXCEPTION_HANDLER   exceptLabel
             * <try body>
             * POP_EXCEPTION_HANDLER
             * JUMP                     endLabel
             */

            Context.CurrentMethod.EmitInstruction (
                tryCatch.Location,
                Opcode.PushExceptionHandler,
                exceptLabel
            );

            tryCatch.TryBody.Visit (this);

            Context.CurrentMethod.EmitInstruction (
                tryCatch.TryBody.Location,
                Opcode.PopExceptionHandler
            );


            Context.CurrentMethod.EmitInstruction (
                tryCatch.TryBody.Location,
                Opcode.Jump,
                endLabel
            );

            /*
             * exceptLabel: 
             */

            Context.CurrentMethod.MarkLabelPosition (exceptLabel);

            tryCatch.TypeList.Visit (this); // except e as <type list>

            if (tryCatch.TypeList.Arguments.Count > 0) {

                /*
                 * <except type list>
                 * BEGIN_EXCEPT             TypeList.Arguments.Count
                 * 
                 * FYI: This instructiion basically list matches the current
                 * exception with the list of types that have been pushed
                 * onto the stack, to see whether or not this exception handler
                 * is valid for the thrown exception
                 */

                Context.CurrentMethod.EmitInstruction (
                    tryCatch.ExceptBody.Location,
                    Opcode.BeginExcept,
                    tryCatch.TypeList.Arguments.Count
                );
            }

            if (tryCatch.ExceptionIdentifier != null) {

                /*
                 * LOAD_EXCEPTION
                 * STORE_LOCAL              <try ExceptionIdentifier>
                 */

                Context.SymbolTable.AddSymbol (tryCatch.ExceptionIdentifier);

                Context.CurrentMethod.EmitInstruction (
                    tryCatch.ExceptBody.Location,
                    Opcode.LoadException
                );

                Context.CurrentMethod.EmitInstruction (tryCatch.ExceptBody.Location,
                    Opcode.StoreLocal,
                    CreateName (tryCatch.ExceptionIdentifier)
                );
            }

            tryCatch.ExceptBody.Visit (this);

            /*
             * endLabel:
             */

            Context.CurrentMethod.MarkLabelPosition (endLabel);
        }

        public override void Accept (WithStatement with)
        {
            Context.SymbolTable.EnterScope ();

            /*
             * BEGIN_WITH
             * <with expression>
             */

            with.Expression.Visit (this);

            Context.CurrentMethod.EmitInstruction (
                with.Location,
                Opcode.BeginWith
            );

            /*
             * <with body>
             * END_WITH
             */

            with.Body.Visit (this);

            Context.CurrentMethod.EmitInstruction (
                with.Location,
                Opcode.EndWith
            );

            Context.SymbolTable.ExitScope ();
        }

        public override void Accept (IfStatement ifStmt)
        {
            var elseLabel = Context.CurrentMethod.CreateLabel ();
            var endLabel = Context.CurrentMethod.CreateLabel ();

            /*
             * <if condition>
             * JUMP_IF_FALSE        elseLabel
             */

            ifStmt.Condition.Visit (this);

            Context.CurrentMethod.EmitInstruction (
                ifStmt.Body.Location,
                Opcode.JumpIfFalse,
                elseLabel
            );

            /*
             * <if body>
             * JUMP                 endLabel
             */

            ifStmt.Body.Visit (this);

            Context.CurrentMethod.EmitInstruction (ifStmt.ElseBody != null
                ? ifStmt.ElseBody.Location
                : ifStmt.Location,
                Opcode.Jump,
                endLabel
            );

            /*
             * elseLabel:
             * <if else-body>
             */

            Context.CurrentMethod.MarkLabelPosition (elseLabel);

            if (ifStmt.ElseBody != null) {
                ifStmt.ElseBody.Visit (this);
            }

            /*
             * endLabel:
             */

            Context.CurrentMethod.MarkLabelPosition (endLabel);
        }

        public override void Accept (WhileStatement whileStmt)
        {
            var whileLabel = Context.CurrentMethod.CreateLabel ();
            var breakLabel = Context.CurrentMethod.CreateLabel ();

            Context.BreakLabels.Push (breakLabel);
            Context.ContinueLabels.Push (whileLabel);

            Context.CurrentMethod.MarkLabelPosition (whileLabel);

            /*
             * whileLabel:
             * <while condition>
             */

            whileStmt.Condition.Visit (this);

            /*
             * JUMP_IF_FALSE    breakLabel
             */

            Context.CurrentMethod.EmitInstruction (
                whileStmt.Condition.Location,
                Opcode.JumpIfFalse,
                breakLabel
            );

            /*
             * <while body>
             * JUMP             whileLabel
             * breakLabel:
             */

            whileStmt.Body.Visit (this);

            Context.CurrentMethod.EmitInstruction (
                whileStmt.Body.Location,
                Opcode.Jump,
                whileLabel
            );

            Context.CurrentMethod.MarkLabelPosition (breakLabel);

            Context.BreakLabels.Pop ();
            Context.ContinueLabels.Pop ();
        }

        public override void Accept (DoStatement doStmt)
        {
            var doLabel = Context.CurrentMethod.CreateLabel ();
            var breakLabel = Context.CurrentMethod.CreateLabel ();

            Context.BreakLabels.Push (breakLabel);
            Context.ContinueLabels.Push (doLabel);

            Context.CurrentMethod.MarkLabelPosition (doLabel);

            /*
             * doLabel:
             */

            doStmt.Body.Visit (this);

            doStmt.Condition.Visit (this);

            Context.CurrentMethod.EmitInstruction (doStmt.Condition.Location,
                Opcode.JumpIfTrue,
                doLabel
            );

            /*
             * JUMP_IF_FALSE        doLabel
             */

            Context.CurrentMethod.MarkLabelPosition (breakLabel);

            Context.BreakLabels.Pop ();
            Context.ContinueLabels.Pop ();
        }

        public override void Accept (ForStatement forStmt)
        {
            var forLabel = Context.CurrentMethod.CreateLabel ();
            var breakLabel = Context.CurrentMethod.CreateLabel ();
            var skipAfterThought = Context.CurrentMethod.CreateLabel ();

            Context.BreakLabels.Push (breakLabel);
            Context.ContinueLabels.Push (forLabel);

            forStmt.Initializer.Visit (this);

            /*
             * JUMP         skipAfterThought
             */

            Context.CurrentMethod.EmitInstruction (
                forStmt.Location,
                Opcode.Jump,
                skipAfterThought
            );

            Context.CurrentMethod.MarkLabelPosition (forLabel);

            forStmt.AfterThought.Visit (this);

            Context.CurrentMethod.MarkLabelPosition (skipAfterThought);

            forStmt.Condition.Visit (this);

            /*
             * <for initializer>
             * JUMP skipAfterThought
             * <for afterthought>
             * skipAfterThought:
             * <for condition>
             * JUMP_IF_FALSE    breakLabel
             * <for body>
             * <for afterthought>
             * JUMP
             */

            Context.CurrentMethod.EmitInstruction (
                forStmt.Condition.Location,
                Opcode.JumpIfFalse,
                breakLabel
            );

            forStmt.Body.Visit (this);
            forStmt.AfterThought.Visit (this);

            Context.CurrentMethod.EmitInstruction (
                forStmt.AfterThought.Location,
                Opcode.Jump,
                skipAfterThought
            );

            Context.CurrentMethod.MarkLabelPosition (breakLabel);

            Context.BreakLabels.Pop ();
            Context.ContinueLabels.Pop ();
        }

        public override void Accept (ForeachStatement foreachStmt)
        {
            var tmp = CreateTemporary ();

            var breakLabel = Context.CurrentMethod.CreateLabel (); // End of foreach
            var foreachLabel = Context.CurrentMethod.CreateLabel (); // beginning of foreach

            Context.BreakLabels.Push (breakLabel);
            Context.ContinueLabels.Push (foreachLabel);

            foreachStmt.Iterator.Visit (this);

            Context.SymbolTable.EnterScope ();

            /*
             * foreachloop:
             * 
             * GET_ITER
             * DUP
             * STORE_LOCAL      tmp
             * ITER_RESET
             * LOAD_LOCAL       tmp
             * ITER_MOVE_NEXT
             * JUMP_IF_FALSE    breakFromLoop
             * LOAD_LOCAL       tmp
             * ITER_GET_NEXT
             */

            Context.CurrentMethod.EmitInstruction (foreachStmt.Iterator.Location, Opcode.GetIter);
            Context.CurrentMethod.EmitInstruction (foreachStmt.Iterator.Location, Opcode.Dup);

            Context.CurrentMethod.EmitInstruction (
                foreachStmt.Iterator.Location,
                Opcode.StoreLocal,
                tmp
            );

            Context.CurrentMethod.EmitInstruction (foreachStmt.Iterator.Location, Opcode.IterReset);
            Context.CurrentMethod.MarkLabelPosition (foreachLabel);
            Context.CurrentMethod.EmitInstruction (foreachStmt.Iterator.Location, Opcode.LoadLocal, tmp);
            Context.CurrentMethod.EmitInstruction (foreachStmt.Iterator.Location, Opcode.IterMoveNext);
            Context.CurrentMethod.EmitInstruction (foreachStmt.Iterator.Location,
                Opcode.JumpIfFalse,
                breakLabel
            );
            
            Context.CurrentMethod.EmitInstruction (foreachStmt.Iterator.Location, Opcode.LoadLocal, tmp);
            Context.CurrentMethod.EmitInstruction (foreachStmt.Iterator.Location, Opcode.IterGetNext);

            if (foreachStmt.Items.Count == 1) {
                Context.SymbolTable.AddSymbol (foreachStmt.Items [0]);

                /*
                 * STORE_LOCAL      foreachStmt.Items [0]
                 */

                Context.CurrentMethod.EmitInstruction (foreachStmt.Iterator.Location,
                    Opcode.StoreLocal,
                    CreateName (foreachStmt.Items [0])
                );
            } else {
                /*
                 * Requires tuple unpacking...
                 */
                CompileForeachWithAutounpack (foreachStmt.Items);
            }

            foreachStmt.Body.Visit (this);

            /*
             * JUMP             foreachLoop
             * breakFromLoop:
             */

            Context.CurrentMethod.EmitInstruction (foreachStmt.Body.Location, Opcode.Jump, foreachLabel);
            Context.CurrentMethod.MarkLabelPosition (breakLabel);

            Context.SymbolTable.ExitScope ();

            Context.BreakLabels.Pop ();
            Context.ContinueLabels.Pop ();
        }

        void CompileForeachWithAutounpack (List<string> identifiers)
        {
            var local = CreateTemporary ();

            Context.CurrentMethod.EmitInstruction (Opcode.StoreLocal, local);

            for (int i = 0; i < identifiers.Count; i++) {

                string ident = identifiers [i];

                Context.CurrentMethod.EmitInstruction (Opcode.LoadLocal, local);

                Context.CurrentMethod.EmitInstruction (
                    Opcode.LoadConst,
                    new IodineInteger (i)
                );

                Context.CurrentMethod.EmitInstruction (Opcode.LoadIndex);

                if (!symbolTable.IsSymbolDefined (ident)) {
                    symbolTable.AddSymbol (ident);
                }

                Context.CurrentMethod.EmitInstruction (
                    Opcode.StoreLocal,
                    CreateName (ident)
                );
            }
        }

        public override void Accept (RaiseStatement raise)
        {
            raise.Value.Visit (this);
            Context.CurrentMethod.EmitInstruction (raise.Location, Opcode.Raise);
        }

        public override void Accept (ReturnStatement returnStmt)
        {
            returnStmt.VisitChildren (this);
            Context.CurrentMethod.EmitInstruction (returnStmt.Location, Opcode.Return);
        }

        public override void Accept (YieldStatement yieldStmt)
        {
            yieldStmt.VisitChildren (this);
            //Context.CurrentMethod.Generator = true;
            Context.CurrentMethod.EmitInstruction (yieldStmt.Location, Opcode.Yield);
        }

        public override void Accept (BreakStatement brk)
        {
            Context.CurrentMethod.EmitInstruction (brk.Location,
                Opcode.Jump,
                Context.BreakLabels.Peek ()
            );
        }

        public override void Accept (ContinueStatement cont)
        {
            Context.CurrentMethod.EmitInstruction (cont.Location,
                Opcode.Jump,
                Context.ContinueLabels.Peek ()
            );
        }

        public override void Accept (SuperCallStatement super)
        {
            if (super.Parent.BaseClass != null) {
                super.VisitChildren (this);
                super.Parent.BaseClass.Visit (this);
                Context.CurrentMethod.EmitInstruction (Opcode.InvokeSuper, super.Arguments.Arguments.Count);
            }
        }

        public override void Accept (AssignStatement assign)
        {
            if (assign.Packed) {
                CompileAssignWithAutoUnpack (assign);
            } else {
                for (int i = 0; i < assign.Identifiers.Count; i++) {
                    assign.Expressions [i].Visit (this);
                    string ident = assign.Identifiers [i];
                    if (symbolTable.IsGlobal (ident) || assign.Global) {
                        Context.CurrentMethod.EmitInstruction (
                            Opcode.StoreGlobal,
                            CreateName (ident)
                        );
                    } else {
                        if (!symbolTable.IsSymbolDefined (ident)) {
                            symbolTable.AddSymbol (ident);
                        }

                        Context.CurrentMethod.EmitInstruction (
                            Opcode.StoreLocal,
                            CreateName (ident)
                        );
                    }
                }
            }
        }

        void CompileAssignWithAutoUnpack (AssignStatement assignStmt)
        {
            var tmp = CreateTemporary ();

            assignStmt.Expressions [0].Visit (this);

            Context.CurrentMethod.EmitInstruction (Opcode.StoreLocal, tmp);

            for (int i = 0; i < assignStmt.Identifiers.Count; i++) {

                string ident = assignStmt.Identifiers [i];

                Context.CurrentMethod.EmitInstruction (Opcode.LoadLocal, tmp);

                Context.CurrentMethod.EmitInstruction (
                    Opcode.LoadConst,
                    new IodineInteger (i)
                );

                Context.CurrentMethod.EmitInstruction (Opcode.LoadIndex);

                if (symbolTable.IsGlobal (ident) || assignStmt.Global) {
                    Context.CurrentMethod.EmitInstruction (
                        Opcode.StoreGlobal,
                        CreateName (ident)
                    );
                } else {
                    if (!symbolTable.IsSymbolDefined (ident)) {
                        symbolTable.AddSymbol (ident);
                    }
                    Context.CurrentMethod.EmitInstruction (
                        Opcode.StoreLocal,
                        CreateName (ident)
                    );
                }
            }
        }

        public override void Accept (Expression expr)
        {
            expr.VisitChildren (this);
            Context.CurrentMethod.EmitInstruction (expr.Location, Opcode.Pop);
        }

        #endregion

        #region Expressions

        public override void Accept (LambdaExpression lambda)
        {
            CompileMethod (lambda);

            if (!symbolTable.IsInGlobalScope) {
                Context.CurrentMethod.EmitInstruction (Opcode.BuildClosure);
            }
        }

        public override void Accept (BinaryExpression binop)
        {
            if (binop.Operation == BinaryOperation.Assign) {
                binop.Right.Visit (this);
                if (binop.Left is NameExpression) {
                    var ident = (NameExpression)binop.Left;
                    bool isGlobal = Context.SymbolTable.IsInGlobalScope || Context.SymbolTable.IsGlobal (ident.Value);
                    if (!isGlobal) {

                        if (!Context.SymbolTable.IsSymbolDefined (ident.Value)) {
                            Context.SymbolTable.AddSymbol (ident.Value);
                        }
                        var localName = CreateName (ident.Value);
                        Context.CurrentMethod.EmitInstruction (ident.Location, Opcode.StoreLocal, localName);
                        Context.CurrentMethod.EmitInstruction (ident.Location, Opcode.LoadLocal, localName);
                    } else {
                        var globalName = CreateName (ident.Value);
                        Context.CurrentMethod.EmitInstruction (ident.Location, Opcode.StoreGlobal, globalName);
                        Context.CurrentMethod.EmitInstruction (ident.Location, Opcode.LoadGlobal, globalName);
                    }
                } else if (binop.Left is MemberExpression) {
                    var getattr = binop.Left as MemberExpression;
                    getattr.Target.Visit (this);
                    var attrName = new IodineName (getattr.Field);
                    Context.CurrentMethod.EmitInstruction (getattr.Location, Opcode.StoreAttribute, attrName);
                    getattr.Target.Visit (this);
                    Context.CurrentMethod.EmitInstruction (getattr.Location, Opcode.LoadAttribute, attrName);
                } else if (binop.Left is IndexerExpression) {
                    var indexer = binop.Left as IndexerExpression;
                    indexer.Target.Visit (this);
                    indexer.Index.Visit (this);
                    Context.CurrentMethod.EmitInstruction (indexer.Location, Opcode.StoreIndex);
                    binop.Left.Visit (this);
                }
                return;
            }

            switch (binop.Operation) {
            case BinaryOperation.InstanceOf:
                binop.Right.Visit (this);
                binop.Left.Visit (this);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.InstanceOf);
                return;
            case BinaryOperation.NotInstanceOf:
                binop.Right.Visit (this);
                binop.Left.Visit (this);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.InstanceOf);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.UnaryOp, (int)UnaryOperation.BoolNot);
                return;
            case BinaryOperation.DynamicCast:
                binop.Right.Visit (this);
                binop.Left.Visit (this);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.DynamicCast);
                return;
            case BinaryOperation.NullCoalescing:
                binop.Right.Visit (this);
                binop.Left.Visit (this);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.DynamicCast);
                return;
            case BinaryOperation.Add:
                binop.Right.Visit (this);
                binop.Left.Visit (this);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.Add);
                return;
            case BinaryOperation.Sub:
                binop.Right.Visit (this);
                binop.Left.Visit (this);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.Sub);
                return;
            case BinaryOperation.Mul:
                binop.Right.Visit (this);
                binop.Left.Visit (this);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.Mul);
                return;
            case BinaryOperation.Div:
                binop.Right.Visit (this);
                binop.Left.Visit (this);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.Div);
                return;
            case BinaryOperation.Mod:
                binop.Right.Visit (this);
                binop.Left.Visit (this);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.Mod);
                return;
            case BinaryOperation.And:
                binop.Right.Visit (this);
                binop.Left.Visit (this);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.And);
                return;
            case BinaryOperation.Or:
                binop.Right.Visit (this);
                binop.Left.Visit (this);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.Or);
                return;
            case BinaryOperation.Xor:
                binop.Right.Visit (this);
                binop.Left.Visit (this);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.Xor);
                return;
            case BinaryOperation.GreaterThan:
                binop.Right.Visit (this);
                binop.Left.Visit (this);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.GreaterThan);
                return;
            case BinaryOperation.GreaterThanOrEqu:
                binop.Right.Visit (this);
                binop.Left.Visit (this);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.GreaterThanOrEqu);
                return;
            case BinaryOperation.LessThan:
                binop.Right.Visit (this);
                binop.Left.Visit (this);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.LessThan);
                return;
            case BinaryOperation.LessThanOrEqu:
                binop.Right.Visit (this);
                binop.Left.Visit (this);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.LessThanOrEqu);
                return;
            case BinaryOperation.ClosedRange:
                binop.Right.Visit (this);
                binop.Left.Visit (this);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.ClosedRange);
                return;
            case BinaryOperation.HalfRange:
                binop.Right.Visit (this);
                binop.Left.Visit (this);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.HalfRange);
                return;
            case BinaryOperation.LeftShift:
                binop.Right.Visit (this);
                binop.Left.Visit (this);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.LeftShift);
                return;
            case BinaryOperation.RightShift:
                binop.Right.Visit (this);
                binop.Left.Visit (this);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.RightShift);
                return;
            case BinaryOperation.Equals:
                binop.Right.Visit (this);
                binop.Left.Visit (this);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.Equals);
                return;
            case BinaryOperation.NotEquals:
                binop.Right.Visit (this);
                binop.Left.Visit (this);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.NotEquals);
                return;
            }

            var shortCircuitTrueLabel = Context.CurrentMethod.CreateLabel ();
            var shortCircuitFalseLabel = Context.CurrentMethod.CreateLabel ();
            var endLabel = Context.CurrentMethod.CreateLabel ();

            binop.Left.Visit (this);

            /*
             * Short circuit evaluation 
             */
            switch (binop.Operation) {
            case BinaryOperation.BoolAnd:
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.Dup);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.JumpIfFalse,
                    shortCircuitFalseLabel);
                binop.Right.Visit (this);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.BoolAnd);
                break;
            case BinaryOperation.BoolOr:
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.Dup);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.JumpIfTrue,
                    shortCircuitTrueLabel);
                binop.Right.Visit (this);
                Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.BoolOr);
                break;
            }

            Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.Jump, endLabel);
            Context.CurrentMethod.MarkLabelPosition (shortCircuitTrueLabel);
            Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.Pop);
            Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.LoadTrue);
            Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.Jump, endLabel);
            Context.CurrentMethod.MarkLabelPosition (shortCircuitFalseLabel);
            Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.Pop);
            Context.CurrentMethod.EmitInstruction (binop.Location, Opcode.LoadFalse);
            Context.CurrentMethod.MarkLabelPosition (endLabel);
        }

        public override void Accept (UnaryExpression unaryop)
        {
            unaryop.VisitChildren (this);
            Context.CurrentMethod.EmitInstruction (unaryop.Location, Opcode.UnaryOp, (int)unaryop.Operation);
        }

        public override void Accept (CallExpression call)
        {
            call.Arguments.Visit (this);
            call.Target.Visit (this);

            if (call.Arguments.Packed) {
                Context.CurrentMethod.EmitInstruction (
                    call.Target.Location,
                    Opcode.InvokeVar,
                    call.Arguments.Arguments.Count - 1
                );
            } else {
                Context.CurrentMethod.EmitInstruction (
                    call.Target.Location,
                    Opcode.Invoke,
                    call.Arguments.Arguments.Count
                );
            }
        }

        public override void Accept (ArgumentList arglist)
        {
            arglist.VisitChildren (this);
        }

        public override void Accept (KeywordArgumentList kwargs)
        {
            foreach (KeyValuePair<string, AstNode> kv in kwargs.Keywords) {
                string kw = kv.Key;
                AstNode val = kv.Value;
                Context.CurrentMethod.EmitInstruction (
                    kwargs.Location,
                    Opcode.LoadConst,
                    new IodineString (kw)
                );

                val.Visit (this);

                Context.CurrentMethod.EmitInstruction (kwargs.Location, Opcode.BuildTuple, 2);
            }

            Context.CurrentMethod.EmitInstruction (kwargs.Location, Opcode.BuildList, kwargs.Keywords.Count);
            Context.CurrentMethod.EmitInstruction (kwargs.Location,
                Opcode.LoadGlobal,
                CreateName ("Dict")
            );
            Context.CurrentMethod.EmitInstruction (kwargs.Location, Opcode.Invoke, 1);
        }

        public override void Accept (MemberExpression getAttr)
        {
            if (Context.IsPatternExpression) {
                CreateContext ();

                getAttr.Target.Visit (this);

                DestroyContext ();

                Context.CurrentMethod.EmitInstruction (getAttr.Location,
                                                       Opcode.LoadAttribute,
                                                       new IodineName (getAttr.Field));

                Context.CurrentMethod.EmitInstruction (getAttr.Location,
                    Opcode.LoadLocal,
                    Context.PatternTemporary
                );
                Context.CurrentMethod.EmitInstruction (getAttr.Location, Opcode.Equals);
            } else {
                getAttr.Target.Visit (this);

                Context.CurrentMethod.EmitInstruction (
                    getAttr.Location,
                    Opcode.LoadAttribute,
                    new IodineName (getAttr.Field)
                );
            }
        }

        public override void Accept (MemberDefaultExpression getAttr)
        {
            getAttr.Target.Visit (this);
            Context.CurrentMethod.EmitInstruction (getAttr.Location,
                Opcode.LoadAttributeOrNull,
                CreateName (getAttr.Field)
            );

            if (Context.IsPatternExpression) {
                Context.CurrentMethod.EmitInstruction (
                    getAttr.Location,
                    Opcode.LoadLocal,
                    Context.PatternTemporary
                );

                Context.CurrentMethod.EmitInstruction (getAttr.Location, Opcode.Equals);
            }
        }

        public override void Accept (IndexerExpression indexer)
        {
            indexer.Target.Visit (this);
            indexer.Index.Visit (this);
            Context.CurrentMethod.EmitInstruction (indexer.Location, Opcode.LoadIndex);
        }

        public override void Accept (SliceExpression slice)
        {
            slice.VisitChildren (this);

            Context.CurrentMethod.EmitInstruction (slice.Location, Opcode.Slice);
        }

        public override void Accept (TernaryExpression ifExpr)
        {
            var elseLabel = Context.CurrentMethod.CreateLabel ();
            var endLabel = Context.CurrentMethod.CreateLabel ();
            ifExpr.Condition.Visit (this);
            Context.CurrentMethod.EmitInstruction (ifExpr.Expression.Location, Opcode.JumpIfFalse, elseLabel);
            ifExpr.Expression.Visit (this);
            Context.CurrentMethod.EmitInstruction (ifExpr.ElseExpression != null
                ? ifExpr.ElseExpression.Location
                : ifExpr.Location,
                Opcode.Jump,
                endLabel
            );
            Context.CurrentMethod.MarkLabelPosition (elseLabel);
            if (ifExpr.ElseExpression != null) {
                ifExpr.ElseExpression.Visit (this);
            }
            Context.CurrentMethod.MarkLabelPosition (endLabel);
        }

        public override void Accept (GeneratorExpression genExpr)
        {
            var anonMethod = new CodeBuilder ();

            CreateContext (anonMethod);

            Context.SymbolTable.EnterScope ();

            var foreachLabel = Context.CurrentMethod.CreateLabel ();
            var breakLabel = Context.CurrentMethod.CreateLabel ();
            var predicateSkip = Context.CurrentMethod.CreateLabel ();

            var tmp = CreateTemporary ();

            genExpr.Iterator.Visit (this);

            Context.CurrentMethod.EmitInstruction (genExpr.Iterator.Location, Opcode.GetIter);
            Context.CurrentMethod.EmitInstruction (genExpr.Iterator.Location, Opcode.Dup);
            Context.CurrentMethod.EmitInstruction (genExpr.Iterator.Location, Opcode.StoreLocal, tmp);
            Context.CurrentMethod.EmitInstruction (genExpr.Iterator.Location, Opcode.IterReset);
            Context.CurrentMethod.MarkLabelPosition (foreachLabel);
            Context.CurrentMethod.EmitInstruction (genExpr.Iterator.Location, Opcode.LoadLocal, tmp);
            Context.CurrentMethod.EmitInstruction (genExpr.Iterator.Location, Opcode.IterMoveNext);

            Context.CurrentMethod.EmitInstruction (genExpr.Iterator.Location,
                Opcode.JumpIfFalse,
                breakLabel
            );

            Context.CurrentMethod.EmitInstruction (genExpr.Iterator.Location, Opcode.LoadLocal, tmp);
            Context.CurrentMethod.EmitInstruction (genExpr.Iterator.Location, Opcode.IterGetNext);
            Context.SymbolTable.AddSymbol (genExpr.Identifier);
            Context.CurrentMethod.EmitInstruction (genExpr.Iterator.Location,
                Opcode.StoreLocal,
                CreateName (genExpr.Identifier)
            );

            if (genExpr.Predicate != null) {
                genExpr.Predicate.Visit (this);
                Context.CurrentMethod.EmitInstruction (genExpr.Iterator.Location, Opcode.JumpIfFalse, predicateSkip);
            }

            genExpr.Expression.Visit (this);

            Context.CurrentMethod.EmitInstruction (genExpr.Expression.Location, Opcode.Yield);

            if (genExpr.Predicate != null) {
                Context.CurrentMethod.MarkLabelPosition (predicateSkip);
            }

            Context.CurrentMethod.EmitInstruction (genExpr.Expression.Location, Opcode.Jump, foreachLabel);
            Context.CurrentMethod.MarkLabelPosition (breakLabel);
            Context.CurrentMethod.EmitInstruction (genExpr.Location, Opcode.LoadNull);


            Context.SymbolTable.ExitScope ();

            anonMethod.FinalizeMethod ();

            DestroyContext ();

            Context.CurrentMethod.EmitInstruction (genExpr.Location,
                                                   Opcode.LoadConst,
                                                   anonMethod);

            Context.CurrentMethod.EmitInstruction (genExpr.Location, Opcode.BuildGenExpr);
        }

        public override void Accept (ListCompExpression list)
        {
            var foreachLabel = Context.CurrentMethod.CreateLabel ();
            var breakLabel = Context.CurrentMethod.CreateLabel ();
            var predicateSkip = Context.CurrentMethod.CreateLabel ();

            var tmp = CreateTemporary ();
            var set = CreateTemporary ();

            Context.CurrentMethod.EmitInstruction (list.Iterator.Location, Opcode.BuildList, 0);

            Context.CurrentMethod.EmitInstruction (list.Iterator.Location, Opcode.StoreLocal, set);

            Context.SymbolTable.EnterScope ();

            list.Iterator.Visit (this);

            Context.CurrentMethod.EmitInstruction (list.Iterator.Location, Opcode.GetIter);
            Context.CurrentMethod.EmitInstruction (list.Iterator.Location, Opcode.Dup);
            Context.CurrentMethod.EmitInstruction (list.Iterator.Location, Opcode.StoreLocal, tmp);
            Context.CurrentMethod.EmitInstruction (list.Iterator.Location, Opcode.IterReset);
            Context.CurrentMethod.MarkLabelPosition (foreachLabel);
            Context.CurrentMethod.EmitInstruction (list.Iterator.Location, Opcode.LoadLocal, tmp);
            Context.CurrentMethod.EmitInstruction (list.Iterator.Location, Opcode.IterMoveNext);
            Context.CurrentMethod.EmitInstruction (
                list.Iterator.Location,
                Opcode.JumpIfFalse,
                breakLabel
            );

            Context.CurrentMethod.EmitInstruction (list.Iterator.Location, Opcode.LoadLocal, tmp);
            Context.CurrentMethod.EmitInstruction (list.Iterator.Location, Opcode.IterGetNext);

            Context.SymbolTable.AddSymbol (list.Identifier);

            Context.CurrentMethod.EmitInstruction (
                list.Iterator.Location,
                Opcode.StoreLocal,
                CreateName (list.Identifier)
            );

            if (list.Predicate != null) {
                list.Predicate.Visit (this);
                Context.CurrentMethod.EmitInstruction (list.Iterator.Location, Opcode.JumpIfFalse, predicateSkip);
            }

            list.Expression.Visit (this);

            Context.CurrentMethod.EmitInstruction (list.Iterator.Location, Opcode.LoadLocal, set);
            Context.CurrentMethod.EmitInstruction (list.Iterator.Location,
                                                   Opcode.LoadAttribute,
                                                   new IodineName ("append"));

            Context.CurrentMethod.EmitInstruction (list.Iterator.Location, Opcode.Invoke, 1);
            Context.CurrentMethod.EmitInstruction (list.Iterator.Location, Opcode.Pop);

            if (list.Predicate != null) {
                Context.CurrentMethod.MarkLabelPosition (predicateSkip);
            }
            Context.CurrentMethod.EmitInstruction (list.Expression.Location, Opcode.Jump, foreachLabel);
            Context.CurrentMethod.MarkLabelPosition (breakLabel);
            Context.CurrentMethod.EmitInstruction (list.Iterator.Location, Opcode.LoadLocal, set);

            Context.SymbolTable.ExitScope ();
        }

        public override void Accept (ListExpression list)
        {
            list.VisitChildren (this);
            Context.CurrentMethod.EmitInstruction (list.Location, Opcode.BuildList, list.Items.Count);
        }

        public override void Accept (TupleExpression tuple)
        {
            CreateContext (Context.IsInClassBody);

            tuple.VisitChildren (this);

            DestroyContext ();


            if (Context.IsPatternExpression) {
                Context.CurrentMethod.EmitInstruction (Opcode.LoadLocal, Context.PatternTemporary);
                Context.CurrentMethod.EmitInstruction (tuple.Location, Opcode.MatchPattern, tuple.Items.Count);

            } else {
                Context.CurrentMethod.EmitInstruction (tuple.Location, Opcode.BuildTuple, tuple.Items.Count);
            }
        }

        public override void Accept (HashExpression hash)
        {
            hash.VisitChildren (this);
            Context.CurrentMethod.EmitInstruction (hash.Location, Opcode.BuildHash, hash.Items.Count / 2);
        }


        #endregion

        #region PatternExpression

        public override void Accept (MatchExpression match)
        {
            var value = match.Expression;

            value.Visit (this);

            var temporary = CreateTemporary ();


            Context.CurrentMethod.EmitInstruction (match.Location, Opcode.StoreLocal, temporary);

            var nextLabel = Context.CurrentMethod.CreateLabel ();
            var endLabel = Context.CurrentMethod.CreateLabel ();

            for (int i = 0; i < match.MatchCases.Count; i++) {
                if (i > 0) {
                    Context.CurrentMethod.MarkLabelPosition (nextLabel);
                    nextLabel = Context.CurrentMethod.CreateLabel ();
                }

                var clause = match.MatchCases [i] as CaseExpression;

                CreatePatternContext (temporary);

                Context.SymbolTable.EnterScope ();

                clause.Pattern.Visit (this);

                DestroyContext ();

                Context.CurrentMethod.EmitInstruction (
                    match.Location,
                    Opcode.JumpIfFalse,
                    nextLabel
                );

                if (clause.Condition != null) {
                    clause.Condition.Visit (this);
                    Context.CurrentMethod.EmitInstruction (
                        match.Location,
                        Opcode.JumpIfFalse,
                        nextLabel
                    );
                }

                clause.Value.Visit (this);

                if (clause.IsStatement) {
                    Context.CurrentMethod.EmitInstruction (
                        match.Location,
                        Opcode.LoadNull
                    );
                }

                Context.SymbolTable.ExitScope ();

                Context.CurrentMethod.EmitInstruction (
                    match.Location,
                    Opcode.Jump,
                    endLabel
                );
            }
            Context.CurrentMethod.MarkLabelPosition (endLabel);
        }

        public override void Accept (PatternExpression expression)
        {
            var shortCircuitTrueLabel = Context.CurrentMethod.CreateLabel ();
            var shortCircuitFalseLabel = Context.CurrentMethod.CreateLabel ();

            var endLabel = Context.CurrentMethod.CreateLabel ();


            if (expression.Operation == BinaryOperation.HalfRange) {

                Context.CurrentMethod.EmitInstruction (
                    Opcode.LoadLocal,
                    Context.PatternTemporary
                );

                CreateContext (Context.CurrentMethod);

                expression.Right.Visit (this);
                expression.Left.Visit (this);

                Context.CurrentMethod.EmitInstruction (
                    expression.Location,
                    Opcode.HalfRange
                );

                Context.CurrentMethod.EmitInstruction (
                    expression.Location,
                    Opcode.RangeCheck
                );

                DestroyContext ();

                return;
            }


            if (expression.Operation == BinaryOperation.ClosedRange) {

                Context.CurrentMethod.EmitInstruction (
                    Opcode.LoadLocal,
                    Context.PatternTemporary
                );

                CreateContext (Context.CurrentMethod);

                expression.Right.Visit (this);
                expression.Left.Visit (this);

                Context.CurrentMethod.EmitInstruction (
                    expression.Location,
                    Opcode.ClosedRange
                );

                Context.CurrentMethod.EmitInstruction (
                    expression.Location,
                    Opcode.RangeCheck
                );

                DestroyContext ();

                return;
            }

            expression.Left.Visit (this);

            /*
             * Short circuit evaluation 
             */
            switch (expression.Operation) {
            case BinaryOperation.And:
                Context.CurrentMethod.EmitInstruction (
                    expression.Location,
                    Opcode.Dup
                );

                Context.CurrentMethod.EmitInstruction (expression.Location,
                    Opcode.JumpIfFalse,
                    shortCircuitFalseLabel
                );
                
                expression.Right.Visit (this);

                Context.CurrentMethod.EmitInstruction (
                    expression.Location,
                    Opcode.BoolAnd
                );
                break;
            case BinaryOperation.Or:

                Context.CurrentMethod.EmitInstruction (
                    expression.Location,
                    Opcode.Dup
                );

                Context.CurrentMethod.EmitInstruction (
                    expression.Location,
                    Opcode.JumpIfTrue,
                    shortCircuitTrueLabel
                );
                expression.Right.Visit (this);

                Context.CurrentMethod.EmitInstruction (
                    expression.Location,
                    Opcode.BoolOr
                );
                break;
            default:
                System.Console.WriteLine ("Missing operator! Yikes");
                break;
            }


            Context.CurrentMethod.EmitInstruction (
                expression.Location,
                Opcode.Jump,
                endLabel
            );

            Context.CurrentMethod.MarkLabelPosition (shortCircuitTrueLabel);

            Context.CurrentMethod.EmitInstruction (
                expression.Location,
                Opcode.Pop
            );

            Context.CurrentMethod.EmitInstruction (
                expression.Location,
                Opcode.LoadTrue
            );

            Context.CurrentMethod.EmitInstruction (
                expression.Location,
                Opcode.Jump,
                endLabel
            );

            Context.CurrentMethod.MarkLabelPosition (shortCircuitFalseLabel);

            Context.CurrentMethod.EmitInstruction (
                expression.Location,
                Opcode.Pop
            );

            Context.CurrentMethod.EmitInstruction (
                expression.Location,
                Opcode.LoadFalse
            );

            Context.CurrentMethod.MarkLabelPosition (endLabel);
        }


        public override void Accept (PatternExtractExpression extractExpression)
        {
            var notInstance = Context.CurrentMethod.CreateLabel ();
            var isInstance = Context.CurrentMethod.CreateLabel ();

            CreateContext (Context.IsInClassBody);

            extractExpression.Target.Visit (this);

            DestroyContext ();

            Context.CurrentMethod.EmitInstruction (extractExpression.Target.Location,
                Opcode.LoadLocal,
                Context.PatternTemporary
            );

            Context.CurrentMethod.EmitInstruction (
                Opcode.InstanceOf
            );

            Context.CurrentMethod.EmitInstruction (Opcode.JumpIfFalse, notInstance);

            Context.CurrentMethod.EmitInstruction (extractExpression.Target.Location,
                Opcode.LoadLocal,
                Context.PatternTemporary
            );

            Context.CurrentMethod.EmitInstruction (
                Opcode.Unwrap,
                extractExpression.Captures.Count
            );

            Context.CurrentMethod.EmitInstruction (Opcode.JumpIfFalse, notInstance);

            if (extractExpression.Captures.Count > 1) {
                Context.CurrentMethod.EmitInstruction (
                    Opcode.Unpack,
                    extractExpression.Captures.Count
                );
            }

            foreach (string capture in extractExpression.Captures) {
                Context.CurrentMethod.EmitInstruction (
                    Opcode.StoreLocal,
                    new IodineName (capture)
                );

                symbolTable.AddSymbol (capture);
            }

            Context.CurrentMethod.EmitInstruction (Opcode.LoadTrue);
            Context.CurrentMethod.EmitInstruction (Opcode.Jump, isInstance);
            Context.CurrentMethod.MarkLabelPosition (notInstance);
            Context.CurrentMethod.EmitInstruction (Opcode.LoadFalse);
            Context.CurrentMethod.MarkLabelPosition (isInstance);
        }
        #endregion

        #region Terminals

        public override void Accept (NameExpression ident)
        {
            if (Context.IsPatternExpression) {
                if (ident.Value == "_") {
                    Context.CurrentMethod.EmitInstruction (ident.Location, Opcode.LoadTrue);
                } else {

                    Context.CurrentMethod.EmitInstruction (
                        ident.Location,
                        Opcode.LoadGlobal,
                        CreateName (ident.Value)
                    );
                    Context.CurrentMethod.EmitInstruction (ident.Location,
                        Opcode.LoadLocal,
                        Context.PatternTemporary
                    );

                    Context.CurrentMethod.EmitInstruction (
                        ident.Location,
                        Opcode.InstanceOf
                    );
                }
                return;

            }

            if (Context.SymbolTable.IsSymbolDefined (ident.Value)) {
                if (!Context.SymbolTable.IsGlobal (ident.Value)) {
                    Context.CurrentMethod.EmitInstruction (
                        ident.Location,
                        Opcode.LoadLocal,
                        CreateName (ident.Value)
                    );
                } else {
                    Context.CurrentMethod.EmitInstruction (
                        ident.Location,
                        Opcode.LoadGlobal,
                        CreateName (ident.Value)
                    );
                }
            } else if (Context.IsInClass && ExistsInOuterClass (ident.Value)) {
                LoadAssociatedClass (ident.Value);
                Context.CurrentMethod.EmitInstruction (
                    ident.Location,
                    Opcode.LoadAttribute,
                    CreateName (ident.Value)
                );
            } else {
                Context.CurrentMethod.EmitInstruction (
                    ident.Location,
                    Opcode.LoadGlobal,
                    CreateName (ident.Value)
                );
            }

        }

        bool ExistsInOuterClass (string name)
        {

            return false;
        }

        /*
         * Emits the instructions required for loading the class that contains
         * the attribute 'item'
         */
        void LoadAssociatedClass (string item = null)
        {
        }

        public override void Accept (IntegerExpression integer)
        {
            Context.CurrentMethod.EmitInstruction (integer.Location,
                                                   Opcode.LoadConst,
                                                   new IodineInteger (integer.Value));

            if (Context.IsPatternExpression) {
                Context.CurrentMethod.EmitInstruction (
                    integer.Location,
                    Opcode.LoadLocal,
                    Context.PatternTemporary
                );
                Context.CurrentMethod.EmitInstruction (
                    integer.Location,
                    Opcode.Equals
                );
            }
        }

        public override void Accept (BigIntegerExpression integer)
        {
            Context.CurrentMethod.EmitInstruction (integer.Location,
                                                   Opcode.LoadConst,
                                                   new IodineBigInt (integer.Value));

            if (Context.IsPatternExpression) {
                Context.CurrentMethod.EmitInstruction (
                    integer.Location,
                    Opcode.LoadLocal,
                    Context.PatternTemporary
                );
                Context.CurrentMethod.EmitInstruction (
                    integer.Location,
                    Opcode.Equals
                );
            }
        }

        public override void Accept (FloatExpression dec)
        {
            Context.CurrentMethod.EmitInstruction (dec.Location,
                                                   Opcode.LoadConst,
                                                   new IodineFloat (dec.Value));

            if (Context.IsPatternExpression) {
                Context.CurrentMethod.EmitInstruction (dec.Location,
                    Opcode.LoadLocal,
                    Context.PatternTemporary
                );
                Context.CurrentMethod.EmitInstruction (dec.Location, Opcode.Equals);
            }
        }


        public override void Accept (RegexExpression regex)
        {
            Context.CurrentMethod.EmitInstruction (regex.Location,
                                                   Opcode.LoadConst,
                                                   new IodineString (regex.Value));

            Context.CurrentMethod.EmitInstruction (regex.Location, Opcode.BuildRegex);
        }


        public override void Accept (StringExpression stringConst)
        {
            stringConst.VisitChildren (this); // A string can contain a list of sub expressions for string interpolation

            IodineObject constant = stringConst.Binary ?
                (IodineObject)new IodineBytes (stringConst.Value) :
                (IodineObject)new IodineString (stringConst.Value);

            Context.CurrentMethod.EmitInstruction (stringConst.Location,
                                                   Opcode.LoadConst,
                                                   constant);

            if (stringConst.SubExpressions.Count != 0) {
                Context.CurrentMethod.EmitInstruction (stringConst.Location,
                                                       Opcode.LoadAttribute,
                                                       new IodineName ("format"));

                Context.CurrentMethod.EmitInstruction (stringConst.Location, Opcode.Invoke, stringConst.SubExpressions.Count);
            }

            if (Context.IsPatternExpression) {
                Context.CurrentMethod.EmitInstruction (stringConst.Location,
                    Opcode.LoadLocal,
                    Context.PatternTemporary
                );
                Context.CurrentMethod.EmitInstruction (stringConst.Location, Opcode.Equals);
            }
        }

        public override void Accept (SelfExpression self)
        {
            Context.CurrentMethod.EmitInstruction (self.Location, Opcode.LoadSelf);
        }

        public override void Accept (NullExpression nil)
        {
            Context.CurrentMethod.EmitInstruction (nil.Location, Opcode.LoadNull);

            if (Context.IsPatternExpression) {
                Context.CurrentMethod.EmitInstruction (nil.Location,
                    Opcode.LoadLocal,
                    Context.PatternTemporary
                );
                Context.CurrentMethod.EmitInstruction (nil.Location, Opcode.Equals);
            }
        }

        public override void Accept (TrueExpression ntrue)
        {
            Context.CurrentMethod.EmitInstruction (ntrue.Location, Opcode.LoadTrue);

            if (Context.IsPatternExpression) {
                Context.CurrentMethod.EmitInstruction (ntrue.Location,
                    Opcode.LoadLocal,
                    Context.PatternTemporary
                );
                Context.CurrentMethod.EmitInstruction (ntrue.Location, Opcode.Equals);

            }
        }

        public override void Accept (FalseExpression nfalse)
        {
            Context.CurrentMethod.EmitInstruction (nfalse.Location, Opcode.LoadFalse);

            if (Context.IsPatternExpression) {
                Context.CurrentMethod.EmitInstruction (nfalse.Location,
                    Opcode.LoadLocal,
                    Context.PatternTemporary
                );
                Context.CurrentMethod.EmitInstruction (nfalse.Location, Opcode.Equals);
            }
        }
        #endregion
    }
}


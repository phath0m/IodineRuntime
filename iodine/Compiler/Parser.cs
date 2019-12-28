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
using System.Text;
using System.Collections.Generic;
using Iodine.Compiler.Ast;
using System.Numerics;

namespace Iodine.Compiler
{
    public sealed class Parser
    {
        ErrorSink errorLog;
        IodineContext context;

        readonly List<Token> tokens = new List<Token> ();

        int position = 0;

        Token Current {
            get {
                return PeekToken ();
            }
        }

        bool EndOfStream {
            get {
                return tokens.Count <= position;
            }
        }

        SourceLocation Location {
            get {
                if (PeekToken () != null) {
                    return PeekToken ().Location;
                }

                if (tokens.Count == 0) {
                    return new SourceLocation (0, 0, "");
                }

                return PeekToken (-1).Location;

            }
        }
            
        Parser (IodineContext context, IEnumerable<Token> tokens)
        {
            errorLog = context.ErrorLog;

            this.context = context;
            this.tokens.AddRange (tokens);
        }

        public static Parser CreateParser (IodineContext context, SourceUnit source)
        {
            var tokenizer = new Tokenizer (
                context.ErrorLog,
                source.GetReader ()
            );
            return new Parser (context, tokenizer.Scan ());
        }

        public CompilationUnit Parse ()
        {
            try {
                var root = new CompilationUnit (Location);

                while (!EndOfStream) {
                    root.Add (ParseStatement ());
                }

                return root;
            } catch (EndOfFileException) {
                throw new SyntaxException (errorLog);
            } finally {
                if (errorLog.ErrorCount > 0) {
                    throw new SyntaxException (errorLog);
                }
            }
        }

        #region Declarations

        /*
         * class <name> [extends <baseclass> [implements <interfaces>, ...]] {
         * 
         * }
         * 
         * OR
         * 
         * class <name> (parameters, ...) [extends <baseclass> [implements <interfaces>, ...]]
         */
        AstNode ParseClass ()
        {
            string doc = Expect (TokenClass.Keyword, "class").Documentation;

            string name = Expect (TokenClass.Identifier).Value;

            var clazz = new ClassDeclaration (Location, name, doc);

            if (Match (TokenClass.OpenParan)) {
                bool isInstanceMethod;
                bool isVariadic;
                bool hasKeywordArgs;
                bool hasDefaultVals;

                var parameters = ParseFuncParameters (
                    out isInstanceMethod,
                    out isVariadic,
                    out hasKeywordArgs,
                    out hasDefaultVals
                );

                if (isInstanceMethod) {
                    errorLog.Add (Errors.RecordCantHaveSelf, Location);
                }

                if (isVariadic) {
                    errorLog.Add (Errors.RecordCantHaveVargs, Location);
                }

                if (hasKeywordArgs) {
                    errorLog.Add (Errors.RecordCantHaveKwargs, Location);
                }

                clazz = new ClassDeclaration (Location, name, doc, parameters);
            }

            if (Accept (TokenClass.Keyword, "extends")) {
                clazz.BaseClass = ParseExpression ();
            }

            if (Accept (TokenClass.Keyword, "implements")) {
                do {
                    clazz.Interfaces.Add (ParseExpression ());
                } while (Accept (TokenClass.Comma));
            }

            if (Accept (TokenClass.Keyword, "use")) {
                do {
                    clazz.Mixins.Add (ParseExpression ());
                } while (Accept (TokenClass.Comma));
            }

            if (Accept (TokenClass.OpenBrace)) {
                while (!Match (TokenClass.CloseBrace)) {
                    if (Match (TokenClass.Keyword, "func") || Match (TokenClass.Operator,
                        "+")) {
                        var node = ParseFunction (false, clazz);
                        if (node is FunctionDeclaration) {
                            var func = node as FunctionDeclaration;
                            if (func == null) {
                                clazz.Add (node);
                            } else if (func.Name == name) {
                                clazz.Constructor = func;
                            } else {
                                clazz.Add (func);
                            }
                        } else {
                            var list = node as StatementList;
                            clazz.Add (list.Statements [0]);
                            clazz.Add (list.Statements [1]);
                        }
                    } else {
                        clazz.Add (ParseStatement ());
                    }
                }

                Expect (TokenClass.CloseBrace);

            }
            return clazz;
        }

        /*
         * enum <name> {
         *  <item> [= <constant>],
         *  ...
         * }
         * 
         */
        AstNode ParseEnum ()
        {
            string doc = Expect (TokenClass.Keyword, "enum").Documentation;
            string name = Expect (TokenClass.Identifier).Value;
            var decl = new EnumDeclaration (Location, name, doc);

            Expect (TokenClass.OpenBrace);

            int defaultVal = -1;

            while (!Match (TokenClass.CloseBrace)) {
                string ident = Expect (TokenClass.Identifier).Value;
                if (Accept (TokenClass.Operator, "=")) {
                    string val = Expect (TokenClass.IntLiteral).Value;
                    int numVal = 0;
                    if (val != "") {
                        numVal = Int32.Parse (val);
                    }
                    decl.Items [ident] = numVal;
                } else {
                    decl.Items [ident] = defaultVal--;
                }

                if (!Accept (TokenClass.Comma)) {
                    break;
                }
            }

            Expect (TokenClass.CloseBrace);

            return decl;
        }

        /*
         * interface <name> {
         *     ...
         * }
         */
        AstNode ParseContract ()
        {
            string doc = Expect (TokenClass.Keyword, "contract").Value;
            string name = Expect (TokenClass.Identifier).Value;

            var contract = new ContractDeclaration (Location, name, doc);

            Expect (TokenClass.OpenBrace);

            while (!Match (TokenClass.CloseBrace)) {
                if (Match (TokenClass.Keyword, "func")) {
                    var func = ParseFunction (true) as FunctionDeclaration;
                    contract.AddMember (func);
                } else {
                    errorLog.Add (Errors.IllegalInterfaceDeclaration, Location);
                }

                while (Accept (TokenClass.SemiColon)) {

                }
            }

            Expect (TokenClass.CloseBrace);

            return contract;
        }

        /*
         * trait <name> {
         *     ...
         * }
         */
        AstNode ParseTrait ()
        {
            string doc = Expect (TokenClass.Keyword, "trait").Documentation;
            string name = Expect (TokenClass.Identifier).Value;

            var trait = new TraitDeclaration (Location, name, doc);

            Expect (TokenClass.OpenBrace);

            while (!Match (TokenClass.CloseBrace)) {
                if (Match (TokenClass.Keyword, "func")) {
                    var func = ParseFunction (true) as FunctionDeclaration;
                    trait.AddMember (func);
                } else {
                    errorLog.Add (Errors.IllegalInterfaceDeclaration, Location);
                }
                while (Accept (TokenClass.SemiColon)) {

                }
            }

            Expect (TokenClass.CloseBrace);

            return trait;
        }
            
        /*
         * mixin <name> {
         *     ...
         * }
         */
        AstNode ParseMixin ()
        {
            string doc = Expect (TokenClass.Keyword, "mixin").Documentation;
            string name = Expect (TokenClass.Identifier).Value;

            var mixin = new MixinDeclaration (Location, name, doc);

            Expect (TokenClass.OpenBrace);

            while (!Match (TokenClass.CloseBrace)) {
                if (Match (TokenClass.Keyword, "func")) {
                    var func = ParseFunction () as FunctionDeclaration;
                    mixin.AddMember (func);
                } else {
                    errorLog.Add (Errors.IllegalInterfaceDeclaration, Location);
                }

                while (Accept (TokenClass.SemiColon)) {
                }
            }

            Expect (TokenClass.CloseBrace);

            return mixin;
        }

        string ParseClassName ()
        {
            var ret = new StringBuilder ();
            do {
                string attr = Expect (TokenClass.Identifier).Value;
                ret.Append (attr);
                if (Match (TokenClass.MemberAccess)) {
                    ret.Append ('.');
                }
            } while (Accept (TokenClass.MemberAccess));
            return ret.ToString ();
        }


        NamedParameter ParseParameterName ()
        {
            var param = Expect (TokenClass.Identifier);

            AstNode type = null;
            AstNode value = null;

            if (Accept (TokenClass.Colon)) {
                type = ParseExpression ();
            }

            if (Accept (TokenClass.Operator, "=")) {
                value = ParseExpression ();
            }

            return new NamedParameter (param.Value, type, value);

        }

        DecompositionParameter ParseTupleParameter ()
        {
            Expect (TokenClass.OpenBracket);

            var paramList = new List<FunctionParameter> ();

            if (Match (TokenClass.CloseBracket)) {
                errorLog.Add (Errors.EmptyDecompositionList, PeekToken ().Location);
            }

            while (!Match (TokenClass.CloseBracket)) {

                if (Match (TokenClass.OpenBracket)) {
                    paramList.Add (ParseTupleParameter ());
                } else {
                    paramList.Add (ParseParameterName ());
                }

                if (!Accept (TokenClass.Comma)) {
                    break;
                }
            }

            Expect (TokenClass.CloseBracket);

            return new DecompositionParameter (paramList);
        }

        AstNode ParseFunction (bool prototype = false, ClassDeclaration cdecl = null)
        {
            string doc = Current.Documentation;

            if (Accept (TokenClass.Operator, "+")) {
                var decorator = ParseExpression (); 
                var originalFunc = ParseFunction (prototype, cdecl) as FunctionDeclaration;
                return new DecoratedFunction (decorator.Location, decorator, originalFunc);
            }

            Expect (TokenClass.Keyword, "func");

            bool isInstanceMethod;
            bool isVariadic;
            bool hasKeywordArgs;
            bool hasDefaultVals;

            var ident = Expect (TokenClass.Identifier);

            var parameters = ParseFuncParameters (
              out isInstanceMethod,
              out isVariadic,
              out hasKeywordArgs,
              out hasDefaultVals
            );

            var decl = new FunctionDeclaration (Location, ident != null ?
                ident.Value : "",
                isInstanceMethod,
                isVariadic,
                hasKeywordArgs,
                hasDefaultVals,
                parameters,
                doc
            );

            if (!prototype) {

                if (Accept (TokenClass.Operator, "=>")) {
                    decl.AddStatement (new ReturnStatement (Location, ParseExpression ()));
                } else {
                    Expect (TokenClass.OpenBrace);
                    var scope = new StatementList (Location);

                    if (Match (TokenClass.Keyword, "super")) {
                        scope.AddStatement (ParseSuperCall (cdecl));
                    } else if (cdecl != null && cdecl.Name == decl.Name) {
                        /*
                         * If this is infact a constructor and no super call is provided, we must implicitly call super ()
                         */
                        scope.AddStatement (new SuperCallStatement (decl.Location, cdecl, new ArgumentList (decl.Location)));
                    }

                    while (!Match (TokenClass.CloseBrace)) {
                        scope.AddStatement (ParseStatement ());
                    }

                    decl.AddStatement (scope);
                    Expect (TokenClass.CloseBrace);
                }
            }
            return decl;
        }

        List<FunctionParameter> ParseFuncParameters (
            out bool isInstanceMethod,
            out bool isVariadic,
            out bool hasKeywordArgs,
            out bool hasDefaultValues)
        {
            isVariadic = false;
            hasKeywordArgs = false;
            isInstanceMethod = false;

            hasDefaultValues = false;

            var ret = new List<FunctionParameter> ();

            if (!Accept (TokenClass.OpenParan)) {
                return ret;
            }

            if (Accept (TokenClass.Keyword, "self")) {
                isInstanceMethod = true;
                if (!Accept (TokenClass.Comma)) {
                    Expect (TokenClass.CloseParan);
                    return ret;
                }
            }

            while (!Match (TokenClass.CloseParan)) {

                if (!hasKeywordArgs && Accept (TokenClass.Operator, "**")) {
                    hasKeywordArgs = true;
                    var ident = Expect (TokenClass.Identifier);
                    ret.Add (new NamedParameter (ident.Value));
                    continue;
                }

                if (hasKeywordArgs) {
                    errorLog.Add (Errors.ArgumentAfterKeywordArgs, Location);
                    continue;
                }

                if (!isVariadic && Accept (TokenClass.Operator, "*")) {
                    isVariadic = true;
                    var ident = Expect (TokenClass.Identifier);
                    ret.Add (new NamedParameter (ident.Value));
                    continue;

                }

                if (isVariadic) {
                    errorLog.Add (Errors.ArgumentAfterVariadicArgs, Location);
                    continue;
                }

                if (Match (TokenClass.OpenBracket)) {
                    ret.Add (ParseTupleParameter ());
                } else {
                    ret.Add (ParseParameterName ());
                }
                if (!Accept (TokenClass.Comma)) {
                    break;
                }
            }
            Expect (TokenClass.CloseParan);
            return ret;
        }

        #endregion

        #region Statements

        /*
         * use <module> |
         * use <class> from <module>
         */
        UseStatement ParseUse ()
        {
            Expect (TokenClass.Keyword, "use");

            var isRelative = Accept (TokenClass.MemberAccess);

            var modPath = "";

            if (!Match (TokenClass.Operator, "*")) {
                modPath = ParseModuleName ();
            }

            if (Match (TokenClass.Keyword, "from") ||
                Match (TokenClass.Comma) ||
                Match (TokenClass.Operator, "*")) {

                var items = new List<string> ();

                bool isWildcardImport = false;

                if (!Accept (TokenClass.Operator, "*")) {
                    items.Add (modPath);

                    Accept (TokenClass.Comma);

                    while (!Match (TokenClass.Keyword, "from")) {
                        var item = Expect (TokenClass.Identifier);
                        items.Add (item.Value);
                        if (!Accept (TokenClass.Comma)) {
                            break;
                        }
                    }

                } else {
                    isWildcardImport = true;
                }

                Expect (TokenClass.Keyword, "from");

                isRelative = Accept (TokenClass.MemberAccess);

                var module = ParseModuleName ();

                return new UseStatement (Location, module, items, isWildcardImport, isRelative);
            }
            return new UseStatement (Location, modPath, isRelative);
        }

        string ParseModuleName ()
        {
            var initialModule = Expect (TokenClass.Identifier);

            if (Match (TokenClass.MemberAccess)) {
                
                var accum = new StringBuilder ();

                accum.Append (initialModule.Value);

                while (Accept (TokenClass.MemberAccess)) {
                    var submodule = Expect (TokenClass.Identifier);
                    accum.Append (Path.DirectorySeparatorChar);
                    accum.Append (submodule.Value);
                }

                return accum.ToString ();

            }
            return initialModule.Value;
        }

        AstNode ParseStatement ()
        {
            try {
                return DoParseStatement ();
            } catch  (SyntaxException) {
                Synchronize ();
                return null;
            }
        }

        AstNode DoParseStatement ()
        {
            if (Match (TokenClass.Keyword)) {
                switch (Current.Value) {
                case "class":
                    return ParseClass ();
                case "enum":
                    return ParseEnum ();
                case "contract":
                    return ParseContract ();
                case "trait":
                    return ParseTrait ();
                case "mixin":
                    return ParseMixin ();
                case "extend":
                    return ParseExtend ();
                case "func":
                    return ParseFunction ();
                case "if":
                    return ParseIf ();
                case "unless":
                    return ParseUnless ();
                case "for":
                    return ParseForeach ();
                case "with":
                    return ParseWith ();
                case "while":
                    return ParseWhile ();
                case "until":
                    return ParseUntil ();
                case "do":
                    return ParseDoWhile ();
                case "use":
                    return ParseUse ();
                case "return":
                    return ParseReturn ();
                case "raise":
                    return ParseRaise ();
                case "yield":
                    return ParseYield ();
                case "try":
                    return ParseTryExcept ();
                case "global":
                    return ParseAssignStatement ();
                case "break":
                    Accept (TokenClass.Keyword);
                    return new BreakStatement (Location);
                case "continue":
                    Accept (TokenClass.Keyword);
                    return new ContinueStatement (Location);
                case "super":
                    errorLog.Add (Errors.SuperCalledAfter, Location);
                    return ParseSuperCall (new ClassDeclaration (Location, "", null));
                }
            }

            if (Match (TokenClass.OpenBrace)) {
                return ParseBlock ();
            }

            if (Accept (TokenClass.SemiColon)) {
                return new Statement (Location);
            }

            if (Match (TokenClass.Operator, "+")) {
                return ParseFunction ();
            }

            if (PeekToken (1) != null && PeekToken (1).Class == TokenClass.Comma) {
                return ParseAssignStatement ();
            } 

            var node = ParseExpression ();

            if (node == null) {
                MakeError ();
            }
            return new Expression (Location, node);

        }

        AstNode ParseBlock ()
        {
            var ret = new CodeBlock (Location);

            Expect (TokenClass.OpenBrace);

            while (!Match (TokenClass.CloseBrace)) {
                ret.Add (ParseStatement ());
            }

            Expect (TokenClass.CloseBrace);
            return ret;
        }

        /*
         * extend <class> [use <mixin>, ...] { 
         *      ...
         * }
         */
        AstNode ParseExtend ()
        {
            Expect (TokenClass.Keyword, "extend");
            var clazz = ParseExpression ();

            var statement = new ExtendStatement (clazz.Location, clazz, "");

            if (Accept (TokenClass.Keyword, "use")) {
                do {
                    statement.Mixins.Add (ParseExpression ());
                } while (Accept (TokenClass.Comma));
            }

            if (Accept (TokenClass.OpenBrace)) {
                while (!Match (TokenClass.CloseBrace) & !EndOfStream) {
                    statement.AddMember (ParseFunction ());
                }
                Expect (TokenClass.CloseBrace);
            }

            return statement;
        }

        /*
         * try {
         * 
         * } except [(<identifier> as <type>)] {
         * 
         * }
         */
        AstNode ParseTryExcept ()
        {
            string exceptionVariable = null;
            Expect (TokenClass.Keyword, "try");
            var tryBody = ParseStatement ();
            var typeList = new ArgumentList (Location);
            Expect (TokenClass.Keyword, "except");
            if (Accept (TokenClass.OpenParan)) {
                var ident = Expect (TokenClass.Identifier);

                if (Accept (TokenClass.Operator, "as")) {
                    typeList = ParseTypeList ();
                }
                Expect (TokenClass.CloseParan);
                exceptionVariable = ident.Value;
            }
            var exceptBody = ParseStatement ();
            return new TryExceptStatement (Location, exceptionVariable, tryBody, exceptBody, typeList);
        }

        ArgumentList ParseTypeList ()
        {
            var argList = new ArgumentList (Location);
            while (!Match (TokenClass.CloseParan)) {
                argList.AddArgument (ParseExpression ());
                if (!Accept (TokenClass.Comma)) {
                    break;
                }
            }
            return argList;
        }

        /*
         * if (<expression> 
         *     <statement>
         * [
         * else
         *     <statement>
         * ]
         */
        AstNode ParseIf ()
        {
            SourceLocation location = Location;

            Expect (TokenClass.Keyword, "if");

            var predicate = ParseExpression ();

            var body = ParseStatement ();

            AstNode elseBody = null;

            if (Accept (TokenClass.Keyword, "else")) {
                elseBody = ParseStatement ();
            }
            return new IfStatement (location, predicate, body, elseBody);
        }

        AstNode ParseUnless ()
        {
            SourceLocation location = Location;

            Expect (TokenClass.Keyword, "unless");

            var predicate = new UnaryExpression(location, UnaryOperation.BoolNot, ParseExpression ());

            var body = ParseStatement ();

            AstNode elseBody = null;

            if (Accept (TokenClass.Keyword, "else")) {
                elseBody = ParseStatement ();
            }

            return new IfStatement (location, predicate, body, elseBody);
        }


        /*
         * NOTE: Usage of this foreach keyword is deprecated infavor of doing 
         * for <identifier> in <expression>
         * for <identifier> in <expression>
         *     <statement>
         */
        AstNode ParseForeach ()
        {
            Expect (TokenClass.Keyword, "for");

            var isParanExpected = Accept (TokenClass.OpenParan);

            bool anotherValue = false;
            var identifiers = new List<string> ();

            do {
                var identifier = Expect (TokenClass.Identifier);
                anotherValue = Accept (TokenClass.Comma);
                identifiers.Add (identifier.Value);
            } while (anotherValue);

            Expect (TokenClass.Keyword, "in");

            var expr = ParseExpression ();

            if (isParanExpected) {
                Expect (TokenClass.CloseParan);
            }

            var body = ParseStatement ();

            return new ForeachStatement (Location, identifiers, expr, body);
        }

        /*
         * do 
         *     <statement>
         * while (<expression>)
         */
        AstNode ParseDoWhile ()
        {
            SourceLocation location = Location;

            Expect (TokenClass.Keyword, "do");

            var body = ParseStatement ();

            if (Accept (TokenClass.Keyword, "while")) {

                var condition = ParseExpression ();

                return new DoStatement (location, condition, body);
            }

            if (Accept (TokenClass.Keyword, "until")) {
                var condition = new UnaryExpression (location, UnaryOperation.BoolNot, ParseExpression ());

                return new DoStatement (location, condition, body);
            }

            Expect (TokenClass.Keyword, "while");

            return null;
        }

        /*
         * while (<expression>) 
         *     <statement>
         */
        AstNode ParseWhile ()
        {
            SourceLocation location = Location;

            Expect (TokenClass.Keyword, "while");

            var condition = ParseExpression ();

            var body = ParseStatement ();

            return new WhileStatement (location, condition, body);
        }

        /*
         * until <expression>
         *    <statement>
         */

        AstNode ParseUntil ()
        {
            SourceLocation location = Location;

            Expect (TokenClass.Keyword, "until");

            var condition = new UnaryExpression (location, UnaryOperation.BoolNot, ParseExpression ());

            var body = ParseStatement ();

            return new WhileStatement (location, condition, body);
        }


        /*
         * with (<expression) 
         *      <statement>
         */
        AstNode ParseWith ()
        {
            SourceLocation location = Location;

            Expect (TokenClass.Keyword, "with");

            var value = ParseExpression ();

            var body = ParseStatement ();

            return new WithStatement (location, value, body);
        }

        /*
         * raise <expression>;
         */
        AstNode ParseRaise ()
        {
            Expect (TokenClass.Keyword, "raise");
            return new RaiseStatement (Location, ParseExpression ());
        }

        AstNode ParseReturn ()
        {
            Expect (TokenClass.Keyword, "return");

            if (Accept (TokenClass.SemiColon)) {
                return new ReturnStatement (Location, new CodeBlock (Location));
            }

            var ret = new ReturnStatement (Location, ParseExpression ());

            if (Accept (TokenClass.Keyword, "when")) {
                return new IfStatement (Location, ParseExpression (), ret);
            }

            return ret;

        }

        AstNode ParseYield ()
        {
            Expect (TokenClass.Keyword, "yield");
            return new YieldStatement (Location, ParseExpression ());
        }


        AstNode ParseAssignStatement ()
        {
            var identifiers = new List<string> ();

            bool isGlobal = false;

            if (Accept (TokenClass.Keyword, "global")) {
                isGlobal = true;
            } else {
                Accept (TokenClass.Keyword, "local");
            }

            SourceLocation location = Location;

            while (!Match (TokenClass.Operator, "=") && !EndOfStream) {
                var ident = Expect (TokenClass.Identifier);
                identifiers.Add (ident.Value);

                if (!Match (TokenClass.Operator, "=")) {
                    Expect (TokenClass.Comma);
                }
            }

            Expect (TokenClass.Operator, "=");

            bool isPacked = false;

            var expressions = new List<AstNode> ();

            do {
                expressions.Add (ParseExpression ());
            } while (Accept (TokenClass.Comma));

            if (identifiers.Count > 1 && expressions.Count == 1) {
                isPacked = true;
            }

            return new AssignStatement (location, isGlobal, identifiers, expressions, isPacked);
        }

        #endregion

        #region Expressions

        AstNode ParseExpression ()
        {
            return ParseGeneratorExpression ();
        }

        AstNode ParseGeneratorExpression ()
        {
            return ParseAssign ();
            // TODO: Reimplement generator expressions in a way that does
            // confict with new for statement

            /*
            bool isGenExpr = Match (TokenClass.Keyword, "for")
                && PeekToken (1) != null
                && PeekToken (1).Class != TokenClass.OpenParan;
            if (isGenExpr) {
                Expect (TokenClass.Keyword, "for");

                string ident = Expect (TokenClass.Identifier).Value;

                Expect (TokenClass.Keyword, "in");

                var iterator = ParseExpression ();
                AstNode predicate = null;
                if (Accept (TokenClass.Keyword, "if")) {
                    predicate = ParseExpression ();
                }
                return new GeneratorExpression (expr.Location, expr, ident, iterator, predicate);
            }
            return expr;
            */
        }

        AstNode ParseAssign ()
        {
            var expr = ParseTernaryIfElse ();
            while (Match (TokenClass.Operator)) {
                switch (Current.Value) {
                case "=":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location, BinaryOperation.Assign,
                        expr, ParseTernaryIfElse ());
                    continue;
                case "+=":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location, BinaryOperation.Assign, expr,
                        new BinaryExpression (Location,
                            BinaryOperation.Add, expr,
                            ParseTernaryIfElse ()));
                    continue;
                case "-=":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location, BinaryOperation.Assign, expr,
                        new BinaryExpression (Location,
                            BinaryOperation.Sub,
                            expr,
                            ParseTernaryIfElse ()));
                    continue;
                case "*=":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location, BinaryOperation.Assign, expr,
                        new BinaryExpression (Location,
                            BinaryOperation.Mul,
                            expr,
                            ParseTernaryIfElse ()));
                    continue;
                case "/=":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location, BinaryOperation.Assign, expr,
                        new BinaryExpression (Location,
                            BinaryOperation.Div,
                            expr,
                            ParseTernaryIfElse ()));
                    continue;
                case "%=":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location, BinaryOperation.Assign, expr,
                        new BinaryExpression (Location,
                            BinaryOperation.Mod,
                            expr,
                            ParseTernaryIfElse ()));
                    continue;
                case "^=":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location, BinaryOperation.Assign, expr,
                        new BinaryExpression (Location,
                            BinaryOperation.Xor,
                            expr, 
                            ParseTernaryIfElse ()));
                    continue;
                case "&=":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location, BinaryOperation.Assign, expr,
                        new BinaryExpression (Location,
                            BinaryOperation.And,
                            expr,
                            ParseTernaryIfElse ()));
                    continue;
                case "|=":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location, BinaryOperation.Assign, expr,
                        new BinaryExpression (Location,
                            BinaryOperation.Or,
                            expr,
                            ParseTernaryIfElse ()));
                    continue;
                case "<<=":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location, BinaryOperation.Assign, expr,
                        new BinaryExpression (Location,
                            BinaryOperation.LeftShift,
                            expr,
                            ParseTernaryIfElse ()));
                    continue;
                case ">>=":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location, BinaryOperation.Assign, expr,
                        new BinaryExpression (Location,
                            BinaryOperation.RightShift,
                            expr,
                            ParseTernaryIfElse ()));
                    continue;
                }
                break;
            }
            return expr;
        }

        AstNode ParseTernaryIfElse ()
        {
            var expr = ParseRange ();

            int backup = position;

            if (Accept (TokenClass.Keyword, "when")) {
                var condition = ParseExpression ();
                if (Accept (TokenClass.Keyword, "else")) {
                    var altValue = ParseTernaryIfElse ();
                    expr = new TernaryExpression (expr.Location, condition, expr, altValue);
                } else {
                    position = backup;
                }
            }
            return expr;
        }

        AstNode ParseRange ()
        {
            var expr = ParseBoolOr ();
            while (Match (TokenClass.Operator)) {
                switch (Current.Value) {
                case "...":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (
                        Location,
                        BinaryOperation.ClosedRange,
                        expr,
                        ParseBoolOr ()
                    );
                    continue;
                case "..":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (
                        Location,
                        BinaryOperation.HalfRange,
                        expr,
                        ParseBoolOr ()
                    );
                    continue;
                }
                break;
            }
            return expr;
        }

        AstNode ParseBoolOr ()
        {
            var expr = ParseBoolAnd ();
            while (Match (TokenClass.Operator)) {
                switch (Current.Value) {
                case "||":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location, BinaryOperation.BoolOr, expr,
                        ParseBoolAnd ());
                    continue;
                case "??":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location, BinaryOperation.NullCoalescing, expr,
                        ParseBoolAnd ());
                    continue;
                }
                break;
            }
            return expr;
        }

        AstNode ParseBoolAnd ()
        {
            var expr = ParseOr ();
            while (Accept (TokenClass.Operator, "&&")) {
                expr = new BinaryExpression (Location, BinaryOperation.BoolAnd, expr, ParseOr ());
            }
            return expr;
        }

        AstNode ParseOr ()
        {
            var expr = ParseXor ();
            while (Accept (TokenClass.Operator, "|")) {
                expr = new BinaryExpression (Location, BinaryOperation.Or, expr, ParseXor ());
            }
            return expr;
        }

        AstNode ParseXor ()
        {
            var expr = ParseAnd ();
            while (Accept (TokenClass.Operator, "^")) {
                expr = new BinaryExpression (Location, BinaryOperation.Xor, expr, ParseAnd ());
            }
            return expr;
        }

        AstNode ParseAnd ()
        {
            var expr = ParseEquals ();
            while (Accept (TokenClass.Operator, "&")) {
                expr = new BinaryExpression (Location, BinaryOperation.And, expr,
                    ParseEquals ());
            }
            return expr;
        }

        AstNode ParseEquals ()
        {
            var expr = ParseRelationalOp ();
            while (Match (TokenClass.Operator)) {
                switch (Current.Value) {
                case "==":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location, BinaryOperation.Equals, expr,
                        ParseRelationalOp ());
                    continue;
                case "!=":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location, BinaryOperation.NotEquals, expr,
                        ParseRelationalOp ());
                    continue;
                }
                break;
            }
            return expr;
        }

        AstNode ParseRelationalOp ()
        {
            var expr = ParseBitshift ();
            while (Match (TokenClass.Operator)) {
                switch (Current.Value) {
                case ">":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location,
                        BinaryOperation.GreaterThan,
                        expr,
                        ParseBitshift ()
                    );
                    continue;
                case "<":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location,
                        BinaryOperation.LessThan,
                        expr,
                        ParseBitshift ()
                    );
                    continue;
                case ">=":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location,
                        BinaryOperation.GreaterThanOrEqu,
                        expr,
                        ParseBitshift ()
                    );
                    continue;
                case "<=":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location,
                        BinaryOperation.LessThanOrEqu,
                        expr,
                        ParseBitshift ()
                    );
                    continue;
                case "is":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location,
                        BinaryOperation.InstanceOf,
                        expr,
                        ParseBitshift ()
                    );
                    continue;
                case "isnot":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location,
                        BinaryOperation.NotInstanceOf,
                        expr,
                        ParseBitshift ()
                    );
                    continue;
                case "as":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location,
                        BinaryOperation.DynamicCast,
                        expr,
                        ParseBitshift ()
                    );
                    continue;
                }
                break;
            }
            return expr;
        }

        AstNode ParseBitshift ()
        {
            var expr = ParseAdditive ();
            while (Match (TokenClass.Operator)) {
                switch (Current.Value) {
                case "<<":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location, BinaryOperation.LeftShift, expr,
                        ParseAdditive ());
                    continue;
                case ">>":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location, BinaryOperation.RightShift, expr,
                        ParseAdditive ());
                    continue;
                }
                break;
            }
            return expr;
        }

        AstNode ParseAdditive ()
        {
            var expr = ParseMultiplicative ();
            while (Match (TokenClass.Operator)) {
                switch (Current.Value) {
                case "+":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location, BinaryOperation.Add, expr,
                        ParseMultiplicative ());
                    continue;
                case "-":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location, BinaryOperation.Sub, expr,
                        ParseMultiplicative ());
                    continue;
                }
                break;
            }
            return expr;
        }

        AstNode ParseMultiplicative ()
        {
            var expr = ParseUnary ();
            while (Match (TokenClass.Operator)) {
                switch (Current.Value) {
                case "**":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location, BinaryOperation.Pow, expr,
                        ParseUnary ());
                    continue;
                case "*":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location, BinaryOperation.Mul, expr,
                        ParseUnary ());
                    continue;
                case "/":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location, BinaryOperation.Div, expr,
                        ParseUnary ());
                    continue;
                case "%":
                    Accept (TokenClass.Operator);
                    expr = new BinaryExpression (Location, BinaryOperation.Mod, expr,
                        ParseUnary ());
                    continue;
                }
                break;
            }
            return expr;
        }

        AstNode ParseUnary ()
        {
            if (Match (TokenClass.Operator)) {
                switch (Current.Value) {
                case "-":
                    Accept (TokenClass.Operator);
                    return new UnaryExpression (Location, UnaryOperation.Negate, ParseUnary ());
                case "~":
                    Accept (TokenClass.Operator);
                    return new UnaryExpression (Location, UnaryOperation.Not, ParseUnary ());
                case "!":
                    Accept (TokenClass.Operator);
                    return new UnaryExpression (Location, UnaryOperation.BoolNot, ParseUnary ());

                }
            }
            return ParseCallSubscriptAccess ();
        }

        AstNode ParseCallSubscriptAccess ()
        {
            return ParseCallSubscriptAccess (ParseMatchExpression ());
        }

        AstNode ParseCallSubscriptAccess (AstNode lvalue)
        {
            if (Current != null) {
                switch (Current.Class) {
                case TokenClass.OpenParan:
                    return ParseCallSubscriptAccess (
                        new CallExpression (Location, lvalue, ParseArgumentList ())
                    );
                case TokenClass.OpenBracket:
                    return ParseCallSubscriptAccess (ParseIndexerExpression (lvalue));
                case TokenClass.MemberAccess:
                    return ParseCallSubscriptAccess (ParseGetExpression (lvalue));
                case TokenClass.MemberDefaultAccess:
                    return ParseCallSubscriptAccess (ParseGetOrNullExpression (lvalue));
                }
            }

            return lvalue;
        }

        AstNode ParseIndexerExpression (AstNode lvalue)
        {
            Expect (TokenClass.OpenBracket);

            if (Accept (TokenClass.Colon)) {
                return ParseSlice (lvalue, null);
            }

            var index = ParseExpression ();

            if (Accept (TokenClass.Colon)) {
                return ParseSlice (lvalue, index);
            }

            Expect (TokenClass.CloseBracket);
            return new IndexerExpression (Location, lvalue, index);
        }

        AstNode ParseGetExpression (AstNode lvalue)
        {
            Expect (TokenClass.MemberAccess);

            var ident = Expect (TokenClass.Identifier);

            return new MemberExpression (Location, lvalue, ident.Value);
        }

        AstNode ParseGetOrNullExpression (AstNode lvalue)
        {
            Expect (TokenClass.MemberDefaultAccess);

            var ident = Expect (TokenClass.Identifier);

            return new MemberDefaultExpression (Location, lvalue, ident.Value);
        }

        AstNode ParseMatchExpression ()
        {
            var matchLocation = Location;

            if (Accept (TokenClass.Keyword, "match")) {
                var expr = new MatchExpression (matchLocation, ParseExpression ());
                Expect (TokenClass.OpenBrace);
                while (Accept (TokenClass.Keyword, "case")) {
                    AstNode condition = null;

                    var pattern = ParsePattern ();

                    if (Accept (TokenClass.Keyword, "when")) {
                        condition = ParseExpression ();
                    }

                    AstNode value = null;

                    if (Accept (TokenClass.Operator, "=>")) {
                        value = ParseExpression ();
                        expr.AddCase (new CaseExpression (
                            pattern.Location,
                            pattern,
                            condition,
                            value,
                            false
                        ));
                    } else {
                        value = ParseStatement ();
                        expr.AddCase (new CaseExpression (
                            pattern.Location,
                            pattern, 
                            condition,
                            value,
                            true
                        ));
                    }
                }
                Expect (TokenClass.CloseBrace);
                return expr;
            }

            return ParseLambdaExpression ();
        }

        AstNode ParsePattern ()
        {
            return ParsePatternOr ();
        }

        AstNode ParsePatternOr ()
        {
            var expr = ParsePatternAnd ();
            while (Match (TokenClass.Operator, "|")) {
                Accept (TokenClass.Operator);
                expr = new PatternExpression (Location,
                    BinaryOperation.Or,
                    expr,
                    ParsePatternAnd ()
                );
            }
            return expr;
        }

        AstNode ParsePatternAnd ()
        {
            var expr = ParsePatternExtractor ();
            while (Match (TokenClass.Operator, "&")) {
                Accept (TokenClass.Operator);
                expr = new PatternExpression (Location,
                    BinaryOperation.And,
                    expr,
                    ParsePatternExtractor ()
                );
            }
            return expr;
        }

        AstNode ParsePatternExtractor ()
        {
            var ret = ParsePatternRange ();

            if (Accept (TokenClass.OpenParan)) {
                ret = new PatternExtractExpression (Location, ret);

                while (!Match (TokenClass.CloseParan)) {
                    var capture = Expect (TokenClass.Identifier);

                    ((PatternExtractExpression)ret).Captures.Add (capture.Value);

                    if (!Match (TokenClass.CloseParan)) {
                        Expect (TokenClass.Comma);
                    }
                }

                Expect (TokenClass.CloseParan);
            }

            return ret;
        }

        AstNode ParsePatternRange ()
        {
            var expr = ParsePatternClosedRange ();

            while (Match (TokenClass.Operator, "..")) {
                Accept (TokenClass.Operator);
                expr = new PatternExpression (
                    Location,
                    BinaryOperation.HalfRange,
                    expr,
                    ParsePatternClosedRange ()
                );
            }
            return expr;
        }

        AstNode ParsePatternClosedRange ()
        {
            var expr = ParsePatternTerm ();

            while (Match (TokenClass.Operator, "...")) {
                Accept (TokenClass.Operator);
                expr = new PatternExpression (
                    Location,
                    BinaryOperation.ClosedRange,
                    expr,
                    ParsePatternTerm ()
                );
            }
            return expr;
        }

        AstNode ParsePatternTerm ()
        {
            return ParseLiteral ();
        }

        AstNode ParseLambdaExpression ()
        {
            if (Accept (TokenClass.Keyword, "lambda")) {
                bool isInstanceMethod;
                bool isVariadic;
                bool acceptsKwargs;
                bool hasDefaultVals;

                var parameters = ParseFuncParameters (
                    out isInstanceMethod,
                    out isVariadic,
                    out acceptsKwargs,
                    out hasDefaultVals
                );

                var decl = new LambdaExpression (
                    Location, 
                    isInstanceMethod, 
                    isVariadic, 
                    acceptsKwargs,
                    hasDefaultVals,
                    parameters
                );

                if (Accept (TokenClass.Operator, "=>")) {
                    decl.AddStatement (new ReturnStatement (
                        Location,
                        ParseExpression ()
                    ));
                } else {
                    decl.AddStatement (ParseStatement ());
                }

                return decl;
            }

            return ParseLiteral ();
        }
            
        AstNode ParseLiteral ()
        {
            if (Current == null) {
                errorLog.Add (Errors.UnexpectedEndOfFile, Location);
                throw new EndOfFileException ();
            }

            switch (Current.Class) {
            case TokenClass.OpenBracket:
                return ParseListLiteral ();
            case TokenClass.OpenBrace:
                return ParseHashLiteral ();
            case TokenClass.OpenParan:
                ReadToken ();
                var expr = ParseExpression ();
                if (Accept (TokenClass.Comma)) {
                    return ParseTupleLiteral (expr);
                }
                Expect (TokenClass.CloseParan);
                return expr;
            default:
                return ParseTerminal ();
            }
        }

        AstNode ParseListLiteral ()
        {
            Expect (TokenClass.OpenBracket);
            var ret = new ListExpression (Location);
            while (!Match (TokenClass.CloseBracket)) {
                var expr = ParseAssign ();
                if (Accept (TokenClass.Keyword, "for")) {
                    string ident = Expect (TokenClass.Identifier).Value;
                    Expect (TokenClass.Keyword, "in");
                    var iterator = ParseExpression ();
                    AstNode predicate = null;
                    if (Accept (TokenClass.Keyword, "if")) {
                        predicate = ParseExpression ();
                    }
                    Expect (TokenClass.CloseBracket);
                    return new ListCompExpression (expr.Location, expr, ident, iterator, predicate);
                }
                ret.AddItem (expr);
                if (!Accept (TokenClass.Comma)) {
                    break;
                }
            }
            Expect (TokenClass.CloseBracket);
            return ret;
        }

        AstNode ParseHashLiteral ()
        {
            Expect (TokenClass.OpenBrace);
            var ret = new HashExpression (Location);
            while (!Match (TokenClass.CloseBrace)) {
                var key = ParseExpression ();
                Expect (TokenClass.Colon);
                var value = ParseExpression ();
                ret.AddItem (key, value);
                if (!Accept (TokenClass.Comma)) {
                    break;
                }
            }
            Expect (TokenClass.CloseBrace);
            return ret;
        }

        AstNode ParseTupleLiteral (AstNode firstVal)
        {
            var tuple = new TupleExpression (Location);
            tuple.AddItem (firstVal);
            while (!Match (TokenClass.CloseParan)) {
                tuple.AddItem (ParseExpression ());
                if (!Accept (TokenClass.Comma)) {
                    break;
                }
            }
            Expect (TokenClass.CloseParan);
            return tuple;
        }

        AstNode ParseTerminal ()
        {
            switch (Current.Class) {
            case TokenClass.Identifier:
                return new NameExpression (Location, ReadToken ().Value);
            case TokenClass.IntLiteral:
                long lval64;
                if (!long.TryParse (Current.Value, out lval64)) {
                    errorLog.Add (Errors.IntegerOverBounds, Current.Location);
                }
                ReadToken ();
                return new IntegerExpression (Location, lval64);
            case TokenClass.BigIntLiteral:
                BigInteger bintVal;
                if (!BigInteger.TryParse (Current.Value, out bintVal)) {
                    errorLog.Add (Errors.IntegerOverBounds, Current.Location);
                }
                ReadToken ();
                return new BigIntegerExpression (Location, bintVal);
            case TokenClass.FloatLiteral:
                return new FloatExpression (Location, double.Parse (
                    ReadToken ().Value));
            case TokenClass.InterpolatedStringLiteral:
                var val = ParseString (Location, ReadToken ().Value);
                if (val == null) {
                    MakeError ();
                    return new StringExpression (Location, "");
                }
                return val;
            case TokenClass.StringLiteral:
                return new StringExpression (Location, ReadToken ().Value);
            case TokenClass.RegexLiteral:
                return new RegexExpression (Location, ReadToken ().Value);
            case TokenClass.BinaryStringLiteral:
                return new StringExpression (Location, ReadToken ().Value, true);
            case TokenClass.Keyword:
                switch (Current.Value) {
                case "self":
                    ReadToken ();
                    return new SelfExpression (Location);
                case "true":
                    ReadToken ();
                    return new TrueExpression (Location);
                case "false":
                    ReadToken ();
                    return new FalseExpression (Location);
                case "null":
                    ReadToken ();
                    return new NullExpression (Location);
                }
                break;
            }
        
            MakeError ();
            return null;
        }
            
        AstNode ParseSlice (AstNode lvalue, AstNode start)
        {
            if (Accept (TokenClass.CloseBracket)) {
                return new SliceExpression (lvalue.Location, lvalue, start, null, null);
            }

            AstNode end = null;
            AstNode step = null;

            if (Accept (TokenClass.Colon)) {
                step = ParseExpression ();
            } else {
                end = ParseExpression ();

                if (Accept (TokenClass.Colon)) {
                    step = ParseExpression ();
                }
            }

            Expect (TokenClass.CloseBracket);

            return new SliceExpression (lvalue.Location, lvalue, start, end, step);
        }
            
        SuperCallStatement ParseSuperCall (ClassDeclaration parent)
        {
            SourceLocation location = Location;
            Expect (TokenClass.Keyword, "super");
            var argumentList = ParseArgumentList ();
            while (Accept (TokenClass.SemiColon))
                ;
            return new SuperCallStatement (location, parent, argumentList);
        }

        ArgumentList ParseArgumentList ()
        {
            var argList = new ArgumentList (Location);
            Expect (TokenClass.OpenParan);
            KeywordArgumentList kwargs = null;
            while (!Match (TokenClass.CloseParan)) {
                if (Accept (TokenClass.Operator, "*")) {
                    argList.Packed = true;
                    argList.AddArgument (ParseExpression ());
                    break;
                }
                var arg = ParseExpression ();
                if (Accept (TokenClass.Colon)) {
                    if (kwargs == null) {
                        kwargs = new KeywordArgumentList (arg.Location);
                    }
                    var ident = arg as NameExpression;
                    var val = ParseExpression ();
                    if (ident == null) {
                        errorLog.Add (Errors.ExpectedIdentifier, Location);
                    } else {
                        kwargs.Add (ident.Value, val);
                    }
                } else
                    argList.AddArgument (arg);
                if (!Accept (TokenClass.Comma)) {
                    break;
                }

            }
            if (kwargs != null) {
                argList.AddArgument (kwargs);
            }
            Expect (TokenClass.CloseParan);
            return argList;

        }

        AstNode ParseString (SourceLocation loc, string str)
        {
            /*
             * This might be a *bit* hacky, but, basically Iodine string interpolation
             * is *basically* just syntactic sugar for Str.format (...)
             */
            int pos = 0;
            string accum = "";
            var subExpressions = new List<string> ();
            while (pos < str.Length) {
                if (str [pos] == '#' && str.Length != pos + 1 && str [pos + 1] == '{') {
                    var substr = str.Substring (pos + 2);
                    if (substr.IndexOf ('}') == -1)
                        return null;
                    substr = substr.Substring (0, substr.IndexOf ('}'));
                    pos += substr.Length + 3;
                    subExpressions.Add (substr);
                    accum += "{}";

                } else {
                    accum += str [pos++];
                }
            }

            var ret = new StringExpression (loc, accum);

            foreach (string name in subExpressions) {
                var tokenizer = new Tokenizer (
                    errorLog,
                    SourceUnit.CreateFromSource (name).GetReader ()
                );

                var parser = new Parser (context, tokenizer.Scan ());
                var expression = parser.ParseExpression ();
                ret.AddSubExpression (expression);
            }
            return ret;
        }

        #endregion

        #region Token Manipulation functions

        public void Synchronize ()
        {
            while (Current != null) {
                var tok = ReadToken ();
                switch (tok.Class) {
                case TokenClass.CloseBracket:
                case TokenClass.SemiColon:
                    return;
                }
            }
        }

        public void AddToken (Token token)
        {
            tokens.Add (token);
        }

        public bool Match (TokenClass clazz)
        {
            return PeekToken () != null && PeekToken ().Class == clazz;
        }

        public bool Match (TokenClass clazz1, TokenClass clazz2)
        {
            return PeekToken () != null &&
                PeekToken ().Class == clazz1 &&
                PeekToken (1) != null &&
                PeekToken (1).Class == clazz2;
        }

        public bool Match (TokenClass clazz, string val)
        {
            return PeekToken () != null &&
                PeekToken ().Class == clazz &&
                PeekToken ().Value == val;
        }

        public bool Match (int lookahead, TokenClass clazz)
        {
            return PeekToken (lookahead) != null &&PeekToken (lookahead).Class == clazz;
        }

        public bool Match (int lookahead, TokenClass clazz, string val)
        {
            return PeekToken (lookahead) != null &&
                PeekToken (lookahead).Class == clazz &&
                PeekToken (lookahead).Value == val;
        }

        public bool Accept (TokenClass clazz)
        {
            if (PeekToken () != null && PeekToken ().Class == clazz) {
                ReadToken ();
                return true;
            }
            return false;
        }

        public bool Accept (TokenClass clazz, ref Token token)
        {
            if (PeekToken () != null && PeekToken ().Class == clazz) {
                token = ReadToken ();
                return true;
            }
            return false;
        }

        public bool Accept (TokenClass clazz, string val)
        {
            if (PeekToken () != null && PeekToken ().Class == clazz && PeekToken ().Value == val) {
                ReadToken ();
                return true;
            }
            return false;
        }

        public Token Expect (TokenClass clazz)
        {
            Token ret = null;

            if (Accept (clazz, ref ret)) {
                return ret;
            }

            var offender = ReadToken ();

            if (offender != null) {
                errorLog.Add (
                    Errors.UnexpectedToken,
                    offender,
                    offender.Location,
                    offender.Value
                );
                throw new SyntaxException (errorLog);
            }

            errorLog.Add (Errors.UnexpectedEndOfFile, Location);

            throw new EndOfFileException ();
        }

        public Token Expect (TokenClass clazz, string val)
        {
            var ret = PeekToken ();

            if (Accept (clazz, val)) {
                return ret;
            }

            var offender = ReadToken ();

            if (offender != null) {
                errorLog.Add (
                    Errors.UnexpectedToken,
                    offender,
                    offender.Location,
                    offender.Value
                );

                throw new SyntaxException (errorLog);
            }

            errorLog.Add (Errors.UnexpectedEndOfFile, Location);
            throw new EndOfFileException ();
        }

        public void MakeError ()
        {
            if (PeekToken () == null) {
                errorLog.Add (Errors.UnexpectedEndOfFile, Location);
                throw new EndOfFileException ();
            }

            errorLog.Add (
                Errors.UnexpectedToken,
                PeekToken ().Location,
                ReadToken ().Value
            );

            throw new SyntaxException (errorLog);
        }

        Token PeekToken ()
        {
            return PeekToken (0);
        }

        public Token PeekToken (int n)
        {
            if (position + n < tokens.Count) {
                return tokens [position + n];
            }
            return null;
        }

        public Token ReadToken ()
        {
            if (position >= tokens.Count) {
                return null;
            }
            return tokens [position++];
        }
        #endregion
    }
}


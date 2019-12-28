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

using System.Collections.Generic;
using Iodine.Compiler.Ast;

namespace Iodine.Compiler
{
    class SemanticAnalyser : AstVisitor
    {
        ErrorSink errorLog;
        SymbolTable symbolTable = new SymbolTable ();

        public SemanticAnalyser (ErrorSink errorLog)
        {
            this.errorLog = errorLog;
        }

        public SymbolTable Analyse (CompilationUnit ast)
        {
            ast.VisitChildren (this);
            return symbolTable;
        }

        public override void Accept (ClassDeclaration classDecl)
        {
            symbolTable.AddSymbol (classDecl.Name);
        }

        public override void Accept (EnumDeclaration enumDecl)
        {
            symbolTable.AddSymbol (enumDecl.Name);
        }

        public override void Accept (ContractDeclaration interfaceDecl)
        {
            symbolTable.AddSymbol (interfaceDecl.Name);
        }

        void AddFunctionParameters (IEnumerable<FunctionParameter> parameters)
        {
            foreach (FunctionParameter param in parameters) {
                var namedParam = param as NamedParameter;

                if (namedParam != null) {
                    symbolTable.AddSymbol (namedParam.Name);
                    continue;
                }

                var tupleParam = param as DecompositionParameter;

                if (tupleParam != null) {
                    AddFunctionParameters (tupleParam.CaptureNames);
                }
            }
        }

        public override void Accept (FunctionDeclaration funcDecl)
        {
            symbolTable.AddSymbol (funcDecl.Name);

            symbolTable.EnterScope ();

            AddFunctionParameters (funcDecl.Parameters);

            funcDecl.VisitChildren (this);

            symbolTable.ExitScope ();

        }

        public override void Accept (CodeBlock scope)
        {
            symbolTable.EnterScope ();
            scope.VisitChildren (this);
            symbolTable.ExitScope ();
        }

        public override void Accept (StatementList stmtList)
        {
            stmtList.VisitChildren (this);
        }

        public override void Accept (IfStatement ifStmt)
        {
            ifStmt.VisitChildren (this);
        }

        public override void Accept (ForStatement forStmt)
        {
            forStmt.VisitChildren (this);
        }

        public override void Accept (ForeachStatement foreachStmt)
        {
            foreachStmt.VisitChildren (this);
        }

        public override void Accept (WhileStatement whileStmt)
        {
            whileStmt.VisitChildren (this);
        }

        public override void Accept (DoStatement doStmt)
        {
            doStmt.VisitChildren (this);
        }

        public override void Accept (SuperCallStatement super)
        {
            super.VisitChildren (this);
        }

        public override void Accept (ReturnStatement returnStmt)
        {
            returnStmt.VisitChildren (this);
        }

        public override void Accept (Expression expr)
        {
            expr.VisitChildren (this);
        }

        public override void Accept (CallExpression call)
        {
            call.VisitChildren (this);
        }

        public override void Accept (ArgumentList arglist)
        {
            arglist.VisitChildren (this);
        }

        public override void Accept (IndexerExpression indexer)
        {
            indexer.VisitChildren (this);
        }

        public override void Accept (MemberExpression getAttr)
        {
            getAttr.VisitChildren (this);
        }

        public override void Accept (MemberDefaultExpression getAttr)
        {
            getAttr.VisitChildren (this);
        }

        public override void Accept (TernaryExpression ifExpr)
        {
            ifExpr.VisitChildren (this);
        }

        public override void Accept (BinaryExpression binop)
        {
            if (binop.Operation == BinaryOperation.Assign &&
                binop.Left is NameExpression) {
                NameExpression name = binop.Left as NameExpression;
                if (!symbolTable.IsSymbolDefined (name.Value)) {
                    symbolTable.AddSymbol (name.Value);
                }
            }
            binop.VisitChildren (this);
        }

        public override void Accept (ListCompExpression list)
        {
            list.VisitChildren (this);
        }

        public override void Accept (TupleExpression tuple)
        {
            tuple.VisitChildren (this);
        }

        public override void Accept (HashExpression hash)
        {
            hash.VisitChildren (this);
        }

        public override void Accept (ListExpression list)
        {
            list.VisitChildren (this);
        }

        public override void Accept (PatternExpression expression)
        {
            expression.VisitChildren (this);
        }

        public override void Accept (TryExceptStatement tryCatch)
        {
            tryCatch.VisitChildren (this);
        }

        public override void Accept (CaseExpression caseExpr)
        {
            caseExpr.VisitChildren (this);
        }

        public override void Accept (WithStatement with)
        {
            with.VisitChildren (this);
        }

        public override void Accept (PatternExtractExpression extractExpression)
        {
            extractExpression.Target.Visit (this);

            foreach (string capture in extractExpression.Captures) {
                symbolTable.AddSymbol (capture);
            }

        }

        public override void Accept (CompilationUnit ast)
        {
            ast.VisitChildren (this);
        }

        public override void Accept (DecoratedFunction funcDecl)
        {
            funcDecl.VisitChildren (this);
        }

        public override void Accept (ExtendStatement exten)
        {
            exten.VisitChildren (this);
        }

        public override void Accept (LambdaExpression lambda)
        {
            lambda.VisitChildren (this);
        }

        public override void Accept (MatchExpression match)
        {
            bool hasCatchall = false;
            bool hasTruePattern = false;
            bool hasFalsePattern = false;

            foreach (AstNode node in match.MatchCases) {
                var matchCase = node as CaseExpression;


                if (matchCase != null) {
                    var nameExpr = matchCase.Pattern as NameExpression;

                    if (nameExpr != null && nameExpr.Value == "_") {
                        hasCatchall = true;
                    }

                    var trueExpr = matchCase.Pattern as TrueExpression;

                    if (trueExpr != null) {
                        hasTruePattern = true;
                    }

                    var falseExpr = matchCase.Pattern as FalseExpression;


                    if (falseExpr != null) {
                        hasFalsePattern = true;
                    }
                }
            }

            bool isLegal = hasCatchall || (hasTruePattern && hasFalsePattern);

            if (!isLegal) {
                errorLog.Add (Errors.MatchDoesNotAccountForAllConditions,
                              match.Location);
            }

            base.Accept (match);
        }
    }
}


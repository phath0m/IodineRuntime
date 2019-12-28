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
using Iodine.Compiler.Ast;

namespace Iodine.Compiler
{
    /// <summary>
    /// Abstract class for implementing a visitor pattern to walk an Iodine abstract syntax
    /// tree.
    /// </summary>
    public abstract class AstVisitor
    {
        public virtual void Accept (CompilationUnit ast)
        {
        }

        public virtual void Accept (Expression expr)
        {
        }

        public virtual void Accept (StatementList stmtList)
        {
        }

        public virtual void Accept (Statement stmt)
        {
        }

        public virtual void Accept (ExtendStatement exten)
        {
        }

        public virtual void Accept (BinaryExpression binop)
        {
        }

        public virtual void Accept (UnaryExpression unaryop)
        {
        }

        public virtual void Accept (NameExpression ident)
        {
        }

        public virtual void Accept (CallExpression call)
        {
        }

        public virtual void Accept (ArgumentList arglist)
        {
        }

        public virtual void Accept (KeywordArgumentList kwargs)
        {
        }

        public virtual void Accept (MemberExpression getAttr)
        {
        }

        public virtual void Accept (MemberDefaultExpression getAttr)
        {
        }

        public virtual void Accept (IntegerExpression integer)
        {
        }

        public virtual void Accept (BigIntegerExpression integer)
        {
        }

        public virtual void Accept (IfStatement ifStmt)
        {
        }

        public virtual void Accept (WhileStatement whileStmt)
        {
        }

        public virtual void Accept (DoStatement doStmt)
        {
        }

        public virtual void Accept (ForStatement forStmt)
        {
        }

        public virtual void Accept (ForeachStatement foreachStmt)
        {
        }

        public virtual void Accept (FunctionDeclaration funcDecl)
        {
        }

        public virtual void Accept (DecoratedFunction funcDecl)
        {
        }

        public virtual void Accept (CodeBlock scope)
        {
        }

        public virtual void Accept (StringExpression stringConst)
        {
        }

        public virtual void Accept (UseStatement useStmt)
        {
        }

        public virtual void Accept (ContractDeclaration interfaceDecl)
        {
        }

        public virtual void Accept (TraitDeclaration traitDecl)
        {
        }

        public virtual void Accept (MixinDeclaration traitDecl)
        {
        }

        public virtual void Accept (ClassDeclaration classDecl)
        {
        }

        public virtual void Accept (ReturnStatement returnStmt)
        {
        }

        public virtual void Accept (YieldStatement yieldStmt)
        {
        }

        public virtual void Accept (IndexerExpression indexer)
        {
        }

        public virtual void Accept (SliceExpression slice)
        {
        }

        public virtual void Accept (ListExpression list)
        {
        }

        public virtual void Accept (HashExpression hash)
        {
        }

        public virtual void Accept (SelfExpression self)
        {
        }

        public virtual void Accept (TrueExpression ntrue)
        {
        }

        public virtual void Accept (FalseExpression nfalse)
        {
        }

        public virtual void Accept (NullExpression nil)
        {
        }

        public virtual void Accept (LambdaExpression lambda)
        {
        }

        public virtual void Accept (TryExceptStatement tryCatch)
        {
        }

        public virtual void Accept (WithStatement with)
        {
        }

        public virtual void Accept (BreakStatement brk)
        {
        }

        public virtual void Accept (ContinueStatement cont)
        {
        }

        public virtual void Accept (TupleExpression tuple)
        {
        }

        public virtual void Accept (FloatExpression dec)
        {
        }

        public virtual void Accept (SuperCallStatement super)
        {
        }

        public virtual void Accept (EnumDeclaration enumDecl)
        {
        }

        public virtual void Accept (RaiseStatement raise)
        {
        }

        public virtual void Accept (MatchExpression match)
        {
        }

        public virtual void Accept (CaseExpression caseExpr)
        {
        }

        public virtual void Accept (PatternExpression expression)
        {
        }

        public virtual void Accept (PatternExtractExpression extractExpression)
        {
        }

        public virtual void Accept (ListCompExpression list)
        {
        }

        public virtual void Accept (GeneratorExpression genExpr)
        {
        }

        public virtual void Accept (TernaryExpression ifExpr)
        {
        }

        public virtual void Accept (AssignStatement assign)
        {
        }

        public virtual void Accept (RegexExpression regex)
        {
        }
    }
}


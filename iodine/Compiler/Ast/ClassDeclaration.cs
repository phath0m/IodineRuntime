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

namespace Iodine.Compiler.Ast
{
    public class ClassDeclaration : AstNode
    {
        public readonly string Name;
        public readonly string Documentation;
        public readonly List<AstNode> Interfaces = new List<AstNode> ();
        public readonly List<AstNode> Mixins = new List<AstNode> ();

        /// <summary>
        /// Expression that when evaluates to this class's base class
        /// </summary>
        /// <value>The base class.</value>
        public AstNode BaseClass {
            get;
            set;
        }

        /// <summary>
        /// Function which will act as the classes constructor (Note: By default this is 
        /// just an empty function)
        /// </summary>
        /// <value>The constructor.</value>
        public FunctionDeclaration Constructor {
            get;
            set;
        }

        public readonly List<AstNode> Members = new List<AstNode> ();

        public ClassDeclaration (SourceLocation location,
            string name,
            string doc) : base (location)
        {
            Name = name;
            Documentation = doc;

            /*
             * Create an empty constructor that will call the class's super constructor
             */

            var emptyCtor = new FunctionDeclaration (
                location,
                name,
                true,
                false,
                false,
                false,
                new List<FunctionParameter> (),
                doc
            );

            /*
             * This is important for insuring that the super class is properly initialized
             */


            var callToSuper = new SuperCallStatement (location, this,
                                                      new ArgumentList (location));

            emptyCtor.Body.AddStatement (callToSuper);

            Constructor = emptyCtor;
        }

        public ClassDeclaration (SourceLocation location,
            string name,
            string doc,
            List<FunctionParameter> parameters)
            : base (location)
        {
            Name = name;
            Documentation = doc;

            var recordCtor = new FunctionDeclaration (
                location,
                name,
                true,
                false,
                false,
                true,
                parameters,
                doc
            );

            recordCtor.Body.AddStatement (new SuperCallStatement (location, this, new ArgumentList (location)));

            foreach (NamedParameter parameter in parameters) {
                recordCtor.Body.AddStatement (
                    new Expression (
                        location,
                        new BinaryExpression (location,
                            BinaryOperation.Assign,
                            new MemberExpression (location,
                                new SelfExpression (location),
                                parameter.Name
                            ),
                            new NameExpression (location, parameter.Name)
                        )
                    )
                );
            }

            Constructor = recordCtor;
        }

        public void Add (AstNode item)
        {
            Members.Add (item);
        }

        public override void Visit (AstVisitor visitor)
        {
            visitor.Accept (this);
        }

        public override void VisitChildren (AstVisitor visitor)
        {
            Constructor.Visit (visitor);
            Members.ForEach (p => p.Visit (visitor));
        }
    }
}

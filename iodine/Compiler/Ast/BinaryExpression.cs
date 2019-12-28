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

namespace Iodine.Compiler.Ast
{
    public class BinaryExpression : AstNode
    {
        public readonly BinaryOperation Operation;

        public readonly AstNode Left;

        public readonly AstNode Right;

        public BinaryExpression (SourceLocation location, BinaryOperation op, AstNode left, AstNode right)
            : base (location)
        {
            Operation = op;
            Left = left;
            Right = right;
        }

        public override void Visit (AstVisitor visitor)
        {
            var reduced = Reduce ();

            if (reduced != this) {
                reduced.Visit (visitor);
            } else {
                visitor.Accept (this);
            }
        }

        public override void VisitChildren (AstVisitor visitor)
        {
            if (Reduce () == this) {
                Left.Visit (visitor);
                Right.Visit (visitor);
            }
        }

        public override AstNode Reduce ()
        {
            var left = Left.Reduce ();
            var right = Right.Reduce ();

            switch (Operation) {
            case BinaryOperation.Add:
                {
                    if (left is IntegerExpression && right is IntegerExpression) {
                        return new IntegerExpression (right.Location,
                            ((IntegerExpression)left).Value + ((IntegerExpression)right).Value
                        );
                    }
                    if (left is BigIntegerExpression && right is BigIntegerExpression) {
                        return new BigIntegerExpression (right.Location,
                            ((BigIntegerExpression)left).Value + ((BigIntegerExpression)right).Value);
                    }
                    break;
                }
            case BinaryOperation.Sub:
                {
                    if (left is IntegerExpression && right is IntegerExpression) {
                        return new IntegerExpression (right.Location,
                            ((IntegerExpression)left).Value - ((IntegerExpression)right).Value
                        );
                    }
                    if (left is BigIntegerExpression && right is BigIntegerExpression) {
                        return new BigIntegerExpression (right.Location,
                            ((BigIntegerExpression)left).Value - ((BigIntegerExpression)right).Value);
                    }
                    break;
                }
            case BinaryOperation.Mul:
                {
                    if (left is IntegerExpression && right is IntegerExpression) {
                        return new IntegerExpression (right.Location,
                            ((IntegerExpression)left).Value * ((IntegerExpression)right).Value
                        );
                    }
                    if (left is BigIntegerExpression && right is BigIntegerExpression) {
                        return new BigIntegerExpression (right.Location,
                            ((BigIntegerExpression)left).Value * ((BigIntegerExpression)right).Value);
                    }
                    break;
                }
            case BinaryOperation.Div:
                {
                    if (left is IntegerExpression && right is IntegerExpression) {
                        return new IntegerExpression (right.Location,
                            ((IntegerExpression)left).Value / ((IntegerExpression)right).Value
                        );
                    }
                    if (left is BigIntegerExpression && right is BigIntegerExpression) {
                        return new BigIntegerExpression (right.Location,
                            ((BigIntegerExpression)left).Value / ((BigIntegerExpression)right).Value);
                    }
                    break;
                }
            
            case BinaryOperation.LeftShift:
                {
                    if (left is IntegerExpression && right is IntegerExpression) {
                        return new IntegerExpression (right.Location,
                            ((IntegerExpression)left).Value << (int)((IntegerExpression)right).Value
                        );
                    }
                    break;
                }
            case BinaryOperation.RightShift:
                {
                    if (left is IntegerExpression && right is IntegerExpression) {
                        return new IntegerExpression (right.Location, 
                            ((IntegerExpression)left).Value >> (int)((IntegerExpression)right).Value
                        );
                    }
                    break;
                }
            case BinaryOperation.Equals:
                {
                    if (left is IntegerExpression && right is IntegerExpression) {
                        bool res = ((IntegerExpression)left).Value == ((IntegerExpression)right).Value;
                        return res ? (AstNode)new TrueExpression (left.Location) : (AstNode)new FalseExpression (right.Location); 
                    }
                    if (left is BigIntegerExpression && right is BigIntegerExpression) {
                        bool res = ((BigIntegerExpression)left).Value == ((BigIntegerExpression)right).Value;
                        return res ? (AstNode)new TrueExpression (left.Location) : (AstNode)new FalseExpression (right.Location);
                    }
                    break;
                }
            case BinaryOperation.NotEquals:
                {
                    if (left is IntegerExpression && right is IntegerExpression) {
                        bool res = ((IntegerExpression)left).Value != ((IntegerExpression)right).Value;
                        return res ? (AstNode)new TrueExpression (left.Location) : (AstNode)new FalseExpression (right.Location); 
                    }
                    if (left is BigIntegerExpression && right is BigIntegerExpression) {
                        bool res = ((BigIntegerExpression)left).Value != ((BigIntegerExpression)right).Value;
                        return res ? (AstNode)new TrueExpression (left.Location) : (AstNode)new FalseExpression (right.Location);
                    }
                    break;
                }
            case BinaryOperation.GreaterThan:
                {
                    if (left is IntegerExpression && right is IntegerExpression) {
                        bool res = ((IntegerExpression)left).Value > ((IntegerExpression)right).Value;
                        return res ? (AstNode)new TrueExpression (left.Location) : (AstNode)new FalseExpression (right.Location); 
                    }
                    if (left is BigIntegerExpression && right is BigIntegerExpression) {
                        bool res = ((BigIntegerExpression)left).Value > ((BigIntegerExpression)right).Value;
                        return res ? (AstNode)new TrueExpression (left.Location) : (AstNode)new FalseExpression (right.Location);
                    }
                    break;
                }
            case BinaryOperation.GreaterThanOrEqu:
                {
                    if (left is IntegerExpression && right is IntegerExpression) {
                        bool res = ((IntegerExpression)left).Value >= ((IntegerExpression)right).Value;
                        return res ? (AstNode)new TrueExpression (left.Location) : (AstNode)new FalseExpression (right.Location); 
                    }
                    if (left is BigIntegerExpression && right is BigIntegerExpression) {
                        bool res = ((BigIntegerExpression)left).Value >= ((BigIntegerExpression)right).Value;
                        return res ? (AstNode)new TrueExpression (left.Location) : (AstNode)new FalseExpression (right.Location);
                    }
                    break;
                }
            case BinaryOperation.LessThan:
                {
                    if (left is IntegerExpression && right is IntegerExpression) {
                        bool res = ((IntegerExpression)left).Value < ((IntegerExpression)right).Value;
                        return res ? (AstNode)new TrueExpression (left.Location) : (AstNode)new FalseExpression (right.Location);  
                    }
                    if (left is BigIntegerExpression && right is BigIntegerExpression) {
                        bool res = ((BigIntegerExpression)left).Value < ((BigIntegerExpression)right).Value;
                        return res ? (AstNode)new TrueExpression (left.Location) : (AstNode)new FalseExpression (right.Location);
                    }
                    break;
                }
            case BinaryOperation.LessThanOrEqu:
                {
                    if (left is IntegerExpression && right is IntegerExpression) {
                        bool res = ((IntegerExpression)left).Value <= ((IntegerExpression)right).Value;
                        return res ? (AstNode)new TrueExpression (left.Location) : (AstNode)new FalseExpression (right.Location); 
                    }
                    if (left is BigIntegerExpression && right is BigIntegerExpression) {
                        bool res = ((BigIntegerExpression)left).Value <= ((BigIntegerExpression)right).Value;
                        return res ? (AstNode)new TrueExpression (left.Location) : (AstNode)new FalseExpression (right.Location);
                    }
                    break;
                }
            }
            return this;
        }
    }
}


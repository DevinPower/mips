using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Lexer.AST
{
    public class ASTExpression
    {
        public Node<ASTExpression> TreeRepresentation { get; private set; }
        public virtual bool SkipGeneration { get { return false; } }

        public void SetTreeRepresentation(Node<ASTExpression> TreeNode)
        {
            TreeRepresentation = TreeNode;
        }

        public override string ToString()
        {
            return "BASECLASS";
        }

        public virtual IntermediaryCodeMeta GenerateCode(CompilationMeta MetaData)
        {
            return new IntermediaryCodeMeta(new string[1] { $";Forgot to override GenerateCode function ({this.GetType()})" }, true);
        }
    }

    internal class Expression : ASTExpression
    {
        public override string ToString()
        {
            return $"E:{GetHashCode().ToString()}";
        }
    }

    internal class Operand : Expression
    {
    }

    public enum LiteralTypes { NUMBER, STRING, VECTOR2, VECTOR3, TRUE, FALSE, NULL }
    internal class Literal : Operand
    {
        LiteralTypes Type;
        object Value;
        public override bool SkipGeneration { get { return true; } }

        public Literal(LiteralTypes Type, object value)
        {
            this.Type = Type;
            Value = value;
        }

        public object GetValue()
        {
            return Value;
        }

        public override string ToString()
        {
            return $"'{Value.ToString()}'";
        }
    }

    public enum OperatorTypes { ADD, SUBTRACT, MULTIPLY, DIVIDE, LESSTHAN, GREATERTHAN, EQUAL }
    internal class Operator : ASTExpression
    {
        OperatorTypes Type;
        public Operator(OperatorTypes Type)
        {
            this.Type = Type;
        }

        public override string ToString()
        {
            return new string[] { "+", "-", "*", "÷", "<", ">", "=="}[(int)Type];
        }

        public override IntermediaryCodeMeta GenerateCode(CompilationMeta MetaData)
        {
            return new IntermediaryCodeMeta(new string[1] { "MUL B, A" }, true );
        }
    }

    internal class Variable : Operand
    {
        public string Name { get; private set; }
        public string Type { get; private set; }
        public override bool SkipGeneration { get { return true; } }

        public Variable(string Name, string Type)
        {
            this.Name = Name;
            this.Type = Type;
        }

        public override string ToString()
        {
            return Type + ":" + Name;
        }
    }

    internal class ParanEnd : ASTExpression { }
    internal class CurlyEnd : ASTExpression { }

    internal class Assignment : ASTExpression
    {
        public Variable LHS { get; private set; }
        public Expression RHS { get; private set; }
        public Assignment(Variable LHS, Expression RHS)
        {
            this.LHS = LHS;
            this.RHS = RHS;
        }

        public override string ToString()
        {
            return "=";
        }

        public override IntermediaryCodeMeta GenerateCode(CompilationMeta MetaData)
        {
            if (RHS is Literal value)
                return new IntermediaryCodeMeta(
                    new string[1] { $"Ori {MetaData.LookupVariable(((Variable)LHS).Name)}, {(value.GetValue())}" },
                    false);
            else
                return base.GenerateCode(MetaData);
        }
    }

    internal class WhileLoop : ASTExpression
    {
        Expression Condition;
        Expression Body;

        public WhileLoop(Expression Condition, Expression Body)
        {
            this.Condition = Condition;
            this.Body = Body;
        }

        public override string ToString()
        {
            return "loop->" + Condition.ToString() + "," + Body.ToString();
        }
    }

    internal class BinaryOperation : Expression
    {
        Expression LHS;
        Operator Operator;
        Expression RHS;

        public BinaryOperation(Expression LHS, Operator Operator, Expression RHS)
        {
            this.LHS = LHS;
            this.Operator = Operator;
            this.RHS = RHS;
        }

        public override string ToString()
        {
            return LHS.ToString() + Operator.ToString() + RHS.ToString();
        }

        public override IntermediaryCodeMeta GenerateCode(CompilationMeta MetaData)
        {
            string LHSLoad = "";
            int leftRegister = -1;
            if (LHS is Variable lvalue)
            {
                leftRegister = MetaData.GetTemporaryRegister(MetaData.LookupVariable(lvalue.Name));
                LHSLoad = $"Li $t{leftRegister}, {lvalue.Name}";
            }
            else
            {
                leftRegister = MetaData.GetTemporaryRegister(-1);
                LHSLoad = $"Ori $t{leftRegister}, {(LHS as Literal).GetValue()}";
            }

            string RHSLoad = "";
            int rightRegister = -1;
            if (RHS is Variable rvalue)
            {
                rightRegister = MetaData.GetTemporaryRegister(MetaData.LookupVariable(rvalue.Name));
                RHSLoad = $"Li $t{rightRegister}, {rvalue.Name}";
            }
            else
            {
                rightRegister = MetaData.GetTemporaryRegister(-1);
                RHSLoad = $"Ori $t{rightRegister}, {(RHS as Literal).GetValue()}";
            }

            return new IntermediaryCodeMeta(new string[3] { LHSLoad, RHSLoad, $"MUL $t{leftRegister}, $t{rightRegister}" }, true );
        }
    }
}

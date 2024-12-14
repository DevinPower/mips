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

        public void SetTreeRepresentation(Node<ASTExpression> TreeNode)
        {
            TreeRepresentation = TreeNode;
        }

        public override string ToString()
        {
            return "BASECLASS";
        }

        public virtual string[] GenerateCode(CompilationMeta MetaData)
        {
            return new string[1] { ";Forgot to override GenerateCode function" };
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

        public Literal(LiteralTypes Type, object value)
        {
            this.Type = Type;
            Value = value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override string[] GenerateCode(CompilationMeta MetaData)
        {
            return base.GenerateCode(MetaData);
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
    }

    internal class Variable : Operand
    {
        public string Name { get; private set; }
        public string Type { get; private set; }

        public Variable(string Name, string Type)
        {
            this.Name = Name;
            this.Type = Type;
        }

        public override string ToString()
        {
            return Type + ":" + Name;
        }

        public override string[] GenerateCode(CompilationMeta MetaData)
        {
            return new string[1] { $"LB $a0, {MetaData.LookupVariable(Name)}" };
        }
    }

    internal class ParanEnd : ASTExpression { }
    internal class CurlyEnd : ASTExpression { }

    internal class Assignment : ASTExpression
    {
        public Operand LHS { get; private set; }
        public Operand RHS { get; private set; }
        public Assignment(Operand LHS, Operand RHS)
        {
            this.LHS = LHS;
            this.RHS = RHS;
        }

        public override string ToString()
        {
            return "=";
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
    }
}

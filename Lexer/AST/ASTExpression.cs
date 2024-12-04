using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexer.AST
{
    internal class ASTExpression
    {
        public Node<ASTExpression> TreeRepresentation { get; private set; }

        public void SetTreeRepresentation(Node<ASTExpression> TreeNode)
        {
            TreeRepresentation = TreeNode;
        }
    }

    internal class Expression : ASTExpression
    {
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
    }

    public enum OperatorTypes { ADD, SUBTRACT }
    internal class Operator : ASTExpression
    {
        OperatorTypes Type;
        public Operator(OperatorTypes Type)
        {
            this.Type = Type;
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
    }

    internal class Assignment : ASTExpression
    {
        public Operand LHS { get; private set; }
        public Operand RHS { get; private set; }
        public Assignment(Operand LHS, Operand RHS)
        {
            this.LHS = LHS;
            this.RHS = RHS;
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
    }
}

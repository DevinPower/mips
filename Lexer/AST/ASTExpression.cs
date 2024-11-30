using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexer.AST
{
    internal class ASTExpression
    {
    }

    internal class Expression : ASTExpression
    {

    }

    public enum LiteralTypes { NUMBER, STRING, VECTOR2, VECTOR3, TRUE, FALSE, NULL }
    internal class Literal : Expression
    {
        LiteralTypes Type;

        public Literal(LiteralTypes Type)
        {
            this.Type = Type;
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

    internal class Variable : ASTExpression
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
        public Variable Variable { get; private set; }
        public Literal Literal { get; private set; }
        public Assignment(Variable Variable, Literal Literal)
        {
            this.Variable = Variable;
            this.Literal = Literal;
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

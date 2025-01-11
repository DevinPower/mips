namespace Lexer.AST
{
    public class Expression
    {

    }

    public class Literal : Expression
    {
        public int Value { get; private set; }
        public Literal(int Value)
        {
            this.Value = Value;
        }
    }

    public class Variable : Expression
    {
        public string Name { get; private set; }
        public Variable(string Name)
        {
            this.Name = Name;
        }
    }

    public class Function : Expression
    {
        public string Name { get; private set; }
        public Function(string Name)
        {
            this.Name = Name;
        }
    }

    public enum OperatorTypes
    {
        ASSIGN,
        ADD, SUBTRACT, MULTIPLY, DIVIDE, LESSTHAN, GREATERTHAN, EQUAL,
        ADDASSIGN, SUBTRACTASSIGN, MULTIPLYASSIGN, DIVIDEASSIGN
    }
    public class Operator : Expression
    {
        public Expression LHS { get; private set; }
        public OperatorTypes Type { get; set; }
        public Expression RHS { get; private set; }
        public Operator(Expression LHS, OperatorTypes Type, Expression RHS)
        {
            this.LHS = LHS;
            this.Type = Type;
            this.RHS = RHS;
        }
    }

    public class MachineCode : Expression
    {
        public string Code { get; private set; }
        public MachineCode(string Code)
        {
            this.Code = Code;
        }
    }
}
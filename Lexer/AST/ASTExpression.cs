using System.Xml.Linq;

namespace Lexer.AST
{
    public class RegisterResult
    {
        public string Register { get; private set; }

        public RegisterResult(string Register)
        {
            int registerStart = 0;

            while (Register[registerStart] == '$')
                registerStart++;

            this.Register = Register.Substring(registerStart);
        }

        public override string ToString()
        {
            return $"${Register}";
        }
    }

    public class Expression
    {
        public virtual RegisterResult GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            return null;
        }
    }

    public class Literal : Expression
    {

    }

    public class IntLiteral : Literal
    {
        public int Value { get; private set; }
        public IntLiteral(int Value)
        {
            this.Value = Value;
        }

        public override RegisterResult GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            RegisterResult ResultRegister = new RegisterResult($"t{ScopeMeta.GetTempRegister()}");
            Code.Add($"Li {ResultRegister}, {Value}");
            return ResultRegister;
        }
    }

    public class StringLiteral : Literal
    {
        public string Value { get; private set; }
        public StringLiteral(string Value)
        {
            this.Value = Value;
        }

        public override RegisterResult GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            RegisterResult ResultRegister = new RegisterResult($"t{ScopeMeta.GetTempRegister()}");
            Code.Add($"La {ResultRegister}, {Value}");
            return ResultRegister;
        }
    }

    public class Conditional : Expression
    {
        public Expression Condition { get; private set; }
        public ScriptBlock Body { get; private set; }

        public Conditional(Expression Condition, ScriptBlock Body)
        {
            this.Condition = Condition;
            this.Body = Body;
        }

        public override RegisterResult GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            string EndGuid = System.Guid.NewGuid().ToString().Replace("-", "");

            var conditionRegister = Condition.GenerateCode(ScopeMeta, Code);

            Code.Add($"Beq $zero, {conditionRegister}, {EndGuid}");
            var resultRegister = Body.GenerateCode(ScopeMeta, Code);
            Code.Add($"{EndGuid}:");

            return resultRegister;
        }
    }

    public class WhileLoop : Expression
    {
        public Expression Condition { get; private set; }
        public ScriptBlock Body { get; private set; }

        public WhileLoop(Expression Condition, ScriptBlock Body)
        {
            this.Condition = Condition;
            this.Body = Body;
        }

        public override RegisterResult GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            string StartGuid = System.Guid.NewGuid().ToString().Replace("-", "");
            string EndGuid = System.Guid.NewGuid().ToString().Replace("-", "");

            Code.Add($"{StartGuid}:");
            var conditionRegister = Condition.GenerateCode(ScopeMeta, Code);

            Code.Add($"Beq $zero, {conditionRegister}, {EndGuid}");
            var resultRegister = Body.GenerateCode(ScopeMeta, Code);
            Code.Add($"J {StartGuid}");
            Code.Add($"{EndGuid}:");

            return resultRegister;
        }
    }

    public class Variable : Expression
    {
        public string Name { get; private set; }
        public Variable(string Name)
        {
            this.Name = Name;
        }

        public override RegisterResult GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            int ArgumentPosition = ScopeMeta.GetArgumentPosition(Name);
            if (ArgumentPosition == -1) 
            {
                RegisterResult ResultRegister = new RegisterResult($"$t{ScopeMeta.GetTempRegister()}");
                Code.Add($"LB {ResultRegister}, {Name}(0)");
                return ResultRegister;
            }
            else
            {
                return new RegisterResult($"a{ArgumentPosition}");
            }
        }
    }

    public class FunctionCall : Expression
    {
        public string FunctionName { get; set; }
        public List<Expression> Arguments { get; set; }
        public FunctionCall(string FunctionName, List<Expression> Arguments)
        {
            this.FunctionName = FunctionName;
            this.Arguments = Arguments;
        }

        public override RegisterResult GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            RegisterResult[] ArgumentRegisters = Arguments.Select((x) => x.GenerateCode(ScopeMeta, Code)).ToArray();

            for (int i = 0; i < ArgumentRegisters.Length; i++)
                Code.Add($"Move $a{i}, {ArgumentRegisters[i].ToString()}");

            Code.Add($"Jal {FunctionName}");

            return new RegisterResult("v0");
        }
    }

    public class FunctionDefinition : Expression
    {
        public string Name { get; private set; }
        public ScriptBlock ScriptBlock { get; private set; }
        public FunctionDefinition(string Name, ScriptBlock ScriptBlock)
        {
            this.Name = Name;
            this.ScriptBlock = ScriptBlock;
        }

        public override RegisterResult GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            string EndGuid = System.Guid.NewGuid().ToString().Replace("-", "");

            Code.Add($"J {EndGuid}");
            Code.Add($"{Name}:");
            var resultRegister = ScriptBlock.GenerateCode(ScopeMeta, Code);
            Code.Add($"Jr $ra");
            Code.Add($"{EndGuid}:");
            return resultRegister;
        }
    }

    public class ReturnStatement : Expression
    {
        public Expression ReturnValue { get; private set; }
        public ReturnStatement(Expression ReturnValue)
        {
            this.ReturnValue = ReturnValue;
        }

        public override RegisterResult GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            var resultRegister = ReturnValue.GenerateCode(ScopeMeta, Code);

            Code.Add($"Move $v0, {resultRegister}");
            Code.Add($"Jr $ra");

            return new RegisterResult("v0");
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
        public bool SelfAssign { get;private set; }
        public OperatorTypes Type { get; set; }
        public Expression RHS { get; private set; }
        public Operator(Expression LHS, OperatorTypes Type, Expression RHS, bool SelfAssign)
        {
            this.LHS = LHS;
            this.Type = Type;
            this.RHS = RHS;
            this.SelfAssign = SelfAssign;
        }

        public string GetCommand(RegisterResult Result, RegisterResult Op1, RegisterResult Op2)
        {
            return new string[] { $";assignment",
                $"Add {Result}, {Op1}, {Op2}", $"Sub {Result}, {Op1}, {Op2}",
                $"Mult {Result}, {Op1}, {Op2}", $"Div {Result}, {Op1}, {Op2}",
                $"Slt {Result}, {Op1}, {Op2}", $"Slt {Result}, {Op2}, {Op1}",
                $"Seq {Result}, {Op1}, {Op2}", $"Add {Op1}, {Op1}, {Op2}",
                $"Sub {Op1}, {Op1}, {Op2}", $"Mul {Op1}, {Op1}, {Op2}",
                $"Div {Op1}, {Op1}, {Op2}"}[(int)Type];
        }

        public override RegisterResult GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            var leftRegister = LHS.GenerateCode(ScopeMeta, Code);
            var rightRegister = RHS.GenerateCode(ScopeMeta, Code);

            RegisterResult resultRegister = new RegisterResult($"t{ScopeMeta.GetTempRegister()}");

            Code.Add(GetCommand(resultRegister, leftRegister, rightRegister));

            if (SelfAssign)
                Code.Add($"SB {leftRegister}, {((Variable)LHS).Name}(0)");

            if (Type == OperatorTypes.MULTIPLY || Type == OperatorTypes.DIVIDE
                || Type == OperatorTypes.MULTIPLYASSIGN || Type == OperatorTypes.DIVIDEASSIGN)
                Code.Add($"Mflo {resultRegister}");

            ScopeMeta.FreeTempRegister(leftRegister);
            ScopeMeta.FreeTempRegister(rightRegister);

            return resultRegister;
        }
    }

    public class Assignment : Expression
    {
        public Variable Variable { get; private set; }
        public Expression RHS { get; private set; }
        public Assignment(Variable Variable, Expression RHS)
        {
            this.Variable = Variable;
            this.RHS = RHS;
        }

        public override RegisterResult GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            RegisterResult LeftRegister = RHS.GenerateCode(ScopeMeta, Code);

            Code.Add($"SB {LeftRegister}, {Variable.Name}(0)");

            return null;
        }
    }

    public class MachineCode : Expression
    {
        public string Code { get; private set; }
        public MachineCode(string Code)
        {
            this.Code = Code;
        }

        public override RegisterResult GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            Code.Add(this.Code);
            return null;
        }
    }

    public class ScriptBlock : Expression
    {
        public List<Expression> Expressions { get; private set; }
        public CompilationMeta ScopedMeta { get; private set; }
        public ScriptBlock(List<Expression> Expressions, CompilationMeta ScopedMeta)
        {
            this.Expressions = Expressions;
            this.ScopedMeta = ScopedMeta;
        }

        public override RegisterResult GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            foreach(Expression expression in Expressions)
            {
                expression.GenerateCode(ScopedMeta, Code);
            }

            return null;
        }
    }
}
namespace Lexer.AST
{
    public class Expression
    {
        public virtual int GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            return -1;
        }
    }

    public class Literal : Expression
    {
        public int Value { get; private set; }
        public Literal(int Value)
        {
            this.Value = Value;
        }

        public override int GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            int ResultRegister = ScopeMeta.GetTempRegister();
            Code.Add($"Li $t{ResultRegister}, {Value}");
            return ResultRegister;
        }
    }

    public class Variable : Expression
    {
        public string Name { get; private set; }
        public Variable(string Name)
        {
            this.Name = Name;
        }

        public override int GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            int ResultRegister = ScopeMeta.GetTempRegister();

            Code.Add($"Li $t{ResultRegister}, ${Name}");

            return ResultRegister;
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

        public override int GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            int[] ArgumentRegisters = Arguments.Select((x) => x.GenerateCode(ScopeMeta, Code)).ToArray();

            for (int i = 0; i < ArgumentRegisters.Length; i++)
            {
                int argumentRegister = ArgumentRegisters[i];
                Code.Add($"Move $a{i}, $t{argumentRegister}");
            }

            Code.Add($"Jalr {FunctionName}");

            int resultRegister = ScopeMeta.GetTempRegister();
            Code.Add($"Li $t{resultRegister}, $v0");

            return resultRegister;
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

        public override int GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            string EndGuid = System.Guid.NewGuid().ToString().Replace("-", "");

            Code.Add($"J {EndGuid}");
            Code.Add($"{Name}:");
            int resultRegister = ScriptBlock.GenerateCode(ScopeMeta, Code);
            Code.Add($"{EndGuid}");
            return resultRegister;
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

        public string GetCommand(int Result, int Op1, int Op2)
        {
            return new string[] { $";assignment",
                $"Add $t{Result}, $t{Op1}, $t{Op2}", $"Sub $t{Result}, $t{Op1}, $t{Op2}",
                $"Mul $t{Result}, $t{Op1}, $t{Op2}", $"Div $t{Result}, $t{Op1}, $t{Op2}",
                $"Slt $t{Result}, $t{Op1}, $t{Op2}", $"Bgt $t{Result}, $t{Op1}, $t{Op2}",
                $"Beq $t{Result}, $t{Op1}, $t{Op2}", $"Add $t{Op1}, $t{Op1}, $t{Op2}",
                $"Sub $t{Op1}, $t{Op1}, $t{Op2}", $"Mul $t{Op1}, $t{Op1}, $t{Op2}",
                $"Div $t{Op1}, $t{Op1}, $t{Op2}"}[(int)Type];
        }

        public override int GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            int leftRegister = LHS.GenerateCode(ScopeMeta, Code);
            int rightRegister = RHS.GenerateCode(ScopeMeta, Code);

            int resultRegister = ScopeMeta.GetTempRegister();

            Code.Add(GetCommand(resultRegister, leftRegister, rightRegister));

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

        public override int GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            int RightRegister = RHS.GenerateCode(ScopeMeta, Code);

            Code.Add($"SB $t{RightRegister}, {Variable.Name}");

            return -1;
        }
    }

    public class MachineCode : Expression
    {
        public string Code { get; private set; }
        public MachineCode(string Code)
        {
            this.Code = Code;
        }

        public override int GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            Code.Add(this.Code);
            return -1;
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

        public override int GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            foreach(Expression expression in Expressions)
            {
                expression.GenerateCode(ScopedMeta, Code);
            }

            return -1;
        }
    }
}
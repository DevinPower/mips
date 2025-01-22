using System.Linq.Expressions;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Lexer.AST
{
    public class FunctionCallRegisterState
    {
        bool[] aRegisters = new bool[4];
        bool[] tRegisters = new bool[8];
        int spaceSum = 1;

        public FunctionCallRegisterState(FunctionCall SubFunction, CompilationMeta MetaScope)
        {

            for (int i = 0; i < SubFunction.Arguments.Count; i++)
            {
                aRegisters[i] = true;
                spaceSum++;
            }

            for (int i = 0; i < 8; i++)
            {
                if (!MetaScope.TempRegisters[i])
                    continue;

                tRegisters[i] = true;
                spaceSum++;
            }
        }

        public void SaveState(List<string> Code)
        {
            int backwardsCount = spaceSum;
            Code.Add($"Ori $t9, $zero, {backwardsCount}");
            Code.Add($"Sub $sp, $sp, $t9");
            for (int i = 0; i < 4; i++)
            {
                if (aRegisters[i])
                    Code.Add($"SB $a{i}, {backwardsCount--}($sp)");
            }

            for (int i = 0; i < 8; i++)
            {
                if (tRegisters[i])
                    Code.Add($"SB $t{i}, {backwardsCount--}($sp)");
            }

            //Code.Add($"SB $v0, $sp({backwardsCount--})");
            //Code.Add($"SB $v1, $sp({backwardsCount--})");
            Code.Add($"SB $ra, {backwardsCount--}($sp)");
        }

        public void LoadState(List<string> Code)
        {
            int count = spaceSum;
            for (int i = 0; i < 4; i++)
            {
                if (aRegisters[i])
                    Code.Add($"LB $a{i}, {count--}($sp)");
            }

            for (int i = 0; i < 8; i++)
            {
                if (tRegisters[i])
                    Code.Add($"LB $t{i}, {count--}($sp)");
            }

            //Code.Add($"LB $v0, $sp({count--})");
            //Code.Add($"LB $v1, $sp({count--})");
            Code.Add($"LB $ra, {count--}($sp)");

            Code.Add($"Ori $t9, $zero, {spaceSum}");
            Code.Add($"Add $sp, $sp, $t9");
        }
    }

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
        public List<Expression> Conditions { get; private set; }
        public ScriptBlock Body { get; private set; }
        public ScriptBlock ElseBody { get; private set; }

        public Conditional(List<Expression> Conditions, ScriptBlock Body, ScriptBlock ElseBody)
        {
            this.Conditions = Conditions;
            this.Body = Body;
            this.ElseBody = ElseBody;
        }

        public override RegisterResult GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            string EndGuid = System.Guid.NewGuid().ToString().Replace("-", "");
            string EndElseGuid = System.Guid.NewGuid().ToString().Replace("-", "");
            List<RegisterResult> conditionRegisters = new List<RegisterResult>();

            foreach (var Condition in Conditions)
            {
                conditionRegisters.Add(Condition.GenerateCode(ScopeMeta, Code));
            }

            foreach (var conditionRegister in conditionRegisters)
                Code.Add($"Beq $zero, {conditionRegister}, {EndGuid}");

            var resultRegister = Body.GenerateCode(ScopeMeta, Code);
            if (ElseBody != null)
                Code.Add($"J {EndElseGuid}");
            
            Code.Add($"{EndGuid}:");

            Body.FreeRegisters(ScopeMeta);

            if (ElseBody != null)
            {
                resultRegister = ElseBody.GenerateCode(ScopeMeta, Code);
            }
            Code.Add($"{EndElseGuid}:");

            foreach (var conditionRegister in conditionRegisters)
            {
                ScopeMeta.FreeTempRegister(conditionRegister);
            }

            return resultRegister;
        }
    }

    public class WhileLoop : Expression
    {
        public List<Expression> Conditions { get; private set; }
        public ScriptBlock Body { get; private set; }

        public WhileLoop(List<Expression> Conditions, ScriptBlock Body)
        {
            this.Conditions = Conditions;
            this.Body = Body;
        }

        public override RegisterResult GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            string StartGuid = System.Guid.NewGuid().ToString().Replace("-", "");
            string EndGuid = System.Guid.NewGuid().ToString().Replace("-", "");

            Code.Add($"{StartGuid}:");
            List<RegisterResult> conditionRegisters = new List<RegisterResult>();

            foreach (var Condition in Conditions)
            {
                conditionRegisters.Add(Condition.GenerateCode(ScopeMeta, Code));
            }

            foreach (var conditionRegister in conditionRegisters)
                Code.Add($"Beq $zero, {conditionRegister}, {EndGuid}");

            var resultRegister = Body.GenerateCode(ScopeMeta, Code);
            Code.Add($"J {StartGuid}");
            Code.Add($"{EndGuid}:");

            Body.FreeRegisters(ScopeMeta);

            return resultRegister;
        }
    }

    public class Variable : Expression
    {
        public string Name { get; private set; }
        public Expression Offset { get; private set; }

        public Variable(string Name)
        {
            this.Name = Name;
        }

        public RegisterResult GetOffsetRegister(CompilationMeta ScopeMeta, List<string> Code)
        {
            return Offset.GenerateCode(ScopeMeta, Code);
        }

        public void SetOffset(Expression Offset)
        {
            this.Offset = Offset;
        }

        public override RegisterResult GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            int ArgumentPosition = ScopeMeta.GetArgumentPosition(Name, true);
            if (ArgumentPosition == -1) 
            {
                RegisterResult ResultRegister = new RegisterResult($"$t{ScopeMeta.GetTempRegister()}");

                string offsetRegister = "$zero";
                RegisterResult offsetResult = null;

                if (Offset != null)
                {
                    offsetResult = Offset.GenerateCode(ScopeMeta, Code);
                    offsetRegister = offsetResult.ToString();

                    if (ScopeMeta.GetVariable(Name).Type == "string")
                    {
                        RegisterResult StringAddress = new RegisterResult($"$t{ScopeMeta.GetTempRegister()}");

                        Code.Add($"LB {StringAddress}, {Name}($zero)");
                        Code.Add($"Add {StringAddress}, {StringAddress}, {offsetRegister}");
                        Code.Add($"LB {ResultRegister}, 0({StringAddress})");

                        ScopeMeta.FreeTempRegister(offsetResult);
                        ScopeMeta.FreeTempRegister(StringAddress);
                        return ResultRegister;
                    }
                }

                ScopeMeta.FreeTempRegister(offsetResult);

                Code.Add($"LB {ResultRegister}, {Name}({offsetRegister})");
                return ResultRegister;
            }
            else
            {
                if (Offset != null)
                {
                    RegisterResult offsetResult = Offset.GenerateCode(ScopeMeta, Code);
                    RegisterResult resultRegister = new RegisterResult($"$t{ScopeMeta.GetTempRegister()}");

                    if (ScopeMeta.GetArgument(Name, true).Type == "string")
                    {
                        RegisterResult ResultRegister = new RegisterResult($"$t{ScopeMeta.GetTempRegister()}");
                        RegisterResult StringAddress = new RegisterResult($"$t{ScopeMeta.GetTempRegister()}");

                        Code.Add($"Move {StringAddress}, $a{ArgumentPosition}");
                        Code.Add($"Add {StringAddress}, {StringAddress}, {offsetResult}");
                        Code.Add($"LB {ResultRegister}, 0({StringAddress})");

                        ScopeMeta.FreeTempRegister(offsetResult);
                        ScopeMeta.FreeTempRegister(StringAddress);
                        return ResultRegister;
                    }

                    Code.Add($"Add {resultRegister}, $a{ArgumentPosition}, {offsetResult}");

                    ScopeMeta.FreeTempRegister(offsetResult);

                    return resultRegister;
                }

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

            FunctionCallRegisterState registerState = new FunctionCallRegisterState(this, ScopeMeta);
            registerState.SaveState(Code);

            for (int i = 0; i < ArgumentRegisters.Length; i++)
                Code.Add($"Move $a{i}, {ArgumentRegisters[i].ToString()}");

            Code.Add($"Jal {FunctionName}");

            registerState.LoadState(Code);

            foreach (var register in ArgumentRegisters)
            {
                ScopeMeta.FreeTempRegister(register);
            }

            RegisterResult resultRegister = new RegisterResult($"t{ScopeMeta.GetTempRegister()}");
            Code.Add($"Move {resultRegister}, $v0");

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

        public override RegisterResult GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            string EndGuid = System.Guid.NewGuid().ToString().Replace("-", "");

            Code.Add($"J {EndGuid}");
            Code.Add($"{Name}:");
            var resultRegister = ScriptBlock.GenerateCode(ScopeMeta, Code);
            Code.Add($"Jr $ra");
            Code.Add($"{EndGuid}:");

            ScriptBlock.FreeRegisters(ScopeMeta);

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

            ScopeMeta.FreeTempRegister(resultRegister);

            return new RegisterResult("v0");
        }
    }

    public enum OperatorTypes
    {
        ASSIGN,
        ADD, SUBTRACT, MULTIPLY, DIVIDE, LESSTHAN, GREATERTHAN, EQUAL,
        ADDASSIGN, SUBTRACTASSIGN, MULTIPLYASSIGN, DIVIDEASSIGN,
        LOGICALOR, LOGICALAND
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
                $"Sub {Op1}, {Op1}, {Op2}", $"Mult {Op1}, {Op1}, {Op2}",
                $"Div {Op1}, {Op1}, {Op2}",
                $"Or {Result}, {Op1}, {Op2}", $"And {Result}, {Op1}, {Op2}"}[(int)Type];
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

            string offsetRegister = "$zero";

            if (Variable.Offset != null)
            {
                RegisterResult offsetResult = Variable.Offset.GenerateCode(ScopeMeta, Code);
                offsetRegister = offsetResult.ToString();
                ScopeMeta.FreeTempRegister(offsetResult);
            }

            Code.Add($"SB {LeftRegister}, {Variable.Name}({offsetRegister})");

            ScopeMeta.FreeTempRegister(LeftRegister);

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
        List<RegisterResult> _usedRegisters = new List<RegisterResult>();
        public ScriptBlock(List<Expression> Expressions, CompilationMeta ScopedMeta)
        {
            this.Expressions = Expressions;
            this.ScopedMeta = ScopedMeta;
        }

        public override RegisterResult GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            foreach(Expression expression in Expressions)
            {
                _usedRegisters.Add(expression.GenerateCode(ScopedMeta, Code));
            }

            return null;
        }

        public void FreeRegisters(CompilationMeta ScopeMeta)
        {
            foreach (var register in _usedRegisters)
            {
                ScopedMeta.FreeTempRegister(register);
            }
        }
    }
}
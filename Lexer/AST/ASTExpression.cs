using System.Linq.Expressions;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Lexer.AST
{
    public class FunctionCallRegisterState
    {
        bool[] aRegisters = new bool[4];
        bool[] tRegisters = new bool[8];
        int spaceSum = 0;

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
            Code.Add($"Ori $t9, $zero, {spaceSum + 1}");
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

            Code.Add($"LB $ra, {count--}($sp)");

            Code.Add($"Ori $t9, $zero, {spaceSum + 1}");
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

        public void ConvertToInt(CompilationMeta ScopeMeta, List<string> Code)
        {
            Code.Add($"Cvt.i.s {ToString()}, {ToString()}, $zero");
        }

        public void ConvertToFloat(CompilationMeta ScopeMeta, List<string> Code)
        {
            Code.Add($"Cvt.s.i {ToString()}, {ToString()}, $zero");
        }
    }

    public class Expression
    {
        public virtual RegisterResult GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            return null;
        }

        public virtual string InferType(CompilationMeta ScopeMeta)
        {
            return "unknown";
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

        public override string InferType(CompilationMeta ScopeMeta)
        {
            return "int";
        }
    }

    public class FloatLiteral : Literal
    {
        public float Value { get; private set; }
        public FloatLiteral(float Value)
        {
            this.Value = Value;
        }

        public override RegisterResult GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            RegisterResult ResultRegister = new RegisterResult($"t{ScopeMeta.GetTempRegister()}");
            Code.Add($"Li {ResultRegister}, {Helpers.FloatToInt(Value)}");
            return ResultRegister;
        }

        public override string InferType(CompilationMeta ScopeMeta)
        {
            return "float";
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

        public override string InferType(CompilationMeta ScopeMeta)
        {
            return "int";
        }
    }

    public class Conditional : Expression
    {
        public List<Expression> Conditions { get; private set; }
        public ScriptBlock Body { get; private set; }
        public ScriptBlock ElseBody { get; private set; }
        public Conditional ElseIf { get; private set; }

        public Conditional(List<Expression> Conditions, ScriptBlock Body, ScriptBlock ElseBody, Conditional ElseIf)
        {
            this.Conditions = Conditions;
            this.Body = Body;
            this.ElseBody = ElseBody;
            this.ElseIf = ElseIf;
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
            if (ElseBody != null || ElseIf != null)
                Code.Add($"J {EndElseGuid}");
            
            Code.Add($"{EndGuid}:");

            Body.FreeRegisters(ScopeMeta);

            if (ElseBody != null)
            {
                resultRegister = ElseBody.GenerateCode(ScopeMeta, Code);
            }

            if (ElseIf != null)
            {
                resultRegister = ElseIf.GenerateCode(ScopeMeta, Code);
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

    public class AddressPointer : Expression
    {
        public string Name { get; private set; }

        public AddressPointer(string Name)
        {
            this.Name = Name;
        }

        public override RegisterResult GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            RegisterResult ResultRegister = new RegisterResult($"$t{ScopeMeta.GetTempRegister()}");

            VariableMeta MetaData = ScopeMeta.GetVariable(Name);

            if (!MetaData.IsLocal)
            {
                Code.Add($"La {ResultRegister}, {Name}(0)");
            }
            else
            {
                Code.Add($"Move {ResultRegister}, $sp");
                Code.Add($"Addi {ResultRegister}, {ResultRegister}, {MetaData.GetStackOffset()}");
            }

            return ResultRegister;
        }

        public override string InferType(CompilationMeta ScopeMeta)
        {
            return "int";
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

        public bool HasOffset()
        {
            return Offset != null;
        }

        public RegisterResult GetOffsetRegister(CompilationMeta ScopeMeta, List<string> Code)
        {
            return Offset.GenerateCode(ScopeMeta, Code);
        }

        public string GetVariableType(CompilationMeta ScopeMeta)
        {
            int ArgumentPosition = ScopeMeta.GetArgumentPosition(Name, true);
            if (ArgumentPosition == -1)
            {
                return ScopeMeta.GetVariable(Name).Type;
            }
            else
            {
                return ScopeMeta.GetArgument(Name, true).Type;
            }
        }

        public void SetOffset(Expression Offset)
        {
            this.Offset = Offset;
        }

        bool VariableIsArrayOrString(CompilationMeta ScopeMeta, string VariableName)
        {
            return ScopeMeta.GetVariable(Name).Type == "string" || ScopeMeta.GetVariable(Name).IsArray;
        }

        bool ArgumentIsArrayOrString(CompilationMeta ScopeMeta, string VariableName, bool CanRecurse)
        {
            return ScopeMeta.GetArgument(Name, CanRecurse).Type == "string" || ScopeMeta.GetArgument(Name, CanRecurse).IsArray;
        }

        //TODO: Can we refactor?
        public override RegisterResult GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            int ArgumentPosition = ScopeMeta.GetArgumentPosition(Name, true);

            if (ArgumentPosition == -1) 
            {
                VariableMeta MetaData = ScopeMeta.GetVariable(Name);
                bool IsLocal = MetaData.IsLocal;
                RegisterResult ResultRegister = new RegisterResult($"$t{ScopeMeta.GetTempRegister()}");

                string offsetRegister = "0";
                RegisterResult offsetResult = null;

                if (Offset != null)
                {
                    offsetResult = Offset.GenerateCode(ScopeMeta, Code);
                    offsetRegister = offsetResult.ToString();

                    if (VariableIsArrayOrString(ScopeMeta, Name))
                    {
                        RegisterResult StringAddress = new RegisterResult($"$t{ScopeMeta.GetTempRegister()}");

                        if (!IsLocal)
                            Code.Add($"La {StringAddress}, {Name}(0)");
                        else
                            Code.Add($"Addi {StringAddress}, $sp, {MetaData.GetStackOffset()}");

                        Code.Add($"Add {StringAddress}, {StringAddress}, {offsetRegister}");

                        Code.Add($"LB {ResultRegister}, 0({StringAddress})");

                        ScopeMeta.FreeTempRegister(offsetResult);
                        ScopeMeta.FreeTempRegister(StringAddress);
                        return ResultRegister;
                    }
                }

                ScopeMeta.FreeTempRegister(offsetResult);

                if (!IsLocal)
                    Code.Add($"LB {ResultRegister}, {Name}({offsetRegister})");
                else
                    Code.Add($"LB {ResultRegister}, {MetaData.GetStackOffset()}($sp)");

                return ResultRegister;
            }
            else
            {
                //TODO: Should arguments be local..?
                if (Offset != null)
                {
                    RegisterResult offsetResult = Offset.GenerateCode(ScopeMeta, Code);
                    RegisterResult resultRegister = new RegisterResult($"$t{ScopeMeta.GetTempRegister()}");

                    if (ArgumentIsArrayOrString(ScopeMeta, Name, true))
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

                RegisterResult ResultRegisterPointer = new RegisterResult($"$t{ScopeMeta.GetTempRegister()}");
                Code.Add($"Move {ResultRegisterPointer}, $a{ArgumentPosition}");

                return ResultRegisterPointer;
            }
        }

        public override string InferType(CompilationMeta ScopeMeta)
        {
            return GetVariableType(ScopeMeta);
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
            {
                string FunctionArgumentType = ScopeMeta.GetFunction(FunctionName).ArgumentTypes[i];

                if (FunctionArgumentType == "float" && Arguments[i].InferType(ScopeMeta) == "int")
                    ArgumentRegisters[i].ConvertToFloat(ScopeMeta, Code);

                if (FunctionArgumentType == "int" && Arguments[i].InferType(ScopeMeta) == "float")
                    ArgumentRegisters[i].ConvertToInt(ScopeMeta, Code);

                Code.Add($"Move $a{i}, {ArgumentRegisters[i].ToString()}");
            }

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

        public override string InferType(CompilationMeta ScopeMeta)
        {
            return ScopeMeta.GetFunction(FunctionName).ReturnType;
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

        public void PrependName(string Pre)
        {
            Name = Pre + Name;
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

            ScopeMeta.ExitScope(Code);

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

        public string GetCommand(RegisterResult Result, RegisterResult Op1, RegisterResult Op2, bool Float)
        {
            string FloatCommand = Float ? ".s" : "";
            return new string[] { $";assignment, this code shouldn't be called 😬",
                $"Add{FloatCommand} {Result}, {Op1}, {Op2}", $"Sub{FloatCommand} {Result}, {Op1}, {Op2}",
                $"Mult{FloatCommand} {Result}, {Op1}, {Op2}", $"Div{FloatCommand} {Result}, {Op1}, {Op2}",
                $"Slt{FloatCommand} {Result}, {Op1}, {Op2}", $"Slt{FloatCommand} {Result}, {Op2}, {Op1}",
                $"Seq {Result}, {Op1}, {Op2}", $"Add{FloatCommand} {Op1}, {Op1}, {Op2}",
                $"Sub{FloatCommand} {Op1}, {Op1}, {Op2}", $"Mult{FloatCommand} {Op1}, {Op1}, {Op2}",
                $"Div{FloatCommand} {Op1}, {Op1}, {Op2}",
                $"Or {Result}, {Op1}, {Op2}", $"And {Result}, {Op1}, {Op2}"}[(int)Type];
        }

        public override RegisterResult GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            var leftRegister = LHS.GenerateCode(ScopeMeta, Code);
            var rightRegister = RHS.GenerateCode(ScopeMeta, Code);
            bool asfloat = false;

            //TODO: Refactor
            if (LHS.InferType(ScopeMeta) == "float" && RHS.InferType(ScopeMeta) == "int")
            {
                rightRegister.ConvertToFloat(ScopeMeta, Code);
                asfloat = true;
            }

            if (LHS.InferType(ScopeMeta) == "int" && RHS.InferType(ScopeMeta) == "float")
            {
                leftRegister.ConvertToFloat(ScopeMeta, Code);
                asfloat = true;
            }

            if (LHS.InferType(ScopeMeta) == "float" && RHS.InferType(ScopeMeta) == "float")
            {
                asfloat = true;
            }

            RegisterResult resultRegister = new RegisterResult($"t{ScopeMeta.GetTempRegister()}");

            Code.Add(GetCommand(resultRegister, leftRegister, rightRegister, asfloat));

            if (SelfAssign)
            {
                VariableMeta LHSMeta = ScopeMeta.GetVariable(((Variable)LHS).Name);
                if (!LHSMeta.IsLocal)
                    Code.Add($"SB {leftRegister}, {((Variable)LHS).Name}(0)");
                else
                    Code.Add($"SB {leftRegister}, {LHSMeta.GetStackOffset()}($sp)");
            }

            if (Type == OperatorTypes.MULTIPLY || Type == OperatorTypes.DIVIDE
                || Type == OperatorTypes.MULTIPLYASSIGN || Type == OperatorTypes.DIVIDEASSIGN)
                Code.Add($"Mflo {resultRegister}");

            ScopeMeta.FreeTempRegister(leftRegister);
            ScopeMeta.FreeTempRegister(rightRegister);

            return resultRegister;
        }

        public override string InferType(CompilationMeta ScopeMeta)
        {
            if (LHS.InferType(ScopeMeta) == "float" || RHS.InferType(ScopeMeta) == "float")
                return "float";

            return "int";
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

            string offsetRegister = "0";

            if (RHS.InferType(ScopeMeta) == "void")
                throw new Exception($"Cannot assign void to '{Variable.Name}'");

            VariableMeta MetaData = ScopeMeta.GetVariable(Variable.Name);

            RegisterResult offsetResult = null;

            if (Variable.Offset != null)
            {
                offsetResult = Variable.Offset.GenerateCode(ScopeMeta, Code);
                if (MetaData.IsLocal)
                {
                    Code.Add($"Add {offsetResult}, {offsetResult}, $sp");
                    Code.Add($"Addi {offsetResult}, {offsetResult}, {MetaData.GetStackOffset()}");
                }
                else
                {
                    offsetRegister = offsetResult.ToString();
                }

                ScopeMeta.FreeTempRegister(offsetResult);
            }

            if (RHS.InferType(ScopeMeta) == "int" && Variable.InferType(ScopeMeta) == "float")
                LeftRegister.ConvertToFloat(ScopeMeta, Code);

            if (RHS.InferType(ScopeMeta) == "float" && Variable.InferType(ScopeMeta) == "int")
                LeftRegister.ConvertToInt(ScopeMeta, Code);

            if (!MetaData.IsLocal)
            {
                Code.Add($"SB {LeftRegister}, {Variable.Name}({offsetRegister})");
            }
            else
            {
                if (Variable.Offset == null)
                {
                    Code.Add($"SB {LeftRegister}, {MetaData.GetStackOffset()}($sp)");
                }
                else
                {
                    Code.Add($"SB {LeftRegister}, 0({offsetResult})");
                }
            }

            ScopeMeta.FreeTempRegister(LeftRegister);

            return null;
        }

        public override string InferType(CompilationMeta ScopeMeta)
        {
            return Variable.InferType(ScopeMeta);
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
            ScopedMeta.EnterScope(Code);

            foreach(Expression expression in Expressions)
            {
                _usedRegisters.Add(expression.GenerateCode(ScopedMeta, Code));
            }

            ScopedMeta.ExitScope(Code);

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

    public class ClassDefinition : Expression
    {
        public string Name { get; private set; }
        CompilationMeta ScopedMeta;
        public List<FunctionDefinition> FunctionDefinitions { get; private set; }
        public List<Variable> VariableDefinitions { get; private set; }


        public ClassDefinition(CompilationMeta ScopedMeta, string Name, List<FunctionDefinition> FunctionDefinitions, List<Variable> VariableDefinitions)
        {
            this.Name = Name;
            this.FunctionDefinitions = FunctionDefinitions;
            this.VariableDefinitions = VariableDefinitions;
            this.ScopedMeta = ScopedMeta;
        }

        public override RegisterResult GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            ClassMeta classMeta = ScopeMeta.GetClass(Name);
            int DataSize = classMeta.GetClassDataSize();
            string EndGuid = System.Guid.NewGuid().ToString().Replace("-", "");

            Code.Add($"J {EndGuid}");
            Code.Add($"{Name}.Instantiate:");

            foreach (FunctionDefinition f in FunctionDefinitions)
            {
                ScopeMeta.FreeTempRegister(f.GenerateCode(ScopedMeta, Code));
            }

            RegisterResult resultRegister = Helpers.HeapAllocation(ScopedMeta, Code, DataSize);
            Code.Add($"Jr $ra");
            Code.Add($"{EndGuid}:");

            return resultRegister;
        }
    }

    public class ClassInstantiation : Expression
    {
        public string Name { get; private set; }

        public ClassInstantiation(string Name)
        {
            this.Name = Name;
        }

        public override RegisterResult GenerateCode(CompilationMeta ScopeMeta, List<string> Code)
        {
            FunctionCallRegisterState registerState = new FunctionCallRegisterState(new FunctionCall(Name, new List<Expression>()), ScopeMeta);
            registerState.SaveState(Code);
            Code.Add($"Jal {Name}.Instantiate");

            registerState.LoadState(Code);

            RegisterResult resultRegister = new RegisterResult($"t{ScopeMeta.GetTempRegister()}");
            Code.Add($"Move {resultRegister}, $v0");

            return resultRegister;
        }
    }
}
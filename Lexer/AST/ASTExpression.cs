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
        public virtual bool SkipChildGeneration { get {  return false; } }

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
            return $"E:{GetHashCode().ToString()} cc= {TreeRepresentation?.Children?.Count}";
        }

        public override IntermediaryCodeMeta GenerateCode(CompilationMeta MetaData)
        {
            if (TreeRepresentation?.Children == null)
                return base.GenerateCode(MetaData);

            List<IntermediaryCodeMeta> icm = TreeRepresentation.Children.Select((x) => x.Data.GenerateCode(MetaData)).ToList();
            return new IntermediaryCodeMeta(icm.SelectMany((x) => x.Code).ToArray(), false);
        }
    }

    internal class MachineCode : ASTExpression
    {
        string Code { get; set; }
        public MachineCode(string Code)
        {
            this.Code = Code;
        }

        public override IntermediaryCodeMeta GenerateCode(CompilationMeta MetaData)
        {
            return new IntermediaryCodeMeta(new string[1] { Code }, true);
        }

        public override string ToString()
        {
            return $"MC-[{Code}";
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

        public override IntermediaryCodeMeta GenerateCode(CompilationMeta MetaData)
        {
            return new IntermediaryCodeMeta(new string[0], false);
        }
    }

    public enum OperatorTypes { ADD, SUBTRACT, MULTIPLY, DIVIDE, LESSTHAN, GREATERTHAN, EQUAL,
                                ADDASSIGN, SUBTRACTASSIGN, MULTIPLYASSIGN, DIVIDEASSIGN }
    internal class Operator : ASTExpression
    {
        OperatorTypes Type;
        public Operator(OperatorTypes Type)
        {
            this.Type = Type;
        }

        public override string ToString()
        {
            return new string[] { "+", "-", "*", "÷", "<", ">", "==", 
                "+=", "-=", "*=", "÷=" }[(int)Type];
        }

        public string GetCommand(bool Immediate, string Result, string Op1, string Op2)
        {
            return new string[] { $"Add {Result}, {Op1}, {Op2}", $"Sub {Result}, {Op1}, {Op2}", 
                $"Mul {Result}, {Op1}, {Op2}", $"Div {Result}, {Op1}, {Op2}",
                $"Slt {Result}, {Op1}, {Op2}", $"Bgt {Result}, {Op1}, {Op2}", 
                $"Beq {Result}, {Op1}, {Op2}", $"Add {Op1}, {Op1}, {Op2}", 
                "-=", "*=", "/="}[(int)Type];
        }

        public bool AssignToSelf()
        {
            if (Type == OperatorTypes.ADDASSIGN) return true;
            if (Type == OperatorTypes.SUBTRACTASSIGN) return true;
            if (Type == OperatorTypes.MULTIPLYASSIGN) return true;
            if (Type == OperatorTypes.DIVIDEASSIGN) return true;

            return false;
        }

        public override IntermediaryCodeMeta GenerateCode(CompilationMeta MetaData)
        {
            return new IntermediaryCodeMeta(new string[] { ";opeartor generated" }, true);
        }
    }

    internal class Variable : Operand
    {
        public string Name { get; private set; }
        public string Type { get; private set; }
        public override bool SkipGeneration { get { return true; } }
        int ArgumentPosition = -1;

        public Variable(string Name, string Type, int ArgumentPosition = -1)
        {
            this.Name = Name;
            this.Type = Type;
            this.ArgumentPosition = ArgumentPosition;
        }

        public int? GetArgumentPosition()
        {
            if (ArgumentPosition >= 0)
                return ArgumentPosition;
            return null;
        }

        public override string ToString()
        {
            return Type + ":" + Name;
        }

        public override IntermediaryCodeMeta GenerateCode(CompilationMeta MetaData)
        {
            return new IntermediaryCodeMeta(new string[0], false);
        }
    }

    internal class ParanEnd : ASTExpression { }
    internal class CurlyEnd : ASTExpression { }

    internal class Semicolon : ASTExpression { }

    internal class FunctionCall : ASTExpression
    {
        public List<Operand> Arguments { get; private set; }
        public string LabelName { get; private set; }

        public FunctionCall(string LabelName, List<Operand> Arguments)
        {
            this.LabelName = LabelName;
            this.Arguments = Arguments;
        }

        public override bool SkipChildGeneration
        {
            get { return true; }
        }

        public override string ToString()
        {
            return $"{LabelName}({Arguments.Count})";
        }

        public override IntermediaryCodeMeta GenerateCode(CompilationMeta MetaData)
        {
            List<string> AllCode = new List<string>();

            AllCode.Add(";start function call-----");

            for (int i = 0; i < Arguments.Count; i++)
            {
                Operand operand = Arguments[i];
                if (operand is Literal literal)
                {
                    if (Int32.TryParse(literal.GetValue().ToString(), out int literalInt))
                    {
                        AllCode.Add($"Li $a{i}, {literalInt}");
                        continue;
                    }
                    
                    string VariableLabel = MetaData.PushStaticString(literal.GetValue().ToString());
                    AllCode.Add($"Li $a{i}, {VariableLabel}");
                    continue;
                }

                if (operand is Variable variable)
                {
                    int VariableTempRegister = MetaData.GetTemporaryRegister(MetaData.LookupVariable(variable.Name));
                    //AllCode.Add($"Li $t{VariableTempRegister}, {MetaData.GetReferenceLabelByPointer(variable.Name)}");
                    AllCode.Add($"LB $t{VariableTempRegister}, {MetaData.GetReferenceLabelByPointer(variable.Name)}(0)");
                    AllCode.Add($"Move $a{i}, $t{VariableTempRegister}");
                    continue;
                }
            }

            AllCode.Add($"Jal {LabelName}");

            AllCode.Add(";end function call-------");

            return new IntermediaryCodeMeta(AllCode.ToArray(), false);
        }
    }

    internal class Assignment : ASTExpression
    {
        public Variable LHS { get; private set; }
        public Expression RHS { get; private set; }
        public string? VariableLabel { get; private set; }

        public Assignment(Variable LHS, Expression RHS, string? VariableLabel)
        {
            this.LHS = LHS;
            this.RHS = RHS;
            this.VariableLabel = VariableLabel;
        }

        public override string ToString()
        {
            return "=";
        }

        public override IntermediaryCodeMeta GenerateCode(CompilationMeta MetaData)
        {
            Variable LeftVar = (Variable)LHS;

            if (LeftVar.Type == "NUMBER")
            {
                if (RHS is Literal value)
                {
                    int tempRegister = MetaData.GetTemporaryRegister(MetaData.LookupVariable(LeftVar.Name));
                    return new IntermediaryCodeMeta(
                        new string[2] { $"Li $t{tempRegister}, {(value.GetValue())}",
                        $"SB $t{tempRegister}, {LeftVar.Name}(0)"},
                        false);
                }

                if (RHS is Variable RHVariable)
                {
                    int tempRegister = MetaData.GetTemporaryRegister(MetaData.LookupVariable(LeftVar.Name));

                    return new IntermediaryCodeMeta(
                        new string[2] { $"LB $t{tempRegister}, {RHVariable.Name}(0)",
                        $"SB $t{tempRegister}, {LeftVar.Name}(0)"},
                        false);
                }
            }
            else
            {
                int tempRegister = MetaData.GetTemporaryRegister(MetaData.LookupVariable(LeftVar.Name));
                string referenceLabel = MetaData.GetReferenceLabelByPointer(LeftVar.Name);
                int pointerRegister = MetaData.GetTemporaryRegister(LeftVar.Name.GetHashCode());

                if (RHS is Variable)
                {
                    int RHtempRegister = MetaData.GetTemporaryRegister(MetaData.LookupVariable((RHS as Variable).Name));

                    return new IntermediaryCodeMeta(
                        new string[2] { $"La $t{RHtempRegister}, {(RHS as Variable).Name}",
                            $"SB $t{RHtempRegister}, {referenceLabel}(0)"},
                        false);
                }
                
                if (VariableLabel == null)
                    VariableLabel = MetaData.PushStaticString((RHS as Literal).GetValue().ToString());

                return new IntermediaryCodeMeta(
                    new string[2] { $"La $t{tempRegister}, {VariableLabel}",
                        $"SB $t{tempRegister}, {referenceLabel}(0)" },
                    false);
            }

            return base.GenerateCode(MetaData);
        }
    }

    internal class Conditional : ASTExpression
    {
        public override bool SkipChildGeneration { get { return true; } }

        Expression Condition;
        Expression Body;

        public Conditional(Expression Condition, Expression Body)
        {
            this.Condition = Condition;
            this.Body = Body;
        }

        public override string ToString()
        {
            return "Conditional->" + Condition.ToString() + "," + Body.ToString();
        }

        public override IntermediaryCodeMeta GenerateCode(CompilationMeta MetaData)
        {
            string[] ConditionCode = ICWalker.GenerateCodeRecursive(Condition.TreeRepresentation, MetaData, true);
            string[] BodyCode = ICWalker.GenerateCodeRecursive(Body.TreeRepresentation, MetaData, true);

            string startGuid = Guid.NewGuid().ToString().Replace("-", "");
            string endGuid = Guid.NewGuid().ToString().Replace("-", "");

            string JumpRegister = ICWalker.GetFirstRegister(ConditionCode[2]);

            ConditionCode[0] = $"{startGuid}: {ConditionCode[0]}";

            List<string> AllCode = new List<string>();
            AllCode.AddRange(ConditionCode);
            AllCode.Add($"Beq {JumpRegister}, $zero, {endGuid}");
            AllCode.AddRange(BodyCode);
            AllCode.Add($"{endGuid}:");

            return new IntermediaryCodeMeta(AllCode.ToArray(), false);
        }
    }

    internal class WhileLoop : ASTExpression
    {
        public override bool SkipChildGeneration { get { return true; } }

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

        public override IntermediaryCodeMeta GenerateCode(CompilationMeta MetaData)
        {
            string[] ConditionCode = ICWalker.GenerateCodeRecursive(Condition.TreeRepresentation, MetaData, true);
            string[] BodyCode = ICWalker.GenerateCodeRecursive(Body.TreeRepresentation, MetaData, true);

            string startGuid = Guid.NewGuid().ToString().Replace("-", "");
            string endGuid = Guid.NewGuid().ToString().Replace("-", "");

            string JumpRegister = ICWalker.GetFirstRegister(ConditionCode[2]);

            ConditionCode[0] = $"{startGuid}: {ConditionCode[0]}";

            List<string> AllCode = new List<string>();
            AllCode.AddRange(ConditionCode);
            AllCode.Add($"Beq {JumpRegister}, $zero, {endGuid}");
            AllCode.AddRange(BodyCode);
            AllCode.Add($"J {startGuid}");
            AllCode.Add($"{endGuid}:");


            return new IntermediaryCodeMeta(AllCode.ToArray(), false);
        }
    }

    internal class Function : ASTExpression
    {
        public override bool SkipChildGeneration { get { return true; } }
        public string FunctionName { get; private set; }
        public int ArgumentCount = 0;

        Expression Body;

        public Function(string FunctionName, Expression Body, int ArgumentCount)
        {
            this.FunctionName = FunctionName;
            this.Body = Body;
            this.ArgumentCount = ArgumentCount;
        }

        public override string ToString()
        {
            return $"func ({FunctionName})[{ArgumentCount}]->" + Body.ToString();
        }

        public override IntermediaryCodeMeta GenerateCode(CompilationMeta MetaData)
        {
            List<string> BodyCode = new List<string>();
            List<string> GeneratedCode = ICWalker.GenerateCodeRecursive(Body.TreeRepresentation, MetaData, true).ToList();

            string FuncEnd = System.Guid.NewGuid().ToString().Replace("-", "");

            BodyCode.Add($"J {FuncEnd}");
            GeneratedCode[0] = $"{FunctionName}: " + GeneratedCode[0];

            BodyCode.AddRange(GeneratedCode);
            BodyCode.Add("Jr $ra");
            BodyCode.Add($"{FuncEnd}: ");

            return new IntermediaryCodeMeta(BodyCode.ToArray(), false);
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
            string Store = "";
            int leftRegister = -1;
            if (LHS is Variable lvalue)
            {
                leftRegister = MetaData.GetTemporaryRegister(MetaData.LookupVariable(lvalue.Name));
                LHSLoad = $"LB $t{leftRegister}, {lvalue.Name}(0)";
                Store = $"SB $t{leftRegister}, {lvalue.Name}(0)";
            }
            else
            {
                if (LHS is Literal)
                {
                    leftRegister = MetaData.GetTemporaryRegister(-2);
                    LHSLoad = $"Ori $t{leftRegister}, $zero, {(LHS as Literal).GetValue()}";
                }
                else
                {
                    if (LHS is Expression)
                    {
                        LHSLoad = string.Join("\n", LHS.GenerateCode(MetaData).Code);
                    }
                }
            }

            string RHSLoad = "";
            int rightRegister = -1;
            if (RHS is Variable rvalue)
            {
                rightRegister = MetaData.GetTemporaryRegister(MetaData.LookupVariable(rvalue.Name));
                RHSLoad = $"LB $t{rightRegister}, {rvalue.Name}(0)";
            }
            else
            {
                rightRegister = MetaData.GetTemporaryRegister(-2);
                RHSLoad = $"Ori $t{rightRegister}, $zero, {(RHS as Literal).GetValue()}";
            }

            int resultRegister = -1;
            if (Operator.AssignToSelf())
                resultRegister = leftRegister;
            else
                resultRegister = MetaData.GetTemporaryRegister(-2);

            return new IntermediaryCodeMeta(new string[4] { LHSLoad, RHSLoad, 
                Operator.GetCommand(false, $"$t{resultRegister}", $"$t{leftRegister}", $"$t{rightRegister}"), 
                Store }, true );
        }
    }

    internal class BooleanOperation : BinaryOperation
    {
        public BooleanOperation(Expression LHS, Operator Operator, Expression RHS) : base(LHS, Operator, RHS)
        {
        }

        public override IntermediaryCodeMeta GenerateCode(CompilationMeta MetaData)
        {
            return base.GenerateCode(MetaData);
        }
    }
}

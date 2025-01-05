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
            return $"E:{GetHashCode().ToString()}";
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
                $"Slt {Result}, {Op1}, {Op2}", $"Bgt {Result}, {Op1}, {Op2}", $"Beq {Result}, {Op1}, {Op2}",
                $"Add {Op1}, {Op1}, {Op2}", "-=", "*=", "/="}[(int)Type];
        }

        public bool AssignToSelf()
        {
            if (Type == OperatorTypes.ADDASSIGN) return true;
            if (Type == OperatorTypes.SUBTRACTASSIGN) return true;
            if (Type == OperatorTypes.MULTIPLYASSIGN) return true;
            if (Type == OperatorTypes.DIVIDEASSIGN) return true;

            return false;
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

        public override IntermediaryCodeMeta GenerateCode(CompilationMeta MetaData)
        {
            return new IntermediaryCodeMeta(new string[0], false);
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

                if (RHS is Variable)
                {
                    int RHtempRegister = MetaData.GetTemporaryRegister(MetaData.LookupVariable((RHS as Variable).Name));

                    return new IntermediaryCodeMeta(
                        new string[1] { $"Li $t{tempRegister}, $t{RHtempRegister}" },
                        false);
                }

                string variableLabel = MetaData.PushStaticString((RHS as Literal).GetValue().ToString());
                return new IntermediaryCodeMeta(
                    new string[1] { $"Li $t{tempRegister}, {variableLabel}" },
                    false);
            }

            return base.GenerateCode(MetaData);
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

            string endGuid = Guid.NewGuid().ToString().Replace("-", "");
            string startGuid = Guid.NewGuid().ToString().Replace("-", "");

            string JumpRegister = ICWalker.GetFirstRegister(ConditionCode[2]);


            ConditionCode[0] = $"{startGuid}: {ConditionCode[0]}";

            List<string> AllCode = new List<string>();
            AllCode.Add(";BEGIN LOOP-------------------------------");
            AllCode.AddRange(ConditionCode);
            AllCode.Add($"Beq {JumpRegister}, 1, {endGuid}");
            AllCode.AddRange(BodyCode);
            AllCode.Add($"J {startGuid}");
            AllCode.Add($"{endGuid}:");
            AllCode.Add(";END LOOP---------------------------------");


            return new IntermediaryCodeMeta(AllCode.ToArray(), false);
        }
    }

    internal class Function : ASTExpression
    {
        public override bool SkipChildGeneration { get { return true; } }
        public string FunctionName { get; private set; }

        Expression Body;

        public Function(string FunctionName, Expression Body)
        {
            this.FunctionName = FunctionName;
            this.Body = Body;
        }

        public override string ToString()
        {
            return $"func ({FunctionName})->" + Body.ToString();
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
                leftRegister = MetaData.GetTemporaryRegister(-2);
                LHSLoad = $"Ori $t{leftRegister}, $zero, {(LHS as Literal).GetValue()}";
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
}

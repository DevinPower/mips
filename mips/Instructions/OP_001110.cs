﻿namespace mips.Instructions
{
    public class OP_001110 : OperationsBase, Instruction
    {
        static List<OperationWrapper<(Computer Computer, int rs, int rt, int imm)>> Operations;

        protected override string GetOpCode()
        {
            return "001110";
        }

        public List<SoftOperationWrapper> GetOperations() { return Operations.Select((x) => x.GetSoftWrapper()).ToList(); }

        public int GetOpCode(string InstructionName)
        {
            return Operations.First((x) => x.OperationName == InstructionName).Funct;
        }

        public void LoadOperations()
        {
            Operations = new List<OperationWrapper<(Computer Computer, int rs, int rt, int imm)>>();
            
            Operations.Add(new OperationWrapper<(Computer Computer, int rs, int rt, int imm)>(nameof(Xori), 32, Xori, GetRtImmediateInstructions()));
        }

        static void Xori((Computer Computer, int rs, int rt, int imm) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rt] = passedArgs.Computer.Memory[passedArgs.rt] ^ passedArgs.imm;
        }

        public void Execute(Computer Computer, int Instruction) 
        {
            //opcode, rs, rt, imm
            int[] InstructionSplits = HelperFunctions.BitsToInt(Instruction, new int[] { 6, 5, 5, 16 });
            Operations[0].FunctionCall.Invoke((Computer, InstructionSplits[2], InstructionSplits[1], InstructionSplits[0]));
        }
    }
}

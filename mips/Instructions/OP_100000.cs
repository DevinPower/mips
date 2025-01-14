namespace mips.Instructions
{
    public class OP_100000 : OperationsBase, Instruction
    {
        static List<OperationWrapper<(Computer Computer, int rs, int rt, int imm)>> Operations;

        protected override string GetOpCode()
        {
            return "100000";
        }

        public List<SoftOperationWrapper> GetOperations() { return Operations.Select((x) => x.GetSoftWrapper()).ToList(); }

        public int GetOpCode(string InstructionName)
        {
            return Operations.First((x) => x.OperationName == InstructionName).Funct;
        }

        public void LoadOperations()
        {
            Operations = new List<OperationWrapper<(Computer Computer, int rs, int rt, int imm)>>();
            
            Operations.Add(new OperationWrapper<(Computer Computer, int rs, int rt, int imm)>(nameof(LB), 32, LB, GetRtCalculatedOffsetRs()));
        }

        static void LB((Computer Computer, int rs, int rt, int offset) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rt] = passedArgs.Computer.Memory[passedArgs.offset + passedArgs.Computer.Memory[passedArgs.rs]];
        }

        public void Execute(Computer Computer, int Instruction) 
        {
            //opcode, rs, rt, imm
            int[] InstructionSplits = HelperFunctions.BitsToInt(Instruction, new int[] { 6, 5, 5, 16 });
            Operations[0].FunctionCall.Invoke((Computer, InstructionSplits[2], InstructionSplits[1], InstructionSplits[0]));
        }
    }
}

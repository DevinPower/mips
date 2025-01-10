namespace mips.Instructions
{
    public class OP_000100 : OperationsBase, Instruction
    {
        static List<OperationWrapper<(Computer Computer, int rs, int rt, int offset)>> Operations;

        protected override string GetOpCode()
        {
            return "000100";
        }

        public List<SoftOperationWrapper> GetOperations() { return Operations.Select((x) => x.GetSoftWrapper()).ToList(); }

        public int GetOpCode(string InstructionName)
        {
            return Operations.First((x) => x.OperationName == InstructionName).Funct;
        }

        public void LoadOperations()
        {
            Operations = new List<OperationWrapper<(Computer Computer, int rs, int rt, int offset)>>();
            
            Operations.Add(new OperationWrapper<(Computer Computer, int rs, int rt, int offset)>(nameof(Beq), 32, Beq, GetRsRtOffsetInstructions()));
        }

        static void Beq((Computer Computer, int rs, int rt, int offset) passedArgs)
        {
            if (passedArgs.Computer.Memory[passedArgs.rs] == passedArgs.Computer.Memory[passedArgs.rt])
                passedArgs.Computer.Jump(passedArgs.offset);
        }

        public void Execute(Computer Computer, int Instruction)
        {
            int[] InstructionSplits = HelperFunctions.BitsToInt(Instruction, new int[] { 6, 5, 5, 16 });
            Operations[0].FunctionCall.Invoke((Computer, InstructionSplits[1], InstructionSplits[2], InstructionSplits[0]));
        }
    }
}

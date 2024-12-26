namespace mips.Instructions
{
    public class OP_000011 : OperationsBase, Instruction
    {
        static List<OperationWrapper<(Computer Computer, int target)>> Operations;

        protected override string GetOpCode()
        {
            return "000011";
        }

        public List<SoftOperationWrapper> GetOperations() { return Operations.Select((x) => x.GetSoftWrapper()).ToList(); }

        public int GetOpCode(string InstructionName)
        {
            return Operations.First((x) => x.OperationName == InstructionName).Funct;
        }

        public void LoadOperations()
        {
            Operations = new List<OperationWrapper<(Computer Computer, int target)>>();
            
            Operations.Add(new OperationWrapper<(Computer Computer, int target)>(nameof(Jal), 32, Jal, GetTargetInstruction()));
        }

        static void Jal((Computer Computer, int target) passedArgs)
        {
            passedArgs.Computer.StoreMemory(passedArgs.Computer.GetProgramCounter(), passedArgs.Computer.GetRegisterAddress("$ra"));
            passedArgs.Computer.Jump(passedArgs.target);
        }

        public void Execute(Computer Computer, int Instruction)
        {
            int[] InstructionSplits = HelperFunctions.BitsToInt(Instruction, new int[] { 6, 26 });
            Operations[0].FunctionCall.Invoke((Computer, InstructionSplits[0]));
        }
    }
}

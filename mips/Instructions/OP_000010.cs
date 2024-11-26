namespace mips.Instructions
{
    public class OP_000010 : OperationsBase, Instruction
    {
        static List<OperationWrapper<(Computer Computer, int target)>> Operations;

        protected override string GetOpCode()
        {
            return "000010";
        }

        public List<SoftOperationWrapper> GetOperations() { return Operations.Select((x) => x.GetSoftWrapper()).ToList(); }

        public int GetOpCode(string InstructionName)
        {
            return Operations.First((x) => x.OperationName == InstructionName).Funct;
        }

        public void LoadOperations()
        {
            Operations = new List<OperationWrapper<(Computer Computer, int target)>>();
            
            Operations.Add(new OperationWrapper<(Computer Computer, int target)>(nameof(J), 32, J, GetTargetInstruction()));
        }

        static void J((Computer Computer, int target) passedArgs)
        {
            passedArgs.Computer.Jump(passedArgs.target);
        }

        public void Execute(Computer Computer, int Instruction)
        {
            int[] InstructionSplits = HelperFunctions.BitsToInt(Instruction, new int[] { 6, 26 });
            Operations[0].FunctionCall.Invoke((Computer, InstructionSplits[0]));
        }
    }
}

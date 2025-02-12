namespace mips.Instructions
{
    public class OP_111000 : OperationsBase, Instruction
    {
        static List<OperationWrapper<(Computer Computer, int rs, int rt, int rd, int sa)>> Operations;

        protected override string GetOpCode()
        {
            return "111000";
        }

        public List<SoftOperationWrapper> GetOperations() { return Operations.Select((x) => x.GetSoftWrapper()).ToList(); }

        public int GetOpCode(string InstructionName)
        {
            return Operations.First((x) => x.OperationName == InstructionName).Funct;
        }

        public void LoadOperations()
        {
            Operations = new List<OperationWrapper<(Computer Computer, int rs, int rt, int rd, int sa)>>();
            
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>($"{nameof(Add)}.s", 32, Add, GetBasicInstructionsWithFunctCode("100000")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>($"{nameof(Sub)}.s", 34, Sub, GetBasicInstructionsWithFunctCode("100010")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>($"{nameof(Div)}.s", 26, Div, GetBasicInstructionsWithFunctCode("011010")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>($"{nameof(Mult)}.s", 24, Mult, GetBasicInstructionsWithFunctCode("011000")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>($"{nameof(Seq)}.s", 58, Seq, GetBasicInstructionsWithFunctCode("111010")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>($"{nameof(Sne)}.s", 59, Sne, GetBasicInstructionsWithFunctCode("111011")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>($"{nameof(Slt)}.s", 42, Slt, GetBasicInstructionsWithFunctCode("101010")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>($"{nameof(Slte)}.s", 44, Slte, GetBasicInstructionsWithFunctCode("101100")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>($"Cvt.i.s", 59, ConvertToInt, GetBasicInstructionsWithFunctCode("111011")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>($"Cvt.s.i", 43, ConvertToFloat, GetBasicInstructionsWithFunctCode("101011")));
        }

        static void Add((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = HelperFunctions.FloatToInt(HelperFunctions.IntToFloat(passedArgs.Computer.Memory[passedArgs.rs]) + HelperFunctions.IntToFloat(passedArgs.Computer.Memory[passedArgs.rt]));
        }

        static void Sub((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = HelperFunctions.FloatToInt(HelperFunctions.IntToFloat(passedArgs.Computer.Memory[passedArgs.rs]) - HelperFunctions.IntToFloat(passedArgs.Computer.Memory[passedArgs.rt]));
        }

        static void Div((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            int quotient = HelperFunctions.FloatToInt(HelperFunctions.IntToFloat(passedArgs.Computer.Memory[passedArgs.rs]) / HelperFunctions.IntToFloat(passedArgs.Computer.Memory[passedArgs.rt]));
            int remainder = HelperFunctions.FloatToInt(HelperFunctions.IntToFloat(passedArgs.Computer.Memory[passedArgs.rs]) % HelperFunctions.IntToFloat(passedArgs.Computer.Memory[passedArgs.rt]));

            passedArgs.Computer.HIRegister = remainder;
            passedArgs.Computer.LORegister = quotient;
        }

        static void Mult((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            int result = HelperFunctions.FloatToInt(HelperFunctions.IntToFloat(passedArgs.Computer.Memory[passedArgs.rs]) * HelperFunctions.IntToFloat(passedArgs.Computer.Memory[passedArgs.rt]));

            int lower32 = (int)(result & 0xFFFFFFFF);
            int upper32 = (int)(result >> 32);

            passedArgs.Computer.HIRegister = upper32;
            passedArgs.Computer.LORegister = lower32;
        }

        static void Slt((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = HelperFunctions.IntToFloat(passedArgs.Computer.Memory[passedArgs.rs]) < HelperFunctions.IntToFloat(passedArgs.Computer.Memory[passedArgs.rt]) ? 1 : 0;
        }

        static void Slte((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = HelperFunctions.IntToFloat(passedArgs.Computer.Memory[passedArgs.rs]) <= HelperFunctions.IntToFloat(passedArgs.Computer.Memory[passedArgs.rt]) ? 1 : 0;
        }

        static void Seq((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = HelperFunctions.IntToFloat(passedArgs.Computer.Memory[passedArgs.rs]) == HelperFunctions.IntToFloat(passedArgs.Computer.Memory[passedArgs.rt]) ? 1 : 0;
        }

        static void Sne((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = HelperFunctions.IntToFloat(passedArgs.Computer.Memory[passedArgs.rs]) != HelperFunctions.IntToFloat(passedArgs.Computer.Memory[passedArgs.rt]) ? 1 : 0;
        }

        //TODO: Reading register it doesnt need
        static void ConvertToInt((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = (int)HelperFunctions.IntToFloat(passedArgs.Computer.Memory[passedArgs.rs]);
        }

        //TODO: Reading register it doesnt need
        static void ConvertToFloat((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = HelperFunctions.FloatToInt((float)passedArgs.Computer.Memory[passedArgs.rs]);
        }

        public void Execute(Computer Computer, int Instruction) 
        {
            int[] InstructionSplits = HelperFunctions.BitsToInt(Instruction, new int[] { 6, 5, 5, 5, 5, 6 });
            OperationWrapper<(Computer Computer, int rs, int rt, int rd, int sa)> operation = Operations.First((x) => x.Funct == InstructionSplits[0]);
            operation.FunctionCall
               .Invoke((Computer, InstructionSplits[3], InstructionSplits[2], InstructionSplits[4], InstructionSplits[1]));
        }
    }
}

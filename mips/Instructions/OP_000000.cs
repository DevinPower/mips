namespace mips.Instructions
{
    public class OP_000000 : OperationsBase, Instruction
    {
        static List<OperationWrapper<(Computer, int, int, int, int)>> Operations;

        protected override string GetOpCode()
        {
            return "000000";
        }

        public List<SoftOperationWrapper> GetOperations() { return Operations.Select((x) => x.GetSoftWrapper()).ToList(); }

        public void LoadOperations()
        {
            Operations = new List<OperationWrapper<(Computer, int, int, int, int)>>();
            
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Add), 32, Add, GetBasicInstructionsWithFunctCode("100000")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Addu), 33, Addu, GetBasicInstructionsWithFunctCode("100001")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(And), 36, And, GetBasicInstructionsWithFunctCode("100100")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Nor), 39, Nor, GetBasicInstructionsWithFunctCode("100111")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Or), 37, Or, GetBasicInstructionsWithFunctCode("100101")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Slt), 42, Slt, GetBasicInstructionsWithFunctCode("101010")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Sltu), 43, Sltu, GetBasicInstructionsWithFunctCode("101011")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Sub), 34, Sub, GetBasicInstructionsWithFunctCode("100010")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Subu), 35, Subu, GetBasicInstructionsWithFunctCode("100011")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Xor), 38, Xor, GetBasicInstructionsWithFunctCode("100110")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Sll), 0, Sll, GetRdRtSaImmediateInstructions("000000")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Sllv), 4, Sllv, GetRdRtRsInstructions("000100")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Sra), 3, Sra, GetRdRtSaImmediateInstructions("000011")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Srav), 7, Srav, GetRdRtRsInstructions("000111")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Srl), 2, Srl, GetRdRtSaImmediateInstructions("000010")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Srlv), 6, Srlv, GetRdRtRsInstructions("000110")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Div), 26, Div, GetRsRtInstructions("011010")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Divu), 27, Divu, GetRsRtInstructions("011011")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Mfhi), 16, Mfhi, GetRdInstructions("010000")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Mflo), 18, Mflo, GetRdInstructions("010010")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Mthi), 17, Mthi, GetRsInstructions("010001")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Mtlo), 19, Mtlo, GetRsInstructions("010011")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Mult), 24, Mult, GetRsRtInstructions("011000")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Multu), 25, Multu, GetRsRtInstructions("011001")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Jalr), 9, Jalr, GetRsInstructions("001001")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Jr), 8, Jr, GetRsInstructions("001000")));
            Operations.Add(new OperationWrapper<(Computer, int, int, int, int)>(nameof(Syscall), 12, Syscall, GetNoInstructionsWithFunctCode("001100")));
        }

        static void Add((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = passedArgs.Computer.Memory[passedArgs.rs] + passedArgs.Computer.Memory[passedArgs.rt];
        }

        static void Addu((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = (int)((uint)passedArgs.Computer.Memory[passedArgs.rs] + (uint)passedArgs.Computer.Memory[passedArgs.rt]);
        }

        static void Sub((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = passedArgs.Computer.Memory[passedArgs.rs] - passedArgs.Computer.Memory[passedArgs.rt];
        }

        static void Subu((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = (int)((uint)passedArgs.Computer.Memory[passedArgs.rs] - (uint)passedArgs.Computer.Memory[passedArgs.rt]);
        }

        static void Div((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            int quotient = passedArgs.Computer.Memory[passedArgs.rs] / passedArgs.Computer.Memory[passedArgs.rt];
            int remainder = passedArgs.Computer.Memory[passedArgs.rs] % passedArgs.Computer.Memory[passedArgs.rt];

            passedArgs.Computer.HIRegister = remainder;
            passedArgs.Computer.LORegister = quotient;
        }

        static void Divu((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            int quotient = (int)((uint)passedArgs.Computer.Memory[passedArgs.rs] / (uint)passedArgs.Computer.Memory[passedArgs.rt]);
            int remainder = (int)((uint)passedArgs.Computer.Memory[passedArgs.rs] % (uint)passedArgs.Computer.Memory[passedArgs.rt]);

            passedArgs.Computer.HIRegister = remainder;
            passedArgs.Computer.LORegister = quotient;
        }

        static void Syscall((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.SysCall();
        }

        static void Mult((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            long result = Convert.ToInt64(passedArgs.Computer.Memory[passedArgs.rs]) * Convert.ToInt64(passedArgs.Computer.Memory[passedArgs.rt]);

            int lower32 = (int)(result & 0xFFFFFFFF);
            int upper32 = (int)(result >> 32);

            passedArgs.Computer.HIRegister = upper32;
            passedArgs.Computer.LORegister = lower32;
        }

        static void Multu((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            ulong result = Convert.ToUInt64(passedArgs.Computer.Memory[passedArgs.rs]) * Convert.ToUInt64(passedArgs.Computer.Memory[passedArgs.rt]);

            int lower32 = (int)(result & 0xFFFFFFFF);
            int upper32 = (int)(result >> 32);

            passedArgs.Computer.HIRegister = upper32;
            passedArgs.Computer.LORegister = lower32;
        }

        static void Mfhi((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = passedArgs.Computer.HIRegister;
        }

        static void Mflo((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = passedArgs.Computer.LORegister;
        }

        static void Mthi((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.HIRegister = passedArgs.Computer.Memory[passedArgs.rs];
        }

        static void Mtlo((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.LORegister = passedArgs.Computer.Memory[passedArgs.rs];
        }

        static void And((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = passedArgs.Computer.Memory[passedArgs.rs] & passedArgs.Computer.Memory[passedArgs.rt];
        }

        static void Nor((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = ~(passedArgs.Computer.Memory[passedArgs.rs] | passedArgs.Computer.Memory[passedArgs.rt]);
        }

        static void Or((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = (passedArgs.Computer.Memory[passedArgs.rs] | passedArgs.Computer.Memory[passedArgs.rt]);
        }

        static void Xor((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = (passedArgs.Computer.Memory[passedArgs.rs] ^ passedArgs.Computer.Memory[passedArgs.rt]);
        }

        static void Slt((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = passedArgs.Computer.Memory[passedArgs.rs] < passedArgs.Computer.Memory[passedArgs.rt] ? 1 : 0;
        }

        static void Sltu((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = (uint)passedArgs.Computer.Memory[passedArgs.rs] < (uint)passedArgs.Computer.Memory[passedArgs.rt] ? 1 : 0;
        }

        static void Sll((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = passedArgs.Computer.Memory[passedArgs.rt] << passedArgs.sa;
        }

        static void Sllv((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = passedArgs.Computer.Memory[passedArgs.rt] << passedArgs.Computer.Memory[passedArgs.rs];
        }

        static void Srl((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = (int)((uint)passedArgs.Computer.Memory[passedArgs.rt] >> passedArgs.sa);
        }

        static void Srlv((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = (int)((uint)passedArgs.Computer.Memory[passedArgs.rt] >> passedArgs.Computer.Memory[passedArgs.rs]);
        }
        static void Sra((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = passedArgs.Computer.Memory[passedArgs.rt] >> passedArgs.sa;
        }

        static void Srav((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = passedArgs.Computer.Memory[passedArgs.rt] >> passedArgs.Computer.Memory[passedArgs.rs];
        }

        static void Jalr((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Memory[passedArgs.rd] = passedArgs.Computer.GetProgramCounter();
            passedArgs.Computer.Jump(passedArgs.Computer.Memory[passedArgs.rs]);
        }

        static void Jr((Computer Computer, int rs, int rt, int rd, int sa) passedArgs)
        {
            passedArgs.Computer.Jump(passedArgs.Computer.Memory[passedArgs.rs]);
        }

        public int GetOpCode(string InstructionName)
        {
            return Operations.First((x) => x.OperationName == InstructionName).Funct;
        }

        public void Execute(Computer Computer, int Instruction)
        {
            //opcode, rs, rt, rd, shamt, funct
            int[] InstructionSplits = HelperFunctions.BitsToInt(Instruction, new int[] { 6, 5, 5, 5, 5, 6 });
            OperationWrapper<(Computer Computer, int rs, int rt, int rd, int sa)> operation = Operations.First((x) => x.Funct == InstructionSplits[0]);
            operation.FunctionCall
               .Invoke((Computer, InstructionSplits[4], InstructionSplits[3], InstructionSplits[2], InstructionSplits[1]));
        }
    }
}

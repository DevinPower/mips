namespace mips.Instructions
{
    public class OperationsBase
    {
        protected virtual string GetOpCode()
        {
            return "ERROR";
        }

        protected InputInstruction[] GetBasicInstructionsWithFunctCode(string Funct)
        {
            return new[] {
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, GetOpCode(), 6),
                new InputInstruction(InputInstruction.InstructionType.ReadRegister, "0", 5),
                new InputInstruction(InputInstruction.InstructionType.ReadRegister, "1", 5),
                new InputInstruction(InputInstruction.InstructionType.ReadRegister, "2", 5),
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, "00000", 5),
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, Funct, 6)
            };
        }

        protected InputInstruction[] GetNoInstructionsWithFunctCode(string Funct)
        {
            return new[] {
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, GetOpCode(), 6),
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, "00000000000000000000", 20),
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, Funct, 6)
            };
        }

        protected InputInstruction[] GetRdInstructions(string Funct)
        {
            return new[] {
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, GetOpCode(), 6),
                new InputInstruction(InputInstruction.InstructionType.ReadRegister, "0", 5),
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, "00000", 5),
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, "00000", 5),
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, "00000", 5),
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, Funct, 6)
            };
        }

        protected InputInstruction[] GetRdRtRsInstructions(string Funct)
        {
            return new[] {
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, GetOpCode(), 6),
                new InputInstruction(InputInstruction.InstructionType.ReadRegister, "0", 5),
                new InputInstruction(InputInstruction.InstructionType.ReadRegister, "2", 5),
                new InputInstruction(InputInstruction.InstructionType.ReadRegister, "1", 5),
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, "00000", 5),
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, Funct, 6)
            };
        }

        protected InputInstruction[] GetRsInstructions(string Funct)
        {
            return new[] {
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, GetOpCode(), 6),
                new InputInstruction(InputInstruction.InstructionType.ReadRegister, "1", 5),
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, "000000000000000", 15),
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, Funct, 6)
            };
        }

        protected InputInstruction[] GetRsRtInstructions(string Funct)
        {
            return new[] {
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, GetOpCode(), 6),
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, "00000", 5),
                new InputInstruction(InputInstruction.InstructionType.ReadRegister, "0", 5),
                new InputInstruction(InputInstruction.InstructionType.ReadRegister, "1", 5),
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, "00000", 5),
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, Funct, 6)
            };
        }

        protected InputInstruction[] GetRtRsImmediateInstructions()
        {
            return new[] {
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, GetOpCode(), 6),
                new InputInstruction(InputInstruction.InstructionType.ReadRegister, "1", 5),
                new InputInstruction(InputInstruction.InstructionType.ReadRegister, "0", 5),
                new InputInstruction(InputInstruction.InstructionType.ReadImmediate, "2", 16)
            };
        }

        protected InputInstruction[] GetRtImmediateInstructions()
        {
            return new[] {
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, GetOpCode(), 6),
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, "00000", 5),
                new InputInstruction(InputInstruction.InstructionType.ReadRegister, "0", 5),
                new InputInstruction(InputInstruction.InstructionType.ReadImmediate, "1", 16)
            };
        }

        protected InputInstruction[] GetRdRtSaImmediateInstructions(string Funct)
        {
            return new[] {
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, GetOpCode(), 6),
                new InputInstruction(InputInstruction.InstructionType.ReadRegister, "1", 5),
                new InputInstruction(InputInstruction.InstructionType.ReadRegister, "0", 5),
                new InputInstruction(InputInstruction.InstructionType.ReadImmediate, "2", 8),
                new InputInstruction(InputInstruction.InstructionType.ReadStatic, Funct, 6),
            };
        }

    }
}
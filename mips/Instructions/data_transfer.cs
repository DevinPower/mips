using mips.Instructions;

public class move : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterAddressLHS = Computer.GetRegisterAddress(Parameters[0]);
        int RegisterAddressRHS = Computer.GetRegisterAddress(Parameters[1]);

        Computer.Registers[RegisterAddressLHS] = Computer.Registers[RegisterAddressRHS];
    }
}

public class li : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterAddress = Computer.GetRegisterAddress(Parameters[0]);
        int Value = Int32.Parse(Parameters[1]);

        Computer.Registers[RegisterAddress] = Value;
    }
}

public class lui : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterAddress = Computer.GetRegisterAddress(Parameters[0]);
        int Value = Int16.Parse(Parameters[1]);

        Computer.Registers[RegisterAddress] = (Value << 16) | (0 & 0xFFFF);
    }
}

public class sw : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterAddress = Computer.GetRegisterAddress(Parameters[0]);
        int MemoryAddress = HelperFunctions.ProcessMemoryAddress(Computer, Parameters[1]);

        Computer.Memory[MemoryAddress] = Computer.Registers[RegisterAddress];
    }
}

public class lw : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterAddress = Computer.GetRegisterAddress(Parameters[0]);
        int MemoryAddress = HelperFunctions.ProcessMemoryAddress(Computer, Parameters[1]);

        Computer.Registers[RegisterAddress] = Computer.Memory[MemoryAddress];
    }
}

public class la : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterAddress = Computer.GetRegisterAddress(Parameters[0]);
        int Value = Int32.Parse(Parameters[1]);

        Computer.Registers[RegisterAddress] = Value;
    }
}

public class mfhi : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterAddress = Computer.GetRegisterAddress(Parameters[0]);

        Computer.Registers[RegisterAddress] = Computer.HIRegister;
    }
}

public class mflo : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterAddress = Computer.GetRegisterAddress(Parameters[0]);

        Computer.Registers[RegisterAddress] = Computer.LORegister;
    }
}
public class add : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterAddressResult = Computer.GetRegisterAddress(Parameters[0]);
        int RegisterAddressLHS = Computer.GetRegisterAddress(Parameters[1]);
        int RegisterAddressRHS = Computer.GetRegisterAddress(Parameters[2]);

        Computer.Registers[RegisterAddressResult] = Computer.Registers[RegisterAddressLHS] + Computer.Registers[RegisterAddressRHS];
    }
}

public class sub : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterAddressResult = Computer.GetRegisterAddress(Parameters[0]);
        int RegisterAddressLHS = Computer.GetRegisterAddress(Parameters[1]);
        int RegisterAddressRHS = Computer.GetRegisterAddress(Parameters[2]);

        Computer.Registers[RegisterAddressResult] = Computer.Registers[RegisterAddressLHS] - Computer.Registers[RegisterAddressRHS];
    }
}

public class addi : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterAddressResult = Computer.GetRegisterAddress(Parameters[0]);
        int RegisterAddressLHS = Computer.GetRegisterAddress(Parameters[1]);
        int RHS = int.Parse(Parameters[2]);

        Computer.Registers[RegisterAddressResult] = Computer.Registers[RegisterAddressLHS] + RHS;
    }
}

public class mul : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterAddressResult = Computer.GetRegisterAddress(Parameters[0]);
        int RegisterAddressLHS = Computer.GetRegisterAddress(Parameters[1]);
        int RegisterAddressRHS = Computer.GetRegisterAddress(Parameters[2]);

        Computer.Registers[RegisterAddressResult] = Computer.Registers[RegisterAddressLHS] * Computer.Registers[RegisterAddressRHS];
    }
}
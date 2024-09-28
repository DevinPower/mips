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

public class addu : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterAddressResult = Computer.GetRegisterAddress(Parameters[0]);
        int RegisterAddressLHS = Computer.GetRegisterAddress(Parameters[1]);
        int RegisterAddressRHS = Computer.GetRegisterAddress(Parameters[2]);

        Computer.Registers[RegisterAddressResult] = Convert.ToInt32(Convert.ToUInt32(Computer.Registers[RegisterAddressLHS]) + Convert.ToUInt32(Computer.Registers[RegisterAddressRHS]));
    }
}

public class addiu : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterAddressResult = Computer.GetRegisterAddress(Parameters[0]);
        int RegisterAddressLHS = Computer.GetRegisterAddress(Parameters[1]);
        uint RHS = UInt32.Parse(Parameters[2]);

        Computer.Registers[RegisterAddressResult] = Convert.ToInt32(Convert.ToUInt32(Computer.Registers[RegisterAddressLHS]) + RHS);
    }
}

public class subu : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterAddressResult = Computer.GetRegisterAddress(Parameters[0]);
        int RegisterAddressLHS = Computer.GetRegisterAddress(Parameters[1]);
        int RegisterAddressRHS = Computer.GetRegisterAddress(Parameters[2]);

        Computer.Registers[RegisterAddressResult] = Convert.ToInt32(Convert.ToUInt32(Computer.Registers[RegisterAddressLHS]) - Convert.ToUInt32(Computer.Registers[RegisterAddressRHS]));
    }
}

public class mult : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterAddressLHS = Computer.GetRegisterAddress(Parameters[0]);
        int RegisterAddressRHS = Computer.GetRegisterAddress(Parameters[1]);

        long result = Convert.ToInt64(Computer.Registers[RegisterAddressLHS]) * Convert.ToInt64(Computer.Registers[RegisterAddressRHS]);
        
        int lower32 = (int)(result & 0xFFFFFFFF);
        int upper32 = (int)(result >> 32);

        Computer.HIRegister = upper32;
        Computer.LORegister = lower32;
    }
}

public class div : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterAddressLHS = Computer.GetRegisterAddress(Parameters[0]);
        int RegisterAddressRHS = Computer.GetRegisterAddress(Parameters[1]);

        int quotient = Computer.Registers[RegisterAddressLHS] / Computer.Registers[RegisterAddressRHS];
        int remainder = Computer.Registers[RegisterAddressLHS] % Computer.Registers[RegisterAddressRHS];

        Computer.HIRegister = remainder;
        Computer.LORegister = quotient;
    }
}
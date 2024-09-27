using mips.Instructions;

public class j : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int Address = int.Parse(Parameters[0]);
        Computer.Jump(Address);
    }
}

public class jr : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int Address = Computer.GetRegisterAddress(Parameters[0]);
        Computer.Jump(Address);
    }
}

public class jal : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        Computer.Registers[Computer.GetRegisterAddress("$ra")] = Computer.GetProgramCounter() + 1;
        int Address = Computer.GetRegisterAddress(Parameters[0]);
        Computer.Jump(Address);
    }
}

public class beq : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterLHS = Computer.Registers[Computer.GetRegisterAddress(Parameters[0])];
        int RegisterRHS = Computer.Registers[Computer.GetRegisterAddress(Parameters[1])];
        int Destination = int.Parse(Parameters[2]);

        if (RegisterLHS == RegisterRHS)
            Computer.Jump(Destination);
    }
}

public class bne : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterLHS = Computer.Registers[Computer.GetRegisterAddress(Parameters[0])];
        int RegisterRHS = Computer.Registers[Computer.GetRegisterAddress(Parameters[1])];
        int Destination = int.Parse(Parameters[2]);

        if (RegisterLHS != RegisterRHS)
            Computer.Jump(Destination);
    }
}

public class bgt : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterLHS = Computer.Registers[Computer.GetRegisterAddress(Parameters[0])];
        int RegisterRHS = Computer.Registers[Computer.GetRegisterAddress(Parameters[1])];
        int Destination = int.Parse(Parameters[2]);

        if (RegisterLHS > RegisterRHS)
            Computer.Jump(Destination);
    }
}

public class blt : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterLHS = Computer.Registers[Computer.GetRegisterAddress(Parameters[0])];
        int RegisterRHS = Computer.Registers[Computer.GetRegisterAddress(Parameters[1])];
        int Destination = int.Parse(Parameters[2]);

        if (RegisterLHS < RegisterRHS)
            Computer.Jump(Destination);
    }
}

public class bge : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterLHS = Computer.Registers[Computer.GetRegisterAddress(Parameters[0])];
        int RegisterRHS = Computer.Registers[Computer.GetRegisterAddress(Parameters[1])];
        int Destination = int.Parse(Parameters[2]);

        if (RegisterLHS >= RegisterRHS)
            Computer.Jump(Destination);
    }
}

public class ble : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterLHS = Computer.Registers[Computer.GetRegisterAddress(Parameters[0])];
        int RegisterRHS = Computer.Registers[Computer.GetRegisterAddress(Parameters[1])];
        int Destination = int.Parse(Parameters[2]);

        if (RegisterLHS <= RegisterRHS)
            Computer.Jump(Destination);
    }
}


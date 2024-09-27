using System.Data;

public class and : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterAddressResult = Computer.GetRegisterAddress(Parameters[0]);
        int RegisterAddressLHS = Computer.GetRegisterAddress(Parameters[1]);
        int RegisterAddressRHS = Computer.GetRegisterAddress(Parameters[2]);

        Computer.Registers[RegisterAddressResult] = Computer.Registers[RegisterAddressLHS] & Computer.Registers[RegisterAddressRHS];
    }
}

public class or : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterAddressResult = Computer.GetRegisterAddress(Parameters[0]);
        int RegisterAddressLHS = Computer.GetRegisterAddress(Parameters[1]);
        int RegisterAddressRHS = Computer.GetRegisterAddress(Parameters[2]);

        Computer.Registers[RegisterAddressResult] = Computer.Registers[RegisterAddressLHS] | Computer.Registers[RegisterAddressRHS];
    }
}

public class andi : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterAddressResult = Computer.GetRegisterAddress(Parameters[0]);
        int RegisterAddressLHS = Computer.GetRegisterAddress(Parameters[1]);
        int RHS = int.Parse(Parameters[2]);

        Computer.Registers[RegisterAddressResult] = Computer.Registers[RegisterAddressLHS] & Computer.Registers[RHS];
    }
}

public class ori : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterAddressResult = Computer.GetRegisterAddress(Parameters[0]);
        int RegisterAddressLHS = Computer.GetRegisterAddress(Parameters[1]);
        int RHS = int.Parse(Parameters[2]);


        Computer.Registers[RegisterAddressResult] = Computer.Registers[RegisterAddressLHS] | Computer.Registers[RHS];
    }
}

public class sll : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterAddressResult = Computer.GetRegisterAddress(Parameters[0]);
        int RegisterAddressLHS = Computer.GetRegisterAddress(Parameters[1]);
        int RHS = int.Parse(Parameters[2]);


        Computer.Registers[RegisterAddressResult] = Computer.Registers[RegisterAddressLHS] << Computer.Registers[RHS];
    }
}

public class srl : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterAddressResult = Computer.GetRegisterAddress(Parameters[0]);
        int RegisterAddressLHS = Computer.GetRegisterAddress(Parameters[1]);
        int RHS = int.Parse(Parameters[2]);


        Computer.Registers[RegisterAddressResult] = Computer.Registers[RegisterAddressLHS] >> Computer.Registers[RHS];
    }
}

public class slt : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterAddressResult = Computer.GetRegisterAddress(Parameters[0]);
        int RegisterAddressLHS = Computer.GetRegisterAddress(Parameters[1]);
        int RegisterAddressRHS = Computer.GetRegisterAddress(Parameters[2]);

        if (Computer.Registers[RegisterAddressLHS] < Computer.Registers[RegisterAddressRHS])
            Computer.Registers[RegisterAddressResult] = 1;
        else
            Computer.Registers[RegisterAddressResult] = 0;
    }
}

public class slti : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        int RegisterAddressResult = Computer.GetRegisterAddress(Parameters[0]);
        int RegisterAddressLHS = Computer.GetRegisterAddress(Parameters[1]);
        int RHS = int.Parse(Parameters[2]);

        if (Computer.Registers[RegisterAddressLHS] < RHS)
            Computer.Registers[RegisterAddressResult] = 1;
        else
            Computer.Registers[RegisterAddressResult] = 0;
    }
}
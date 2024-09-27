public class syscall : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        Computer.SysCall();
    }
}
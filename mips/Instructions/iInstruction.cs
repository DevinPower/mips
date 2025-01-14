using mips.Instructions;

public interface Instruction
{
    public void Execute(Computer Computer, int Instruction);
    public int GetOpCode(string InstructionName);
    public void LoadOperations();
    public List<SoftOperationWrapper> GetOperations();
}

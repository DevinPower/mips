using mips.Instructions;

public class asciiz : Instruction
{
    public void Execute(Computer Computer, params string[] Parameters)
    {
        string myString = HelperFunctions.ProcessString(String.Join(" ", Parameters));

        foreach(Char c in myString)
        {
            Computer.StoreMemory((int)c);
        }
        Computer.StoreMemory(0);        //Null terminator
    }
}

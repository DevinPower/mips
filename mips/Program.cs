using mips;

string[] Program = new string[]
{
            //Instruction
            
            "PROMPT: la $a0 MSG",
            "li $v0 4",
            "syscall",

            "li $t0 10",        //const for checking

            //Read an integer
            "li $v0 5",
            "syscall",
            "move $t1 $v0",

            //check
            "beq $t0 $t1 10",
            "j PROMPT",
            
            "la $a0 SUCCESS",
            "li $v0 4",
            "syscall",

            "MSG: asciiz \"Please enter the number 10!\"",
            "SUCCESS: asciiz \":)\"",
};


string[] OpTest = new[]
{
    "LABUL: .asciiz Hello, world!",
    "Syscall",
    "Syscall",
    "Syscall",
    "Syscall",
    "Syscall",
    "Syscall",
    "Syscall",
    ".data",
    "Addi $t0, $t0, 123",
    "Addi $t1, $t0, 4",
    "Addi $t0, $t0, 40"
    //"Syscall"
    //"Syscall"
    //"Jr $t5"
};

Computer c = new Computer(128);
var compiled = c.Compile(OpTest);

foreach (var item in compiled)
{
    Console.WriteLine(Convert.ToString((int)item, 2).PadLeft(32, '0'));
}
c.ProcessFull(compiled);
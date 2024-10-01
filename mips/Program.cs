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
    "PROMT: .asciiz Enter a number?",
    "Syscall",
    "Syscall",
    "Syscall",
    "Syscall",
    "Syscall",
    "Syscall",
    ".data",
    "PROGSTRT: Addi $t0, $t0, 123",
    "Addi $t1, $t0, 4",
    "Addi $t0, $t0, 40",
    "Add $t0, PROMT, PROMT",
    "BYE: .asciiz Goodbye"
    //"Syscall"
    //"Syscall"
    //"Jr $t5"
};

string[] LabelTest = new[]
{
    "LABELT: Addi $t0, $t0, 3"
};

string[] PrintTest = new[]
{
    "TEXT: .asciiz Hello, world from my string!",
    "TEXT2: .asciiz This is my second string",
    "PROMPT: .asciiz Please enter a string!",
    ".data",
    "Ori $v0, $zero, 4",
    "LB $a0, TEXT",
    "Syscall",
    "LB $a0, TEXT2",
    "Syscall",
    "LB $a0, PROMPT",
    "Syscall"
    //"",
    //"",
    //""
};

string[] ReadStrTest = new[]
{
    //"MONITOR: .asciiz XX",
    "PROMPT: .asciiz Please enter 10",
    //"INPUT: .asciiz 0000000000000000000000000000000000000000",
    ".data",
    "Ori $t0, $zero, 65",
    "Ori $t1, $zero, 1",
    "SB $t0, MONITOR(1)",
    "SB $t1, MONITOR(0)",
    "SB $t1, MONITOR(0)",
    "SB $t1, MONITOR(0)",
    "SB $t1, MONITOR(0)",
    "Ori $t0, $zero, 68",
    "SB $t0, MONITOR(1)",
    "SB $t1, MONITOR(0)",
    "SB $t1, MONITOR(0)",
    "SB $t1, MONITOR(0)",
    "SB $t1, MONITOR(0)",

    //"Ori MONITOR, $zero, 1",
    //"Ori MONITOR, $zero, 1",
    //"LB $a0, MONITOR(1)",
    //"SB $zero, MONITOR(0)",
    //"SB $zero, MONITOR(1)",
    //"SB $zero, PROMPT(5)",
    //"LB $a0, PROMPT",
    //"Syscall",
    //"Ori $v0, $zero, 8",
    //"LB $a0, INPUT",
    //"LB $a1, 40",
    //"Syscall",
    //"Ori $v0, $zero, 4",
    //"Ori MONITOR, $zero, 101",
    //"Syscall",
};

Computer c = new Computer(128);
c.Compile(ReadStrTest);

c.ProcessFull();

//foreach (var item in compiled)
//{
//    Console.WriteLine(Convert.ToString((int)item, 2).PadLeft(32, '0'));
//}
//c.ProcessFull(compiled);
c.DumpMemory();
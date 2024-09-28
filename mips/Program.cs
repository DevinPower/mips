using mips;

string[] Program = new string[]
{
            //Instruction
            ".asciiz \"Please enter the number 10!\"",
            "PROMPT: li $a0 0",
            "li $v0 4",
            "syscall",

            "CHECK: li $t0 10",        //const for checking

            //Read an integer
            "li $v0 5",
            "syscall",
            "move $t1 $v0",

            //check
            "beq $t0 $t1 10",
            "j PROMPT",

            ".asciiz \":)\"",
            "li $a0 28",
            "li $v0 4",
            "syscall",
};

Computer c = new Computer();
string[] Result = new Compiler().Compile(c, Program);

foreach(string s in Result)
{
    Console.WriteLine(s);
}
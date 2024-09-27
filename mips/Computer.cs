using System.Linq;
using System.Net.Sockets;
using System.Text;

public class Computer
{
    public int[] Memory = new int[64];
    int _memoryPointer = 0;
    public int[] Registers = new int[32];
    public static Instruction[] Instructions;
    Action[] _Syscall;
    int _programCounter = 0;
    string[] InstructionRegisterDefinitions = new[] {
        "$zero",
        "$a0",
        "$v0", "$v1",
        "$a0", "$a1", "$a2", "$a3",
        "$t0", "$t1", "$t2", "$t3", "$t4", "$t5", "$t6", "$t7",
        "$s0", "$s1", "$s2", "$s3", "$s4", "$s5", "$s6", "$s7",
        "$t8", "$t9",
        "$k0", "$k1",
        "$gp",
        "$sp",
        "$ra",
        "$temp"
    };

    string[] Program;


    public Computer()
    {
        InitializeRegisters();

        Instructions = new Instruction[]
        {
            //Arithmetic
            new add(),
            new addi(),
            new sub(),
            new mul(),

            //Logical
            new and(),
            new or(),
            new andi(),
            new ori(),
            new sll(),
            new srl(),

            //data transfer
            new li(),
            new move(),
            new sw(),
            new lw(),

            //Control
            new j(),
            new jr(),
            new jal(),
            new slt(),
            new slti(),
            new beq(),
            new bne(),
            new bgt(),
            new bge(),
            new blt(),
            new ble(),

            new asciiz(),

            new syscall()
        };

        _Syscall = new[]
        {
            Sys_Null, Print_Int, Print_Float, Print_Double, Print_String,
            Read_Int, Read_Float, Read_Double, Read_String,
            SBRK, Exit,
            Print_Char, Read_Char,
            Sys_Null, Sys_Null, Sys_Null, Sys_Null,
            Exit2
        };


        //Print 22 to console
        //Program = new string[]{
        //    "li $a0 22",
        //    "li $v0 1",
        //    "syscall"
        //};


        //Read integer from user, print it back out
        //Program = new string[]
        //{
        //    "li $v0 5",
        //    "syscall",
        //    "move $a0 $v0",
        //    "li $v0 1",
        //    "syscall"
        //};

        //Program = new string[]
        //{
        //    "asciiz \"Hello, world from computer!\"",
        //    "li $a0 0",
        //    "li $v0 4",
        //    "syscall"
        //};

        //Read two integers, add together, print
        //Program = new string[]
        //{
        //    "li $v0 5",
        //    "syscall",
        //    "move $t0 $v0",
        //    "li $v0 5",
        //    "syscall",
        //    "move $t1 $v0",
        //    "add $a0 $t0 $t1",
        //    "li $v0 1",
        //    "syscall"
        //};

        //Enter strings and print
        //Program = new string[]
        //{
        //    //output the prompt
        //    "asciiz \"Please enter a string!\"",
        //    "li $a0 0",
        //    "li $v0 4",
        //    "syscall",
        //
        //    //get input
        //    "li $a0 24",
        //    "li $a1 255",   //255 = string legnth
        //    "li $v0 8",
        //    "syscall",
        //
        //    //write back the output
        //    "li $a0 24",
        //    "li $v0 4",
        //    "syscall",
        //    "j 1"           //loop
        //};

        Program = new string[]
        {
            //Instruction
            "asciiz \"Please enter the number 10!\"",
            "li $a0 0",        
            "li $v0 4",
            "syscall",

            "li $t0 10",        //const for checking

            //Read an integer
            "li $v0 5",
            "syscall",
            "move $t1 $v0",

            //check
            "beq $t0 $t1 10",
            "j 1",

            "asciiz \":)\"",
            "li $a0 28",
            "li $v0 4",
            "syscall",
        };
    
        ProcessFull();

        //DumpMemory();
    }

    void ProcessFull()
    {
        int count = 0;
        while (!StepProgram()) count++;
        Console.WriteLine($"Finished program with {count} instructions!");
    }

    public void SysCall()
    {
        _Syscall[Registers[GetRegisterAddress("$v0")]]();
    }

    public int GetProgramCounter()
    {
        return _programCounter;
    }

    public void Jump(int index)
    {
        _programCounter = index;
    }

    void ThrowSimulatedException(string Exception)
    {
        throw new Exception(Exception);
    }

    public int GetRegisterAddress(string Register)
    {
        for (int i = 0; i < InstructionRegisterDefinitions.Length; i++)
        {
            if (InstructionRegisterDefinitions[i].Equals(Register)) return i;
        }

        ThrowSimulatedException($"Register for {Register} does not exist.");
        return -1;
    }

    void InitializeRegisters()
    {
        Registers[0] = 0;
        Registers[28] = 0;
        Registers[29] = 0;
    }

    public void StoreMemory(int Value)
    {
        Memory[_memoryPointer++] = Value;
    }

    bool StepProgram()
    {
        if (_programCounter >= Program.Length)
            return true;

        string[] Splits = Program[_programCounter++].Split(' ');
        string Command = Splits[0];
        string[] Arguments = Splits.Skip(1).ToArray();

        int Instruction = GetInstructionIndex(Splits[0]);
        Instructions[Instruction].Execute(this, Arguments);

        return false;
    }

    int GetInstructionIndex(string Instruction)
    {
        //TODO: Refactor
        for (int i = 0; i < Instructions.Length; i++)
        {
            if (Instructions[i].GetType().Name == Instruction)
                return i;
        }
        return -1;
    }

    void DumpMemory()
    {
        Console.WriteLine("Registers: ");
        int i = 0;
        foreach (var register in Registers)
        {
            Console.WriteLine($"{InstructionRegisterDefinitions[i]}\t\t\t\t{register.ToString()}");
            i++;
        }

        Console.WriteLine("Memory: ");

        foreach (var mem in Memory)
        {
            Console.WriteLine(mem.ToString());
        }
    }
    #region SYSCALLS
    void Sys_Null()
    {
        ThrowSimulatedException("SYSCALL OUTSIDE BOUNDS OF ALLOWED");
    }

    void Print_Int()
    {
        Console.WriteLine(Registers[GetRegisterAddress("$a0")]);
    }

    void Print_Float()
    {
        throw new NotImplementedException();
    }

    void Print_Double()
    {
        throw new NotImplementedException();
    }

    void Print_String()
    {
        StringBuilder value = new StringBuilder();
        int memoryPointer = Registers[GetRegisterAddress("$a0")];

        while (true)
        {
            int CurrentChar = Memory[memoryPointer++];
            if (CurrentChar == 0)
            {
                Console.WriteLine(value.ToString());
                break;
            }
            value.Append((char)CurrentChar);
        }
    }

    void Read_Int()
    {
        Registers[GetRegisterAddress("$v0")] = Int32.Parse(Console.ReadLine());
    }

    void Read_Float()
    {
        throw new NotImplementedException();
    }

    void Read_Double()
    {
        throw new NotImplementedException();
    }

    void Read_String()
    {
        string str = Console.ReadLine();
        int i = Registers[GetRegisterAddress("$a0")];
        int max = Registers[GetRegisterAddress("$a1")] + i;

        foreach(char c in str)
        {
            Memory[i++] = c;

            if (i >= max)
                break;
        }

        Memory[i++] = 0;    //null terminator
    }

    void SBRK()
    {
        throw new NotImplementedException();
    }

    void Exit()
    {
        _programCounter = int.MaxValue;     //Put counter outside of bounds to finish
    }

    void Print_Char()
    {
        throw new NotImplementedException();
    }

    void Read_Char()
    {
        throw new NotImplementedException();
    }

    void Exit2()
    {
        throw new NotImplementedException();
    }
    #endregion
}

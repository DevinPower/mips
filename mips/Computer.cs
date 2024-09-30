using mips;
using mips.Instructions;
using System.Linq;
using System.Net.Sockets;
using System.Text;

public class Computer
{
    public int[] Memory { get; set; }
    int _memoryPointer = 33;

    Action[] _Syscall;
    int _programCounter = 33;
    public static string[] InstructionRegisterDefinitions = new[] {
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
        "$fp",
        "$ra"
    };

    public int HIRegister { get; set; }
    public int LORegister { get; set; }
    static Dictionary<int, Instruction> InstructionProcessors;

    public Computer(int MemorySize)
    {
        Memory = new int[MemorySize];
        InitializeRegisters();
        LoadInstructionProcessors();

        _Syscall = new[]
        {
            Sys_Null, Print_Int, Print_Float, Print_Double, Print_String,
            Read_Int, Read_Float, Read_Double, Read_String,
            SBRK, Exit,
            Print_Char, Read_Char,
            Sys_Null, Sys_Null, Sys_Null, Sys_Null,
            Exit2
        };
    }

    public void ProcessFull()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        int count = 0;
        while (!StepProgram()) count++;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"Finished program with {count} instructions!");
        //DumpMemory();
    }

    public void SysCall()
    {
        _Syscall[Memory[GetRegisterAddress("$v0")]]();
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
        Memory[0] = 0;
        Memory[28] = 0;
        Memory[29] = 0;
    }

    public void StoreMemory(int Value)
    {
        Memory[_memoryPointer++] = Value;
    }

    //TODO: Not sure I like exposing this...
    public void StoreMemory(int Value, int Position)
    {
        Memory[Position] = Value;
    }

    public int GetMemoryPointer()
    {
        return _memoryPointer;
    }

    bool StepProgram()
    {
        int CurrentLine = Memory[_programCounter++];
        if (CurrentLine == 0)
        {
            return true;
        }

        int OpCode = (CurrentLine >> 26) & 0b111111;
        InstructionProcessors[OpCode].Execute(this, CurrentLine);

        return false;
    }

    void LoadInstructionProcessors()
    {
        InstructionProcessors = new Dictionary<int, Instruction>();
        
        OP_000000 op0 = new OP_000000();
        op0.LoadOperations();

        OP_001000 op1 = new OP_001000();
        op1.LoadOperations();

        OP_001001 op2 = new OP_001001();
        op2.LoadOperations();

        OP_001100 op3 = new OP_001100();
        op3.LoadOperations();

        OP_001111 op4 = new OP_001111();
        op4.LoadOperations();

        OP_001101 op5 = new OP_001101();
        op5.LoadOperations();

        OP_001010 op6 = new OP_001010();
        op6.LoadOperations();

        OP_001011 op7 = new OP_001011();
        op7.LoadOperations();

        OP_001110 op8 = new OP_001110();
        op8.LoadOperations(); 
        
        OP_100000 op9 = new OP_100000();
        op9.LoadOperations();

        OP_101000 op10 = new OP_101000();
        op10.LoadOperations();

        InstructionProcessors.Add(0, op0);
        InstructionProcessors.Add(8, op1);
        InstructionProcessors.Add(9, op2);
        InstructionProcessors.Add(12, op3);
        InstructionProcessors.Add(15, op4);
        InstructionProcessors.Add(13, op5);
        InstructionProcessors.Add(10, op6);
        InstructionProcessors.Add(11, op7);
        InstructionProcessors.Add(14, op8);
        InstructionProcessors.Add(32, op9);
        InstructionProcessors.Add(40, op10);

    }

    List<SoftOperationWrapper> GetAllInstructions()
    {
        List<SoftOperationWrapper> AllOperations = new List<SoftOperationWrapper>();

        foreach(int key in InstructionProcessors.Keys)
        {
            AllOperations.AddRange(InstructionProcessors[key].GetOperations());
        }

        return AllOperations;
    }

    /*public void Compile(string[] Program)
    {
        List<uint> CompiledProgram = new List<uint>();
        foreach(string line in Program)
        {
            uint opCode = getOpCode(line.Split(' ')[0]);
            uint command = uint.Parse(line.Split(' ')[1]);
            uint compiledCode = (opCode << 26) | (command & 0x03FFFFFF);
            CompiledProgram.Add(compiledCode);
        }

        foreach (var line in CompiledProgram)
        {
            string binary = Convert.ToString((int)line, 2).PadLeft(32, '0');

            int[] result = HelperFunctions.BitsToInt((int)line, new[] { 6, 26 });
            int op = result[0];
            int tar = result[1];

            Console.WriteLine($"{binary}\t{op}\t\t\t{tar}");
        }
    }*/

    public int[] Compile(string[] Program)
    {
        var ops = GetAllInstructions();

        List<int> result = new List<int>();
        InputProcessor ip = new InputProcessor(this, ops);

        foreach (string Line in Program)
        {
            ip.FindLabels(Line);
        }

        foreach (string Line in Program)
        {
            result.Add(ip.ProcessLine(Line));
        }

        //ip.DumpLabels();

        return result.ToArray();
    }

    public void ProcessOp(string op, int ipResult, string Line)
    {
        string[] SplitLine = Line.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        string LineRemainder = String.Join(" ", SplitLine.Skip(1));
        switch (op)
        {
            case "data":
                Jump(ipResult);
                break;
            case "asciiz":
                foreach(char c in LineRemainder)
                {
                    StoreMemory((int)c);
                }
                StoreMemory(0);
                break;
        }
    }

    public void DumpMemory()
    {
        Console.WriteLine("Registers: ");
        int i = 0;
        //foreach (var register in Registers)
        //{
        //    Console.WriteLine($"{InstructionRegisterDefinitions[i]}\t\t\t\t{register.ToString()}");
        //    i++;
        //}

        Console.WriteLine("Memory: ");
        int ind = 0;

        foreach (var mem in Memory)
        {
            Console.WriteLine($"{ind.ToString().PadLeft(4, '0')}\t{((char)mem).ToString().Trim()}\t{mem.ToString()}");
            ind++;
        }

        Console.WriteLine($"Program Counter at: {_programCounter}");
    }
    #region SYSCALLS
    void Sys_Null()
    {
        ThrowSimulatedException("SYSCALL OUTSIDE BOUNDS OF ALLOWED");
    }

    void Print_Int()
    {
        //Console.WriteLine(Registers[GetRegisterAddress("$a0")]);
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
        int memoryPointer = Memory[GetRegisterAddress("$a0")];

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
        Memory[GetRegisterAddress("$v0")] = Int32.Parse(Console.ReadLine());
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
        int i = Memory[GetRegisterAddress("$a0")];
        int max = Memory[GetRegisterAddress("$a1")] + i;

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

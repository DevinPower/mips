using mips;
using mips.Instructions;
using mips.Peripherals;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;

public class Computer
{
    public int[] Memory { get; set; }
    public List<Peripheral> Peripherals { get; set; }
    
    int _programCounter;
    int _heapPointer;
    int _memoryPointer;

    Action[] _Syscall;
    
    public static string[] InstructionRegisterDefinitions = new[] {
        "$zero",
        "$v0", "$v1",
        "$a0", "$a1", "$a2", "$a3",
        "$t0", "$t1", "$t2", "$t3", "$t4", "$t5", "$t6", "$t7",
        "$s0", "$s1", "$s2", "$s3", "$s4", "$s5", "$s6", "$s7",
        "$t8", "$t9",
        "$k0", "$k1",
        "$sp",
        "$fp",
        "$at",
        "$ra"
    };

    public int HIRegister { get; set; }
    public int LORegister { get; set; }
    static Dictionary<int, Instruction> InstructionProcessors;

    public Computer(int MemorySize, int HeapSize)
    {
        Memory = new int[MemorySize];
        _heapPointer = HeapSize + 34;
        _programCounter = _heapPointer + 3;

        InitializeRegisters(_heapPointer + 3);
        LoadInstructionProcessors();
        InitializePeripherals();

        _Syscall = new[]
        {
            Sys_Null, Print_Int, Print_Float, Print_Double, Print_String,
            Read_Int, Read_Float, Read_Double, Read_String,
            SBRK, Exit,
            Print_Char, Read_Char,
            Sys_Null, Sys_Null, Sys_Null, Sys_Null,
            Exit2, Get_Time,
            DotNetBreak
        };
    }

    public void ProcessFull()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        Console.ForegroundColor = ConsoleColor.Yellow;
        int count = 0;
        while (!StepProgram()) count++;
        watch.Stop();
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine();
        Console.WriteLine($"Finished program with {count} instructions in {watch.ElapsedMilliseconds}ms");
        //DumpMemory();
    }

    public void SysCall()
    {
        _Syscall[Memory[GetRegisterAddress("$v0")]]();
    }

    void InitializePeripherals()
    {
        Peripherals = new List<Peripheral>();
        Peripherals.Add(new TestPeripheral());

        foreach (var peripheral in Peripherals)
        {
            peripheral.Initialize(this);
        }
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

    void InitializeRegisters(int StackAddress)
    {
        Memory[0] = 0;
        Memory[GetRegisterAddress("$sp")] = StackAddress - 1;
        _memoryPointer = StackAddress;
    }

    public void StoreHeap(int[] Value)
    {
        for (int i = Value.Length - 1; i >= 0; i--)
        {
            Memory[_heapPointer - i] = Value[i];
        }

        _heapPointer -= Value.Length;
    }

    public void StoreHeap(int Value)
    {
        Memory[_heapPointer--] = Value;
    }

    public int ReserveMemory(int Size)
    {
        int memoryAddress = _heapPointer;
        _heapPointer += Size;
        return memoryAddress;
    }

    //TODO: Not sure I like exposing this...
    public void StoreMemory(int Value, int Position)
    {
        Memory[Position] = Value;
    }

    public void StoreMemory(int Value)
    {
        Memory[_memoryPointer++] = Value;
    }

    public bool StepProgram()
    {
        int CurrentLine = Memory[_programCounter++];
        if (CurrentLine == 0)
        {
            ProcessPeripherals();
            return true;
        }

        int OpCode = (CurrentLine >> 26) & 0b111111;
        InstructionProcessors[OpCode].Execute(this, CurrentLine);

        ProcessPeripherals();

        return false;
    }

    void ProcessPeripherals()
    {
        foreach (var peripheral in Peripherals)
        {
            peripheral.Step(this);
        }
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

        OP_000010 op11 = new OP_000010();
        op11.LoadOperations();

        OP_000011 op12 = new OP_000011();
        op12.LoadOperations();

        OP_000100 op13 = new OP_000100();
        op13.LoadOperations();

        InstructionProcessors.Add(0, op0);
        InstructionProcessors.Add(4, op13);
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
        InstructionProcessors.Add(2, op11);
        InstructionProcessors.Add(3, op12);

        //Floating point operators
        OP_111000 opf1 = new OP_111000();
        opf1.LoadOperations();

        InstructionProcessors.Add(56, opf1);
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

    public int[] Compile(string[] Program)
    {
        var ops = GetAllInstructions();

        List<int> result = new List<int>();
        InputProcessor ip = new InputProcessor(this, ops, _programCounter);

        for (int i = 0; i < Program.Length; i++)
        {
            string Line = Program[i];
            ip.FindLabels(ip.GetLineWithoutComments(Line), i);
        }

        List<string> expandedProgram = new List<string>();

        for (int i = 0; i < Program.Length; i++)
        {
            string Line = Program[i];
            var lineResults = ip.CheckPseudoInstructions(Line);

            int offset = lineResults.Length - 1;
            if (offset > 0)
            {
                ip.BumpLabels(i, offset);
            }

            expandedProgram.AddRange(lineResults);
        }
        
        foreach (var peripheral in Peripherals)
        {
            //ip.AddLabel(peripheral.Name, peripheral.MemoryAddress);
        }

        foreach (string Line in expandedProgram)
        {
            result.Add(ip.ProcessLine(ip.GetLineWithoutComments(Line)));
        }

        return result.ToArray();
    }

    public void DumpMemory()
    {
        Console.WriteLine("Memory: ");
        int ind = 0;

        foreach (var mem in Memory)
        {
            string index = ind.ToString();

            if (ind < InstructionRegisterDefinitions.Length)
                index = InstructionRegisterDefinitions[ind].PadLeft(4, ' ');

            Console.WriteLine($"{index.PadLeft(4, '0')}\t{((char)mem).ToString().Trim()}\t{mem.ToString()}");
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
        Console.Write(Memory[GetRegisterAddress("$a0")]);
    }

    void Print_Float()
    {
        Console.Write(HelperFunctions.IntToFloat(Memory[GetRegisterAddress("$a0")]));
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
                Console.Write(value.ToString());
                break;
            }
            value.Append((char)CurrentChar);
        }
    }

    void Get_Time()
    {
        Memory[GetRegisterAddress("$v0")] = (int)DateTime.Now.TimeOfDay.TotalSeconds;
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

    int FindMemoryOfLength(int Length)
    {
        int startSearch = 31;

        while (true)
        {
            int size = 0;
            for (int i = 0; i < Length; i++)
            {
                if (Memory[startSearch + i] != 0)
                {
                    startSearch += Memory[startSearch + i];
                    break;
                }
                size++;
            }

            if (size == Length)
                return startSearch;

            if (startSearch >= _heapPointer)
                throw new Exception("Out of memory exception");
        }
    }

    void SBRK()
    {
        Memory[GetRegisterAddress("$v0")] = FindMemoryOfLength(Memory[GetRegisterAddress("$a0")]);
    }

    void Exit()
    {
        _programCounter = int.MaxValue;     //Put counter outside of bounds to finish
    }

    void Print_Char()
    {
        Console.Write((char)Memory[GetRegisterAddress("$a0")]);
    }

    void Read_Char()
    {
        Memory[GetRegisterAddress("$v0")] = Console.ReadLine()[0];
    }

    void Exit2()
    {
        throw new NotImplementedException();
    }

    void DotNetBreak()
    {
        Debugger.Break();
    }
    #endregion
}

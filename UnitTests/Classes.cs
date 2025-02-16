using Lexer;
using mips;
using System.Text;

namespace UnitTests
{
    public class Classes
    {
        Computer computer;
        string stdout;

        [SetUp]
        public void Setup()
        {
            stdout = "";
            computer = new Computer(4096, 1024);
            computer.SetSysCalls(new Action[]
            {
                computer.Sys_Null, Print_Int, computer.Print_Float, computer.Print_Double, Print_String,
                computer.Read_Int, computer.Read_Float, computer.Read_Double, computer.Read_String,
                computer.SBRK, computer.Exit,
                computer.Print_Char, computer.Read_Char,
                computer.Sys_Null, computer.Sys_Null, computer.Sys_Null, computer.Sys_Null,
                computer.Exit2, computer.Get_Time,
                computer.DotNetBreak
            });
        }

        void CompileProgram(string Program)
        {
            Lexer.Lexer lexer = new Lexer.Lexer();
            List<Token> tokens = lexer.Lexicate(Program, false, false);

            Parser parser = new Parser(tokens, (file) =>
            {
                return string.Join("\n", File.ReadAllLines($"Scripts\\{file}"));
            });
            computer.Compile(parser.Parse());
        }

        void Print_Int()
        {
            stdout += computer.Memory[computer.GetRegisterAddress("$a0")].ToString();
        }

        void Print_String()
        {
            StringBuilder value = new StringBuilder();
            int memoryPointer = computer.Memory[computer.GetRegisterAddress("$a0")];

            while (true)
            {
                int CurrentChar = computer.Memory[memoryPointer++];
                if (CurrentChar == 0)
                {
                    stdout += value.ToString();
                    break;
                }
                value.Append((char)CurrentChar);
            }
        }

        [Test, Combinatorial]
        public void AllocationTests([Values("GlobalScope.td")]string ExternalFile)
        {
            CompileProgram(File.ReadAllText($"Scripts\\ClassTests\\{ExternalFile}"));
            computer.ProcessFull();

            Assert.That(computer.Memory[31], Is.EqualTo(3));                    //Heap size
            Assert.That(computer.ReadStringFromPointer(32), Is.EqualTo("Devin"));    //Name pointer
            Assert.That(computer.Memory[33], Is.EqualTo(150));                  //Score
            Assert.That(stdout, Is.EqualTo("Devin150"));
        }
    }
}
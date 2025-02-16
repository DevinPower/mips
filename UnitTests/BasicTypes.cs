using Lexer;
using mips;

namespace UnitTests
{
    public class BasicTypes
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
                computer.Sys_Null, Print_Int, computer.Print_Float, computer.Print_Double, computer.Print_String,
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

            Parser parser = new Parser(tokens, (file) => { 
                return string.Join("\n", File.ReadAllLines($"Scripts\\{file}")); 
            });
            computer.Compile(parser.Parse());
        }

        void Print_Int()
        {
            stdout += computer.Memory[computer.GetRegisterAddress("$a0")].ToString();
        }

        [Test]
        public void AdditionTest()
        {
            CompileProgram(@"#include stdio.td
int a = 4 + 4;
printNum(a);");
            computer.ProcessFull();

            Assert.That(stdout, Is.EqualTo("8"));
        }

        [Test]
        public void OperatorPrecedenceTest()
        {
            CompileProgram(@"#include stdio.td
int a = 4 + 4 * 2;
printNum(a);");
            computer.ProcessFull();

            Assert.That(stdout, Is.EqualTo("16"));
        }

        [Test, Combinatorial]
        public void OrOperatorTests([Values("int a = 1 || 1;",
            "int a = 0 || 65;",
            "int a = -65 || 0;")] string Command)
        {
            CompileProgram(@$"#include stdio.td
{Command}
printNum(a);");
            computer.ProcessFull();

            Assert.That(stdout, Is.EqualTo("1"));
        }

        [Test, Combinatorial]
        public void AndOperatorTests([Values("int a = 1 && 1;",
            "int a = 65 && -1;",
            "int a = 1 && 65;")] string Command)
        {
            CompileProgram(@$"#include stdio.td
{Command}
printNum(a);");
            computer.ProcessFull();

            Assert.That(stdout, Is.EqualTo("1"));
        }

        [Test]
        public void WhileTests()
        {
            CompileProgram(@"#include stdio.td
int a = 0;
while (a < 10){
    a += 1;
}
printNum(a);");
            computer.ProcessFull();

            Assert.That(stdout, Is.EqualTo("10"));
        }
    }
}
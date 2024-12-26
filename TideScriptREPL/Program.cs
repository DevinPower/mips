using Lexer;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;
using mips;
using Microsoft.VisualBasic;

namespace TideScriptREPL
{
    internal class Program
    {
        static int count = 0;

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                REPL();
            }
            else
            {
                throw new NotImplementedException("Does not support passing in args yet");
            }
        }

        static void REPL()
        {
            //var Code = "var i = 0;\n" +
            //    "while (i < 10) { i = 2;};\n" +
            //    "var b = 22;\n";

            //var Code = "var i = 4124;\n" +
            //    "var z = 1212;\n" +
            //    "var str = \"my string\";\n";

            var SysIo = 
                "|Ori $v0, $zero, 4\n" +
                "|LB $a0, PROMPT(0)\n" +
                "|Syscall\n";

            //var Code = SysIo + "var PROMPT = \"Hello, world!\";\n";

            var Code = "var i = 10;\n" +
                "function testFunction(){\n" +
                "   i = 77;\n" +
                "};\n" +
                "testFunction();\n" +
                "var x = 444;\n";

            var Code2 = "var x = 10;\n" +
                "var y = 33;\n" +
                "var z = 2 * y;\n" +
                "var label = \"hey\";\n" + 
                "label = \"hello\";\n" +
                "y = 3;\n" +
                "y = 545;\n" + 
                "var l = x;\n";

            //var Code = "var left = 10;\n" +
            //    "var right = 20;\n" +
            //    "var third = \"test \\\"value\\\" string\";\n" +
            //    "right + third;";

            //var Code = "var x = (9 * 7 * 67);\n" +
            //    "var y = 6;";

            //var Code = "var label = \"my test\";\n" +
            //    "var num = 44;\n" +
            //    "label = \"mt\";";

            while (true)
            {
                string Input = Console.ReadLine();
                var ic = Compile(Code);

                Computer c = new Computer(128);
                c.Compile(ic);

                c.ProcessFull();

                c.DumpMemory();
            }
        }

        static string[] Compile(string FileName)
        {
            //string Code = File.ReadAllText(FileName);

            string Code = FileName;
            
            Lexer.Lexer l = new Lexer.Lexer();
            var tokens = l.Lexicate(Code);

            CompilationMeta meta = new CompilationMeta();

            Parser parser = new Parser(tokens, meta);
            var result = parser.Parse();

            string[] intermediaryCode = ICWalker.GenerateCodeRecursive(result, meta);
            List<string> TotalProgram = new List<string>();

            Console.ForegroundColor = ConsoleColor.Green;
            foreach(string data in meta.GetDataSection())
            {
                Console.WriteLine(data);
                TotalProgram.Add(data);
            }

            foreach(string s in intermediaryCode)
            {
                Console.WriteLine(s);
                TotalProgram.Add(s);
            }
            Console.ForegroundColor = ConsoleColor.White;

            return TotalProgram.ToArray();
        }
    }
}

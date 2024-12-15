using Lexer;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;

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
            //var Code = "while (i < 10) { i = 2;\nj = 5; 9 * 2 / i;};";

            //var Code = "var x = 10;\n" +
            //    "var y = 33;\n" +
            //    "2 * y;\n";

            var Code = "var left = 10;\n" +
                "var right = 20;\n" +
                "var third = 1;\n" +
                "left * right;\n" + 
                "right + third;";

            while (true)
            {
                string Input = Console.ReadLine();
                Compile(Code);
            }
        }

        static void Compile(string FileName)
        {
            //string Code = File.ReadAllText(FileName);

            string Code = FileName;
            
            Lexer.Lexer l = new Lexer.Lexer();
            var tokens = l.Lexicate(Code);

            CompilationMeta meta = new CompilationMeta();

            Parser parser = new Parser(tokens, meta);
            var result = parser.Parse();


            result.PostOrderTraversal((x) =>
            {
                //if (x.Data.SkipGeneration) return;

                IntermediaryCodeMeta generatedMeta = x.Data.GenerateCode(meta);
                foreach (var line in generatedMeta.Code)
                {
                    Console.WriteLine(line);
                }
            });
        }
    }
}

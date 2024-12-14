using Lexer;

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

            var Code = "var x = 10;";

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

            Lexer.Parser parser = new Lexer.Parser(tokens);
            var result = parser.Parse();

            result.PostOrderTraversal((x) =>
            {
                Console.WriteLine(x.Data.ToString());
            });
        }
    }
}

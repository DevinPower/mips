namespace Lexer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //tidescript
            //string Code = "var test = 5;//A simple example statement\n" +
            //    "var test2 = 61;" +
            //    "test = test2;";

            var Code = "9 + 12 + 22;";

            Lexer l = new Lexer();
            var tokens = l.Lexicate(Code);

            Console.WriteLine("----");

            Parser parser = new Parser(tokens);
            parser.Parse();
        }
    }
}
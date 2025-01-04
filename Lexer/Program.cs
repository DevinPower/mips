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

            //var Code = "12 * (22 + 7);\n" +
            //    "var test = 90;";

            var Code = "while ( i < 10 ) { i = 2;\nj = 5; 9 * 2 / i;};";

            //todo: cannot do X = 9 * ....
            //var Code = "while ( i < 10 ) { i = 2;\nj = 5; x = 9 * (2 / i);};";


            Lexer l = new Lexer();
            var tokens = l.Lexicate(Code, false);

            Console.WriteLine("----");

            //Parser parser = new Parser(tokens);
            //parser.Parse();
        }
    }
}
namespace Lexer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string Code = "while (true){\n" +
                "   MyVariable2 = 97;//this is a comment\n" +
                "}\n" +
                "//comment all by itself\n" +
                "foreach (bla in blabla){}\n" +
                "foreachard = 2;\n" +
                "str = \"hello, world!\";";
            Lexer l = new Lexer();
            l.Lexicate(Code);
        }
    }
}

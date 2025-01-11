using Lexer;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;
using mips;
using Microsoft.VisualBasic;
using System.Text;
using System.Drawing;

namespace TideScriptREPL
{
    internal class Program
    {
        static int count = 0;
        const int TABSIZE = 5;
        static readonly char[] AlwaysAlpha = new [] { '=', '+', '-', '$', '|', '>', '<', ',' };

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Interactive();
            }
            else
            {
                throw new NotImplementedException("Does not support passing in args yet");
            }
        }

        static void Interactive()
        {
            Console.CursorSize = 100;

            List<List<char>> Contents = new List<List<char>>();
            Contents.Add(new List<char>());

            int currentLine = 0;
            int currentPosition = 0;

            Lexer.Lexer l = new Lexer.Lexer();

            DrawContents(l, Contents);
            DrawFooter();

            while (true)
            {
                ConsoleKeyInfo Key = Console.ReadKey(true);
                if (AlwaysAlpha.Contains(Key.KeyChar) || (Key.KeyChar != '\0' && (Char.IsLetterOrDigit(Key.KeyChar) || Char.IsPunctuation(Key.KeyChar))))
                {
                    Contents[currentLine].Insert(currentPosition++, Key.KeyChar);
                }
                else
                {
                    if (Key.Key == ConsoleKey.F1)
                    {
                        try
                        {
                            Lexer.Lexer l2 = new Lexer.Lexer();

                            Console.Clear();

                            bool PrintAST = Key.Modifiers == ConsoleModifiers.Shift;

                            var ic = Compile(GetTokens(l2, Contents, false, PrintAST));

                            if (PrintAST)
                            {
                                Console.WriteLine("Press any key to continue");
                                Console.ReadKey();
                            }

                            Computer c = new Computer(128, 32);
                            c.Compile(ic);

                            if (Key.Modifiers != ConsoleModifiers.Control)
                                c.ProcessFull();//StepProgram(c, ic);

                            c.DumpMemory();

                            DrawAlert("Press any key to exit", ConsoleColor.Red);

                            Console.ReadKey();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message.ToString());
                            Console.WriteLine("");
                            Console.WriteLine(ex.StackTrace);
                            Console.ReadKey();
                        }
                    }

                    if (Key.Key == ConsoleKey.Spacebar)
                    {
                        Contents[currentLine].Insert(currentPosition++, ' ');
                    }

                    if (Key.Key == ConsoleKey.Backspace) 
                    {
                        if (Contents[currentLine].Count == 0 && currentLine != 0)
                        {
                            Contents.RemoveAt(currentLine--);
                            currentPosition = Contents[currentLine].Count;
                        }
                        else if (currentPosition > 0)
                        {
                            Contents[currentLine].RemoveAt(--currentPosition);
                        }
                    }

                    if (Key.Key == ConsoleKey.UpArrow)
                    {
                        if (currentLine != 0)
                        {
                            currentLine--;
                            currentPosition = Math.Min(currentPosition, Contents[currentLine].Count);
                        }
                        else
                        {
                            currentPosition = 0;
                        }
                    }

                    if (Key.Key == ConsoleKey.DownArrow)
                    {
                        if (currentLine != Contents.Count - 1)
                        {
                            currentLine++;
                            currentPosition = Math.Min(currentPosition, Contents[currentLine].Count);
                        }
                        else
                        {
                            currentPosition = Contents[currentLine].Count;
                        }
                    }

                    if (Key.Key == ConsoleKey.LeftArrow)
                    {
                        if (currentPosition != 0)
                        {
                            currentPosition--;
                        }
                        else if (currentLine != 0)
                        {
                            currentLine--;
                            currentPosition = Contents[currentLine].Count;
                        }
                    }

                    if (Key.Key == ConsoleKey.RightArrow)
                    {
                        if (currentPosition != Contents[currentLine].Count)
                        {
                            currentPosition++;
                        }
                        else if (currentLine != Contents.Count - 1)
                        {
                            currentLine++;
                            currentPosition = 0;
                        }
                    }

                    if (Key.Key == ConsoleKey.Tab)
                    {
                        if (Key.Modifiers == ConsoleModifiers.Shift)
                        {
                            for (int i = 0; i < TABSIZE; i++)
                            {
                                if (currentPosition == 0 || Contents[currentLine][currentPosition - 1] != ' ')
                                    break;
                                Contents[currentLine].RemoveAt(--currentPosition);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < TABSIZE; i++)
                            {
                                Contents[currentLine].Insert(currentPosition++, ' ');
                            }
                        }
                    }

                    if (Key.Key == ConsoleKey.Enter)
                    {
                        if (currentPosition == Contents[currentLine].Count)
                        {
                            Contents.Insert(++currentLine, new List<char>());
                            currentPosition = 0;
                        }
                        else
                        {
                            List<char> CurrentLineText = Contents[currentLine].GetRange(0, currentPosition);
                            List<char> NewLineText = Contents[currentLine].GetRange(currentPosition, Contents[currentLine].Count - currentPosition);
                            Contents[currentLine] = CurrentLineText;
                            Contents.Insert(++currentLine, NewLineText);
                            currentPosition = 0;
                        }
                    }
                }

                DrawContents(l, Contents);
                DrawFooter();
                Console.SetCursorPosition(currentPosition, currentLine);
            }
        }

        static List<Token> GetTokens(Lexer.Lexer Lexer, List<List<char>> Contents, bool ForDraw, bool PrintAnalysis = false)
        {
            StringBuilder sb = new StringBuilder();
            foreach (List<char> Line in Contents)
            {
                foreach (char c in Line)
                {
                    sb.Append(c);
                }
                sb.Append('\n');
            }

            return Lexer.Lexicate(sb.ToString(), ForDraw, PrintAnalysis);
        }

        static void StepProgram(Computer Computer, string[] Program)
        {
            ConsoleColor DefaultBackground = Console.BackgroundColor;

            while (true)
            {
                Console.Clear();
                Console.SetCursorPosition(0, 0);

                for (int i = 0; i < Program.Length; i++)
                {
                    if (i == Computer.GetProgramCounter())
                    {
                        Console.BackgroundColor = ConsoleColor.Green;
                        Console.ForegroundColor = ConsoleColor.Black;
                    }
                    else
                    {
                        Console.BackgroundColor = DefaultBackground;
                        Console.ForegroundColor = ConsoleColor.White;
                    }

                    Console.WriteLine(Program[i]);
                }

                int widthSize = 20;

                for (int iX = 0; 20 + (iX * widthSize) < Console.BufferWidth - 1; iX++)
                {
                    for (int iY = 0; iY < Console.BufferHeight - 1; iY++)
                    {
                        if ((iX * widthSize) + iY > Computer.Memory.Length - 1)
                            break;
                        Console.SetCursorPosition(20 + (iX * widthSize), iY);
                        int ind = (iX * widthSize) + iY;
                        int mem = Computer.Memory[ind];

                        string index = ind.ToString();

                        if (ind < Computer.InstructionRegisterDefinitions.Length)
                            index = Computer.InstructionRegisterDefinitions[ind].PadLeft(4, ' ');

                        Console.Write($"{index}={mem.ToString()}");
                    }
                }

                Console.ReadKey();
                if (Computer.StepProgram())
                    break;
            }
        }

        static void DrawContents(Lexer.Lexer Lexer, List<List<char>> Contents)
        {
            var tokens = GetTokens(Lexer, Contents, true);

            var getColor = (TokenTypes type) =>
            {
                try
                {
                    switch (type)
                    {
                        case TokenTypes.Keyword:
                            return ConsoleColor.Blue;
                        case TokenTypes.Comment:
                            return ConsoleColor.Green;
                        case TokenTypes.Identifier:
                            return ConsoleColor.Yellow;
                        case TokenTypes.Literal:
                            return ConsoleColor.Magenta;
                        case TokenTypes.Error:
                            return ConsoleColor.Red;
                        case TokenTypes.MachineCode: 
                            return ConsoleColor.Cyan;
                        default:
                            return ConsoleColor.White;
                    }
                }
                catch
                {
                    return ConsoleColor.White;
                }
            };

            Console.Clear();

            foreach(Token t in tokens)
            {
                var color = getColor(t.TokenType);
                Console.ForegroundColor = color;

                Console.Write(t.Value);
            }

            //int i = 0;
            //foreach(List<char> Line in Contents)
            //{
            //    foreach(char c in Line)
            //    {
            //        Console.ForegroundColor = getColor.Invoke(i);
            //        Console.Write(c);
            //        i++;
            //    }
            //
            //    Console.Write("\n");
            //}
        }

        static void DrawAlert(string Message, ConsoleColor Color)
        {
            Console.SetCursorPosition(0, Console.BufferHeight - 1);
            ConsoleColor startColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = Color;

            Console.Write(Message);

            Console.ForegroundColor = Color;
            int startX = Console.GetCursorPosition().Left;
            for (int i = startX; i < Console.BufferWidth; i++)
            {
                Console.Write('█');
            }

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = startColor;
        }

        static void DrawFooter()
        {
            string[] Commands = new string[] { "Run", "Save", "Load" };

            ConsoleColor startColor = Console.ForegroundColor;
            
            Console.SetCursorPosition(0, Console.BufferHeight - 1);

            int padding = 4;

            foreach (var command in Commands)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Gray;
                Console.Write($"{command}");
                Console.ForegroundColor = ConsoleColor.Gray;
                for (int i = 0; i < padding; i++)
                {
                    Console.Write('█');
                }
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            int startX = Console.GetCursorPosition().Left;
            for (int i = startX; i < Console.BufferWidth; i++)
            {
                Console.Write('█');
            }

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = startColor;
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

            //var Code = "var i = 10;\n" +
            //    "function testFunction(){\n" +
            //    "   i += 1;\n" +
            //    "};\n" +
            //    "testFunction();\n" +
            //    "var x = 444;\n" +
            //    "//testFunction();\n";

            var Code = "var num = 1212;\n" +
                "num += 1;\n";

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

            //while (true)
            //{
            //    string Input = Console.ReadLine();
            //    var ic = Compile(Code);
            //
            //    Computer c = new Computer(128);
            //    c.Compile(ic);
            //
            //    c.ProcessFull();
            //
            //    c.DumpMemory();
            //}
        }

        static string[] Compile(List<Token> tokens)
        {
            Parser parser = new Parser(tokens);
            var result = parser.Parse();

            return new string[] { };
        }
    }
}

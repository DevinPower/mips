using Lexer;
using System.Text;

namespace TideScriptREPL
{
    internal class Program
    {
        const string LastSession = "lastSession.td";
        static int count = 0;
        const int TABSIZE = 5;
        static readonly char[] AlwaysAlpha = new [] { '=', '+', '-', '$', '|', '>', '<', ',' };

        static void Main(string[] args)
        {
            string[] ContentsDefault = null;
            if (File.Exists(LastSession))
            {
                ContentsDefault = File.ReadLines(LastSession).ToArray();
            }

            if (args.Length == 0)
            {
                Interactive(ContentsDefault);
            }
            else
            {
                ContentsDefault = File.ReadLines(args[0]).ToArray();
                Interactive(ContentsDefault);
            }
        }

        public static int GetCursorX(int CurrentPosition)
        {
            return CurrentPosition + 5;
        }

        static void Interactive(string[] ContentsDefault)
        {
            Console.CursorSize = 100;

            List<List<char>> Contents = new List<List<char>>();
            if (ContentsDefault != null)
                Contents = ContentsDefault.Select((x) => x.ToCharArray().ToList()).ToList();
            else
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
                            string FileContents = string.Join(Environment.NewLine, Contents.Select(l => new string(l.ToArray())));
                            File.WriteAllText(LastSession, FileContents);

                            Lexer.Lexer l2 = new Lexer.Lexer();

                            Console.Clear();

                            bool PrintAST = Key.Modifiers == ConsoleModifiers.Shift;

                            var ic = Compile(GetTokens(l2, Contents, false, PrintAST));

                            if (PrintAST)
                            {
                                Console.WriteLine("Press any key to continue");
                                Console.ReadKey();
                            }

                            Computer c = new Computer(2048, 64);
                            c.SetSysCalls(new Action[]
                            {
                                c.Sys_Null, c.Print_Int, c.Print_Float, c.Print_Double, c.Print_String,
                                c.Read_Int, c.Read_Float, c.Read_Double, c.Read_String,
                                c.SBRK, c.Exit,
                                c.Print_Char, c.Read_Char,
                                c.Sys_Null, c.Sys_Null, c.Sys_Null, c.Sys_Null,
                                c.Exit2, c.Get_Time,
                                c.DotNetBreak
                            });

                            c.Compile(ic);

                            if (Key.Modifiers != ConsoleModifiers.Control)
                                c.ProcessFull();//StepProgram(c, ic);

                            DrawAlert("Press any key to exit", ConsoleColor.Red);

                            Console.ReadKey();

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
                Console.SetCursorPosition(GetCursorX(currentPosition), currentLine);
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
                        case TokenTypes.Include:
                            return ConsoleColor.Magenta;
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

            bool isNewLine = true;
            int line = 1;

            foreach(Token t in tokens)
            {
                if (isNewLine)
                {
                    var originalBackground = Console.BackgroundColor;
                    var originalForeground = Console.ForegroundColor;

                    Console.BackgroundColor = ConsoleColor.Gray;
                    Console.ForegroundColor = ConsoleColor.Black;

                    Console.Write(" ");

                    Console.Write(line.ToString("000"));
                    
                    line++;

                    Console.BackgroundColor = originalBackground;
                    Console.ForegroundColor = originalForeground;

                    Console.Write(" ");
                }

                var color = getColor(t.TokenType);
                Console.ForegroundColor = color;

                Console.Write(t.Value);
                isNewLine = t.Value == "\n";
            }
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

        static string[] Compile(List<Token> tokens)
        {
            Parser parser = new Parser(tokens);
            return parser.Parse();
        }
    }
}

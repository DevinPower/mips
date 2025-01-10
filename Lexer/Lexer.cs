using Lexer.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Lexer
{
    public enum TokenTypes { Error, Nothing, Operator, Identifier, Keyword, Separator, Literal, Comment, MachineCode }
    public class Token
    {
        public TokenTypes TokenType { get; private set; }
        public string Value { get; private set; }
        public int StartPosition {  get; private set; }
        public int EndPosition { get; private set; }

        public Token(TokenTypes tokenType, string value, int startPosition, int endPosition)
        {
            TokenType = tokenType;
            Value = value;
            StartPosition = startPosition;
            EndPosition = endPosition;
        }
    }

    public class Lexer
    {
       
        readonly string[] BinaryOperators = new[] { 
            "=", "==", "!=", "<", "<=", ">", ">=", "++", "--",
            "+", "-", "*", "/", "%", "+=", "-=", "*=", "/="
        };

        readonly string[] Keywords = new[] {
            "if", "return", "while", "var", "function"
        };

        readonly string[] Separators = new[] {
            ";", "(", ")", "{", "}", "[", "]", "\n", ","
        };

        public Lexer()
        {

        }

        public List<Token> Lexicate(string Contents, bool ForDraw, bool PrintAnalysisDebug)
        {
            string CurrentToken = "";

            List<(string, TokenTypes, int, int)> LexedCode = new List<(string, TokenTypes, int, int)>();

            for (int i = 0; i < Contents.Length; i++)
            {
                char c = Contents[i];
                int tokenStart = i;

                if (c == '/' && Contents[i + 1] == '/')
                {
                    i += 2;
                    string comment = ForDraw ? "//" : "";
                    while (i < Contents.Length && Contents[i] != '\n')
                    {
                        comment += Contents[i++];
                    }

                    if (Contents[i] == '\n')
                        i--;

                    LexedCode.Add((comment, TokenTypes.Comment, tokenStart, i));

                    CurrentToken = "";
                    continue;
                }

                if (c == '"')
                {
                    string literalString = "";
                    bool escaped = false;
                    for (i = i + 1; i < Contents.Length; i++)
                    {
                        c = Contents[i];

                        if (c == '\\')
                        {
                            if (ForDraw) 
                            { 
                                literalString += c;
                            }
                            escaped = true;
                            continue;
                        }

                        if (c == '"' && !escaped) break;
                        literalString += c;

                        escaped = false;
                    }

                    if (ForDraw)
                        literalString = $"\"{literalString}\"";

                    LexedCode.Add((literalString, TokenTypes.Literal, tokenStart, i));

                    CurrentToken = "";
                    continue;
                }

                if (c == '|')
                {
                    i += 1;
                    string machineCode = "";
                    while (i < Contents.Length && Contents[i] != '\n')
                    {
                        machineCode += Contents[i++];
                    }

                    if (Contents[i] == '\n')
                        i--;

                    if (ForDraw)
                        machineCode = "|" + machineCode;

                    LexedCode.Add((machineCode, TokenTypes.MachineCode, tokenStart, i));

                    CurrentToken = "";
                    continue;
                }

                if (c != ' ')
                {
                    CurrentToken += c;
                }

                if (c != ' ' && !Separators.Contains(c.ToString()))
                {
                    continue;
                }

                bool addCAsToken = false;
                if (Separators.Contains(c.ToString()))
                {
                    CurrentToken = CurrentToken.Substring(0, CurrentToken.Length - 1);
                    addCAsToken = true;
                }

                if (char.IsWhiteSpace(c) && ForDraw)
                {
                    addCAsToken = true;
                }

                TokenTypes tokenType = CheckType(CurrentToken.Trim());

                if (tokenType != TokenTypes.Nothing)
                    LexedCode.Add((CurrentToken, tokenType, tokenStart, i));

                if (addCAsToken)
                    LexedCode.Add((c.ToString(), TokenTypes.Separator, tokenStart, i));

                CurrentToken = "";
            }

            if (PrintAnalysisDebug)
                PrintAnalysis(LexedCode.Select((x) => { return (x.Item1, x.Item2);  }).ToList());

            return LexedCode.Select((x) => new Token(x.Item2, x.Item1, x.Item3, x.Item4)).ToList();
        }

        public void PrintAnalysis(List<(string, TokenTypes)> Tokens)
        {
            foreach(var Token in Tokens)
            {
                Console.WriteLine($"{Token.Item1.PadRight(70)}{Token.Item2}");
            }
        }

        TokenTypes CheckType(string Value)
        {
            if (string.IsNullOrWhiteSpace(Value))
                return TokenTypes.Nothing;

            if (BinaryOperators.Contains(Value))
                return TokenTypes.Operator;

            if (Keywords.Contains(Value))
                return TokenTypes.Keyword;

            if (Separators.Contains(Value))
                return TokenTypes.Separator;

            if (Int32.TryParse(Value, out int result))
                return TokenTypes.Literal;

            return TokenTypes.Identifier;
        }
    }
}

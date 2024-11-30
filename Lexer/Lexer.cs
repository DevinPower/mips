using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexer
{
    internal class Lexer
    {
        enum TokenTypes { Error, Nothing, Operator, Identifier, Keyword, Separator, Comment }
        readonly string[] Operators = new[] { 
            "=", "+", "-", "/", "*", "%",
            "!=", "=", "==", ">=", "<=",
            "<", ">", "++", "--"
        };

        readonly string[] Keywords = new[] {
            "if", "return", "while", "for", "var", "foreach", "in"
        };

        readonly string[] Separators = new[] {
            ";", "(", ")", "{", "}", "'", "\""
        };

        public Lexer()
        {

        }

        public void Lexicate(string Contents)
        {
            string CurrentToken = "";

            List<(string, TokenTypes)> LexedCode = new List<(string, TokenTypes)>();

            for (int i = 0; i < Contents.Length; i++)
            {
                char c = Contents[i];

                if (c == '/' && Contents[i + 1] == '/')
                {
                    i += 2;
                    string comment = "";
                    while (i < Contents.Length && Contents[i] != '\n')
                    {
                        comment += Contents[i++];
                    }

                    LexedCode.Add((comment, TokenTypes.Comment));

                    CurrentToken = "";
                    continue;
                }

                if (c != ' ')
                    CurrentToken += c;

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

                TokenTypes tokenType = CheckType(CurrentToken.Trim());

                if (tokenType != TokenTypes.Nothing)
                    LexedCode.Add((CurrentToken, tokenType));

                if (addCAsToken)
                    LexedCode.Add((c.ToString(), TokenTypes.Separator));

                CurrentToken = "";
            }

            PrintAnalysis(LexedCode);
        }

        void PrintAnalysis(List<(string, TokenTypes)> Tokens)
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

            if (Operators.Contains(Value))
                return TokenTypes.Operator;

            if (Keywords.Contains(Value))
                return TokenTypes.Keyword;

            if (Separators.Contains(Value))
                return TokenTypes.Separator;

            return TokenTypes.Identifier;
        }
    }
}

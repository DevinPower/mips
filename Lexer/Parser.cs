using Lexer.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexer
{
    internal class Parser
    {
        List<Token> _tokens;
        int current = 0;
        public Parser(List<Token> Tokens)
        {
            _tokens = Tokens;
        }

        public void Parse()
        {
            Node<ASTExpression> ASTRoot;

            while (current < _tokens.Count)
            {
                if (IsMatch(TokenTypes.Keyword))
                {
                    if (CheckType(TokenTypes.Identifier) && Previous().Value == "var")
                    {
                        Variable currentExpression = new Variable(Peek().Value, "var");
                        Console.WriteLine($"variable declaration for {Peek().Value}");
                    }

                    continue;
                }

                if (IsMatch(TokenTypes.Operator))
                {
                    if (Previous().Value == "=")
                    {
                        Console.WriteLine("Assignment to");
                        Assignment Assignment = new Assignment(null, null);
                    }

                    continue;
                }

                if (IsMatch(TokenTypes.Literal))
                {
                    Console.WriteLine($"Literal = {Previous().Value}");
                    continue;
                }

                if (IsMatch(TokenTypes.Identifier))
                {
                    continue;
                }

                if (IsMatch(TokenTypes.Separator))
                {
                    continue;
                }

                if (IsMatch(TokenTypes.Nothing) || IsMatch(TokenTypes.Comment)) continue;   //Do Nothing
            }
        }

        bool CheckType(TokenTypes type) => _tokens[current].TokenType == type;
        bool IsMatch(TokenTypes type) {
            if (_tokens[current].TokenType == type)
            {
                Advance();
                return true;
            }
            return false;
        }

        Token Advance()
        {
            current++;
            return Previous();
        }

        Token Previous()
        {
            return _tokens[current - 1];
        }

        Token Peek() 
        { 
            return _tokens[current]; 
        }

    }
}

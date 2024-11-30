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
            Node<ASTExpression> ASTRoot = new Node<ASTExpression>(null);

            while (current < _tokens.Count)
            {
                if (IsMatch(TokenTypes.Keyword))
                {
                    if (CheckType(TokenTypes.Identifier) && Previous().Value == "var")
                    {
                        Variable currentExpression = new Variable(Peek().Value, "var");
                        Console.WriteLine($"variable declaration for {Peek().Value}");
                    }
                }

                if (CheckType(TokenTypes.Operator))
                {
                    if (Peek().Value == "=")
                    {
                        //Assignment
                        Assignment Assignment = new Assignment(null, null);
                    }
                }

                if (CheckType(TokenTypes.Literal))
                {
                    Console.WriteLine($"Literal = {Peek().Value}");
                }

                current++;
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

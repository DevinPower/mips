using Lexer.AST;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexer
{
    public class Parser
    {
        List<Token> _tokens;
        int current = 0;
        Stack<Expression> ExpressionStack = new Stack<Expression>();
        CompilationMeta CompilationMeta;

        public Parser(List<Token> Tokens)
        {
            _tokens = Tokens.Where((x) => x.TokenType != TokenTypes.Nothing).ToList();
            CompilationMeta = new CompilationMeta();
        }

        Expression Expression()
        {
            if (IsMatch(TokenTypes.Literal))
                return Literal();
            if (IsMatch(TokenTypes.Identifier))
                return Identifier();
            if (IsMatch(TokenTypes.Keyword))
                return KeyWord();
            if (IsMatch(TokenTypes.MachineCode)) 
                return MachineCode();
            if (IsLineEnd())
                return null;

            return null;
        }

        Expression MachineCode()
        {
            return new MachineCode(Previous().Value);
        }

        Expression Literal()
        {
            Literal literal = new Literal(Int32.Parse(Previous().Value));
            if (IsMatch(TokenTypes.Operator))
            {
                ExpressionStack.Push(literal);
                return Operator();
            }

            return literal;
        }

        Expression KeyWord()
        {
            switch (Previous().Value)
            {
                case "int":
                case "string":
                case "float":
                case "double":
                case "char":
                    CompilationMeta.AddVariable(Peek().Value, Previous().Value);
                    return Expression();
            }

            return null;
        }

        OperatorTypes GetOperatorType(string Operator)
        {
            switch (Operator)
            {
                case "+":
                    return OperatorTypes.ADD;
                case "-":
                    return OperatorTypes.SUBTRACT;
                case "*":
                    return OperatorTypes.MULTIPLY;
                case "/":
                    return OperatorTypes.DIVIDE;
                case "=":
                    return OperatorTypes.ASSIGN;
            }
            throw new Exception("unknown operator type");
        }

        Expression Operator()
        {
            OperatorTypes type = GetOperatorType(Previous().Value);
            if (IsMatch(TokenTypes.Literal))
            {
                return new Operator(ExpressionStack.Pop(), type, Literal());
            }

            if (IsMatch(TokenTypes.Identifier))
            {
                return new Operator(ExpressionStack.Pop(), type, Identifier());
            }

            return null;
        }

        Expression Identifier()
        {
            Variable identifier = new Variable(Previous().Value);

            if (IsMatch(TokenTypes.Operator))
            {
                ExpressionStack.Push(identifier);
                return Operator();
            }

            return identifier;
        }

        public List<Expression> Parse()
        {
            List<Expression> expressions = new List<Expression>();

            while (!IsOutOfRange())
            {
                var result = Expression();
                if (result != null)
                {
                    expressions.Add(result);
                }

                if (current == _tokens.Count)
                    return expressions;

                ExpressionStack.Clear();
            }

            return expressions;
        }

        bool CheckType(TokenTypes type) => _tokens[current].TokenType == type;
        bool IsMatch(TokenTypes type) {
            if (current >= _tokens.Count) return false;
            if (_tokens[current].TokenType == type)
            {
                Advance();
                return true;
            }

            return false;
        }

        bool IsLineEnd()
        {
            if (current >= _tokens.Count) return false;
            if (_tokens[current].Value == ";" || _tokens[current].Value == "\n")
            {
                Advance();
                return true;
            }

            return false;
        }

        bool IsOutOfRange()
        {
            return (current > _tokens.Count);
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

        Token PeekAhead(int Offset)
        {
            return _tokens[current + Offset];
        }

    }
}

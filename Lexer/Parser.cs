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
            if (IsMatch(TokenTypes.Separator, "{"))
                return ScriptBlock(false);
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

        Expression ScriptBlock(bool FreshScope)
        {
            List<Expression> expressions = new List<AST.Expression>();
            while (!IsMatch(TokenTypes.Separator, "}"))
            {
                var result = Expression();
                if (result != null)
                {
                    expressions.Add(result);
                }

                if (current == _tokens.Count)
                    break;
            }

            CompilationMeta subScope = CompilationMeta;
            if (FreshScope)
                subScope = new CompilationMeta();

            return new ScriptBlock(expressions, subScope);
        }

        List<(string, string)> GetArguments()
        {
            List<(string, string)> Arguments = new List<(string, string)> ();
            if (!IsMatch(TokenTypes.Separator, "("))
                throw new Exception("Expected arguments....");

            while (!IsMatch(TokenTypes.Separator, ")"))
            {
                string type = Peek().Value;
                Advance();
                string name = Peek().Value;
                Advance();

                if (!IsMatch(TokenTypes.Separator, ",") && Peek().Value != ")")
                    throw new Exception("Argument format issue");

                Arguments.Add((type, name));
            }

            return Arguments;
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
                case "function":
                    {
                        string FunctionName = Peek().Value;
                        Advance();
                        List<(string type, string name)> Arguments = GetArguments();

                        if (!IsMatch(TokenTypes.Separator, "{"))
                            throw new Exception("Expected script block");

                        Expression block = ScriptBlock(true);

                        CompilationMeta.AddFunction(FunctionName, "void");

                        return new FunctionDefinition(FunctionName, (ScriptBlock)block);
                    }
                    break;
            }

            return null;
        }

        OperatorTypes GetOperatorType(string Operator)
        {
            switch (Operator)
            {
                case "+":
                    return OperatorTypes.ADD;
                case "+=":
                    return OperatorTypes.ADDASSIGN;
                case "-":
                    return OperatorTypes.SUBTRACT;
                case "-=":
                    return OperatorTypes.SUBTRACTASSIGN;
                case "*":
                    return OperatorTypes.MULTIPLY;
                case "*=":
                    return OperatorTypes.MULTIPLYASSIGN;
                case "/":
                    return OperatorTypes.DIVIDE;
                case "/=":
                    return OperatorTypes.DIVIDEASSIGN;
                case ">":
                    return OperatorTypes.GREATERTHAN;
                case "<":
                    return OperatorTypes.LESSTHAN;
                case "=":
                    return OperatorTypes.ASSIGN;
            }
            throw new Exception("unknown operator type");
        }

        Expression Operator()
        {
            OperatorTypes type = GetOperatorType(Previous().Value);

            if (type == OperatorTypes.ASSIGN)
            {
                return new Assignment((Variable)ExpressionStack.Pop(), Expression());
            }

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
            var FunctionData = CompilationMeta.GetFunction(identifier.Name);

            if (FunctionData != null)
            {
                List<Expression> Arguments = new List<Expression>();
                if (!IsMatch(TokenTypes.Separator, "("))
                    throw new Exception("Expected arguments block");

                while (!IsMatch(TokenTypes.Separator, ")"))
                {
                    if (IsMatch(TokenTypes.Identifier))
                        Arguments.Add(Identifier());
                    else if (IsMatch(TokenTypes.Literal))
                        Arguments.Add(Literal());

                    if (!IsMatch(TokenTypes.Separator, ",") && Peek().Value != ")")
                        throw new Exception("Argument format issue");
                }

                return new FunctionCall(identifier.Name, Arguments);
            }

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
                    break;

                ExpressionStack.Clear();
            }

            List<string> Code = new List<string>();
            foreach(Expression e in expressions)
            {
                e.GenerateCode(CompilationMeta, Code);
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

        bool IsMatch(TokenTypes type, string value)
        {
            if (current >= _tokens.Count) return false;
            if (_tokens[current].TokenType == type && _tokens[current].Value == value)
            {
                Advance();
                return true;
            }

            return false;
        }

        bool IsLineEnd()
        {
            if (current >= _tokens.Count) return true;
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

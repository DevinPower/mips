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
            CompilationMeta = new CompilationMeta(null);
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
                return ScriptBlock(null);
            if (IsLineEnd())
                return null;
            if (IsMatch(TokenTypes.Comment))
                return null;

            return null;
        }

        Expression MachineCode()
        {
            return new MachineCode(Previous().Value);
        }

        Expression Literal()
        {
            Literal literal = null;
            string literalValue = Previous().Value;
            if (Int32.TryParse(literalValue, out int intLiteral))
            {
                literal = new IntLiteral(intLiteral);
            }
            else
            {
                string strGuid = CompilationMeta.AddString(literalValue);
                literal = new StringLiteral(strGuid);
            }

            if (literal == null)
                throw new Exception($"Literal value type unknown '{literalValue}'");

            if (IsMatch(TokenTypes.Operator))
            {
                ExpressionStack.Push(literal);
                return Operator();
            }

            return literal;
        }

        Expression ScriptBlock(CompilationMeta subScope)
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

            if (subScope == null)
                subScope = CompilationMeta;

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

                        CompilationMeta subScope = CompilationMeta.AddSubScope();
                        Expression block = ScriptBlock(subScope);

                        CompilationMeta.AddFunction(FunctionName, "void");
                        foreach (var argument in Arguments)
                        {
                            subScope.AddArgument(argument.name, argument.type);
                        }

                        return new FunctionDefinition(FunctionName, (ScriptBlock)block);
                    }
                case "return":
                    {
                        Expression returnExpression = null;

                        if (IsMatch(TokenTypes.Identifier))
                            returnExpression = Identifier();
                        else if (IsMatch(TokenTypes.Literal))
                            returnExpression = Literal();
                        //TODO: Support function calls

                        if (returnExpression == null)
                            throw new Exception("Invalid return type");

                        return new ReturnStatement(returnExpression);
                    }
                case "if":
                    {
                        if (!IsMatch(TokenTypes.Separator, "("))
                            throw new Exception("Expected condition");
                        Expression condition = Expression();
                        if (!IsMatch(TokenTypes.Separator, ")"))
                            throw new Exception("Expected condition close");
                        if (!IsMatch(TokenTypes.Separator, "{"))
                            throw new Exception("Expected script block");
                        ScriptBlock body = (ScriptBlock)ScriptBlock(null);

                        return new Conditional(condition, body);
                    }
                case "while":
                    {
                        if (!IsMatch(TokenTypes.Separator, "("))
                            throw new Exception("Expected condition");
                        Expression condition = Expression();
                        if (!IsMatch(TokenTypes.Separator, ")"))
                            throw new Exception("Expected condition close");
                        if (!IsMatch(TokenTypes.Separator, "{"))
                            throw new Exception("Expected script block");
                        ScriptBlock body = (ScriptBlock)ScriptBlock(null);

                        return new WhileLoop(condition, body);
                    }
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
                case "==":
                    return OperatorTypes.EQUAL;
            }
            throw new Exception("unknown operator type");
        }

        bool IsSelfAssign(string Operator)
        {
            switch (Operator)
            {
                case "+=":
                case "-=":
                case "*=":
                case "/=":
                    return true;
            }
            return false;
        }

        Expression Operator()
        {
            OperatorTypes type = GetOperatorType(Previous().Value);
            bool SelfAssign = IsSelfAssign(Previous().Value);

            if (type == OperatorTypes.ASSIGN)
            {
                return new Assignment((Variable)ExpressionStack.Pop(), Expression());
            }

            if (IsMatch(TokenTypes.Literal))
            {
                return new Operator(ExpressionStack.Pop(), type, Literal(), SelfAssign);
            }

            if (IsMatch(TokenTypes.Identifier))
            {
                return new Operator(ExpressionStack.Pop(), type, Identifier(), SelfAssign);
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

        public string[] Parse()
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

            CompilationMeta.GenerateData(Code);

            return Code.ToArray();
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

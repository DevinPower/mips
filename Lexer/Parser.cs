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

        public ASTExpression ConsumeToken(Stack<ASTExpression> Expressions, Node<ASTExpression> ASTRoot)
        {
            if (IsOutOfRange()) return null;

            if (IsMatch(TokenTypes.Keyword))
            {
                if (CheckType(TokenTypes.Identifier))
                {
                    //Declaration
                    if (Previous().Value == "var")
                    {
                        Variable currentExpression = new Variable(Peek().Value, "var");
                        Console.WriteLine($"variable declaration for {Peek().Value}");
                        current++;
                        Expressions.Push(currentExpression);

                        return currentExpression;
                    }
                }
            }

            if (IsMatch(TokenTypes.Identifier))
            {
                Variable currentExpression = new Variable(Previous().Value, "var");
                Expressions.Push(currentExpression);

                return currentExpression;
            }

            if (IsMatch(TokenTypes.Operator))
            {
                if (Previous().Value == "=")
                {
                    if (Expressions.Pop() is Operand LHS)
                    {
                        if (ConsumeToken(Expressions, ASTRoot) is Operand RHS)
                        {
                            ASTExpression Assignment = new Assignment(LHS, RHS);

                            ASTRoot.AddChild(Assignment);
                            Assignment.TreeRepresentation.AddChild(LHS);
                            Assignment.TreeRepresentation.AddChild(RHS);

                            Expressions.Push(Assignment);

                            return Assignment;
                        }
                    }
                }

                if (HandleOperator("+", Expressions, ASTRoot, OperatorTypes.ADD) is var result && result != null)
                    return result;
            }

            if (IsMatch(TokenTypes.Literal))
            {
                Console.WriteLine($"Literal = {Previous().Value}");

                Literal literal = new Literal(LiteralTypes.NUMBER, Previous().Value);

                Expressions.Push(literal);

                return literal;
            }

            if (IsMatch(TokenTypes.Separator))
            {
                return null;
            }

            if (IsMatch(TokenTypes.Nothing) || IsMatch(TokenTypes.Comment)) return null;   //Do Nothing


            return null;
        }

        ASTExpression HandleOperator(string OperatorSymbol, Stack<ASTExpression> Expressions, Node<ASTExpression> ASTRoot, OperatorTypes OperatorType)
        {
            if (Previous().Value == OperatorSymbol)
            {
                if (Expressions.Pop() is Expression LHS)
                {
                    if (ConsumeToken(Expressions, ASTRoot) is Expression RHS)
                    {
                        BinaryOperation Operation = new BinaryOperation(LHS, 
                            new Operator(OperatorType), RHS);

                        ASTRoot.AddChild(Operation);
                        Operation.TreeRepresentation.AddChild(LHS);
                        Operation.TreeRepresentation.AddChild(RHS);

                        Expressions.Push(Operation);

                        return Operation;
                    }
                }
            }

            return null;
        }

        public void Parse()
        {
            Stack<ASTExpression> Expressions = new Stack<ASTExpression>();
            Node<ASTExpression> ASTRoot = new Node<ASTExpression>(null);

            while (current < _tokens.Count)
            {
                if (IsOutOfRange()) break;
                ConsumeToken(Expressions, ASTRoot);
            }

            Console.WriteLine("DONE");
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

    }
}

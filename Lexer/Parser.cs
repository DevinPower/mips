using Lexer.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexer
{
    public class Parser
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

                if (Previous().Value == "while")
                {
                    Stack<ASTExpression> stack = new Stack<ASTExpression>();
                    Node<ASTExpression> subRoot = new Node<ASTExpression>(null);
                    ASTExpression parsedExpression = ConsumeToken(stack, subRoot);

                    if (Peek().Value != "{")
                        throw new Exception("Unhandled exception for not seeing scriptblock on while loop");

                    ASTExpression parsedBody = ConsumeToken(stack, subRoot);

                    ASTExpression loop = new WhileLoop((Expression)parsedExpression, (Expression)parsedBody);
                    Node<ASTExpression> loopNode = ASTRoot.AddChild(loop);

                    Expression LoopContents = new Expression();
                    var LoopContentsAST = loopNode.AddChild(LoopContents);

                    subRoot.Children[0].Children.ForEach(child => { loopNode.AddChild(child); });
                    subRoot.Children[1].Children.ForEach(child => { LoopContentsAST.AddChild(child); });

                    Expressions.Push(loop);

                    return loop;
                }

                return null;
            }

            if (IsMatch(TokenTypes.Separator))
            {
                if (Previous().Value == "(")
                {
                    Node<ASTExpression> parsedExpression = ParseToSymbol(typeof(ParanEnd));
                    ASTRoot.AddChild(parsedExpression);
                    return parsedExpression.Data;
                }

                if (Previous().Value == "{")
                {
                    Node<ASTExpression> parsedExpression = ParseToSymbol(typeof(CurlyEnd));
                    ASTRoot.AddChild(parsedExpression);
                    return parsedExpression.Data;
                }

                if (Previous().Value == ")")
                {
                    return new ParanEnd();
                }

                if (Previous().Value == "}")
                {
                    return new CurlyEnd();
                }

                return null;
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

                //arithmetic
                if (HandleOperator("+", Expressions, ASTRoot, OperatorTypes.ADD) is var addResult && addResult != null)
                    return addResult;

                if (HandleOperator("-", Expressions, ASTRoot, OperatorTypes.SUBTRACT) is var subResult && subResult != null)
                    return subResult;

                if (HandleOperator("*", Expressions, ASTRoot, OperatorTypes.MULTIPLY) is var multResult && multResult != null)
                    return multResult;

                if (HandleOperator("/", Expressions, ASTRoot, OperatorTypes.DIVIDE) is var divResult && divResult != null)
                    return divResult;

                //comparison
                if (HandleOperator("<", Expressions, ASTRoot, OperatorTypes.LESSTHAN) is var lessResult && lessResult != null)
                    return lessResult;
            }

            if (IsMatch(TokenTypes.Literal))
            {
                Console.WriteLine($"Literal = {Previous().Value}");

                Literal literal = new Literal(LiteralTypes.NUMBER, Previous().Value);

                Expressions.Push(literal);

                return literal;
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

        public Node<ASTExpression> Parse()
        {
            Stack<ASTExpression> Expressions = new Stack<ASTExpression>();
            Node<ASTExpression> ASTRoot = new Node<ASTExpression>(new Expression());

            while (current < _tokens.Count)
            {
                if (IsOutOfRange()) break;
                ConsumeToken(Expressions, ASTRoot);
            }

            ASTRoot.PrintPretty("", true);

            return ASTRoot;
        }

        public Node<ASTExpression> ParseToSymbol(Type SymbolType)
        {
            Console.WriteLine($"Parsing to a {SymbolType.ToString()}");
            Stack<ASTExpression> Expressions = new Stack<ASTExpression>();
            Node<ASTExpression> ASTRoot = new Node<ASTExpression>(new Expression());

            while (current < _tokens.Count)
            {
                if (IsOutOfRange()) break;
                var currentToken = ConsumeToken(Expressions, ASTRoot);
                if (currentToken == null) continue;
                if (currentToken.GetType() == SymbolType) 
                    break;
            }

            return ASTRoot;
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

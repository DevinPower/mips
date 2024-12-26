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
        CompilationMeta _meta;
        int current = 0;
        public Parser(List<Token> Tokens, CompilationMeta CompilationMeta)
        {
            _tokens = Tokens;
            _meta = CompilationMeta;
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
                        string VarName = Peek().Value;
                        string initialValue = PeekAhead(2).Value;

                        if (Int32.TryParse(initialValue, out int literalVal))
                        {
                            Variable currentExpression = new Variable(Peek().Value, "NUMBER");
                            _meta.PushInt(Peek().Value, literalVal);

                            current++;
                            Expressions.Push(currentExpression);

                            return currentExpression;
                        }
                        else
                        {
                            Variable currentExpression = new Variable(Peek().Value, "STRING");
                            _meta.PushString(Peek().Value, initialValue);

                            current++;
                            Expressions.Push(currentExpression);

                            return currentExpression;
                        }
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
                    Expression LoopCondition = new Expression();
                    var LoopConditionAST = loopNode.AddChild(LoopCondition);
                    var LoopContentsAST = loopNode.AddChild(LoopContents);

                    subRoot.Children[0].Children.ForEach(child => { 
                        LoopConditionAST.AddChild(child); 
                        child.Data.SetTreeRepresentation(child); 
                    });
                    subRoot.Children[1].Children.ForEach(child => { 
                        LoopContentsAST.AddChild(child); 
                        child.Data.SetTreeRepresentation(child); 
                    });

                    LoopConditionAST.Data.SetTreeRepresentation(LoopConditionAST);
                    LoopContentsAST.Data.SetTreeRepresentation(LoopContentsAST);

                    subRoot.Children[0].Data.SetTreeRepresentation(subRoot.Children[0]);
                    subRoot.Children[1].Data.SetTreeRepresentation(subRoot.Children[1]);

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
                Variable currentExpression = new Variable(Previous().Value, 
                    _meta.GetVariableType(Previous().Value));

                Expressions.Push(currentExpression);
                
                return currentExpression;
            }

            if (IsMatch(TokenTypes.Operator))
            {
                if (Previous().Value == "=")
                {
                    if (Expressions.Pop() is Variable LHS)
                    {
                        if (ConsumeToken(Expressions, ASTRoot) is Expression RHS)
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
                string PreviousValue = Previous().Value;

                if (Int32.TryParse(PreviousValue, out int result))
                {
                    //_meta.PushInt(Previous().Value.GetHashCode().ToString(), result);
                    Literal literal = new Literal(LiteralTypes.NUMBER, result);

                    Expressions.Push(literal);

                    return literal;
                }
                else
                {
                    _meta.PushString(Previous().Value.GetHashCode().ToString(), PreviousValue);
                    Literal literal = new Literal(LiteralTypes.STRING, PreviousValue);

                    Expressions.Push(literal);

                    return literal;
                }
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

        Token PeekAhead(int index)
        {
            return _tokens[current + index];
        }

    }
}

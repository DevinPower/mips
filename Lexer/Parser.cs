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

        public Parser(List<Token> Tokens)
        {
            _tokens = Tokens.Where((x) => x.TokenType != TokenTypes.Nothing).ToList();
        }

        public ASTExpression ConsumeToken(Stack<ASTExpression> Expressions, Node<ASTExpression> ASTRoot, CompilationMeta scopeMeta)
        {
            if (IsOutOfRange()) return null;

            if (IsMatch(TokenTypes.Keyword))
            {
                if (CheckType(TokenTypes.Identifier))
                {
                    if (Previous().Value == "function")
                    {
                        string functionName = Peek().Value;

                        Advance();

                        Stack<ASTExpression> stack = new Stack<ASTExpression>();
                        Node<ASTExpression> subRoot = new Node<ASTExpression>(null);
                        //ASTExpression parsedExpression = ConsumeToken(stack, subRoot);

                        CompilationMeta functionScope = scopeMeta.AddSubScope();

                        Stack<ASTExpression> argstack = new Stack<ASTExpression>();
                        Node<ASTExpression> argsubRoot = new Node<ASTExpression>(null);

                        var args = ParseArguments(Expressions, ASTRoot.Parent, scopeMeta);
                        Advance();

                        for (int i = 0; i < args.Count; i++)
                        {
                            string? argument = args[i];
                            functionScope.PushArgument(argument, i);
                        }

                        if (Peek().Value != "{")
                            throw new Exception($"Unhandled exception for not seeing scriptblock on function {functionName}. Got {Peek().Value}");

                        ASTExpression parsedBody = ConsumeToken(stack, subRoot, functionScope);

                        ASTExpression function = new Function(functionName, (Expression)parsedBody, args.Count);
                        Node<ASTExpression> functionNode = ASTRoot.AddChild(function);

                        //Expression LoopCondition = new Expression();
                        //var LoopConditionAST = functionNode.AddChild(LoopCondition);
                        var LoopContentsAST = functionNode.AddChild(parsedBody);

                        subRoot.Children[0].Children.ForEach(child => {
                            LoopContentsAST.AddChild(child);
                            child.Data.SetTreeRepresentation(child);
                        });

                        LoopContentsAST.Data.SetTreeRepresentation(LoopContentsAST);
                        subRoot.Children[0].Data.SetTreeRepresentation(subRoot.Children[0]);

                        Expressions.Push(function);

                        scopeMeta.AddFunction(functionName, args.Count);

                        return function;
                    }

                    //Declaration
                    if (Previous().Value == "var")
                    {
                        string VarName = Peek().Value;
                        string initialValue = PeekAhead(2).Value;

                        if (Int32.TryParse(initialValue, out int literalVal))
                        {
                            Variable currentExpression = new Variable(Peek().Value, "NUMBER");
                            scopeMeta.PushInt(VarName, literalVal);

                            current++;
                            Expressions.Push(currentExpression);

                            return currentExpression;
                        }
                        else
                        {
                            Variable currentExpression = new Variable(Peek().Value, "STRING");
                            scopeMeta.PushString(VarName, initialValue, false);

                            current++;
                            Expressions.Push(currentExpression);

                            return currentExpression;
                        }
                    }
                }

                if (Previous().Value == "if")
                {
                    Stack<ASTExpression> stack = new Stack<ASTExpression>();
                    Node<ASTExpression> subRoot = new Node<ASTExpression>(null);

                    CompilationMeta conditionalMeta = scopeMeta.AddSubScope();

                    ASTExpression parsedExpression = ConsumeToken(stack, subRoot, scopeMeta);

                    if (Peek().Value != "{")
                        throw new Exception("Unhandled exception for not seeing scriptblock on if statement");

                    ASTExpression parsedBody = ConsumeToken(stack, subRoot, scopeMeta);

                    ASTExpression conditional = new Conditional((Expression)parsedExpression, (Expression)parsedBody);

                    Node<ASTExpression> conditionalNode = ASTRoot.AddChild(conditional);

                    Expression ConditionalContents = new Expression();
                    Expression ConditionalCondition = new Expression();
                    var ConditionalConditionAST = conditionalNode.AddChild(ConditionalCondition);
                    var ConditionalContentsAST = conditionalNode.AddChild(ConditionalContents);

                    subRoot.Children[0].Children.ForEach(child => {
                        ConditionalConditionAST.AddChild(child);
                        child.Data.SetTreeRepresentation(child);
                    });
                    subRoot.Children[1].Children.ForEach(child => {
                        ConditionalContentsAST.AddChild(child);
                        child.Data.SetTreeRepresentation(child);
                    });

                    ConditionalConditionAST.Data.SetTreeRepresentation(ConditionalConditionAST);
                    ConditionalContentsAST.Data.SetTreeRepresentation(ConditionalContentsAST);

                    subRoot.Children[0].Data.SetTreeRepresentation(subRoot.Children[0]);
                    subRoot.Children[1].Data.SetTreeRepresentation(subRoot.Children[1]);

                    Expressions.Push(conditional);

                    return conditional;
                }

                if (Previous().Value == "while")
                {
                    Stack<ASTExpression> stack = new Stack<ASTExpression>();
                    Node<ASTExpression> subRoot = new Node<ASTExpression>(null);

                    CompilationMeta loopMeta = scopeMeta.AddSubScope();

                    ASTExpression parsedExpression = ConsumeToken(stack, subRoot, scopeMeta);

                    if (Peek().Value != "{")
                        throw new Exception("Unhandled exception for not seeing scriptblock on while loop");

                    ASTExpression parsedBody = ConsumeToken(stack, subRoot, scopeMeta);

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
                    Node<ASTExpression> parsedExpression = ParseToSymbol(typeof(ParanEnd), scopeMeta);
                    ASTRoot.AddChild(parsedExpression);
                    return parsedExpression.Data;
                }

                if (Previous().Value == "{")
                {
                    Node<ASTExpression> parsedExpression = ParseToSymbol(typeof(CurlyEnd), scopeMeta);
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
                if (scopeMeta.FunctionExists(Previous().Value))
                {
                    string FunctionName = Previous().Value;
                    int argCount = scopeMeta.FunctionArgumentCount(FunctionName);

                    Advance();

                    List<Operand> arguments = new List<Operand>();
                    for (int i = 0; i < argCount; i++)
                    {
                        var result = ConsumeToken(Expressions, ASTRoot, scopeMeta);
                        if (result is Operand operand)
                        {
                            arguments.Add(operand);
                        }
                    }

                    FunctionCall jumpInstruction = new FunctionCall(FunctionName, arguments);

                    ASTRoot.AddChild(jumpInstruction);
                    Expressions.Push(jumpInstruction);

                    return jumpInstruction;
                }

                Token previous = Previous();

                Variable currentExpression = new Variable(previous.Value,
                    scopeMeta.GetVariableType(previous.Value));

                Expressions.Push(currentExpression);
                
                return currentExpression;
            }

            if (IsMatch(TokenTypes.MachineCode))
            {
                MachineCode machineExpression = new MachineCode(Previous().Value);

                ASTRoot.AddChild(machineExpression);
                Expressions.Push(machineExpression);

                return machineExpression;
            }

            if (IsMatch(TokenTypes.Operator))
            {
                if (Previous().Value == "=")
                {
                    if (Expressions.Pop() is Variable LHS)
                    {
                        if (ConsumeToken(Expressions, ASTRoot, scopeMeta) is Expression RHS)
                        {
                            string? Label = null;
                            if (Previous(4).Value == "var")
                                Label = Previous(3).Value;

                            ASTExpression Assignment = new Assignment(LHS, RHS, Label);

                            ASTRoot.AddChild(Assignment);
                            Assignment.TreeRepresentation.AddChild(LHS);
                            Assignment.TreeRepresentation.AddChild(RHS);

                            Expressions.Push(Assignment);

                            return Assignment;
                        }
                    }
                }

                //arithmetic
                if (HandleOperator("+", Expressions, ASTRoot, OperatorTypes.ADD, scopeMeta) is var addResult && addResult != null)
                    return addResult;

                if (HandleOperator("-", Expressions, ASTRoot, OperatorTypes.SUBTRACT, scopeMeta) is var subResult && subResult != null)
                    return subResult;

                if (HandleOperator("*", Expressions, ASTRoot, OperatorTypes.MULTIPLY, scopeMeta) is var multResult && multResult != null)
                    return multResult;

                if (HandleOperator("/", Expressions, ASTRoot, OperatorTypes.DIVIDE, scopeMeta) is var divResult && divResult != null)
                    return divResult;

                if (HandleOperator("+=", Expressions, ASTRoot, OperatorTypes.ADDASSIGN, scopeMeta) is var addaResult && addaResult != null)
                    return addaResult;

                //comparison
                if (HandleOperator("<", Expressions, ASTRoot, OperatorTypes.LESSTHAN, scopeMeta) is var lessResult && lessResult != null)
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
                    //_meta.PushString(Previous(3).Value, PreviousValue);
                    Literal literal = new Literal(LiteralTypes.STRING, PreviousValue);

                    Expressions.Push(literal);

                    return literal;
                }
            }

            if (IsMatch(TokenTypes.Nothing) || IsMatch(TokenTypes.Comment)) return null;   //Do Nothing


            return null;
        }

        List<string> ParseArguments(Stack<ASTExpression> Expressions, Node<ASTExpression> ASTRoot, CompilationMeta scopeMeta)
        {
            Advance();
            List<string> Arguments = new List<string>();
            while (Peek().Value != ")")
            {
                Arguments.Add(Peek().Value.Replace(",", "").Trim());
                Advance();
            }

            return Arguments;
        }

        ASTExpression HandleOperator(string OperatorSymbol, Stack<ASTExpression> Expressions, Node<ASTExpression> ASTRoot, OperatorTypes OperatorType, CompilationMeta scopeMeta)
        {
            if (Previous().Value == OperatorSymbol)
            {
                if (Expressions.Pop() is Expression LHS)
                {
                    if (ConsumeToken(Expressions, ASTRoot, scopeMeta) is Expression RHS)
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

        public Node<ASTExpression> Parse(CompilationMeta ScopeMeta)
        {
            Stack<ASTExpression> Expressions = new Stack<ASTExpression>();
            Node<ASTExpression> ASTRoot = new Node<ASTExpression>(new Expression());

            while (current < _tokens.Count)
            {
                if (IsOutOfRange()) break;
                ConsumeToken(Expressions, ASTRoot, ScopeMeta);
            }

            ASTRoot.PrintPretty("", true);

            return ASTRoot;
        }

        public Node<ASTExpression> ParseToSymbol(Type SymbolType, CompilationMeta ScopeMeta)
        {
            Stack<ASTExpression> Expressions = new Stack<ASTExpression>();
            Node<ASTExpression> ASTRoot = new Node<ASTExpression>(new Expression());

            while (current < _tokens.Count)
            {
                if (IsOutOfRange()) break;
                var currentToken = ConsumeToken(Expressions, ASTRoot, ScopeMeta);
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

        Token Previous(int Offset = 1)
        {
            return _tokens[current - Offset];
        }

        Token Peek(int Offset = 0)
        {
            return _tokens[current + Offset]; 
        }

        Token PeekAhead(int index)
        {
            return _tokens[current + index];
        }

    }
}

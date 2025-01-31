using Lexer.AST;
using System.Linq;

namespace Lexer
{
    public class OperatorOrder : OperatorListItem
    {
        public Token Token { get; set; }
        public int Precedence = 100;

        public OperatorOrder(Token Token, int Precedence)
        {
            this.Token = Token;
            this.Precedence = Precedence;
        }
    }

    public class OperatorExpression : OperatorListItem
    {
        public Expression Expression { get; set; }

        public OperatorExpression(Expression Expression)
        {
            this.Expression = Expression;
        }
    }

    public class OperatorListItem
    {

    }

    public class Parser
    {
        List<Token> _tokens;
        int current = 0;
        Stack<Expression> ExpressionStack = new Stack<Expression>();

        public Parser(List<Token> Tokens)
        {
            _tokens = Tokens.Where((x) => x.TokenType != TokenTypes.Nothing).ToList();
        }

        Expression Expression(CompilationMeta CompilationMeta)
        {
            if (IsMatch(TokenTypes.Literal))
                return ParseExpressionChain(CompilationMeta, ScoopOperatorExpressions(CompilationMeta));
            if (IsMatch(TokenTypes.Keyword))
                return KeyWord(CompilationMeta);
            if (IsMatch(TokenTypes.MachineCode)) 
                return MachineCode(CompilationMeta);
            if (IsMatch(TokenTypes.Separator, "{"))
                return ScriptBlock(CompilationMeta, null);
            if (IsLineEnd())
                return null;
            if (IsMatch(TokenTypes.Comment))
                return null;
            if (IsMatch(TokenTypes.Include))
                return AddInclude(CompilationMeta);

            if (IsMatch(TokenTypes.Identifier))
            {
                var identifier = ParseExpressionChain(CompilationMeta, ScoopOperatorExpressions(CompilationMeta));
                ExpressionStack.Push(identifier);

                if (identifier is FunctionCall)
                    return identifier;

                if (IsMatch(TokenTypes.Operator, "="))
                    return Assignment(CompilationMeta);
                
                return identifier;
            }

            return null;
        }

        ScriptBlock AddInclude(CompilationMeta CompilationMeta)
        {
            string Contents = string.Join("\n", File.ReadAllLines(Previous().Value));
            Lexer lexer = new Lexer();

            Parser parser = new Parser(lexer.Lexicate(Contents, false, false));
            var includeResults = parser.ParseCompilationMeta();

            CompilationMeta.MergeExternal(includeResults.Item1);
            List<AST.Expression> expressions = includeResults.Item2;
            return new ScriptBlock(expressions, CompilationMeta);
        }

        Expression MachineCode(CompilationMeta CompilationMeta)
        {
            return new MachineCode(Previous().Value);
        }

        Expression Literal(CompilationMeta CompilationMeta)
        {
            string literalValue = Previous().Value;
            return HandleLiteral(CompilationMeta, literalValue);
        }

        Expression HandleLiteral(CompilationMeta CompilationMeta, string Value)
        {
            Literal literal = null;

            if (Value.StartsWith("0x"))
            {
                literal = new IntLiteral(Conversions.HexToInt(Value));
            }
            else if (Value.EndsWith("f"))
            {
                float floatLiteral = float.Parse(Value.Substring(0, Value.Length - 1));
                literal = new FloatLiteral(floatLiteral);
            }
            else if (Value.StartsWith('\'') && Value.EndsWith('\''))
            {
                literal = new IntLiteral((int)Value[1]);
            }
            else if (Int32.TryParse(Value, out int intLiteral))
            {
                literal = new IntLiteral(intLiteral);
            }
            else
            {
                string strGuid = CompilationMeta.AddString(Value);
                literal = new StringLiteral(strGuid);
            }

            if (literal == null)
                throw new Exception($"Literal value type unknown '{Value}'");

            return literal;
        }

        Expression ScriptBlock(CompilationMeta CompilationMeta, CompilationMeta subScope)
        {
            List<Expression> expressions = new List<AST.Expression>();
            while (!IsMatch(TokenTypes.Separator, "}"))
            {
                var result = Expression(subScope);
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

        List<(string, string, bool)> GetArguments()
        {
            List<(string, string, bool)> Arguments = new List<(string, string, bool)> ();
            if (!IsMatch(TokenTypes.Separator, "("))
                throw new Exception("Expected arguments....");

            while (!IsMatch(TokenTypes.Separator, ")"))
            {
                string type = Peek().Value;
                Advance();

                bool isArray = IsMatch(TokenTypes.Separator, "[") && IsMatch(TokenTypes.Separator, "]");

                string name = Peek().Value;
                Advance();

                if (!IsMatch(TokenTypes.Separator, ",") && Peek().Value != ")")
                    throw new Exception("Argument format issue");

                Arguments.Add((type, name, isArray));
            }

            return Arguments;
        }

        Expression KeyWord(CompilationMeta CompilationMeta)
        {
            switch (Previous().Value)
            {
                case "int":
                case "string":
                case "float":
                case "char":
                    {
                        string Name = Peek().Value;
                        string Type = Previous().Value;
                        Expression result = null;

                        if (IsMatch(TokenTypes.Separator, "["))
                        {
                            if (IsMatch(TokenTypes.Literal))
                            {
                                Literal size = (Literal)Literal(CompilationMeta);
                                if (size is IntLiteral intSize)
                                {
                                    if (!IsMatch(TokenTypes.Separator, "]"))
                                        throw new Exception("Expected array close ']'");

                                    Console.WriteLine($"Array of size {intSize.Value}");

                                    Name = Peek().Value;
                                    CompilationMeta.AddVariableArray(Name, Type, intSize.Value);
                                    result = Expression(CompilationMeta);
                                    ExpressionStack.Push(result);
                                    return result;
                                }
                                else
                                {
                                    throw new Exception($"Expected array size, got '{size.ToString()}'");
                                }
                            }
                            else
                            {
                                throw new Exception("Expected int literal for array");
                            }
                        }

                        CompilationMeta.AddVariable(Name, Type);
                        result = Expression(CompilationMeta);
                        ExpressionStack.Push(result);
                        return result;
                    }
                case "function":
                    {
                        string ReturnType = Peek().Value;
                        Advance();
                        string FunctionName = Peek().Value;
                        Advance();
                        List<(string type, string name, bool isArray)> Arguments = GetArguments();

                        ConsumeWhitespaceAndComments();

                        if (!IsMatch(TokenTypes.Separator, "{"))
                            throw new Exception("Expected script block");

                        CompilationMeta subScope = CompilationMeta.AddSubScope(false);
                        Expression block = ScriptBlock(CompilationMeta, subScope);

                        CompilationMeta.AddFunction(FunctionName, ReturnType, Arguments.Select((x) => x.type).ToList());
                        foreach (var argument in Arguments)
                        {
                            subScope.AddArgument(argument.name, argument.type, argument.isArray);
                        }

                        return new FunctionDefinition(FunctionName, (ScriptBlock)block);
                    }
                case "return":
                    {
                        Expression returnExpression = Expression(CompilationMeta);

                        if (returnExpression == null)
                            throw new Exception("Invalid return type");

                        return new ReturnStatement(returnExpression);
                    }
                case "if":
                    {
                        return Conditional(CompilationMeta);
                    }
                case "while":
                    {
                        if (!IsMatch(TokenTypes.Separator, "("))
                            throw new Exception("Expected condition");

                        List<Expression> Conditions = new List<Expression>();

                        while (!IsMatch(TokenTypes.Separator, ")"))
                        {
                            Expression condition = Expression(CompilationMeta);
                            ExpressionStack.Push(condition);
                            if (IsLogicOperator(Peek().Value))
                                continue;
                            Conditions.Add(condition);
                        }

                        ConsumeWhitespaceAndComments();

                        if (!IsMatch(TokenTypes.Separator, "{"))
                            throw new Exception("Expected script block");

                        CompilationMeta subScope = CompilationMeta.AddSubScope(true);

                        ScriptBlock body = (ScriptBlock)ScriptBlock(CompilationMeta, subScope);

                        return new WhileLoop(Conditions, body);
                    }
            }

            return null;
        }

        private Expression Conditional(CompilationMeta CompilationMeta)
        {
            if (!IsMatch(TokenTypes.Separator, "("))
                throw new Exception("Expected condition");

            List<Expression> Conditions = new List<Expression>();

            while (!IsMatch(TokenTypes.Separator, ")"))
            {
                Expression condition = Expression(CompilationMeta);
                ExpressionStack.Push(condition);
                if (IsLogicOperator(Peek().Value))
                    continue;
                Conditions.Add(condition);
            }

            ConsumeWhitespaceAndComments();

            if (!IsMatch(TokenTypes.Separator, "{"))
                throw new Exception("Expected script block");

            CompilationMeta subScope = CompilationMeta.AddSubScope(true);

            ScriptBlock body = (ScriptBlock)ScriptBlock(CompilationMeta, subScope);

            ScriptBlock ElseBody = null;

            ConsumeWhitespaceAndComments();

            Conditional ElseIfBody = null;

            if (IsMatch(TokenTypes.Keyword, "elseif"))
            {
                ElseIfBody = (Conditional)Conditional(CompilationMeta);
            }

            if (IsMatch(TokenTypes.Keyword, "else"))
            {
                ConsumeWhitespaceAndComments();

                CompilationMeta elseScope = CompilationMeta.AddSubScope(true);
                if (!IsMatch(TokenTypes.Separator, "{"))
                    throw new Exception("Expected script block");
                ElseBody = (ScriptBlock)ScriptBlock(CompilationMeta, elseScope);
            }

            return new Conditional(Conditions, body, ElseBody, ElseIfBody);
        }

        void ConsumeWhitespaceAndComments()
        {
            while (true)
            {
                if (IsMatch(TokenTypes.Separator, "\n"))
                    continue;
                if (IsMatch(TokenTypes.Comment, " "))
                    continue;

                break;
            }
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
                case "||":
                    return OperatorTypes.LOGICALOR;
                case "&&":
                    return OperatorTypes.LOGICALAND;
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

        Expression Assignment(CompilationMeta CompilationMeta)
        {
            var result = new Assignment((Variable)ExpressionStack.Pop(), Expression(CompilationMeta));
            ExpressionStack.Push(result);
            return result;
        }

        bool IsLogicOperator(string Value)
        {
            switch (Value)
            {
                case "&&":
                case "||":
                case "==":
                case "<":
                case ">":
                    return true;
                default:
                    return false;
            }
        }

        public int GetOperatorPrecedence(string Value)
        {
            switch (Value)
            {
                case "&&":
                case "||":
                    return 4;
                case "==":
                case "<":
                case ">":
                    return 3;
                case "*":
                case "/":
                case "%":
                case "*=":
                case "/=":
                    return 2;
                case "+":
                case "-":
                case "+=":
                case "-=":
                    return 1;
                default:
                    return 0;
            }
        }

        Expression Identifier(CompilationMeta CompilationMeta)
        {
            return HandleIdentifier(CompilationMeta, Previous().Value);
        }

        Expression HandleIdentifier(CompilationMeta CompilationMeta, string Value)
        {
            Variable identifier = new Variable(Value);

            if (IsMatch(TokenTypes.Separator, "["))
            {
                Expression offset = Expression(CompilationMeta);
                identifier.SetOffset(offset);

                if (!IsMatch(TokenTypes.Separator, "]"))
                    throw new Exception("Expected array close");
            }

            var FunctionData = CompilationMeta.GetFunction(identifier.Name);

            if (FunctionData != null)
            {
                List<Expression> Arguments = new List<Expression>();
                if (!IsMatch(TokenTypes.Separator, "("))
                    throw new Exception("Expected arguments block");

                while (!IsMatch(TokenTypes.Separator, ")"))
                {
                    Arguments.Add(Expression(CompilationMeta));

                    if (!IsMatch(TokenTypes.Separator, ",") && Peek().Value != ")")
                        throw new Exception("Argument format issue");
                }

                FunctionCall func = new FunctionCall(identifier.Name, Arguments);

                return func;
            }

            if (IsMatch(TokenTypes.Separator, "("))
            {
                throw new Exception($"Unknown function '{identifier.Name}'");
            }

            if (CompilationMeta.GetVariable(identifier.Name) != null && !identifier.HasOffset() && CompilationMeta.GetVariable(identifier.Name).IsArray)
            {
                return new AddressPointer(identifier.Name);
            }

            return identifier;
        }

        public string[] Parse()
        {
            var results = ParseCompilationMeta();
            var CompilationMeta = results.Item1;
            var expressions = results.Item2;

            List<string> Code = new List<string>();
            foreach (Expression e in expressions)
            {
                e.GenerateCode(CompilationMeta, Code);
                CompilationMeta.FreeAllUsedRegisters();
            }

            CompilationMeta.GenerateData(Code);

            foreach (var line in Code)
            {
                Console.WriteLine(line);
            }
            return Code.ToArray();
        }

        public List<OperatorListItem> ScoopOperatorExpressions(CompilationMeta CompilationMeta)
        {
            List<OperatorListItem> operatorItems = new List<OperatorListItem>();

            void handleToken(Token currentToken){
                int precedence = GetOperatorPrecedence(currentToken.Value);

                switch (currentToken.TokenType)
                {
                    case TokenTypes.Operator:
                        operatorItems.Add(new OperatorOrder(currentToken, precedence));
                        if (operatorItems.Count != 1)
                            Advance();
                        break;
                    case TokenTypes.Identifier:
                        operatorItems.Add(new OperatorExpression(HandleIdentifier(CompilationMeta, currentToken.Value)));
                        if (operatorItems.Count != 1)
                            Advance();
                        break;
                    case TokenTypes.Literal:
                        operatorItems.Add(new OperatorExpression(HandleLiteral(CompilationMeta, currentToken.Value)));
                        if (operatorItems.Count != 1)
                            Advance();
                        break;
                }
            }

            handleToken(Previous());

            while ((Peek().TokenType == TokenTypes.Operator && Peek().Value != "=") || Peek().TokenType == TokenTypes.Literal || Peek().TokenType == TokenTypes.Identifier)
            {
                handleToken(Peek());
            }

            return operatorItems;
        }

        //TODO: Consider finding max precedence dynamically in case we make changes in the future
        public Expression ParseExpressionChain(CompilationMeta CompilationMeta, List<OperatorListItem> orders)
        {
            for (int precedenceLevel = 1; precedenceLevel <= 4; precedenceLevel++)
            {
                for (int i = 0; i < orders.Count; i++)
                {
                    if (orders[i] is OperatorOrder operatorToken)
                    {
                        if (operatorToken.Precedence > precedenceLevel)
                            continue;

                        OperatorTypes type = GetOperatorType(operatorToken.Token.Value);
                        bool SelfAssign = IsSelfAssign(operatorToken.Token.Value);

                        Expression LHS = (orders[i - 1] as OperatorExpression).Expression;
                        Expression RHS = (orders[i + 1] as OperatorExpression).Expression;

                        orders[i] = new OperatorExpression(new Operator(LHS, type, RHS, SelfAssign));
                        orders.RemoveAt(i + 1);
                        orders.RemoveAt(i - 1);
                    }
                }
            }

            if (orders.Count > 1 || !(orders[0] is OperatorExpression))
                throw new Exception("Expression chain resulting in 2 orders or non-expression?");

            ExpressionStack.Push((orders[0] as OperatorExpression).Expression);
            return (orders[0] as OperatorExpression).Expression;
        }

        public (CompilationMeta, List<Expression>) ParseCompilationMeta()
        {
            List<Expression> expressions = new List<Expression>();
            CompilationMeta CompilationMeta = new CompilationMeta(null, false);

            while (!IsOutOfRange())
            {
                var result = Expression(CompilationMeta);
                if (result != null)
                {
                    expressions.Add(result);
                }

                if (current == _tokens.Count)
                    break;

                ExpressionStack.Clear();
            }

            return (CompilationMeta, expressions);
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

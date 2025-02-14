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
        Func<string, string> _includeLoader;

        public Parser(List<Token> Tokens, Func<string, string> IncludeLoader)
        {
            _tokens = Tokens.Where((x) => x.TokenType != TokenTypes.Nothing).ToList();
            _includeLoader = IncludeLoader;
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
                if (CompilationMeta.IsClass(Previous().Value))
                {
                    string type = Previous().Value;
                    string name = Peek().Value;
                    var result = VariableDeclaration(CompilationMeta, name, type);
                    ExpressionStack.Push(result);

                    if (IsMatch(TokenTypes.Operator, "="))
                        return Assignment(CompilationMeta);

                    return result;
                }

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

        Expression Class(CompilationMeta CompilationMeta)
        {
            string ClassName = Peek().Value;
            Advance();

            if (!IsMatch(TokenTypes.Separator, "{"))
                throw new Exception($"Expected class block for {ClassName}");

            var classScope = CompilationMeta.AddSubScope(false);
            List<FunctionDefinition> functionDefinitions = new List<FunctionDefinition>();
            List<Variable> variableDefinitions = new List<Variable>();

            while (!IsMatch(TokenTypes.Separator, "}"))
            {
                ConsumeWhitespaceAndComments();
                if (IsMatch(TokenTypes.Separator, "}"))
                    break;

                if (IsMatch(TokenTypes.Keyword))
                {
                    var result = KeyWord(classScope);
                    if (result is FunctionDefinition func)
                    {
                        func.PrependName($"{ClassName}.");
                        functionDefinitions.Add(func);

                        var subex = func.GetSubExpressions();
                        subex.ForEach((x) =>
                        {
                            if (x is Variable classFunctionVariable)
                            {
                                classFunctionVariable.IsPropertyInClass = true;
                                classFunctionVariable.PropertyClassName = ClassName;
                            }
                        });

                        continue;
                    }

                    if (result is Variable var)
                    {
                        variableDefinitions.Add(var);

                        if (!IsMatch(TokenTypes.Separator, ";"))
                            throw new Exception($"Expected semicolon after variable {var.Name} in {ClassName}");

                        continue;
                    }
                }
                else
                {
                    throw new Exception($"Unexpected token type in class '{Peek().Value}'");
                }

                if (current == _tokens.Count)
                    break;
            }

            var variablesMeta = variableDefinitions.Select((x) => {
                return classScope.GetVariable(x.Name);
            }).ToList();

            functionDefinitions.ForEach((x) => {
                string NewName = $"{ClassName}." + classScope.GetFunction(x.Name.Split('.')[1]).Name;
                classScope.GetFunction(x.Name.Split('.')[1]).Name = NewName;
                classScope.RaiseFunctionToRoot(NewName);
            });

            CompilationMeta.AddClass(ClassName,
                variablesMeta, 
                functionDefinitions.Select((x) => {
                    return classScope.GetFunction(x.Name);
                }).ToList());

            return new ClassDefinition(classScope, ClassName, functionDefinitions, variableDefinitions);
        }

        ScriptBlock AddInclude(CompilationMeta CompilationMeta)
        {
            string Contents = _includeLoader(Previous().Value);
            Lexer lexer = new Lexer();

            Parser parser = new Parser(lexer.Lexicate(Contents, false, false), _includeLoader);
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
                literal = new IntLiteral(Helpers.HexToInt(Value));
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

        List<(string, string, bool, bool)> GetArguments(CompilationMeta CompilationMeta)
        {
            List<(string, string, bool, bool)> Arguments = new List<(string, string, bool, bool)> ();
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

                bool isClass = CompilationMeta.IsClass(type);

                Arguments.Add((type, name, isArray, isClass));
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
                        return HandleVariableDeclaration(CompilationMeta);
                    }
                case "class":
                    {
                        return Class(CompilationMeta);
                    }
                case "new":
                    {
                        string ClassName = Peek().Value;
                        Advance();

                        if (!IsMatch(TokenTypes.Separator, "("))
                            throw new Exception("Missing parans on object instantiation");

                        if (!IsMatch(TokenTypes.Separator, ")"))
                            throw new Exception("Missing parans on object instantiation");

                        return new ClassInstantiation(ClassName);
                    }
                case "function":
                    {
                        string ReturnType = Peek().Value;
                        Advance();
                        string FunctionName = Peek().Value;
                        Advance();
                        List<(string type, string name, bool isArray, bool isClass)> Arguments = GetArguments(CompilationMeta);

                        CompilationMeta.AddFunction(FunctionName, ReturnType, Arguments.Select((x) => x.type).ToList());
                        CompilationMeta subScope = CompilationMeta.AddSubScope(false);

                        foreach (var argument in Arguments)
                        {
                            subScope.AddArgument(argument.name, argument.type, argument.isArray, argument.isClass);
                        }

                        ConsumeWhitespaceAndComments();

                        if (!IsMatch(TokenTypes.Separator, "{"))
                            throw new Exception("Expected script block");
                        
                        Expression block = ScriptBlock(CompilationMeta, subScope);

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

        private Expression HandleVariableDeclaration(CompilationMeta CompilationMeta)
        {
            string Name = Peek().Value;
            string Type = Previous().Value;

            return VariableDeclaration(CompilationMeta, Name, Type);
        }

        private Expression VariableDeclaration(CompilationMeta CompilationMeta, string Name, string Type)
        {
            Expression result = null;
            bool isClass = CompilationMeta.IsClass(Type);

            if (IsMatch(TokenTypes.Separator, "["))
            {
                if (IsMatch(TokenTypes.Literal))
                {
                    Literal size = (Literal)Literal(CompilationMeta);
                    if (size is IntLiteral intSize)
                    {
                        if (!IsMatch(TokenTypes.Separator, "]"))
                            throw new Exception("Expected array close ']'");

                        Name = Peek().Value;
                        CompilationMeta.AddVariableArray(Name, Type, intSize.Value, isClass);
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

            CompilationMeta.AddVariable(Name, Type, isClass);
            result = Expression(CompilationMeta);
            ExpressionStack.Push(result);
            return result;
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
                case ">=":
                    return OperatorTypes.GREATERTHANEQUAL;
                case "<=":
                    return OperatorTypes.LESSTHANEQUAL;
                case "=":
                    return OperatorTypes.ASSIGN;
                case "==":
                    return OperatorTypes.EQUAL;
                case "!=":
                    return OperatorTypes.NOTEQUAL;
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
                case "!=":
                case "<":
                case ">":
                case "<=":
                case ">=":
                    return true;
                default:
                    return false;
            }
        }

        public int GetOperatorPrecedence(string Value)
        {
            switch (Value)
            {
                case "*=":
                case "/=":
                case "+=":
                case "-=":
                    return 5;
                case "&&":
                case "||":
                    return 4;
                case "==":
                case "!=":
                case "<":
                case ">":
                case "<=":
                case ">=":
                    return 3;
                case "*":
                case "/":
                case "%":
                    return 2;
                case "+":
                case "-":
                    return 1;
                default:
                    return 0;
            }
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

            VariableMeta? VariableMeta = CompilationMeta.GetVariable(identifier.Name);
            if (VariableMeta == null)
                VariableMeta = CompilationMeta.GetArgument(identifier.Name, false);

            FunctionMeta? FunctionData = null;

            bool IsClassFunctionCall = false;
            AddressPointer ClassAddressPointer = null;

            if (VariableMeta != null && CompilationMeta.IsClass(VariableMeta.Type))
            {
                if (IsMatch(TokenTypes.Separator, "."))
                {
                    string accessName = Peek().Value;
                    Advance();

                    ClassMeta ClassMeta = CompilationMeta.GetClass(identifier.GetVariableType(CompilationMeta));

                    if (Peek().Value == "(")
                    {
                        FunctionData = CompilationMeta.GetFunction($"{ClassMeta.Name}.{accessName}");
                        ClassAddressPointer = new AddressPointer(identifier);
                        Expression originalOffset = identifier.Offset;
                        identifier = new Variable($"{ClassMeta.Name}.{accessName}");
                        identifier.SetOffset(originalOffset);
                        IsClassFunctionCall = true;
                    }
                    else
                    {
                        int address = ClassMeta.GetClassDataPosition(accessName);
                        identifier.SetPropertyOffset(new IntLiteral(address));
                        return identifier;
                    }
                }
            }

            if (FunctionData == null)
                FunctionData = CompilationMeta.GetFunction(identifier.Name);

            if (FunctionData != null)
            {
                List<Expression> Arguments = new List<Expression>();
                if (!IsMatch(TokenTypes.Separator, "("))
                    throw new Exception("Expected arguments block");

                while (!IsMatch(TokenTypes.Separator, ")"))
                {
                    var result = Expression(CompilationMeta);

                    if (result is Variable v)
                    {
                        if (CompilationMeta.GetVariable(v.Name) != null && !v.HasOffset() && CompilationMeta.GetVariable(v.Name).IsClass())
                        {
                            Arguments.Add(new AddressPointer(v));
                        }
                        else
                        {
                            Arguments.Add(result);
                        }
                    }
                    else
                    {
                        Arguments.Add(result);
                    }

                    if (!IsMatch(TokenTypes.Separator, ",") && Peek().Value != ")")
                        throw new Exception("Argument format issue");
                }

                if (!IsClassFunctionCall)
                {
                    FunctionCall func = new FunctionCall(identifier.Name, Arguments);

                    return func;
                }
                else
                {
                    ClassFunctionCall func = new ClassFunctionCall(ClassAddressPointer, identifier.Name, Arguments);

                    return func;
                }
            }

            if (IsMatch(TokenTypes.Separator, "("))
            {
                throw new Exception($"Unknown function '{identifier.Name}'");
            }

            if (CompilationMeta.GetVariable(identifier.Name) != null && !identifier.HasOffset() && CompilationMeta.GetVariable(identifier.Name).IsArray)
            {
                return new AddressPointer(identifier);
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
            for (int precedenceLevel = 1; precedenceLevel <= 5; precedenceLevel++)
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

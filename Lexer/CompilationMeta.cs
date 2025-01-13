using Lexer.AST;

namespace Lexer
{
    public class VariableMeta
    {
        public string Name { get; set; }
        public string Type { get; set; }

        public VariableMeta(string Name, string Type)
        {
            this.Name = Name;
            this.Type = Type;
        }

        string HandleType()
        {
            switch (Type)
            {
                case "int":
                    return $".word 1";
                case "string":
                    return $".word 1";
            }

            throw new Exception($";error generating data for Type {Type} on variable {Name}");

        }

        public string GenerateData()
        {
            return $"{Name}: {HandleType()}";
        }
    }

    public class FunctionMeta
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }

        public FunctionMeta(string Name, string ReturnType)
        {
            this.Name = Name;
            this.ReturnType = ReturnType;
        }
    }

    public class CompilationMeta
    {
        CompilationMeta _Parent;
        List<VariableMeta> Variables = new List<VariableMeta>();
        List<FunctionMeta> Functions = new List<FunctionMeta>();
        Dictionary<string, string> StringData = new Dictionary<string, string>();
        public bool[] TempRegisters = new bool[8];
        List<CompilationMeta> _childScopes = new List<CompilationMeta>();
        VariableMeta[] Arguments = new VariableMeta[4];

        public CompilationMeta(CompilationMeta Parent)
        {
            _Parent = Parent;
        }

        public CompilationMeta AddSubScope()
        {
            CompilationMeta newScope = new CompilationMeta(this);
            _childScopes.Add(newScope);

            return newScope;

        }

        public void AddFunction(string Name, string ReturnType)
        {
            Functions.Add(new FunctionMeta(Name, ReturnType));
        }

        public FunctionMeta? GetFunction(string Name)
        {
            var Matches = Functions.Where((x) => x.Name == Name);
            if (Matches.Count() != 1)
                return null;

            return Matches.First();
        }

        public void AddVariable(string Variable, string Type)
        {
            Variables.Add(new VariableMeta(Variable, Type));
        }

        public void AddArgument(string Name, string Type)
        {
            for(int i = 0; i < Arguments.Length; i++)
            {
                if (Arguments[i] == null)
                {
                    Arguments[i] = new VariableMeta(Name, Type);
                    return;
                }
            }
            throw new Exception("Too many arguments exception");
        }

        public int GetArgumentPosition(string Name)
        {
            for (int i = 0; i < Arguments.Length; i++)
            {
                if (Arguments[i] == null)
                    return -1;

                if (Arguments[i].Name == Name)
                    return i;
            }

            return -1;
        }

        public string AddString(string Value)
        {
            string Name = System.Guid.NewGuid().ToString().Replace("-", "");
            StringData.Add(Name, Value);
            return Name;
        }

        public int GetTempRegister()
        {
            for (int i = 0; i < TempRegisters.Length; i++)
            {
                if (TempRegisters[i] == false)
                {
                    TempRegisters[i] = true;
                    return i;
                }
            }

            throw new Exception("Out of registers exception");
        }

        public void GenerateData(List<string> Code)
        {
            int InserCount = 0;
            Code.Insert(InserCount++, ".data");

            foreach(VariableMeta variable in Variables)
            {
                Code.Insert(InserCount++, variable.GenerateData());
            }

            foreach(string key in StringData.Keys)
            {
                Code.Insert(InserCount++, $"{key}: .asciiz \"{StringData[key]}\"");
            }

            Code.Insert(InserCount++, ".main");
        }

        public void FreeTempRegister(RegisterResult Register)
        {
            if (Register.Register.StartsWith("t"))
            {
                int registerIndex = int.Parse(Register.Register.Replace("t", ""));
                TempRegisters[registerIndex] = false;
            }

            //TempRegisters[Index] = false;
        }
    }
}
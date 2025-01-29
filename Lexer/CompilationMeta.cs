using Lexer.AST;

namespace Lexer
{
    public class VariableMeta
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int ArraySize { get; set; } = 1;

        public VariableMeta(string Name, string Type)
        {
            this.Name = Name;
            this.Type = Type;
        }

        public VariableMeta(string Name, string Type, int ArraySize)
        {
            this.Name = Name;
            this.Type = Type;
            this.ArraySize = ArraySize;
        }

        string HandleType()
        {
            switch (Type)
            {
                case "int":
                    return $".word {ArraySize}";
                case "float":
                    return $".word {ArraySize}";
                case "string":
                    return $".word {ArraySize}";
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
        public List<string> ArgumentTypes { get; set; }

        public FunctionMeta(string Name, string ReturnType, List<string> ArgumentTypes)
        {
            this.Name = Name;
            this.ReturnType = ReturnType;
            this.ArgumentTypes = ArgumentTypes;
        }
    }

    public class CompilationMeta
    {
        protected List<string> Includes = new List<string>();
        protected CompilationMeta _Parent;
        protected List<VariableMeta> Variables = new List<VariableMeta>();
        protected List<FunctionMeta> Functions = new List<FunctionMeta>();
        protected Dictionary<string, string> StringData = new Dictionary<string, string>();
        public bool[] TempRegisters = new bool[8];
        protected List<CompilationMeta> _childScopes = new List<CompilationMeta>();
        protected VariableMeta[] Arguments = new VariableMeta[4];

        public CompilationMeta(CompilationMeta Parent, bool CopyTempRegisters)
        {
            _Parent = Parent;
            if (CopyTempRegisters)
            {
                for (int i = 0; i < 8; i++)
                {
                    TempRegisters[i] = _Parent.TempRegisters[i];
                }
            }
        }

        public void MergeExternal(CompilationMeta ExternalMeta)
        {
            Variables.AddRange(ExternalMeta.Variables);
            Functions.AddRange(ExternalMeta.Functions);
            _childScopes.AddRange(ExternalMeta._childScopes);
            foreach (var key in ExternalMeta.StringData.Keys)
            {
                StringData.Add(key, ExternalMeta.StringData[key]);
            }
        }

        public List<string> GetIncludedFiles()
        {
            return Includes;
        }

        public void AddInclude(string FileName)
        {
            if (_Parent != null)
                throw new Exception($"Include for '{FileName}' added outside of parent scope.");

            Includes.Add(FileName);
        }

        public CompilationMeta AddSubScope(bool CopyRegisters)
        {
            CompilationMeta newScope = new CompilationMeta(this, CopyRegisters);
            _childScopes.Add(newScope);

            return newScope;
        }

        public void AddFunction(string Name, string ReturnType, List<string> ArgumentTypes)
        {
            Functions.Add(new FunctionMeta(Name, ReturnType, ArgumentTypes));
        }

        public FunctionMeta? GetFunction(string Name)
        {
            var Matches = Functions.Where((x) => x.Name == Name);
            if (Matches.Count() != 1)
            {
                if (_Parent == null)
                    return null;
                else
                    return _Parent.GetFunction(Name);
            }

            return Matches.First();
        }

        public VariableMeta? GetVariable(string Name)
        {
            var Matches = Variables.Where((x) => x.Name == Name).ToList();
            if (Matches.Count() != 1)
            {
                if (_Parent == null)
                    return null;
                else
                    return _Parent.GetVariable(Name);
            }

            return Matches.First();
        }

        public VariableMeta? GetArgument(string Name, bool CanRecurse)
        {
            var Matches = Arguments.Where((x) => x != null && x.Name == Name).ToList();
            if (Matches.Count() != 1)
            {
                if (_Parent == null)
                    return null;
                else if (CanRecurse)
                    return _Parent.GetArgument(Name, CanRecurse);
                else
                    return null;
            }

            return Matches.First();
        }

        public void AddVariable(string Variable, string Type)
        {
            Variables.Add(new VariableMeta(Variable, Type));
        }

        public void AddVariableArray(string Variable, string Type, int Size)
        {
            Variables.Add(new VariableMeta(Variable, Type, Size));
        }

        public void AddArgument(string Name, string Type, bool IsArray)
        {
            for(int i = 0; i < Arguments.Length; i++)
            {
                if (Arguments[i] == null)
                {
                    Arguments[i] = new VariableMeta(Name, Type, IsArray ? 2 : 0);
                    return;
                }
            }
            throw new Exception("Too many arguments exception");
        }

        public int GetArgumentPosition(string Name, bool CanRecurse)
        {
            for (int i = 0; i < Arguments.Length; i++)
            {
                if (Arguments[i] == null)
                {
                    if (!CanRecurse || _Parent == null)
                        return -1;
                    
                    return _Parent.GetArgumentPosition(Name, CanRecurse);
                }

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
            int InsertCount = 0;
            Code.Insert(InsertCount++, ".data");

            GenerateVariableCode(Code, ref InsertCount);
            GenerateStringConstantCode(Code, ref InsertCount);

            Code.Insert(InsertCount++, ".main");
        }

        protected void GenerateVariableCode(List<string> Code, ref int InsertCount)
        {
            foreach (VariableMeta variable in Variables)
            {
                Code.Insert(InsertCount++, variable.GenerateData());
            }

            foreach(var child in _childScopes)
                child.GenerateVariableCode(Code, ref InsertCount);
        }

        protected void GenerateStringConstantCode(List<string> Code, ref int InsertCount)
        {
            foreach (string key in StringData.Keys)
            {
                Code.Insert(InsertCount++, $"{key}: .asciiz \"{StringData[key]}\"");
            }

            foreach (var child in _childScopes)
                child.GenerateStringConstantCode(Code, ref InsertCount);
        }

        public void FreeTempRegister(RegisterResult Register)
        {
            if (Register == null)
                return;

            if (Register.Register.StartsWith("t"))
            {
                int registerIndex = int.Parse(Register.Register.Replace("t", ""));
                TempRegisters[registerIndex] = false;
            }
        }

        public void FreeAllUsedRegisters()
        {
            for (int i = 0; i < TempRegisters.Length; i++)
            {
                TempRegisters[i] = false;
            }
        }
    }
}
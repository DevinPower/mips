using Lexer.AST;

namespace Lexer
{
    public class VariableMeta
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int DataSize { get; set; } = 1;
        public bool IsArray { get; set; }
        public bool IsLocal { get; private set; }
        public bool IsHeapAllocated { get; set; }
        bool _IsClass = false;

        int? _stackOffset = null;

        public VariableMeta(string Name, string Type, bool IsClass)
        {
            this.Name = Name;
            this.Type = Type;
            _IsClass = IsClass;
        }

        public VariableMeta(string Name, string Type, int ArraySize, bool IsArray, bool IsClass)
        {
            this.Name = Name;
            this.Type = Type;
            this.DataSize = ArraySize;
            this.IsArray = IsArray;
            _IsClass = IsClass;
        }

        public bool IsClass()
        {
            return _IsClass;
        }

        string HandleType()
        {
            switch (Type)
            {
                case "int":
                    return $".word {DataSize}";
                case "float":
                    return $".word {DataSize}";
                case "string":
                    return $".word {DataSize}";
            }

            if (_IsClass)
                return $".word {DataSize}";

            throw new Exception($";error generating data for Type {Type} on variable {Name}");
        }

        public int GetStackOffset()
        {
            if (_stackOffset == null)
                throw new Exception($"Variable {Name} not yet allocated on stack!");

            return _stackOffset.Value;
        }

        public string GenerateData()
        {
            return $"{Name}: {HandleType()}";
        }

        public void GenerateStack(CompilationMeta CompilationMeta, List<string> Code)
        {
            IsLocal = true;

            Code.Add($"Ori $t9, $zero, {DataSize}");
            Code.Add($"Sub $sp, $sp, $t9");
            _stackOffset = CompilationMeta.GetAndOffsetStack(DataSize);
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

    public class ClassMeta
    {
        public string Name { get; set; }
        public List<VariableMeta> Properties { get; set; }
        public List<FunctionMeta> Functions { get; set; }


        public int GetClassDataPosition(string Property)
        {
            int offset = 1;
            foreach(VariableMeta Meta in Properties)
            {
                if (Meta.Name == Property)
                    return offset;

                offset += Meta.DataSize;
            }

            throw new Exception($"Property {Property} not found on class type {Name}");
        }

        public string GetPropertyType(string Property)
        {
            foreach (VariableMeta Meta in Properties)
            {
                if (Meta.Name == Property)
                    return Meta.Type;
            }

            throw new Exception($"Property {Property} not found on class type {Name}");
        }

        public int GetClassDataSize()
        {
            int offset = 0;
            foreach (VariableMeta Meta in Properties)
            {
                offset += Meta.DataSize;
            }

            return offset;
        }


        public ClassMeta(string Name, List<VariableMeta> Properties, List<FunctionMeta> Functions)
        {
            this.Name = Name;
            this.Properties = Properties;
            this.Functions = Functions;
        }
    }

    public class CompilationMeta
    {
        protected List<string> Includes = new List<string>();
        protected CompilationMeta _Parent;
        protected List<VariableMeta> Variables = new List<VariableMeta>();
        protected List<FunctionMeta> Functions = new List<FunctionMeta>();
        protected List<ClassMeta> Classes = new List<ClassMeta>();
        protected Dictionary<string, string> StringData = new Dictionary<string, string>();
        public bool[] TempRegisters = new bool[8];
        protected List<CompilationMeta> _childScopes = new List<CompilationMeta>();
        protected VariableMeta[] Arguments = new VariableMeta[4];
        protected int StackOffset = 0;

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

        public int GetAndOffsetStack(int DataSize)
        {
            StackOffset += DataSize;
            return StackOffset;
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

        public void AddClass(string ClassName, List<VariableMeta> Properties, List<FunctionMeta> Functions)
        {
            Classes.Add(new ClassMeta(ClassName, Properties, Functions));
        }

        public void RaiseFunctionToRoot(string FunctionName)
        {
            if (_Parent == null)
                return;

            FunctionMeta? FunctionMeta = GetFunction(FunctionName);

            if (FunctionMeta == null)
                throw new Exception($"Unknown function {FunctionName}; cannot raise to root");

            CompilationMeta Current = this;
            while(Current._Parent != null)
            {
                Current = Current._Parent;
            }

            Current.AddFunction(FunctionMeta);
            Functions.Remove(FunctionMeta);
        }

        public ClassMeta GetClass(string ClassName)
        {
            if (Classes.Count((x) => x.Name == ClassName) == 1)
                return Classes.First((x) => x.Name == ClassName);

            if (_Parent == null)
                throw new Exception($"Class {ClassName} not found");

            return _Parent.GetClass(ClassName);
        }

        public bool IsClass(string ClassName)
        {
            if (Classes.Count((x)=>x.Name == ClassName) == 1)
                return true;

            if (_Parent == null) return false;

            return _Parent.IsClass(ClassName);
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

        public void AddFunction(FunctionMeta FunctionMeta)
        {
            Functions.Add(FunctionMeta);
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

        public void AddVariable(string Variable, string Type, bool IsClass)
        {
            Variables.Add(new VariableMeta(Variable, Type, IsClass));
        }

        public void AddVariableArray(string Variable, string Type, int Size, bool IsClass)
        {
            Variables.Add(new VariableMeta(Variable, Type, Size, true, IsClass));
        }

        public void AddArgument(string Name, string Type, bool IsArray, bool IsClass)
        {
            for(int i = 0; i < Arguments.Length; i++)
            {
                if (Arguments[i] == null)
                {
                    Arguments[i] = new VariableMeta(Name, Type, IsArray ? 2 : 0, IsArray, IsClass);
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
                if (!variable.IsHeapAllocated)
                    Code.Insert(InsertCount++, variable.GenerateData());
            }
        }

        public void EnterScope(List<string> Code)
        {
            if (_Parent == null)
                return;

            foreach(VariableMeta variable in Variables)
            {
                variable.GenerateStack(this, Code);
            }
        }

        public void ExitScope(List<string> Code)
        {
            if (StackOffset == 0)
                return;

            Code.Add($"Ori $t9, $zero, {StackOffset}");
            Code.Add($"Add $sp, $sp, $t9");
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
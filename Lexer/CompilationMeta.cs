﻿namespace Lexer
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
        List<VariableMeta> Variables = new List<VariableMeta>();
        List<FunctionMeta> Functions = new List<FunctionMeta>();
        public bool[] TempRegisters = new bool[8];

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

        public void FreeTempRegister(int Index)
        {
            TempRegisters[Index] = false;
        }
    }
}
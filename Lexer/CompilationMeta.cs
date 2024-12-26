using Lexer.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexer
{
    public class CompilationDataEntry
    { 
        public int Position { get; }
        string Value;
        public LiteralTypes Type { get; private set; }

        public string? GetValue()
        {
            if (Type == LiteralTypes.NUMBER) return $".word 1";
            if (Type == LiteralTypes.STRING) return $".asciiz \"{Value}\"";
            return $";unhandled compilation data entry for {Value}";
        }

        public CompilationDataEntry(int Position, string Value, LiteralTypes Type)
        {
            this.Position = Position;
            this.Value = Value;
            this.Type = Type;
        }
    }

    public class FunctionMeta
    {
        public string Name { get; private set; }

        public FunctionMeta(string Name) 
        { 
            this.Name = Name;
        }
    }

    public class CompilationMeta
    {
        public Dictionary<string, CompilationDataEntry> Variables = new Dictionary<string, CompilationDataEntry>();
        static int _dataCount = 0;
        static int[] _Registers = new int[8] { -1, -1, -1, -1, -1, -1, -1, -1 };
        static List<string> _VariableNames = new List<string>();
        static List<FunctionMeta> _Functions = new List<FunctionMeta>();

        public void AddFunction(string FunctionName)
        {
            _Functions.Add(new FunctionMeta(FunctionName));
        }

        public bool FunctionExists(string FunctionName)
        {
            return _Functions.Count(x=>x.Name == FunctionName) == 1;
        }

        public int LookupVariable(string Name)
        {
            if (!_VariableNames.Contains(Name))
                _VariableNames.Add(Name);
            return _VariableNames.IndexOf(Name);
        }

        public string LookupLabelByHashCode(int HashCode)
        {
            return Variables.Keys.ToList().First((x) => x == HashCode.ToString());
        }

        public string GetVariableType(string Name)
        {
            return Variables[Name].Type.ToString();
        }

        public void PushInt(string Name, int DefaultValue)
        {
            if (Variables.ContainsKey(Name))
                throw new Exception($"Variable {Name} already declared.");

            Variables.Add(Name, new CompilationDataEntry(_dataCount += 1, DefaultValue.ToString(), LiteralTypes.NUMBER));
        }

        public void PushString(string Name, string DefaultValue)
        {
            if (Variables.ContainsKey(Name))
                return; // throw new Exception($"Variable {Name} already declared.");

            Variables.Add(Name, new CompilationDataEntry(_dataCount += DefaultValue.Length, DefaultValue, LiteralTypes.STRING));
        }

        public int GetTemporaryRegister(int VariableIndex)
        {
            if (VariableIndex != -2)
            {
                for (int i = 0; i < _Registers.Length; i++)
                {
                    if (_Registers[i] == VariableIndex)
                    {
                        return i;
                    }
                }
            }

            for (int i = 0; i < _Registers.Length; i++)
            {
                if (_Registers[i] == -1)
                {
                    _Registers[i] = VariableIndex;
                    return i;
                }
            }

            throw new Exception("Register overflow exception");
        }

        public string[] GetDataSection()
        {
            List<string> DataSection = new List<string>();
            DataSection.Add(".data");
            foreach (var variableKey in Variables.Keys)
            {
                string? value = Variables[variableKey].GetValue();
                if (value == null) continue;
                DataSection.Add($"{variableKey}: {value}");
            }
            DataSection.Add(".main");
            return DataSection.ToArray();
        }
    }
}

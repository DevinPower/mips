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
        LiteralTypes Type;

        public string GetValue()
        {
            if (Type == LiteralTypes.NUMBER) return $".word {Value}";
            if (Type == LiteralTypes.STRING) return $".asciiz {Value}";
            return $";unhandled compilation data entry for {Value}";
        }

        public CompilationDataEntry(int Position, string Value, LiteralTypes Type)
        {
            this.Position = Position;
            this.Value = Value;
            this.Type = Type;
        }
    }

    public class CompilationMeta
    {
        public Dictionary<string, CompilationDataEntry> Variables = new Dictionary<string, CompilationDataEntry>();
        static int _dataCount = 0;
        static int[] _Registers = new int[8] { -1, -1, -1, -1, -1, -1, -1, -1 };

        public int LookupVariable(string Name)
        {
            return Variables[Name].Position;
        }

        public void PushInt(string Name, int DefaultValue)
        {
            if (Variables.ContainsKey(Name))
                return;// throw new Exception($"Variable {Name} already declared.");

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
            for (int i = 0; i < _Registers.Length; i++)
            {
                if (_Registers[i] == VariableIndex)
                {
                    return i;
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
                DataSection.Add($"{variableKey}: {Variables[variableKey].GetValue()}");
            }

            return DataSection.ToArray();
        }
    }
}

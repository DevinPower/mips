using Lexer.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Lexer
{
    public class CompilationArgumentEntry : CompilationDataEntry
    {
        public int ArgumentPosition { get; private set; }
        public CompilationArgumentEntry(int ArgumentPosition, int Position, string Value, LiteralTypes Type) : base(Position, Value, Type)
        {
            this.ArgumentPosition = ArgumentPosition;
        }
    }

    public class CompilationPointerEntry : CompilationDataEntry
    {
        public string OriginalName { get; private set; }
        public CompilationPointerEntry(string OriginalName, int Position, string Value, LiteralTypes Type) : base(Position, Value, Type)
        {
            this.OriginalName = OriginalName;
        }
    }

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
        public int ArgumentCount { get; private set; }

        public FunctionMeta(string Name, int ArgumentCount) 
        { 
            this.Name = Name;
            this.ArgumentCount = ArgumentCount;
        }
    }

    public class Initialization
    {
        int _loadTo;
        int _loadValue;

        public Initialization(int LoadTo, int LoadValue)
        {
            _loadTo = LoadTo;
            _loadValue = LoadValue;
        }
    }

    public class CompilationMeta
    {
        static int _scopeIDCount = 0;
        CompilationMeta Parent;
        public Dictionary<string, CompilationDataEntry> Variables = new Dictionary<string, CompilationDataEntry>();
        int _dataCount = 0;
        int[] _Registers = new int[8] { -1, -1, -1, -1, -1, -1, -1, -1 };
        List<string> _VariableNames = new List<string>();
        List<FunctionMeta> _Functions = new List<FunctionMeta>();
        List<Initialization> _Initializations = new List<Initialization>();
        List<CompilationMeta> _SubScopes = new List<CompilationMeta>();

        public int ScopeID { get; private set; }

        public CompilationMeta() 
        {
            SetScopeID();
        }

        public CompilationMeta(CompilationMeta Parent)
        {
            this.Parent = Parent;
            SetScopeID();
        }

        void SetScopeID()
        {
            ScopeID = _scopeIDCount++;
        }

        public CompilationMeta AddSubScope()
        {
            CompilationMeta newScope = new CompilationMeta(this);
            _SubScopes.Add(newScope);
            return newScope;
        }

        public void AddFunction(string FunctionName, int ArgumentCount)
        {
            _Functions.Add(new FunctionMeta(FunctionName, ArgumentCount));
        }

        //Ascends up for scoping
        public bool FunctionExists(string FunctionName)
        {
            if (_Functions.Count(x=>x.Name == FunctionName) == 1)
            {
                return true;
            }
            else
            {
                if (Parent == null)
                    return false;

                return (Parent.FunctionExists(FunctionName));
            }
        }

        //Ascends up for scoping
        public int FunctionArgumentCount(string FunctionName)
        {
            if (_Functions.Count(x => x.Name == FunctionName) == 1)
            {
                return _Functions.First((x) => { return x.Name == FunctionName; }).ArgumentCount;
            }
            else
            {
                if (Parent == null)
                    throw new Exception($"Function {FunctionName} does not exist");

                return (Parent.FunctionArgumentCount(FunctionName));
            }
        }

        //Descends up for scoping
        public int LookupVariable(string Name)
        {
            if (!_VariableNames.Contains(Name))
                _VariableNames.Add(Name);
            return _VariableNames.IndexOf(Name) + (100 * ScopeID);
        }

        //public string LookupLabelByHashCode(int HashCode)
        //{
        //    return Variables.Keys.ToList().First((x) => x == HashCode.ToString());
        //}

        //Happens at compilation time, descends down
        public string GetReferenceLabelByPointer(string PointerName)
        {
            var match = Variables.FirstOrDefault((pair) =>
            {
                if (pair.Value is CompilationPointerEntry pointer)
                {
                    return pointer.OriginalName == PointerName;
                }
                return false;
            }, new KeyValuePair<string, CompilationDataEntry>("", null));

            if (match.Value == null && match.Key == "")
            {
                foreach(var subSCope in _SubScopes)
                {
                    var subMatch = subSCope.GetReferenceLabelByPointer(PointerName);
                    if (subMatch != "")
                        return subMatch;
                }
                return "";
            }

            return match.Key;
        }

        public string GetVariableType(string Name)
        {
            if (!Variables.ContainsKey(Name))
            {
                if (Parent == null)
                    throw new Exception($"Variable '{Name}' not found.");

                return Parent.GetVariableType(Name);
            }

            return Variables[Name].Type.ToString();
        }

        public void PushInt(string Name, int DefaultValue)
        {
            if (Variables.ContainsKey(Name))
                throw new Exception($"Variable {Name} already declared.");

            Variables.Add(Name, new CompilationDataEntry(_dataCount += 1, DefaultValue.ToString(), LiteralTypes.NUMBER));
        }

        public void PushArgument(string Name, int ArgumentPosition)
        {
            if (Variables.ContainsKey(Name))
                throw new Exception($"Variable {Name} already declared.");

            Variables.Add(Name, new CompilationArgumentEntry(ArgumentPosition, _dataCount += 1, "0", LiteralTypes.NUMBER));
        }

        public string PushStaticString(string Value)
        {
            string Label = Guid.NewGuid().ToString().Replace("-", "");

            PushString(Label, Value, true);

            return Label;
        }

        public void PushString(string Name, string DefaultValue, bool IsStatic)
        {
            if (Variables.ContainsKey(Name))
                throw new Exception($"Variable {Name} already declared.");

            if (!IsStatic)
            {
                string Label = Guid.NewGuid().ToString().Replace("-", "");
                Variables.Add(Label, new CompilationPointerEntry(Name, _dataCount + 1, "53", LiteralTypes.NUMBER));
            }

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

        public string[] GetProgram()
        {
            List<string> DataSection = new List<string>();
            DataSection.Add(".data");

            GetDataSectionRecusrive(DataSection);

            DataSection.Add(".main");
            return DataSection.ToArray();
        }

        void GetDataSectionRecusrive(List<string> CurrentList)
        {
            foreach (var variableKey in Variables.Keys)
            {
                string? value = Variables[variableKey].GetValue();
                if (value == null) continue;
                CurrentList.Add($"{variableKey}: {value}");
            }

            foreach (var subScope in _SubScopes)
            {
                subScope.GetDataSectionRecusrive(CurrentList);
            }
        }
    }
}

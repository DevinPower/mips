using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexer
{
    public class CompilationMeta
    {
        public Dictionary<string, int> Variables = new Dictionary<string, int>();
        static int _dataCount = 0;
        static int[] _Registers = new int[8] { -1, -1, -1, -1, -1, -1, -1, -1 };

        public int LookupVariable(string Name)
        {
            return Variables[Name];
        }

        public void PushVariable(string Name)
        {
            Variables.Add(Name, _dataCount++);
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
    }
}

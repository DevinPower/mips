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

        public int LookupVariable(string Name)
        {
            throw new NotImplementedException();
        }

        public void PushVariable(string Name)
        {

        }
    }
}

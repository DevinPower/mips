using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexer
{
    //TODO: Maybe not public
    public class IntermediaryCodeMeta
    {
        public string[] Code { get; }
        public bool ContinueProcessing { get; }

        public IntermediaryCodeMeta(string[] Code, bool ContinueProcessing)
        {
            this.Code = Code;
            this.ContinueProcessing = ContinueProcessing;
        }
    }
}

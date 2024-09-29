using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mips.Instructions
{
    public class OperationWrapper<T>
    {
        public int OpCode { get { return 0; } }
        public string OperationName { get; private set; }
        public int Funct { get; private set; }
        public Action<T> FunctionCall { get; private set; }
        public InputInstruction[] InputInstructions { get; private set; }

        public OperationWrapper(string OperationName, int OpCode, Action<T> FunctionCall, InputInstruction[] InputInstructions)
        {
            this.OperationName = OperationName;
            this.Funct = OpCode;
            this.FunctionCall = FunctionCall;
            this.InputInstructions = InputInstructions;
        }

        public SoftOperationWrapper GetSoftWrapper()
        {
            return new SoftOperationWrapper(OperationName, OpCode, InputInstructions);
        }
    }

    public class SoftOperationWrapper
    {
        public int OpCode { get { return 0; } }
        public string OperationName { get; private set; }
        public int Funct { get; private set; }
        public InputInstruction[] inputInstructions { get; private set; }

        public SoftOperationWrapper(string OperationName, int OpCode, InputInstruction[] InputInstructions)
        {
            this.OperationName = OperationName;
            this.Funct = OpCode;
            this.inputInstructions = InputInstructions;
        }
    }
}
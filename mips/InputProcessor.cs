using mips.Instructions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mips
{
    internal class InputProcessor
    {
        int _result;
        public InputProcessor(string Line, List<SoftOperationWrapper> AllOperations)
        {
            string[] SplitLine = Line.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            List<InputInstruction> Input = AllOperations.First((x) => x.OperationName == SplitLine[0]).inputInstructions.ToList();

            Input.Reverse();
            Func<string[], string, int, int>[] actions = new Func<string[], string, int, int>[]
            {
                ReadStatic, ReadRegister, ReadImmediate
            };

            int pointer = 0;
            int Result = 0;
            
            foreach (var item in Input)
            {
                int processResult = actions[(int)item.InstructionName].Invoke(SplitLine, item.InstructionValue, item.Length);

                Console.WriteLine($"PR: {processResult}");
                int mask = ((1 << item.Length) - 1) << pointer;

                Result &= ~mask;
                int shiftedValue = (processResult & ((1 << item.Length) - 1)) << pointer;

                Result |= shiftedValue;
                
                pointer += item.Length;
            }

            string binary = Convert.ToString((int)Result, 2).PadLeft(32, '0');
            ////Console.WriteLine(binary);
            WriteStringSeparations(binary);
            _result = Result;
        }

        public int GetResult()
        {
            return _result;
        }

        void WriteStringSeparations(string input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (i == 6 || i == 11 || i == 16 || i == 21 || i == 26)
                    Console.Write(' ');
                Console.Write(input[i]);
            }
            Console.Write('\n');
        }

        int ReadStatic(string[] FullLine, string StaticValue, int Length)
        {
            return Convert.ToInt32(StaticValue, 2);
        }

        int ReadRegister(string[] FullLine, string RegisterValue, int Length) 
        {
            int ParameterPosition = Int32.Parse(RegisterValue);
            Console.WriteLine("Lookup for " + FullLine[ParameterPosition]);
            return Computer.InstructionRegisterDefinitions.ToList().IndexOf(FullLine[ParameterPosition]);
        }

        int ReadImmediate(string[] FullLine, string ImmediateValue, int Length)
        {
            int ParameterPosition = Int32.Parse(ImmediateValue);
            return Convert.ToInt32(FullLine[ParameterPosition + 1]);
        }
    }

    public class InputInstruction
    {
        public enum InstructionType { ReadStatic, ReadRegister, ReadImmediate }
        public InstructionType InstructionName { get; set; }
        public string InstructionValue { get; set; }
        public int Length { get; set; }

        public InputInstruction(InstructionType InstructionName, string InstructionValue, int length)
        {
            this.InstructionName = InstructionName;
            this.InstructionValue = InstructionValue;
            Length = length;
        }
    }

    /*public class InputInstruction
    {
        public int Value { get; set; }
        List<string> Instructions;

        public InputInstruction(List<string> Instructions)
        {
            this.Instructions = Instructions;
        }

        public void SetResult(int Result)
        {
            this.Value = Result;
        }


    }*/
}



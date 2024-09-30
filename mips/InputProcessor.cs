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
        List<SoftOperationWrapper> AllOperations;
        Computer Owner;
        int addressPointer = 32;
        Dictionary<string, int> LabelPositions = new Dictionary<string, int>();
        List<int> ValidCommandLines = new List<int>();
        int WriteCommandLine = 0;

        public InputProcessor(Computer Owner, List<SoftOperationWrapper> AllOperations)
        {
            this.AllOperations = AllOperations;
            this.Owner = Owner;
        }

        public void FindLabels(string Line)
        {
            string[] SplitLine = Line.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (SplitLine[0][SplitLine[0].Length - 1] == ':')
            {
                string LabelName = SplitLine[0].Remove(SplitLine[0].Length - 1, 1);
                Line = Line.Substring(SplitLine[0].Length + 1, Line.Length - SplitLine[0].Length - 1);
                SplitLine = Line.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                LabelPositions.Add(LabelName, addressPointer);
            }

            if (SplitLine[0][0] == '.')
            {
                string LineRemainder = Line.Substring(SplitLine[0].Length, Line.Length - SplitLine[0].Length);
                string op = SplitLine[0].Remove(0, 1);
                SplitLine = LineRemainder.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                switch (op)
                {
                    case "data":
                        Owner.Jump(addressPointer);
                        break;
                    case "asciiz":
                        foreach (char c in LineRemainder)
                        {
                            Owner.StoreMemory((int)c, addressPointer++);
                        }
                        Owner.StoreMemory(0, addressPointer++);
                        //addressPointer += LineRemainder.Length + 1;
                        break;
                }

                return;
            }

            ValidCommandLines.Add(addressPointer);

            addressPointer++;
        }

        public int ProcessLine(string Line)
        {
            string[] SplitLine = Line.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (SplitLine[0][SplitLine[0].Length - 1] == ':')
            {
                SplitLine = Line.Substring(SplitLine[0].Length, Line.Length - SplitLine[0].Length).Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                Line = Line.Substring(SplitLine[0].Length, Line.Length - SplitLine[0].Length);
            }

            if (SplitLine[0][0] == '.')
            {
                return -1;
            }

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

                int mask = ((1 << item.Length) - 1) << pointer;

                Result &= ~mask;
                int shiftedValue = (processResult & ((1 << item.Length) - 1)) << pointer;

                Result |= shiftedValue;

                pointer += item.Length;
            }

            Owner.StoreMemory(Result, ValidCommandLines[WriteCommandLine++]);
            return Result;
        }

        public void DumpLabels()
        {
            Console.WriteLine("Labels found:");
            foreach(string key in LabelPositions.Keys)
            {
                Console.WriteLine($"{key}\t\t{LabelPositions[key]}");
            }
        }

        int ReadStatic(string[] FullLine, string StaticValue, int Length)
        {
            return Convert.ToInt32(StaticValue, 2);
        }

        int ReadRegister(string[] FullLine, string RegisterValue, int Length) 
        {
            int ParameterPosition = Int32.Parse(RegisterValue) + 1;

            if (LabelPositions.ContainsKey(FullLine[ParameterPosition]))
            {
                return LabelPositions[FullLine[ParameterPosition]];
            }

            return Computer.InstructionRegisterDefinitions.ToList().IndexOf(FullLine[ParameterPosition]);
        }

        int ReadImmediate(string[] FullLine, string ImmediateValue, int Length)
        {
            int ParameterPosition = Int32.Parse(ImmediateValue);

            if (LabelPositions.ContainsKey(FullLine[ParameterPosition + 1]))
            {
                return LabelPositions[FullLine[ParameterPosition + 1]];
            }

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



using mips.Instructions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net;

namespace mips
{
    internal class LabelPositionWrapper
    {
        public int LineNumber { get; set; }
        public string OriginalLine { get; private set; }
        public int StaticLineIndex { get; private set; }

        public LabelPositionWrapper(int LineNumber, string OriginalLine, int StaticLineIndex) 
        {
            this.LineNumber = LineNumber;
            this.OriginalLine = OriginalLine;
            this.StaticLineIndex = StaticLineIndex;
        }
    }

    internal class InputProcessor
    {
        List<SoftOperationWrapper> AllOperations;
        Computer Owner;
        int addressPointer = 33;
        int initialAddressPointer = 34;
        Dictionary<string, LabelPositionWrapper> LabelPositions = new Dictionary<string, LabelPositionWrapper>();
        List<int> ValidCommandLines = new List<int>();
        int WriteCommandLine = 0;

        Dictionary<string, Func<Match, string[]>> _pseudoInstructions;

        public InputProcessor(Computer Owner, List<SoftOperationWrapper> AllOperations, int startIndex)
        {
            this.AllOperations = AllOperations;
            this.Owner = Owner;
            addressPointer = startIndex;
            initialAddressPointer = startIndex;

            InitializePseudoInstructions();
        }

        void InitializePseudoInstructions()
        {
            _pseudoInstructions = new Dictionary<string, Func<Match, string[]>>();
            _pseudoInstructions.Add(@"Li\s+(\$\w+),\s*(-?\w+)", PI_li);
            _pseudoInstructions.Add(@"La\s+(\$\w+),\s*(\w+)", PI_la);
            _pseudoInstructions.Add(@"Move\s+(\$\w+),\s*(\$\w+)", PI_move);
        }

        string[] PI_li(Match RegexResults)
        {
            if (Int32.TryParse(RegexResults.Groups[2].Value, out int result) && Math.Abs(result) > 65535)
            {
                string hexValue = result.ToString("X");
                hexValue = hexValue.PadLeft(8, '0');

                int upper16 = Convert.ToInt32(hexValue.Substring(0, 4), 16);
                int lower16 = Convert.ToInt32(hexValue.Substring(4, 4), 16);

                return new string[] {  $"Lui $at, {upper16}",
                    $"Ori {RegexResults.Groups[1].Value}, $at, {lower16}" };
            }

            return new string[] { $"Ori {RegexResults.Groups[1].Value}, $zero, {RegexResults.Groups[2].Value}" };
        }

        string[] PI_la(Match RegexResults)
        {
            int addressLine = LabelPositions[RegexResults.Groups[2].Value].LineNumber;
            return new string[] { $"Ori {RegexResults.Groups[1].Value}, $zero, {addressLine}" };
        }

        string[] PI_move(Match RegexResults)
        {
            return new string[] { $"Add {RegexResults.Groups[1].Value}, {RegexResults.Groups[2].Value}, $zero" };
        }

        public string GetLineWithoutComments(string Line)
        {
            bool inQuotes = false;

            for (int i = 0; i < Line.Length; i++)
            {
                char c = Line[i];
                if (c == '"')
                    inQuotes = !inQuotes;

                if (c == ';' && !inQuotes)
                    return Line.Substring(0, i);
            }

            return Line;
        }

        public string[] CheckPseudoInstructions(string Line)
        {
            foreach(string PseudoInstruction in _pseudoInstructions.Keys)
            {
                Match match = Regex.Match(Line, PseudoInstruction);
                if (match.Success)
                {
                    string Label = "";
                    if (Line.Contains(':'))
                        Label = Line.Split(':')[0] + ": ";

                    string[] Lines = _pseudoInstructions[PseudoInstruction].Invoke(match);
                    Lines[0] = Label + Lines[0];

                    return Lines;
                }
            }

            return new string[] { Line };
        }

        public void BumpLabels(int Index, int Amount)
        {
            foreach(string Key in LabelPositions.Keys)
            {
                if (LabelPositions[Key].StaticLineIndex >= Index)
                    LabelPositions[Key].LineNumber += Amount;
            }
        }

        public void FindLabels(string Line, int StaticLineCount)
        {
            string[] SplitLine = Line.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (SplitLine.Length == 0)
                return;

            if (SplitLine[0][SplitLine[0].Length - 1] == ':')
            {
                string LabelName = SplitLine[0].Remove(SplitLine[0].Length - 1, 1);
                if (SplitLine.Length == 1)
                {
                    LabelPositions.Add(LabelName, new LabelPositionWrapper(addressPointer, Line, StaticLineCount));
                    return;
                }

                Line = Line.Substring(SplitLine[0].Length + 1, Line.Length - SplitLine[0].Length - 1);
                SplitLine = Line.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                LabelPositions.Add(LabelName, new LabelPositionWrapper(addressPointer, Line, StaticLineCount));
            }

            if (SplitLine[0][0] == '.')
            {
                string LineRemainder = Line.Substring(SplitLine[0].Length, Line.Length - SplitLine[0].Length);
                string op = SplitLine[0].Remove(0, 1);
                SplitLine = LineRemainder.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                switch (op)
                {
                    case "main":
                        Owner.Jump(addressPointer);
                        break;
                    case "asciiz":
                        bool open = false;
                        int count = 0;
                        int[] fullString = new int[LineRemainder.Length - 2];
                        foreach (char c in LineRemainder)
                        {
                            if (c == '"')
                            {
                                if (open)
                                    break;

                                open = true;
                                continue;
                            }

                            if (open)
                            {
                                fullString[count] = (int)c;
                                count++;
                            }
                        }

                        addressPointer += fullString.Length;
                        fullString.ToList().ForEach((x) =>
                        {
                            Owner.StoreMemory(x);
                        });
                        break;
                    case "word":
                        if (Int32.TryParse(LineRemainder.Trim(), out int result))
                        {
                            for (int i = 0; i < result; i++)
                            {
                                addressPointer++;
                                Owner.StoreMemory(0);
                            }
                        }
                        else
                        {
                            throw new Exception("Expected int and didn't get one");
                        }
                        break;
                }

                return;
            }

            ValidCommandLines.Add(addressPointer);

            addressPointer++;
        }

        public void AddLabel(string Name, int Index)
        {
            throw new NotImplementedException("Peripheral labelling not implemented");
            //LabelPositions.Add(Name, Index);
        }

        public int ProcessLine(string Line)
        {
            string[] SplitLine = Line.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (SplitLine.Length == 0)
                return -1;

            if (SplitLine[0][SplitLine[0].Length - 1] == ':')
            {
                SplitLine = Line.Substring(SplitLine[0].Length, Line.Length - SplitLine[0].Length).Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (SplitLine.Length == 0)
                    return -1;
                Line = Line.Substring(SplitLine[0].Length, Line.Length - SplitLine[0].Length);
            }

            if (SplitLine[0][0] == '.')
            {
                return -1;
            }

            var Matches = AllOperations.Where((x) => x.OperationName == SplitLine[0]);
            
            if (Matches.Count() == 0)
            {
                throw new Exception($"Operation '{SplitLine[0]}' unknown");
            }
            List<InputInstruction> Input = Matches.First().inputInstructions.ToList();
            
            Input.Reverse();

            Func<string[], string, int, int>[] actions = new Func<string[], string, int, int>[]
            {
                ReadStatic, ReadRegister, ReadImmediate, ReadCalculatedInner, ReadCalculatedOuter
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

            //Owner.StoreMemory(Result, ValidCommandLines[WriteCommandLine++]);
            Owner.StoreMemory(Result);
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
            int ParameterPosition = Int32.Parse(RegisterValue);

            if (LabelPositions.ContainsKey(FullLine[ParameterPosition]))
            {
                return LabelPositions[FullLine[ParameterPosition]].LineNumber;
            }

            int result = Computer.InstructionRegisterDefinitions.ToList().IndexOf(FullLine[ParameterPosition]);
            
            if (result == -1)
            {
                throw new Exception("Error reading parameter position");
            }

            return result;
        }

        int ReadCalculatedInner(string[] FullLine, string RegisterValue, int Length)
        {
            int ParameterPosition = Int32.Parse(RegisterValue);
            string[] parts = FullLine[ParameterPosition].Split(new char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);

            if (LabelPositions.ContainsKey(parts[1]))
            {
                return LabelPositions[parts[1]].LineNumber;
            }

            int registerIndex = Computer.InstructionRegisterDefinitions.ToList().IndexOf(parts[1]);
            if (registerIndex != -1)
                return registerIndex;

            return int.Parse(parts[1]);
        }

        int ReadCalculatedOuter(string[] FullLine, string RegisterValue, int Length)
        {
            int ParameterPosition = Int32.Parse(RegisterValue);
            string[] parts = FullLine[ParameterPosition].Split(new char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);

            if (LabelPositions.ContainsKey(parts[0]))
            {
                return LabelPositions[parts[0]].LineNumber;
            }

            int registerIndex = Computer.InstructionRegisterDefinitions.ToList().IndexOf(parts[0]);
            if (registerIndex != -1)
                return registerIndex;

            return int.Parse(parts[0]);
        }

        int ReadImmediate(string[] FullLine, string ImmediateValue, int Length)
        {
            int ParameterPosition = Int32.Parse(ImmediateValue);

            if (LabelPositions.ContainsKey(FullLine[ParameterPosition]))
            {
                return LabelPositions[FullLine[ParameterPosition]].LineNumber;
            }

            return Convert.ToInt32(FullLine[ParameterPosition]);
        }
    }

    public class InputInstruction
    {
        public enum InstructionType { ReadStatic, ReadRegister, ReadImmediate, CalculatedInner, CalculatedOuter }
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


